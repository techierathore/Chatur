using System.Diagnostics;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.SourceControl;
using Chatur.Tests.Support;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.SourceControl;

/// <summary>
/// Tests for the working-copy half of <see cref="SourceControlActions"/> (REQ-FN-079, REQ-FN-080, REQ-UI-068), against a
/// throwaway git repository and a migrated database, both under the test project's own bin folder.
/// </summary>
public sealed class WorktreeActionsTests : IDisposable
{
    private readonly string objScratch = Path.Combine(AppContext.BaseDirectory, "scratch-worktrees", Guid.NewGuid().ToString("N"));
    private readonly string objRepository;
    private readonly string objCopies;
    private readonly string objConnectionString;
    private readonly SourceControlActions objActions;
    private readonly int objProjectId;

    /// <summary>Creates a repository with branches <c>main</c>, <c>feat/one</c> and <c>feat/two</c>, and a project row for it.</summary>
    public WorktreeActionsTests()
    {
        objRepository = Path.Combine(objScratch, "repo");
        objCopies = Path.Combine(objScratch, "copies");
        Directory.CreateDirectory(objRepository);
        Directory.CreateDirectory(objCopies);
        Git(objRepository, "init", "-b", "main");
        Git(objRepository, "config", "user.email", "chatur-tests@example.com");
        Git(objRepository, "config", "user.name", "Chatur Tests");
        File.WriteAllText(Path.Combine(objRepository, "notes.txt"), "first line\n");
        Git(objRepository, "add", "-A");
        Git(objRepository, "commit", "-m", "initial");
        Git(objRepository, "branch", "feat/one");
        Git(objRepository, "branch", "feat/two");

        objConnectionString = $"Data Source={Path.Combine(objScratch, "chatur.db")}";
        var vMigrated = ChaturDbMigrator.Migrate(objConnectionString);
        Assert.True(vMigrated.Successful, vMigrated.Error?.Message);
        using (var vConnection = new SqliteConnection(objConnectionString))
        {
            vConnection.Open();
            using var vInsert = vConnection.CreateCommand();
            vInsert.CommandText = "INSERT INTO Project (Name, Path, Kind) VALUES ('repo', $path, 'Folder'); SELECT last_insert_rowid();";
            vInsert.Parameters.AddWithValue("$path", objRepository);
            objProjectId = (int)(long)vInsert.ExecuteScalar()!;
        }

        objActions = new SourceControlActions(new TestDbConnectionFactory(objConnectionString));
    }

    /// <summary>When a copy of a branch is added, then the list shows its folder and its branch, and the folder holds the branch's files (REQ-FN-079).</summary>
    [Fact]
    public async Task AddedCopyIsListedWithItsPathAndBranch()
    {
        var vFolder = Path.Combine(objCopies, "one");

        await objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false);

