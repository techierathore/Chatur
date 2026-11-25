using System.Globalization;
using Chatur.Core.Actions;
using ChaturUI.Components.Metrics;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Measurements (mockups/measurements.html): the open project's runs, tokens, cost, time and misses for a period, read
/// from its own <c>docs/metrics</c> files and nothing else (REQ-UI-115 to REQ-UI-120), and the zip TfLens imports
/// (REQ-FN-314). The page holds no figure of its own: every number comes from <see cref="IMetricsReportActions"/>.
/// </summary>
public partial class Measurements
{
    private MetricsReport? objReport;
    private EstimateAgainstActual? objEstimate;
    private MetricsPeriod objPeriod = MetricsPeriod.AllHistory;
    private bool objIsLoading;

    /// <summary>Which split shows: model, agent or command.</summary>
    private string objSplit = "model";

    /// <summary>The three splits, in the mockup's order.</summary>
    private static readonly (string Key, string Label)[] SplitKinds = [("model", "By model"), ("agent", "By agent"), ("command", "By command")];

    private IReadOnlyCollection<RunLine> objSelected = Array.Empty<RunLine>();
    private RunLine? objDetail;
    private bool objDetailOpen;

    private bool objZipOpen;
    private bool objIsSaving;
    private string objZipFolder = string.Empty;
    private string? objZipError;
    private MetricsExport? objZip;

    [Inject]
    private IMetricsReportActions MetricsActions { get; set; } = default!;

    [Inject]
    private IEstimateActions EstimateActions { get; set; } = default!;

    private sealed record Tile(string Key, string Label, string Value, string? Note);

    private string FolderText => objReport?.MetricsFolder ?? string.Empty;

    private string PeriodNote => objPeriod.Kind switch
    {
        MetricsPeriodKind.All => "All history",
        MetricsPeriodKind.Today => "Today",
        MetricsPeriodKind.Last7Days => "Last 7 days",
        MetricsPeriodKind.Last30Days => "Last 30 days",
        _ => $"Between {objPeriod.From:d MMM yyyy} and {objPeriod.To:d MMM yyyy}",
    };

    /// <summary>One line saying why the figures are empty, when they are.</summary>
    private string? EmptyLine => objReport switch
    {
        null => null,
        { FolderFound: false } => "This project has no docs/metrics folder yet, so there is nothing to count.",
        { Totals.Runs: 0 } => "No run was recorded in this period.",
        _ => null,
    };

    private string MissCountText => objReport is null
        ? "—"
        : $"{objReport.Misses.Misses.Count} miss{(objReport.Misses.Misses.Count == 1 ? string.Empty : "es")}";

    private string? FirstPassRate => objReport is null ? null : MetricsFormat.FirstPassRate(objReport.Misses.RowsGraded, objReport.Misses.RowsFirstPass);

    private string FirstPassText => objReport is null
        ? string.Empty
        : objReport.Misses.RowsGraded == 0
            ? "no row was graded in this period"
            : $"{objReport.Misses.RowsFirstPass} of {objReport.Misses.RowsGraded} rows passed the first time";

    private string UnreadableText => objReport is null || objReport.UnreadableLines == 0
        ? string.Empty
        : $" {objReport.UnreadableLines.ToString(CultureInfo.InvariantCulture)} line{(objReport.UnreadableLines == 1 ? " was" : "s were")} not JSON, counted as unreadable and never guessed at.";

