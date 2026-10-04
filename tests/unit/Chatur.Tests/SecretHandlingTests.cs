using System.Collections.Concurrent;
using Chatur.Core.Platform;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Serilog;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-001: every secret is kept in the operating system's secret store, never in the
/// database, a file or a log. The test plays out the acceptance criterion literally — connect a
/// provider, then scan the database file and the log file for the secret's own text — against the
/// real, migrated database schema and a real Serilog file sink, so a future change that logs a
/// secret or writes it to a column fails this test.
/// </summary>
public sealed class SecretHandlingTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objLogFolder = Path.Combine(Path.GetTempPath(), $"chatur-nfr001-{Guid.NewGuid():N}");

    public SecretHandlingTests()
    {
        Directory.CreateDirectory(objLogFolder);
    }

    /// <summary>
    /// When the <c>Provider</c> table's columns are inspected, then none of them but
    /// <c>SecretName</c> — which holds only the name a secret is filed under, never the secret
    /// itself — could hold a secret's value.
    /// </summary>
    [Fact]
    public void ProviderTableHasNoColumnForTheSecretItself()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";
        var vMigrationResult = ChaturDbMigrator.Migrate(vConnectionString);
        Assert.True(vMigrationResult.Successful, vMigrationResult.Error?.Message);

        using var vConnection = new SqliteConnection(vConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "PRAGMA table_info(Provider);";
        using var vReader = vCommand.ExecuteReader();

        var vColumns = new List<string>();
        while (vReader.Read())
        {
            vColumns.Add(vReader.GetString(vReader.GetOrdinal("name")));
        }

        Assert.Equal(
            new[] { "ProviderId", "Name", "Connector", "SignInMethod", "BaseUrl", "SecretName", "State" },
            vColumns);
    }

    /// <summary>
    /// When the owner connects a provider — a secret saved to the secret store and a
    /// <c>Provider</c> row filed only under the secret's name — and the database file and the log
    /// file are read afterwards, then the secret's own text is in neither.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-001 a connected provider secret stays out of the database and the log")]
    public async Task ConnectingAProviderLeavesTheSecretOutOfTheDatabaseAndTheLog()
    {
        const string cSecret = "sk-live-4f9a2c7e1b3d4a5f8901ffea";
        const string cSecretName = "provider:openai:api-key";

        var vConnectionString = $"Data Source={objDatabasePath}";
        var vMigrationResult = ChaturDbMigrator.Migrate(vConnectionString);
        Assert.True(vMigrationResult.Successful, vMigrationResult.Error?.Message);

        // A local logger instance, never Serilog's static ambient Log.Logger — this test runs in
        // parallel with others (e.g. SerilogStartupTests) that legitimately mutate that same static
        // field to reproduce production's startup shape, so sharing it here would race. Disposed
        // explicitly below (before reading the log file back), not via `using`, which would only
        // flush at method exit.
        var vLogger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(objLogFolder, "connect-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var vSecretStore = new RecordingSecretStore();

        // The connect flow: the secret goes to the store under a name; the log names the
        // provider, never the secret; the database row keeps only the name.
        vLogger.Information("Connecting provider {ProviderName}", "OpenAI");
        await vSecretStore.SaveAsync(cSecretName, cSecret);

        using (var vConnection = new SqliteConnection(vConnectionString))
        {
            vConnection.Open();
            using var vCommand = vConnection.CreateCommand();
            vCommand.CommandText =
                "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) " +
                "VALUES ('OpenAI', 'openai', 'ApiKey', 'https://api.openai.com', @SecretName, 'Connected');";
            vCommand.Parameters.AddWithValue("@SecretName", cSecretName);
            vCommand.ExecuteNonQuery();
        }

        vLogger.Dispose();

        // Microsoft.Data.Sqlite pools connections and keeps the file open on Windows; release it so
        // the raw read below is not refused as "used by another process".
        SqliteConnection.ClearAllPools();

        // The secret store itself holds the secret and can give it back.
        Assert.Equal(cSecret, await vSecretStore.ReadAsync(cSecretName));

        // The database file, read as raw bytes and as text, never contains the secret.
        var vDatabaseBytes = await File.ReadAllBytesAsync(objDatabasePath);
        var vDatabaseAsLatin1 = System.Text.Encoding.Latin1.GetString(vDatabaseBytes);
        Assert.DoesNotContain(cSecret, vDatabaseAsLatin1, StringComparison.Ordinal);

        // The database row keeps only the name the secret is filed under.
        using (var vCheckConnection = new SqliteConnection(vConnectionString))
        {
            vCheckConnection.Open();
            using var vCheckCommand = vCheckConnection.CreateCommand();
            vCheckCommand.CommandText = "SELECT SecretName FROM Provider WHERE Name = 'OpenAI';";
            Assert.Equal(cSecretName, vCheckCommand.ExecuteScalar() as string);
        }

        // The log file never contains the secret either.
        var vLogFile = Directory.GetFiles(objLogFolder, "connect-*.log").Single();
        var vLogText = await File.ReadAllTextAsync(vLogFile);
        Assert.DoesNotContain(cSecret, vLogText, StringComparison.Ordinal);
        Assert.Contains("Connecting provider", vLogText, StringComparison.Ordinal);
    }

    /// <summary>
    /// An in-memory <see cref="ISecretStore"/> standing in for the real Credential Manager / Keychain
    /// store this test cannot reach on this machine — named honestly for what it is, matching
    /// <c>Chatur.WebHarness.Services.HarnessSecretStore</c>'s own shape.
    /// </summary>
    private sealed class RecordingSecretStore : ISecretStore
    {
        private readonly ConcurrentDictionary<string, string> objSecrets = new();

        public Task SaveAsync(string aName, string aSecret, CancellationToken aCt = default)
        {
            objSecrets[aName] = aSecret;
            return Task.CompletedTask;
        }

        public Task<string?> ReadAsync(string aName, CancellationToken aCt = default) =>
            Task.FromResult(objSecrets.TryGetValue(aName, out var vSecret) ? vSecret : null);

        public Task DeleteAsync(string aName, CancellationToken aCt = default)
        {
            objSecrets.TryRemove(aName, out _);
            return Task.CompletedTask;
        }
    }

    /// <summary>Deletes the temporary database file and log folder this test created.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objLogFolder))
        {
            Directory.Delete(objLogFolder, recursive: true);
        }
    }
}
