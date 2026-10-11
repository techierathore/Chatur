using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Carries out one step or check of a run (REQ-FN-052). The run engine owns order, pausing, resuming
/// and measuring; what a step actually does is this seam's one job, so a process can be given real
/// work without touching the engine.
/// </summary>
public interface IProcessStepExecutor
{
    /// <summary>Carries out a step.</summary>
    /// <param name="aRunId">The run the step belongs to; what a step records is filed under it.</param>
    /// <param name="aProjectId">The project the run works on.</param>
    /// <param name="aProcessCode">The process being run.</param>
    /// <param name="aStep">The step to carry out; never an owner stop.</param>
    /// <param name="aCt">A token that cancels the step.</param>
    /// <returns>What came of it; a check that did not hold returns a failed outcome naming the check and why.</returns>
    Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default);
}
