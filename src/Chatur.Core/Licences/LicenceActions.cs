using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Licences;

/// <summary>
/// <see cref="ILicenceActions"/> over an <see cref="ILicenceSource"/>: joins what the source reports with
/// Chatur's own feature list, words the over-cap title, and keeps <see cref="AppState.OffFeatures"/> current
/// so every surface can leave an off feature undrawn (REQ-UI-064 to REQ-UI-066, REQ-FN-075).
/// </summary>
public sealed class LicenceActions : ILicenceActions
{
    private readonly AppState objAppState;
    private readonly ILicenceSource objSource;
    private readonly IClock objClock;
    private readonly ILogger<LicenceActions> objLogger;

    /// <summary>Creates the action.</summary>
    /// <param name="aAppState">The shared state.</param>
    /// <param name="aSource">Where the licence comes from.</param>
    /// <param name="aClock">The clock.</param>
    /// <param name="aLogger">Where a failed read is logged.</param>
    public LicenceActions(AppState aAppState, ILicenceSource aSource, IClock aClock, ILogger<LicenceActions> aLogger)
    {
        objAppState = aAppState;
        objSource = aSource;
        objClock = aClock;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<LicenceView> GetAsync(CancellationToken aCt = default)
    {
        var vUser = objAppState.CurrentUser;
        if (vUser is null)
        {
            return Unavailable("Sign in to see your licence.", string.Empty, string.Empty);
        }

        try
        {
            var vSnapshot = await objSource.ReadAsync(aCt).ConfigureAwait(false);
            var vFeatures = Join(vSnapshot.Features);
            objAppState.SetOffFeatures(vUser.UserId, vFeatures.Where(aFeature => !aFeature.IsOn).Select(aFeature => aFeature.Code));

            var vSignedIn = vSnapshot.Devices.Count(aDevice => !aDevice.IsBlocked);
            LicenceWarning? vWarning = null;
            if (!string.IsNullOrWhiteSpace(vSnapshot.ServerWarning))
            {
                var vCap = vSnapshot.Licence?.MaxDevices;
                var vTitle = vCap is null
                    ? "App Manager: over the device cap"
                    : $"App Manager: {vSignedIn} devices against a cap of {vCap}";
                vWarning = new LicenceWarning(vTitle, vSnapshot.ServerWarning);
            }

            return new LicenceView(true, null, vUser.DisplayName, vUser.Email, objClock.UtcNow, vSnapshot.Licence, vWarning, vSnapshot.Devices, vFeatures, vSnapshot.CanChangeFeatures);
        }
        catch (Exception vException) when (vException is not OperationCanceledException)
        {
            objLogger.LogWarning(vException, "The licence could not be read");
            return Unavailable("The licence could not be read from App Manager. Check your connection and try again.", vUser.DisplayName, vUser.Email);
        }
    }

    /// <inheritdoc />
    public async Task EnsureFeatureSwitchesAsync(CancellationToken aCt = default)
    {
        var vUser = objAppState.CurrentUser;
        if (vUser is null || objAppState.FeaturesReadFor == vUser.UserId)
        {
            return;
        }

        // Marked read even when the read fails, so a source that is down does not delay every window; the Licence screen reads again.
        objAppState.MarkFeaturesRead(vUser.UserId);
        await GetAsync(aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default) =>
        await Guard(() => objSource.ForgetDeviceAsync(aDeviceId, aCt)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default) =>
        await Guard(() => objSource.SetBlockedAsync(aDeviceId, aBlocked, aCt)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default)
    {
        var vResult = await Guard(() => objSource.SetFeatureAsync(aCode, aOn, aCt)).ConfigureAwait(false);
        if (vResult.Succeeded)
        {
            await GetAsync(aCt).ConfigureAwait(false);
        }

        return vResult;
    }

    private async Task<LicenceActionResult> Guard(Func<Task<LicenceActionResult>> aCall)
    {
        try
        {
            return await aCall().ConfigureAwait(false);
        }
        catch (Exception vException) when (vException is not OperationCanceledException)
        {
            objLogger.LogWarning(vException, "A licence change failed");
            return new LicenceActionResult(false, "App Manager could not be reached. Try again.");
        }
    }

    /// <summary>A Chatur feature the source does not name is on; only a feature the licence names switched off is off.</summary>
    private static List<LicenceFeature> Join(IReadOnlyList<FeatureAccess> aAccess) =>
        ChaturFeatures.All
            .Select(aFeature => new LicenceFeature(
                aFeature.Code,
                aFeature.Name,
                aFeature.Description,
                aAccess.FirstOrDefault(aItem => string.Equals(aItem.Code, aFeature.Code, StringComparison.OrdinalIgnoreCase))?.IsOn ?? true))
            .ToList();

    private LicenceView Unavailable(string aMessage, string aName, string aEmail) =>
        new(false, aMessage, aName, aEmail, objClock.UtcNow, null, null, [], Join([]), false);
}
