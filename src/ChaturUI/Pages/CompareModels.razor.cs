using System.Globalization;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Compare models (mockups/compare-models.html): pick two to four connected models, give them one task on the open project,
/// and see each one's time, tokens, cost and answer or changed files side by side (REQ-FN-327 to REQ-FN-329, REQ-UI-132);
/// mark each right or wrong (REQ-UI-133); open a past comparison again (REQ-FN-330). The page holds no figure of its own:
/// every number comes from <see cref="ICompareModelsActions"/>.
/// </summary>
public partial class CompareModels : IDisposable
{
    private IReadOnlyList<CompareModelOption> objModels = [];
    private readonly List<int> objPicked = [];
    private CompareKind objKind = CompareKind.Question;
    private string objTask = string.Empty;
    private string? objMessage;
    private bool objIsRunning;
    private bool objIsMarking;
    private Comparison? objComparison;
    private List<HistoryLine> objHistory = [];
    private IReadOnlyCollection<HistoryLine> objHistoryPicked = Array.Empty<HistoryLine>();
    private CancellationTokenSource? objRunCts;
    private Timer? objTicker;

    [Inject]
    private ICompareModelsActions CompareActions { get; set; } = default!;

    /// <summary>One line of Past comparisons, its cells already written as the table shows them.</summary>
    /// <param name="Line">1 for the newest.</param>
    /// <param name="Summary">The comparison.</param>
    /// <param name="When">When it ran, local time.</param>
    /// <param name="Kind">"question" or "changes files".</param>
    /// <param name="Task">The task, cut to a line.</param>
    /// <param name="Models">The models, joined.</param>
    /// <param name="Marks">The marks, for sorting.</param>
    private sealed record HistoryLine(int Line, ComparisonSummary Summary, string When, string Kind, string Task, string Models, string Marks);

    /// <summary>What the pick line says: how many are picked, or why the pick cannot run.</summary>
    private string PickLine => objMessage
        ?? (objPicked.Count > ICompareModelsActions.MaxModels
            ? CompareActions.PickRefusal(objPicked.Count)!
            : $"{objPicked.Count.ToString(CultureInfo.InvariantCulture)} picked · two to four");

    private bool PickLineIsRefusal => objMessage is not null || objPicked.Count > ICompareModelsActions.MaxModels;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (!State.IsFeatureOn(Chatur.Core.Licences.ChaturFeatures.Measurements))
        {
            return;
        }

