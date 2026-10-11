namespace Chatur.Core.Actions;

/// <summary>
/// The Requirements board: every requirement of the selected project, read from and written to the
/// project's own checklist files (page Requirements board, <c>/board</c>; BRD-107 to BRD-112).
/// </summary>
public interface IRequirementBoardActions
{
    /// <summary>The agents the board can filter by (REQ-FN-056).</summary>
    IReadOnlyList<BoardAgent> Agents { get; }

    /// <summary>
    /// Reads the selected project's checklist files again from disk and narrows them by a filter
    /// (REQ-UI-049, REQ-UI-052, REQ-FN-056). Nothing is cached, so an edit made outside Chatur shows
    /// the next time this is called.
    /// </summary>
    /// <param name="aFilter">What to narrow to; <see cref="RequirementFilter.None"/> lists everything.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<RequirementBoard> LoadAsync(RequirementFilter aFilter, CancellationToken aCt = default);

    /// <summary>
    /// Writes a new status into the checklist file row of one requirement (REQ-FN-057). The row's
    /// status cell holds the checklist's own word for it afterwards; every other row and every other
    /// line of the file is untouched.
    /// </summary>
    /// <param name="aChecklistFile">The file the requirement is in, as <see cref="RequirementItem.ChecklistFile"/> names it.</param>
    /// <param name="aRequirementId">The requirement to change.</param>
    /// <param name="aBoardStatus">One of <see cref="BoardStatuses.All"/>.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="ArgumentException">The status is not one of the four, or the file or requirement is not in the selected project.</exception>
    /// <exception cref="InvalidOperationException">No project is selected.</exception>
    Task SetStatusAsync(string aChecklistFile, string aRequirementId, string aBoardStatus, CancellationToken aCt = default);
}
