namespace Chatur.Core.AutoRun;

/// <summary>The real <see cref="IAutoRunDelay"/>: <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.</summary>
public sealed class TaskAutoRunDelay : IAutoRunDelay
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan aSpan, CancellationToken aCt) => Task.Delay(aSpan, aCt);
}
