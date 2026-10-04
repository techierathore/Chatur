using System.Data;
using System.Diagnostics;
using Chatur.Core.Data;
using Chatur.Core.SourceControl;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="SourceControlActions"/>'s branch listing, branch switching and the run's own
/// automatic check-in (REQ-FN-047, REQ-FN-048), against a real, throwaway git repository created in
/// a temporary folder — never the Chatur repository itself.
/// </summary>
public sealed class SourceControlActionsTests : IDisposable
{
    private readonly string objRepositoryPath = Path.Combine(Path.GetTempPath(), $"chatur-tests-repo-{Guid.NewGuid():N}");
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly SourceControlActions objActions;
    private readonly int objProjectId;

    /// <summary>Creates a throwaway git repository with one commit on <c>main</c>, and a database holding one project row that points at it.</summary>
    public SourceControlActionsTests()
    {
        Directory.CreateDirectory(objRepositoryPath);
        RunGit(objRepositoryPath, "init", "-b", "main");
        RunGit(objRepositoryPath, "config", "user.email", "chatur-tests@techierathore.com");
        RunGit(objRepositoryPath, "config", "user.name", "Chatur Tests");
        File.WriteAllText(Path.Combine(objRepositoryPath, "readme.txt"), "first check-in");
        RunGit(objRepositoryPath, "add", "-A");
        RunGit(objRepositoryPath, "commit", "-m", "first check-in");

        var vConnectionFactory = new TestDbConnectionFactory(objDatabasePath);
        using (var vConnection = vConnectionFactory.OpenConnection())
        {
            using (var vCreate = vConnection.CreateCommand())
            {
                vCreate.CommandText = "CREATE TABLE Project (ProjectId INTEGER PRIMARY KEY, Path TEXT NOT NULL);";
                vCreate.ExecuteNonQuery();
            }

            using (var vInsert = vConnection.CreateCommand())
            {
                vInsert.CommandText = "INSERT INTO Project (Path) VALUES ($path); SELECT last_insert_rowid();";
                var vParameter = vInsert.CreateParameter();
                vParameter.ParameterName = "$path";
                vParameter.Value = objRepositoryPath;
                vInsert.Parameters.Add(vParameter);
                objProjectId = Convert.ToInt32(vInsert.ExecuteScalar());
            }
        }

        objActions = new SourceControlActions(vConnectionFactory);
    }

    /// <summary>When branches exist on the repository, then every one of them is listed.</summary>
    [Fact]
    public async Task BranchesListsEveryBranch()
    {
        RunGit(objRepositoryPath, "branch", "feature/board");

        var vBranches = await objActions.BranchesAsync(objProjectId);

        Assert.Contains("main", vBranches);
        Assert.Contains("feature/board", vBranches);
    }

    /// <summary>When the owner switches to another branch, then the repository's own HEAD moves to it (REQ-FN-047).</summary>
    [Fact]
    public async Task SwitchBranchChecksOutTheNamedBranch()
    {
        RunGit(objRepositoryPath, "branch", "feature/board");

        await objActions.SwitchBranchAsync(objProjectId, "feature/board");

        Assert.Equal("feature/board", CurrentBranch());
    }

    /// <summary>When the named branch does not exist, then switching to it fails with git's own message rather than silently doing nothing.</summary>
    [Fact]
    public async Task SwitchBranchFailsForAnUnknownBranch()
    {
        var vException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => objActions.SwitchBranchAsync(objProjectId, "no-such-branch"));

