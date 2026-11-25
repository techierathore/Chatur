using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Licences;

/// <summary>
/// The real <see cref="ILicenceSource"/>: the signed-in account's licence, devices and features from App
/// Manager. It reads the stored access token and refreshes it once when App Manager refuses it. App Manager
/// reports the device cap and its warning only on a sign-in, so those come from <see cref="AppState.DeviceCap"/>;
/// App Manager has no call to switch a feature or to unblock a device, so those say so.
/// </summary>
public sealed class AppManagerLicenceSource : ILicenceSource
{
    private readonly IAppManagerLicenceClient objClient;
    private readonly IAppManagerClient objAuthClient;
    private readonly ISecretStore objSecrets;
    private readonly AppState objAppState;
    private readonly IClock objClock;
    private readonly IDbConnectionFactory objDb;
    private readonly ILogger<AppManagerLicenceSource> objLogger;

    /// <summary>Creates the source.</summary>
    /// <param name="aClient">The licence calls.</param>
    /// <param name="aAuthClient">The sign-in client, used to refresh a token.</param>
    /// <param name="aSecrets">Where the session's tokens are kept.</param>
    /// <param name="aAppState">The shared state, holding the last device-cap report.</param>
    /// <param name="aClock">The clock.</param>
    /// <param name="aDb">Chatur's database, where this installation's id is kept.</param>
    /// <param name="aLogger">Where a failure is logged.</param>
    public AppManagerLicenceSource(IAppManagerLicenceClient aClient, IAppManagerClient aAuthClient, ISecretStore aSecrets, AppState aAppState, IClock aClock, IDbConnectionFactory aDb, ILogger<AppManagerLicenceSource> aLogger)
    {
        objClient = aClient;
        objAuthClient = aAuthClient;
        objSecrets = aSecrets;
        objAppState = aAppState;
        objClock = aClock;
        objDb = aDb;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<LicenceSnapshot> ReadAsync(CancellationToken aCt = default)
    {
        var vLicences = await CallAsync(aToken => objClient.GetLicencesAsync(aToken, aCt), aCt).ConfigureAwait(false);
        var vDevices = await CallAsync(aToken => objClient.GetDevicesAsync(aToken, aCt), aCt).ConfigureAwait(false);
        var vFeatures = await CallAsync(aToken => objClient.GetFeaturesAsync(aToken, aCt), aCt).ConfigureAwait(false);
        if (!vLicences.Ok)
        {
            throw new InvalidOperationException(vLicences.Message ?? $"App Manager answered {vLicences.Status} for the licence.");
        }

        var vCap = objAppState.DeviceCap;
        var vInstallationId = await InstallationId.GetOrCreateAsync(objDb, objClock, aCt).ConfigureAwait(false);
        var vList = (vDevices.Data ?? [])
            .Select(aDevice => ToDevice(aDevice, string.Equals(aDevice.DeviceIdentifier, vInstallationId, StringComparison.Ordinal)))
            .ToList();

        var vLicence = (vLicences.Data ?? []).FirstOrDefault(aItem => string.Equals(aItem.Status, "Active", StringComparison.OrdinalIgnoreCase))
            ?? (vLicences.Data ?? []).FirstOrDefault();
        LicenceInfo? vInfo = null;
        if (vLicence is not null)
        {
            vInfo = new LicenceInfo(
                vLicence.LicenseName ?? "Licence",
                vLicence.Status ?? "Unknown",
                vLicence.LicenseKey,
                vLicence.ExpiryDate,
                vLicence.DaysRemaining,
                vLicence.ActivationDate,
                vCap?.MaxDevices);
        }

        var vSignedIn = vList.Count(aDevice => !aDevice.IsBlocked);
        // A report from an earlier sign-in is shown only while it still holds: once enough machines are forgotten the warning is gone.
        var vWarning = vCap is { IsOverCap: true } && vSignedIn > vCap.MaxDevices ? vCap.Warning : null;
        var vAccess = vFeatures.Ok
            ? (vFeatures.Data ?? []).Where(aFeature => aFeature.FeatureCode is not null).Select(aFeature => new FeatureAccess(aFeature.FeatureCode!, aFeature.HasAccess)).ToList()
            : [];
        return new LicenceSnapshot(vInfo, vWarning, vList, vAccess, CanChangeFeatures: false);
    }

    /// <inheritdoc />
    public async Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default)
    {
        if (!long.TryParse(aDeviceId, out var vId))
        {
            return new LicenceActionResult(false, "That machine is not on the licence.");
        }

        var vCall = await CallAsync(aToken => objClient.ForgetDeviceAsync(aToken, vId, aCt), aCt).ConfigureAwait(false);
        return new LicenceActionResult(vCall.Ok, vCall.Ok ? null : vCall.Message ?? "App Manager refused to forget the machine.");
    }

