using System.Reflection;
using DbUp;
using DbUp.Engine;
using DbUp.Sqlite;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Proves REQ-NFR-003: a database change that has shipped in a nightly build is never edited, a new
/// one is added, and installing that new one keeps the owner's data. The test plays out the real
/// scenario in two steps: first it applies only the two scripts a "previous nightly" would have had
/// (0001, 0002, by the exact embedded names DbUp journals), then it writes a row as if the owner had
/// made it on that build, then it runs the full, current <see cref="ChaturDbMigrator.Migrate"/> — the
/// "new nightly", which now includes every migration since, including cluster O's own additive
/// 0024-O-GuardIndexes.sql — and checks every row from the old build is still there.
/// </summary>
public sealed class MigrationUpgradeTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objConnectionString;

    public MigrationUpgradeTests()
    {
        objConnectionString = $"Data Source={objDatabasePath}";
    }

    /// <summary>
    /// When the owner made data on a build that only had the first two migrations, and the current
    /// build (with every migration since) is installed over it, then the owner's row survives and the
    /// new migration's own change is present.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-003 migrating over existing data keeps every row")]
    public void MigratingOverExistingDataKeepsEveryRow()
    {
        ApplyOnlyThePreviousNightlysScripts();
        var vOwnerFolderId = InsertOwnerData();

        var vUpgradeResult = ChaturDbMigrator.Migrate(objConnectionString);

        Assert.True(vUpgradeResult.Successful, vUpgradeResult.Error?.Message);
        AssertOwnerDataSurvived(vOwnerFolderId);
        AssertTheNewMigrationRan();
    }

    /// <summary>
    /// Reproduces "the previous nightly": a DbUp run over only 0001-InitialSchema.sql and
    /// 0002-SeedRoles.sql, using their exact embedded resource names so the later, full
    /// <see cref="ChaturDbMigrator.Migrate"/> call's journal recognizes them as already applied and
    /// skips them, applying only what is new — exactly what installing an update does.
    /// </summary>
    private void ApplyOnlyThePreviousNightlysScripts()
    {
        var vAssembly = Assembly.GetAssembly(typeof(ChaturDbMigrator))!;
        var vInitialSchemaName = vAssembly.GetManifestResourceNames().Single(aName => aName.EndsWith("0001-InitialSchema.sql", StringComparison.Ordinal));
        var vSeedRolesName = vAssembly.GetManifestResourceNames().Single(aName => aName.EndsWith("0002-SeedRoles.sql", StringComparison.Ordinal));

        var vScripts = new[]
        {
            new SqlScript(vInitialSchemaName, ReadEmbeddedResource(vAssembly, vInitialSchemaName)),
            new SqlScript(vSeedRolesName, ReadEmbeddedResource(vAssembly, vSeedRolesName)),
        };

        var vUpgrader = DeployChanges.To
            .SqliteDatabase(objConnectionString)
            .WithScripts(vScripts)
            .LogToConsole()
            .Build();

        var vResult = vUpgrader.PerformUpgrade();
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    private static string ReadEmbeddedResource(Assembly aAssembly, string aName)
    {
        using var vStream = aAssembly.GetManifestResourceStream(aName)!;
        using var vReader = new StreamReader(vStream);
        return vReader.ReadToEnd();
    }

    /// <summary>Writes a row as the owner would have, on the build that only had 0001 and 0002.</summary>
    private long InsertOwnerData()
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText =
            "INSERT INTO ProjectFolder (Path, CreatedUtc) VALUES ('/owner/projects', '2026-09-01T00:00:00Z'); SELECT last_insert_rowid();";
        return (long)vCommand.ExecuteScalar()!;
    }

    private void AssertOwnerDataSurvived(long aOwnerFolderId)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Path FROM ProjectFolder WHERE ProjectFolderId = @Id;";
        vCommand.Parameters.AddWithValue("@Id", aOwnerFolderId);
        var vPath = vCommand.ExecuteScalar() as string;

        Assert.Equal("/owner/projects", vPath);
    }

    /// <summary>Confirms 0024-O-GuardIndexes.sql actually ran, not merely that the upgrade reported success.</summary>
    private void AssertTheNewMigrationRan()
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND name = 'IXRoleRightRoleIdAction';";
        var vIndexName = vCommand.ExecuteScalar() as string;

        Assert.Equal("IXRoleRightRoleIdAction", vIndexName);
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
