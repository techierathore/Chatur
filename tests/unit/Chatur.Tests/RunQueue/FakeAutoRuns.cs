using Chatur.Core.Actions;

namespace Chatur.Tests.RunQueue;

/// <summary>An in-memory <see cref="IAutoRunActions"/> that records what the queue asked of it.</summary>
public sealed class FakeAutoRuns : IAutoRunActions
{
    private readonly Dictionary<int, (int ProjectId, string Brief, AutoRunState State)> objRuns = [];
    private int objNextId = 1;

    /// <summary>The calls made, in order, e.g. <c>start 1</c>, <c>pause 1</c>, <c>resume 1</c>.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Ends a run the way a finished chain does.</summary>
    /// <param name="aAutoRunId">The run to end.</param>
    public void Finish(int aAutoRunId) => Set(aAutoRunId, AutoRunState.ReadyForAcceptance);

    /// <inheritdoc />
    public Task<AutoRunView> StartAsync(int aProjectId, string aBrief, CancellationToken aCt = default)
    {
        if (objRuns.Values.Any(aRun => aRun.ProjectId == aProjectId && aRun.State is AutoRunState.Running or AutoRunState.Paused))
        {
            throw new InvalidOperationException("A run is already going on the project.");
        }

        var vId = objNextId++;
        objRuns[vId] = (aProjectId, aBrief, AutoRunState.Running);
        Calls.Add($"start {vId}");
        return Task.FromResult(View(vId)!);
    }

    /// <inheritdoc />
    public Task<AutoRunView?> GetAsync(int aAutoRunId, CancellationToken aCt = default) => Task.FromResult(View(aAutoRunId));

    /// <inheritdoc />
    public Task<AutoRunView?> PauseAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        Calls.Add($"pause {aAutoRunId}");
        Set(aAutoRunId, AutoRunState.Paused);
        return Task.FromResult(View(aAutoRunId));
    }

    /// <inheritdoc />
    public Task<AutoRunView?> ResumeAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        Calls.Add($"resume {aAutoRunId}");
        Set(aAutoRunId, AutoRunState.Running);
        return Task.FromResult(View(aAutoRunId));
    }

    /// <inheritdoc />
    public Task<AutoRunView?> StopAsync(int aAutoRunId, CancellationToken aCt = default)
    {
        Calls.Add($"stop {aAutoRunId}");
        Set(aAutoRunId, AutoRunState.Stopped);
        return Task.FromResult(View(aAutoRunId));
    }

    /// <inheritdoc />
    public Task<AutoRunView?> AdvanceAsync(int aAutoRunId, CancellationToken aCt = default) => Task.FromResult(View(aAutoRunId));

    /// <inheritdoc />
    public IReadOnlyList<AutoRunChainStep> PlannedChain() => [];

    /// <inheritdoc />
    public Task<AutoRunView?> CurrentAsync(int aProjectId, CancellationToken aCt = default) => Task.FromResult<AutoRunView?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyList<AutoRunSummary>> ListAsync(int aProjectId, CancellationToken aCt = default) =>
        Task.FromResult<IReadOnlyList<AutoRunSummary>>([]);

    /// <inheritdoc />
    public Task<AutoRunView?> MarkCompleteAsync(int aAutoRunId, CancellationToken aCt = default) => Task.FromResult(View(aAutoRunId));

    private void Set(int aId, AutoRunState aState)
    {
        var vRun = objRuns[aId];
        objRuns[aId] = (vRun.ProjectId, vRun.Brief, aState);
    }

    private AutoRunView? View(int aId)
    {
        if (!objRuns.TryGetValue(aId, out var vRun))
        {
            return null;
        }

        var vEnded = vRun.State is AutoRunState.Running or AutoRunState.Paused ? (DateTime?)null : DateTime.UtcNow;
        return new AutoRunView(
            aId, vRun.ProjectId, "P", vRun.Brief, vRun.State, vRun.State.ToString(), DateTime.UtcNow.AddMinutes(-5), vEnded,
            null, null, 0, [], null, new AutoRunOpenCounts(0, 0, 0, 0), false, []);
    }
}
