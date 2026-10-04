using Chatur.Core.BuildRun;
using Chatur.Core.Changes;
using Chatur.Core.Files;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Dapper;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for the Workbench's file rows: saving (REQ-FN-044), opening in the owner's editor
/// (REQ-FN-045) and the machine's default program (REQ-FN-046), approving and rejecting a proposed
/// change (REQ-FN-033, REQ-FN-034) and the run target chosen last time (REQ-FN-010), against a
/// migrated temporary database and a temporary project folder.
/// </summary>
public sealed class WorkbenchFileAndChangeTests : MigratedDatabaseFixture
{
    private readonly string objProjectPath = Path.Combine(Path.GetTempPath(), $"chatur-tests-wb-{Guid.NewGuid():N}");
    private readonly FakeLauncher objLauncher = new();
    private readonly int objProjectId;

    /// <summary>Creates a project folder holding one file and a project row that points at it.</summary>
    public WorkbenchFileAndChangeTests()
    {
        Directory.CreateDirectory(objProjectPath);
        File.WriteAllText(Path.Combine(objProjectPath, "Program.cs"), "old text");
        objProjectId = CreateProject("Wb", objProjectPath);
    }

    /// <summary>When the owner changes the text and saves, then the file on disk holds the new text (REQ-FN-044).</summary>
    [Fact(DisplayName = "REQ-FN-044 saving writes the new text to disk")]
    public async Task SaveWritesTheNewText()
    {
        await CreateFiles().SaveAsync(objProjectId, "Program.cs", "new text");

        Assert.Equal("new text", File.ReadAllText(Path.Combine(objProjectPath, "Program.cs")));
    }

    /// <summary>When a save names a path outside the project, then it is refused and nothing is written (REQ-FN-044).</summary>
    [Fact(DisplayName = "REQ-FN-044 a save outside the project is refused")]
    public async Task SaveOutsideTheProjectIsRefused()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CreateFiles().SaveAsync(objProjectId, "../escaped.txt", "x"));

        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(objProjectPath)!, "escaped.txt")));
    }

    /// <summary>When the owner chooses his own editor for a file, then that program is launched with the file's full path (REQ-FN-045).</summary>
    [Fact(DisplayName = "REQ-FN-045 the owner's editor is launched with the file")]
    public async Task OwnersEditorOpensTheFile()
    {
        await CreateFiles().OpenInEditorAsync(objProjectId, "Program.cs");

        Assert.Equal(Path.Combine(objProjectPath, "Program.cs"), objLauncher.EditorPath);
        Assert.Null(objLauncher.DefaultAppPath);
    }

    /// <summary>When the owner opens a file with the system's default application, then the machine's own launcher gets the file (REQ-FN-046).</summary>
    [Fact(DisplayName = "REQ-FN-046 the system default application is launched with the file")]
    public async Task DefaultApplicationOpensTheFile()
    {
        await CreateFiles().OpenWithDefaultAppAsync(objProjectId, "Program.cs");

        Assert.Equal(Path.Combine(objProjectPath, "Program.cs"), objLauncher.DefaultAppPath);
        Assert.Null(objLauncher.EditorPath);
    }

    /// <summary>When the owner approves a change, then the file holds the new text and the row is Approved (REQ-FN-033).</summary>
    [Fact(DisplayName = "REQ-FN-033 approving writes the file and moves the row to approved")]
    public async Task ApproveWritesTheFileAndMarksTheRow()
    {
        var vChangeId = InsertChange("Program.cs", "old text", "approved text");

        await CreateChanges().ApproveAsync(vChangeId);

        Assert.Equal("approved text", File.ReadAllText(Path.Combine(objProjectPath, "Program.cs")));
        Assert.Equal("Approved", StatusOf(vChangeId));
    }

    /// <summary>When the owner rejects a change, then the file is untouched and the row is Rejected (REQ-FN-034).</summary>
    [Fact(DisplayName = "REQ-FN-034 rejecting leaves the file alone and moves the row to rejected")]
    public async Task RejectLeavesTheFileUntouched()
    {
        var vChangeId = InsertChange("Program.cs", "old text", "rejected text");

        await CreateChanges().RejectAsync(vChangeId);

        Assert.Equal("old text", File.ReadAllText(Path.Combine(objProjectPath, "Program.cs")));
        Assert.Equal("Rejected", StatusOf(vChangeId));
    }

    /// <summary>When a change is already settled, then approving it again does not write the file (REQ-FN-033, REQ-FN-035).</summary>
    [Fact(DisplayName = "REQ-FN-035 nothing is written for a change that was rejected")]
    public async Task ARejectedChangeIsNeverWritten()
    {
        var vChangeId = InsertChange("Program.cs", "old text", "rejected text");
        await CreateChanges().RejectAsync(vChangeId);

        await CreateChanges().ApproveAsync(vChangeId);

        Assert.Equal("old text", File.ReadAllText(Path.Combine(objProjectPath, "Program.cs")));
        Assert.Equal("Rejected", StatusOf(vChangeId));
    }

    /// <summary>When the owner built a target last time, then it is the first one offered and the rest follow (REQ-FN-010).</summary>
    [Fact(DisplayName = "REQ-FN-010 the target used last time is offered first")]
    public async Task LastUsedTargetIsOfferedFirst()
    {
        File.WriteAllText(Path.Combine(objProjectPath, "Wb.sln"), string.Empty);
        var vActions = new BuildRunActions(CreateFactory(), objLauncher);
        var vTargets = await vActions.TargetsAsync(objProjectId);
        var vSecond = vTargets[1];
        Assert.NotEqual(vTargets[0].RunTargetId, vSecond.RunTargetId);

        await vActions.BuildAsync(objProjectId, vSecond.RunTargetId);

        var vAfter = await new BuildRunActions(CreateFactory(), objLauncher).TargetsAsync(objProjectId);
        Assert.Equal(vSecond.RunTargetId, vAfter[0].RunTargetId);
    }

    /// <summary>Deletes the temporary project folder as well as the database.</summary>
    public override void Dispose()
    {
        if (Directory.Exists(objProjectPath))
        {
            Directory.Delete(objProjectPath, true);
        }

        base.Dispose();
    }

    private FileActions CreateFiles() => new(CreateFactory(), objLauncher);

    private ChangeActions CreateChanges() => new(CreateFactory(), CreateFiles());

    private int InsertChange(string aFilePath, string aBefore, string aAfter)
    {
        using var vConnection = CreateFactory().OpenConnection();
        var vSessionId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@objProjectId, (SELECT MIN(RoleId) FROM Role), 'AskFirst', 'Active', '2026-10-02T10:00:00Z'); SELECT last_insert_rowid();",
            new { objProjectId });
        return (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO Change (SessionId, FilePath, Before, After, Status) VALUES (@vSessionId, @aFilePath, @aBefore, @aAfter, 'Proposed'); SELECT last_insert_rowid();",
            new { vSessionId, aFilePath, aBefore, aAfter });
    }

    private string StatusOf(int aChangeId)
    {
        using var vConnection = CreateFactory().OpenConnection();
        return vConnection.ExecuteScalar<string>("SELECT Status FROM Change WHERE ChangeId = @aChangeId", new { aChangeId })!;
    }

    private sealed class FakeLauncher : IProcessLauncher
    {
        public string? EditorPath { get; private set; }

        public string? DefaultAppPath { get; private set; }

        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default)
        {
            EditorPath = aFilePath;
            return Task.CompletedTask;
        }

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default)
        {
            DefaultAppPath = aFilePath;
            return Task.CompletedTask;
        }

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default) =>
            Task.FromResult(0);
    }
}