    /// <inheritdoc />
    public async Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default)
    {
        if (!aBlocked)
        {
            return new LicenceActionResult(false, "App Manager has no way to unblock a machine yet. Forget it instead.");
        }

        if (!long.TryParse(aDeviceId, out var vId))
        {
            return new LicenceActionResult(false, "That machine is not on the licence.");
        }

        var vCall = await CallAsync(aToken => objClient.BlockDeviceAsync(aToken, vId, aCt), aCt).ConfigureAwait(false);
        return new LicenceActionResult(vCall.Ok, vCall.Ok ? null : vCall.Message ?? "App Manager refused to block the machine.");
    }

    /// <inheritdoc />
    public Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default) =>
        Task.FromResult(new LicenceActionResult(false, "A feature is switched in App Manager, not in Chatur."));

    private static LicenceDevice ToDevice(AppManagerDevice aDevice, bool aIsThisDevice) =>
        new(
            aDevice.UserDeviceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            aDevice.DeviceName ?? "Unnamed machine",
            aDevice.Platform,
            aDevice.AppVersion,
            aDevice.LastSeenDate,
            aDevice.SignInCount,
            aDevice.IsBlocked,
            aIsThisDevice);

    /// <summary>Runs a call with the stored access token; on a 401 refreshes it once and runs the call again.</summary>
    private async Task<AppManagerCall<T>> CallAsync<T>(Func<string, Task<AppManagerCall<T>>> aCall, CancellationToken aCt)
    {
        var vToken = await objSecrets.ReadAsync(AccountSecretNames.AccessToken, aCt).ConfigureAwait(false);
        if (vToken is null)
        {
            return new AppManagerCall<T>(false, 401, "Sign in again with \"Remember me\" ticked to see the licence.", default);
        }

        var vResult = await aCall(vToken).ConfigureAwait(false);
        if (vResult.Status != 401)
        {
            return vResult;
        }

        var vRefreshToken = await objSecrets.ReadAsync(AccountSecretNames.RefreshToken, aCt).ConfigureAwait(false);
        if (vRefreshToken is null)
        {
            return vResult;
        }

        var vRefreshed = await objAuthClient.RefreshAsync(vRefreshToken, aCt).ConfigureAwait(false);
        if (!vRefreshed.Success || vRefreshed.AccessToken is null)
        {
            objLogger.LogInformation("The licence call's token could not be refreshed");
            return vResult;
        }

        await objSecrets.SaveAsync(AccountSecretNames.AccessToken, vRefreshed.AccessToken, aCt).ConfigureAwait(false);
        if (vRefreshed.RefreshToken is not null)
        {
            await objSecrets.SaveAsync(AccountSecretNames.RefreshToken, vRefreshed.RefreshToken, aCt).ConfigureAwait(false);
        }

        await objSecrets.SaveAsync(AccountSecretNames.TokenExpiresAtUtc, (vRefreshed.TokenExpiresAtUtc ?? objClock.UtcNow.AddHours(1)).ToString("O"), aCt).ConfigureAwait(false);
        return await aCall(vRefreshed.AccessToken).ConfigureAwait(false);
    }
}
