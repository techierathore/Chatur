namespace Chatur.Core.ProcessRuns;

/// <summary>The limits a process run puts on the project's own commands (REQ-FN-052).</summary>
public sealed class ProcessRunOptions
{
    /// <summary>How long one build command may run before it is stopped. Default 10 minutes.</summary>
    public TimeSpan BuildTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>How long one test command may run before it is stopped. Default 20 minutes.</summary>
    public TimeSpan TestTimeout { get; init; } = TimeSpan.FromMinutes(20);

    /// <summary>How many characters of the end of a command's output are kept. Default 16,000.</summary>
    public int MaxOutputChars { get; init; } = 16_000;
}
