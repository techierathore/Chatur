using Chatur.Core.Actions;
using ChaturUI.Components.Estimates;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// End-to-end run (mockups/auto-run.html). Starts the whole chain from a brief, and while a run is going
/// shows what is happening now — the agent working, the process, the step and the requirement
/// (REQ-UI-056) — and which role each process starts as it is reached (REQ-UI-055). The run itself is
/// carried by <see cref="IAutoRunActions"/>; the page starts it, re-reads it every two seconds and relays
/// Pause, Resume and Stop.
/// </summary>
public partial class AutoRun : IDisposable
{
    /// <summary>The licence's switched-off features: a link to the board is not offered when the board is off (REQ-FN-075).</summary>
    [Microsoft.AspNetCore.Components.Inject]
    private Chatur.Core.AppState LicenceState { get; set; } = default!;

    private static readonly TimeSpan PollEvery = TimeSpan.FromSeconds(2);

    private readonly CancellationTokenSource objCts = new();

    private IReadOnlyList<ProjectSummary> objProjects = [];
    private string objProjectValue = string.Empty;
    private string objBrief = string.Empty;
    private string? objBriefError;
    private AutoRunView? objRun;
    private string? objError;
    private bool objIsLoading = true;
    private bool objIsBusy;
    private bool objIsStopDialogOpen;
    private PhaseEstimate? objEstimate;
    private int? objEstimateProjectId;
    private KeptEstimate? objKept;
    private int? objKeptRunId;
    private string? objEstimateError;
    private string objBriefAtStart = string.Empty;

    private int? objProjectId => int.TryParse(objProjectValue, out var vId) ? vId : null;

    /// <summary>Whether the run is going: running, waiting out a usage limit or paused.</summary>
    private bool objIsGoing => objRun?.State is AutoRunState.Running or AutoRunState.WaitingForLimit or AutoRunState.Paused;

    /// <summary>The run's chain, or, with no run, the planned chain as eight processes still to come.</summary>
    private IReadOnlyList<AutoRunProcessView> ChainRows =>
        objRun?.Chain
        ?? AutoRuns.PlannedChain()
            .Select(aStep => new AutoRunProcessView(aStep.Position, aStep.ProcessCode, aStep.Agent, AutoRunProcessState.ToCome, null, 0))
            .ToList();

    private bool IsBriefFilled => (objBrief ?? string.Empty).Trim().Length >= AutoRunBrief.MinLength;

    /// <summary>A fresh estimate shows for a brief the owner is about to start; never while a run is going, and never for the brief already started.</summary>
    private bool ShowFresh => IsBriefFilled && !objIsGoing && (objRun is null || objBriefAtStart != objBrief.Trim());

    private PhaseEstimate? ShownEstimate => ShowFresh ? objEstimate : objKept?.Estimate;

    private string EstimateTitle => ShowFresh ? "Before the run starts — what it should cost" : "What this run was started with";

    /// <summary>The run's output tokens and time so far (REQ-UI-087), as measured: never divided by or added to the estimate.</summary>
    private string ActualSoFarText =>
        $"{(objRun?.TokensOutSoFar ?? 0).ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} tokens out · {Elapsed}";

    /// <summary>The build estimate the run was started with, low · middle · high, beside the actual; said plainly when none was kept.</summary>
    private string EstimateSoFarText =>
        objKept is { Estimate.NoChecklist: false } vKept && vKept.Estimate.Total(EstimateSteps.Build) is { TokensOut: { } vTokens, DurationS: { } vTime }
            ? $"build so far — estimate {EstimateLines.TokensText(vTokens)} and {EstimateLines.DurationText(vTime)}"
            : "no estimate figure was kept for this run";

    private AutoRunEvent? objLimitEvent => objRun?.Events.LastOrDefault(aEvent => aEvent.Kind == AutoRunEventKind.LimitWait);

    private AutoRunEvent? objLimitResumed =>
        objLimitEvent is { } vWait
            ? objRun?.Events.LastOrDefault(aEvent => aEvent.Kind == AutoRunEventKind.LimitResumed && aEvent.AtUtc >= vWait.AtUtc)
            : null;

    private IReadOnlyList<AutoRunEvent> objRetryEvents =>
        objRun?.Events.Where(aEvent => aEvent.Kind == AutoRunEventKind.Retry).ToList() ?? [];

