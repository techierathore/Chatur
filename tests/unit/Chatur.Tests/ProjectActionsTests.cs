using System.Data;
using Chatur.Core;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using ChaturDb;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="ProjectActions"/> against a real, temporary SQLite file and a real,
/// temporary folder on disk — the folder naming, removal and scan cluster E owns (REQ-UI-005,
/// REQ-UI-006, REQ-UI-008, REQ-UI-009), plus the selection cluster E and F share (REQ-UI-007).
/// </summary>
public sealed class ProjectActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-tests-{Guid.NewGuid():N}.db");
    private readonly string objSearchRoot = Path.Combine(Path.GetTempPath(), $"chatur-tests-root-{Guid.NewGuid():N}");
    private readonly AppState objAppState = new();
    private readonly ProjectActions objSut;

    /// <summary>Migrates a fresh temporary database and creates a fresh temporary search folder for every test.</summary>
    public ProjectActionsTests()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(vConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);

        Directory.CreateDirectory(objSearchRoot);

        objSut = new ProjectActions(
            new FixedPathConnectionFactory(objDatabasePath),
            new FixedClock(new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Utc)),
            objAppState);
    }

    /// <summary>
    /// When the owner adds a folder in the Project folders dialog, then it appears in the folder
    /// list on Projects (REQ-UI-005).
    /// </summary>
    [Fact]
    public async Task AddedFolderAppearsInTheList()
    {
        var vAdded = await objSut.AddFolderAsync(objSearchRoot);

        var vFolders = await objSut.ListFoldersAsync();

        Assert.Contains(vFolders, aFolder => aFolder.FolderId == vAdded.FolderId && aFolder.Path == objSearchRoot);
    }

    /// <summary>Naming the same path twice never creates a second row — the folder list stays exactly what the owner named.</summary>
    [Fact]
    public async Task AddingTheSameFolderTwiceReturnsTheSameRow()
    {
        var vFirst = await objSut.AddFolderAsync(objSearchRoot);
        var vSecond = await objSut.AddFolderAsync(objSearchRoot);

        Assert.Equal(vFirst.FolderId, vSecond.FolderId);
        Assert.Single(await objSut.ListFoldersAsync());
    }

    /// <summary>
    /// When a named folder holds projects on Start, then Projects lists each one with its name, path
    /// and kind (REQ-UI-006): a subfolder with a <c>.sln</c> is a Solution named after that file, a
    /// bare subfolder is a Folder.
    /// </summary>
    [Fact]
    public async Task ScanFindsASolutionAndABareFolder()
    {
        var vSolutionFolder = Path.Combine(objSearchRoot, "TfLens");
        Directory.CreateDirectory(vSolutionFolder);
        File.WriteAllText(Path.Combine(vSolutionFolder, "TfLens.sln"), string.Empty);

        var vBareFolder = Path.Combine(objSearchRoot, "scratch");
        Directory.CreateDirectory(vBareFolder);

        await objSut.AddFolderAsync(objSearchRoot);

        var vProjects = await objSut.ScanAsync();

        Assert.Contains(vProjects, aProject => aProject.Name == "TfLens" && aProject.Kind == "Solution" && aProject.Path.EndsWith("TfLens.sln", StringComparison.Ordinal));
        Assert.Contains(vProjects, aProject => aProject.Name == "scratch" && aProject.Kind == "Folder" && aProject.Path == vBareFolder);
    }

    /// <summary>
    /// When no folder has been named on Start, then the scan finds nothing to list, which is what
    /// drives the empty state (REQ-UI-008).
    /// </summary>
    [Fact]
    public async Task ScanWithNoFoldersNamedFindsNothing()
    {
        var vProjects = await objSut.ScanAsync();

        Assert.Empty(vProjects);
    }

    /// <summary>
    /// When the owner removes a folder in the dialog, then its projects leave the list, and scanning
    /// again after re-adding the same folder proves the old rows are truly gone rather than merely
    /// hidden (REQ-UI-009).
    /// </summary>
    [Fact]
    public async Task RemovingAFolderTakesItsProjectsWithIt()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "AstroLyfe"));

        var vFolder = await objSut.AddFolderAsync(objSearchRoot);
        await objSut.ScanAsync();

        await objSut.RemoveFolderAsync(vFolder.FolderId);

        Assert.Empty(await objSut.ListFoldersAsync());

        await objSut.AddFolderAsync(objSearchRoot);
        var vProjectsAfterReAdding = await objSut.ScanAsync();

        Assert.Contains(vProjectsAfterReAdding, aProject => aProject.Name == "AstroLyfe");
    }

    /// <summary>
    /// When the owner removes a folder holding a project that has been worked on (it has a session,
    /// so the database cannot drop it), then the removal still succeeds, the project leaves the list
    /// and stops being the selected one, its session survives, and naming the folder again brings the
    /// same project back (REQ-UI-009, fix 2026-09-30 for "FOREIGN KEY constraint failed").
    /// </summary>
    [Fact(DisplayName = "REQ-UI-009 removing a folder whose project has history hides it and keeps the history")]
    public async Task RemovingAFolderKeepsTheHistoryOfAWorkedOnProject()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "Worked"));
        var vFolder = await objSut.AddFolderAsync(objSearchRoot);
        var vProject = Assert.Single(await objSut.ScanAsync());
        await objSut.SelectProjectAsync(vProject.ProjectId);

        using (var vConnection = new SqliteConnection($"Data Source={objDatabasePath}"))
        {
            vConnection.Open();
            vConnection.Execute(
                "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@ProjectId, (SELECT MIN(RoleId) FROM Role), 'AskFirst', 'Active', '2026-09-30T10:00:00Z');",
                new { vProject.ProjectId });
        }

        await objSut.RemoveFolderAsync(vFolder.FolderId);

        Assert.Empty(await objSut.ScanAsync());
        Assert.Null(await objSut.SelectedProjectAsync());
        using (var vConnection = new SqliteConnection($"Data Source={objDatabasePath}"))
        {
            Assert.Equal(1L, vConnection.ExecuteScalar<long>("SELECT COUNT(1) FROM Session WHERE ProjectId = @ProjectId;", new { vProject.ProjectId }));
        }

        await objSut.AddFolderAsync(objSearchRoot);
        var vBack = Assert.Single(await objSut.ScanAsync());
        Assert.Equal(vProject.ProjectId, vBack.ProjectId);
    }

    /// <summary>
    /// When the owner forgets a project on Start, then it leaves the list and stays out when its
    /// folder is scanned again, while the folder on disk and the other projects are untouched
    /// (UI Design "Screen: Start", <c>forget</c>).
    /// </summary>
    [Fact]
    public async Task ForgottenProjectStaysOffTheListAfterAScan()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "Keep"));
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "Drop"));
        await objSut.AddFolderAsync(objSearchRoot);
        var vProjects = await objSut.ScanAsync();
        var vDrop = Assert.Single(vProjects, aProject => aProject.Name == "Drop");

        await objSut.ForgetProjectAsync(vDrop.ProjectId);

        var vAfter = await objSut.ScanAsync();
        Assert.DoesNotContain(vAfter, aProject => aProject.Name == "Drop");
        Assert.Contains(vAfter, aProject => aProject.Name == "Keep");
        Assert.True(Directory.Exists(Path.Combine(objSearchRoot, "Drop")));
    }

    /// <summary>
    /// When the owner forgets the project that is selected, then nothing is selected any more — in
    /// the database and in <see cref="AppState"/> — so Start does not reopen a project that is no
    /// longer in the list.
    /// </summary>
    [Fact]
    public async Task ForgettingTheSelectedProjectClearsTheSelection()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "Open"));
        await objSut.AddFolderAsync(objSearchRoot);
        var vProject = Assert.Single(await objSut.ScanAsync());
        await objSut.SelectProjectAsync(vProject.ProjectId);

        await objSut.ForgetProjectAsync(vProject.ProjectId);

        Assert.Null(await objSut.SelectedProjectAsync());
        Assert.Null(objAppState.SelectedProject);
    }

    /// <summary>Forgetting a project that was never listed changes nothing and does not throw.</summary>
    [Fact]
    public async Task ForgettingAnUnknownProjectChangesNothing()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "Stay"));
        await objSut.AddFolderAsync(objSearchRoot);
        Assert.Single(await objSut.ScanAsync());

        await objSut.ForgetProjectAsync(9999);

        Assert.Single(await objSut.ScanAsync());
    }

    /// <summary>
    /// When the owner opens a project from the list, then it becomes the selected project every
    /// window reads from <see cref="AppState"/> (REQ-UI-007).
    /// </summary>
    [Fact]
    public async Task SelectingAScannedProjectUpdatesAppState()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "MyDiary"));
        await objSut.AddFolderAsync(objSearchRoot);
        var vProject = Assert.Single(await objSut.ScanAsync());

        await objSut.SelectProjectAsync(vProject.ProjectId);

        Assert.NotNull(objAppState.SelectedProject);
        Assert.Equal(vProject.ProjectId, objAppState.SelectedProject!.ProjectId);
    }

    /// <summary>
    /// When Chatur is closed and opened again, then the same project is still selected (REQ-FN-006):
    /// a fresh <see cref="ProjectActions"/> instance over the same database file, with its own empty
    /// <see cref="AppState"/> — exactly what a new process starts with — reads back the choice a
    /// previous instance made, rather than relying on anything kept in memory.
    /// </summary>
    [Fact]
    public async Task SelectedProjectSurvivesARestart()
    {
        Directory.CreateDirectory(Path.Combine(objSearchRoot, "AppManager"));
        await objSut.AddFolderAsync(objSearchRoot);
        var vProject = Assert.Single(await objSut.ScanAsync());
        await objSut.SelectProjectAsync(vProject.ProjectId);

        var vRestartedAppState = new AppState();
        var vRestarted = new ProjectActions(
            new FixedPathConnectionFactory(objDatabasePath),
            new FixedClock(new DateTime(2026, 9, 22, 10, 5, 0, DateTimeKind.Utc)),
            vRestartedAppState);

        var vReopened = await vRestarted.SelectedProjectAsync();

        Assert.NotNull(vReopened);
        Assert.Equal(vProject.ProjectId, vReopened!.ProjectId);
        Assert.Equal(vProject.ProjectId, vRestartedAppState.SelectedProject?.ProjectId);
    }

    /// <summary>Before anything has ever been selected, there is nothing to reopen (REQ-FN-006).</summary>
    [Fact]
    public async Task SelectedProjectIsNullBeforeAnythingIsSelected()
    {
        var vSelected = await objSut.SelectedProjectAsync();

        Assert.Null(vSelected);
    }

    /// <summary>Selecting a project that does not exist is refused, never silently recorded (REQ-FN-006).</summary>
    [Fact]
    public async Task SelectingAnUnknownProjectThrows()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => objSut.SelectProjectAsync(999999));
    }

    /// <summary>Deletes the temporary database and search folder this test created.</summary>
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objSearchRoot))
        {
            Directory.Delete(objSearchRoot, recursive: true);
        }
    }

    /// <summary>Opens connections to one fixed, temporary database file (Coding Standards §Testability).</summary>
    private sealed class FixedPathConnectionFactory : IDbConnectionFactory
    {
        private readonly string objConnectionString;

        public FixedPathConnectionFactory(string aDatabasePath)
        {
            objConnectionString = $"Data Source={aDatabasePath}";
        }

        public IDbConnection OpenConnection()
        {
            var vConnection = new SqliteConnection(objConnectionString);
            vConnection.Open();
            using var vPragma = vConnection.CreateCommand();
            vPragma.CommandText = "PRAGMA foreign_keys = ON;";
            vPragma.ExecuteNonQuery();
            return vConnection;
        }
    }

    /// <summary>A clock fixed to one instant, so <c>LastOpenedUtc</c>/<c>CreatedUtc</c> are deterministic in a test.</summary>
    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime aUtcNow)
        {
            UtcNow = aUtcNow;
        }

        public DateTime UtcNow { get; }
    }
}
