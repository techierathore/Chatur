using Chatur.Core.Actions;
using Chatur.Core.ProcessRuns;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>Tests for <see cref="VerdictPlanner"/>: a verdict is decided from test evidence and nothing else (REQ-UI-048).</summary>
public sealed class VerdictPlannerTests
{
    private static readonly DateTime Day = new(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>When every test carrying an id passed and every command ran, then the requirement is Verified at 100%.</summary>
    [Fact(DisplayName = "REQ-UI-048 all passing tests verify a requirement")]
    public void AllPassingVerifies()
    {
        var vPlan = Plan(Item("REQ-FN-001"), Evidence([Test("REQ-FN-001 a", ProcessTestOutcome.Passed), Test("REQ-FN-001 b", ProcessTestOutcome.Passed)]));

        var vVerdict = Assert.Single(vPlan);
        Assert.Equal("Verified", vVerdict.Status);
        Assert.Equal(100, vVerdict.Percent);
        Assert.Equal("[run 4] verify-phase 2026-10-07: Verified — 2 of 2 tests carrying REQ-FN-001 passed.", vVerdict.Remark);
    }

    /// <summary>When any test carrying an id failed, then the requirement is Needs re-verify and the failing names are listed, three at most.</summary>
    [Fact(DisplayName = "REQ-UI-048 a failing test names itself in the reason")]
    public void FailingTestsAreNamedThreeAtMost()
    {
        var vTests = new[] { "a", "b", "c", "d", "e" }.Select(aName => Test($"REQ-FN-001 {aName}", ProcessTestOutcome.Failed)).Append(Test("REQ-FN-001 ok", ProcessTestOutcome.Passed)).ToList();

        var vVerdict = Assert.Single(Plan(Item("REQ-FN-001"), Evidence(vTests)));

        Assert.Equal("Needs re-verify", vVerdict.Status);
        Assert.EndsWith("Needs re-verify — 5 of 6 tests failed: REQ-FN-001 a; REQ-FN-001 b; REQ-FN-001 c; and 2 more.", vVerdict.Remark, StringComparison.Ordinal);
        Assert.Equal((6, 1, 5), (vVerdict.Evidence.TestsFound, vVerdict.Evidence.TestsPassed, vVerdict.Evidence.TestsFailed));
    }

    /// <summary>When a test carrying the id was skipped, then it does not count as passed and the requirement is not Verified.</summary>
    [Fact(DisplayName = "REQ-UI-048 a skipped test does not verify")]
    public void SkippedTestDoesNotVerify()
    {
        var vVerdict = Assert.Single(Plan(Item("REQ-FN-001"), Evidence([Test("REQ-FN-001 a", ProcessTestOutcome.Passed), Test("REQ-FN-001 b", ProcessTestOutcome.Skipped)])));

        Assert.Equal("Needs re-verify", vVerdict.Status);
        Assert.EndsWith("1 of 2 tests did not pass: REQ-FN-001 b.", vVerdict.Remark, StringComparison.Ordinal);
    }

    /// <summary>When tests ran but none carries the id, or the project has no tests, then the reason is "no test carries".</summary>
    [Fact(DisplayName = "REQ-UI-048 an untested requirement is Needs re-verify")]
    public void UntestedRequirementIsNotVerified()
    {
        var vRan = Assert.Single(Plan(Item("REQ-FN-009"), Evidence([Test("REQ-FN-001 a", ProcessTestOutcome.Passed)])));
        var vNone = Assert.Single(Plan(Item("REQ-FN-009"), TestEvidence.None));

        Assert.EndsWith("Needs re-verify — no test carries REQ-FN-009.", vRan.Remark, StringComparison.Ordinal);
        Assert.EndsWith("Needs re-verify — no test carries REQ-FN-009.", vNone.Remark, StringComparison.Ordinal);
        Assert.True(vRan.Evidence.TestsRan);
        Assert.False(vNone.Evidence.TestsRan);
    }

    /// <summary>When a test command could not run, then even a requirement whose tests passed is not Verified and the reason says why.</summary>
    [Fact(DisplayName = "REQ-UI-048 a test command that could not run blocks Verified")]
    public void UnrunTestCommandBlocksVerified()
    {
        var vEvidence = new TestEvidence(true, true, ["npx playwright test ran past its limit"], [Test("REQ-FN-001 a", ProcessTestOutcome.Passed)]);

        var vPassing = Assert.Single(Plan(Item("REQ-FN-001"), vEvidence));
        var vUntested = Assert.Single(Plan(Item("REQ-FN-002"), vEvidence));

        Assert.EndsWith("Needs re-verify — tests could not run: npx playwright test ran past its limit.", vPassing.Remark, StringComparison.Ordinal);
        Assert.EndsWith("Needs re-verify — tests could not run: npx playwright test ran past its limit.", vUntested.Remark, StringComparison.Ordinal);
    }

    /// <summary>When a requirement is Implemented or Needs re-verify, defect mark or not, then it is graded; any other status is left out.</summary>
    [Fact(DisplayName = "REQ-UI-048 only Implemented and Needs re-verify rows are graded")]
    public void OnlyWaitingRowsAreGraded()
    {
        var vItems = new[] { "Implemented", "Needs re-verify", "Not Started", "In Progress", "Blocked", "N/A", "Verified" }
            .Select((aStatus, aIndex) => Item($"REQ-FN-00{aIndex + 1}", aStatus, 75, "⚠ visual: clipped."))
            .ToList();

        var vPlan = VerdictPlanner.Plan(vItems, TestEvidence.None, 1, "verify-phase", Day);

        Assert.Equal(["REQ-FN-001", "REQ-FN-002"], vPlan.Select(aVerdict => aVerdict.RequirementId));
    }

    /// <summary>When a requirement is not proven, then it keeps the percentage it had but never claims 100.</summary>
    [Theory(DisplayName = "REQ-UI-048 an unproven row never keeps 100 percent")]
    [InlineData(100, 75)]
    [InlineData(75, 75)]
    [InlineData(50, 50)]
    [InlineData(0, 0)]
    [InlineData(60, 75)]
    public void UnprovenPercent(int aBefore, int aAfter)
    {
        var vVerdict = Assert.Single(Plan(Item("REQ-FN-001", "Needs re-verify", aBefore), TestEvidence.None));

        Assert.Equal(aAfter, vVerdict.Percent);
    }

    private static IReadOnlyList<PlannedVerdict> Plan(RequirementItem aItem, TestEvidence aEvidence) =>
        VerdictPlanner.Plan([aItem], aEvidence, 4, "verify-phase", Day);

    private static TestEvidence Evidence(IReadOnlyList<ReportedTest> aTests) => new(true, true, [], aTests);

    private static ReportedTest Test(string aName, ProcessTestOutcome aOutcome) =>
        new("dotnet", aName, aOutcome, RequirementIdScanner.Scan(aName), null, 1);

    private static RequirementItem Item(string aId, string aStatus = "Implemented", int aPercent = 75, string aRemarks = "") =>
        new(aId, "REQ-FN", "Title", aStatus, aStatus, aPercent, aRemarks, "flow-master", "Flow master", 2, "docs/App-P2-Checklist.md", string.Empty, string.Empty, null, null);
}
