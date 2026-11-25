using System.Globalization;
using Chatur.Core.Actions;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// Licence (mockups/licence.html). Shows the licence in force (REQ-UI-064), the warning the server sent when
/// more machines are signed in than the cap (REQ-UI-065), the machines with Forget and Block (REQ-UI-066) and
/// the features the licence switches (REQ-FN-075). Everything comes from <see cref="ILicenceActions"/>.
/// </summary>
public partial class Licence
{
    private readonly CultureInfo objCulture = CultureInfo.CurrentCulture;

    private LicenceView? objView;
    private bool objIsLoading = true;

    [Inject]
    private BrowserInterop Browser { get; set; } = default!;

    private string Subtitle => objView is null || !objView.Available
        ? "Checking App Manager."
        : $"{objView.AccountName} · {objView.AccountEmail} · checked against App Manager at {objView.CheckedAtUtc.ToLocalTime().ToString("HH:mm", objCulture)} today.";

    private int SignedIn => objView?.Devices.Count(aDevice => !aDevice.IsBlocked) ?? 0;

    private int? TermDays =>
        objView?.Licence is { ExpiresOn: { } vEnd, StartedOn: { } vStart } ? Math.Max(0, (int)Math.Round((vEnd - vStart).TotalDays)) : null;

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        objIsLoading = true;
        objView = await Actions.GetAsync();
        objIsLoading = false;
    }

    private Task RecheckAsync() => LoadAsync();

    private static BadgeVariant StateVariant(string aState) =>
        aState.Equals("active", StringComparison.OrdinalIgnoreCase) ? BadgeVariant.Success : BadgeVariant.Warning;

    private static string DeviceState(LicenceDevice aDevice) =>
        aDevice.IsBlocked ? "blocked" : aDevice.IsThisDevice ? "this device" : "signed in";

    private static BadgeVariant DeviceVariant(LicenceDevice aDevice) =>
        aDevice.IsBlocked ? BadgeVariant.Secondary : aDevice.IsThisDevice ? BadgeVariant.Success : BadgeVariant.Warning;

    private async Task CopyKeyAsync()
    {
        var vKey = objView?.Licence?.Key;
        if (vKey is not null && await Browser.CopyTextAsync(vKey))
        {
            Toast.Success("Copied the licence key.");
        }
        else
        {
            Toast.Error("The licence key could not be copied.");
        }
    }

    private void AddSeat() =>
        Toast.Info("Add a machine in App Manager, then use Check again here.");

    private async Task ForgetAsync(LicenceDevice aDevice)
    {
        var vResult = await Actions.ForgetDeviceAsync(aDevice.DeviceId);
        Report(vResult, $"Forgot {aDevice.Name}.");
        await LoadAsync();
    }

    private async Task BlockAsync(LicenceDevice aDevice)
    {
        var vResult = await Actions.SetBlockedAsync(aDevice.DeviceId, !aDevice.IsBlocked);
        Report(vResult, aDevice.IsBlocked ? $"Unblocked {aDevice.Name}." : $"Blocked {aDevice.Name}.");
        await LoadAsync();
    }

    private async Task SetFeatureAsync(LicenceFeature aFeature, bool aOn)
    {
        var vResult = await Actions.SetFeatureAsync(aFeature.Code, aOn);
        Report(vResult, $"{aFeature.Name} — now {(aOn ? "on" : "off")}.");
        await LoadAsync();
    }

    private void Report(LicenceActionResult aResult, string aDone)
    {
        if (aResult.Succeeded)
        {
            Toast.Success(aDone);
        }
        else
        {
            Toast.Error(aResult.Message ?? "That did not work.");
        }
    }
}
