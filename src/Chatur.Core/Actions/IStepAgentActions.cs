namespace Chatur.Core.Actions;

/// <summary>
/// Hands a work step of a process run to the agent that owns the process and keeps what the agent did
/// (REQ-FN-084..087, REQ-FN-090). It runs the agent through the existing agent path (<see cref="IAgentActions"/>, the
/// guard pipeline and routing), so rights, the source-control refusal and the routed tier apply unchanged.
/// </summary>
public interface IStepAgentActions
{
    /// <summary>
    /// Starts a session with the process's agent, gives it the step's wording, the run and the project, and waits until the
    /// agent finishes or is stopped (REQ-FN-084, REQ-FN-087). A usage limit raised while the call is made for an unattended
    /// run (<c>ModelCallScope.ForRun</c>) leaves the step handed and propagates as <c>ModelUsageLimitException</c>.
    /// </summary>
    /// <param name="aRunId">The process run.</param>
    /// <param name="aPosition">The 1-based step position.</param>
    /// <param name="aCt">A token that cancels the wait; the agent's session is ended as well.</param>
    /// <returns>What the agent did.</returns>
    Task<ProcessStepWork> HandAsync(int aRunId, int aPosition, CancellationToken aCt = default);

    /// <summary>What the agent has done so far on a step, or <see langword="null"/> when the step was never handed (BRD-167).</summary>
    /// <param name="aRunId">The process run.</param>
    /// <param name="aPosition">The 1-based step position.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<ProcessStepWork?> WorkAsync(int aRunId, int aPosition, CancellationToken aCt = default);

    /// <summary>What the agent did on every step of a run that was handed out, by 1-based position (BRD-167).</summary>
    /// <param name="aRunId">The process run.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyDictionary<int, ProcessStepWork>> WorksAsync(int aRunId, CancellationToken aCt = default);

    /// <summary>
    /// Ends the session of the step a run has handed out, if one is working, and marks the step stopped by the owner (REQ-FN-090).
    /// Does nothing when no step of the run is being worked.
    /// </summary>
    /// <param name="aRunId">The process run.</param>
    /// <param name="aCt">A token that cancels the request itself.</param>
    /// <returns><see langword="true"/> when a working step was stopped.</returns>
    Task<bool> StopAsync(int aRunId, CancellationToken aCt = default);
}
