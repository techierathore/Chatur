using Chatur.Core.Actions;
using ChaturUI.Components.Estimates;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;
using ChaturUI.Shell;
using TrBlazeUI.Components.Stepper;

namespace ChaturUI.Pages;

/// <summary>
/// Process run (mockups/process-run.html). Shows one run: its steps with their results, the question
/// an owner stop asks, the check that stopped it and why (REQ-UI-047), and what it wrote, including
/// the verdicts that reach the checklist (REQ-UI-048). The run itself is carried by
/// <see cref="IProcessRunActions"/>; the page only reads it, carries it on and relays the owner's answer.
/// </summary>
public partial class ProcessRun : IDisposable
{
    private const string HandoffCode = "handoff-phase";

    private readonly CancellationTokenSource objCts = new();

    private ProcessRunView? objRun;
    private IReadOnlyList<ProcessRunVerdict> objVerdicts = [];
    private string? objProjectName;
    private bool objIsLoading = true;
    private bool objIsBusy;
    private bool objIsStopDialogOpen;
    private int objLoadedId;
    private int? objAnnouncedSessionId;

    private KeptEstimate? objKept;
    private EstimateAgainstActual? objHandoff;

    private int objEarlierRunId;
    private DateTime objEarlierStartedUtc;
    private ProcessRunStepView? objEarlierFailure;

    /// <summary>The run's id from the route.</summary>
    [Parameter]
    public int Id { get; set; }

    /// <summary>The step the run is waiting at, or <see langword="null"/> when it is not at an owner stop.</summary>
    private ProcessRunStepView? objWaiting =>
        objRun is { State: ProcessRunState.Waiting }
            ? objRun.Steps.FirstOrDefault(aStep => aStep.State == ProcessStepState.Waiting)
            : null;

    /// <summary>The check that failed in this run, when it did.</summary>
    private ProcessRunStepView? objFailedStep =>
        objRun?.Steps.FirstOrDefault(aStep => aStep.State == ProcessStepState.Failed);

    /// <summary>The step handed to an agent that the panel describes: the one being worked, else the last one handed (REQ-UI-070).</summary>
    private ProcessRunStepView? objHandedStep =>
        objRun?.Steps.FirstOrDefault(aStep => aStep.Work is { State: ProcessStepWorkState.Working })
        ?? objRun?.Steps.LastOrDefault(aStep => aStep.Work is not null);

    /// <summary>The line of work the run's own process counts towards, or <see langword="null"/> for a process that is not estimated (REQ-UI-087).</summary>
    private string? objEstimatedStep => objRun is null ? null : EstimateSteps.StepOf(objRun.ProcessCode);

    /// <summary>The kept estimate's total for the run's own step: Build for build-phase, its own for verify-phase and fix-issues.</summary>
    private StepTotal? objStepTotal =>
        objKept is { Estimate.NoChecklist: false } vKept && objEstimatedStep is { } vStep ? vKept.Estimate.Total(vStep) : null;

    /// <summary>The model and tier the kept estimate was made for, or <see langword="null"/> when routing named none.</summary>
    private string? SoFarModelText
    {
        get
        {
            var vParts = new List<string>();
            if (objStepTotal?.Model is { } vModel)
            {
                vParts.Add(vModel);
            }

            if (objStepTotal?.Tier is { } vTier)
            {
                vParts.Add($"tier {vTier}");
            }

            return vParts.Count == 0 ? null : string.Join(" · ", vParts);
        }
    }

    /// <summary>
    /// The run's rows against its estimate, "so far" beside the estimate and never combined: the output tokens the run's
    /// model calls have returned so far (<see cref="ProcessRunView.TokensOutSoFar"/>, counted after every call) and how
    /// long the run has been going (REQ-UI-087).
    /// </summary>
    private IReadOnlyList<SoFarLine> SoFarLines
    {
        get
        {
            if (objRun is null || objStepTotal is not { } vTotal)
            {
                return [];
            }

            var vSpan = (objRun.EndedUtc ?? Clock.UtcNow) - objRun.StartedUtc;
            var vSeconds = vSpan < TimeSpan.Zero ? 0 : (long)vSpan.TotalSeconds;
            return
            [
                new SoFarLine("tokens", "Output tokens", objRun.TokensOutSoFar.ToString("N0", System.Globalization.CultureInfo.InvariantCulture), EstimateLines.TokensText(vTotal.TokensOut)),
                new SoFarLine("time", "Time", EstimateLines.Duration(vSeconds), EstimateLines.DurationText(vTotal.DurationS))
            ];
        }
    }

