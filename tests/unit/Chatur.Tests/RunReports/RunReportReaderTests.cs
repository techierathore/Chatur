using Chatur.Core.Actions;
using Chatur.Core.RunReports;
using Xunit;

namespace Chatur.Tests.RunReports;

/// <summary>
/// Tests for the Run report reader (REQ-UI-058 to REQ-UI-061): it ties a run to its verdicts, misses and
/// cost by the time the run started.
/// </summary>
public sealed class RunReportReaderTests
{
    private const string Newer = "2026-09-22T02:00:00Z";
    private const string Older = "2026-09-20T09:05:00Z";

    private static readonly string[] Runs =
    [
        $$"""{"kind":"run","cmd":"auto","started":"{{Older}}","ended":"2026-09-20T10:05:00Z","reqs_touched":["REQ-UI-001"],"model":"m-old","tokens_in":100,"tokens_out":50,"duration_s":3600}""",
        $$"""{"kind":"run","cmd":"auto","started":"{{Newer}}","ended":"2026-09-22T05:14:00Z","reqs_touched":["REQ-UI-014","REQ-FN-021","REQ-NFR-004"],"model":"m-a","tier":"deep","model_tokens_out":{"m-a":900,"m-b":100},"tokens_in":1000,"tokens_out":1000,"duration_s":11640,"waited_s":1320}"""
    ];

    private static readonly string[] Gates =
    [
        $$"""{"kind":"gate","run_id":"{{Newer}}","req_id":"REQ-UI-014","verdict":"FAIL","ts":"2026-09-22T02:30:00Z"}""",
        $$"""{"kind":"gate","run_id":"{{Newer}}","req_id":"REQ-UI-014","verdict":"Verified","ts":"2026-09-22T02:41:00Z"}""",
        $$"""{"kind":"gate","run_id":"{{Newer}}","req_id":"REQ-FN-021","verdict":"Needs re-verify","ts":"2026-09-22T03:58:00Z"}""",
        $$"""{"kind":"gate","run_id":"{{Older}}","req_id":"REQ-UI-001","verdict":"Verified","ts":"2026-09-20T09:30:00Z"}"""
    ];

    private static readonly string[] Misses =
    [
        $$"""{"kind":"miss","miss_id":"M1","req_id":"REQ-FN-021","miss_class":"unspecified-gap","artifact":"brd","what":"The brief never said which projects the filter spans","found_run_id":"{{Newer}}"}""",
        $$"""{"kind":"miss","miss_id":"M2","req_id":"REQ-UI-017","miss_class":"missed-requirement","artifact":"src","found_by":"library-feedback","req_class":"UI","what":"The chip has no disabled state","found_run_id":"{{Newer}}"}""",
        $$"""{"kind":"miss","miss_id":"M3","req_id":"REQ-FN-022","miss_class":"wrong-behaviour","artifact":"src","what":"Step 4 was retried","origin_run_id":"{{Newer}}"}""",
        """{"kind":"miss-fix","miss_id":"M3","verdict_after":"Verified"}""",
        $$"""{"kind":"miss","miss_id":"M4","req_id":"REQ-UI-001","miss_class":"other","artifact":"src","found_run_id":"{{Older}}"}"""
    ];

    private static readonly IReadOnlyList<RequirementItem> Items =
    [
        new("REQ-UI-014", "UI", "Coverage panel file list", "Implemented", "In progress", 100, "", "flow-master", "Flow master", 3, "x.md", "", "", null, null)
    ];

    /// <summary>The picker lists both runs, newest first (REQ-UI-061).</summary>
    [Fact]
    public void RunsListsNewestFirst()
    {
        var vRuns = RunReportReader.Runs("P", Runs);

        Assert.Equal([Newer, Older], vRuns.Select(aRun => aRun.Key));
        Assert.All(vRuns, aRun => Assert.True(aRun.IsClosed));
    }

