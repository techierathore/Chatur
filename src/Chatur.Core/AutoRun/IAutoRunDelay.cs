namespace Chatur.Core.AutoRun;

/// <summary>Waits, so a test can let a usage-limit wait pass without waiting (Coding Standards §Testability).</summary>
public interface IAutoRunDelay
{
    /// <summary>Waits for a span of time.</summary>
    /// <param name="aSpan">How long to wait.</param>
    /// <param name="aCt">A token that ends the wait early.</param>
    Task DelayAsync(TimeSpan aSpan, CancellationToken aCt);
}
