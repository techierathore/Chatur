using Chatur.Core.Actions;
using Chatur.Core.Platform;

namespace Chatur.Core.Licences;

/// <summary>One machine the test licence starts with.</summary>
/// <param name="Id">The machine's id; also names its test ids (<c>forget-{Id}</c>).</param>
/// <param name="Name">The machine's name.</param>
/// <param name="Platform">The platform.</param>
/// <param name="AppVersion">The Chatur version last seen.</param>
/// <param name="DaysSinceSeen">How many days ago it was last seen.</param>
/// <param name="SignInCount">How many times it signed in.</param>
/// <param name="ThisDevice">Whether it is the machine the owner is on.</param>
/// <param name="Blocked">Whether it starts blocked.</param>
public sealed record TestLicenceDevice(string Id, string Name, string Platform, string AppVersion, int DaysSinceSeen, int SignInCount, bool ThisDevice, bool Blocked);

/// <summary>
/// The test licence: what the web harness's configuration file (<c>testLicence</c> in
/// <c>tests/verify/test-settings.json</c>) describes, held in memory. The real app never builds one.
/// </summary>
/// <param name="Name">The licence's name.</param>
/// <param name="State">Its state.</param>
/// <param name="Key">Its key.</param>
/// <param name="DaysRemaining">Days left.</param>
/// <param name="DaysTotal">Days in the whole term.</param>
/// <param name="DeviceCap">The device cap.</param>
/// <param name="OverCapWarning">The text the "server" sends while more machines are signed in than the cap.</param>
/// <param name="Devices">The machines.</param>
/// <param name="FeaturesOff">Codes of the features that start switched off.</param>
public sealed record TestLicenceOptions(
    string Name,
    string State,
    string Key,
    int DaysRemaining,
    int DaysTotal,
    int DeviceCap,
    string OverCapWarning,
    IReadOnlyList<TestLicenceDevice> Devices,
    IReadOnlyList<string> FeaturesOff);

/// <summary>
/// An <see cref="ILicenceSource"/> that holds one licence in memory, so a test can forget a machine or
/// switch a feature without touching the shared App Manager account. A forgotten machine can no longer
/// sign in (<see cref="CanSignIn"/>). <see cref="Reset"/> puts everything back as configured.
/// </summary>
public sealed class InMemoryLicenceSource : ILicenceSource
{
    private readonly object objLock = new();
    private readonly TestLicenceOptions objOptions;
    private readonly IClock objClock;
    private List<TestLicenceDevice> objDevices = [];
    private HashSet<string> objOff = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> objBlocked = new(StringComparer.Ordinal);

    /// <summary>Creates the source from the options a configuration file gave.</summary>
    /// <param name="aOptions">The licence to hold.</param>
    /// <param name="aClock">The clock the last-seen times are worked out against.</param>
    public InMemoryLicenceSource(TestLicenceOptions aOptions, IClock aClock)
    {
        objOptions = aOptions;
        objClock = aClock;
        Reset();
    }

    /// <summary>Puts the licence back as configured: every machine known again, blocks and feature switches as first set.</summary>
    public void Reset()
    {
        lock (objLock)
        {
            objDevices = objOptions.Devices.ToList();
            objBlocked = objOptions.Devices.Where(aDevice => aDevice.Blocked).Select(aDevice => aDevice.Id).ToHashSet(StringComparer.Ordinal);
            objOff = new HashSet<string>(objOptions.FeaturesOff, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Whether a machine could sign in now: it is known and not blocked.</summary>
    /// <param name="aDeviceId">The machine's id.</param>
    public bool CanSignIn(string aDeviceId)
    {
        lock (objLock)
        {
            return objDevices.Any(aDevice => aDevice.Id == aDeviceId) && !objBlocked.Contains(aDeviceId);
        }
    }

    /// <inheritdoc />
    public Task<LicenceSnapshot> ReadAsync(CancellationToken aCt = default)
    {
        lock (objLock)
        {
            var vNow = objClock.UtcNow;
            var vDevices = objDevices
                .Select(aDevice => new LicenceDevice(aDevice.Id, aDevice.Name, aDevice.Platform, aDevice.AppVersion, vNow.AddDays(-aDevice.DaysSinceSeen), aDevice.SignInCount, objBlocked.Contains(aDevice.Id), aDevice.ThisDevice))
                .ToList();
            var vSignedIn = vDevices.Count(aDevice => !aDevice.IsBlocked);
            var vInfo = new LicenceInfo(
                objOptions.Name,
                objOptions.State,
                objOptions.Key,
                vNow.Date.AddDays(objOptions.DaysRemaining),
                objOptions.DaysRemaining,
                vNow.Date.AddDays(objOptions.DaysRemaining - objOptions.DaysTotal),
                objOptions.DeviceCap);
            var vWarning = vSignedIn > objOptions.DeviceCap ? objOptions.OverCapWarning : null;
            var vFeatures = ChaturFeatures.All.Select(aFeature => new FeatureAccess(aFeature.Code, !objOff.Contains(aFeature.Code))).ToList();
            return Task.FromResult(new LicenceSnapshot(vInfo, vWarning, vDevices, vFeatures, CanChangeFeatures: true));
        }
    }

    /// <inheritdoc />
    public Task<LicenceActionResult> ForgetDeviceAsync(string aDeviceId, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            if (objDevices.RemoveAll(aDevice => aDevice.Id == aDeviceId) == 0)
            {
                return Task.FromResult(new LicenceActionResult(false, "That machine is not on the licence."));
            }

            objBlocked.Remove(aDeviceId);
            return Task.FromResult(new LicenceActionResult(true, null));
        }
    }

    /// <inheritdoc />
    public Task<LicenceActionResult> SetBlockedAsync(string aDeviceId, bool aBlocked, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            if (!objDevices.Any(aDevice => aDevice.Id == aDeviceId))
            {
                return Task.FromResult(new LicenceActionResult(false, "That machine is not on the licence."));
            }

            if (aBlocked)
            {
                objBlocked.Add(aDeviceId);
            }
            else
            {
                objBlocked.Remove(aDeviceId);
            }

            return Task.FromResult(new LicenceActionResult(true, null));
        }
    }

    /// <inheritdoc />
    public Task<LicenceActionResult> SetFeatureAsync(string aCode, bool aOn, CancellationToken aCt = default)
    {
        lock (objLock)
        {
            if (!ChaturFeatures.All.Any(aFeature => string.Equals(aFeature.Code, aCode, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(new LicenceActionResult(false, "Chatur has no such feature."));
            }

            if (aOn)
            {
                objOff.Remove(aCode);
            }
            else
            {
                objOff.Add(aCode);
            }

            return Task.FromResult(new LicenceActionResult(true, null));
        }
    }
}
