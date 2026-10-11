using Chatur.Core.Guards;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Guards;

/// <summary>
/// Tests for the "writes documents" right (<c>write-docs</c>): which roles hold it, and that
/// <see cref="RoleRightsGuard"/> lets a write through only when the fully resolved path is inside docs/.
/// </summary>
public sealed class DocumentRightsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-docs-{Guid.NewGuid():N}");
    private readonly string objConnectionString;

    /// <summary>Migrates a fresh database and lays out a project folder with docs/ and src/.</summary>
    public DocumentRightsTests()
    {
        objConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(objConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);
        Directory.CreateDirectory(Path.Combine(objProjectPath, "docs"));
        Directory.CreateDirectory(Path.Combine(objProjectPath, "src"));
    }

    /// <summary>When the Analyst, holding only "write-docs", writes docs/Plan.md, then the write is allowed.</summary>
    [Fact(DisplayName = "REQ-FN-103 a role with only write-docs may write inside docs")]
    public async Task WriteDocsAllowsAPathInsideDocs()
    {
        var vResult = await EvaluateAsync("analyst", "docs/Plan.md");

        Assert.True(vResult.Allowed, vResult.RefusalReason);
    }

    /// <summary>When the Verifier, holding only "write-docs", writes docs/sub/Report.md in a folder that does not exist yet, then the write is allowed.</summary>
    [Fact(DisplayName = "REQ-FN-103 a role with only write-docs may write a new folder inside docs")]
    public async Task WriteDocsAllowsANewFolderInsideDocs()
    {
        var vResult = await EvaluateAsync("verifier", "docs/new/deep/Report.md");

        Assert.True(vResult.Allowed, vResult.RefusalReason);
    }

    /// <summary>When a write-docs-only role writes <c>docs/../src/x.cs</c>, then it is refused and the reason names the path and the right it lacks.</summary>
    [Fact(DisplayName = "REQ-FN-103 a dot-dot path out of docs is refused with the reason")]
    public async Task WriteDocsRefusesDotDotOutOfDocs()
    {
        var vResult = await EvaluateAsync("analyst", "docs/../src/x.cs");

        Assert.False(vResult.Allowed);
        Assert.Contains("docs/../src/x.cs", vResult.RefusalReason);
        Assert.Contains("edit-file", vResult.RefusalReason);
    }

    /// <summary>When a write-docs-only role writes an absolute path outside docs/, then it is refused.</summary>
    [Fact(DisplayName = "REQ-FN-103 an absolute path outside docs is refused")]
    public async Task WriteDocsRefusesAnAbsolutePathOutsideDocs()
    {
        var vOutside = Path.Combine(objProjectPath, "src", "x.cs");

        var vResult = await EvaluateAsync("analyst", vOutside);

        Assert.False(vResult.Allowed);
        Assert.Contains(vOutside, vResult.RefusalReason);
    }

    /// <summary>When a write-docs-only role writes an absolute path that is inside docs/, then it is allowed.</summary>
    [Fact(DisplayName = "REQ-FN-103 an absolute path inside docs is allowed")]
    public async Task WriteDocsAllowsAnAbsolutePathInsideDocs()
    {
        var vResult = await EvaluateAsync("analyst", Path.Combine(objProjectPath, "docs", "A.md"));

        Assert.True(vResult.Allowed, vResult.RefusalReason);
    }

    /// <summary>When docs/link is a symlink to src/ and a write-docs-only role writes docs/link/x.cs, then it is refused because the path resolves outside docs/.</summary>
    [Fact(DisplayName = "REQ-FN-103 a symlink out of docs is refused")]
    public async Task WriteDocsRefusesASymlinkOutOfDocs()
    {
        var vLink = Path.Combine(objProjectPath, "docs", "link");
        var vTarget = Path.Combine(objProjectPath, "src");
        try
        {
            Directory.CreateSymbolicLink(vLink, vTarget);
        }
        catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
        {
            // Windows without the link privilege: a junction is the same escape and needs none.
            if (!OperatingSystem.IsWindows() || !TryCreateJunction(vLink, vTarget))
            {
                return;
            }
        }

        var vResult = await EvaluateAsync("analyst", "docs/link/x.cs");

        Assert.False(vResult.Allowed);
    }

    /// <summary>When docs/note.md is a symlink to src/x.cs, then writing docs/note.md is refused.</summary>
    [Fact(DisplayName = "REQ-FN-103 a file symlink out of docs is refused")]
    public async Task WriteDocsRefusesAFileSymlinkOutOfDocs()
    {
        var vLink = Path.Combine(objProjectPath, "docs", "note.md");
        try
        {
            File.CreateSymbolicLink(vLink, Path.Combine(objProjectPath, "src", "x.cs"));
        }
        catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var vResult = await EvaluateAsync("analyst", "docs/note.md");

        Assert.False(vResult.Allowed);
    }

    /// <summary>When a sibling folder is named docs-old, then a write-docs-only role writing docs-old/a.md is refused (prefix match is not containment).</summary>
    [Fact(DisplayName = "REQ-FN-103 a folder that only starts with docs is refused")]
    public async Task WriteDocsRefusesASiblingThatStartsWithDocs()
    {
        var vResult = await EvaluateAsync("analyst", "docs-old/a.md");

        Assert.False(vResult.Allowed);
    }

    /// <summary>When a write-docs-only role reads or builds, then the guard still decides by the rights it holds; write-docs adds nothing to run-build.</summary>
    [Fact(DisplayName = "REQ-FN-103 write-docs adds nothing to other tools")]
    public async Task WriteDocsDoesNotCoverOtherTools()
    {
        var vSessionId = CreateSession("analyst");
        var vGuard = new RoleRightsGuard(new TestDbConnectionFactory(objConnectionString));

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "run-build", "Build the project."));

        Assert.False(vResult.Allowed);
    }

    /// <summary>When the Flow master, holding edit-file, writes src/x.cs, then the write is allowed as before.</summary>
    [Fact(DisplayName = "REQ-FN-103 an edit-file holder still writes source")]
    public async Task EditFileHolderStillWritesSource()
    {
        var vResult = await EvaluateAsync("flow-master", "src/x.cs");

        Assert.True(vResult.Allowed, vResult.RefusalReason);
    }

    /// <summary>When the Flow master (edit-file only) writes docs/Plan.md, then the write is allowed: edit-file covers docs/.</summary>
    [Fact(DisplayName = "REQ-FN-103 an edit-file holder writes docs too")]
    public async Task EditFileHolderWritesDocs()
    {
        var vResult = await EvaluateAsync("flow-master", "docs/Plan.md");

        Assert.True(vResult.Allowed, vResult.RefusalReason);
    }

    /// <summary>When the seeded rights are read, then the Analyst, Architect and Verifier hold write-docs and the Flow master does not.</summary>
    [Theory(DisplayName = "REQ-FN-104 seeded write-docs per role")]
    [InlineData("analyst", 1)]
    [InlineData("architect", 1)]
    [InlineData("verifier", 1)]
    [InlineData("flow-master", 0)]
    public void SeededWriteDocsPerRole(string aRole, long aExpected)
    {
        Assert.Equal(aExpected, RightOf(aRole, "write-docs"));
    }

    /// <summary>When the seeded edit-file rights are read, then they are as before: the Analyst and Verifier without, the Architect and Flow master with.</summary>
    [Theory(DisplayName = "REQ-FN-104 every source-file right is as before")]
    [InlineData("analyst", 0)]
    [InlineData("architect", 1)]
    [InlineData("verifier", 0)]
    [InlineData("flow-master", 1)]
    public void SeededEditFileUnchanged(string aRole, long aExpected)
    {
        Assert.Equal(aExpected, RightOf(aRole, "edit-file"));
    }

    private static bool TryCreateJunction(string aLink, string aTarget)
    {
        using var vProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{aLink}\" \"{aTarget}\"")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        vProcess!.WaitForExit();
        return vProcess.ExitCode == 0 && Directory.Exists(aLink);
    }

    private async Task<GuardResult> EvaluateAsync(string aRole, string aPath)
    {
        var vSessionId = CreateSession(aRole);
        var vGuard = new RoleRightsGuard(new TestDbConnectionFactory(objConnectionString));
        return await vGuard.EvaluateAsync(new ToolRequest(vSessionId, "edit-file", $"Edit \"{aPath}\".", aPath, string.Empty, "text"));
    }

    private long RightOf(string aRole, string aAction)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();
        using var vCommand = vConnection.CreateCommand();
        vCommand.CommandText = "SELECT Allowed FROM RoleRight rr JOIN Role r ON r.RoleId = rr.RoleId WHERE r.Code = @Code AND rr.Action = @Action;";
        vCommand.Parameters.AddWithValue("@Code", aRole);
        vCommand.Parameters.AddWithValue("@Action", aAction);
        return (long)vCommand.ExecuteScalar()!;
    }

    private int CreateSession(string aRoleCode)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();

        using var vProjectCommand = vConnection.CreateCommand();
        vProjectCommand.CommandText =
            "INSERT INTO Project (Name, Path, Kind, LastOpenedUtc) VALUES ('Sample', @Path, 'dotnet', NULL); SELECT last_insert_rowid();";
        vProjectCommand.Parameters.AddWithValue("@Path", objProjectPath);
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

    /// <summary>Deletes the temporary database and project folder.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objProjectPath))
        {
            Directory.Delete(objProjectPath, true);
        }
    }
}
