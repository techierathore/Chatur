using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Board;
using Chatur.Core.Processes;
using Chatur.Core.RunReports;
using Chatur.Tests.AutoRun;
using Chatur.Tests.Support;
using Dapper;
using Xunit;

namespace Chatur.Tests.RunReports;

/// <summary>
/// Tests for the Run report over a real, migrated, temporary database holding a finished end-to-end run, a real
/// temporary project folder with a checklist and measurement streams (REQ-UI-058 to REQ-UI-061).
/// </summary>
public sealed class AutoRunReportTests : MigratedDatabaseFixture
{
    private const string Brief = "Finish the coverage panel and verify it before you stop, tagging every REQ id.";

    private readonly string objFolder = Path.Combine(Path.GetTempPath(), $"chatur-report-{Guid.NewGuid():N}");

    /// <summary>Creates the project folder with a checklist and the three measurement streams.</summary>
    public AutoRunReportTests()
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "docs", "metrics"));
        File.WriteAllText(
            Path.Combine(objFolder, "docs", "App-Checklist.md"),
            "# App — Checklist (phase 1)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n"
            + "| REQ-FN-002 | Two | Implemented | 75% | — | [view](#d-req-fn-002) |\n"
            + "| REQ-FN-003 | Three | Blocked | 25% | — | [view](#d-req-fn-003) |\n\n"
            + "## Page: P (`/p`)\n");
        File.WriteAllLines(Path.Combine(objFolder, "docs", "metrics", "runs.jsonl"),
        [
            """{"kind":"run","cmd":"build-phase","started":"2026-10-07T12:05:00Z","ended":"2026-10-07T12:09:00Z","model":"m-a","tier":"standard","tokens_in":1000,"tokens_out":400,"duration_s":300}""",
            """{"kind":"run","cmd":"build-phase","started":"2026-10-07T12:10:00Z","ended":"2026-10-07T12:20:00Z","model":"m-a","model_tokens_out":{"m-a":300,"m-b":100},"tokens_in":2000,"tokens_out":400,"duration_s":600}""",
            """{"kind":"run","cmd":"build-phase","started":"2026-10-07T11:00:00Z","ended":"2026-10-07T11:30:00Z","model":"m-old","tokens_in":9999,"tokens_out":9999,"duration_s":1800}""",
            """{"kind":"run","cmd":"verify-phase","mode":"process","started":"2026-10-07T12:15:00Z","ended":"2026-10-07T12:16:00Z","duration_s":60}"""
        ]);
        File.WriteAllLines(Path.Combine(objFolder, "docs", "metrics", "misses.jsonl"),
        [
            """{"kind":"miss","miss_id":"M1","req_id":"REQ-FN-003","miss_class":"unspecified-gap","artifact":"brd","what":"The brief never said which projects the filter spans","ts":"2026-10-07T12:12:00Z"}""",
            """{"kind":"miss","miss_id":"M2","req_id":"REQ-FN-002","miss_class":"wrong-behaviour","artifact":"src","what":"Before the run","ts":"2026-10-07T11:00:00Z"}""",
            """{"kind":"miss","miss_id":"M3","req_id":"REQ-FN-002","miss_class":"wrong-behaviour","artifact":"src","what":"After the run","ts":"2026-10-07T13:00:00Z"}"""
        ]);
    }

    /// <summary>
    /// When a run has finished, then the report lists the run, the requirement verdicts its process runs wrote with
    /// titles, the waits and retries, the misses inside its window and a cost whose rows add up to its totals
    /// (REQ-UI-058, REQ-UI-059, REQ-UI-060, REQ-UI-061).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-058 the report of a finished run reads verdicts, waits, misses and cost from the run")]
    public async Task ReportOfAFinishedRunIsReadFromTheRun()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        vHarness.Executor.Script = (aProcess, aStep, aTimes, _) =>
        {
            if (aProcess == "mockups" && aStep.StepCode == "s1")
            {
                vHarness.Clock.UtcNow += TimeSpan.FromMinutes(10);
            }

            if (aProcess == "build-phase" && aStep.StepCode == "s4" && aTimes == 1)
            {
                throw new Chatur.Core.ProcessRuns.ModelUsageLimitException("Usage limit reached", TimeSpan.FromMinutes(20));
            }

            return aProcess == "deploy-checklist" && aStep.StepCode == "s2" && aTimes == 1
                ? throw new InvalidOperationException("boom")
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        };
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        var vEnd = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vEnd!.State);
        var vRunIds = vEnd.Chain.ToDictionary(aProcess => aProcess.ProcessCode, aProcess => aProcess.ProcessRunId!.Value);
        AddVerdict(vRunIds["build-phase"], "REQ-FN-003", "FAIL");
        AddVerdict(vRunIds["verify-phase"], "REQ-FN-002", "Needs re-verify");
        AddVerdict(vRunIds["fix-issues"], "REQ-FN-003", "Verified");
        var vReports = Create(vProject);

        var vRuns = await vReports.RunsAsync();
        var vReport = await vReports.ReportAsync(vRuns[0].Key);

        Assert.Equal($"auto-{vStarted.AutoRunId}", vRuns[0].Key);
        Assert.Equal("auto", vRuns[0].Command);
        Assert.NotNull(vRuns[0].EndedUtc);
        Assert.Equal(["REQ-FN-003", "REQ-FN-002"], vReport!.Requirements.Select(aItem => aItem.Id));
        Assert.Equal("Verified", vReport.Requirements[0].Verdict);
        Assert.Equal("Three", vReport.Requirements[0].Title);
        Assert.Equal("Needs re-verify", vReport.Requirements[1].Verdict);
        Assert.Equal(1, vReport.WaitCount);
        Assert.Equal(1, vReport.RetryCount);
        Assert.Equal(20 * 60, vReport.WaitedSeconds);
        // Ten minutes of work plus the pause before the retry; the 20-minute wait is not counted.
        Assert.Equal((10 * 60) + (long)new AutoRunOptions().RetryDelay.TotalSeconds, vReport.DurationSeconds);
        Assert.False(vReport.IsPartial);
        var vMiss = Assert.Single(vReport.Misses);
        Assert.Equal("M1", vMiss.MissId);
        Assert.Equal(MissOwner.OwnerBrief, vMiss.Owner);
        Assert.Equal(["m-a", "m-b", "Not split by model"], vReport.Cost.Select(aCost => aCost.Model));
        Assert.Equal("standard", vReport.Cost[0].Tier);
        Assert.Equal(1000, vReport.Cost[0].TokensIn);
        Assert.Equal(700, vReport.Cost[0].TokensOut);
        Assert.Null(vReport.Cost[1].TokensIn);
        Assert.Equal(2000, vReport.Cost[2].TokensIn);
        Assert.All(vReport.Cost, aCost => Assert.Null(aCost.Calls));
        Assert.Equal(3000, vReport.TotalTokensIn);
        Assert.Equal(800, vReport.TotalTokensOut);
        Assert.Equal(3800, vReport.TotalTokens);
        Assert.Equal(vReport.TotalTokensIn, vReport.Cost.Sum(aCost => aCost.TokensIn ?? 0));
        Assert.Equal(vReport.TotalTokensOut, vReport.Cost.Sum(aCost => aCost.TokensOut ?? 0));
    }

    /// <summary>When nothing in the project's streams falls in a run's window, then cost is not recorded, never zero (REQ-UI-060).</summary>
    [Fact(DisplayName = "REQ-UI-060 a run with nothing recorded reports cost as not recorded")]
    public async Task CostNotRecordedStaysNull()
    {
        File.WriteAllText(Path.Combine(objFolder, "docs", "metrics", "runs.jsonl"), string.Empty);
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vReports = Create(vProject);

        var vReport = await vReports.ReportAsync($"auto-{vStarted.AutoRunId}");

        Assert.Empty(vReport!.Cost);
        Assert.Null(vReport.TotalTokens);
        Assert.Null(vReport.TotalTokensIn);
        Assert.Null(vReport.WaitedSeconds);
        Assert.Equal(0, vReport.WaitCount);
        Assert.Empty(vReport.Requirements);
    }

    /// <summary>Earlier runs are listed newest first and each is pickable by its key; an unknown key reads nothing (REQ-UI-061).</summary>
    [Fact(DisplayName = "REQ-UI-061 earlier runs are listed newest first and a run of another project is not reachable")]
    public async Task EarlierRunsArePickable()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vFirst = await vHarness.Actions.StartAsync(vProject, Brief);
        await vHarness.Actions.StopAsync(vFirst.AutoRunId);
        vHarness.Clock.UtcNow += TimeSpan.FromHours(1);
        var vSecond = await vHarness.Actions.StartAsync(vProject, Brief);
        var vOther = CreateProject("Other", objFolder + "-other");
        var vOtherRun = await vHarness.Actions.StartAsync(vOther, Brief);
        var vReports = Create(vProject);

        var vRuns = await vReports.RunsAsync();

        Assert.Equal([$"auto-{vSecond.AutoRunId}", $"auto-{vFirst.AutoRunId}"], vRuns.Select(aRun => aRun.Key));
        Assert.NotNull(await vReports.ReportAsync($"auto-{vFirst.AutoRunId}"));
        Assert.True((await vReports.ReportAsync($"auto-{vSecond.AutoRunId}"))!.IsPartial);
        Assert.Null(await vReports.ReportAsync($"auto-{vOtherRun.AutoRunId}"));
        Assert.Null(await vReports.ReportAsync("auto-9999"));
    }

    /// <summary>Deletes the temporary project folder as well as the database.</summary>
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objFolder))
        {
            Directory.Delete(objFolder, true);
        }
    }

    private RunReportActions Create(int aProjectId)
    {
        var vFactory = CreateFactory();
        var vProject = new ProjectSummary(aProjectId, "App", objFolder, "Folder", null);
        var vClock = new ManualClock();
        return new RunReportActions(
            new FixedProjectActions(vProject),
            new RequirementBoardActions(new FixedProjectActions(vProject)),
            new AutoRunStore(vFactory, vClock),
            vFactory,
            vClock);
    }

    private void AddVerdict(long aProcessRunId, string aRequirementId, string aStatus)
    {
        using var vConnection = CreateFactory().OpenConnection();
        vConnection.Execute(
            "INSERT INTO ProcessRunVerdict (RunId, RequirementId, ChecklistFile, Status, Percent, Remark, WrittenUtc) VALUES (@aProcessRunId, @aRequirementId, 'docs/App-Checklist.md', @aStatus, 100, 'r', '2026-10-07T12:20:00.0000000Z')",
            new { aProcessRunId, aRequirementId, aStatus });
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
