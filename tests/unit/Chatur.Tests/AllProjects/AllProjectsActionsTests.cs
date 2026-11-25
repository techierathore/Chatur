using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Chatur.Core.Projects;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.AllProjects;

/// <summary>
/// Tests for All projects (REQ-FN-076, REQ-UI-067, REQ-FN-077) against real folders under the test bin folder,
/// removed when the test ends.
/// </summary>
public sealed class AllProjectsActionsTests : MigratedDatabaseFixture, IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly string objRoot = Path.Combine(AppContext.BaseDirectory, $"allprojects-{Guid.NewGuid():N}");
    private readonly List<ProjectSummary> objProjects = [];
    private readonly FakeProjects objFake;
    private readonly AllProjectsActions objSut;

    /// <summary>Two projects: one with two checklists (2 verified, 3 not), one with a single open requirement.</summary>
    public AllProjectsActionsTests()
    {
        var vA = Project("alpha", 1, Now.AddHours(-2));
        Checklist(vA, "A-Checklist.md", ("REQ-NFR-001", "Verified"), ("REQ-NFR-002", "Verified"));
        Checklist(vA, "A-P2-Checklist.md", ("REQ-UI-001", "Implemented"), ("REQ-FN-001", "Not Started"), ("REQ-RAG-001", "Blocked"));
        var vB = Project("beta", 2, Now.AddDays(-30));
        Checklist(vB, "B-Checklist.md", ("REQ-FN-001", "Not Started"));
        Project("gamma", 3, null);
        objFake = new FakeProjects(objProjects);
        objSut = new AllProjectsActions(objFake, CreateFactory(), new FixedClock(Now));
    }

    /// <summary>Removes the project folders.</summary>
    void IDisposable.Dispose()
    {
        Directory.Delete(objRoot, true);
        Dispose();
    }

    /// <summary>When All projects opens, then every project is listed with its phase and when it was last worked (REQ-FN-076).</summary>
    [Fact]
    public async Task EveryProjectIsListedWithItsPhaseAndLastWorked()
    {
        var vView = await objSut.LoadAsync(null, AllProjectsOrder.LastWorked);

        Assert.Equal(["alpha", "beta", "gamma"], vView.Projects.Select(aProject => aProject.Name));
        Assert.Equal(3, vView.TotalProjects);
        Assert.Equal(2, vView.Projects[0].Phase);
        Assert.Equal(1, vView.Projects[1].Phase);
        Assert.Null(vView.Projects[2].Phase);
        Assert.Equal(Now.AddHours(-2), vView.Projects[0].LastWorkedUtc);
        Assert.Equal(["active", "paused", "not opened"], vView.Projects.Select(aProject => aProject.State));
    }

    /// <summary>When a project has requirements, then its row counts the open and the verified ones (REQ-UI-067).</summary>
    [Fact]
    public async Task EachRowCountsOpenAndVerified()
    {
        var vView = await objSut.LoadAsync(null, AllProjectsOrder.Name);

        Assert.Equal((5, 3, 2), (vView.Projects[0].Requirements, vView.Projects[0].Open, vView.Projects[0].Verified));
        Assert.Equal((1, 1, 0), (vView.Projects[1].Requirements, vView.Projects[1].Open, vView.Projects[1].Verified));
        Assert.Equal(4, vView.OpenRequirements);
        Assert.Equal(2, vView.VerifiedRequirements);
    }

    /// <summary>The search narrows by name or folder, and the order can be by open requirements.</summary>
    [Fact]
    public async Task SearchAndOrderNarrowTheList()
    {
        var vSearched = await objSut.LoadAsync("BET", AllProjectsOrder.Name);
        var vByOpen = await objSut.LoadAsync(null, AllProjectsOrder.OpenRequirements);

        Assert.Equal(["beta"], vSearched.Projects.Select(aProject => aProject.Name));
        Assert.Equal(3, vSearched.TotalProjects);
        Assert.Equal("alpha", vByOpen.Projects[0].Name);
    }

    /// <summary>When the owner opens a project from the view, then it becomes the selected project (REQ-FN-077).</summary>
    [Fact]
    public async Task OpeningAProjectSelectsIt()
    {
        await objSut.OpenAsync(2);

        Assert.Equal(2, objFake.Selected);
    }

    private ProjectSummary Project(string aName, int aId, DateTime? aLastOpened)
    {
        var vPath = Path.Combine(objRoot, aName);
        Directory.CreateDirectory(Path.Combine(vPath, "docs"));
        var vProject = new ProjectSummary(aId, aName, vPath, "Folder", aLastOpened);
        objProjects.Add(vProject);
        return vProject;
    }

    private static void Checklist(ProjectSummary aProject, string aFile, params (string Id, string Status)[] aRows)
    {
        var vRows = string.Join("\n", aRows.Select(aRow => $"| {aRow.Id} | Thing | {aRow.Status} | 0% | — | [view](#d-{aRow.Id.ToLowerInvariant()}) |"));
        File.WriteAllText(
            Path.Combine(aProject.Path, "docs", aFile),
            $"# Checklist\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n{vRows}\n");
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime aUtcNow) => UtcNow = aUtcNow;

        public DateTime UtcNow { get; }
    }

    private sealed class FakeProjects : IProjectActions
    {
        private readonly List<ProjectSummary> objList;

        public FakeProjects(List<ProjectSummary> aList) => objList = aList;

        public int Selected { get; private set; }

        public Task<IReadOnlyList<ProjectSummary>> ScanAsync(CancellationToken aCt = default) => Task.FromResult<IReadOnlyList<ProjectSummary>>(objList);

        public Task SelectProjectAsync(int aProjectId, CancellationToken aCt = default)
        {
            Selected = aProjectId;
            return Task.CompletedTask;
        }

        public Task<ProjectSummary?> SelectedProjectAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProjectFolder>> ListFoldersAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectFolder> AddFolderAsync(string aPath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task RemoveFolderAsync(int aFolderId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task ForgetProjectAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();
    }
}
