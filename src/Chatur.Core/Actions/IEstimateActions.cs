namespace Chatur.Core.Actions;

/// <summary>
/// Estimate before, actual after (BRD-192 to BRD-209; Architecture §7 "Estimator"). What a process or a whole run
/// should cost, worked out in code from earlier run records with no model asked; written to the project's
/// <c>estimates.jsonl</c> only when the owner confirms a start or a queue entry; set beside what the runs measured.
/// </summary>
public interface IEstimateActions
{
    /// <summary>
    /// The estimate for the project's newest phase, for the model and tier routing gives each step now. Shown, not
    /// written. A project with no usable run anywhere gets an estimate whose <see cref="PhaseEstimate.NoEstimateYet"/>
    /// is true; one with no checklist yet gets one whose <see cref="PhaseEstimate.NoChecklist"/> is true. Never throws
    /// for a project that has no run records (BRD-208).
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The estimate, or <see langword="null"/> when the project or its folder is not known.</returns>
    Task<PhaseEstimate?> EstimateAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// The owner confirmed a start or a queue entry: writes the estimate as one batch to the project's
    /// <c>docs/metrics/estimates.jsonl</c> (nothing when it has no rows) and keeps it for the run, so a later tier
    /// change does not move it (BRD-194, BRD-201, BRD-205). Writes nothing to <c>runs.jsonl</c>, the checklist or
    /// PROJECT-STATUS (BRD-206). A run that already keeps an estimate with rows keeps it; one kept without rows (a
    /// brief before its checklist) is replaced.
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aEstimate">The estimate as it was shown.</param>
    /// <param name="aRunKind">One of <see cref="EstimateRunKinds"/>.</param>
    /// <param name="aRunId">The run's own id.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task<KeptEstimate> ConfirmAsync(int aProjectId, PhaseEstimate aEstimate, string aRunKind, int aRunId, CancellationToken aCt = default);

    /// <summary>
    /// Keeps an estimate already written for another run, without writing a new batch: a queued run that starts
    /// carries the estimate it was queued with (BRD-194).
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aKept">The estimate to carry.</param>
    /// <param name="aRunKind">One of <see cref="EstimateRunKinds"/>.</param>
    /// <param name="aRunId">The run's own id.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task KeepAsync(int aProjectId, KeptEstimate aKept, string aRunKind, int aRunId, CancellationToken aCt = default);

    /// <summary>The estimate a run was started or queued with, or <see langword="null"/> when it has none.</summary>
    /// <param name="aRunKind">One of <see cref="EstimateRunKinds"/>.</param>
    /// <param name="aRunId">The run's own id.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<KeptEstimate?> KeptAsync(string aRunKind, int aRunId, CancellationToken aCt = default);

    /// <summary>
    /// A written batch beside what the runs measured since (BRD-203, BRD-204): per step, the actual over the rows the
    /// estimate covered, apportioned where a run also touched other rows, and runs that named no row apart.
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aBatchTs">The batch's <c>ts</c>.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The reconciliation, or <see langword="null"/> when the batch is not in the project's <c>estimates.jsonl</c>.</returns>
    Task<EstimateAgainstActual?> AgainstActualAsync(int aProjectId, string aBatchTs, CancellationToken aCt = default);

    /// <summary>
    /// The project's newest phase against its estimate, as handoff shows it (BRD-204): its latest forecast batch, or
    /// its latest batch made after the fact when it has no forecast.
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The reconciliation, or <see langword="null"/> when the phase has no estimate.</returns>
    Task<EstimateAgainstActual?> PhaseAgainstActualAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The sister projects, each with its live run count now (BRD-197).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<SisterProject>> SistersAsync(CancellationToken aCt = default);

    /// <summary>
    /// Names a folder as a sister project (BRD-197). Refused, with the reason in the exception's message, when the
    /// folder does not exist, has no <c>docs/metrics/runs.jsonl</c>, or is already a sister project.
    /// </summary>
    /// <param name="aFolder">The folder.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <exception cref="ArgumentException">The folder is refused; the message says why, in words for the owner.</exception>
    Task<SisterProject> AddSisterAsync(string aFolder, CancellationToken aCt = default);

    /// <summary>Takes a sister project off the list. Its files are not touched.</summary>
    /// <param name="aSisterProjectId">The sister project.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task RemoveSisterAsync(int aSisterProjectId, CancellationToken aCt = default);
}
