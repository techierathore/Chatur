namespace Chatur.Core.ProcessRuns;

/// <summary>
/// A model reported a usage limit while a step was being carried out (REQ-FN-064). The run keeps its
/// place on that step, so an unattended run waits and carries on from the same step.
/// </summary>
public sealed class ModelUsageLimitException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="aMessage">What the model reported, in its own words.</param>
    /// <param name="aRetryAfter">How long the model said to wait, or <see langword="null"/> when it did not say.</param>
    public ModelUsageLimitException(string aMessage, TimeSpan? aRetryAfter = null)
        : base(aMessage)
    {
        RetryAfter = aRetryAfter;
    }

    /// <summary>How long the model said to wait, or <see langword="null"/> when it did not say.</summary>
    public TimeSpan? RetryAfter { get; }
}

/// <summary>An unattended step ran longer than the stall limit and was stopped (REQ-FN-065).</summary>
public sealed class StepStalledException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="aPosition">The step that stalled.</param>
    /// <param name="aLimit">The limit it passed.</param>
    public StepStalledException(int aPosition, TimeSpan aLimit)
        : base($"Step {aPosition} made no progress for {aLimit.TotalMinutes:0} minutes and was stopped.")
    {
    }
}

/// <summary>An unattended step threw instead of answering (REQ-FN-065). The cause is the inner exception.</summary>
public sealed class StepCrashedException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="aPosition">The step that crashed.</param>
    /// <param name="aInner">What it threw.</param>
    public StepCrashedException(int aPosition, Exception aInner)
        : base($"Step {aPosition} crashed: {aInner.Message}", aInner)
    {
    }
}
