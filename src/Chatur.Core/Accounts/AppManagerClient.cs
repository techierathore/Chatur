using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Accounts;

/// <summary>
/// <see cref="IAppManagerClient"/> over HTTP, against the base address and API key headers
/// <c>ServiceCollectionExtensions</c> configures on the injected <see cref="HttpClient"/>
/// (docs/AppManager-api-usage-guide.md §3.1, REQ-FN-001, REQ-FN-002).
/// </summary>
public sealed class AppManagerClient : IAppManagerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The sign-in and register bodies: an <c>applicationId</c> that is not configured is left out, not sent as null.</summary>
    private static readonly JsonSerializerOptions AuthBodyOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient objHttp;
    private readonly int? objApplicationId;
    private readonly ILogger<AppManagerClient> objLogger;

    /// <summary>Creates the client.</summary>
    /// <param name="aHttp">The <see cref="HttpClient"/> configured with App Manager's base address and API key headers.</param>
    /// <param name="aConfiguration">
    /// Where <c>AppManager:ApplicationId</c> is read: Chatur's application on the production App
    /// Manager (id 1). It is sent on sign-in and register (API usage guide §1 "Option B"), so the
    /// account is scoped to Chatur even when no API key is configured, and App Manager refuses with
    /// <c>APP_ID_MISMATCH</c> when a key belongs to another application.
    /// </param>
    /// <param name="aLogger">Where a refused or unreachable call is logged.</param>
    public AppManagerClient(HttpClient aHttp, IConfiguration aConfiguration, ILogger<AppManagerClient> aLogger)
    {
        objHttp = aHttp;
        objApplicationId = int.TryParse(aConfiguration["AppManager:ApplicationId"], out var vId) && vId > 0 ? vId : null;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fetched fresh on every call, never cached: the guide says the key "only changes if the
    /// server's encryption keys are rotated", and a cached key after a rotation would encrypt against
    /// a key the server has already replaced and turn every later sign-in into a <c>DECRYPTION_FAILED</c>.
    /// </remarks>
    public async Task<string> GetPublicKeyAsync(CancellationToken aCt = default)
    {
        var vResponse = await objHttp.GetAsync("AuthSvc/public-key", aCt).ConfigureAwait(false);
        var vEnvelope = await ReadEnvelopeAsync<PublicKeyData>(vResponse, aCt).ConfigureAwait(false);
        if (!vEnvelope.Success || vEnvelope.Data is null || string.IsNullOrWhiteSpace(vEnvelope.Data.PublicKey))
        {
            throw new InvalidOperationException(vEnvelope.Message ?? "App Manager did not return a public key.");
        }

        return vEnvelope.Data.PublicKey;
    }

    private bool HasApiKey => objHttp.DefaultRequestHeaders.Contains("X-Api-Key");

    /// <inheritdoc />
    public Task<AppManagerAuthResult> DeviceLoginAsync(string aEmail, string aEncryptedPassword, AppManagerDeviceInfo aDevice, CancellationToken aCt = default)
    {
        var vBody = new Dictionary<string, object?>
        {
            ["email"] = aEmail,
            ["encryptedPassword"] = aEncryptedPassword,
            ["deviceInfo"] = ToDeviceInfoBody(aDevice)
        };
        if (!HasApiKey && objApplicationId.HasValue)
            vBody["applicationId"] = objApplicationId.Value;
        return PostAuthAsync("AuthSvc/device-login", vBody, aCt);
    }

    /// <inheritdoc />
    public Task<AppManagerAuthResult> DeviceRegisterAsync(string aEmail, string aEncryptedPassword, string aFirstName, string aLastName, AppManagerDeviceInfo aDevice, CancellationToken aCt = default)
    {
        var vBody = new Dictionary<string, object?>
        {
            ["email"] = aEmail,
            ["encryptedPassword"] = aEncryptedPassword,
            ["firstName"] = aFirstName,
            ["lastName"] = aLastName,
            ["deviceInfo"] = ToDeviceInfoBody(aDevice)
        };
        if (!HasApiKey && objApplicationId.HasValue)
            vBody["applicationId"] = objApplicationId.Value;
        return PostAuthAsync("AuthSvc/device-register", vBody, aCt);
    }

    /// <inheritdoc />
    public async Task<AppManagerAuthResult> RefreshAsync(string aRefreshToken, CancellationToken aCt = default)
    {
        var vResponse = await objHttp.PostAsJsonAsync("AuthSvc/refresh", new { refreshToken = aRefreshToken }, JsonOptions, aCt).ConfigureAwait(false);
        var vEnvelope = await ReadEnvelopeAsync<RefreshData>(vResponse, aCt).ConfigureAwait(false);
        if (!vEnvelope.Success || vEnvelope.Data is null)
        {
            return new AppManagerAuthResult(false, vEnvelope.Message ?? "Could not refresh the session.", null, null, null, null, null, null, null);
        }

        return new AppManagerAuthResult(true, null, null, null, null, null, vEnvelope.Data.AccessToken, vEnvelope.Data.RefreshToken, vEnvelope.Data.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string? aRefreshToken, bool aLogoutAllDevices, CancellationToken aCt = default)
    {
        var vBody = new { refreshToken = aRefreshToken, logoutAllDevices = aLogoutAllDevices };
        var vResponse = await objHttp.PostAsJsonAsync("AuthSvc/logout", vBody, JsonOptions, aCt).ConfigureAwait(false);
        if (!vResponse.IsSuccessStatusCode)
        {
            objLogger.LogWarning("App Manager logout returned {StatusCode}", vResponse.StatusCode);
        }
    }

    private async Task<AppManagerAuthResult> PostAuthAsync(string aPath, object aBody, CancellationToken aCt)
    {
        var vResponse = await objHttp.PostAsJsonAsync(aPath, aBody, AuthBodyOptions, aCt).ConfigureAwait(false);
        var vEnvelope = await ReadEnvelopeAsync<AuthData>(vResponse, aCt).ConfigureAwait(false);
        if (!vEnvelope.Success || vEnvelope.Data is null)
        {
            objLogger.LogInformation("App Manager refused {Path}: {Message}", aPath, vEnvelope.Message);
            return new AppManagerAuthResult(false, vEnvelope.Message ?? "App Manager refused the request.", null, null, null, null, null, null, null);
        }

        var vData = vEnvelope.Data;
        return new AppManagerAuthResult(true, null, vData.UserId, vData.Email, vData.FirstName, vData.LastName, vData.AccessToken, vData.RefreshToken, vData.TokenExpiresAt);
    }

    private static object ToDeviceInfoBody(AppManagerDeviceInfo aDevice) => new
    {
        deviceId = aDevice.DeviceId,
        deviceName = aDevice.DeviceName,
        deviceType = aDevice.DeviceType,
        platform = aDevice.Platform,
        appVersion = aDevice.AppVersion
    };

    private async Task<ApiEnvelope<T>> ReadEnvelopeAsync<T>(HttpResponseMessage aResponse, CancellationToken aCt)
    {
        var vJson = await aResponse.Content.ReadAsStringAsync(aCt).ConfigureAwait(false);
        try
        {
            var vEnvelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(vJson, JsonOptions);
            if (vEnvelope is not null)
            {
                return vEnvelope;
            }
        }
        catch (JsonException vException)
        {
            objLogger.LogWarning(vException, "App Manager returned a response that could not be parsed");
        }

        return new ApiEnvelope<T> { Success = false, Message = $"App Manager returned an unreadable response ({(int)aResponse.StatusCode})." };
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Message { get; set; }

        public string? Error { get; set; }
    }

    private sealed class PublicKeyData
    {
        public string PublicKey { get; set; } = string.Empty;
    }

    private sealed class AuthData
    {
        public long UserId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime TokenExpiresAt { get; set; }
    }

    private sealed class RefreshData
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
