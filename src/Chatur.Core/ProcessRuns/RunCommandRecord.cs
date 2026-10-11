namespace Chatur.Core.ProcessRuns;

/// <summary>One command a verifying run ran on the project, as kept in <c>ProcessRunCommand</c>.</summary>
/// <param name="Kind">Build or test.</param>
/// <param name="Command">The command as it was run.</param>
/// <param name="Outcome">How it ended.</param>
/// <param name="ExitCode">Its exit code, or <see langword="null"/> when it did not start or was stopped at its limit.</param>
/// <param name="Warnings">The warnings a build reported, or <see langword="null"/> when the build tool does not say.</param>
/// <param name="DurationMs">How long it ran, in milliseconds.</param>
/// <param name="TimeoutMs">The time limit it ran under, in milliseconds.</param>
/// <param name="LogTail">The end of its output.</param>
/// <param name="Reason">Why it did not succeed, in one line; <see langword="null"/> when it did.</param>
public sealed record RunCommandRecord(
    RunCommandKind Kind,
    string Command,
    RunCommandOutcome Outcome,
    int? ExitCode,
    int? Warnings,
    long DurationMs,
    long TimeoutMs,
    string LogTail,
    string? Reason);
