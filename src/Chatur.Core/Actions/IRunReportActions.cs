namespace Chatur.Core.Actions;

/// <summary>
/// The report of a run, read from the selected project's measurement streams (page Run report,
/// <c>/auto/report</c>; BRD-131 to BRD-134). Read only: nothing is written and nothing is cached, so a
/// run that ended a moment ago is there the next time the page asks.
/// </summary>
public interface IRunReportActions
{
    /// <summary>The runs on record for the selected project, newest first (REQ-UI-061).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The runs; empty when no project is selected or no run has been recorded.</returns>
    Task<IReadOnlyList<RunReportEntry>> RunsAsync(CancellationToken aCt = default);

    /// <summary>The report of one run (REQ-UI-058, REQ-UI-059, REQ-UI-060, REQ-UI-061).</summary>
    /// <param name="aRunKey">A run's <see cref="RunReportEntry.Key"/>.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The report, or <see langword="null"/> when the run is not on record.</returns>
    Task<RunReportView?> ReportAsync(string aRunKey, CancellationToken aCt = default);
}
