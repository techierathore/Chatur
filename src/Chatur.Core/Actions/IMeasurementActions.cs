namespace Chatur.Core.Actions;

/// <summary>
/// Appending to the project's five measurement streams and reporting what has been written
/// (Architecture §7 "Measurements"; page Settings ▸ Measurements).
/// </summary>
public interface IMeasurementActions
{
    /// <summary>A summary of each of the five streams for a project (REQ-UI-036).</summary>
    /// <param name="aProjectId">The project whose streams to summarise.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<MeasurementStream>> StreamsAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>The folder a project's streams are written to and its newest run record (REQ-UI-036).</summary>
    /// <param name="aProjectId">The project whose folder to describe.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The folder, or <see langword="null"/> when the project is not known.</returns>
    Task<MeasurementFolder?> FolderAsync(int aProjectId, CancellationToken aCt = default);

    /// <summary>
    /// Appends one record to a stream, signed with <c>chatur</c> as the tool name (REQ-FN-040,
    /// REQ-FN-041, REQ-FN-042, REQ-FN-043). A field that was not measured is written empty, never
    /// guessed; a field or value the schema does not know refuses the whole write.
    /// </summary>
    /// <param name="aProjectId">The project the record belongs to.</param>
    /// <param name="aStreamName">The stream to append to, e.g. <c>"runs.jsonl"</c>.</param>
    /// <param name="aFields">The record's fields; a missing value is written empty.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <remarks>A failed write is logged and never stops the session (REQ-UI-035).</remarks>
    Task AppendAsync(int aProjectId, string aStreamName, IReadOnlyDictionary<string, string?> aFields, CancellationToken aCt = default);
}
