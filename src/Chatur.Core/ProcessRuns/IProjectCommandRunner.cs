namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Runs one command in a project's folder under a time limit and keeps only the end of its output, so
/// a stuck or noisy build or test run can never hang a process run or fill memory (REQ-FN-052).
/// </summary>
public interface IProjectCommandRunner
{
    /// <summary>Runs the command to its end, to its time limit, or until the caller cancels.</summary>
    /// <param name="aCommand">What to run, where, and under which limits.</param>
    /// <param name="aCt">A token that stops the command's whole process tree and cancels the call.</param>
    /// <returns>What came of it; never throws for a command that failed, timed out or was not found.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="aCt"/> was cancelled; the process tree has been stopped.</exception>
    Task<ProjectCommandResult> RunAsync(ProjectCommand aCommand, CancellationToken aCt = default);
}
