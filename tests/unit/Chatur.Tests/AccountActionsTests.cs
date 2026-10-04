using System.Security.Cryptography;
using System.Text;
using Chatur.Core;
using Chatur.Core.Accounts;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using ChaturDb;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="AccountActions"/>'s cluster B slice: the RSA-encrypted device sign-in
/// (REQ-FN-001), this installation's own device id (REQ-FN-002) and the session that survives a
/// restart (REQ-FN-003), against a real, temporary SQLite database and a fake
/// <see cref="IAppManagerClient"/>.
/// </summary>
public sealed class AccountActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-account-tests-{Guid.NewGuid():N}.db");
    private readonly RSA objRsa = RSA.Create();

    /// <summary>Runs the real migrations against a fresh temporary database before every test.</summary>
    public AccountActionsTests()
    {
        var vResult = ChaturDbMigrator.Migrate($"Data Source={objDatabasePath}");
        Assert.True(vResult.Successful, vResult.Error?.Message);
    }

    /// <summary>
    /// When Chatur signs in, then the password App Manager receives is RSA-encrypted, not the
    /// plain text typed on Sign in.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-001 the password App Manager receives is encrypted, never the plain text")]
    public async Task SignInAsyncNeverSendsThePlainPasswordOnTheWire()
    {
        var vAppManager = new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() };
        var vActions = CreateActions(vAppManager, new FakeSecretStore(), new FakeClock());

        await vActions.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);

        Assert.NotNull(vAppManager.LastEncryptedPassword);
        Assert.NotEqual("Sup3r$ecret!", vAppManager.LastEncryptedPassword);

        var vDecryptedBytes = objRsa.Decrypt(Convert.FromBase64String(vAppManager.LastEncryptedPassword!), RSAEncryptionPadding.OaepSHA256);
        Assert.Equal("Sup3r$ecret!", Encoding.UTF8.GetString(vDecryptedBytes));
    }

    /// <summary>
    /// When Chatur signs in more than once, then it sends the device identifier it generated on
    /// first run every time — never a fresh one per call.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-002 every sign-in sends the same device id")]
    public async Task SignInAsyncSendsTheSameDeviceIdEveryTime()
    {
        var vAppManager = new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() };
        var vActions = CreateActions(vAppManager, new FakeSecretStore(), new FakeClock());

        await vActions.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);
        var vFirstDeviceId = vAppManager.LastDevice!.DeviceId;

        await vActions.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);
        var vSecondDeviceId = vAppManager.LastDevice!.DeviceId;

        Assert.False(string.IsNullOrWhiteSpace(vFirstDeviceId));
        Assert.Equal(vFirstDeviceId, vSecondDeviceId);
    }

    /// <summary>
    /// When the installation is asked for its device id across what a restart would be — a fresh
    /// <see cref="AccountActions"/> over the same database file — then it is the same id as before.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-002 the device id survives a restart")]
    public async Task DeviceIdAsyncKeepsTheSameIdAcrossARestart()
    {
        var vFirstRunId = await CreateActions(new FakeAppManagerClient(objRsa), new FakeSecretStore(), new FakeClock()).DeviceIdAsync();
        var vSecondRunId = await CreateActions(new FakeAppManagerClient(objRsa), new FakeSecretStore(), new FakeClock()).DeviceIdAsync();

        Assert.Equal(vFirstRunId, vSecondRunId);
    }

    /// <summary>
    /// When the owner leaves "remember this device" on and signs in, then the session is written to
    /// the secret store so it can survive a restart.
    /// </summary>
    [Fact]
    public async Task SignInAsyncPersistsTheSessionWhenRemembered()
    {
        var vSecrets = new FakeSecretStore();
        var vActions = CreateActions(new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() }, vSecrets, new FakeClock());

        await vActions.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);

        Assert.Equal("access-token", await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
        Assert.Equal("refresh-token", await vSecrets.ReadAsync(AccountSecretNames.RefreshToken));
        Assert.Equal("owner@example.com", await vSecrets.ReadAsync(AccountSecretNames.Email));
    }

    /// <summary>
    /// When the owner turns "remember this device" off and signs in, then nothing is left in the
    /// secret store for a later restart to pick up.
    /// </summary>
    [Fact]
    public async Task SignInAsyncLeavesNoSessionWhenNotRemembered()
    {
        var vSecrets = new FakeSecretStore();
        var vActions = CreateActions(new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() }, vSecrets, new FakeClock());

        await vActions.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: false);

        Assert.Null(await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
    }

    /// <summary>
    /// When the owner closes Chatur while signed in and opens it again, then Projects opens without
    /// asking for the password — <see cref="AccountActions.CurrentUserAsync"/> rebuilds the owner
    /// from the persisted session, with no fresh <c>AppState</c> in this "process" ever having seen
    /// a sign-in.
    /// </summary>
    [Fact]
    public async Task CurrentUserAsyncRestoresTheSessionAfterARestart()
    {
        var vSecrets = new FakeSecretStore();
        var vClock = new FakeClock();
        var vFirstProcess = CreateActions(new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() }, vSecrets, vClock);
        await vFirstProcess.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);

        var vSecondAppManager = new FakeAppManagerClient(objRsa);
        var vSecondProcess = CreateActions(vSecondAppManager, vSecrets, vClock);

        var vUser = await vSecondProcess.CurrentUserAsync();

        Assert.NotNull(vUser);
        Assert.Equal("owner@example.com", vUser!.Email);
        Assert.Null(vSecondAppManager.LastRefreshedToken);
    }

    /// <summary>
    /// When the owner signs out, then the stored token is gone, so a new process (a restart) finds
    /// nobody signed in and the token no longer opens the app (REQ-FN-009).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-009 signing out removes the stored token so it no longer opens the app")]
    public async Task SignOutAsyncRemovesTheStoredToken()
    {
        var vSecrets = new FakeSecretStore();
        var vClock = new FakeClock();
        var vFirstProcess = CreateActions(new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult() }, vSecrets, vClock);
        await vFirstProcess.SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);
        Assert.NotNull(await vSecrets.ReadAsync(AccountSecretNames.RefreshToken));

        await vFirstProcess.SignOutAsync();

        Assert.Null(await vFirstProcess.CurrentUserAsync());
        Assert.Null(await vSecrets.ReadAsync(AccountSecretNames.RefreshToken));
        Assert.Null(await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
        var vSecondProcess = CreateActions(new FakeAppManagerClient(objRsa), vSecrets, vClock);
        Assert.Null(await vSecondProcess.CurrentUserAsync());
    }

    /// <summary>
    /// When the restored session's access token has expired, then <see cref="AccountActions.CurrentUserAsync"/>
    /// refreshes it and writes the new token back to the secret store, rather than asking to sign in
    /// again while the refresh token is still good.
    /// </summary>
    [Fact]
    public async Task CurrentUserAsyncRefreshesAnExpiredAccessToken()
    {
        var vSecrets = new FakeSecretStore();
        var vClock = new FakeClock { UtcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var vFirstAppManager = new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult(vClock.UtcNow.AddMinutes(1)) };
        await CreateActions(vFirstAppManager, vSecrets, vClock).SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);

        vClock.UtcNow = vClock.UtcNow.AddHours(1);
        var vSecondAppManager = new FakeAppManagerClient(objRsa)
        {
            RefreshResult = new AppManagerAuthResult(true, null, null, null, null, null, "refreshed-access-token", "refreshed-refresh-token", vClock.UtcNow.AddHours(1))
        };

        var vUser = await CreateActions(vSecondAppManager, vSecrets, vClock).CurrentUserAsync();

        Assert.NotNull(vUser);
        Assert.Equal("refresh-token", vSecondAppManager.LastRefreshedToken);
        Assert.Equal("refreshed-access-token", await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
    }

    /// <summary>
    /// When the stored refresh token itself no longer works, then the persisted session is cleared
    /// and <see cref="AccountActions.CurrentUserAsync"/> returns <see langword="null"/> — sending the
    /// owner back to Sign in rather than ever bypassing it.
    /// </summary>
    [Fact]
    public async Task CurrentUserAsyncSignsOutWhenTheRefreshTokenNoLongerWorks()
    {
        var vSecrets = new FakeSecretStore();
        var vClock = new FakeClock { UtcNow = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var vFirstAppManager = new FakeAppManagerClient(objRsa) { LoginResult = SuccessResult(vClock.UtcNow.AddMinutes(1)) };
        await CreateActions(vFirstAppManager, vSecrets, vClock).SignInAsync("owner@example.com", "Sup3r$ecret!", aRemember: true);

        vClock.UtcNow = vClock.UtcNow.AddHours(1);
        var vSecondAppManager = new FakeAppManagerClient(objRsa)
        {
            RefreshResult = new AppManagerAuthResult(false, "Refresh token has expired", null, null, null, null, null, null, null)
        };

        var vUser = await CreateActions(vSecondAppManager, vSecrets, vClock).CurrentUserAsync();

        Assert.Null(vUser);
        Assert.Null(await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
    }

    /// <summary>
    /// When the password is wrong, then Sign in shows App Manager's own message and nothing is
    /// stored.
    /// </summary>
    [Fact]
    public async Task SignInAsyncReturnsAppManagersOwnMessageOnRefusal()
    {
        var vAppManager = new FakeAppManagerClient(objRsa)
        {
            LoginResult = new AppManagerAuthResult(false, "Invalid email or password", null, null, null, null, null, null, null)
        };
        var vActions = CreateActions(vAppManager, new FakeSecretStore(), new FakeClock());

        var vResult = await vActions.SignInAsync("owner@example.com", "wrong", aRemember: true);

        Assert.False(vResult.Succeeded);
        Assert.Equal("Invalid email or password", vResult.ErrorMessage);
    }

    /// <summary>Deletes the temporary database file and disposes the shared RSA key this test class created.</summary>
    public void Dispose()
    {
        objRsa.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }

    private AccountActions CreateActions(IAppManagerClient aAppManagerClient, ISecretStore aSecretStore, IClock aClock) =>
        new(
            new AppState(),
            new ConfigurationBuilder().Build(),
            NullLogger<AccountActions>.Instance,
            aAppManagerClient,
            aSecretStore,
            new SqliteConnectionFactory(new FixedAppPaths(objDatabasePath)),
            aClock);

    private static AppManagerAuthResult SuccessResult(DateTime? aExpiresAtUtc = null) =>
        new(true, null, 42, "owner@example.com", "The", "Owner", "access-token", "refresh-token", aExpiresAtUtc ?? DateTime.UtcNow.AddHours(1));

    private sealed class FixedAppPaths : IAppPaths
    {
        public FixedAppPaths(string aDatabasePath) => DatabasePath = aDatabasePath;

        public string AppDataFolder => Path.GetDirectoryName(DatabasePath) ?? ".";

        public string DatabasePath { get; }

        public string LogFolder => AppDataFolder;
    }

    private sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    }

    private sealed class FakeSecretStore : ISecretStore
    {
        private readonly Dictionary<string, string> objSecrets = new();

        public Task SaveAsync(string aName, string aSecret, CancellationToken aCt = default)
        {
            objSecrets[aName] = aSecret;
            return Task.CompletedTask;
        }

        public Task<string?> ReadAsync(string aName, CancellationToken aCt = default) =>
            Task.FromResult(objSecrets.TryGetValue(aName, out var vValue) ? vValue : null);

        public Task DeleteAsync(string aName, CancellationToken aCt = default)
        {
            objSecrets.Remove(aName);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAppManagerClient : IAppManagerClient
    {
        private readonly RSA objRsa;

        public FakeAppManagerClient(RSA aRsa) => objRsa = aRsa;

        public AppManagerAuthResult LoginResult { get; set; } = new(false, "not configured", null, null, null, null, null, null, null);

        public AppManagerAuthResult RefreshResult { get; set; } = new(false, "not configured", null, null, null, null, null, null, null);

        public string? LastEncryptedPassword { get; private set; }

        public AppManagerDeviceInfo? LastDevice { get; private set; }

        public string? LastRefreshedToken { get; private set; }

        public Task<string> GetPublicKeyAsync(CancellationToken aCt = default) =>
            Task.FromResult(objRsa.ExportSubjectPublicKeyInfoPem());

        public Task<AppManagerAuthResult> DeviceLoginAsync(string aEmail, string aEncryptedPassword, AppManagerDeviceInfo aDevice, CancellationToken aCt = default)
        {
            LastEncryptedPassword = aEncryptedPassword;
            LastDevice = aDevice;
            return Task.FromResult(LoginResult);
        }

        public Task<AppManagerAuthResult> DeviceRegisterAsync(string aEmail, string aEncryptedPassword, string aFirstName, string aLastName, AppManagerDeviceInfo aDevice, CancellationToken aCt = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<AppManagerAuthResult> RefreshAsync(string aRefreshToken, CancellationToken aCt = default)
        {
            LastRefreshedToken = aRefreshToken;
            return Task.FromResult(RefreshResult);
        }

        public Task LogoutAsync(string? aRefreshToken, bool aLogoutAllDevices, CancellationToken aCt = default) => Task.CompletedTask;
    }
}
