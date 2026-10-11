namespace Chatur.Core.Actions;

/// <summary>
/// The run queue (page Run queue, <c>/auto/queue</c>; BRD-126 to BRD-130): end-to-end runs waiting
/// their turn, taken one at a time through <see cref="IAutoRunActions"/>. Every fact is in the
/// database, so the queue survives Chatur being closed.
/// </summary>
public interface IRunQueueActions
{
    /// <summary>
    /// The queue as it stands, with each row's state read from its end-to-end run, and the next waiting
    /// run started when none is going (REQ-FN-067, REQ-UI-057).
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<RunQueueView> ListAsync(CancellationToken aCt = default);

    /// <summary>
    /// Puts a run at the end of the queue; it is listed with its project and its position (REQ-FN-067).
    /// The brief is checked as <see cref="IAutoRunActions.StartAsync"/> checks it. A project that is
    /// already queued is warned about, not refused.
    /// </summary>
    /// <param name="aProjectId">The project to run on.</param>
    /// <param name="aBrief">What should be true when the run finishes.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="ArgumentException">The project is not known, or the brief is too short or longer than <see cref="RunQueueBrief.MaxLength"/>.</exception>
    Task<RunQueueAdded> QueueAsync(int aProjectId, string aBrief, CancellationToken aCt = default);

    /// <summary>Pauses a going run at the end of its step (REQ-FN-068). A row it does not apply to comes back unchanged.</summary>
    /// <param name="aQueueItemId">The row to pause.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The row afterwards, or <see langword="null"/> when there is no such row.</returns>
    Task<RunQueueItem?> PauseAsync(int aQueueItemId, CancellationToken aCt = default);

    /// <summary>
    /// Carries a paused run on from the step it was paused on (REQ-FN-068), taking first the open
    /// requirement furthest from verified (REQ-FN-070). A row it does not apply to comes back unchanged.
    /// </summary>
    /// <param name="aQueueItemId">The row to resume.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>The row afterwards, or <see langword="null"/> when there is no such row.</returns>
    Task<RunQueueItem?> ResumeAsync(int aQueueItemId, CancellationToken aCt = default);

    /// <summary>
    /// Takes a waiting, paused or lost run out of the queue; it is never started (REQ-FN-069). A going
    /// run must be paused or stopped first, and a finished run is kept for its report.
    /// </summary>
    /// <param name="aQueueItemId">The row to remove.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns><see langword="true"/> when the row left the queue.</returns>
    Task<bool> RemoveAsync(int aQueueItemId, CancellationToken aCt = default);

    /// <summary>
    /// The open requirement of the row's project furthest from verified: blocked first, then partial,
    /// then untouched (REQ-FN-070).
    /// </summary>
    /// <param name="aQueueItemId">The row whose project to read.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The requirement id, or <see langword="null"/> when none is open or the row is unknown.</returns>
    Task<string?> NextRequirementAsync(int aQueueItemId, CancellationToken aCt = default);
}
