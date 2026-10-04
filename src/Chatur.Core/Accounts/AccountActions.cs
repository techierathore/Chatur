using System.Security.Cryptography;
using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// AppState lives directly under Chatur.Core; visible here without a using because this file's
// namespace, Chatur.Core.Accounts, nests inside it.

namespace Chatur.Core.Accounts;

/// <summary>
/// <see cref="IAccountActions"/> against App Manager (Architecture §1 Q4, §5 "Identity").
/// <see cref="SignInAsync"/>, <see cref="CurrentUserAsync"/> and <see cref="DeviceIdAsync"/> are
/// cluster B's rows: <c>POST /AuthSvc/device-login</c> through <see cref="IAppManagerClient"/> with
/// the RSA-encrypted password (REQ-FN-001) and this installation's own device id, generated once and
/// kept in the <c>Installation</c> table (REQ-FN-002); the session is persisted to
/// <see cref="ISecretStore"/> under the names <see cref="AccountSecretNames"/> lists so it survives a
/// restart, refreshing the access token once it has expired (REQ-FN-003). <see cref="RegisterAsync"/>
/// is cluster C's row and calls <c>AuthSvc/device-register</c> through the same
/// <see cref="IAppManagerClient"/>.
/// </summary>
public sealed class AccountActions : IAccountActions
{
    private readonly AppState objAppState;
    private readonly IConfiguration objConfiguration;
    private readonly ILogger<AccountActions> objLogger;
    private readonly IAppManagerClient objAppManagerClient;
    private readonly ISecretStore objSecretStore;
    private readonly IDbConnectionFactory objDb;
    private readonly IClock objClock;

