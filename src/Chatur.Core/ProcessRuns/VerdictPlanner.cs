using System.Globalization;
using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Decides each requirement's verdict from test evidence and nothing else (REQ-UI-048, owner decision
/// of 2026-10-07). A requirement is <c>Verified</c> at 100% only when at least one test carrying its id
/// ran and every test carrying its id passed and no test command went unrun; every other graded
/// requirement is <c>Needs re-verify</c> with the reason. Pure, so it is tested without a disk.
/// </summary>
public static class VerdictPlanner
{
    /// <summary>The status written for a requirement a passing test proves.</summary>
    public const string VerifiedStatus = "Verified";

    /// <summary>The status written for a requirement no passing test proves.</summary>
    public const string NeedsReverifyStatus = "Needs re-verify";

    private const string ImplementedStatus = "Implemented";
    private const int MostPercentUnproven = 75;
    private const int MostNamed = 3;
    private const int MostNameChars = 60;

    /// <summary>Whether a requirement with this status word is graded by a verifying run.</summary>
    /// <param name="aFileStatus">The status cell's word.</param>
    public static bool IsGraded(string aFileStatus) =>
        string.Equals(aFileStatus, ImplementedStatus, StringComparison.OrdinalIgnoreCase)
        || string.Equals(aFileStatus, NeedsReverifyStatus, StringComparison.OrdinalIgnoreCase);

    /// <summary>Grades every requirement a verifying run grades; the others are not in the result.</summary>
    /// <param name="aItems">The checklist's requirements.</param>
    /// <param name="aEvidence">What the run's tests showed.</param>
    /// <param name="aRunId">The run, named in each remark.</param>
    /// <param name="aProcessCode">The process, named in each remark.</param>
    /// <param name="aDate">The date written into each remark.</param>
    public static IReadOnlyList<PlannedVerdict> Plan(
        IEnumerable<RequirementItem> aItems,
        TestEvidence aEvidence,
        int aRunId,
        string aProcessCode,
        DateTime aDate)
    {
        var vPrefix = string.Create(CultureInfo.InvariantCulture, $"[run {aRunId}] {aProcessCode} {aDate:yyyy-MM-dd}: ");
        return aItems
            .Where(aItem => IsGraded(aItem.FileStatus))
            .Select(aItem => Grade(aItem, aEvidence, vPrefix))
            .ToList();
    }

    private static PlannedVerdict Grade(RequirementItem aItem, TestEvidence aEvidence, string aPrefix)
    {
        var vTests = aEvidence.Carrying(aItem.Id);
        var vEvidence = aEvidence.For(aItem.Id);
        var vNotPassed = vTests.Where(aTest => aTest.Outcome != ProcessTestOutcome.Passed).ToList();
        if (vTests.Count > 0 && vNotPassed.Count == 0 && aEvidence.NotRunReasons.Count == 0)
        {
            return new PlannedVerdict(
                aItem.Id,
                VerifiedStatus,
                100,
                $"{aPrefix}Verified — {vTests.Count} of {vTests.Count} test{(vTests.Count == 1 ? string.Empty : "s")} carrying {aItem.Id} passed.",
                vEvidence);
        }

        return new PlannedVerdict(aItem.Id, NeedsReverifyStatus, UnprovenPercent(aItem), $"{aPrefix}Needs re-verify — {Reason(aItem, aEvidence, vTests, vNotPassed)}", vEvidence);
    }

    private static string Reason(RequirementItem aItem, TestEvidence aEvidence, IReadOnlyList<ReportedTest> aTests, List<ReportedTest> aNotPassed)
    {
        if (aEvidence.NotRunReasons.Count > 0 && aNotPassed.Count == 0)
        {
            return $"tests could not run: {string.Join("; ", aEvidence.NotRunReasons)}.";
        }

        if (aTests.Count == 0)
        {
            return $"no test carries {aItem.Id}.";
        }

        var vFailed = aNotPassed.Count(aTest => aTest.Outcome == ProcessTestOutcome.Failed);
        var vWord = vFailed == aNotPassed.Count ? "failed" : "did not pass";
        return $"{aNotPassed.Count} of {aTests.Count} test{(aTests.Count == 1 ? string.Empty : "s")} {vWord}: {Names(aNotPassed)}.";
    }

    private static string Names(IReadOnlyList<ReportedTest> aTests)
    {
        var vNames = aTests.Take(MostNamed).Select(aTest => Short(aTest.Name)).ToList();
        var vMore = aTests.Count - vNames.Count;
        return string.Join("; ", vNames) + (vMore > 0 ? $"; and {vMore} more" : string.Empty);
    }

    private static string Short(string aName)
    {
        var vOneLine = string.Join(' ', aName.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return vOneLine.Length > MostNameChars ? vOneLine[..(MostNameChars - 1)] + "…" : vOneLine;
    }

    /// <summary>An unproven requirement keeps the percentage it had, but never claims 100.</summary>
    private static int UnprovenPercent(RequirementItem aItem) =>
        aItem.Percent is >= 0 and < 100 && aItem.Percent % 25 == 0 ? aItem.Percent : MostPercentUnproven;
}
