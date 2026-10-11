using Chatur.Core.Actions;

namespace Chatur.Core.AutoRun;

/// <summary>Builds the <see cref="AutoRunView"/> the screen shows from a run's rows, its process runs and the checklists.</summary>
public sealed class AutoRunViewBuilder
{
    private readonly AutoRunStore objStore;
    private readonly IProcessRunActions objRuns;
    private readonly OpenRequirementReader objOpen;

    /// <summary>Creates the builder.</summary>
    /// <param name="aStore">Reads the run's rows.</param>
    /// <param name="aRuns">Reads the process run the chain is on.</param>
    /// <param name="aOpen">Reads the project's open requirements.</param>
    public AutoRunViewBuilder(AutoRunStore aStore, IProcessRunActions aRuns, OpenRequirementReader aOpen)
    {
        objStore = aStore;
        objRuns = aRuns;
        objOpen = aOpen;
    }

    /// <summary>The state in the owner's words.</summary>
    /// <param name="aState">The state.</param>
    public static string TextOf(AutoRunState aState) => aState switch
    {
        AutoRunState.Running => "running",
        AutoRunState.WaitingForLimit => "waiting out a usage limit",
        AutoRunState.Paused => "paused",
        AutoRunState.Stopped => "stopped",
        AutoRunState.ReadyForAcceptance => "ready for your acceptance test",
        _ => "complete"
    };

    /// <summary>Reads a run and everything the screen shows about it.</summary>
    /// <param name="aRow">The run's row.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<AutoRunView> BuildAsync(AutoRunRow aRow, CancellationToken aCt)
    {
        var vState = Enum.Parse<AutoRunState>(aRow.State);
        var vChainRows = await objStore.ProcessesAsync((int)aRow.AutoRunId, aCt).ConfigureAwait(false);
        var vOpen = await objOpen.OpenAsync((int)aRow.ProjectId, aCt).ConfigureAwait(false);
        var vCounts = OpenRequirementReader.Count(vOpen);
        var vFocus = await objOpen.FocusAsync((int)aRow.ProjectId, aCt).ConfigureAwait(false);
        var vCurrent = vChainRows.FirstOrDefault(aProcess => aProcess.Position == aRow.CurrentProcess);
        var vProcessRun = vCurrent?.ProcessRunId is { } vRunId ? await objRuns.GetAsync((int)vRunId, aCt).ConfigureAwait(false) : null;
        var vGoing = vState is AutoRunState.Running or AutoRunState.WaitingForLimit or AutoRunState.Paused;

        return new AutoRunView(
            (int)aRow.AutoRunId,
            (int)aRow.ProjectId,
            aRow.ProjectName,
            aRow.Brief,
            vState,
            TextOf(vState),
            AutoRunStore.ParseStamp(aRow.StartedUtc),
            aRow.EndedUtc is null ? null : AutoRunStore.ParseStamp(aRow.EndedUtc),
            aRow.StopReason,
            aRow.ResumeAtUtc is null ? null : AutoRunStore.ParseStamp(aRow.ResumeAtUtc),
            ProgressOf(vState, vChainRows, vProcessRun),
            vChainRows.Select(aProcess => new AutoRunProcessView(
                (int)aProcess.Position,
                aProcess.ProcessCode,
                AutoRunChain.AgentOf(aProcess.ProcessCode),
                Enum.Parse<AutoRunProcessState>(aProcess.State),
                aProcess.ProcessRunId is { } vId ? (int)vId : null,
                (int)aProcess.Retries)).ToList(),
            vGoing && vCurrent is not null ? NowOf(vCurrent, vChainRows.Count, vProcessRun, vFocus) : null,
            vCounts,
            vState == AutoRunState.ReadyForAcceptance && vCounts.Total == 0,
            await objStore.EventsAsync((int)aRow.AutoRunId, aCt).ConfigureAwait(false));
    }

    private static int ProgressOf(AutoRunState aState, IReadOnlyList<AutoRunProcessRow> aChain, ProcessRunView? aProcessRun)
    {
        if (aState is AutoRunState.ReadyForAcceptance or AutoRunState.Complete)
        {
            return 100;
        }

        if (aChain.Count == 0)
        {
            return 0;
        }

        var vFinished = aChain.Count(aProcess => aProcess.State == nameof(AutoRunProcessState.Finished));
        var vShare = aProcessRun is null || aProcessRun.Steps.Count == 0
            ? 0.0
            : aProcessRun.Steps.Count(aStep => aStep.State == ProcessStepState.Done) / (double)aProcessRun.Steps.Count;
        if (aProcessRun is { State: ProcessRunState.Finished })
        {
            vShare = 0.0;
        }

        return (int)Math.Round((vFinished + vShare) * 100.0 / aChain.Count);
    }

    private static AutoRunNow NowOf(AutoRunProcessRow aCurrent, int aCount, ProcessRunView? aProcessRun, RequirementItem? aFocus)
    {
        var vStep = aProcessRun?.CurrentPosition is { } vPosition && vPosition <= aProcessRun.Steps.Count ? aProcessRun.Steps[vPosition - 1] : null;
        var vRequirement = aFocus;
        return new AutoRunNow(
            AutoRunChain.AgentOf(aCurrent.ProcessCode),
            aCurrent.ProcessCode,
            (int)aCurrent.Position,
            aCount,
            vStep?.Position ?? 0,
            vStep?.Wording ?? string.Empty,
            vRequirement?.Id,
            vRequirement?.Title ?? string.Empty);
    }
}