        Assert.Contains("no-such-branch", vException.Message);
    }

    /// <summary>When a run checks itself in, then the commit lands on the run's own branch, carrying the requirement id, and the owner's branch is left checked out afterwards (REQ-FN-048).</summary>
    [Fact(DisplayName = "REQ-FN-048 an automatic check-in goes to the run's own branch with the requirement id")]
    public async Task RunBranchCheckInCommitsOnlyToTheRunsOwnBranch()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "run-output.txt"), "written by a run");

        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-047", new[] { "run-output.txt" }, "[REQ-FN-047] switch branch");

        Assert.Equal("main", CurrentBranch());
        Assert.Contains("[REQ-FN-047]", RunGit(objRepositoryPath, "log", "run/REQ-FN-047", "-1", "--pretty=%s"));
        Assert.DoesNotContain("[REQ-FN-047]", RunGit(objRepositoryPath, "log", "main", "--pretty=%s"));
    }

    /// <summary>When a second run checks in on the same run branch, then both commits are on it, one after the other.</summary>
    [Fact]
    public async Task RunBranchCheckInReusesAnExistingRunBranch()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "run-output.txt"), "first run");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-048", new[] { "run-output.txt" }, "[REQ-FN-048] first automatic check-in");

        File.WriteAllText(Path.Combine(objRepositoryPath, "run-output.txt"), "second run");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-048", new[] { "run-output.txt" }, "[REQ-FN-048] second automatic check-in");

        var vLog = RunGit(objRepositoryPath, "log", "run/REQ-FN-048", "--pretty=%s");
        Assert.Contains("[REQ-FN-048] first automatic check-in", vLog);
        Assert.Contains("[REQ-FN-048] second automatic check-in", vLog);
        Assert.Equal("main", CurrentBranch());
    }

    /// <summary>When the run has no files to check in, then the request is refused before git is ever asked to do anything.</summary>
    [Fact]
    public async Task RunBranchCheckInRefusesAnEmptyFileList()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-048", Array.Empty<string>(), "[REQ-FN-048] nothing to commit"));
    }

    /// <summary>When the repository has check-ins, then the history lists them newest first with their message, author and short hash, and none is marked as a process's.</summary>
    [Fact]
    public async Task HistoryListsCheckInsNewestFirst()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "second.txt"), "two");
        RunGit(objRepositoryPath, "add", "-A");
        RunGit(objRepositoryPath, "commit", "-m", "[REQ-UI-044] second check-in");

        var vHistory = await objActions.HistoryAsync(objProjectId, 10);

        Assert.Equal(2, vHistory.Count);
        Assert.Equal("[REQ-UI-044] second check-in", vHistory[0].Message);
        Assert.Equal("first check-in", vHistory[1].Message);
        Assert.Equal("Chatur Tests", vHistory[0].Author);
        Assert.Equal(7, vHistory[0].Hash.Length);
        Assert.All(vHistory, aEntry => Assert.False(aEntry.WrittenByProcess));
    }

    /// <summary>When the history is asked for fewer check-ins than exist, then only the newest are returned.</summary>
    [Fact]
    public async Task HistoryStopsAtTheRequestedCount()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "second.txt"), "two");
        RunGit(objRepositoryPath, "add", "-A");
        RunGit(objRepositoryPath, "commit", "-m", "second check-in");

        var vHistory = await objActions.HistoryAsync(objProjectId, 1);

        Assert.Single(vHistory);
        Assert.Equal("second check-in", vHistory[0].Message);
    }

    /// <summary>When the repository has no check-in yet, then the history is empty rather than an error.</summary>
    [Fact]
    public async Task HistoryOfARepositoryWithNoCheckInsIsEmpty()
    {
        var vEmptyPath = Path.Combine(Path.GetTempPath(), $"chatur-tests-empty-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vEmptyPath);
        try
        {
            RunGit(vEmptyPath, "init", "-b", "main");
            var vFactory = new TestDbConnectionFactory(objDatabasePath);
            using (var vConnection = vFactory.OpenConnection())
            {
                using var vInsert = vConnection.CreateCommand();
                vInsert.CommandText = "INSERT INTO Project (Path) VALUES ($path); SELECT last_insert_rowid();";
                var vParameter = vInsert.CreateParameter();
                vParameter.ParameterName = "$path";
                vParameter.Value = vEmptyPath;
                vInsert.Parameters.Add(vParameter);
                var vEmptyProjectId = Convert.ToInt32(vInsert.ExecuteScalar());

                Assert.Empty(await new SourceControlActions(vFactory).HistoryAsync(vEmptyProjectId, 10));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(vEmptyPath, recursive: true);
        }
    }

    /// <summary>When a run has checked in on its own branch, then the history of that branch marks the run's check-in as a process's and the owner's own as not (REQ-FN-048).</summary>
    [Fact]
    public async Task HistoryMarksACheckInOnlyARunBranchHolds()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "run-output.txt"), "written by a run");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-048", new[] { "run-output.txt" }, "[REQ-FN-048] by a process");
        File.Delete(Path.Combine(objRepositoryPath, "run-output.txt")); // git refuses to check a branch out over an untracked file it also holds
        await objActions.SwitchBranchAsync(objProjectId, "run/REQ-FN-048");

        var vHistory = await objActions.HistoryAsync(objProjectId, 10);

        Assert.True(vHistory.Single(aEntry => aEntry.Message == "[REQ-FN-048] by a process").WrittenByProcess);
        Assert.False(vHistory.Single(aEntry => aEntry.Message == "first check-in").WrittenByProcess);
    }

    /// <summary>When the owner stays on their own branch and a run has checked in on its own branch, then the history still lists the run's check-in, marked as a process's (REQ-FN-048).</summary>
    [Fact]
    public async Task HistoryListsARunCheckInWhileTheOwnerStaysOnTheirBranch()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "run-output.txt"), "written by a run");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-048", new[] { "run-output.txt" }, "[REQ-FN-048] by a process");

        var vHistory = await objActions.HistoryAsync(objProjectId, 10);

        Assert.True(vHistory.Single(aEntry => aEntry.Message == "[REQ-FN-048] by a process").WrittenByProcess);
    }

    /// <summary>When a process checked in on a run branch, then the process branches list it with its requirement ids, its check-in count and when it was last touched (REQ-FN-048).</summary>
    [Fact]
    public async Task ProcessBranchesListsARunBranchWithItsRequirementIds()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "a.txt"), "a");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-096", new[] { "a.txt" }, "[REQ-FN-096] first");
        File.WriteAllText(Path.Combine(objRepositoryPath, "b.txt"), "b");
        await objActions.RunBranchCheckInAsync(objProjectId, "run/REQ-FN-096", new[] { "b.txt" }, "[REQ-FN-101] [REQ-FN-096] second");

        var vBranches = await objActions.ProcessBranchesAsync(objProjectId);

        var vBranch = Assert.Single(vBranches);
        Assert.Equal("run/REQ-FN-096", vBranch.Name);
        Assert.Equal(2, vBranch.CheckInCount);
        Assert.Equal(new[] { "REQ-FN-096", "REQ-FN-101" }, vBranch.RequirementIds);
        Assert.True(vBranch.LastCheckInUtc > DateTime.UtcNow.AddMinutes(-5));
    }

    /// <summary>When the repository has only the owner's own branches, or a run branch with nothing the owner's branches lack, then no process branch is listed.</summary>
    [Fact]
    public async Task ProcessBranchesIgnoresBranchesThatHoldNothingNew()
    {
        RunGit(objRepositoryPath, "branch", "feature/board");
        RunGit(objRepositoryPath, "branch", "run/REQ-FN-000");

        Assert.Empty(await objActions.ProcessBranchesAsync(objProjectId));
    }

    private string CurrentBranch() => RunGit(objRepositoryPath, "rev-parse", "--abbrev-ref", "HEAD").Trim();

    private static string RunGit(string aWorkingDirectory, params string[] aArguments)
    {
        var vStartInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = aWorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var vArgument in aArguments)
        {
            vStartInfo.ArgumentList.Add(vArgument);
        }

        using var vProcess = Process.Start(vStartInfo)!;
        var vOutput = vProcess.StandardOutput.ReadToEnd();
        vProcess.WaitForExit();
        return vOutput;
    }

    /// <summary>Deletes the temporary database and repository this test created.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objRepositoryPath))
        {
            var vDirectory = new DirectoryInfo(objRepositoryPath);
            foreach (var vFile in vDirectory.GetFiles("*", SearchOption.AllDirectories))
            {
                vFile.Attributes = FileAttributes.Normal;
            }

            Directory.Delete(objRepositoryPath, recursive: true);
        }
    }

    /// <summary>Opens connections to a temporary SQLite file, standing in for <see cref="SqliteConnectionFactory"/> in this test.</summary>
    private sealed class TestDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string objConnectionString;

        public TestDbConnectionFactory(string aDatabasePath)
        {
            objConnectionString = $"Data Source={aDatabasePath}";
        }

        public IDbConnection OpenConnection()
        {
            var vConnection = new SqliteConnection(objConnectionString);
            vConnection.Open();
            return vConnection;
        }
    }
}
