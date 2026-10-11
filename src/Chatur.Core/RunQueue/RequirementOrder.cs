using Chatur.Core.Actions;

namespace Chatur.Core.RunQueue;

/// <summary>
/// Orders a project's open requirements by how far they are from verified, so a resumed run takes the
/// hardest thing left first (REQ-FN-070). Pure, so the order is tested without a disk.
/// </summary>
public static class RequirementOrder
{
    /// <summary>
    /// How far a checklist status word is from verified: 0 for blocked or failed, 1 for partial or in
    /// progress, 2 for untouched, 3 for built and waiting to be verified, <see langword="null"/> when
    /// the requirement is not open (verified, pre-existing, not applicable).
    /// </summary>
    /// <param name="aFileStatus">The status cell's text.</param>
    public static int? Distance(string aFileStatus)
    {
        var vWord = aFileStatus.Replace("*", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
        return vWord switch
        {
            "blocked" or "fail" => 0,
            "partial" or "in progress" => 1,
            "not started" => 2,
            "implemented" or "needs re-verify" => 3,
            _ => null,
        };
    }

    /// <summary>The open requirements, furthest from verified first; equals keep their file order, the lower percentage first.</summary>
    /// <param name="aItems">Every requirement of the project.</param>
    public static IReadOnlyList<RequirementItem> OpenFurthestFirst(IEnumerable<RequirementItem> aItems) =>
        aItems
            .Select(aItem => (Item: aItem, Distance: Distance(aItem.FileStatus)))
            .Where(aPair => aPair.Distance is not null)
            .OrderBy(aPair => aPair.Distance)
            .ThenBy(aPair => aPair.Item.Percent)
            .Select(aPair => aPair.Item)
            .ToList();
}