    /// <summary>The seven tiles; a figure still loading reads as a dash (UI design, Measurements, loading).</summary>
    private IReadOnlyList<Tile> Tiles
    {
        get
        {
            const string Dash = "—";
            if (objReport is null)
            {
                return
                [
                    new("runs", "Runs recorded", Dash, null),
                    new("tokens-in", "Tokens in", Dash, null),
                    new("tokens-out", "Tokens out", Dash, null),
                    new("cache-read", "Cache read", Dash, null),
                    new("cache-write", "Cache write", Dash, null),
                    new("usd", "Measured USD", Dash, null),
                    new("wall", "Total wall clock", Dash, null),
                ];
            }

            var vTotals = objReport.Totals;
            return
            [
                new("runs", "Runs recorded", vTotals.Runs.ToString("N0", CultureInfo.InvariantCulture), null),
                new("tokens-in", "Tokens in", MetricsFormat.Sum(vTotals.TokensIn), MetricsFormat.SumNote(vTotals.TokensIn)),
                new("tokens-out", "Tokens out", MetricsFormat.Sum(vTotals.TokensOut), MetricsFormat.SumNote(vTotals.TokensOut)),
                new("cache-read", "Cache read", MetricsFormat.Sum(vTotals.CacheRead), MetricsFormat.SumNote(vTotals.CacheRead)),
                new("cache-write", "Cache write", MetricsFormat.Sum(vTotals.CacheWrite), MetricsFormat.SumNote(vTotals.CacheWrite)),
                new("usd", "Measured USD", MetricsFormat.Usd(vTotals.Cost), MetricsFormat.CostNote(vTotals.Cost)),
                new("wall", "Total wall clock", MetricsFormat.Wall(vTotals.WallClockS), MetricsFormat.SumNote(vTotals.WallClockS)),
            ];
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        if (State.SelectedProject is not { } vProject || !State.IsFeatureOn(Chatur.Core.Licences.ChaturFeatures.Measurements))
        {
            objReport = null;
            objEstimate = null;
            return;
        }

        objIsLoading = true;
        try
        {
            objReport = await MetricsActions.ReportAsync(vProject.ProjectId, objPeriod);
            objEstimate = await EstimateActions.PhaseAgainstActualAsync(vProject.ProjectId);
        }
        finally
        {
            objIsLoading = false;
        }
    }

    private async Task OnPeriodChangedAsync(MetricsPeriod aPeriod)
    {
        objPeriod = aPeriod;
        await LoadAsync();
    }

    private async Task OnRunPickedAsync(IReadOnlyCollection<RunLine> aRuns)
    {
        objSelected = aRuns;
        if (aRuns.FirstOrDefault() is { } vRun)
        {
            objDetail = vRun;
            objDetailOpen = true;
        }

        await Task.CompletedTask;
    }

    private void OnDetailOpenChanged(bool aOpen)
    {
        objDetailOpen = aOpen;
        if (!aOpen)
        {
            objSelected = Array.Empty<RunLine>();
        }
    }

    private void OpenZipDialog()
    {
        objZipFolder = ProjectRoot() ?? string.Empty;
        objZipError = null;
        objZip = null;
        objZipOpen = true;
    }

    private void OnZipOpenChanged(bool aOpen) => objZipOpen = aOpen;

    private void CloseZipDialog() => objZipOpen = false;

    /// <summary>The project's own folder: two levels above its <c>docs/metrics</c> folder.</summary>
    private string? ProjectRoot()
    {
        if (objReport is null)
        {
            return null;
        }

        var vMetrics = objReport.MetricsFolder.TrimEnd('/', '\\');
        return Path.GetDirectoryName(Path.GetDirectoryName(vMetrics));
    }

    /// <summary>Writes the zip into the folder named; a folder that cannot be written says so (REQ-FN-314).</summary>
    private async Task SaveZipAsync()
    {
        if (State.SelectedProject is not { } vProject)
        {
            return;
        }

        objIsSaving = true;
        objZipError = null;
        objZip = null;
        try
        {
            objZip = await MetricsActions.ExportAsync(vProject.ProjectId, objZipFolder.Trim());
            if (objZip is null)
            {
                objZipError = "This project has no measurement files to save.";
            }
        }
        catch (Exception vException) when (vException is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            objZipError = $"That folder cannot be written: {vException.Message}";
        }
        finally
        {
            objIsSaving = false;
        }
    }

    private static BadgeVariant StatusVariant(string aStatus) => aStatus switch
    {
        "Verified" => BadgeVariant.Success,
        "open" => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary,
    };

    private static BadgeVariant ResultVariant(string? aResult) => aResult switch
    {
        "pass" => BadgeVariant.Success,
        "fail" => BadgeVariant.Destructive,
        _ => BadgeVariant.Secondary,
    };
}
