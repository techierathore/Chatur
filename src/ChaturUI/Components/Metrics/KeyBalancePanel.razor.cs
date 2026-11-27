using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Components.Metrics;

/// <summary>
/// "What is left on each key" on Measurements and Measurements across projects (mockups/measurements.html "keys-panel"
/// and "limit-dialog"; REQ-UI-129 to REQ-UI-131, REQ-FN-326). Presentation only: every line, note and check comes from
/// <see cref="IKeyBalanceActions"/>.
/// </summary>
public partial class KeyBalancePanel
{
    private IReadOnlyList<KeyBalanceLine>? objLines;
    private bool objIsLoading;
    private string? objError;

    private bool objLimitOpen;
    private KeyBalanceLine? objLimitLine;
    private string objAmount = string.Empty;
    private bool objAmountTouched;
    private DateTime? objFrom;
    private bool objIsSaving;

    [Inject]
    private IKeyBalanceActions KeyActions { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    /// <summary>The Limit dialog's two fields as the action reads them; Save stays inert until both pass.</summary>
    private KeyLimitCheck Check => KeyActions.CheckLimit(objAmount, objFrom is { } vFrom ? DateOnly.FromDateTime(vFrom) : null);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    /// <summary>Reads every line again, asking each balance address again ("Ask again").</summary>
    private async Task LoadAsync()
    {
        objIsLoading = true;
        objError = null;
        try
        {
            objLines = await KeyActions.ListAsync();
        }
        catch (Exception vException) when (vException is InvalidOperationException or System.Data.Common.DbException)
        {
            objLines ??= [];
            objError = $"The keys could not be read: {vException.Message}";
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>The line's 1-based place in the table, which ends its test ids (<c>key-row-1</c>, …).</summary>
    private int RowOf(KeyBalanceLine aLine)
    {
        if (objLines is null)
        {
            return 0;
        }

        for (var vIndex = 0; vIndex < objLines.Count; vIndex++)
        {
            if (objLines[vIndex].ProviderId == aLine.ProviderId)
            {
                return vIndex + 1;
            }
        }

        return 0;
    }

    private void OpenLimit(KeyBalanceLine aLine)
    {
        objLimitLine = aLine;
        objAmount = aLine.Limit is { } vLimit ? vLimit.LimitUsd.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
        objAmountTouched = aLine.Limit is not null;
        // The mockup counts from the first of the month; a limit already typed keeps its own day.
        var vToday = Clock.UtcNow.ToLocalTime().Date;
        objFrom = aLine.Limit is { } vTyped ? vTyped.CountingFrom.ToDateTime(TimeOnly.MinValue) : new DateTime(vToday.Year, vToday.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        objLimitOpen = true;
    }

    private void OnAmountChanged(string? aValue)
    {
        objAmount = aValue ?? string.Empty;
        objAmountTouched = true;
    }

    private void OnFromChanged(DateTime? aValue) => objFrom = aValue;

    private void OnLimitOpenChanged(bool aOpen) => objLimitOpen = aOpen;

    private void CloseLimit() => objLimitOpen = false;

    private async Task SaveLimitAsync()
    {
        var vCheck = Check;
        objAmountTouched = true;
        if (objLimitLine is null || !vCheck.IsValid || vCheck.LimitUsd is not { } vAmount || objFrom is not { } vFrom)
        {
            return;
        }

        objIsSaving = true;
        try
        {
            await KeyActions.SetLimitAsync(objLimitLine.ProviderId, new KeyLimit(vAmount, DateOnly.FromDateTime(vFrom)));
            objLimitOpen = false;
        }
        finally
        {
            objIsSaving = false;
        }

        await LoadAsync();
    }

    private async Task ClearLimitAsync()
    {
        if (objLimitLine is null)
        {
            return;
        }

        objIsSaving = true;
        try
        {
            await KeyActions.ClearLimitAsync(objLimitLine.ProviderId);
            objLimitOpen = false;
        }
        finally
        {
            objIsSaving = false;
        }

        await LoadAsync();
    }

    /// <summary>The pill's colour: ok for asked of the provider, neutral for Chatur's own count, warn for not known (as the mockup).</summary>
    private static BadgeVariant SourceVariant(KeyBalanceKind aKind) => aKind switch
    {
        KeyBalanceKind.AskedOfProvider => BadgeVariant.Success,
        KeyBalanceKind.OwnCount => BadgeVariant.Secondary,
        _ => BadgeVariant.Warning,
    };
}
