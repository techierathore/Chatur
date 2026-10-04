using System.Diagnostics;
using Chatur.Core.SourceControl;
using Chatur.Tests.Support;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="SourceControlActions"/>'s changes, diff, check-in, push and pull (REQ-UI-042,
/// REQ-UI-043, REQ-UI-044), against a real, throwaway git repository created in a temporary folder —
/// never the Chatur repository itself.
/// </summary>
public sealed class SourceControlChangesTests : IDisposable
{
    private readonly string objRepositoryPath = Path.Combine(Path.GetTempPath(), $"chatur-tests-changes-{Guid.NewGuid():N}");
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-changes-{Guid.NewGuid():N}.db");
    private readonly SourceControlActions objActions;
    private readonly int objProjectId;

    /// <summary>Creates a throwaway git repository with one commit on <c>main</c>, and a database holding one project row that points at it.</summary>
    public SourceControlChangesTests()
    {
        Directory.CreateDirectory(objRepositoryPath);
        RunGit(objRepositoryPath, "init", "-b", "main");
        RunGit(objRepositoryPath, "config", "user.email", "chatur-tests@techierathore.com");
        RunGit(objRepositoryPath, "config", "user.name", "Chatur Tests");
        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\n");
        RunGit(objRepositoryPath, "add", "-A");
        RunGit(objRepositoryPath, "commit", "-m", "first check-in");

        var vConnectionFactory = new TestDbConnectionFactory($"Data Source={objDatabasePath}");
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

    /// <summary>When nothing has changed since the last check-in, then the list is empty (REQ-UI-042).</summary>
    [Fact]
    public async Task ChangesIsEmptyWhenNothingHasChanged()
    {
        var vChanges = await objActions.ChangesAsync(objProjectId);

        Assert.Empty(vChanges);
    }

    /// <summary>When a tracked file has a line added, then it is listed as modified with the count (REQ-UI-042).</summary>
    [Fact]
    public async Task ChangesListsAModifiedFileWithItsLineCounts()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\nline three\n");

        var vChanges = await objActions.ChangesAsync(objProjectId);

        var vEntry = Assert.Single(vChanges);
        Assert.Equal("kept.txt", vEntry.Path);
        Assert.Equal("modified", vEntry.State);
        Assert.Equal(1, vEntry.LinesAdded);
        Assert.Equal(0, vEntry.LinesRemoved);
    }

