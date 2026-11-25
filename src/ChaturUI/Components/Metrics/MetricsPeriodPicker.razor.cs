using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Components.Metrics;

/// <summary>
/// Picks the period the Measurements screens count over (REQ-UI-116, BRD-254): All history, Today, Last 7 days, Last 30 days or
/// Between two dates. Between needs both dates and To on or after From; until then the page keeps its figures and the
/// fields say why. The page is told only of a period that can be counted.
/// </summary>
public partial class MetricsPeriodPicker
{
    private MetricsPeriodKind objKind;
    private DateTime? objFrom;
    private DateTime? objTo;
    private string? objError;
    private bool objSeeded;

    /// <summary>The region's <c>data-testid</c>: <c>metrics-period</c> or <c>across-period</c>.</summary>
    [Parameter]
    public string Region { get; set; } = "metrics-period";

    /// <summary>The prefix of the controls' test ids (<c>period</c> or <c>across</c>).</summary>
    [Parameter]
    public string Prefix { get; set; } = "period";

    /// <summary>The period shown first.</summary>
    [Parameter]
    public MetricsPeriodKind Initial { get; set; } = MetricsPeriodKind.All;

    /// <summary>Raised with a period that can be counted.</summary>
    [Parameter]
    public EventCallback<MetricsPeriod> OnChanged { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!objSeeded)
        {
            objKind = Initial;
            objSeeded = true;
        }
    }

    private async Task OnKindChangedAsync(string? aValue)
    {
        objKind = Enum.TryParse<MetricsPeriodKind>(aValue, out var vKind) ? vKind : MetricsPeriodKind.All;
        await ApplyAsync();
    }

    private async Task OnFromChangedAsync(DateTime? aValue)
    {
        objFrom = aValue;
        await ApplyAsync();
    }

    private async Task OnToChangedAsync(DateTime? aValue)
    {
        objTo = aValue;
        await ApplyAsync();
    }

    private async Task ApplyAsync()
    {
        objError = null;
        if (objKind != MetricsPeriodKind.Between)
        {
            await OnChanged.InvokeAsync(new MetricsPeriod(objKind));
            return;
        }

        if (objFrom is not { } vFrom || objTo is not { } vTo)
        {
            objError = "Pick both dates. The figures stay as they were.";
            return;
        }

        if (vTo.Date < vFrom.Date)
        {
            objError = "To must be on or after From. The figures stay as they were.";
            return;
        }

        await OnChanged.InvokeAsync(new MetricsPeriod(MetricsPeriodKind.Between, DateOnly.FromDateTime(vFrom), DateOnly.FromDateTime(vTo)));
    }

    private static string KindLabel(string? aValue) => aValue switch
    {
        nameof(MetricsPeriodKind.Today) => "Today",
        nameof(MetricsPeriodKind.Last7Days) => "Last 7 days",
        nameof(MetricsPeriodKind.Last30Days) => "Last 30 days",
        nameof(MetricsPeriodKind.Between) => "Between two dates",
        _ => "All history",
    };
}
