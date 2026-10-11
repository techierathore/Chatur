namespace Chatur.Core.RunQueue;

/// <summary>A <c>RunQueueItem</c> row as Dapper reads it.</summary>
internal sealed class RunQueueRow
{
    public int RunQueueItemId { get; set; }

    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string Brief { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? AutoRunId { get; set; }

    public string? ResumeFocus { get; set; }
}
