namespace Chatur.Core.AutoRun;

/// <summary>An <c>AutoRunProcess</c> row, as read by Dapper.</summary>
public sealed class AutoRunProcessRow
{
    /// <summary>The place in the chain.</summary>
    public long Position { get; set; }

    /// <summary>The process.</summary>
    public string ProcessCode { get; set; } = string.Empty;

    /// <summary>The state, as its enum name.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>The process run that carries it out, if begun.</summary>
    public long? ProcessRunId { get; set; }

    /// <summary>How many times it was tried again.</summary>
    public long Retries { get; set; }
}