    /// <summary>When a new file has never been checked in, then it is listed as added with every line counted (REQ-UI-042).</summary>
    [Fact]
    public async Task ChangesListsANewFileAsAdded()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "new.txt"), "alpha\nbeta\n");

        var vChanges = await objActions.ChangesAsync(objProjectId);

        var vEntry = Assert.Single(vChanges);
        Assert.Equal("new.txt", vEntry.Path);
        Assert.Equal("added", vEntry.State);
        Assert.Equal(2, vEntry.LinesAdded);
    }

    /// <summary>When a tracked file is removed from disk, then it is listed as deleted (REQ-UI-042).</summary>
    [Fact]
    public async Task ChangesListsARemovedFileAsDeleted()
    {
        File.Delete(Path.Combine(objRepositoryPath, "kept.txt"));

        var vChanges = await objActions.ChangesAsync(objProjectId);

        var vEntry = Assert.Single(vChanges);
        Assert.Equal("kept.txt", vEntry.Path);
        Assert.Equal("deleted", vEntry.State);
    }

    /// <summary>When a tracked file changed, then its diff shows the added line (REQ-UI-042).</summary>
    [Fact]
    public async Task DiffShowsTheAddedLineOfAModifiedFile()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\nline three\n");

        var vDiff = await objActions.DiffAsync(objProjectId, "kept.txt");

        Assert.Contains("+line three", vDiff);
    }

    /// <summary>When a file has never been checked in, then its diff shows every line as added, with no last check-in to compare against (REQ-UI-042).</summary>
    [Fact]
    public async Task DiffShowsTheWholeFileForANewFile()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "new.txt"), "alpha\nbeta\n");

        var vDiff = await objActions.DiffAsync(objProjectId, "new.txt");

        Assert.Contains("+alpha", vDiff);
        Assert.Contains("+beta", vDiff);
    }

    /// <summary>When the owner checks in the chosen files, then they leave the changes list and the rest stay (REQ-UI-043).</summary>
    [Fact]
    public async Task CheckInCommitsOnlyTheChosenFiles()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\nline three\n");
        File.WriteAllText(Path.Combine(objRepositoryPath, "other.txt"), "left uncommitted\n");

        await objActions.CheckInAsync(objProjectId, new[] { "kept.txt" }, "[REQ-UI-043] check-in message");

        var vChanges = await objActions.ChangesAsync(objProjectId);
        var vRemaining = Assert.Single(vChanges);
        Assert.Equal("other.txt", vRemaining.Path);
        Assert.Contains("[REQ-UI-043] check-in message", RunGit(objRepositoryPath, "log", "-1", "--pretty=%s"));
    }

    /// <summary>When no file is chosen, then the check-in is refused before git is ever asked to do anything.</summary>
    [Fact]
    public async Task CheckInRefusesAnEmptyFileList()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => objActions.CheckInAsync(objProjectId, Array.Empty<string>(), "[REQ-UI-043] nothing chosen"));
    }

    /// <summary>When the message is blank, then the check-in is refused (REQ-UI-043).</summary>
    [Fact]
    public async Task CheckInRefusesABlankMessage()
    {
        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\nline three\n");

        await Assert.ThrowsAsync<ArgumentException>(
            () => objActions.CheckInAsync(objProjectId, new[] { "kept.txt" }, "   "));
    }

    /// <summary>When the branch has no upstream yet, then the status names the branch and has nothing to be ahead or behind of (REQ-UI-044).</summary>
    [Fact]
    public async Task StatusReportsTheBranchWithNoUpstream()
    {
        var vStatus = await objActions.StatusAsync(objProjectId);

        Assert.Equal("main", vStatus.Branch);
        Assert.Equal(0, vStatus.Ahead);
        Assert.Equal(0, vStatus.Behind);
    }

    /// <summary>
    /// When the owner pushes a new check-in and then pulls a change made on the same remote from
    /// elsewhere, then Chatur reports the correct ahead/behind count after each (REQ-UI-044).
    /// </summary>
    [Fact]
    public async Task PushAndPullUpdateHowFarAheadOrBehindTheBranchIs()
    {
        var vRemotePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-remote-{Guid.NewGuid():N}.git");
        RunGit(Path.GetTempPath(), "init", "--bare", "-b", "main", vRemotePath);
        RunGit(objRepositoryPath, "remote", "add", "origin", vRemotePath);
        RunGit(objRepositoryPath, "push", "--set-upstream", "origin", "main");

        File.WriteAllText(Path.Combine(objRepositoryPath, "kept.txt"), "line one\nline two\nline three\n");
        await objActions.CheckInAsync(objProjectId, new[] { "kept.txt" }, "[REQ-UI-044] a local check-in");

        var vAfterCheckIn = await objActions.StatusAsync(objProjectId);
        Assert.Equal(1, vAfterCheckIn.Ahead);
        Assert.Equal(0, vAfterCheckIn.Behind);

        var vAfterPush = await objActions.PushAsync(objProjectId);
        Assert.Equal(0, vAfterPush.Ahead);
        Assert.Equal(0, vAfterPush.Behind);

        // Someone else pushes to the same remote from a second clone.
        var vOtherClonePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-clone-{Guid.NewGuid():N}");
        RunGit(Path.GetTempPath(), "clone", vRemotePath, vOtherClonePath);
        RunGit(vOtherClonePath, "config", "user.email", "chatur-tests@techierathore.com");
        RunGit(vOtherClonePath, "config", "user.name", "Chatur Tests");
        File.WriteAllText(Path.Combine(vOtherClonePath, "from-elsewhere.txt"), "written on another machine\n");
        RunGit(vOtherClonePath, "add", "-A");
        RunGit(vOtherClonePath, "commit", "-m", "a check-in from elsewhere");
        RunGit(vOtherClonePath, "push");

        var vAfterOtherPush = await objActions.StatusAsync(objProjectId);
        Assert.Equal(0, vAfterOtherPush.Ahead);
        Assert.Equal(1, vAfterOtherPush.Behind);

        var vAfterPull = await objActions.PullAsync(objProjectId);
        Assert.Equal(0, vAfterPull.Ahead);
        Assert.Equal(0, vAfterPull.Behind);
        Assert.True(File.Exists(Path.Combine(objRepositoryPath, "from-elsewhere.txt")));

        CleanUpDirectory(vRemotePath);
        CleanUpDirectory(vOtherClonePath);
    }

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

    private static void CleanUpDirectory(string aPath)
    {
        if (!Directory.Exists(aPath))
        {
            return;
        }

        foreach (var vFile in new DirectoryInfo(aPath).GetFiles("*", SearchOption.AllDirectories))
        {
            vFile.Attributes = FileAttributes.Normal;
        }

        Directory.Delete(aPath, recursive: true);
    }

    /// <summary>Deletes the temporary database and repository this test created.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        CleanUpDirectory(objRepositoryPath);
    }
}
