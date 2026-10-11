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
            objOpenCode = aCode;
        }
        catch (Exception aEx)
        {
            ToastService.Error(aEx.Message, "Could not read the process");
        }
    }

    /// <summary>Reads everything again, so reworded steps show without a restart (REQ-UI-046).</summary>
    private Task ReloadAsync() => LoadAsync();

    /// <summary>Starts a process on the selected project and opens its run.</summary>
    /// <param name="aCode">The process to start.</param>
    private async Task StartAsync(string aCode)
    {
        if (objProject is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            var vStarted = await ProcessActionsService.StartAsync(objProject.ProjectId, aCode);
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
