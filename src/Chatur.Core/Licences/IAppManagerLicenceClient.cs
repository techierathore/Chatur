namespace Chatur.Core.Licences;

/// <summary>What one App Manager call returned.</summary>
/// <typeparam name="T">The shape of <c>data</c>.</typeparam>
/// <param name="Ok">Whether App Manager accepted the call.</param>
/// <param name="Status">The HTTP status.</param>
/// <param name="Message">App Manager's own message, or <see langword="null"/>.</param>
/// <param name="Data">The <c>data</c> on success, or <see langword="null"/>.</param>
public sealed record AppManagerCall<T>(bool Ok, int Status, string? Message, T? Data);

/// <summary>A licence as <c>GET /LicenseSvc</c> returns it.</summary>
public sealed class AppManagerLicence
{
    /// <summary>The licence name.</summary>
    public string? LicenseName { get; set; }

    /// <summary>The licence key.</summary>
    public string? LicenseKey { get; set; }

    /// <summary>The status, e.g. Active.</summary>
    public string? Status { get; set; }

    /// <summary>When it was activated.</summary>
    public DateTime? ActivationDate { get; set; }

    /// <summary>When it expires.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Days left; never below 0.</summary>
    public int? DaysRemaining { get; set; }
}

/// <summary>A device as <c>GET /AuthSvc/devices</c> returns it.</summary>
public sealed class AppManagerDevice
{
    /// <summary>App Manager's id for the device.</summary>
    public long UserDeviceId { get; set; }

    /// <summary>The id the installation declared.</summary>
    public string? DeviceIdentifier { get; set; }

    /// <summary>The device's name.</summary>
    public string? DeviceName { get; set; }

    /// <summary>The platform.</summary>
    public string? Platform { get; set; }

    /// <summary>The Chatur version.</summary>
    public string? AppVersion { get; set; }

    /// <summary>Sign-ins from it.</summary>
    public int SignInCount { get; set; }

    /// <summary>When it was last seen.</summary>
    public DateTime? LastSeenDate { get; set; }

    /// <summary>Whether it is blocked.</summary>
    public bool IsBlocked { get; set; }
}

/// <summary>A feature as <c>GET /FeatureSvc</c> returns it.</summary>
public sealed class AppManagerFeature
{
    /// <summary>The feature code.</summary>
    public string? FeatureCode { get; set; }

    /// <summary>Whether this user has it.</summary>
    public bool HasAccess { get; set; }
}

/// <summary>The App Manager calls the Licence screen needs (docs/AppManager-api-usage-guide.md §3.1, §3.2, §3.4).</summary>
public interface IAppManagerLicenceClient
{
    /// <summary>Lists this account's licences (<c>GET /LicenseSvc</c>).</summary>
    /// <param name="aAccessToken">The bearer token.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerCall<List<AppManagerLicence>>> GetLicencesAsync(string aAccessToken, CancellationToken aCt = default);

    /// <summary>Lists this account's devices (<c>GET /AuthSvc/devices</c>).</summary>
    /// <param name="aAccessToken">The bearer token.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerCall<List<AppManagerDevice>>> GetDevicesAsync(string aAccessToken, CancellationToken aCt = default);

    /// <summary>Forgets a device (<c>DELETE /AuthSvc/devices/{id}</c>).</summary>
    /// <param name="aAccessToken">The bearer token.</param>
    /// <param name="aUserDeviceId">App Manager's id for the device.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerCall<object>> ForgetDeviceAsync(string aAccessToken, long aUserDeviceId, CancellationToken aCt = default);

    /// <summary>Blocks a device (<c>POST /AuthSvc/devices/{id}/block</c>).</summary>
    /// <param name="aAccessToken">The bearer token.</param>
    /// <param name="aUserDeviceId">App Manager's id for the device.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerCall<object>> BlockDeviceAsync(string aAccessToken, long aUserDeviceId, CancellationToken aCt = default);

    /// <summary>Lists the features and this user's access (<c>GET /FeatureSvc</c>).</summary>
    /// <param name="aAccessToken">The bearer token.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerCall<List<AppManagerFeature>>> GetFeaturesAsync(string aAccessToken, CancellationToken aCt = default);
}
