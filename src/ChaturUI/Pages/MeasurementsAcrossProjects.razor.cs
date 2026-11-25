using System.Globalization;
using Chatur.Core.Actions;
using ChaturUI.Components.Metrics;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Measurements across projects (mockups/measurements-all.html): the same totals for every project of the account, for a
/// period, each read from that project's own <c>docs/metrics</c> files (REQ-FN-313). A project with no such folder reads
/// "not measured" and is greyed rather than dropped. Opening one selects it and shows its Measurements.
/// </summary>
public partial class MeasurementsAcrossProjects
{
    private MetricsAcrossProjects? objAcross;
    private MetricsPeriod objPeriod = new(MetricsPeriodKind.Last7Days);

    [Inject]
    private IMetricsReportActions MetricsActions { get; set; } = default!;

    [Inject]
    private IAllProjectsActions AllProjectsActions { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    private sealed record Tile(string Key, string Label, string Value);

    private IReadOnlyList<Tile> Tiles => objAcross is null
        ?
        [
            new("projects", "Projects", "—"),
            new("runs", "Runs recorded", "—"),
            new("tokens-out", "Tokens out", "—"),
            new("usd", "Measured USD", "—"),
            new("misses", "Misses", "—"),
        ]
        :
        [
            new("projects", "Projects", objAcross.Projects.Count.ToString("N0", CultureInfo.InvariantCulture)),
            new("runs", "Runs recorded", objAcross.Totals.Runs.ToString("N0", CultureInfo.InvariantCulture)),
            new("tokens-out", "Tokens out", MetricsFormat.Sum(objAcross.Totals.TokensOut)),
            new("usd", "Measured USD", MetricsFormat.Usd(objAcross.Totals.Cost)),
            new("misses", "Misses", objAcross.Misses.ToString("N0", CultureInfo.InvariantCulture)),
        ];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (State.IsFeatureOn(Chatur.Core.Licences.ChaturFeatures.Measurements))
        {
            objAcross = await MetricsActions.AcrossProjectsAsync(objPeriod);
        }
    }

    private async Task OnPeriodChangedAsync(MetricsPeriod aPeriod)
    {
        objPeriod = aPeriod;
        objAcross = await MetricsActions.AcrossProjectsAsync(objPeriod);
    }

    /// <summary>A project with no docs/metrics folder reads "not measured" in every figure column.</summary>
    private static string Figure(ProjectMetricsLine aLine, string aText) => aLine.FolderFound ? aText : MetricsFormat.NotMeasured;

    private async Task OpenProjectAsync(int aProjectId)
    {
        await AllProjectsActions.OpenAsync(aProjectId);
        Nav.NavigateTo("/measurements");
    }
}
