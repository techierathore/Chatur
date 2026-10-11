namespace Chatur.Core.AutoRun;

/// <summary>An <c>AutoRun</c> row joined to its project's name, as read by Dapper.</summary>
public sealed class AutoRunRow
{
    /// <summary>The run's id.</summary>
    public long AutoRunId { get; set; }

    /// <summary>The project's id.</summary>
    public long ProjectId { get; set; }

    /// <summary>The project's name.</summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>The brief.</summary>
    public string Brief { get; set; } = string.Empty;

    /// <summary>The state, as its enum name.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>The place in the chain the run is on.</summary>
    public long CurrentProcess { get; set; }

    /// <summary>When it began.</summary>
    public string StartedUtc { get; set; } = string.Empty;

    /// <summary>When it ended, if it has.</summary>
    public string? EndedUtc { get; set; }

    /// <summary>Why it stopped, if it did.</summary>
    public string? StopReason { get; set; }

    /// <summary>When it carries on, while it waits out a limit.</summary>
    public string? ResumeAtUtc { get; set; }
}