    /// <summary>Creates the action with the shared app state, this machine's App Manager configuration and the App Manager client.</summary>
    public AccountActions(
        AppState aAppState,
        IConfiguration aConfiguration,
        ILogger<AccountActions> aLogger,
        IAppManagerClient aAppManagerClient,
        ISecretStore aSecretStore,
        IDbConnectionFactory aDb,
        IClock aClock)
    {
        objAppState = aAppState;
        objConfiguration = aConfiguration;
        objLogger = aLogger;
        objAppManagerClient = aAppManagerClient;
        objSecretStore = aSecretStore;
        objDb = aDb;
        objClock = aClock;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Sends this installation's device id and calls <c>POST /AuthSvc/device-login</c> through
    /// <see cref="IAppManagerClient"/>, which RSA-encrypts the password before it ever leaves the
    /// machine (REQ-FN-001) and carries the device id generated once on first run (REQ-FN-002). On
    /// success, records the signed-in owner in <see cref="AppState"/> so <see cref="CurrentUserAsync"/>
    /// and the route guard see it at once (REQ-UI-001), and — when <paramref name="aRemember"/> is
    /// <see langword="true"/> — persists the session to <see cref="ISecretStore"/> so it survives a
    /// restart (REQ-FN-003). On refusal, returns App Manager's own message verbatim (REQ-UI-002).
    /// </remarks>
    public async Task<SignInResult> SignInAsync(string aEmail, string aPassword, bool aRemember, CancellationToken aCt = default)
    {
        try
        {
            var vDeviceId = await DeviceIdAsync(aCt).ConfigureAwait(false);
            var vPublicKeyPem = await objAppManagerClient.GetPublicKeyAsync(aCt).ConfigureAwait(false);
            var vEncryptedPassword = EncryptPassword(aPassword, vPublicKeyPem);
            var vDevice = new AppManagerDeviceInfo(vDeviceId, Environment.MachineName, "Desktop", CurrentPlatformName(), CurrentAppVersion());

            var vResult = await objAppManagerClient.DeviceLoginAsync(aEmail, vEncryptedPassword, vDevice, aCt).ConfigureAwait(false);
            if (!vResult.Success || vResult.UserId is null || vResult.AccessToken is null || vResult.RefreshToken is null)
            {
                objLogger.LogInformation("Sign-in refused for {Email}", aEmail);
                return new SignInResult(false, vResult.ErrorMessage ?? "Invalid email or password", null);
            }

            var vUser = new CurrentUser(
                vResult.UserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                vResult.Email!,
                BuildDisplayName(vResult.FirstName, vResult.LastName, vResult.Email!));

            await PersistSessionAsync(vResult, aRemember, aCt).ConfigureAwait(false);
            objAppState.SetCurrentUser(vUser);
            return new SignInResult(true, null, vUser);
        }
        catch (Exception vException)
        {
            objLogger.LogError(vException, "Sign-in to App Manager failed");
            return new SignInResult(false, "Could not reach App Manager. Check your connection and try again.", null);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Refuses without a network call when <paramref name="aRequest"/>'s password breaks
    /// <see cref="PasswordRule"/> (REQ-FN-005 — Register itself checks the same rule before this is
    /// ever reached, but a caller that skips the screen still gets a refusal, never a bad request
    /// sent to App Manager). Otherwise sends this installation's device id and calls
    /// <c>POST /AuthSvc/device-register</c> through <see cref="IAppManagerClient"/>, the same client
    /// and the same RSA encryption <see cref="SignInAsync"/> uses (REQ-FN-001, REQ-FN-002, REQ-FN-004).
    /// On success, persists the session exactly as a remembered sign-in would (REQ-FN-003) and records
    /// the signed-in owner in <see cref="AppState"/> so the route guard opens Projects at once. On
    /// refusal — including an email already in use — returns App Manager's own message verbatim
    /// (REQ-UI-004).
    /// </remarks>
    public async Task<SignInResult> RegisterAsync(RegisterRequest aRequest, CancellationToken aCt = default)
    {
        if (!PasswordRule.Evaluate(aRequest.Password).MeetsRule)
        {
            return new SignInResult(false, "Password must be at least 8 characters with an uppercase letter, a number and a special character.", null);
        }

        try
        {
            var vDeviceId = await DeviceIdAsync(aCt).ConfigureAwait(false);
            var vPublicKeyPem = await objAppManagerClient.GetPublicKeyAsync(aCt).ConfigureAwait(false);
            var vEncryptedPassword = EncryptPassword(aRequest.Password, vPublicKeyPem);
            var vDevice = new AppManagerDeviceInfo(vDeviceId, Environment.MachineName, "Desktop", CurrentPlatformName(), CurrentAppVersion());

            var vResult = await objAppManagerClient.DeviceRegisterAsync(
                aRequest.Email, vEncryptedPassword, aRequest.FirstName, aRequest.LastName, vDevice, aCt).ConfigureAwait(false);
            if (!vResult.Success || vResult.UserId is null || vResult.AccessToken is null || vResult.RefreshToken is null)
            {
                objLogger.LogInformation("Registration refused for {Email}", aRequest.Email);
                return new SignInResult(false, vResult.ErrorMessage ?? "Could not create the account.", null);
            }

            var vUser = new CurrentUser(
                vResult.UserId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                vResult.Email!,
                BuildDisplayName(vResult.FirstName, vResult.LastName, vResult.Email!));

            await PersistSessionAsync(vResult, aRemember: true, aCt).ConfigureAwait(false);
            objAppState.SetCurrentUser(vUser);
            return new SignInResult(true, null, vUser);
        }
        catch (Exception vException)
        {
            objLogger.LogError(vException, "Registration with App Manager failed");
            return new SignInResult(false, "Could not reach App Manager. Check your connection and try again.", null);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Revokes this device's session at App Manager (<c>POST /AuthSvc/logout</c>) with whatever
    /// refresh token is on file, then removes the persisted token from <see cref="Platform.ISecretStore"/>
    /// and clears <see cref="AppState.CurrentUser"/>, so the route guard sends every window back to
    /// <c>/sign-in</c> at once (REQ-FN-009). The local token is always cleared, even when App Manager
    /// cannot be reached, so sign-out never leaves the owner stuck signed in on this machine (Coding
    /// Standards §Errors — a failure Chatur cannot control never blocks the owner's own action).
    /// </remarks>
    public async Task SignOutAsync(CancellationToken aCt = default)
    {
        var vRefreshToken = await objSecretStore.ReadAsync(AccountSecretNames.RefreshToken, aCt).ConfigureAwait(false);

        try
        {
            await objAppManagerClient.LogoutAsync(vRefreshToken, aLogoutAllDevices: false, aCt).ConfigureAwait(false);
        }
        catch (Exception vException)
        {
            objLogger.LogWarning(vException, "App Manager logout call failed; clearing the local token anyway.");
        }

        await ClearPersistedSessionAsync(aCt).ConfigureAwait(false);
        objAppState.SetCurrentUser(null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads <see cref="AppState.CurrentUser"/> first, which <see cref="SignInAsync"/> sets on
    /// success (REQ-UI-001) and the route guard checks on every layout but <c>BareLayout</c>. When
    /// nobody has signed in yet this process — the first screen after a restart — falls back to the
    /// session <see cref="SignInAsync"/> persisted to <see cref="ISecretStore"/> (REQ-FN-003):
    /// refreshes the access token first when it has expired, and clears the stored session and
    /// returns <see langword="null"/> (sending the owner back to Sign in) when the refresh token
    /// itself no longer works, rather than ever bypassing sign-in.
    /// </remarks>
    public async Task<CurrentUser?> CurrentUserAsync(CancellationToken aCt = default)
    {
        if (objAppState.CurrentUser is not null)
        {
            return objAppState.CurrentUser;
        }

        var vAccessToken = await objSecretStore.ReadAsync(AccountSecretNames.AccessToken, aCt).ConfigureAwait(false);
        var vRefreshToken = await objSecretStore.ReadAsync(AccountSecretNames.RefreshToken, aCt).ConfigureAwait(false);
        var vUserId = await objSecretStore.ReadAsync(AccountSecretNames.UserId, aCt).ConfigureAwait(false);
        var vEmail = await objSecretStore.ReadAsync(AccountSecretNames.Email, aCt).ConfigureAwait(false);

        if (vAccessToken is null || vRefreshToken is null || vUserId is null || vEmail is null)
        {
            return null;
        }

        var vDisplayName = await objSecretStore.ReadAsync(AccountSecretNames.DisplayName, aCt).ConfigureAwait(false);
        var vExpiresAtRaw = await objSecretStore.ReadAsync(AccountSecretNames.TokenExpiresAtUtc, aCt).ConfigureAwait(false);
        var vExpiresAt = DateTime.TryParse(
            vExpiresAtRaw,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var vParsedExpiry)
                ? vParsedExpiry
                : DateTime.MinValue;

        if (vExpiresAt <= objClock.UtcNow)
        {
            var vRefreshed = await objAppManagerClient.RefreshAsync(vRefreshToken, aCt).ConfigureAwait(false);
            if (!vRefreshed.Success || vRefreshed.AccessToken is null)
            {
                objLogger.LogInformation("The stored session could not be refreshed; the owner will need to sign in again.");
                await ClearPersistedSessionAsync(aCt).ConfigureAwait(false);
                return null;
            }

            await objSecretStore.SaveAsync(AccountSecretNames.AccessToken, vRefreshed.AccessToken, aCt).ConfigureAwait(false);
            if (vRefreshed.RefreshToken is not null)
            {
                await objSecretStore.SaveAsync(AccountSecretNames.RefreshToken, vRefreshed.RefreshToken, aCt).ConfigureAwait(false);
            }

            var vNewExpiry = vRefreshed.TokenExpiresAtUtc ?? objClock.UtcNow.AddHours(1);
            await objSecretStore.SaveAsync(AccountSecretNames.TokenExpiresAtUtc, vNewExpiry.ToString("O"), aCt).ConfigureAwait(false);
        }

        var vUser = new CurrentUser(vUserId, vEmail, string.IsNullOrWhiteSpace(vDisplayName) ? vEmail : vDisplayName);
        objAppState.SetCurrentUser(vUser);
        return vUser;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This installation's own <c>Installation.DeviceId</c> row (Architecture §4), generated once
    /// with a new GUID and kept forever afterwards (REQ-FN-002): read first, and only created when no
    /// row exists yet.
    /// </remarks>
    public async Task<string> DeviceIdAsync(CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();

        var vExisting = await vConnection.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition("SELECT DeviceId FROM Installation ORDER BY InstallationId LIMIT 1", cancellationToken: aCt)).ConfigureAwait(false);
        if (vExisting is not null)
        {
            return vExisting;
        }

        var vDeviceId = Guid.NewGuid().ToString("N");
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO Installation (DeviceId, CreatedUtc) VALUES (@DeviceId, @CreatedUtc)",
                new { DeviceId = vDeviceId, CreatedUtc = objClock.UtcNow.ToString("O") },
                cancellationToken: aCt)).ConfigureAwait(false);

        return vDeviceId;
    }

    /// <summary>Writes the signed-in session to <see cref="ISecretStore"/> (REQ-FN-003), or clears any session already on file when the owner did not ask to be remembered.</summary>
    private async Task PersistSessionAsync(AppManagerAuthResult aResult, bool aRemember, CancellationToken aCt)
    {
        if (!aRemember)
        {
            await ClearPersistedSessionAsync(aCt).ConfigureAwait(false);
            return;
        }

        await objSecretStore.SaveAsync(AccountSecretNames.AccessToken, aResult.AccessToken!, aCt).ConfigureAwait(false);
        await objSecretStore.SaveAsync(AccountSecretNames.RefreshToken, aResult.RefreshToken!, aCt).ConfigureAwait(false);
        await objSecretStore.SaveAsync(AccountSecretNames.UserId, aResult.UserId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), aCt).ConfigureAwait(false);
        await objSecretStore.SaveAsync(AccountSecretNames.Email, aResult.Email!, aCt).ConfigureAwait(false);
        await objSecretStore.SaveAsync(AccountSecretNames.DisplayName, BuildDisplayName(aResult.FirstName, aResult.LastName, aResult.Email!), aCt).ConfigureAwait(false);

        var vExpiresAt = aResult.TokenExpiresAtUtc ?? objClock.UtcNow.AddHours(1);
        await objSecretStore.SaveAsync(AccountSecretNames.TokenExpiresAtUtc, vExpiresAt.ToString("O"), aCt).ConfigureAwait(false);
    }

    /// <summary>Removes every name <see cref="AccountSecretNames"/> lists from <see cref="ISecretStore"/> (REQ-FN-009, and REQ-FN-003 when "remember" is off).</summary>
    private async Task ClearPersistedSessionAsync(CancellationToken aCt)
    {
        await objSecretStore.DeleteAsync(AccountSecretNames.AccessToken, aCt).ConfigureAwait(false);
        await objSecretStore.DeleteAsync(AccountSecretNames.RefreshToken, aCt).ConfigureAwait(false);
        await objSecretStore.DeleteAsync(AccountSecretNames.UserId, aCt).ConfigureAwait(false);
        await objSecretStore.DeleteAsync(AccountSecretNames.Email, aCt).ConfigureAwait(false);
        await objSecretStore.DeleteAsync(AccountSecretNames.DisplayName, aCt).ConfigureAwait(false);
        await objSecretStore.DeleteAsync(AccountSecretNames.TokenExpiresAtUtc, aCt).ConfigureAwait(false);
    }

    private static string BuildDisplayName(string? aFirstName, string? aLastName, string aEmail)
    {
        var vName = $"{aFirstName} {aLastName}".Trim();
        return vName.Length > 0 ? vName : aEmail;
    }

    private static string CurrentPlatformName() =>
        OperatingSystem.IsWindows() ? "Windows" :
        OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? "macOS" :
        "Other";

    private static string CurrentAppVersion() =>
        typeof(AccountActions).Assembly.GetName().Version?.ToString() ?? "0.0.0";

    private static string EncryptPassword(string aPassword, string aPublicKeyPem)
    {
        using var vRsa = RSA.Create();
        vRsa.ImportFromPem(aPublicKeyPem);
        var vEncryptedBytes = vRsa.Encrypt(Encoding.UTF8.GetBytes(aPassword), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(vEncryptedBytes);
    }
}