    /// <summary>Every requirement the run touched is listed with the last verdict recorded for it, and one with none reads empty (REQ-UI-058).</summary>
    [Fact]
    public void ReportListsEveryRequirementWithItsLastVerdict()
    {
        var vReport = RunReportReader.Report("P", Newer, Runs, Gates, Misses, Items)!;

        Assert.Equal(["REQ-UI-014", "REQ-FN-021", "REQ-NFR-004"], vReport.Requirements.Select(aItem => aItem.Id));
        Assert.Equal("Verified", vReport.Requirements[0].Verdict);
        Assert.Equal("Coverage panel file list", vReport.Requirements[0].Title);
        Assert.Equal("Needs re-verify", vReport.Requirements[1].Verdict);
        Assert.Equal(string.Empty, vReport.Requirements[2].Verdict);
        Assert.Equal("Verifier", vReport.Requirements[2].AgentName);
    }

    /// <summary>Each miss the run recorded is listed with whose gap it was and whether it was fixed; another run's miss is not (REQ-UI-059).</summary>
    [Fact]
    public void ReportListsMissesWithWhoseGap()
    {
        var vReport = RunReportReader.Report("P", Newer, Runs, Gates, Misses, Items)!;

        Assert.Equal(["M1", "M2", "M3"], vReport.Misses.Select(aMiss => aMiss.MissId));
        Assert.Equal(MissOwner.OwnerBrief, vReport.Misses[0].Owner);
        Assert.Equal(MissOwner.TrBlazeUI, vReport.Misses[1].Owner);
        Assert.Equal(MissOwner.Chatur, vReport.Misses[2].Owner);
        Assert.False(vReport.Misses[0].IsFixed);
        Assert.True(vReport.Misses[2].IsFixed);
        Assert.Equal("The chip has no disabled state", vReport.Misses[1].What);
    }

    /// <summary>Tokens, models and time come from the run's record; a figure it lacks stays empty (REQ-UI-060).</summary>
    [Fact]
    public void ReportShowsTokensModelsAndTime()
    {
        var vReport = RunReportReader.Report("P", Newer, Runs, Gates, Misses, Items)!;

        Assert.Equal(2000, vReport.TotalTokens);
        Assert.Equal(11640, vReport.DurationSeconds);
        Assert.Equal(1320, vReport.WaitedSeconds);
        Assert.Equal(["m-a", "m-b"], vReport.Cost.Select(aCost => aCost.Model));
        Assert.Equal("deep", vReport.Cost[0].Tier);
        Assert.Equal(900, vReport.Cost[0].TokensOut);
        Assert.Null(vReport.Cost[0].TokensIn);
        Assert.Null(vReport.Cost[0].Calls);
    }

    /// <summary>Picking the older run shows its own report, not the newest (REQ-UI-061).</summary>
    [Fact]
    public void ReportOfAnEarlierRunIsThatRunsOwn()
    {
        var vReport = RunReportReader.Report("P", Older, Runs, Gates, Misses, Items)!;

        Assert.Equal(["REQ-UI-001"], vReport.Requirements.Select(aItem => aItem.Id));
        Assert.Equal(["M4"], vReport.Misses.Select(aMiss => aMiss.MissId));
        Assert.Equal(150, vReport.TotalTokens);
        Assert.Null(RunReportReader.Report("P", "no-such-run", Runs, Gates, Misses, Items));
    }

    /// <summary>A run whose record was never closed reports itself as partial (UI design, error state).</summary>
    [Fact]
    public void UnclosedRunIsPartial()
    {
        string[] vRuns = ["""{"kind":"run","started":"2026-09-22T02:00:00Z","model":"m","tokens_out":5}"""];

        var vReport = RunReportReader.Report("P", "2026-09-22T02:00:00Z", vRuns, [], [], [])!;

        Assert.True(vReport.IsPartial);
        Assert.Null(vReport.DurationSeconds);
        Assert.Equal(5, vReport.TotalTokens);
    }
}
