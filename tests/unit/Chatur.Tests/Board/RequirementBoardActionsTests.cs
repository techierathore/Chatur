using Chatur.Core.Actions;
using Chatur.Core.Board;
using Xunit;

namespace Chatur.Tests.Board;

/// <summary>
/// Tests for the Requirements board: filtering (REQ-FN-056) and writing a status into the project's
/// checklist file (REQ-FN-057), against real temporary folders.
/// </summary>
public sealed class RequirementBoardActionsTests : IDisposable
{
    private const string PhaseOneText =
        "# Chatur — Checklist (phase 1)\r\n\r\n## Requirements Status\r\n\r\n| ID | Requirement | Status | % | Remarks | Details |\r\n|----|---|---|---|---|---|\r\n"
        + "| REQ-UI-001 | Sign in | Verified | 100% | done | [view](#d-req-ui-001) |\r\n"
        + "| REQ-NFR-001 | Secrets | Blocked | 25% | waits | [view](#d-req-nfr-001) |\r\n\r\n"
        + "## Page: Sign in (`/sign-in`)\r\n\r\n<a id=\"d-req-ui-001\"></a>\r\n"
        + "- **REQ-UI-001** — Sign in. *BRD:* BRD-1 · *Mockup:* mockups/sign-in.html — **Verified**\r\n"
        + "  - *Acceptance:* When the owner signs in, then Projects opens.\r\n";

    private const string PhaseTwoText =
        "# Chatur — Checklist (phase 2)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n"
        + "| REQ-FN-056 | Narrow it down | Not Started | 0% | — | [view](#d-req-fn-056) |\n"
        + "| REQ-FN-057 | A change is a change in the file | Implemented | 75% | a \\| b | [view](#d-req-fn-057) |\n"
        + "| REQ-UI-049 | Everything | In Progress | 50% | — | [view](#d-req-ui-049) |\n\n"
        + "## Page: Requirements board (`/board`)\n\n"
        + "- **REQ-FN-056** — Narrow it down. *BRD:* BRD-109 · *Mockup:* mockups/board.html\n"
        + "  - *Acceptance:* When the owner filters, then only the matching requirements are listed.\n";

    private readonly string objFolder = Path.Combine(Path.GetTempPath(), $"chatur-board-{Guid.NewGuid():N}");
    private readonly RequirementBoardActions objSut;

