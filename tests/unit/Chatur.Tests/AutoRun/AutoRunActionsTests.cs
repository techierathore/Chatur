using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Processes;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.AutoRun;

/// <summary>
/// Tests for the end-to-end run (<see cref="AutoRunActions"/>) over a real, migrated, temporary
/// database, a real temporary project folder with a checklist, and a scripted step executor
/// (REQ-FN-061 to REQ-FN-066).
/// </summary>
public sealed class AutoRunActionsTests : MigratedDatabaseFixture
{
    private const string Brief = "Finish the coverage panel and verify it before you stop, tagging every REQ id.";

    private const string OpenChecklist =
        "# App — Checklist (phase 1)\n\n## Requirements Status\n\n| ID | Requirement | Status | % | Remarks | Details |\n|----|---|---|---|---|---|\n"
        + "| REQ-FN-001 | One | Not Started | 0% | — | [view](#d-req-fn-001) |\n"
        + "| REQ-FN-002 | Two | Implemented | 75% | — | [view](#d-req-fn-002) |\n"
        + "| REQ-FN-003 | Three | Blocked | 25% | — | [view](#d-req-fn-003) |\n"
        + "| REQ-FN-004 | Four | Verified | 100% | — | [view](#d-req-fn-004) |\n\n"
        + "## Page: P (`/p`)\n";

    private readonly string objFolder = Path.Combine(Path.GetTempPath(), $"chatur-auto-{Guid.NewGuid():N}");

    /// <summary>Creates the project folder with a checklist that has open requirements.</summary>
    public AutoRunActionsTests()
    {
        Directory.CreateDirectory(Path.Combine(objFolder, "docs"));
        File.WriteAllText(Path.Combine(objFolder, "docs", "App-Checklist.md"), OpenChecklist);
    }

