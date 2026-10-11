using Chatur.Core.Actions;
using Chatur.Core.Board;
using Chatur.Core.ProcessRuns;
using Chatur.Core.RunQueue;

namespace Chatur.Core.AutoRun;

/// <summary>
/// Reads a project's checklists to say which requirements are still open (REQ-FN-066): every one that is
/// not verified (<see cref="BoardStatuses.Planned"/>, <see cref="BoardStatuses.InProgress"/> or
/// <see cref="BoardStatuses.Blocked"/>). A status that fits none of the four — <c>N/A</c>, <c>Owner-UAT</c> —
/// is not open. Nothing is cached, so an edit shows the next time it is read.
/// </summary>
public sealed class OpenRequirementReader
{
    private readonly ProjectFolders objFolders;

    /// <summary>Creates the reader.</summary>
    /// <param name="aFolders">Finds the project's folder.</param>
    public OpenRequirementReader(ProjectFolders aFolders)
    {
        objFolders = aFolders;
    }

    /// <summary>The project's open requirements, furthest from verified first (lowest percentage, then file order).</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<IReadOnlyList<RequirementItem>> OpenAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false);
        var vDocs = vFolder is null ? null : Path.Combine(vFolder, "docs");
        if (vDocs is null || !Directory.Exists(vDocs))
        {
            return [];
        }

        var vOpen = new List<RequirementItem>();
        foreach (var vPath in Directory.EnumerateFiles(vDocs, "*Checklist*.md").Order(StringComparer.Ordinal))
        {
            var vName = "docs/" + Path.GetFileName(vPath);
            var vText = await File.ReadAllTextAsync(vPath, aCt).ConfigureAwait(false);
            vOpen.AddRange(ChecklistDocument.Parse(vText, vName, ChecklistDocument.PhaseOf(vName)).Where(IsOpen));
        }

        return vOpen.OrderBy(aItem => aItem.Percent).ToList();
    }

    /// <summary>
    /// The one requirement a run works from first: the open requirement furthest from verified by
    /// <see cref="RequirementOrder"/> (blocked or failed, then partial or in progress, then untouched, then
    /// built but unverified). The run queue's resume and the end-to-end run both take their focus from here
    /// (REQ-FN-070), so they cannot disagree.
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The requirement, or <see langword="null"/> when none is open.</returns>
    public async Task<RequirementItem?> FocusAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false);
        var vDocs = vFolder is null ? null : Path.Combine(vFolder, "docs");
        if (vDocs is null || !Directory.Exists(vDocs))
        {
            return null;
        }

        var vAll = new List<RequirementItem>();
        foreach (var vPath in Directory.EnumerateFiles(vDocs, "*Checklist*.md").Order(StringComparer.Ordinal))
        {
            var vName = "docs/" + Path.GetFileName(vPath);
            var vText = await File.ReadAllTextAsync(vPath, aCt).ConfigureAwait(false);
            vAll.AddRange(ChecklistDocument.Parse(vText, vName, ChecklistDocument.PhaseOf(vName)));
        }

        return RequirementOrder.OpenFurthestFirst(vAll).FirstOrDefault();
    }

    /// <summary>The counts of open requirements by where they stand.</summary>
    /// <param name="aOpen">The open requirements.</param>
    public static AutoRunOpenCounts Count(IReadOnlyList<RequirementItem> aOpen) => new(
        aOpen.Count,
        aOpen.Count(aItem => aItem.BoardStatus == BoardStatuses.Planned),
        aOpen.Count(aItem => aItem.BoardStatus == BoardStatuses.InProgress),
        aOpen.Count(aItem => aItem.BoardStatus == BoardStatuses.Blocked));

    private static bool IsOpen(RequirementItem aItem) =>
        aItem.BoardStatus is BoardStatuses.Planned or BoardStatuses.InProgress or BoardStatuses.Blocked;
}
