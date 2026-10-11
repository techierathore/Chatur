namespace Chatur.Core.Actions;

/// <summary>
/// The four statuses the Requirements board offers (UI Design "Screen: Requirements board",
/// Status field). The checklist's own words are folded into these; see
/// <see cref="IRequirementBoardActions"/>.
/// </summary>
public static class BoardStatuses
{
    /// <summary>Nothing has been built yet — the checklist's <c>Not Started</c>.</summary>
    public const string Planned = "Planned";

    /// <summary>Work has begun or is built but not yet verified — <c>In Progress</c>, <c>Implemented</c>, <c>PARTIAL</c>, <c>Needs re-verify</c>.</summary>
    public const string InProgress = "In progress";

    /// <summary>An executed verify run passed — <c>Verified</c>, <c>Done (pre-existing)</c>.</summary>
    public const string Verified = "Verified";

    /// <summary>Held up — <c>Blocked</c>, <c>FAIL</c>.</summary>
    public const string Blocked = "Blocked";

    /// <summary>The four statuses, in the order the board lists them.</summary>
    public static IReadOnlyList<string> All { get; } = [Planned, InProgress, Verified, Blocked];
}

/// <summary>One requirement as the board shows it, read from a project's checklist file (REQ-UI-049).</summary>
/// <param name="Id">The requirement id, e.g. <c>REQ-UI-007</c>.</param>
/// <param name="Type">The id's type prefix, e.g. <c>REQ-UI</c>.</param>
/// <param name="Title">The requirement's title.</param>
/// <param name="FileStatus">The status word exactly as the checklist file holds it, e.g. <c>Implemented</c>.</param>
/// <param name="BoardStatus">The file status folded into a <see cref="BoardStatuses"/> value, or the file word itself when it fits none (e.g. <c>N/A</c>).</param>
/// <param name="Percent">The percentage cell, or 0 when it is not a number.</param>
/// <param name="Remarks">The remarks cell.</param>
/// <param name="AgentCode">The code of the agent that owns this type of requirement (REQ-UI-050).</param>
/// <param name="AgentName">The owning agent's display name.</param>
/// <param name="Phase">The phase of the checklist file the row is in.</param>
/// <param name="ChecklistFile">The checklist file's path relative to the project folder, e.g. <c>docs/Chatur-P2-Checklist.md</c>.</param>
/// <param name="Acceptance">The acceptance line from the checklist's detail section, or empty.</param>
/// <param name="ScreenName">The name of the page section the requirement is under, or empty.</param>
/// <param name="ScreenRoute">That page's route, e.g. <c>/board</c>, or <see langword="null"/>.</param>
/// <param name="Mockup">The mockup path named in the detail section, or <see langword="null"/>.</param>
public sealed record RequirementItem(
    string Id,
    string Type,
    string Title,
    string FileStatus,
    string BoardStatus,
    int Percent,
    string Remarks,
    string AgentCode,
    string AgentName,
    int Phase,
    string ChecklistFile,
    string Acceptance,
    string ScreenName,
    string? ScreenRoute,
    string? Mockup);

/// <summary>What the board narrows to (REQ-FN-056). A <see langword="null"/> member does not narrow.</summary>
/// <param name="Phase">Only requirements of this phase.</param>
/// <param name="Status">Only requirements with this <see cref="BoardStatuses"/> value.</param>
/// <param name="AgentCode">Only requirements owned by this agent code, e.g. <c>flow-master</c>.</param>
/// <param name="Search">Words from the title or an id; 2 to 40 characters, anything else is ignored.</param>
public sealed record RequirementFilter(int? Phase = null, string? Status = null, string? AgentCode = null, string? Search = null)
{
    /// <summary>The shortest search that narrows the list.</summary>
    public const int MinSearchLength = 2;

    /// <summary>The longest search that narrows the list.</summary>
    public const int MaxSearchLength = 40;

    /// <summary>The filter that lists everything.</summary>
    public static RequirementFilter None { get; } = new();

    /// <summary>Whether a search text is long enough, and not too long, to narrow the list.</summary>
    /// <param name="aSearch">The text typed in the search box.</param>
    public static bool IsValidSearch(string? aSearch)
    {
        var vLength = aSearch?.Trim().Length ?? 0;
        return vLength is >= MinSearchLength and <= MaxSearchLength;
    }

    /// <summary>The requirements that pass every member of this filter, in their original order (REQ-FN-056).</summary>
    /// <param name="aItems">Every requirement of the project.</param>
    public IReadOnlyList<RequirementItem> Apply(IEnumerable<RequirementItem> aItems)
    {
        var vSearch = IsValidSearch(Search) ? Search!.Trim() : null;
        return aItems
            .Where(aItem => Phase is null || aItem.Phase == Phase)
            .Where(aItem => string.IsNullOrEmpty(Status) || string.Equals(aItem.BoardStatus, Status, StringComparison.OrdinalIgnoreCase))
            .Where(aItem => string.IsNullOrEmpty(AgentCode) || string.Equals(aItem.AgentCode, AgentCode, StringComparison.OrdinalIgnoreCase))
            .Where(aItem => vSearch is null
                || aItem.Id.Contains(vSearch, StringComparison.OrdinalIgnoreCase)
                || aItem.Title.Contains(vSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

/// <summary>How many requirements sit in each board status, over the whole project (the <c>status-counts</c> row).</summary>
/// <param name="All">Every requirement.</param>
/// <param name="Planned">Requirements in <see cref="BoardStatuses.Planned"/>.</param>
/// <param name="InProgress">Requirements in <see cref="BoardStatuses.InProgress"/>.</param>
/// <param name="Verified">Requirements in <see cref="BoardStatuses.Verified"/>.</param>
/// <param name="Blocked">Requirements in <see cref="BoardStatuses.Blocked"/>.</param>
public sealed record BoardCounts(int All, int Planned, int InProgress, int Verified, int Blocked);

/// <summary>An agent the board can filter by.</summary>
/// <param name="Code">The agent's code, e.g. <c>flow-master</c>.</param>
/// <param name="Name">The agent's display name, e.g. <c>Flow master</c>.</param>
public sealed record BoardAgent(string Code, string Name);

/// <summary>What the board shows for the selected project, read fresh from the checklist files (REQ-UI-049, REQ-UI-052).</summary>
/// <param name="HasProject">Whether a project is selected and its folder exists.</param>
/// <param name="ProjectName">The project's display name, or empty.</param>
/// <param name="ChecklistFiles">The checklist files read, relative to the project folder.</param>
/// <param name="Phases">The distinct phases found, ascending.</param>
/// <param name="Items">Every requirement that passes the filter.</param>
/// <param name="Counts">The counts over the whole project, before the filter.</param>
public sealed record RequirementBoard(
    bool HasProject,
    string ProjectName,
    IReadOnlyList<string> ChecklistFiles,
    IReadOnlyList<int> Phases,
    IReadOnlyList<RequirementItem> Items,
    BoardCounts Counts);
