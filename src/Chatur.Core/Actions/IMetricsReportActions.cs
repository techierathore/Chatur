namespace Chatur.Core.Actions;

/// <summary>
/// Chatur's own measurements, inside Chatur (BRD-253 to BRD-261; screens Measurements and Measurements across projects).
/// Every figure is read from the project's own <c>docs/metrics</c> files and nothing else: no network call, no sign-in,
/// no upload, so it works for a private project on a machine with no way out to the internet.
/// </summary>
public interface IMetricsReportActions
{
    /// <summary>The open project's figures for a period (REQ-UI-115 to REQ-UI-118, REQ-UI-120).</summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aPeriod">The period.</param>
    /// <param name="aCt">Cancels the read.</param>
    /// <returns>The report, or <see langword="null"/> when the project does not exist.</returns>
    Task<MetricsReport?> ReportAsync(int aProjectId, MetricsPeriod aPeriod, CancellationToken aCt = default);

    /// <summary>One line per project of the signed-in account, for a period (REQ-FN-313).</summary>
    /// <param name="aPeriod">The period.</param>
    /// <param name="aCt">Cancels the read.</param>
    Task<MetricsAcrossProjects> AcrossProjectsAsync(MetricsPeriod aPeriod, CancellationToken aCt = default);

    /// <summary>
    /// Saves the project's measurement files as one zip in the shape TfLens imports (REQ-FN-314): each stream file present
    /// in <c>docs/metrics</c> as <c>docs/metrics/&lt;name&gt;.jsonl</c>, nothing else, no manifest.
    /// </summary>
    /// <param name="aProjectId">The project.</param>
    /// <param name="aFolder">The folder to write the zip into; <see langword="null"/> means the project's own folder.</param>
    /// <param name="aCt">Cancels the write.</param>
    /// <returns>The zip written, or <see langword="null"/> when the project has no measurement files.</returns>
    Task<MetricsExport?> ExportAsync(int aProjectId, string? aFolder, CancellationToken aCt = default);
}
