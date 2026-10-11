using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Everything a verifying run learned from the project's tests: which test commands there were, which
/// of them could not run and why, and every test reported. The one place a requirement's test
/// evidence is worked out, for the grading and for what the screen shows beside each verdict.
/// </summary>
/// <param name="HasCommands">Whether the project has any test command at all.</param>
/// <param name="RanAny">Whether at least one test command ran far enough to report results.</param>
/// <param name="NotRunReasons">Why each test command that did not report results could not.</param>
/// <param name="Tests">Every test reported, by every command.</param>
public sealed record TestEvidence(
    bool HasCommands,
    bool RanAny,
    IReadOnlyList<string> NotRunReasons,
    IReadOnlyList<ReportedTest> Tests)
{
    /// <summary>The evidence of a run whose tests were never looked at.</summary>
    public static TestEvidence None { get; } = new(false, false, [], []);

    /// <summary>Builds the evidence from a run's recorded test commands and tests.</summary>
    /// <param name="aCommands">The test commands the run recorded.</param>
    /// <param name="aTests">The tests the run recorded.</param>
    public static TestEvidence From(IReadOnlyList<RunCommandRecord> aCommands, IReadOnlyList<ReportedTest> aTests) => new(
        aCommands.Count > 0,
        aCommands.Any(aCommand => aCommand.Outcome == RunCommandOutcome.Succeeded),
        aCommands.Where(aCommand => aCommand.Outcome != RunCommandOutcome.Succeeded)
            .Select(aCommand => aCommand.Reason ?? $"{aCommand.Command} did not report results")
            .ToList(),
        aTests);

    /// <summary>The tests whose names carry the requirement's id.</summary>
    /// <param name="aRequirementId">The requirement.</param>
    public IReadOnlyList<ReportedTest> Carrying(string aRequirementId) =>
        Tests.Where(aTest => aTest.Requirements.Contains(aRequirementId, StringComparer.Ordinal)).ToList();

    /// <summary>The evidence shown beside a requirement's verdict.</summary>
    /// <param name="aRequirementId">The requirement.</param>
    public ProcessRunVerdictEvidence For(string aRequirementId)
    {
        var vTests = Carrying(aRequirementId);
        return new ProcessRunVerdictEvidence(
            RanAny,
            vTests.Count,
            vTests.Count(aTest => aTest.Outcome == ProcessTestOutcome.Passed),
            vTests.Count(aTest => aTest.Outcome == ProcessTestOutcome.Failed),
            vTests.Select(aTest => new ProcessRunTestResult(aTest.Name, aTest.Outcome, aTest.Message)).ToList());
    }
}
