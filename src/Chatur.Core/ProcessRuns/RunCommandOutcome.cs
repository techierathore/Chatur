namespace Chatur.Core.ProcessRuns;

/// <summary>How a command of a verifying run ended.</summary>
public enum RunCommandOutcome
{
    /// <summary>A build that exited 0, or a test command whose results were read (some tests may have failed).</summary>
    Succeeded,

    /// <summary>A build that exited non-zero.</summary>
    Failed,

    /// <summary>The command ran past its time limit and its process tree was stopped.</summary>
    TimedOut,

    /// <summary>The command could not start, or a test command ended without results anyone can read.</summary>
    NotRun
}