    /// <summary>Creates a project folder holding a phase 1 and a phase 2 checklist.</summary>
    public RequirementBoardActionsTests()
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "docs"));
        File.WriteAllText(Path.Combine(objFolder, "docs", "App-Checklist.md"), PhaseOneText);
        File.WriteAllText(Path.Combine(objFolder, "docs", "App-P2-Checklist.md"), PhaseTwoText);
        objSut = new RequirementBoardActions(new FixedProjectActions(new ProjectSummary(1, "App", objFolder, "Folder", null)));
    }

    /// <summary>Removes the temporary project folder.</summary>
    public void Dispose() => Directory.Delete(objFolder, true);

    /// <summary>When the owner filters by phase 2, then only the phase 2 requirements are listed (REQ-FN-056).</summary>
    [Fact]
    public async Task PhaseFilterKeepsThatPhaseOnly()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(Phase: 2));

        Assert.Equal(["REQ-FN-056", "REQ-FN-057", "REQ-UI-049"], vBoard.Items.Select(aItem => aItem.Id));
        Assert.Equal(5, vBoard.Counts.All);
    }

    /// <summary>When the owner filters by status Blocked, then only blocked requirements are listed (REQ-FN-056).</summary>
    [Fact]
    public async Task StatusFilterKeepsThatStatusOnly()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(Status: BoardStatuses.Blocked));

        Assert.Equal(["REQ-NFR-001"], vBoard.Items.Select(aItem => aItem.Id));
    }

    /// <summary>Implemented and In Progress both fall under the board's In progress (REQ-FN-056).</summary>
    [Fact]
    public async Task InProgressCoversImplementedToo()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(Status: BoardStatuses.InProgress));

        Assert.Equal(["REQ-FN-057", "REQ-UI-049"], vBoard.Items.Select(aItem => aItem.Id));
    }

    /// <summary>When the owner filters by agent Verifier, then only the requirements that agent owns are listed (REQ-FN-056).</summary>
    [Fact]
    public async Task AgentFilterKeepsThatAgentOnly()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(AgentCode: "verifier"));

        Assert.Equal(["REQ-NFR-001"], vBoard.Items.Select(aItem => aItem.Id));
    }

    /// <summary>The three filters and a search narrow together (REQ-FN-056).</summary>
    [Fact]
    public async Task FiltersCombine()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(2, BoardStatuses.InProgress, "flow-master", "everything"));

        Assert.Equal(["REQ-UI-049"], vBoard.Items.Select(aItem => aItem.Id));
    }

    /// <summary>A one-letter search is too short to narrow anything.</summary>
    [Fact]
    public async Task ShortSearchIsIgnored()
    {
        var vBoard = await objSut.LoadAsync(new RequirementFilter(Search: "e"));

        Assert.Equal(5, vBoard.Items.Count);
    }

    /// <summary>The detail section's acceptance line, screen and mockup come with the row (REQ-UI-051).</summary>
    [Fact]
    public async Task RowCarriesItsDetail()
    {
        var vBoard = await objSut.LoadAsync(RequirementFilter.None);
        var vItem = vBoard.Items.Single(aItem => aItem.Id == "REQ-FN-056");

        Assert.Equal("When the owner filters, then only the matching requirements are listed.", vItem.Acceptance);
        Assert.Equal("/board", vItem.ScreenRoute);
        Assert.Equal("mockups/board.html", vItem.Mockup);
    }

    /// <summary>When the owner changes a status on the board, then the checklist file holds that status (REQ-FN-057).</summary>
    [Fact]
    public async Task ChangedStatusIsInTheFile()
    {
        await objSut.SetStatusAsync("docs/App-P2-Checklist.md", "REQ-FN-056", BoardStatuses.Verified);

        var vText = await File.ReadAllTextAsync(Path.Combine(objFolder, "docs", "App-P2-Checklist.md"));
        Assert.Contains("| REQ-FN-056 | Narrow it down | Verified | 100% | — | [view](#d-req-fn-056) |", vText);
        var vBoard = await objSut.LoadAsync(RequirementFilter.None);
        Assert.Equal(BoardStatuses.Verified, vBoard.Items.Single(aItem => aItem.Id == "REQ-FN-056").BoardStatus);
    }

    /// <summary>A change touches only its own row: every other line, and the line endings, stay as they were (REQ-FN-057).</summary>
    [Fact]
    public async Task OtherLinesAreUntouched()
    {
        await objSut.SetStatusAsync("docs/App-Checklist.md", "REQ-NFR-001", BoardStatuses.Planned);

        var vText = await File.ReadAllTextAsync(Path.Combine(objFolder, "docs", "App-Checklist.md"));
        Assert.Equal(PhaseOneText.Replace("| REQ-NFR-001 | Secrets | Blocked | 25% |", "| REQ-NFR-001 | Secrets | Not Started | 0% |"), vText);
    }

    /// <summary>The detail bullet's trailing status word follows the table (REQ-FN-057).</summary>
    [Fact]
    public async Task DetailBulletFollowsTheTable()
    {
        await objSut.SetStatusAsync("docs/App-Checklist.md", "REQ-UI-001", BoardStatuses.InProgress);

        var vText = await File.ReadAllTextAsync(Path.Combine(objFolder, "docs", "App-Checklist.md"));
        Assert.Contains("mockups/sign-in.html — **In Progress**\r\n", vText);
        Assert.Contains("| REQ-UI-001 | Sign in | In Progress | 50% | done |", vText);
    }

    /// <summary>A remark holding an escaped pipe is read and written back whole (REQ-FN-057).</summary>
    [Fact]
    public async Task EscapedPipeSurvivesAWrite()
    {
        await objSut.SetStatusAsync("docs/App-P2-Checklist.md", "REQ-FN-057", BoardStatuses.Blocked);

        var vText = await File.ReadAllTextAsync(Path.Combine(objFolder, "docs", "App-P2-Checklist.md"));
        Assert.Contains("| REQ-FN-057 | A change is a change in the file | Blocked | 75% | a \\| b |", vText);
    }

    /// <summary>A status the board does not offer is refused and the file is left alone (REQ-FN-057).</summary>
    [Fact]
    public async Task UnknownStatusIsRefused()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SetStatusAsync("docs/App-P2-Checklist.md", "REQ-FN-056", "Done"));

        Assert.Equal(PhaseTwoText, await File.ReadAllTextAsync(Path.Combine(objFolder, "docs", "App-P2-Checklist.md")));
    }

    /// <summary>A file outside the project's checklists cannot be written through the board.</summary>
    [Fact]
    public async Task FileOutsideTheChecklistsIsRefused()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => objSut.SetStatusAsync("../outside.md", "REQ-FN-056", BoardStatuses.Verified));
    }

    /// <summary>When the file is edited outside Chatur, then loading again shows the new status (REQ-UI-052).</summary>
    [Fact]
    public async Task OutsideEditShowsOnNextLoad()
    {
        await objSut.LoadAsync(RequirementFilter.None);
        File.WriteAllText(Path.Combine(objFolder, "docs", "App-P2-Checklist.md"), PhaseTwoText.Replace("| Not Started |", "| Verified |"));

        var vBoard = await objSut.LoadAsync(RequirementFilter.None);

        Assert.Equal(BoardStatuses.Verified, vBoard.Items.Single(aItem => aItem.Id == "REQ-FN-056").BoardStatus);
    }

    /// <summary>The repository's own phase 2 checklist is read whole: every table row has an id, a title and a status (REQ-UI-049).</summary>
    [Fact]
    public async Task RealChecklistIsReadWhole()
    {
        var vRoot = AppContext.BaseDirectory;
        while (vRoot is not null && !File.Exists(Path.Combine(vRoot, "docs", "Chatur-P2-Checklist.md")))
        {
            vRoot = Path.GetDirectoryName(vRoot);
        }

        Assert.NotNull(vRoot);
        var vBoard = await new RequirementBoardActions(new FixedProjectActions(new ProjectSummary(1, "Chatur", vRoot!, "Folder", null)))
            .LoadAsync(new RequirementFilter(Phase: 2));

        Assert.True(vBoard.Items.Count >= 22);
        Assert.All(vBoard.Items, aItem => Assert.False(string.IsNullOrWhiteSpace(aItem.Title) || string.IsNullOrWhiteSpace(aItem.FileStatus)));
        Assert.Contains(vBoard.Items, aItem => aItem.Id == "REQ-FN-056" && aItem.ScreenRoute == "/board");
    }

    /// <summary>With no project selected the board is empty rather than failing.</summary>
    [Fact]
    public async Task NoProjectGivesAnEmptyBoard()
    {
        var vBoard = await new RequirementBoardActions(new FixedProjectActions(null)).LoadAsync(RequirementFilter.None);

        Assert.False(vBoard.HasProject);
        Assert.Empty(vBoard.Items);
    }

    /// <summary>REQ-UI-049: a selected project whose folder is gone gives an empty board that names the missing folder, so the screen does not say "no project".</summary>
    [Fact]
    public async Task MissingFolderIsNamedNotReportedAsNoProject()
    {
        var vGone = Path.Combine(Path.GetTempPath(), $"chatur-gone-{Guid.NewGuid():N}");
        var vBoard = await new RequirementBoardActions(new FixedProjectActions(new ProjectSummary(1, "SmokeProj", vGone, "Folder", null)))
            .LoadAsync(RequirementFilter.None);

        Assert.False(vBoard.HasProject);
        Assert.Equal("SmokeProj", vBoard.ProjectName);
        Assert.Equal(vGone, vBoard.MissingFolder);
        Assert.Null((await objSut.LoadAsync(RequirementFilter.None)).MissingFolder);

        // A missing project directly under a folder that does exist is still missing, not an empty project.
        var vInsideExisting = Path.Combine(objFolder, "swept-away");
        var vSwept = await new RequirementBoardActions(new FixedProjectActions(new ProjectSummary(2, "Swept", vInsideExisting, "Folder", null)))
            .LoadAsync(RequirementFilter.None);
        Assert.Equal(vInsideExisting, vSwept.MissingFolder);
    }

    private sealed class FixedProjectActions : IProjectActions
    {
        private readonly ProjectSummary? objProject;

        public FixedProjectActions(ProjectSummary? aProject) => objProject = aProject;

        public Task<ProjectSummary?> SelectedProjectAsync(CancellationToken aCt = default) => Task.FromResult(objProject);

        public Task<IReadOnlyList<ProjectFolder>> ListFoldersAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProjectFolder> AddFolderAsync(string aPath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task RemoveFolderAsync(int aFolderId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProjectSummary>> ScanAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task SelectProjectAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task ForgetProjectAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();
    }
}
