using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>What an unattended advance of a process run does differently from the owner's own (REQ-FN-061).</summary>
/// <param name="StallTimeout">How long one step may run before it counts as stalled.</param>
/// <param name="ShouldYield">Asked before every step; when it answers <see langword="true"/> the advance returns with the run where it is.</param>
/// <param name="OnOwnerStopAnswered">Told when an owner stop is answered yes by the run itself, with the stop's position and wording.</param>
public sealed record UnattendedAdvance(
    TimeSpan StallTimeout,
    Func<Task<bool>>? ShouldYield = null,
    Func<int, string, Task>? OnOwnerStopAnswered = null);

/// <summary>
/// The way the end-to-end run carries a process run on without an owner (REQ-FN-061, REQ-FN-064,
/// REQ-FN-065). It uses the same engine as <see cref="IProcessRunActions.AdvanceAsync"/>, with three
/// differences: an owner stop is answered yes and recorded, a usage limit and a stalled or crashed
/// step surface as exceptions with the run left on that step, so the caller can wait or retry and
/// call again.
/// </summary>
public interface IUnattendedProcessRuns
{
    /// <summary>Carries the run on until it ends or <see cref="UnattendedAdvance.ShouldYield"/> says to give way.</summary>
    /// <param name="aRunId">The process run.</param>
    /// <param name="aPolicy">How to behave unattended.</param>
    /// <param name="aCt">A token that cancels the run.</param>
    /// <returns>The run afterwards, or <see langword="null"/> when there is no such run.</returns>
    /// <exception cref="ModelUsageLimitException">A model reported a usage limit; the run is left on that step.</exception>
    /// <exception cref="StepStalledException">A step ran past the stall limit; the run is left on that step.</exception>
    /// <exception cref="StepCrashedException">A step threw; the run is left on that step.</exception>
    Task<ProcessRunView?> AdvanceUnattendedAsync(int aRunId, UnattendedAdvance aPolicy, CancellationToken aCt = default);
}