        var vCopies = await objActions.ListWorkingCopiesAsync(objProjectId);
        Assert.Equal(2, vCopies.Count);
        Assert.True(vCopies[0].IsMain);
        var vAdded = Assert.Single(vCopies, aCopy => !aCopy.IsMain);
        Assert.Equal(Path.GetFullPath(vFolder), vAdded.Path);
        Assert.Equal("feat/one", vAdded.Branch);
        Assert.Equal("free", vAdded.State);
        Assert.True(File.Exists(Path.Combine(vFolder, "notes.txt")));
    }

    /// <summary>When the folder is inside the project's own folder, then the copy is refused and nothing is made.</summary>
    [Fact]
    public async Task FolderInsideTheProjectIsRefused()
    {
        var vFolder = Path.Combine(objRepository, "inside");

        await Assert.ThrowsAsync<InvalidOperationException>(() => objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false));

        Assert.False(Directory.Exists(vFolder));
    }

    /// <summary>When the folder already holds files, then the copy is refused.</summary>
    [Fact]
    public async Task FolderThatIsNotEmptyIsRefused()
    {
        var vFolder = Path.Combine(objCopies, "busy");
        Directory.CreateDirectory(vFolder);
        File.WriteAllText(Path.Combine(vFolder, "x.txt"), "x");

        await Assert.ThrowsAsync<InvalidOperationException>(() => objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false));
    }

    /// <summary>When a new branch is asked for, then it is made from the current commit and the copy sits on it.</summary>
    [Fact]
    public async Task NewBranchIsMadeForTheCopy()
    {
        var vFolder = Path.Combine(objCopies, "fresh");

        var vAdded = await objActions.AddWorkingCopyAsync(objProjectId, "feat/fresh", vFolder, true);

        Assert.Equal("feat/fresh", vAdded.Branch);
        Assert.Contains("feat/fresh", await objActions.BranchesAsync(objProjectId));
    }

    /// <summary>When a branch already has a copy, then the dialog no longer offers it (git allows one copy per branch).</summary>
    [Fact]
    public async Task ChoicesLeaveOutBranchesAlreadyCheckedOut()
    {
        await objActions.AddWorkingCopyAsync(objProjectId, "feat/one", Path.Combine(objCopies, "one"), false);

        var vChoices = await objActions.WorkingCopyChoicesAsync(objProjectId);

        Assert.DoesNotContain("main", vChoices.Branches);
        Assert.DoesNotContain("feat/one", vChoices.Branches);
        Assert.Contains("feat/two", vChoices.Branches);
        Assert.StartsWith(Path.GetFullPath(objScratch), vChoices.FolderRoot);
    }

    /// <summary>When a copy is removed, then it leaves the list and its folder is gone, even with a file nobody checked in (REQ-UI-068).</summary>
    [Fact]
    public async Task RemovedCopyLeavesTheListAndItsFolder()
    {
        var vFolder = Path.Combine(objCopies, "two");
        await objActions.AddWorkingCopyAsync(objProjectId, "feat/two", vFolder, false);
        File.WriteAllText(Path.Combine(vFolder, "left-over.txt"), "not checked in");

        await objActions.RemoveWorkingCopyAsync(objProjectId, vFolder);

        Assert.DoesNotContain(await objActions.ListWorkingCopiesAsync(objProjectId), aCopy => !aCopy.IsMain);
        Assert.False(Directory.Exists(vFolder));
        Assert.Equal(0, CountProjects(Path.GetFullPath(vFolder)));
    }

    /// <summary>When the owner's own folder is named, then it is refused and stays.</summary>
    [Fact]
    public async Task OwnFolderCannotBeRemoved()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objActions.RemoveWorkingCopyAsync(objProjectId, objRepository));

        Assert.True(Directory.Exists(objRepository));
    }

    /// <summary>When a run is going in a copy, then removing it is refused and the folder stays.</summary>
    [Fact]
    public async Task CopyWithARunGoingCannotBeRemoved()
    {
        var vFolder = Path.Combine(objCopies, "busy-run");
        await objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false);
        var vCopyProject = await objActions.WorkingCopyProjectAsync(objProjectId, vFolder);
        InsertRun(vCopyProject, "refresh-status", "running");

        var vEx = await Assert.ThrowsAsync<InvalidOperationException>(() => objActions.RemoveWorkingCopyAsync(objProjectId, vFolder));

        Assert.Contains("Stop the run first", vEx.Message);
        Assert.True(Directory.Exists(vFolder));
        var vCopy = Assert.Single(await objActions.ListWorkingCopiesAsync(objProjectId), aCopy => !aCopy.IsMain);
        Assert.Equal("in use", vCopy.State);
    }

    /// <summary>When a run is started in a copy, then it is recorded against a project row of the copy's folder that no list shows, never the owner's project (REQ-FN-080).</summary>
    [Fact]
    public async Task RunProjectOfACopyPointsAtTheCopyAndIsHidden()
    {
        var vFolder = Path.Combine(objCopies, "run-here");
        await objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false);

        var vCopyProject = await objActions.WorkingCopyProjectAsync(objProjectId, vFolder);
        var vAgain = await objActions.WorkingCopyProjectAsync(objProjectId, vFolder);

        Assert.NotEqual(objProjectId, vCopyProject);
        Assert.Equal(vCopyProject, vAgain);
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();
        using var vSelect = vConnection.CreateCommand();
        vSelect.CommandText = "SELECT Path, RemovedUtc FROM Project WHERE ProjectId = $id";
        vSelect.Parameters.AddWithValue("$id", vCopyProject);
        using var vReader = vSelect.ExecuteReader();
        Assert.True(vReader.Read());
        Assert.Equal(Path.GetFullPath(vFolder), vReader.GetString(0));
        Assert.False(vReader.IsDBNull(1));
    }

    /// <summary>When a run has ended in a copy, then the list names the run and the copy is finished, not free.</summary>
    [Fact]
    public async Task FinishedRunIsNamedOnItsCopy()
    {
        var vFolder = Path.Combine(objCopies, "done");
        await objActions.AddWorkingCopyAsync(objProjectId, "feat/one", vFolder, false);
        InsertRun(await objActions.WorkingCopyProjectAsync(objProjectId, vFolder), "refresh-status", "finished");

        var vCopy = Assert.Single(await objActions.ListWorkingCopiesAsync(objProjectId), aCopy => !aCopy.IsMain);

        Assert.Equal("finished", vCopy.State);
        Assert.Equal("refresh-status", vCopy.RunProcess);
        Assert.NotNull(vCopy.RunId);
    }

    /// <summary>When a folder is not a copy of the project, then no run project is made for it.</summary>
    [Fact]
    public async Task RunProjectForAStrangerFolderIsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objActions.WorkingCopyProjectAsync(objProjectId, objCopies));
    }

    /// <summary>Counts the project rows with a path.</summary>
    private int CountProjects(string aPath)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();
        using var vSelect = vConnection.CreateCommand();
        vSelect.CommandText = "SELECT COUNT(*) FROM Project WHERE Path = $path";
        vSelect.Parameters.AddWithValue("$path", aPath);
        return (int)(long)vSelect.ExecuteScalar()!;
    }

    private void InsertRun(int aProjectId, string aProcess, string aState)
    {
        using var vConnection = new SqliteConnection(objConnectionString);
        vConnection.Open();
        using var vInsert = vConnection.CreateCommand();
        vInsert.CommandText = "INSERT INTO ProcessRun (ProjectId, ProcessCode, State, CurrentStep, StartedUtc) VALUES ($p, $c, $s, 1, '2026-11-20T10:00:00.0000000Z')";
        vInsert.Parameters.AddWithValue("$p", aProjectId);
        vInsert.Parameters.AddWithValue("$c", aProcess);
        vInsert.Parameters.AddWithValue("$s", aState);
        vInsert.ExecuteNonQuery();
    }

    private static void Git(string aWorkingDirectory, params string[] aArguments)
    {
        var vStart = new ProcessStartInfo("git") { WorkingDirectory = aWorkingDirectory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var vArgument in aArguments)
        {
            vStart.ArgumentList.Add(vArgument);
        }

        using var vProcess = Process.Start(vStart)!;
        vProcess.StandardOutput.ReadToEnd();
        vProcess.StandardError.ReadToEnd();
        vProcess.WaitForExit();
        Assert.Equal(0, vProcess.ExitCode);
    }

    /// <summary>Deletes the scratch folder, repository, copies and database this test made.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(objScratch))
        {
            foreach (var vFile in Directory.EnumerateFiles(objScratch, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(vFile, FileAttributes.Normal);
            }

            Directory.Delete(objScratch, recursive: true);
        }
    }
}
