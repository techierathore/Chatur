using Chatur.Core.Actions;

namespace Chatur.Core.Licences;

/// <summary>A feature's access as a licence source reports it.</summary>
/// <param name="Code">The feature's code.</param>
/// <param name="IsOn">Whether the licence has it on.</param>
public sealed record FeatureAccess(string Code, bool IsOn);

/// <summary>What a licence source knows about the signed-in account.</summary>
/// <param name="Licence">The licence in force, or <see langword="null"/>.</param>
/// <param name="ServerWarning">The server's over-cap warning, or <see langword="null"/> while within the cap.</param>
/// <param name="Devices">The machines.</param>
/// <param name="Features">The features the source names; a Chatur feature it does not name is on.</param>
/// <param name="CanChangeFeatures">Whether <see cref="ILicenceSource.SetFeatureAsync"/> works.</param>
public sealed record LicenceSnapshot(
    LicenceInfo? Licence,
    string? ServerWarning,
    IReadOnlyList<LicenceDevice> Devices,
    IReadOnlyList<FeatureAccess> Features,
    bool CanChangeFeatures);

/// <summary>
/// Where the licence comes from. The real app uses App Manager (<see cref="AppManagerLicenceSource"/>);
/// the web harness can swap in <see cref="InMemoryLicenceSource"/> from its configuration file so tests
/// never touch the shared App Manager account.
/// </summary>
public interface ILicenceSource
{
    /// <summary>Reads the licence, machines and features. Throws when the source cannot be reached.</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<LicenceSnapshot> ReadAsync(CancellationToken aCt = default);

    /// <summary>Forgets a machine and ends its sign-in.</summary>
    /// <param name="aDeviceId">The machine's id.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default);

    /// <summary>Blocks or unblocks a machine.</summary>
    /// <param name="aDeviceId">The machine's id.</param>
    /// <param name="aBlocked">Whether it should be blocked.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default);

    /// <summary>Switches a feature.</summary>
    /// <param name="aCode">The feature's code.</param>
    /// <param name="aOn">Whether it should be on.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default);
}
