using Chatur.Core.Guards;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Guards;

/// <summary>
/// Tests for <see cref="AskMeFirstGuard"/> against a real, migrated, temporary database (REQ-NFR-005:
/// one test per guard for what it allows and what it refuses).
/// </summary>
public sealed class AskMeFirstGuardTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objConnectionString;

    /// <summary>Migrates a fresh database.</summary>
    public AskMeFirstGuardTests()
    {
        objConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(objConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    /// <summary>
    /// When the session is in "ask me first" and the tool would write a file, then the guard refuses
    /// the request (REQ-FN-035).
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-005 ask-me-first guard refuses a write when the session asks first")]
    public async Task EvaluateRefusesAWriteWhenTheSessionAsksFirst()
    {
        var vSessionId = CreateSession("AskFirst");
        var vGuard = new AskMeFirstGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "edit-file", "Edit Program.cs"));

        Assert.False(vResult.Allowed);
        Assert.NotNull(vResult.RefusalReason);
    }

    /// <summary>
    /// When the session is in "go ahead", then the same write tool passes the guard without stopping.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-005 ask-me-first guard allows a write when the session goes ahead")]
    public async Task EvaluateAllowsAWriteWhenTheSessionGoesAhead()
    {
        var vSessionId = CreateSession("GoAhead");
        var vGuard = new AskMeFirstGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "edit-file", "Edit Program.cs"));

        Assert.True(vResult.Allowed);
        Assert.Null(vResult.RefusalReason);
    }

    /// <summary>
    /// When the session is in "ask me first" but the tool only reads, then the guard allows it — the
    /// hold applies to writes, never to reads.
    /// </summary>
    [Fact]
    public async Task EvaluateAllowsAReadEvenWhenTheSessionAsksFirst()
    {
        var vSessionId = CreateSession("AskFirst");
        var vGuard = new AskMeFirstGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "read-file", "Read Program.cs"));

        Assert.True(vResult.Allowed);
    }

    private int CreateSession(string aMode)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vProjectCommand = vConnection.CreateCommand();
        vProjectCommand.CommandText =
            "INSERT INTO Project (Name, Path, Kind, LastOpenedUtc) VALUES ('Sample', '/tmp/sample', 'dotnet', NULL); SELECT last_insert_rowid();";
        var vProjectId = (long)vProjectCommand.ExecuteScalar()!;

        using var vRoleCommand = vConnection.CreateCommand();
        vRoleCommand.CommandText = "SELECT RoleId FROM Role WHERE Code = 'flow-master';";
        var vRoleId = (long)vRoleCommand.ExecuteScalar()!;

        using var vSessionCommand = vConnection.CreateCommand();
        vSessionCommand.CommandText =
            "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@ProjectId, @RoleId, @Mode, 'Active', '2026-09-22T00:00:00Z'); SELECT last_insert_rowid();";
        vSessionCommand.Parameters.AddWithValue("@ProjectId", vProjectId);
        vSessionCommand.Parameters.AddWithValue("@RoleId", vRoleId);
        vSessionCommand.Parameters.AddWithValue("@Mode", aMode);
        return (int)(long)vSessionCommand.ExecuteScalar()!;
    }

    private TestDbConnectionFactory CreateFactory() => new(objConnectionString);

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
