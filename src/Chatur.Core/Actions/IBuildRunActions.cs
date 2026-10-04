namespace Chatur.Core.Actions;

/// <summary>
/// Offering run targets, building, running, streaming output and stopping (Architecture §7 "Build
/// and run"; page Workbench's toolbar and output strip).
/// </summary>
public interface IBuildRunActions
{
    /// <summary>The targets that fit this machine, the last-used one first (REQ-UI-011, REQ-FN-010).</summary>
    /// <param name="aProjectId">The project to offer targets for.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<RunTarget>> TargetsAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Builds the project on the chosen target (REQ-UI-012, REQ-UI-016).
    /// </summary>
    /// <param name="aProjectId">The project to build.</param>
    /// <param name="aRunTargetId">The target to build for.</param>
    /// <param name="aCt">A token that cancels the build.</param>
    Task<BuildResult> BuildAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default);

    /// <summary>
    /// Runs the project on the chosen target (REQ-UI-013).
    /// </summary>
    /// <param name="aProjectId">The project to run.</param>
    /// <param name="aRunTargetId">The target to run.</param>
    /// <param name="aCt">A token that cancels the run.</param>
    Task RunAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default);

    /// <summary>
    /// Stops the run and every program it started (REQ-UI-015).
    /// </summary>
    /// <param name="aProjectId">The project whose run should stop.</param>
    /// <param name="aCt">A token that cancels the stop request itself.</param>
    Task StopAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// The build or run's output, one line at a time, while it is still going (REQ-UI-014).
    /// </summary>
    /// <param name="aProjectId">The project whose output to stream.</param>
    /// <param name="aCt">A token that stops the stream.</param>
    IAsyncEnumerable<OutputLine> StreamOutputAsync(int aProjectId, CancellationToken aCt = default);
}
