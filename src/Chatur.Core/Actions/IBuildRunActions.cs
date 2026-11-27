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
    /// Makes a target the one to run for this project, chosen from the target picker, the Project menu
    /// or a right-click in Files; it is offered first the next time this account opens the project (BRD-228).
    /// </summary>
    /// <param name="aProjectId">The project (a solution or a folder).</param>
    /// <param name="aRunTargetId">The target chosen.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetRunTargetAsync(int aProjectId, int aRunTargetId, CancellationToken aCt = default);

    /// <summary>
    /// Makes the target that runs the project file the one to run, so a project just added by New project is the project
    /// that runs (REQ-FN-309, owner's test of 25 Nov 2026). A project that cannot run (a class library), or one that only
    /// runs tests, is never made the project to run and leaves the choice as it was.
    /// </summary>
    /// <param name="aProjectId">The project (a solution or a folder).</param>
    /// <param name="aProjectFile">The full path of the project's file.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>Whether a target was found and chosen.</returns>
    Task<bool> SetRunTargetForProjectFileAsync(int aProjectId, string aProjectFile, CancellationToken aCt = default) => Task.FromResult(false);

    /// <summary>
    /// The targets that start together when Run is pressed on this solution (REQ-FN-324): the saved choice of Multiple
    /// startup projects, read against the targets as they are now. Empty when the solution starts one project.
    /// </summary>
    /// <param name="aProjectId">The project (a solution or a folder).</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<RunTarget>> StartupAsync(int aProjectId, CancellationToken aCt = default) => Task.FromResult<IReadOnlyList<RunTarget>>([]);

    /// <summary>
    /// Saves which projects start together for this solution and account (REQ-FN-324): two or more project files make
    /// Run and Build act on all of them; fewer clear the choice, so Run starts the one project chosen in the picker.
    /// </summary>
    /// <param name="aProjectId">The project (a solution or a folder).</param>
    /// <param name="aProjectFiles">The projects' files, relative to the project's folder with forward slashes, as <see cref="RunTarget.ProjectFile"/> gives them.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SetStartupAsync(int aProjectId, IReadOnlyList<string> aProjectFiles, CancellationToken aCt = default) => Task.CompletedTask;

    /// <summary>
    /// The build or run's output, one line at a time, while it is still going (REQ-UI-014).
    /// </summary>
    /// <param name="aProjectId">The project whose output to stream.</param>
    /// <param name="aCt">A token that stops the stream.</param>
    IAsyncEnumerable<OutputLine> StreamOutputAsync(int aProjectId, CancellationToken aCt = default);
}
