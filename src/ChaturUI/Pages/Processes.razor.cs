using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Processes (mockups/processes.html). Lists the twelve TechieFlow commands, starts one on the selected
/// project, and shows the opened process's steps, checks and owner stops in order (REQ-UI-045) with
/// the wording the database holds now, read afresh each time (REQ-UI-046).
/// </summary>
public partial class Processes
{
    // The mockup's hooks on the tables' own header rows (TrBlazeUI HeaderRowAttributes).
    private static readonly IReadOnlyDictionary<string, object> ProcessHeadAttributes = TestId("process-head");
    private static readonly IReadOnlyDictionary<string, object> StepsHeadAttributes = TestId("steps-head");

    private static IReadOnlyDictionary<string, object> TestId(string aId) =>
        new Dictionary<string, object> { ["data-testid"] = aId };

    /// <summary>The process to open its steps for (a deep link, <c>?process=verify-phase</c>); <see langword="null"/> opens the one running, else the first.</summary>
    [Parameter]
    public string? Process { get; set; }

    /// <summary>The processes that show an estimate before they start (BRD-192).</summary>
    private static readonly string[] EstimatedProcesses = [EstimateSteps.Build, EstimateSteps.Verify, EstimateSteps.Fix];

    private string? objAppliedProcess;
    private ProjectSummary? objProject;
    private IReadOnlyList<ProcessSummary> objProcesses = Array.Empty<ProcessSummary>();
    private IReadOnlyList<ProcessStepInfo> objSteps = Array.Empty<ProcessStepInfo>();
    private ProcessDocuments? objDocuments;
    private string? objOpenCode;
    private ProcessRunSummary? objLastRun;
    private string? objError;
    private bool objIsLoading;
    private bool objIsBusy;
    private string? objEditStepCode;
    private string objEditText = string.Empty;
    private bool objIsSaving;
    private PhaseEstimate? objEstimate;
    private string? objEstimateCode;

