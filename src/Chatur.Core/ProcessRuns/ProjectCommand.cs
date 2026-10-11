namespace Chatur.Core.ProcessRuns;

/// <summary>One command to run in a project's folder.</summary>
/// <param name="FileName">The program, resolved against <c>PATH</c>.</param>
/// <param name="Arguments">Its arguments, as one string.</param>
/// <param name="WorkingDirectory">The folder it runs in.</param>
/// <param name="Timeout">How long it may run before its process tree is stopped.</param>
/// <param name="MaxOutputChars">How many characters of the end of its output are kept.</param>
/// <param name="Environment">Extra environment variables, or <see langword="null"/>.</param>
public sealed record ProjectCommand(
    string FileName,
    string Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    int MaxOutputChars,
    IReadOnlyDictionary<string, string>? Environment = null)
{
    /// <summary>The command as it would be typed, for the record.</summary>
    public string Display => Arguments.Length == 0 ? FileName : $"{Path.GetFileName(FileName)} {Arguments}";
}

/// <summary>What running a <see cref="ProjectCommand"/> came to.</summary>
/// <param name="Started"><see langword="false"/> when the program could not be started at all.</param>
/// <param name="ExitCode">The exit code; -1 when it did not start or was stopped at its time limit.</param>
/// <param name="TimedOut"><see langword="true"/> when it was stopped for running past its limit.</param>
/// <param name="Tail">The end of its output, standard output and error together.</param>
/// <param name="Error">Why it could not start; <see langword="null"/> otherwise.</param>
/// <param name="Elapsed">How long it ran.</param>
public sealed record ProjectCommandResult(
    bool Started,
    int ExitCode,
    bool TimedOut,
    string Tail,
    string? Error,
    TimeSpan Elapsed);
