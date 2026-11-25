namespace Chatur.Core.Actions;

/// <summary>
/// The licence in force, the machines on it and the features it switches (Architecture §7 "Accounts";
/// page Licence — REQ-UI-064 to REQ-UI-066, REQ-FN-075).
/// </summary>
public interface ILicenceActions
{
    /// <summary>Reads the licence, its machines, its warning and its features, and records which features are off in <see cref="AppState"/>.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<LicenceView> GetAsync(CancellationToken aCt = default);

    /// <summary>Reads the feature switches once per signed-in account, so every menu and list can leave a feature that is off undrawn (REQ-FN-075). Never throws; a licence that cannot be read leaves every feature on.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task EnsureFeatureSwitchesAsync(CancellationToken aCt = default);

    /// <summary>Forgets a machine: it leaves the list and its sign-in ends (REQ-UI-066).</summary>
    /// <param name="aDeviceId">The machine's <see cref="LicenceDevice.DeviceId"/>.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default);

    /// <summary>Blocks or unblocks a machine.</summary>
    /// <param name="aDeviceId">The machine's <see cref="LicenceDevice.DeviceId"/>.</param>
    /// <param name="aBlocked">Whether it should be blocked.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default);

    /// <summary>Switches a feature on or off where the licence source allows it (REQ-FN-075).</summary>
    /// <param name="aCode">The feature's code.</param>
    /// <param name="aOn">Whether it should be on.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default);
}