    /// <summary>
    /// When the owner starts a run with a brief and the run is carried on, then every process of the chain
    /// runs in order, each with its own process run, without anyone starting them (REQ-FN-061).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-061 a brief in runs the whole chain without being told each process")]
    public async Task BriefRunsTheWholeChain()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);

        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal([vStarted.AutoRunId], vHarness.Launcher.Launched);
        Assert.Equal(AutoRunChain.Processes, vAfter!.Chain.Select(aProcess => aProcess.ProcessCode));
        Assert.All(vAfter.Chain, aProcess => Assert.Equal(AutoRunProcessState.Finished, aProcess.State));
        Assert.All(vAfter.Chain, aProcess => Assert.NotNull(aProcess.ProcessRunId));
        Assert.Equal(AutoRunChain.Processes, vHarness.Executor.Calls.Select(aCall => aCall.Split(':')[0]).Distinct());
        Assert.Contains(vAfter.Events, aEvent => aEvent.Kind == AutoRunEventKind.ProcessStarted && aEvent.Message.StartsWith("Flow master started on build-phase", StringComparison.Ordinal));
    }

    /// <summary>When the brief is shorter than a sentence or the project is unknown, then nothing is started (REQ-FN-061).</summary>
    [Fact(DisplayName = "REQ-FN-061 a brief that is too short or a project that is unknown starts nothing")]
    public async Task ShortBriefOrUnknownProjectStartsNothing()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);

        await Assert.ThrowsAsync<ArgumentException>(() => vHarness.Actions.StartAsync(vProject, "Too short"));
        await Assert.ThrowsAsync<ArgumentException>(() => vHarness.Actions.StartAsync(9999, Brief));

        Assert.Empty(vHarness.Launcher.Launched);
        Assert.Empty(await vHarness.Actions.ListAsync(vProject));
    }

    /// <summary>When a run is already going on the project, then a second one is refused (REQ-FN-061).</summary>
    [Fact(DisplayName = "REQ-FN-061 a second run is refused while one is going on the project")]
    public async Task SecondRunIsRefusedWhileOneGoes()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vFirst = await vHarness.Actions.StartAsync(vProject, Brief);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vHarness.Actions.StartAsync(vProject, Brief));

        Assert.Equal(vFirst.AutoRunId, (await vHarness.Actions.CurrentAsync(vProject))!.AutoRunId);
    }

    /// <summary>
    /// When the chain finishes, then the run reads ready for the owner's acceptance test, and carrying it on
    /// again runs nothing further (REQ-FN-062).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-062 the finished chain reads ready for acceptance and nothing further is run")]
    public async Task FinishedChainStopsAtAcceptance()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        var vReady = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vCalls = vHarness.Executor.Calls.Count;
        var vRuns = (await vHarness.Runs.ListAsync(vProject)).Count;

        var vAgain = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.ReadyForAcceptance, vReady!.State);
        Assert.Equal("ready for your acceptance test", vReady.StateText);
        Assert.Equal(100, vReady.ProgressPercent);
        Assert.NotNull(vReady.EndedUtc);
        Assert.Null(vReady.Now);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vAgain!.State);
        Assert.Equal(vCalls, vHarness.Executor.Calls.Count);
        Assert.Equal(vRuns, (await vHarness.Runs.ListAsync(vProject)).Count);
        Assert.Null(await vHarness.Actions.CurrentAsync(vProject));
    }

    /// <summary>
    /// When an owner stop is reached inside the chain, then the run answers it yes itself and writes that
    /// into its record, so the only real stop is the acceptance test (REQ-FN-061, REQ-FN-062).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-062 owner stops inside the chain are answered by the run and recorded")]
    public async Task OwnerStopsInsideTheChainAreAnsweredAndRecorded()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Contains(vAfter!.Events, aEvent => aEvent.Kind == AutoRunEventKind.OwnerStopAnswered);
        var vBuild = await vHarness.Runs.GetAsync(vAfter.Chain.First(aProcess => aProcess.ProcessCode == "build-phase").ProcessRunId!.Value);
        Assert.Equal("Yes", vBuild!.Steps[8].Answer);
        Assert.Equal(ProcessRunState.Finished, vBuild.State);
    }

    /// <summary>
    /// When no end-to-end run is going, then every one of the twelve processes can still be started on its
    /// own, exactly as Processes does, and no end-to-end run is recorded (REQ-FN-063).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-063 with no run going every process can still be started on its own")]
    public async Task EveryProcessStartsOnItsOwnWithNoRunGoing()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);

        Assert.Null(await vHarness.Actions.CurrentAsync(vProject));
        var vStarts = new List<ProcessRunStarted>();
        foreach (var vProcess in ProcessCatalogue.All)
        {
            vStarts.Add(await vHarness.Processes.StartAsync(vProject, vProcess.Code));
        }

        Assert.Equal(12, vStarts.Select(aStart => aStart.RunId).Distinct().Count());
        Assert.All(vStarts, aStart => Assert.False(aStart.AlreadyRunning));
        Assert.Empty(await vHarness.Actions.ListAsync(vProject));
        var vListed = await vHarness.Processes.ListAsync(vProject);
        Assert.All(vListed, aProcess => Assert.True(aProcess.IsRunning));
    }

    /// <summary>When a run has ended, then a process can be started on its own again (REQ-FN-063).</summary>
    [Fact(DisplayName = "REQ-FN-063 after a run has ended a process starts on its own again")]
    public async Task ProcessStartsOnItsOwnAfterARunEnded()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        var vOwn = await vHarness.Processes.StartAsync(vProject, "mockups");

        Assert.False(vOwn.AlreadyRunning);
        Assert.Equal(1, vOwn.CurrentStep);
    }

    /// <summary>
    /// When a model reports a usage limit in the middle of a process, then the run waits the time the model
    /// named, carries on by itself on the same step, redoes nothing, and the wait is in its record (REQ-FN-064).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-064 a usage limit is waited out, resumed on the same step and recorded")]
    public async Task UsageLimitIsWaitedOutAndRecorded()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        vHarness.Executor.Script = (aProcess, aStep, aTimes, _) =>
            aProcess == "build-phase" && aStep.StepCode == "s4" && aTimes == 1
                ? throw new Chatur.Core.ProcessRuns.ModelUsageLimitException("Usage limit reached", TimeSpan.FromMinutes(20))
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStartedAt = vHarness.Clock.UtcNow;
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        var vWait = Assert.Single(vAfter!.Events, aEvent => aEvent.Kind == AutoRunEventKind.LimitWait);
        Assert.Equal(vStartedAt.AddMinutes(20), vWait.UntilUtc);
        Assert.Equal("build-phase", vWait.ProcessCode);
        Assert.Contains("20 minutes", vWait.Message, StringComparison.Ordinal);
        Assert.Contains("step 4 of build-phase", vWait.Message, StringComparison.Ordinal);
        Assert.Single(vAfter.Events, aEvent => aEvent.Kind == AutoRunEventKind.LimitResumed);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vAfter.State);
        Assert.Equal(TimeSpan.FromMinutes(20), vHarness.Delay.Waited.Aggregate(TimeSpan.Zero, (aSum, aSpan) => aSum + aSpan));
        Assert.Equal(2, vHarness.Executor.Calls.Count(aCall => aCall == "build-phase:s4"));
        Assert.Equal(1, vHarness.Executor.Calls.Count(aCall => aCall == "build-phase:s3"));
        Assert.Null(vAfter.ResumeAtUtc);
    }

    /// <summary>
    /// When the model names no wait, then the default wait is used; and while the run waits the state reads
    /// waiting out a usage limit with the time it carries on (REQ-FN-064).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-064 a limit with no named wait uses the default and the state shows the wait")]
    public async Task LimitWithNoNamedWaitUsesTheDefault()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        AutoRunView? vSeen = null;
        vHarness.Delay.AfterWait = async () => vSeen ??= await vHarness.Actions.GetAsync(1);
        vHarness.Executor.Script = (aProcess, aStep, aTimes, _) =>
            aProcess == "day1-greenfield" && aStep.StepCode == "s1" && aTimes == 1
                ? throw new Chatur.Core.ProcessRuns.ModelUsageLimitException("Rate limited")
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStartedAt = vHarness.Clock.UtcNow;
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.WaitingForLimit, vSeen!.State);
        Assert.Equal("waiting out a usage limit", vSeen.StateText);
        Assert.NotNull(vSeen.ResumeAtUtc);
        Assert.Equal(vStartedAt + new AutoRunOptions().DefaultLimitWait, vSeen.ResumeAtUtc);
    }

    /// <summary>When the owner pauses a run that is waiting out a limit, then it stops waiting and does not carry on by itself (REQ-FN-064).</summary>
    [Fact(DisplayName = "REQ-FN-064 a run paused during a limit wait does not carry on by itself")]
    public async Task PausedDuringAWaitDoesNotCarryOn()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        vHarness.Delay.AfterWait = async () => await vHarness.Actions.PauseAsync(1);
        vHarness.Executor.Script = (aProcess, aStep, aTimes, _) =>
            aProcess == "day1-greenfield" && aStep.StepCode == "s1" && aTimes == 1
                ? throw new Chatur.Core.ProcessRuns.ModelUsageLimitException("Rate limited", TimeSpan.FromMinutes(5))
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Paused, vAfter!.State);
        Assert.Single(vHarness.Executor.Calls);
    }

    /// <summary>When the limit keeps coming back past the allowed waits, then the run stops with a reason (REQ-FN-064).</summary>
    [Fact(DisplayName = "REQ-FN-064 a limit that never lifts stops the run with a reason after the allowed waits")]
    public async Task LimitThatNeverLiftsStopsTheRun()
    {
        var vHarness = new AutoRunHarness(CreateFactory(), new AutoRunOptions { MaxLimitWaits = 2 });
        vHarness.Executor.Script = (_, _, _, _) => throw new Chatur.Core.ProcessRuns.ModelUsageLimitException("Usage limit reached", TimeSpan.FromMinutes(1));
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Stopped, vAfter!.State);
        Assert.Contains("usage limit", vAfter.StopReason, StringComparison.Ordinal);
        Assert.Equal(2, vAfter.Events.Count(aEvent => aEvent.Kind == AutoRunEventKind.LimitWait));
    }

    /// <summary>
    /// When a step crashes once, then it is tried again, the retry is recorded against the process, and the
    /// run carries on to the end (REQ-FN-065).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-065 a crashed step is retried, the retry is recorded and the run carries on")]
    public async Task CrashedStepIsRetriedAndRecorded()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        vHarness.Executor.Script = (aProcess, aStep, aTimes, _) =>
            aProcess == "deploy-checklist" && aStep.StepCode == "s2" && aTimes == 1
                ? throw new InvalidOperationException("boom")
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        var vRetry = Assert.Single(vAfter!.Events, aEvent => aEvent.Kind == AutoRunEventKind.Retry);
        Assert.Equal("deploy-checklist", vRetry.ProcessCode);
        Assert.Contains("crashed", vRetry.Message, StringComparison.Ordinal);
        Assert.Contains("boom", vRetry.Message, StringComparison.Ordinal);
        Assert.Equal(1, vAfter.Chain.First(aProcess => aProcess.ProcessCode == "deploy-checklist").Retries);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vAfter.State);
        Assert.Equal(2, vHarness.Executor.Calls.Count(aCall => aCall == "deploy-checklist:s2"));
    }

    /// <summary>When a step stalls past the stall limit, then it is stopped and tried again, and the retry says it stalled (REQ-FN-065).</summary>
    [Fact(DisplayName = "REQ-FN-065 a stalled step is stopped, retried and recorded as stalled")]
    public async Task StalledStepIsRetriedAndRecorded()
    {
        var vHarness = new AutoRunHarness(CreateFactory(), new AutoRunOptions { StallTimeout = TimeSpan.FromMilliseconds(100) });
        vHarness.Executor.Script = async (aProcess, aStep, aTimes, aCt) =>
        {
            if (aProcess == "split-brd" && aStep.StepCode == "s1" && aTimes == 1)
            {
                await Task.Delay(Timeout.Infinite, aCt);
            }

            return new ProcessStepOutcome(true, "Done");
        };
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        var vRetry = Assert.Single(vAfter!.Events, aEvent => aEvent.Kind == AutoRunEventKind.Retry);
        Assert.Contains("stalled", vRetry.Message, StringComparison.Ordinal);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vAfter.State);
    }

    /// <summary>When a step keeps crashing past the allowed retries, then the run stops and says which step and why (REQ-FN-065).</summary>
    [Fact(DisplayName = "REQ-FN-065 a step that keeps crashing stops the run with the reason")]
    public async Task StepThatKeepsCrashingStopsTheRunWithAReason()
    {
        var vHarness = new AutoRunHarness(CreateFactory(), new AutoRunOptions { MaxRetries = 2 });
        vHarness.Executor.Script = (aProcess, aStep, _, _) =>
            aProcess == "build-phase" && aStep.StepCode == "s4"
                ? throw new InvalidOperationException("disk gone")
                : Task.FromResult(new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Stopped, vAfter!.State);
        Assert.Contains("Step 4 of build-phase crashed 3 times in a row", vAfter.StopReason, StringComparison.Ordinal);
        Assert.Contains("disk gone", vAfter.StopReason, StringComparison.Ordinal);
        Assert.Equal(2, vAfter.Chain.First(aProcess => aProcess.ProcessCode == "build-phase").Retries);
        Assert.Equal(AutoRunProcessState.Stopped, vAfter.Chain.First(aProcess => aProcess.ProcessCode == "build-phase").State);
        Assert.Equal(AutoRunProcessState.ToCome, vAfter.Chain.First(aProcess => aProcess.ProcessCode == "verify-phase").State);
        Assert.NotNull(vAfter.EndedUtc);
    }

    /// <summary>When a check of a process fails, then the run stops with the process, the step and the check's own words (REQ-FN-065).</summary>
    [Fact(DisplayName = "REQ-FN-065 a failed check stops the run and names the step and why")]
    public async Task FailedCheckStopsTheRunWithTheReason()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        vHarness.Executor.Script = (aProcess, aStep, _, _) =>
            Task.FromResult(aProcess == "build-phase" && aStep.StepCode == "s5"
                ? new ProcessStepOutcome(false, "The build left 3 warnings behind.")
                : new ProcessStepOutcome(true, "Done"));
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Stopped, vAfter!.State);
        Assert.Contains("build-phase stopped at step 5", vAfter.StopReason, StringComparison.Ordinal);
        Assert.Contains("3 warnings", vAfter.StopReason, StringComparison.Ordinal);
    }

    /// <summary>
    /// When the chain has finished but requirements are still open, then the run is ready for acceptance but
    /// cannot be marked complete, and the view counts what is open (REQ-FN-066).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-066 a run with a requirement open cannot be marked complete")]
    public async Task RunWithOpenRequirementsCannotBeCompleted()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        var vReady = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        var vError = await Assert.ThrowsAsync<InvalidOperationException>(() => vHarness.Actions.MarkCompleteAsync(vStarted.AutoRunId));

        Assert.Equal(new AutoRunOpenCounts(3, 1, 1, 1), vReady!.Open);
        Assert.False(vReady.CanBeCompleted);
        Assert.Contains("3 requirements are still open", vError.Message, StringComparison.Ordinal);
        Assert.Equal(AutoRunState.ReadyForAcceptance, (await vHarness.Actions.GetAsync(vStarted.AutoRunId))!.State);
    }

    /// <summary>When every requirement is verified, then the run that is ready for acceptance can be marked complete (REQ-FN-066).</summary>
    [Fact(DisplayName = "REQ-FN-066 a run with nothing open can be marked complete after acceptance")]
    public async Task RunWithNothingOpenCanBeCompleted()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vAllVerified = OpenChecklist
            .Replace("Not Started", "Verified", StringComparison.Ordinal)
            .Replace("Implemented", "Verified", StringComparison.Ordinal)
            .Replace("Blocked", "Done (pre-existing)", StringComparison.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(objFolder, "docs", "App-Checklist.md"), vAllVerified);

        var vReady = await vHarness.Actions.GetAsync(vStarted.AutoRunId);
        var vDone = await vHarness.Actions.MarkCompleteAsync(vStarted.AutoRunId);

        Assert.True(vReady!.CanBeCompleted);
        Assert.Equal(0, vReady.Open.Total);
        Assert.Equal(AutoRunState.Complete, vDone!.State);
        Assert.Contains(vDone.Events, aEvent => aEvent.Kind == AutoRunEventKind.Completed);
    }

    /// <summary>When a run has not reached the acceptance test, then it cannot be marked complete even with nothing open (REQ-FN-066).</summary>
    [Fact(DisplayName = "REQ-FN-066 a run that has not finished its chain cannot be marked complete")]
    public async Task RunMidChainCannotBeCompleted()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        await Assert.ThrowsAsync<InvalidOperationException>(() => vHarness.Actions.MarkCompleteAsync(vStarted.AutoRunId));

        Assert.Equal(AutoRunState.Running, (await vHarness.Actions.GetAsync(vStarted.AutoRunId))!.State);
    }

    /// <summary>When carrying a run on with requirements open, then it never becomes Complete by itself (REQ-FN-066).</summary>
    [Fact(DisplayName = "REQ-FN-066 carrying a run on never marks it complete by itself")]
    public async Task AdvanceNeverMarksARunComplete()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vAgain = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.NotEqual(AutoRunState.Complete, vAgain!.State);
    }

    /// <summary>
    /// When the owner pauses a run and resumes it, then it carries on from the process and step it was on
    /// and does not start over (REQ-FN-061; the owner's own control, mockup Pause and Stop).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-061 a paused run carries on from the same step when resumed")]
    public async Task PausedRunCarriesOnFromTheSameStep()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        vHarness.Executor.Script = (aProcess, aStep, _, _) =>
        {
            if (aProcess == "split-brd" && aStep.StepCode == "s2")
            {
                vHarness.Actions.PauseAsync(vStarted.AutoRunId).GetAwaiter().GetResult();
            }

            return Task.FromResult(new ProcessStepOutcome(true, "Done"));
        };

        var vPaused = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vCallsAtPause = vHarness.Executor.Calls.Count;
        var vIdle = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vCallsWhileIdle = vHarness.Executor.Calls.Count;
        var vResumed = await vHarness.Actions.ResumeAsync(vStarted.AutoRunId);
        var vEnd = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Paused, vPaused!.State);
        Assert.Equal(vCallsAtPause, vCallsWhileIdle);
        Assert.Equal(AutoRunState.Paused, vIdle!.State);
        Assert.Equal(AutoRunState.Running, vResumed!.State);
        Assert.Equal(2, vHarness.Launcher.Launched.Count);
        Assert.Equal(AutoRunState.ReadyForAcceptance, vEnd!.State);
        Assert.Equal(1, vHarness.Executor.Calls.Count(aCall => aCall == "split-brd:s2"));
        Assert.Equal(1, vHarness.Executor.Calls.Count(aCall => aCall == "split-brd:s1"));
    }

    /// <summary>When the owner stops a run, then it ends stopped with the reason, its process run is ended and nothing more runs (REQ-FN-061).</summary>
    [Fact(DisplayName = "REQ-FN-061 stopping a run ends it and its process run and runs nothing more")]
    public async Task StoppedRunEndsAndRunsNothingMore()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);
        vHarness.Executor.Script = (aProcess, aStep, _, _) =>
        {
            if (aProcess == "day1-greenfield" && aStep.StepCode == "s1")
            {
                vHarness.Actions.StopAsync(vStarted.AutoRunId).GetAwaiter().GetResult();
            }

            return Task.FromResult(new ProcessStepOutcome(true, "Done"));
        };

        var vAfter = await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);
        var vCalls = vHarness.Executor.Calls.Count;
        await vHarness.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.Stopped, vAfter!.State);
        Assert.Equal("You stopped the run.", vAfter.StopReason);
        Assert.Equal(vCalls, vHarness.Executor.Calls.Count);
        var vRun = await vHarness.Runs.GetAsync(vAfter.Chain[0].ProcessRunId!.Value);
        Assert.Equal(ProcessRunState.Stopped, vRun!.State);
    }

    /// <summary>
    /// When Chatur is closed and opened again mid-run (a new action object over the same database), then the
    /// run carries on from the saved process and step and does not redo the earlier ones (REQ-FN-061).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-061 a run carries on after a restart from where it was")]
    public async Task RunCarriesOnAfterARestart()
    {
        var vFirst = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vFirst.Actions.StartAsync(vProject, Brief);
        vFirst.Executor.Script = (aProcess, aStep, _, _) =>
        {
            if (aProcess == "mockups" && aStep.StepCode == "s2")
            {
                vFirst.Actions.PauseAsync(vStarted.AutoRunId).GetAwaiter().GetResult();
            }

            return Task.FromResult(new ProcessStepOutcome(true, "Done"));
        };
        await vFirst.Actions.AdvanceAsync(vStarted.AutoRunId);
        await vFirst.Actions.ResumeAsync(vStarted.AutoRunId);

        var vSecond = new AutoRunHarness(CreateFactory());
        var vEnd = await vSecond.Actions.AdvanceAsync(vStarted.AutoRunId);

        Assert.Equal(AutoRunState.ReadyForAcceptance, vEnd!.State);
        Assert.DoesNotContain(vSecond.Executor.Calls, aCall => aCall.StartsWith("day1-greenfield:", StringComparison.Ordinal));
        Assert.DoesNotContain("mockups:s1", vSecond.Executor.Calls);
        Assert.Contains("mockups:s3", vSecond.Executor.Calls);
    }

    /// <summary>When a run is going, then the view names the agent, the process, the step and the requirement furthest from verified (REQ-FN-061).</summary>
    [Fact(DisplayName = "REQ-FN-061 a run going shows the agent, process and the weakest open requirement")]
    public async Task GoingRunShowsWhatIsHappeningNow()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vProject = CreateProject("App", objFolder);
        var vStarted = await vHarness.Actions.StartAsync(vProject, Brief);

        var vNow = vStarted.Now!;

        Assert.Equal("Analyst", vNow.Agent);
        Assert.Equal("day1-greenfield", vNow.ProcessCode);
        Assert.Equal(1, vNow.ProcessPosition);
        Assert.Equal(8, vNow.ProcessCount);
        Assert.Equal(0, vNow.StepPosition);
        Assert.Equal(string.Empty, vNow.StepWording);
        Assert.Equal("REQ-FN-003", vNow.RequirementId);
        Assert.Equal("running", vStarted.StateText);
    }

    /// <summary>When a run stopped on the queue is resumed, then the queue and the run both work from the weakest requirement of the checklist (REQ-FN-070).</summary>
    [Fact(DisplayName = "REQ-FN-070 a resumed run continues from the open requirement furthest from verified")]
    public async Task ResumedRunWorksFromTheWeakestRequirement()
    {
        var vHarness = new AutoRunHarness(CreateFactory());
        var vFactory = CreateFactory();
        var vProject = CreateProject("App", objFolder);
        var vQueue = new Chatur.Core.RunQueue.RunQueueActions(
            vFactory, vHarness.Actions, new Chatur.Core.ProcessRuns.ProjectFolders(vFactory),
            new Chatur.Core.Platform.SystemClock(), Microsoft.Extensions.Logging.Abstractions.NullLogger<Chatur.Core.RunQueue.RunQueueActions>.Instance);
        await vQueue.QueueAsync(vProject, Brief);
        var vRow = (await vQueue.ListAsync()).Items[0];
        await vQueue.PauseAsync(vRow.QueueItemId);

        var vResumed = await vQueue.ResumeAsync(vRow.QueueItemId);
        var vRun = await vHarness.Actions.GetAsync(vResumed!.AutoRunId!.Value);

        // The fixture holds Not Started 0%, Implemented 75%, Blocked 25%, Verified: Blocked is furthest from verified.
        Assert.Equal("REQ-FN-003", vResumed.ResumeFocus);
        Assert.Equal("REQ-FN-003", vRun!.Now!.RequirementId);
        Assert.Contains(vRun.Events, aEvent => aEvent.Kind == AutoRunEventKind.Resumed && aEvent.Message.Contains("REQ-FN-003", StringComparison.Ordinal));
    }

    /// <summary>When the planned chain is asked for before any run, then the eight processes come back in order with their agents (REQ-UI-055).</summary>
    [Fact(DisplayName = "REQ-UI-055 the planned chain lists the eight processes and their agents with no run")]
    public void PlannedChainNeedsNoRun()
    {
        var vChain = new AutoRunHarness(CreateFactory()).Actions.PlannedChain();

        Assert.Equal(AutoRunChain.Processes, vChain.Select(aStep => aStep.ProcessCode));
        Assert.Equal(Enumerable.Range(1, 8), vChain.Select(aStep => aStep.Position));
        Assert.All(vChain, aStep => Assert.False(string.IsNullOrEmpty(aStep.Agent)));
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(objFolder))
        {
            Directory.Delete(objFolder, true);
        }
    }
}