    private string SoFarNote =>
        objStepTotal is { } vTotal
            ? $"The estimate for {objRun?.ProcessCode} on {vTotal.RowsCovered} row{(vTotal.RowsCovered == 1 ? "" : "s")}, made when you started the run. The two are shown side by side and never combined."
            : string.Empty;

    /// <summary>One line of the "so far, against the estimate" table.</summary>
    private sealed record SoFarLine(string Key, string Label, string SoFar, string? Estimate);

    private static BadgeVariant HandedVariant(ProcessStepWork aWork) => aWork.State switch
    {
        ProcessStepWorkState.Finished => BadgeVariant.Success,
        ProcessStepWorkState.Stopped => BadgeVariant.Danger,
        _ => BadgeVariant.Secondary
    };

    private static string HandedStateText(ProcessStepWork aWork) => aWork.State switch
    {
        ProcessStepWorkState.Finished => "finished",
        ProcessStepWorkState.Stopped => "stopped by you",
        _ => "working"
    };

    private static string ListOrNothing(IReadOnlyList<string> aItems) => aItems.Count == 0 ? "Nothing yet" : string.Join(" · ", aItems);

    private static string HandedFinalText(ProcessStepWork aWork) => aWork.State switch
    {
        ProcessStepWorkState.Working => "The final message appears here when the agent finishes; the checks that follow then read the project on disk.",
        ProcessStepWorkState.Stopped => "You stopped the agent. What it already wrote stays written.",
        _ => aWork.FinalMessage ?? "The agent finished without a closing message."
    };

    /// <summary>
    /// Tells the conversation which agent session to follow while a step is handed (REQ-UI-071), and
    /// that it can stop following once the step has ended.
    /// </summary>
    private void AnnounceHanded()
    {
        if (objRun is null)
        {
            return;
        }

        if (objHandedStep is { Work: { State: ProcessStepWorkState.Working } vWork } vStep)
        {
            objAnnouncedSessionId = vWork.SessionId;
            WorkbenchTabs.ShowHandedSession(new HandedSession(vWork.SessionId, vWork.AgentName, vWork.ModelName, $"{objRun.ProcessCode} run {objRun.RunId}, step {vStep.Position}", objRun.RunId));
        }
        else if (objAnnouncedSessionId is { } vEnded)
        {
            objAnnouncedSessionId = null;
            WorkbenchTabs.EndHandedSession(vEnded);
        }
    }

    private BadgeVariant StateVariant => objRun?.State switch
    {
        ProcessRunState.Finished => BadgeVariant.Success,
        ProcessRunState.Stopped => BadgeVariant.Danger,
        _ => BadgeVariant.Warning
    };

    private string StateIcon => objRun?.State switch
    {
        ProcessRunState.Finished => "check",
        ProcessRunState.Stopped => "circle-slash",
        ProcessRunState.Waiting => "pause",
        _ => "terminal"
    };

    private string StateText => objRun?.State switch
    {
        ProcessRunState.Finished => "finished",
        ProcessRunState.Stopped => "stopped",
        ProcessRunState.Waiting => "waiting for you",
        _ => "running"
    };

    private string Elapsed
    {
        get
        {
            if (objRun is null)
            {
                return "—";
            }

            var vSpan = (objRun.EndedUtc ?? Clock.UtcNow) - objRun.StartedUtc;
            if (vSpan < TimeSpan.Zero)
            {
                vSpan = TimeSpan.Zero;
            }

            return vSpan.TotalMinutes >= 1
                ? $"{(int)vSpan.TotalMinutes} min {vSpan.Seconds} s"
                : $"{(int)vSpan.TotalSeconds} s";
        }
    }

    private string ProgressCount
    {
        get
        {
            if (objRun is null)
            {
                return string.Empty;
            }

            var vDone = objRun.Steps.Count(aStep => aStep.State == ProcessStepState.Done);
            var vRunning = objRun.Steps.Count(aStep => aStep.State == ProcessStepState.Running);
            var vWaiting = objRun.Steps.Count(aStep => aStep.State == ProcessStepState.Waiting);
            var vFailed = objRun.Steps.Count(aStep => aStep.State == ProcessStepState.Failed);
            var vToCome = objRun.Steps.Count(aStep => aStep.State == ProcessStepState.ToCome);
            // The mockup's "5 done · 1 waiting · 1 to come": a step held for the owner is waiting, not running.
            var vActive = vWaiting > 0 ? $"{vWaiting} waiting" : $"{vRunning} running";
            var vText = $"{vDone} done · {vActive} · {vToCome} to come";
            return vFailed > 0 ? $"{vDone} done · {vFailed} failed · {vToCome} to come" : vText;
        }
    }

