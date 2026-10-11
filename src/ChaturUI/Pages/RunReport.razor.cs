using System.Globalization;
using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Run report (mockups/run-report.html). Shows one run's report — every requirement it touched with the
/// verdict it was left at (REQ-UI-058), every miss and whose gap it was (REQ-UI-059), the tokens, models
/// and time it took (REQ-UI-060) — and lets the owner read an earlier run instead of the newest
/// (REQ-UI-061). The newest run is shown when the view opens. Everything comes from
/// <see cref="IRunReportActions"/>; the page only shows it.
/// </summary>
public partial class RunReport : IDisposable
{
    private readonly CancellationTokenSource objCts = new();

    private IReadOnlyList<RunReportEntry> objRuns = [];
    private RunReportView? objReport;
    private IReadOnlyList<RequirementRow> objRequirementRows = [];
    private IReadOnlyList<MissRow> objMissRows = [];
    private IReadOnlyList<CostRow> objCostRows = [];
    private string objSelectedKey = string.Empty;
    private bool objIsLoading = true;
    private bool objFailed;


    /// <summary>A requirement row with its position, which names its test hooks.</summary>
    /// <param name="Number">One-based position.</param>
    /// <param name="Item">The requirement.</param>
    private sealed record RequirementRow(int Number, RunReportRequirement Item);

    /// <summary>A miss row with its position.</summary>
    /// <param name="Number">One-based position.</param>
    /// <param name="Item">The miss.</param>
    private sealed record MissRow(int Number, RunReportMiss Item);

    /// <summary>A cost row already laid out for reading.</summary>
    /// <param name="Number">One-based position; the total row is 0.</param>
    /// <param name="Model">Model name, or "Total".</param>
    /// <param name="Tier">Tier, or the count of models on the total row.</param>
    /// <param name="Calls">Calls.</param>
    /// <param name="In">Tokens in.</param>
    /// <param name="Out">Tokens out.</param>
    /// <param name="Time">Time.</param>
    /// <param name="IsTotal">Whether this is the total row.</param>
    private sealed record CostRow(int Number, string Model, string Tier, string Calls, string In, string Out, string Time, bool IsTotal);

    private string Subtitle
    {
        get
        {
            if (objReport is null)
            {
                return objIsLoading ? "Reading the record…" : "Pick a run to read its report.";
            }

            var vRun = objReport.Run;
            var vStarted = vRun.StartedUtc.ToLocalTime();
            var vDay = vStarted.Date == DateTime.Today ? "today" : vStarted.ToString("d MMM", CultureInfo.InvariantCulture);
            var vEnd = vRun.EndedUtc is { } vEnded ? $"finished {vEnded.ToLocalTime():HH:mm}" : "record not closed";
            return $"{vRun.ProjectName} · started {vStarted:HH:mm} {vDay} · {vEnd}";
        }
    }

    private string RequirementsShown => $"{objRequirementRows.Count} of {objRequirementRows.Count} shown";