    private BadgeVariant StateVariant => objRun?.State switch
    {
        AutoRunState.Stopped => BadgeVariant.Danger,
        AutoRunState.ReadyForAcceptance or AutoRunState.Complete => BadgeVariant.Success,
        AutoRunState.Paused => BadgeVariant.Secondary,
        _ => BadgeVariant.Warning
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

            return vSpan.TotalHours >= 1
                ? $"{(int)vSpan.TotalHours} h {vSpan.Minutes} m"
                : vSpan.TotalMinutes >= 1 ? $"{(int)vSpan.TotalMinutes} m {vSpan.Seconds} s" : $"{(int)vSpan.TotalSeconds} s";
        }
    }

    private string StartedText
    {
        get
        {
            var vLocal = objRun?.StartedUtc.ToLocalTime() ?? default;
            return vLocal.Date == Clock.UtcNow.ToLocalTime().Date ? $"{vLocal:HH:mm}, today" : $"{vLocal:d MMM, HH:mm}";
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            objProjects = await ProjectActions.ScanAsync(objCts.Token);
            var vSelected = await ProjectActions.SelectedProjectAsync(objCts.Token);
            var vChosen = vSelected ?? objProjects.FirstOrDefault();
            if (vChosen is not null)
            {
                objProjectValue = vChosen.ProjectId.ToString();
                await LoadRunAsync(vChosen.ProjectId);
                await LoadKeptAsync();
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception aEx) when (aEx is InvalidOperationException or NotImplementedException)
        {
            objError = aEx.Message;
        }
        finally
        {
            objIsLoading = false;
        }

        _ = PollAsync();
    }

    /// <summary>Takes the project's going run, or its latest one when it is waiting for the owner's acceptance test.</summary>
    private async Task LoadRunAsync(int aProjectId)
    {
        objRun = await AutoRuns.CurrentAsync(aProjectId, objCts.Token);
        if (objRun is not null)
        {
            return;
        }

        var vLatest = (await AutoRuns.ListAsync(aProjectId, objCts.Token)).FirstOrDefault();
        if (vLatest is { State: AutoRunState.ReadyForAcceptance })
        {
            objRun = await AutoRuns.GetAsync(vLatest.AutoRunId, objCts.Token);
        }
    }

    /// <summary>Re-reads the run every two seconds while one is going, so the agent, the step and the bar move as the chain does.</summary>
    private async Task PollAsync()
    {
        using var vTimer = new PeriodicTimer(PollEvery);
        try
        {
            while (await vTimer.WaitForNextTickAsync(objCts.Token))
            {
                if (objRun is null || objIsBusy)
                {
                    continue;
                }

                var vNow = await AutoRuns.GetAsync(objRun.AutoRunId, objCts.Token);
                if (vNow is not null)
                {
                    objRun = vNow;
                    await LoadKeptAsync();
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // The tab was closed; the run carries on without it.
        }
        catch (Exception aEx) when (aEx is InvalidOperationException)
        {
            objError = aEx.Message;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task OnProjectChangedAsync(string aValue)
    {
        objProjectValue = aValue;
        objRun = null;
        objEstimate = null;
        objEstimateProjectId = null;
        objKept = null;
        objKeptRunId = null;
        objEstimateError = null;
        objBriefAtStart = string.Empty;
        if (objProjectId is { } vId)
        {
            try
            {
                await LoadRunAsync(vId);
                await LoadKeptAsync();
            }
            catch (Exception aEx) when (aEx is InvalidOperationException)
            {
                objError = aEx.Message;
            }

            await LoadFreshEstimateAsync();
        }
    }

    private async Task OnBriefChangedAsync(string? aValue)
    {
        objBrief = aValue ?? string.Empty;
        await LoadFreshEstimateAsync();
    }

    /// <summary>Works out the estimate for the project once its brief is filled in; the estimate depends on the project, so typing on does not ask again.</summary>
    private async Task LoadFreshEstimateAsync()
    {
        if (!IsBriefFilled || objProjectId is not { } vProjectId || (objEstimateProjectId == vProjectId && objEstimate is not null))
        {
            return;
        }

        try
        {
            objEstimate = await EstimateActions.EstimateAsync(vProjectId, objCts.Token);
            objEstimateProjectId = vProjectId;
            objEstimateError = null;
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            objEstimate = null;
            objEstimateProjectId = null;
            objEstimateError = aEx.Message;
        }
    }

    /// <summary>Reads the estimate the shown run was started with; reads again while a run that has none, or none with rows, goes on (it makes one at its build step).</summary>
    private async Task LoadKeptAsync()
    {
        if (objRun is null)
        {
            objKept = null;
            objKeptRunId = null;
            return;
        }

        var vRunId = objRun.AutoRunId;
        if (objKeptRunId == vRunId && (objKept is { Estimate.NoChecklist: false } || !objIsGoing))
        {
            return;
        }

        try
        {
            objKept = await EstimateActions.KeptAsync(EstimateRunKinds.Auto, vRunId, objCts.Token);
            objKeptRunId = vRunId;
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            objEstimateError = aEx.Message;
        }
    }

    /// <summary>Keeps the estimate the owner was shown with the run that just started. A failure is shown and never undoes the start.</summary>
    private async Task KeepStartEstimateAsync(int aProjectId, AutoRunView aRun)
    {
        try
        {
            var vEstimate = objEstimateProjectId == aProjectId && objEstimate is not null
                ? objEstimate
                : await EstimateActions.EstimateAsync(aProjectId, objCts.Token);
            if (vEstimate is not null)
            {
                objKept = await EstimateActions.ConfirmAsync(aProjectId, vEstimate, EstimateRunKinds.Auto, aRun.AutoRunId, objCts.Token);
                objKeptRunId = aRun.AutoRunId;
            }
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            ToastService.Error(aEx.Message, "The run started but its estimate was not kept");
        }
    }

    private static IReadOnlyList<ModelLine> ModelLinesOf(PhaseEstimate aEstimate) =>
        aEstimate.Totals.Select(aTotal => new ModelLine(KeyOf(aTotal.Step), LabelOf(aTotal.Step), ModelText(aTotal))).ToList();

    private static string KeyOf(string aStep) => aStep switch
    {
        EstimateSteps.Build => "build",
        EstimateSteps.Verify => "verify",
        EstimateSteps.Fix => "fix",
        _ => "triage"
    };

    private static string LabelOf(string aStep) => aStep switch
    {
        EstimateSteps.Build => "Build",
        EstimateSteps.Verify => "Machine QA: verify",
        EstimateSteps.Fix => "Machine QA: fix",
        _ => "After handoff (triage)"
    };

    private static string ModelText(StepTotal aTotal)
    {
        var vParts = new List<string>();
        if (aTotal.Model is not null)
        {
            vParts.Add(aTotal.Model);
        }

        if (aTotal.Tier is { } vTier)
        {
            vParts.Add($"tier {vTier}");
        }

        return vParts.Count == 0 ? "routing names no model yet" : string.Join(" · ", vParts);
    }

    /// <summary>One step's model and tier, as the small list beside the estimate shows it.</summary>
    private sealed record ModelLine(string Key, string Label, string Text);

    private async Task StartAsync()
    {
        objBriefError = null;
        if (objProjectId is not { } vProjectId)
        {
            objBriefError = "Choose the project the run works on.";
            return;
        }

        var vBrief = (objBrief ?? string.Empty).Trim();
        if (vBrief.Length < AutoRunBrief.MinLength)
        {
            objBriefError = "Say what should be true when the run finishes.";
            return;
        }

        objIsBusy = true;
        try
        {
            objError = null;
            objRun = await AutoRuns.StartAsync(vProjectId, vBrief, objCts.Token);
            objBriefAtStart = vBrief;
            await KeepStartEstimateAsync(vProjectId, objRun);
        }
        catch (Exception aEx) when (aEx is ArgumentException or InvalidOperationException)
        {
            // The brief stays filled; the reason is shown where the rule is.
            objBriefError = aEx.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private async Task PauseAsync() => await ChangeAsync(aId => AutoRuns.PauseAsync(aId, objCts.Token), "pause");

    private async Task ResumeAsync() => await ChangeAsync(aId => AutoRuns.ResumeAsync(aId, objCts.Token), "resume");

    private async Task ChangeAsync(Func<int, Task<AutoRunView?>> aAct, string aVerb)
    {
        if (objRun is null)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            objRun = await aAct(objRun.AutoRunId) ?? objRun;
        }
        catch (Exception aEx) when (aEx is InvalidOperationException)
        {
            ToastService.Error(aEx.Message, $"Could not {aVerb} the run");
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private void AskToStop() => objIsStopDialogOpen = true;

    private void CancelStop() => objIsStopDialogOpen = false;

    private void OnStopDialogOpenChanged(bool aOpen) => objIsStopDialogOpen = aOpen;

    private async Task ConfirmStopAsync()
    {
        objIsStopDialogOpen = false;
        await ChangeAsync(aId => AutoRuns.StopAsync(aId, objCts.Token), "stop");
    }

    private string ProjectNameOf(string aValue) =>
        objProjects.FirstOrDefault(aProject => aProject.ProjectId.ToString() == aValue)?.Name ?? "Choose a project";

    private static string StepText(AutoRunNow aNow) =>
        aNow.StepPosition <= 0 || string.IsNullOrWhiteSpace(aNow.StepWording) ? "reading the checklist" : aNow.StepWording;

    private static string RowClass(AutoRunProcessView aProcess) =>
        aProcess.State == AutoRunProcessState.Running ? "border-t bg-accent" : "border-t";

    private static BadgeVariant ProcessVariant(AutoRunProcessView aProcess) => aProcess.State switch
    {
        AutoRunProcessState.Finished => BadgeVariant.Success,
        AutoRunProcessState.Stopped => BadgeVariant.Danger,
        AutoRunProcessState.Running => BadgeVariant.Secondary,
        _ => BadgeVariant.Secondary
    };

    private static string ProcessStateText(AutoRunProcessView aProcess) => aProcess.State switch
    {
        AutoRunProcessState.Finished => "finished",
        AutoRunProcessState.Stopped => "stopped",
        AutoRunProcessState.Running => "running",
        _ => "to come"
    };

    private static string CountWord(int aCount) => aCount switch
    {
        1 => "one",
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        7 => "seven",
        8 => "eight",
        9 => "nine",
        10 => "ten",
        _ => aCount.ToString()
    };

    /// <inheritdoc />
    public void Dispose()
    {
        objCts.Cancel();
        objCts.Dispose();
    }
}