    /// <summary>The step whose work the output panel shows: the last step of kind
    /// <see cref="ProcessStepKind.Step"/> that has started. A check or an owner stop produces no
    /// output of its own, so while the run waits for the owner the panel still shows the step
    /// before it (the Process run mockup's "Output — step 4 · done").</summary>
    private ProcessRunStepView? objOutputStep =>
        objRun?.Steps.LastOrDefault(aStep => aStep.Kind == ProcessStepKind.Step && aStep.State != ProcessStepState.ToCome);

    private string OutputStepText => objOutputStep is null ? "no step yet" : $"step {objOutputStep.Position}";

    private BadgeVariant OutputVariant => objOutputStep is null ? BadgeVariant.Secondary : StepVariant(objOutputStep);

    private string OutputStateText => objOutputStep is null ? "to come" : StepStateText(objOutputStep);

    /// <summary>The output of the run so far: one line per step that has something to say.</summary>
    private string OutputText =>
        objRun is null
            ? string.Empty
            : string.Join(
                Environment.NewLine,
                objRun.Steps
                    .Where(aStep => aStep.State != ProcessStepState.ToCome)
                    .Select(aStep => $"{aStep.Position} · {aStep.Wording} — {ResultOf(aStep)}"));

    private string RunsText => objRun?.EndedUtc is null
        ? "Nothing yet. One record for this run, with the tool named chatur, is added when it ends."
        : $"One record for run {objRun.RunId}, with the tool named chatur — not the agent, not the model.";

    private string GatesText
    {
        get
        {
            var vChecks = objRun?.Steps.Count(aStep => aStep.Kind == ProcessStepKind.Check && aStep.State is ProcessStepState.Done or ProcessStepState.Failed) ?? 0;
            return vChecks == 0 ? "No check has run yet." : $"{vChecks} gate record(s) so far, one for each check that has run.";
        }
    }

    private string MissesText
    {
        get
        {
            var vFailed = objRun?.Steps.Count(aStep => aStep.State == ProcessStepState.Failed) ?? 0;
            return vFailed == 0 ? "None. No check has failed in this run." : $"{vFailed} miss — the check that failed on step {objFailedStep?.Position}.";
        }
    }