    /// <summary>The "7 steps · 4 checks · 1 owner stop" pill over the opened process's rows.</summary>
    private string StepsSummary
    {
        get
        {
            var vSummary = objProcesses.FirstOrDefault(aProcess => aProcess.Code == objOpenCode);
            var vSteps = vSummary?.Steps ?? objSteps.Count;
            var vChecks = vSummary?.Checks ?? objSteps.Count(aStep => aStep.Kind == ProcessStepKind.Check || aStep.HasCheck);
            var vStops = vSummary?.OwnerStops ?? objSteps.Count(aStep => aStep.Kind == ProcessStepKind.OwnerStop);
            return $"{vSteps} step{(vSteps == 1 ? "" : "s")} · {vChecks} check{(vChecks == 1 ? "" : "s")} · {vStops} owner stop{(vStops == 1 ? "" : "s")}";
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // A deep link to another process while the screen is already open: show that one.
        if (Process is not null && Process != objAppliedProcess && objProcesses.Any(aProcess => aProcess.Code == Process))
        {
            objAppliedProcess = Process;
            await OpenProcessAsync(Process);
        }
    }

    /// <summary>Reads the process list, the project's documents, the last run and the opened process's rows.</summary>
    private async Task LoadAsync()
    {
        objIsLoading = true;
        objError = null;
        try
        {
            objProject = await ProjectActions.SelectedProjectAsync();
            objProcesses = await ProcessActionsService.ListAsync(objProject?.ProjectId);
            objDocuments = objProject is null ? null : await ProcessActionsService.DocumentsAsync(objProject.ProjectId);

            var vRuns = objProject is null
                ? Array.Empty<ProcessRunSummary>()
                : await ProcessRuns.ListAsync(objProject.ProjectId);
            objLastRun = vRuns.Count > 0 ? vRuns[0] : null;

            // Open the one running now, else keep the open one, else the first.
            var vWanted = Process is not null && objAppliedProcess != Process && objProcesses.Any(aProcess => aProcess.Code == Process)
                ? Process
                : null;
            objAppliedProcess = vWanted ?? objAppliedProcess;
            var vCode = vWanted ?? (objOpenCode is not null && objProcesses.Any(aProcess => aProcess.Code == objOpenCode)
                ? objOpenCode
                : (objProcesses.FirstOrDefault(aProcess => aProcess.IsRunning) ?? objProcesses.FirstOrDefault())?.Code);
            if (vCode is not null)
            {
                await OpenProcessAsync(vCode);
            }
            else
            {
                objOpenCode = null;
                objSteps = Array.Empty<ProcessStepInfo>();
            }
        }
        catch (Exception aEx)
        {
            objError = aEx.Message;
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>Shows one process's rows, read from the database now.</summary>
    /// <param name="aCode">The process to open.</param>
    private async Task OpenProcessAsync(string aCode)
    {
        try
        {
            objSteps = await ProcessActionsService.StepsAsync(aCode);
            if (objOpenCode != aCode)
            {
                objEditStepCode = null;
            }

            objOpenCode = aCode;
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not read the process");
        }
    }

    /// <summary>Opens one step's wording for editing in place (REQ-UI-075).</summary>
    /// <param name="aStep">The step to edit.</param>
    private void BeginEdit(ProcessStepInfo aStep)
    {
        objEditStepCode = aStep.StepCode;
        objEditText = aStep.Wording;
    }

    /// <summary>Leaves the wording as it was.</summary>
    private void CancelEdit() => objEditStepCode = null;

    /// <summary>Stores the new wording, then shows what the database now holds (REQ-UI-075).</summary>
    private async Task SaveEditAsync()
    {
        if (objOpenCode is null || objEditStepCode is null)
        {
            return;
        }

        objIsSaving = true;
        try
        {
            await ProcessActionsService.SaveStepWordingAsync(objOpenCode, objEditStepCode, objEditText);
            objEditStepCode = null;
            objSteps = await ProcessActionsService.StepsAsync(objOpenCode);
            ToastService.Success("The new wording is saved and the next run uses it.", "Step reworded");
        }
        catch (ArgumentException aEx)
        {
            ToastService.Error(aEx.Message, "Could not save the wording");
        }
        finally
        {
            objIsSaving = false;
        }
    }

    /// <summary>Reads everything again, so reworded steps show without a restart (REQ-UI-046).</summary>
    private Task ReloadAsync() => LoadAsync();

    /// <summary>
    /// Start on build-phase, verify-phase or fix-issues opens the estimate and waits for the owner's confirmation
    /// (REQ-UI-083); any other process starts at once, as it always did.
    /// </summary>
    /// <param name="aCode">The process to start.</param>
    private async Task StartAsync(string aCode)
    {
        // A press while an estimate is being worked out or a start is under way is ignored here rather than by disabling the
        // row's button: the processes table does not redraw its cells when only this flag changes, so a disabled Start
        // stayed disabled after Cancel until the page was reloaded (REQ-UI-083, verify 2026-11-13).
        if (objProject is null || objIsBusy)
        {
            return;
        }

        if (!EstimatedProcesses.Contains(aCode))
        {
            await StartNowAsync(aCode, null);
            return;
        }

        objIsBusy = true;
        try
        {
            var vEstimate = await EstimateActions.EstimateAsync(objProject.ProjectId);
            if (vEstimate is null)
            {
                // The project or its folder is not known to the estimator: nothing to show, so start as before.
                await StartNowAsync(aCode, null);
                return;
            }

            objEstimate = vEstimate;
            objEstimateCode = aCode;
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not work out the estimate");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>The owner pressed the panel's Start: starts the process, then keeps the estimate it was shown with.</summary>
    private async Task ConfirmEstimateAsync()
    {
        if (objEstimate is { } vEstimate && objEstimateCode is { } vCode)
        {
            await StartNowAsync(vCode, vEstimate);
        }
    }

    /// <summary>Closes the panel. Nothing is started and nothing is written.</summary>
    private void CancelEstimate()
    {
        objEstimate = null;
        objEstimateCode = null;
    }

    /// <summary>Starts a process on the selected project, keeps its estimate when one was confirmed, and opens its run.</summary>
    /// <param name="aCode">The process to start.</param>
    /// <param name="aEstimate">The estimate the owner confirmed, or <see langword="null"/> when none was shown.</param>
    private async Task StartNowAsync(string aCode, PhaseEstimate? aEstimate)
    {
        if (objProject is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            var vStarted = await ProcessActionsService.StartAsync(objProject.ProjectId, aCode);
            if (aEstimate is not null)
            {
                try
                {
                    await EstimateActions.ConfirmAsync(objProject.ProjectId, aEstimate, EstimateRunKinds.Process, vStarted.RunId);
                }
                catch (Exception aEx)
                {
                    ToastService.Error(aEx.Message, "The run started but its estimate was not kept");
                }
            }

            CancelEstimate();
            WorkbenchTabs.OpenProcessRun(vStarted.RunId, aCode);
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not start the process");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Opens the run that is going on now.</summary>
    /// <param name="aRunId">The run to watch.</param>
    /// <param name="aProcessCode">The process the run belongs to.</param>
    private void WatchRun(int aRunId, string aProcessCode) => WorkbenchTabs.OpenProcessRun(aRunId, aProcessCode);

    /// <summary>Opens the project's newest run.</summary>
    private void OpenLastRun()
    {
        if (objLastRun is { } vRun)
        {
            WatchRun(vRun.RunId, vRun.ProcessCode);
        }
    }

    /// <summary>The "when it was last run" line: a date, "never", or a dash while loading.</summary>
    /// <param name="aProcess">The process row.</param>
    private string LastRunText(ProcessSummary aProcess) =>
        aProcess.LastRunUtc is { } vWhen ? vWhen.ToLocalTime().ToString("d MMM") : objProject is null ? "—" : "never run";

    /// <summary>The word for a count of processes in the panel heading.</summary>
    /// <param name="aCount">How many processes.</param>
    private static string CountWord(int aCount) => aCount switch
    {
        12 => "Twelve",
        _ => aCount.ToString()
    };

    /// <summary>The label of the pill marking a row as a step, a check or a place it stops.</summary>
    /// <param name="aStep">The row.</param>
    private static string KindLabel(ProcessStepInfo aStep) => aStep.Kind switch
    {
        ProcessStepKind.Check => "check",
        ProcessStepKind.OwnerStop => "owner stops",
        _ => aStep.HasCheck ? "step · check" : "step"
    };

    /// <summary>The look of the kind pill.</summary>
    /// <param name="aStep">The row.</param>
    private static BadgeVariant KindVariant(ProcessStepInfo aStep) => aStep.Kind switch
    {
        ProcessStepKind.Check => BadgeVariant.Success,
        ProcessStepKind.OwnerStop => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary
    };
}