        objModels = await CompareActions.ModelsAsync();
        await LoadHistoryAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Leaving the screen stops the models; the copies are removed and the results kept as they stand.
        objRunCts?.Cancel();
        objTicker?.Dispose();
    }

    /// <summary>The kind's words, as the mockup writes them.</summary>
    /// <param name="aKind">The kind.</param>
    private static string KindLabel(CompareKind aKind) =>
        aKind == CompareKind.Question ? "Question, changes nothing" : "Task that changes files";

    private void SetKind(CompareKind aKind) => objKind = aKind;

    private void OnPickChanged(int aModelId, bool aOn)
    {
        objMessage = null;
        if (aOn && !objPicked.Contains(aModelId))
        {
            objPicked.Add(aModelId);
        }
        else if (!aOn)
        {
            objPicked.Remove(aModelId);
        }
    }

    private async Task RunAsync()
    {
        objMessage = CompareActions.PickRefusal(objPicked.Count);
        if (objMessage is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(objTask))
        {
            objMessage = "Type the task every model is given.";
            return;
        }

        if (State.SelectedProject is not { } vProject)
        {
            objMessage = "Open a project first; every model works on it.";
            return;
        }

        objIsRunning = true;
        objComparison = null;
        objHistoryPicked = Array.Empty<HistoryLine>();
        objRunCts?.Dispose();
        objRunCts = new CancellationTokenSource();
        objTicker = new Timer(_ => _ = InvokeAsync(StateHasChanged), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        try
        {
            objComparison = await CompareActions.RunAsync(
                new CompareRequest(vProject.ProjectId, [.. objPicked], objKind, objTask),
                OnProgressAsync,
                objRunCts.Token);
        }
        catch (ArgumentException vException)
        {
            objMessage = vException.Message;
        }
        finally
        {
            objIsRunning = false;
            objTicker?.Dispose();
            objTicker = null;
        }

        await LoadHistoryAsync();
    }

    /// <summary>Shows the comparison as each model ends; called from any thread.</summary>
    private Task OnProgressAsync(Comparison aComparison) =>
        InvokeAsync(() =>
        {
            objComparison = aComparison;
            StateHasChanged();
        });

    private async Task MarkAsync(CompareResult aResult, bool aRight)
    {
        objIsMarking = true;
        try
        {
            objComparison = await CompareActions.MarkAsync(aResult.ResultId, aRight);
            objMessage = null;
        }
        catch (InvalidOperationException vException)
        {
            objMessage = vException.Message;
        }
        finally
        {
            objIsMarking = false;
        }

        await LoadHistoryAsync();
    }

    private async Task OnHistoryPickedAsync(IReadOnlyCollection<HistoryLine> aLines)
    {
        objHistoryPicked = aLines;
        if (objIsRunning || aLines.FirstOrDefault() is not { } vLine)
        {
            return;
        }

        objComparison = await CompareActions.OpenAsync(vLine.Summary.ComparisonId);
    }

    private async Task LoadHistoryAsync()
    {
        if (State.SelectedProject is not { } vProject)
        {
            objHistory = [];
            return;
        }

        var vSummaries = await CompareActions.HistoryAsync(vProject.ProjectId);
        objHistory = vSummaries.Select((aSummary, aIndex) => new HistoryLine(
            aIndex + 1,
            aSummary,
            When(aSummary.StartedUtc),
            aSummary.Kind == CompareKind.Question ? "question" : "changes files",
            aSummary.Task.Length > 60 ? aSummary.Task[..57].TrimEnd() + "…" : aSummary.Task,
            string.Join(" · ", aSummary.Models),
            $"{aSummary.Right} right, {aSummary.Wrong} wrong, {aSummary.NotMarked} not marked")).ToList();
    }

    private static string When(DateTime aUtc) => aUtc.ToLocalTime().ToString("d MMM HH:mm", CultureInfo.InvariantCulture);

    private static string StatusNote(Comparison aComparison)
    {
        var vNote = $"{KindLabel(aComparison.Kind)} · {When(aComparison.StartedUtc)}";
        var vFailed = aComparison.Results.Count(aResult => aResult.State == "failed");
        if (aComparison.State == "running")
        {
            var vLeft = aComparison.Results.Count(aResult => aResult.State == "running");
            return $"{vNote} · {vLeft} of {aComparison.Results.Count} still running";
        }

        if (vFailed > 0)
        {
            vNote += $" · {vFailed} failed";
        }

        if (aComparison.Kind == CompareKind.Question && aComparison.ProjectUnchanged is { } vUnchanged)
        {
            vNote += vUnchanged ? " · nothing in the project changed" : " · a file of the project changed while it ran";
        }

        if (aComparison.Kind == CompareKind.Change && aComparison.CopiesRemoved is { } vRemoved)
        {
            vNote += vRemoved ? " · each copy removed" : " · a copy under bin/chatur-compare could not be removed";
        }

        return vNote;
    }

    private static BadgeVariant StateVariant(string aState) => aState switch
    {
        "done" => BadgeVariant.Success,
        "failed" => BadgeVariant.Danger,
        _ => BadgeVariant.Info
    };

    private static BadgeVariant MarkVariant(CompareResult aResult) => aResult.Mark switch
    {
        "right" => BadgeVariant.Success,
        "wrong" => BadgeVariant.Danger,
        _ => BadgeVariant.Secondary
    };

    private static string MarkText(CompareResult aResult) => aResult.Mark ?? (aResult.State == "running" ? "running" : "not marked");

    /// <summary>The time a model took, or for a model still running the time so far.</summary>
    private static string TimeText(CompareResult aResult)
    {
        if (aResult.State == "running")
        {
            return $"running · {Seconds((DateTime.UtcNow - aResult.StartedUtc).TotalMilliseconds)}";
        }

        return aResult.DurationMs is { } vMs ? Seconds(vMs) : "—";
    }

    private static string Seconds(double aMs)
    {
        var vSeconds = Math.Max(0, aMs / 1000d);
        if (vSeconds < 10)
        {
            return vSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s";
        }

        if (vSeconds < 60)
        {
            return vSeconds.ToString("0", CultureInfo.InvariantCulture) + "s";
        }

        var vWhole = (int)Math.Round(vSeconds);
        return $"{vWhole / 60}m {(vWhole % 60).ToString("00", CultureInfo.InvariantCulture)}s";
    }

    private static string TokensText(CompareResult aResult)
    {
        if (aResult.State == "running")
        {
            return "—";
        }

        return aResult.TokensIn is null && aResult.TokensOut is null
            ? "not reported"
            : $"{Count(aResult.TokensIn)} / {Count(aResult.TokensOut)}";
    }

    private static string Count(long? aValue) => aValue?.ToString("N0", CultureInfo.InvariantCulture) ?? "—";

    /// <summary>TechieRag's own price of the model's calls, or "no price" when it gave none (BRD-294).</summary>
    private static string CostText(CompareResult aResult)
    {
        if (aResult.State == "running")
        {
            return "—";
        }

        if (aResult.CostUsd is not { } vCost)
        {
            return "no price";
        }

        return "$" + vCost.ToString(vCost >= 0.01m ? "0.00" : "0.0000", CultureInfo.InvariantCulture);
    }

    private static string FileLine(CompareChangedFile aFile) => aFile.Change switch
    {
        "A" => $"A {aFile.Path} (+{aFile.LinesAdded})",
        "D" => $"D {aFile.Path} (−{aFile.LinesRemoved})",
        _ => $"M {aFile.Path} (+{aFile.LinesAdded} −{aFile.LinesRemoved})"
    };
}
