using Chatur.Core.Data;
using Chatur.Core.Guards;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Guards;

/// <summary>
/// Tests for <see cref="RoleRightsGuard"/> against a real, migrated, temporary database (REQ-NFR-005:
/// one test per guard for what it allows and what it refuses).
/// </summary>
public sealed class RoleRightsGuardTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objConnectionString;

    /// <summary>Migrates a fresh database — the four seeded roles and their rights come from 0002-SeedRoles.sql.</summary>
    public RoleRightsGuardTests()
    {
        objConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(objConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    /// <summary>
    /// When the acting role's <c>RoleRight</c> row for the action is allowed — the flow-master's
    /// "edit-file" — then the guard allows the request.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-005 role rights guard allows an action the role may do")]
    public async Task EvaluateAllowsAnActionTheRoleMayDo()
    {
        var vSessionId = CreateSession("flow-master");
        var vGuard = new RoleRightsGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "edit-file", "Edit Program.cs"));

        Assert.True(vResult.Allowed);
        Assert.Null(vResult.RefusalReason);
    }

    /// <summary>
    /// When the acting role's <c>RoleRight</c> row for the action is not allowed — the analyst's
    /// "edit-file", refused by 0002-SeedRoles.sql — then the guard refuses the request and says why.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-005 role rights guard refuses an action the role may not do")]
    public async Task EvaluateRefusesAnActionTheRoleMayNotDo()
    {
        var vSessionId = CreateSession("analyst");
        var vGuard = new RoleRightsGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "edit-file", "Edit Program.cs"));

        Assert.False(vResult.Allowed);
        Assert.NotNull(vResult.RefusalReason);
    }

    /// <summary>
    /// When no <c>RoleRight</c> row exists at all for the role and action, then the guard refuses —
    /// a right must be granted explicitly, never assumed.
    /// </summary>
    [Fact]
    public async Task EvaluateRefusesAnActionWithNoRightRowAtAll()
    {
        var vSessionId = CreateSession("analyst");
        var vGuard = new RoleRightsGuard(CreateFactory());

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "an-action-nobody-seeded", "Something new"));

        Assert.False(vResult.Allowed);
        Assert.NotNull(vResult.RefusalReason);
    }

    private int CreateSession(string aRoleCode)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vProjectCommand = vConnection.CreateCommand();
        vProjectCommand.CommandText =
            "INSERT INTO Project (Name, Path, Kind, LastOpenedUtc) VALUES ('Sample', '/tmp/sample', 'dotnet', NULL); SELECT last_insert_rowid();";
        var vProjectId = (long)vProjectCommand.ExecuteScalar()!;

        using var vRoleCommand = vConnection.CreateCommand();
        vRoleCommand.CommandText = "SELECT RoleId FROM Role WHERE Code = @Code;";
        vRoleCommand.Parameters.AddWithValue("@Code", aRoleCode);
        var vRoleId = (long)vRoleCommand.ExecuteScalar()!;

        using var vSessionCommand = vConnection.CreateCommand();
        vSessionCommand.CommandText =
            "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@ProjectId, @RoleId, 'GoAhead', 'Active', '2026-09-22T00:00:00Z'); SELECT last_insert_rowid();";
        vSessionCommand.Parameters.AddWithValue("@ProjectId", vProjectId);
        vSessionCommand.Parameters.AddWithValue("@RoleId", vRoleId);
        return (int)(long)vSessionCommand.ExecuteScalar()!;
    }

    private IDbConnectionFactory CreateFactory() => new TestDbConnectionFactory(objConnectionString);

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
