namespace Chatur.Core.Prerequisites;

/// <summary>
/// What came back from running a short-lived probe command such as <c>dotnet --version</c>
/// (REQ-FN-011, REQ-FN-012).
/// </summary>
/// <param name="Started">Whether the executable was found and actually started.</param>
/// <param name="ExitCode">The process's exit code; meaningless when <see cref="Started"/> is <see langword="false"/>.</param>
/// <param name="StandardOutput">Everything the process wrote to standard output (or standard error, when standard output was empty).</param>
public sealed record ExternalCommandResult(bool Started, int ExitCode, string StandardOutput);

/// <summary>
/// Runs a short-lived command and captures its output, so <see cref="PrerequisiteActions"/> can be
/// unit tested without spawning a real process (REQ-FN-011).
/// </summary>
public interface IExternalCommandRunner
{
    /// <summary>Runs <paramref name="aCommand"/> with <paramref name="aArguments"/> and waits for it to exit.</summary>
    /// <param name="aCommand">The executable to start, resolved against <c>PATH</c>.</param>
    /// <param name="aArguments">The arguments to pass it.</param>
    /// <param name="aCt">A token that cancels the run.</param>
    /// <returns>A result with <see cref="ExternalCommandResult.Started"/> false when the executable could not be found at all.</returns>
    Task<ExternalCommandResult> RunAsync(string aCommand, string aArguments, CancellationToken aCt = default);
}
