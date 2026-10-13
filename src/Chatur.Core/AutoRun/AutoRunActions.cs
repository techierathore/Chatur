using System.Collections.Concurrent;
using System.Globalization;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.AutoRun;

/// <summary>
/// <see cref="IAutoRunActions"/>: the end-to-end run. It walks <see cref="AutoRunChain"/> through the
/// same engine the owner uses by hand (<see cref="IProcessActions"/> and <see cref="IUnattendedProcessRuns"/>),
/// waits out a usage limit, retries a stall or a crash, and ends at
/// <see cref="AutoRunState.ReadyForAcceptance"/> (REQ-FN-061 to REQ-FN-066). Owner stops inside the chain are
/// answered yes by the run itself and written into its record: the BRD leaves them open, and an unattended run
/// that waited at each would not be unattended. The one real stop is the owner's acceptance test at the end.
/// </summary>
public sealed class AutoRunActions : IAutoRunActions
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> Loops = new();

    private readonly AutoRunStore objStore;
    private readonly AutoRunViewBuilder objViews;
    private readonly IProcessActions objProcesses;
    private readonly IProcessRunActions objRuns;
    private readonly IUnattendedProcessRuns objUnattended;
    private readonly IAutoRunLauncher objLauncher;
    private readonly IAutoRunDelay objDelay;
    private readonly AutoRunOptions objOptions;
    private readonly IClock objClock;
    private readonly ILogger<AutoRunActions> objLogger;

    /// <summary>Creates the actions.</summary>
    /// <param name="aStore">The run's rows.</param>
    /// <param name="aViews">Builds what the screen shows.</param>
    /// <param name="aProcesses">Starts a process on the project.</param>
    /// <param name="aRuns">Reads and stops a process run.</param>
    /// <param name="aUnattended">Carries a process run on without an owner.</param>
    /// <param name="aLauncher">Carries a run on in the background.</param>
    /// <param name="aDelay">Waits.</param>
    /// <param name="aOptions">The limits a run works under.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aLogger">Where a failure inside Chatur is logged.</param>
    public AutoRunActions(
        AutoRunStore aStore,
        AutoRunViewBuilder aViews,
        IProcessActions aProcesses,
        IProcessRunActions aRuns,
        IUnattendedProcessRuns aUnattended,
        IAutoRunLauncher aLauncher,
        IAutoRunDelay aDelay,
        AutoRunOptions aOptions,
        IClock aClock,
        ILogger<AutoRunActions> aLogger)
    {
        objStore = aStore;
        objViews = aViews;
        objProcesses = aProcesses;
        objRuns = aRuns;
        objUnattended = aUnattended;
        objLauncher = aLauncher;
        objDelay = aDelay;
        objOptions = aOptions;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<AutoRunView> StartAsync(int aProjectId, string aBrief, CancellationToken aCt = default)
    {
        var vBrief = (aBrief ?? string.Empty).Trim();
        if (vBrief.Length < AutoRunBrief.MinLength)
        {
            throw new ArgumentException($"Say what should be true when the run finishes: a brief of at least {AutoRunBrief.MinLength} characters.", nameof(aBrief));
        }

        // The documents of a project are only known for a project that exists.
        if (await objProcesses.DocumentsAsync(aProjectId, aCt).ConfigureAwait(false) is null)
        {
            throw new ArgumentException($"Project {aProjectId} is not known.", nameof(aProjectId));
        }

        if (await objStore.GoingAsync(aProjectId, aCt).ConfigureAwait(false) is not null)
        {
            throw new InvalidOperationException("A run is already going on this project. Stop it or let it finish before starting another.");
        }

        var vId = await objStore.CreateAsync(aProjectId, vBrief, AutoRunChain.Processes, aCt).ConfigureAwait(false);
        objLauncher.Launch(vId);
        return (await GetAsync(vId, aCt).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> AdvanceAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        if (await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false) is null)
        {
            return null;
        }

        var vLoop = Loops.GetOrAdd(aAutoRunId, static _ => new SemaphoreSlim(1, 1));
        if (await vLoop.WaitAsync(TimeSpan.FromSeconds(5), aCt).ConfigureAwait(false))
        {
            try
            {
                await CarryOnAsync(aAutoRunId, aCt).ConfigureAwait(false);
                if ((await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false))?.State == nameof(AutoRunState.Stopped))
                {
                    await StopProcessRunAsync(aAutoRunId, aCt).ConfigureAwait(false);
                }
            }
            finally
            {
                vLoop.Release();
            }
        }

        return await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> GetAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        var vRow = await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        return vRow is null ? null : await objViews.BuildAsync(vRow, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadOnlyList<AutoRunChainStep> PlannedChain() =>
        AutoRunChain.Processes
            .Select((aCode, aIndex) => new AutoRunChainStep(aIndex + 1, aCode, AutoRunChain.AgentOf(aCode)))
            .ToList();

    /// <inheritdoc />
    public async Task<AutoRunView?> CurrentAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vRow = await objStore.GoingAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vRow is null)
        {
            return null;
        }

        if (vRow.State is nameof(AutoRunState.Running) or nameof(AutoRunState.WaitingForLimit))
        {
            // A run found going with nobody carrying it on (Chatur was closed and opened again, or the work
            // crashed) is picked up here, and its record says so (REQ-FN-065: the retry is recorded).
            if (objLauncher.Launch((int)vRow.AutoRunId))
            {
                var vPosition = (int)vRow.CurrentProcess;
                var vCode = vPosition >= 1 && vPosition <= AutoRunChain.Processes.Count ? AutoRunChain.Processes[vPosition - 1] : null;
                await objStore.AddEventAsync(
                    (int)vRow.AutoRunId, AutoRunEventKind.Retry, vCode is null ? null : vPosition, vCode,
                    $"Nothing was carrying this run on: Chatur was closed while it was going, or its work crashed. It was picked up again{(vCode is null ? "" : $" at {vCode}")}, on the step it had reached.",
                    null, aCt).ConfigureAwait(false);
            }
        }

        return await objViews.BuildAsync(vRow, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AutoRunSummary>> ListAsync(int aProjectId, CancellationToken aCt = default)
    {
        var vRows = await objStore.ListAsync(aProjectId, aCt).ConfigureAwait(false);
        return vRows.Select(aRow => new AutoRunSummary(
            (int)aRow.AutoRunId,
            aRow.Brief,
            Enum.Parse<AutoRunState>(aRow.State),
            AutoRunStore.ParseStamp(aRow.StartedUtc),
            aRow.EndedUtc is null ? null : AutoRunStore.ParseStamp(aRow.EndedUtc))).ToList();
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> PauseAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        var vRow = await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        if (vRow is not null && vRow.State is nameof(AutoRunState.Running) or nameof(AutoRunState.WaitingForLimit))
        {
            await objStore.SetStateAsync(aAutoRunId, AutoRunState.Paused, null, null, false, aCt).ConfigureAwait(false);
            await objStore.AddEventAsync(aAutoRunId, AutoRunEventKind.Paused, null, null, "You paused the run. It carries on from the same step when you resume it.", null, aCt).ConfigureAwait(false);
        }

        return await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> ResumeAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        var vRow = await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        if (vRow is not null && vRow.State == nameof(AutoRunState.Paused))
        {
            await objStore.SetStateAsync(aAutoRunId, AutoRunState.Running, null, null, false, aCt).ConfigureAwait(false);
            // The requirement it works from is the one furthest from verified (REQ-FN-070): the same pick the run queue stores as its resume focus.
            var vFocus = (await GetAsync(aAutoRunId, aCt).ConfigureAwait(false))?.Now?.RequirementId;
            var vFrom = vFocus is null ? string.Empty : $" It works from {vFocus}, the open requirement furthest from verified.";
            await objStore.AddEventAsync(aAutoRunId, AutoRunEventKind.Resumed, null, null, $"You resumed the run; it carried on from the step it was paused on.{vFrom}", null, aCt).ConfigureAwait(false);
            objLauncher.Launch(aAutoRunId);
        }

        return await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> StopAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        var vRow = await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        if (vRow is not null && IsGoing(vRow))
        {
            await EndStoppedAsync(aAutoRunId, "You stopped the run.", aCt).ConfigureAwait(false);
            if (Loops.GetOrAdd(aAutoRunId, static _ => new SemaphoreSlim(1, 1)).CurrentCount > 0)
            {
                // Nobody is carrying the run on, so its process run is ended here; a running loop does it as it gives way.
                await StopProcessRunAsync(aAutoRunId, aCt).ConfigureAwait(false);
            }
        }

        return await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AutoRunView?> MarkCompleteAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        var vView = await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
        if (vView is null)
        {
            return null;
        }

        if (vView.State != AutoRunState.ReadyForAcceptance)
        {
            throw new InvalidOperationException("The run is not ready for your acceptance test yet, so it cannot be marked complete.");
        }

        if (vView.Open.Total > 0)
        {
            var vPlural = vView.Open.Total == 1 ? "requirement is" : "requirements are";
            throw new InvalidOperationException($"{vView.Open.Total} {vPlural} still open on {vView.ProjectName}, so this run cannot be marked complete.");
        }

        await objStore.SetStateAsync(aAutoRunId, AutoRunState.Complete, null, null, false, aCt).ConfigureAwait(false);
        await objStore.AddEventAsync(aAutoRunId, AutoRunEventKind.Completed, null, null, "Marked complete after your acceptance test; no requirement was open.", null, aCt).ConfigureAwait(false);
        return await GetAsync(aAutoRunId, aCt).ConfigureAwait(false);
    }

    private static bool IsGoing(AutoRunRow aRow) =>
        aRow.State is nameof(AutoRunState.Running) or nameof(AutoRunState.WaitingForLimit) or nameof(AutoRunState.Paused);

    private async Task CarryOnAsync(int aAutoRunId, CancellationToken aCt)
    {
        while (await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false) is { } vRow)
        {
            if (vRow.State == nameof(AutoRunState.WaitingForLimit))
            {
                if (!await WaitOutAsync(vRow, aCt).ConfigureAwait(false))
                {
                    return;
                }

                continue;
            }

            if (vRow.State != nameof(AutoRunState.Running) || !await RunCurrentProcessAsync(vRow, aCt).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    /// <summary>Carries the process the run is on to its end; <see langword="true"/> when the run should go on to the next, <see langword="false"/> when it is over or gave way.</summary>
    private async Task<bool> RunCurrentProcessAsync(AutoRunRow aRow, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        var vChain = await objStore.ProcessesAsync(vId, aCt).ConfigureAwait(false);
        var vProcess = vChain.First(aProcess => aProcess.Position == aRow.CurrentProcess);
        var vRunId = await EnsureProcessRunAsync(aRow, vProcess, aCt).ConfigureAwait(false);

        var vFinished = await DriveProcessRunAsync(aRow, vProcess, vRunId, aCt).ConfigureAwait(false);
        if (!vFinished)
        {
            return false;
        }

        await objStore.SetProcessAsync(vId, (int)vProcess.Position, AutoRunProcessState.Finished, null, aCt).ConfigureAwait(false);
        await objStore.AddEventAsync(vId, AutoRunEventKind.ProcessFinished, (int)vProcess.Position, vProcess.ProcessCode, $"{vProcess.ProcessCode} finished.", null, aCt).ConfigureAwait(false);
        if (vProcess.Position >= vChain.Count)
        {
            await EndReadyAsync(aRow, aCt).ConfigureAwait(false);
            return false;
        }

        await objStore.SetCurrentAsync(vId, (int)vProcess.Position + 1, aCt).ConfigureAwait(false);
        return true;
    }

    private async Task<int> EnsureProcessRunAsync(AutoRunRow aRow, AutoRunProcessRow aProcess, CancellationToken aCt)
    {
        if (aProcess.ProcessRunId is { } vExisting)
        {
            return (int)vExisting;
        }

        var vStarted = await objProcesses.StartAsync((int)aRow.ProjectId, aProcess.ProcessCode, aCt).ConfigureAwait(false);
        var vAgent = AutoRunChain.AgentOf(aProcess.ProcessCode);
        await objStore.SetProcessAsync((int)aRow.AutoRunId, (int)aProcess.Position, AutoRunProcessState.Running, vStarted.RunId, aCt).ConfigureAwait(false);
        await objStore.AddEventAsync((int)aRow.AutoRunId, AutoRunEventKind.ProcessStarted, (int)aProcess.Position, aProcess.ProcessCode, $"{vAgent} started on {aProcess.ProcessCode}.", null, aCt).ConfigureAwait(false);
        return vStarted.RunId;
    }

    /// <summary>Drives one process run to its end, waiting out a limit and retrying a stall or a crash; <see langword="true"/> only when it finished.</summary>
    private async Task<bool> DriveProcessRunAsync(AutoRunRow aRow, AutoRunProcessRow aProcess, int aRunId, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        var vFailures = 0;
        while (true)
        {
            try
            {
                var vView = await objUnattended.AdvanceUnattendedAsync(aRunId, PolicyFor(aRow), aCt).ConfigureAwait(false);
                return await SettleAsync(vId, aProcess, vView, aCt).ConfigureAwait(false);
            }
            catch (ModelUsageLimitException aEx)
            {
                if (!await BeginLimitWaitAsync(aRow, aProcess, aRunId, aEx, aCt).ConfigureAwait(false))
                {
                    return false;
                }

                var vWaiting = (await objStore.RunAsync(vId, aCt).ConfigureAwait(false))!;
                if (!await WaitOutAsync(vWaiting, aCt).ConfigureAwait(false))
                {
                    return false;
                }
            }
            catch (Exception aEx) when (aEx is StepStalledException or StepCrashedException)
            {
                vFailures++;
                if (!await RetryAsync(aRow, aProcess, aRunId, aEx, vFailures, aCt).ConfigureAwait(false))
                {
                    return false;
                }
            }
        }
    }

    private UnattendedAdvance PolicyFor(AutoRunRow aRow)
    {
        var vId = (int)aRow.AutoRunId;
        return new UnattendedAdvance(
            objOptions.StallTimeout,
            async () => (await objStore.RunAsync(vId, CancellationToken.None).ConfigureAwait(false))?.State != nameof(AutoRunState.Running),
            async (aPosition, aWording) => await objStore.AddEventAsync(
                vId, AutoRunEventKind.OwnerStopAnswered, null, null, $"Step {aPosition}, \"{aWording}\", was answered yes by the run.", null, CancellationToken.None).ConfigureAwait(false));
    }

    /// <summary>What an advance came to for the chain: finished, stopped on a check, or given way to the owner.</summary>
    private async Task<bool> SettleAsync(int aAutoRunId, AutoRunProcessRow aProcess, ProcessRunView? aView, CancellationToken aCt)
    {
        if (aView is null)
        {
            await StopRunAsync(aAutoRunId, aProcess, $"{aProcess.ProcessCode} has no process run any more.", aCt).ConfigureAwait(false);
            return false;
        }

        if (aView.State == ProcessRunState.Finished)
        {
            return true;
        }

        if (aView.State == ProcessRunState.Stopped)
        {
            var vFailed = aView.Steps.FirstOrDefault(aStep => aStep.State == ProcessStepState.Failed);
            var vWhy = vFailed is null
                ? $"{aProcess.ProcessCode} was stopped."
                : $"{aProcess.ProcessCode} stopped at step {vFailed.Position}, \"{vFailed.Wording}\": {vFailed.Result}";
            await StopRunAsync(aAutoRunId, aProcess, vWhy, aCt).ConfigureAwait(false);
            return false;
        }

        // The run gave way: the owner paused or stopped it. A stopped run's process run is ended here.
        var vNow = await objStore.RunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        if (vNow?.State == nameof(AutoRunState.Stopped))
        {
            await StopProcessRunAsync(aAutoRunId, aCt).ConfigureAwait(false);
        }

        return false;
    }

    private async Task StopRunAsync(int aAutoRunId, AutoRunProcessRow aProcess, string aReason, CancellationToken aCt)
    {
        await objStore.SetProcessAsync(aAutoRunId, (int)aProcess.Position, AutoRunProcessState.Stopped, null, aCt).ConfigureAwait(false);
        await EndStoppedAsync(aAutoRunId, aReason, aCt, (int)aProcess.Position, aProcess.ProcessCode).ConfigureAwait(false);
    }

    private async Task EndStoppedAsync(int aAutoRunId, string aReason, CancellationToken aCt, int? aPosition = null, string? aProcessCode = null)
    {
        await objStore.SetStateAsync(aAutoRunId, AutoRunState.Stopped, aReason, null, true, aCt).ConfigureAwait(false);
        await objStore.AddEventAsync(aAutoRunId, AutoRunEventKind.Stopped, aPosition, aProcessCode, aReason, null, aCt).ConfigureAwait(false);
    }

    private async Task EndReadyAsync(AutoRunRow aRow, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        await objStore.SetStateAsync(vId, AutoRunState.ReadyForAcceptance, null, null, true, aCt).ConfigureAwait(false);
        var vView = await GetAsync(vId, aCt).ConfigureAwait(false);
        var vOpen = vView!.Open.Total;
        var vTail = vOpen == 0
            ? "No requirement is open."
            : $"{vOpen} requirement{(vOpen == 1 ? " is" : "s are")} still open, so it cannot be marked complete.";
        await objStore.AddEventAsync(vId, AutoRunEventKind.ReadyForAcceptance, null, null, $"The chain finished. The run is ready for your acceptance test; nothing further is run. {vTail}", null, aCt).ConfigureAwait(false);
    }

    private async Task StopProcessRunAsync(int aAutoRunId, CancellationToken aCt)
    {
        var vChain = await objStore.ProcessesAsync(aAutoRunId, aCt).ConfigureAwait(false);
        foreach (var vProcess in vChain.Where(aProcess => aProcess.State == nameof(AutoRunProcessState.Running) && aProcess.ProcessRunId is not null))
        {
            await objRuns.StopAsync((int)vProcess.ProcessRunId!.Value, aCt).ConfigureAwait(false);
            await objStore.SetProcessAsync(aAutoRunId, (int)vProcess.Position, AutoRunProcessState.Stopped, null, aCt).ConfigureAwait(false);
        }
    }

    /// <summary>Records the wait and puts the run into <see cref="AutoRunState.WaitingForLimit"/>; <see langword="false"/> when the run has waited too often and stopped instead.</summary>
    private async Task<bool> BeginLimitWaitAsync(AutoRunRow aRow, AutoRunProcessRow aProcess, int aRunId, ModelUsageLimitException aLimit, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        var vWaits = (await objStore.EventsAsync(vId, aCt).ConfigureAwait(false)).Count(aEvent => aEvent.Kind == AutoRunEventKind.LimitWait);
        if (vWaits >= objOptions.MaxLimitWaits)
        {
            await StopRunAsync(vId, aProcess, $"A model reported a usage limit {vWaits + 1} times; the run stopped waiting. {aLimit.Message}", aCt).ConfigureAwait(false);
            return false;
        }

        var vSpan = aLimit.RetryAfter ?? objOptions.DefaultLimitWait;
        vSpan = vSpan < TimeSpan.Zero ? TimeSpan.Zero : vSpan > objOptions.MaxLimitWait ? objOptions.MaxLimitWait : vSpan;
        var vUntil = objClock.UtcNow + vSpan;
        await objStore.SetStateAsync(vId, AutoRunState.WaitingForLimit, null, vUntil, false, aCt).ConfigureAwait(false);
        var vStep = (await objRuns.GetAsync(aRunId, aCt).ConfigureAwait(false))?.CurrentPosition;
        var vOn = vStep is null ? "the same step" : $"step {vStep} of {aProcess.ProcessCode}";
        var vMessage = $"A model reported a usage limit ({aLimit.Message}) on {vOn}. The run waits {Minutes(vSpan)} and carries on by itself on that step.";
        await objStore.AddEventAsync(vId, AutoRunEventKind.LimitWait, (int)aProcess.Position, aProcess.ProcessCode, vMessage, vUntil, aCt).ConfigureAwait(false);
        return true;
    }

    /// <summary>Waits until the run's resume time, watching for the owner pausing or stopping it; <see langword="true"/> when the run carries on.</summary>
    private async Task<bool> WaitOutAsync(AutoRunRow aRow, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        var vUntil = aRow.ResumeAtUtc is null ? objClock.UtcNow : AutoRunStore.ParseStamp(aRow.ResumeAtUtc);
        while (objClock.UtcNow < vUntil)
        {
            var vSlice = TimeSpan.FromTicks(Math.Min((vUntil - objClock.UtcNow).Ticks, objOptions.WaitSlice.Ticks));
            await objDelay.DelayAsync(vSlice, aCt).ConfigureAwait(false);
            var vState = (await objStore.RunAsync(vId, aCt).ConfigureAwait(false))?.State;
            if (vState != nameof(AutoRunState.WaitingForLimit))
            {
                return false;
            }
        }

        await objStore.SetStateAsync(vId, AutoRunState.Running, null, null, false, aCt).ConfigureAwait(false);
        await objStore.AddEventAsync(vId, AutoRunEventKind.LimitResumed, (int)aRow.CurrentProcess, null, "The wait is over. The run carried on by itself, on the same step.", null, aCt).ConfigureAwait(false);
        return true;
    }

    /// <summary>Records a retry and waits a little; <see langword="false"/> when the step has failed too often (the run stops with the reason) or the owner gave way.</summary>
    private async Task<bool> RetryAsync(AutoRunRow aRow, AutoRunProcessRow aProcess, int aRunId, Exception aCause, int aFailures, CancellationToken aCt)
    {
        var vId = (int)aRow.AutoRunId;
        var vStep = (await objRuns.GetAsync(aRunId, aCt).ConfigureAwait(false))?.CurrentPosition ?? 0;
        var vKind = aCause is StepStalledException ? "stalled" : "crashed";
        if (aFailures > objOptions.MaxRetries)
        {
            var vWhy = $"Step {vStep} of {aProcess.ProcessCode} {vKind} {aFailures} times in a row and the run gave up. {aCause.Message}";
            await StopRunAsync(vId, aProcess, vWhy, aCt).ConfigureAwait(false);
            return false;
        }

        await objStore.AddRetryAsync(vId, (int)aProcess.Position, aCt).ConfigureAwait(false);
        var vMessage = $"Step {vStep} of {aProcess.ProcessCode} {vKind}: {aCause.Message} Trying again (try {aFailures} of {objOptions.MaxRetries}).";
        await objStore.AddEventAsync(vId, AutoRunEventKind.Retry, (int)aProcess.Position, aProcess.ProcessCode, vMessage, null, aCt).ConfigureAwait(false);
        await objDelay.DelayAsync(TimeSpan.FromTicks(objOptions.RetryDelay.Ticks * aFailures), aCt).ConfigureAwait(false);
        return (await objStore.RunAsync(vId, aCt).ConfigureAwait(false))?.State == nameof(AutoRunState.Running);
    }

    private static string Minutes(TimeSpan aSpan) =>
        aSpan.TotalMinutes >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{Math.Round(aSpan.TotalMinutes)} minutes")
            : string.Create(CultureInfo.InvariantCulture, $"{Math.Round(aSpan.TotalSeconds)} seconds");
}
