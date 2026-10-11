namespace Chatur.Core.ProcessRuns;

/// <summary>Which of a verifying run's commands a record is.</summary>
public enum RunCommandKind
{
    /// <summary>A command that builds the project.</summary>
    Build,

    /// <summary>A command that runs the project's tests.</summary>
    Test
}
