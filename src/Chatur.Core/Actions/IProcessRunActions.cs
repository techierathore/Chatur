namespace Chatur.Core.Actions;

/// <summary>
/// One run of one process, watched as it goes (page Process run, <c>/processes/{id}/run</c>;
/// REQ-FN-052, REQ-FN-053, REQ-FN-054, REQ-FN-055). A run is started by
/// <see cref="IProcessActions.StartAsync"/>; this interface carries it on. Every fact about a run is
/// in the database, so a run paused at an owner stop survives Chatur being closed.
/// </summary>
public interface IProcessRunActions
{
    /// <summary>The run as it stands: the current step marked and every finished step with its result (REQ-FN-052).</summary>
    /// <param name="aRunId">The run to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The run, or <see langword="null"/> when there is no such run.</returns>
    Task<ProcessRunView?> GetAsync(int aRunId, CancellationToken aCt = default);

    /// <summary>The runs on a project, newest first.</summary>
    /// <param name="aProjectId">The project whose runs to list.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProcessRunSummary>> ListAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Carries the run on from the step it is on, until it reaches an owner stop, a failed check or the
    /// end. A run that is waiting, stopped or finished is returned unchanged, so nothing further runs
    /// until the owner answers (REQ-FN-053). Safe to call after a restart: it continues from the saved
    /// step, never from the first (REQ-FN-055). The run's record is appended when it ends (REQ-FN-054).
    /// </summary>
    /// <param name="aRunId">The run to carry on.</param>
    /// <param name="aCt">A token that cancels the run.</param>
    /// <returns>The run as it stands afterwards, or <see langword="null"/> when there is no such run.</returns>
    Task<ProcessRunView?> AdvanceAsync(int aRunId, CancellationToken aCt = default);

    /// <summary>
    /// Answers the owner stop a run is waiting at and carries the run on from the step after it. The
    /// answer <see cref="ProcessAnswer.Later"/> leaves the run waiting (REQ-FN-053, REQ-FN-055).
    /// </summary>
    /// <param name="aRunId">The waiting run.</param>
    /// <param name="aAnswer">The owner's answer.</param>
    /// <param name="aCt">A token that cancels the run.</param>
    /// <returns>The run as it stands afterwards, or <see langword="null"/> when there is no such run.</returns>
    /// <exception cref="InvalidOperationException">The run is not waiting at an owner stop.</exception>
    Task<ProcessRunView?> AnswerAsync(int aRunId, ProcessAnswer aAnswer, CancellationToken aCt = default);

    /// <summary>Ends a run that is not over, as the owner asked; its record is appended (REQ-FN-054).</summary>
    /// <param name="aRunId">The run to stop.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The run afterwards, or <see langword="null"/> when there is no such run.</returns>
    Task<ProcessRunView?> StopAsync(int aRunId, CancellationToken aCt = default);

    /// <summary>
    /// The verdicts this run wrote into the project's checklist table, in the order written; empty for
    /// a run that has not reached its write step, stopped before it or was not approved (REQ-UI-048).
    /// </summary>
    /// <param name="aRunId">The run to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ProcessRunVerdict>> VerdictsAsync(int aRunId, CancellationToken aCt = default);
}
