namespace Chatur.Core.AutoRun;

/// <summary>The limits an end-to-end run works under (REQ-FN-064, REQ-FN-065).</summary>
public sealed class AutoRunOptions
{
    /// <summary>How many times in a row one step is tried again before the run stops with a reason. Default 3.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>How long one step may run before it counts as stalled. Default 45 minutes, above the build and test limits.</summary>
    public TimeSpan StallTimeout { get; init; } = TimeSpan.FromMinutes(45);

    /// <summary>How long to wait out a usage limit when the model did not say. Default 30 minutes.</summary>
    public TimeSpan DefaultLimitWait { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>The longest a single wait may be, whatever the model said. Default 12 hours.</summary>
    public TimeSpan MaxLimitWait { get; init; } = TimeSpan.FromHours(12);

    /// <summary>How many usage-limit waits one run makes before it stops with a reason. Default 100.</summary>
    public int MaxLimitWaits { get; init; } = 100;

    /// <summary>The pause before a failed step is tried again; it grows with each try. Default 30 seconds.</summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>How often a wait looks to see whether the owner paused or stopped the run. Default 15 seconds.</summary>
    public TimeSpan WaitSlice { get; init; } = TimeSpan.FromSeconds(15);
}
