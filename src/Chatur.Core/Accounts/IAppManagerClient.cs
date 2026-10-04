namespace Chatur.Core.Accounts;

/// <summary>
/// The device signing in or registering, sent on every App Manager v1.5 device endpoint
/// (docs/AppManager-api-usage-guide.md §3.1, REQ-FN-002). <see cref="DeviceId"/> is this
/// installation's own identifier, generated once and kept forever; the rest is descriptive.
/// </summary>
/// <param name="DeviceId">This installation's own identifier (Architecture §4 <c>Installation.DeviceId</c>).</param>
/// <param name="DeviceName">A display name for the user's device list, e.g. the machine name.</param>
/// <param name="DeviceType">E.g. <c>"Desktop"</c>.</param>
/// <param name="Platform">E.g. <c>"Windows"</c> or <c>"macOS"</c>.</param>
/// <param name="AppVersion">Chatur's own version string.</param>
public sealed record AppManagerDeviceInfo(string DeviceId, string? DeviceName, string? DeviceType, string? Platform, string? AppVersion);

/// <summary>
/// What App Manager's <c>device-login</c>, <c>device-register</c> and <c>refresh</c> endpoints hand
/// back (docs/AppManager-api-usage-guide.md §3.1). Every field but <see cref="Success"/> and
/// <see cref="ErrorMessage"/> is <see langword="null"/> on a refusal.
/// </summary>
/// <param name="Success">Whether App Manager accepted the call.</param>
/// <param name="ErrorMessage">App Manager's own <c>message</c>, verbatim, when it refused (REQ-UI-002).</param>
/// <param name="UserId">App Manager's identifier for the account.</param>
/// <param name="Email">The account's email address.</param>
/// <param name="FirstName">The account's first name.</param>
/// <param name="LastName">The account's last name.</param>
/// <param name="AccessToken">The bearer token for subsequent calls.</param>
/// <param name="RefreshToken">The token used to obtain a new access token once this one expires.</param>
/// <param name="TokenExpiresAtUtc">When <see cref="AccessToken"/> stops being valid.</param>
public sealed record AppManagerAuthResult(
    bool Success,
    string? ErrorMessage,
    long? UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    string? AccessToken,
    string? RefreshToken,
    DateTime? TokenExpiresAtUtc);

/// <summary>
/// The App Manager calls Accounts needs: the public key for password encryption, the two device
/// endpoints, refresh and logout (docs/AppManager-api-usage-guide.md §3.1, Architecture §1 Q4). One
/// <see cref="System.Net.Http.HttpClient"/>, configured once in <c>ServiceCollectionExtensions</c>
/// from <c>AppManager:BaseUrl</c> / <c>ApiKey</c> / <c>ApiSecret</c> — cluster C's Register
/// (REQ-FN-004) reuses this same client rather than opening a second one.
/// </summary>
public interface IAppManagerClient
{
    /// <summary>
    /// Fetches (and caches for the life of this instance) the server's RSA public key used to
    /// encrypt a password before it ever leaves the machine (REQ-FN-001).
    /// </summary>
    /// <param name="aCt">A token that cancels the call.</param>
    /// <returns>The PEM-encoded public key.</returns>
    Task<string> GetPublicKeyAsync(CancellationToken aCt = default);

    /// <summary>Signs in from this device (<c>POST /AuthSvc/device-login</c>).</summary>
    /// <param name="aEmail">The email address typed on Sign in.</param>
    /// <param name="aEncryptedPassword">The password, already RSA-OAEP-SHA256 encrypted and base64-encoded.</param>
    /// <param name="aDevice">This installation's device identity.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerAuthResult> DeviceLoginAsync(string aEmail, string aEncryptedPassword, AppManagerDeviceInfo aDevice, CancellationToken aCt = default);

    /// <summary>Registers a new account from this device (<c>POST /AuthSvc/device-register</c>).</summary>
    /// <param name="aEmail">The email address typed on Register.</param>
    /// <param name="aEncryptedPassword">The password, already RSA-OAEP-SHA256 encrypted and base64-encoded.</param>
    /// <param name="aFirstName">The first name typed on Register.</param>
    /// <param name="aLastName">The last name typed on Register.</param>
    /// <param name="aDevice">This installation's device identity.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerAuthResult> DeviceRegisterAsync(string aEmail, string aEncryptedPassword, string aFirstName, string aLastName, AppManagerDeviceInfo aDevice, CancellationToken aCt = default);

    /// <summary>Exchanges a refresh token for a new access token (<c>POST /AuthSvc/refresh</c>).</summary>
    /// <param name="aRefreshToken">The refresh token stored from a previous sign-in.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<AppManagerAuthResult> RefreshAsync(string aRefreshToken, CancellationToken aCt = default);

    /// <summary>Signs out (<c>POST /AuthSvc/logout</c>), so the stored token no longer opens the app (REQ-FN-009).</summary>
    /// <param name="aRefreshToken">The session's refresh token, when one was stored; revokes only this device.</param>
    /// <param name="aLogoutAllDevices">Whether to revoke every device's session for this user.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task LogoutAsync(string? aRefreshToken, bool aLogoutAllDevices, CancellationToken aCt = default);
}
