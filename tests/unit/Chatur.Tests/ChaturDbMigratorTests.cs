using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="ChaturDbMigrator"/> against a real, temporary SQLite file.</summary>
public sealed class ChaturDbMigratorTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");

    /// <summary>
    /// When the migrations run against a fresh database, then every seeded role — Analyst, Architect,
    /// Flow master and Verifier — exists afterwards.
    /// </summary>
    [Fact]
    public void MigrateSeedsTheFourRoles()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";

        var vResult = ChaturDbMigrator.Migrate(vConnectionString);

        Assert.True(vResult.Successful, vResult.Error?.Message);

        using var vConnection = new SqliteConnection(vConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Code FROM Role ORDER BY Code;";
        using var vReader = vCommand.ExecuteReader();

        var vCodes = new List<string>();
        while (vReader.Read())
        {
            vCodes.Add(vReader.GetString(0));
        }

        Assert.Equal(new[] { "analyst", "architect", "flow-master", "verifier" }, vCodes);
    }

    /// <summary>Deletes the temporary database file this test created.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }
}
