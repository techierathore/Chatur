namespace Chatur.Core.Actions;

/// <summary>One of the five measurement streams and what has been written to it (REQ-UI-036).</summary>
/// <param name="Name">The stream's file name, e.g. <c>"runs.jsonl"</c>.</param>
/// <param name="RecordCount">How many records the stream holds.</param>
/// <param name="NewestUtc">The newest record's time, or <see langword="null"/> when the stream is empty.</param>
/// <param name="LastWriteFailed">Whether the most recent write to this stream failed (REQ-UI-035).</param>
/// <param name="FieldsEmpty">How many fields across the stream's records were written empty because they were not measured (REQ-FN-042).</param>
/// <param name="Refused">How many writes to this stream were refused or failed and recorded as such (REQ-FN-043, REQ-UI-035).</param>
public sealed record MeasurementStream(
    string Name,
    int RecordCount,
    DateTime? NewestUtc,
    bool LastWriteFailed,
    int FieldsEmpty = 0,
    int Refused = 0);

/// <summary>Where a project's measurement files are written, and the newest run record in them (REQ-UI-036).</summary>
/// <param name="FolderPath">The project's <c>docs/metrics</c> folder.</param>
/// <param name="NewestRunRecord">The newest line of <c>runs.jsonl</c> laid out for reading, or <see langword="null"/> when that stream has no records.</param>
public sealed record MeasurementFolder(string FolderPath, string? NewestRunRecord);