    private string VerdictCounts
    {
        get
        {
            if (objReport is null || objReport.Requirements.Count == 0)
            {
                return "No verdicts recorded.";
            }

            var vParts = objReport.Requirements
                .GroupBy(aItem => VerdictText(aItem.Verdict))
                .OrderByDescending(aGroup => aGroup.Count())
                .Select(aGroup => $"{aGroup.Count()} {aGroup.Key}");
            return string.Join(" · ", vParts) + ".";
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        try
        {
            objRuns = await Reports.RunsAsync(objCts.Token);
            if (objRuns.Count > 0)
            {
                // The newest run is chosen when the view opens.
                objSelectedKey = objRuns[0].Key;
                await LoadAsync(objSelectedKey);
            }
        }
        catch (OperationCanceledException)
        {
            // The tab was closed while the record was being read.
        }
        catch (Exception aEx) when (aEx is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            objFailed = true;
        }
        finally
        {
            objIsLoading = false;
        }
    }

    private async Task OnPickedAsync(string aKey)
    {
        objSelectedKey = aKey;
        await LoadAsync(aKey);
    }

    private Task OpenPickedAsync() => LoadAsync(objSelectedKey);

    private async Task LoadAsync(string aKey)
    {
        objIsLoading = true;
        objFailed = false;
        try
        {
            objReport = await Reports.ReportAsync(aKey, objCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception aEx) when (aEx is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            objReport = null;
            objFailed = true;
        }
        finally
        {
            objIsLoading = false;
        }

        Lay();
    }

    private void Lay()
    {
        objRequirementRows = objReport?.Requirements.Select((aItem, aIndex) => new RequirementRow(aIndex + 1, aItem)).ToList() ?? [];
        objMissRows = objReport?.Misses.Select((aItem, aIndex) => new MissRow(aIndex + 1, aItem)).ToList() ?? [];
        var vCost = objReport?.Cost ?? [];
        var vRows = vCost
            .Select((aItem, aIndex) => new CostRow(aIndex + 1, aItem.Model, aItem.Tier.Length == 0 ? NotRecorded : aItem.Tier, Count(aItem.Calls), Count(aItem.TokensIn), Count(aItem.TokensOut), Seconds(aItem.Seconds), false))
            .ToList();
        if (vCost.Count > 0 && objReport is { } vReport)
        {
            // A total is a sum only when every model's figure was recorded; a partial sum would read as the whole.
            vRows.Add(new CostRow(
                0,
                "Total",
                vCost.Count == 1 ? "1 model" : $"{vCost.Count} models",
                Count(SumOrNull(vCost, aItem => aItem.Calls)),
                Count(vReport.TotalTokensIn ?? SumOrNull(vCost, aItem => aItem.TokensIn)),
                Count(vReport.TotalTokensOut ?? SumOrNull(vCost, aItem => aItem.TokensOut)),
                Seconds(vReport.DurationSeconds),
                true));
        }

        objCostRows = vRows;
    }

    private string LabelOf(string aKey) =>
        objRuns.FirstOrDefault(aRun => aRun.Key == aKey) is { } vRun ? LabelOf(vRun) : string.Empty;

    private static string LabelOf(RunReportEntry aRun) =>
        $"{aRun.ProjectName} · {aRun.StartedUtc.ToLocalTime().ToString("d MMM HH:mm", CultureInfo.InvariantCulture)} · {(aRun.IsClosed ? "finished" : "record not closed")}";

    /// <summary>What a figure the run's record does not carry reads as.</summary>
    private const string NotRecorded = "not recorded";

    private static long? SumOrNull(IReadOnlyList<RunReportCost> aCost, Func<RunReportCost, long?> aFigure) =>
        aCost.All(aItem => aFigure(aItem) is not null) ? aCost.Sum(aItem => aFigure(aItem)!.Value) : null;

    private static string Tile(int? aCount) => aCount is null ? "—" : aCount.Value.ToString(CultureInfo.InvariantCulture);

    private static string FigureClass(string aText, string aNormal) =>
        aText == NotRecorded ? "whitespace-nowrap text-sm font-medium text-muted-foreground" : aNormal;

    private static string Count(long? aValue) => aValue is null ? NotRecorded : aValue.Value.ToString("N0", CultureInfo.InvariantCulture);

    private static string TokensText(long? aTokens) => aTokens switch
    {
        null => NotRecorded,
        >= 1_000_000 => (aTokens.Value / 1_000_000d).ToString("0.00", CultureInfo.InvariantCulture) + " M",
        >= 10_000 => (aTokens.Value / 1_000d).ToString("0.0", CultureInfo.InvariantCulture) + " k",
        _ => aTokens.Value.ToString("N0", CultureInfo.InvariantCulture)
    };

    private static string DurationText(long? aSeconds)
    {
        if (aSeconds is null)
        {
            return NotRecorded;
        }

        var vSpan = TimeSpan.FromSeconds(aSeconds.Value);
        return vSpan.TotalHours >= 1
            ? $"{(int)vSpan.TotalHours} h {vSpan.Minutes} m"
            : vSpan.TotalMinutes >= 1 ? $"{vSpan.Minutes} m {vSpan.Seconds} s" : $"{vSpan.Seconds} s";
    }

    private static string Seconds(long? aSeconds) => DurationText(aSeconds);

    private static string WaitText(long aSeconds) => aSeconds >= 60 ? $"{aSeconds / 60} minutes" : $"{aSeconds} seconds";

    private static string WhenText(DateTime? aUtc) => aUtc is null ? "—" : aUtc.Value.ToLocalTime().ToString("HH:mm", CultureInfo.InvariantCulture);

    private static string VerdictText(string aVerdict) => aVerdict switch
    {
        "" => "Not graded",
        "Needs re-verify" => "Partial",
        _ => aVerdict
    };

    private static BadgeVariant VerdictVariant(string aVerdict) => aVerdict switch
    {
        "Verified" or "Done (pre-existing)" => BadgeVariant.Success,
        "Needs re-verify" or "Implemented" => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary
    };

    // The mockup's Blocked pill is the neutral one with red text, not the red-filled one.
    private static string VerdictClass(string aVerdict) => aVerdict is "Blocked" or "FAIL"
        ? "gap-1 rounded-full border-transparent text-destructive"
        : "gap-1 rounded-full border-transparent";

    private static string OwnerText(MissOwner aOwner) => aOwner switch
    {
        MissOwner.OwnerBrief => "the owner's brief",
        MissOwner.TrBlazeUI => "TrBlazeUI",
        MissOwner.TechieRag => "TechieRag",
        _ => "Chatur"
    };

    private static IReadOnlyDictionary<string, object>? RequirementHooks(RequirementRow aRow) =>
        new Dictionary<string, object> { ["data-testid"] = $"req-row-{aRow.Number}", ["class"] = "border-t" };

    private static IReadOnlyDictionary<string, object>? MissHooks(MissRow aRow) =>
        new Dictionary<string, object> { ["data-testid"] = $"miss-row-{aRow.Number}", ["class"] = "border-t" };

    private static IReadOnlyDictionary<string, object>? CostHooks(CostRow aRow) =>
        new Dictionary<string, object> { ["data-testid"] = aRow.IsTotal ? "cost-row-total" : $"cost-row-{aRow.Number}", ["class"] = "border-t" };

    private void OpenQueue() => WorkbenchTabs.OpenRunQueue();

    private void OpenAutoRun() => WorkbenchTabs.OpenAutoRun();

    private void OpenBoard() => WorkbenchTabs.OpenBoard();

    /// <summary>Opens the feedback file of the library the run's misses name, from the project's docs folder.</summary>
    private async Task OpenFeedbackAsync()
    {
        var vProject = await ProjectActions.SelectedProjectAsync(objCts.Token);
        var vLibraries = objReport?.Misses
            .Select(aMiss => aMiss.Owner)
            .Where(aOwner => aOwner is MissOwner.TrBlazeUI or MissOwner.TechieRag)
            .Distinct()
            .Select(aOwner => aOwner.ToString()).ToList() ?? [];
        if (vProject is null || vLibraries.Count == 0)
        {
            Toast.Show("This run's misses name no library, so there is no feedback file to open.");
            return;
        }

        foreach (var vLibrary in vLibraries)
        {
            var vPath = $"docs/{vProject.Name}-{vLibrary}-Feedback.md";
            try
            {
                var vText = await Files.ReadAsync(vProject.ProjectId, vPath, objCts.Token);
                WorkbenchTabs.Open(vPath, vText);
                return;
            }
            catch (Exception aEx) when (aEx is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Try the next library's file.
            }
        }

        Toast.Show($"The project has no feedback file for {string.Join(" or ", vLibraries)} yet.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        objCts.Cancel();
        objCts.Dispose();
    }
}
