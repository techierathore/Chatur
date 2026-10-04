using Chatur.Core.Data;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Support;

/// <summary>
/// A fresh, fully migrated, temporary SQLite database — the same pattern
/// <c>Guards/RoleRightsGuardTests.cs</c> uses, shared so cluster K's action-layer tests do not each
/// repeat the migrate-and-delete boilerplate.
/// </summary>
public abstract class MigratedDatabaseFixture : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");

    /// <summary>Migrates a fresh database before every test in a derived class.</summary>
    protected MigratedDatabaseFixture()
    {
        ConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(ConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    /// <summary>The migrated database's own connection string.</summary>
    protected string ConnectionString { get; }

    /// <summary>Opens connections to <see cref="ConnectionString"/>, exactly as the real head would.</summary>
    protected IDbConnectionFactory CreateFactory() => new TestDbConnectionFactory(ConnectionString);

    /// <summary>The <c>RoleId</c> seeded for a role's code, e.g. <c>"verifier"</c>.</summary>
    protected int RoleIdFor(string aCode)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT RoleId FROM Role WHERE Code = @Code;";
        vCommand.Parameters.AddWithValue("@Code", aCode);
        return (int)(long)vCommand.ExecuteScalar()!;
    }

    /// <summary>Inserts a project row and returns its id, for tests that need one to attach data to.</summary>
    protected int CreateProject(string aName, string aPath)
    {
        using var vConnection = new SqliteConnection(ConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "INSERT INTO Project (Name, Path, Kind, LastOpenedUtc) VALUES (@Name, @Path, 'dotnet', NULL); SELECT last_insert_rowid();";
        vCommand.Parameters.AddWithValue("@Name", aName);
        vCommand.Parameters.AddWithValue("@Path", aPath);
        return (int)(long)vCommand.ExecuteScalar()!;
    }

    /// <summary>Deletes the temporary database file this fixture created. Virtual so a derived fixture that also creates temporary folders (e.g. a project's own files) can clean those up too.</summary>
    public virtual void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }
}