    /// <summary>REQ-UI-048: where the verdicts stand — written into the checklist table once the verifying run finishes.</summary>
    private string VerdictsText
    {
        get
        {
            if (objRun is null)
            {
                return string.Empty;
            }

            var vLast = objRun.Steps.LastOrDefault();
            return objRun.State switch
            {
                ProcessRunState.Finished when vLast?.Result is { Length: > 0 } vResult =>
                    $"Written into the project's checklist table. {vResult}",
                ProcessRunState.Finished => "The run finished; every requirement's verdict is in the project's checklist table.",
                ProcessRunState.Stopped => "Nothing was written. The run stopped before the verdicts were put in the checklist.",
                _ => $"Nothing yet. The verdicts go into the project's checklist table at step {objRun.Steps.Count}, once the steps before it are done."
            };
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (objLoadedId == Id)
        {
            return;
        }

        objLoadedId = Id;
        objIsLoading = true;
        objEarlierFailure = null;
        try
        {
            objRun = await Runs.GetAsync(Id, objCts.Token);
            objIsLoading = false;
            if (objRun is null)
            {
                return;
            }

            await ReadProjectNameAsync();
            await LoadEarlierFailureAsync();
            AnnounceHanded();

            // REQ-UI-087: the estimate the run was started with is read before the run is carried on, so "so far, against
            // the estimate" shows while it is still working; it is read again below, for the handoff figures at the end.
            await LoadEstimateAsync();
            if (objRun.State == ProcessRunState.Running)
            {
                await AdvanceAsync();
            }

            await LoadVerdictsAsync();
            await LoadEstimateAsync();
        }
        catch (OperationCanceledException)
        {
            // The page was left while the run was being carried on; nothing to show.
        }
        catch (Exception aEx) when (aEx is not NotImplementedException)
        {
            objIsLoading = false;
            ToastService.Error(aEx.Message, "Could not read the run");
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>Carries the run on until an owner stop, a failed check or the end, showing each step as it lands.</summary>
    private async Task AdvanceAsync()
    {
        objIsBusy = true;
        StateHasChanged();
        try
        {
            objRun = await ShowingProgressAsync(Runs.AdvanceAsync(Id, objCts.Token)) ?? objRun;
            AnnounceHanded();
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>
    /// Waits for a call that carries the run on, re-reading the run every second meanwhile so each step
    /// shows as it lands instead of all at once when the run stops.
    /// </summary>
    private async Task<ProcessRunView?> ShowingProgressAsync(Task<ProcessRunView?> aCarrying)
    {
        while (!aCarrying.IsCompleted)
        {
            await Task.WhenAny(aCarrying, Task.Delay(TimeSpan.FromSeconds(1), objCts.Token));
            if (aCarrying.IsCompleted)
            {
                break;
            }

            try
            {
                if (await Runs.GetAsync(Id, objCts.Token) is { } vNow)
                {
                    objRun = vNow;
                    AnnounceHanded();
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (Exception aEx) when (aEx is InvalidOperationException)
            {
                // A read that races the write is tried again a second later.
            }
        }

        return await aCarrying;
    }

    /// <summary>Relays the owner's answer to the stop the run is waiting at (REQ-FN-053).</summary>
    /// <param name="aAnswer">What the owner chose.</param>
    private async Task AnswerAsync(ProcessAnswer aAnswer)
    {
        objIsBusy = true;
        try
        {
            objRun = await ShowingProgressAsync(Runs.AnswerAsync(Id, aAnswer, objCts.Token)) ?? objRun;
            AnnounceHanded();
            await LoadVerdictsAsync();
            await LoadEstimateAsync();
        }
        catch (Exception aEx) when (aEx is InvalidOperationException)
        {
            ToastService.Error(aEx.Message, "Could not record the answer");
            objRun = await Runs.GetAsync(Id, objCts.Token) ?? objRun;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Reads the verdicts this run wrote; whatever status and remark come back are shown as they are.</summary>
    private async Task LoadVerdictsAsync()
    {
        try
        {
            objVerdicts = await Runs.VerdictsAsync(Id, objCts.Token);
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            objVerdicts = [];
        }
    }

    /// <summary>
    /// Reads the estimate the run kept (REQ-UI-087) and, for a handoff-phase run, the phase against its estimate
    /// (REQ-FN-113). A project or run with no estimate shows nothing.
    /// </summary>
    private async Task LoadEstimateAsync()
    {
        if (objRun is null)
        {
            return;
        }

        try
        {
            objKept = await EstimateActions.KeptAsync(EstimateRunKinds.Process, objRun.RunId, objCts.Token);
            objHandoff = objRun.ProcessCode == HandoffCode
                ? await EstimateActions.PhaseAgainstActualAsync(objRun.ProjectId, objCts.Token)
                : null;
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or IOException or NotImplementedException)
        {
            objKept = null;
            objHandoff = null;
        }
    }

    private static string EvidenceText(ProcessRunVerdictEvidence aEvidence) =>
        aEvidence.TestsRan
            ? $"{aEvidence.TestsFound} test(s) found · {aEvidence.TestsPassed} passed · {aEvidence.TestsFailed} failed"
            : "The tests did not run, or the project has none.";

    private static BadgeVariant VerdictVariant(string aStatus) => aStatus switch
    {
        "Verified" => BadgeVariant.Success,
        "Needs re-verify" => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary
    };

    private static string StepIcon(ProcessRunStepView aStep) => aStep.State switch
    {
        ProcessStepState.Done => "check",
        ProcessStepState.Running => "loader-circle",
        ProcessStepState.Waiting => "pause",
        ProcessStepState.Failed => "x",
        _ => "clock"
    };

    private void AskToStop() => objIsStopDialogOpen = true;

    private void CancelStop() => objIsStopDialogOpen = false;

    private void OnStopDialogOpenChanged(bool aOpen) => objIsStopDialogOpen = aOpen;

    private async Task ConfirmStopAsync()
    {
        objIsStopDialogOpen = false;
        objIsBusy = true;
        try
        {
            objRun = await Runs.StopAsync(Id, objCts.Token) ?? objRun;
            AnnounceHanded();
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            ToastService.Error(aEx.Message, "Could not stop the run");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private async Task ReadProjectNameAsync()
    {
        try
        {
            var vProject = await ProjectActions.SelectedProjectAsync(objCts.Token);
            objProjectName = vProject is not null && vProject.ProjectId == objRun?.ProjectId ? vProject.Name : null;
        }
        catch (NotImplementedException)
        {
            objProjectName = null;
        }
    }

    /// <summary>Finds the newest earlier run of the same process that a check stopped (the mockup's "failed on the run before this one").</summary>
    private async Task LoadEarlierFailureAsync()
    {
        if (objRun is null)
        {
            return;
        }

        try
        {
            var vRuns = await Runs.ListAsync(objRun.ProjectId, objCts.Token);
            var vEarlier = vRuns.FirstOrDefault(aRun =>
                aRun.RunId < objRun.RunId && aRun.ProcessCode == objRun.ProcessCode && aRun.State == ProcessRunState.Stopped);
            if (vEarlier is null)
            {
                return;
            }

            var vView = await Runs.GetAsync(vEarlier.RunId, objCts.Token);
            var vFailed = vView?.Steps.FirstOrDefault(aStep => aStep.State == ProcessStepState.Failed);
            if (vFailed is not null)
            {
                objEarlierRunId = vEarlier.RunId;
                objEarlierStartedUtc = vEarlier.StartedUtc;
                objEarlierFailure = vFailed;
            }
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            objEarlierFailure = null;
        }
    }

    private void OpenEarlierRun() => WorkbenchTabs.OpenProcessRun(objEarlierRunId, objRun?.ProcessCode ?? string.Empty);

    private void BackToProcesses() => WorkbenchTabs.OpenProcesses();

    private void OpenMeasurements() => SecondaryWindow.Open("/settings/measurements");

    /// <summary>Copies the whole output of the run to the clipboard.</summary>
    private async Task CopyOutputAsync()
    {
        if (await Browser.CopyTextAsync(OutputText))
        {
            ToastService.Success("Copied the output of this run.");
        }
        else
        {
            ToastService.Error("Could not copy to the clipboard.");
        }
    }

    private string FormatStarted(DateTime aUtc) => aUtc.ToLocalTime().ToString("d MMM, HH:mm");

    private static string NumberWord(int aCount) => aCount switch
    {
        1 => "One",
        2 => "Two",
        3 => "Three",
        4 => "Four",
        5 => "Five",
        6 => "Six",
        7 => "Seven",
        8 => "Eight",
        9 => "Nine",
        10 => "Ten",
        _ => aCount.ToString()
    };

    private static StepStatus StatusOf(ProcessRunStepView aStep) => aStep.State switch
    {
        ProcessStepState.Done => StepStatus.Done,
        ProcessStepState.Running => StepStatus.Running,
        ProcessStepState.Waiting => StepStatus.Waiting,
        ProcessStepState.Failed => StepStatus.Failed,
        _ => StepStatus.Pending
    };

    private static BadgeVariant StepVariant(ProcessRunStepView aStep) => aStep.State switch
    {
        ProcessStepState.Done => BadgeVariant.Success,
        ProcessStepState.Failed => BadgeVariant.Danger,
        ProcessStepState.Running or ProcessStepState.Waiting => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary
    };

    private static string StepStateText(ProcessRunStepView aStep) => aStep.State switch
    {
        ProcessStepState.Done when aStep.Kind == ProcessStepKind.Check => "check passed",
        ProcessStepState.Done => "done",
        ProcessStepState.Failed => "check failed",
        ProcessStepState.Running => "running",
        ProcessStepState.Waiting => "waiting for you",
        _ => "to come"
    };

    private static string ResultOf(ProcessRunStepView aStep) => aStep.State switch
    {
        ProcessStepState.ToCome => "Not started",
        ProcessStepState.Running => aStep.Result ?? "Running",
        ProcessStepState.Waiting => "Waiting for you — the question is below",
        ProcessStepState.Failed => $"Check failed — {aStep.Result ?? "no reason recorded"}",
        _ => aStep.Result ?? "Done"
    };

    /// <inheritdoc />
    public void Dispose()
    {
        // Leaving or closing the Process run tab ends the view that showed the step: the conversation returns to the owner's session (REQ-UI-071).
        if (objAnnouncedSessionId is { } vShown)
        {
            objAnnouncedSessionId = null;
            WorkbenchTabs.EndHandedSession(vShown);
        }

        objCts.Cancel();
        objCts.Dispose();
    }
}
