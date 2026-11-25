using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Licences;

/// <summary><see cref="IAppManagerLicenceClient"/> over HTTP, against the same base address and headers as the sign-in client.</summary>
public sealed class AppManagerLicenceClient : IAppManagerLicenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient objHttp;
    private readonly ILogger<AppManagerLicenceClient> objLogger;

    /// <summary>Creates the client.</summary>
    /// <param name="aHttp">The <see cref="HttpClient"/> configured with App Manager's address and headers.</param>
    /// <param name="aLogger">Where an unreadable answer is logged.</param>
    public AppManagerLicenceClient(HttpClient aHttp, ILogger<AppManagerLicenceClient> aLogger)
    {
        objHttp = aHttp;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public Task<AppManagerCall<List<AppManagerLicence>>> GetLicencesAsync(string aAccessToken, CancellationToken aCt = default) =>
        SendAsync<List<AppManagerLicence>>(HttpMethod.Get, "LicenseSvc", aAccessToken, aCt);

    /// <inheritdoc />
    public Task<AppManagerCall<List<AppManagerDevice>>> GetDevicesAsync(string aAccessToken, CancellationToken aCt = default) =>
        SendAsync<List<AppManagerDevice>>(HttpMethod.Get, "AuthSvc/devices", aAccessToken, aCt);

    /// <inheritdoc />
    public Task<AppManagerCall<object>> ForgetDeviceAsync(string aAccessToken, long aUserDeviceId, CancellationToken aCt = default) =>
        SendAsync<object>(HttpMethod.Delete, $"AuthSvc/devices/{aUserDeviceId}", aAccessToken, aCt);

    /// <inheritdoc />
    public Task<AppManagerCall<object>> BlockDeviceAsync(string aAccessToken, long aUserDeviceId, CancellationToken aCt = default) =>
        SendAsync<object>(HttpMethod.Post, $"AuthSvc/devices/{aUserDeviceId}/block", aAccessToken, aCt);

    /// <inheritdoc />
    public Task<AppManagerCall<List<AppManagerFeature>>> GetFeaturesAsync(string aAccessToken, CancellationToken aCt = default) =>
        SendAsync<List<AppManagerFeature>>(HttpMethod.Get, "FeatureSvc", aAccessToken, aCt);

    private async Task<AppManagerCall<T>> SendAsync<T>(HttpMethod aMethod, string aPath, string aAccessToken, CancellationToken aCt)
    {
        using var vRequest = new HttpRequestMessage(aMethod, aPath);
        vRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aAccessToken);
        using var vResponse = await objHttp.SendAsync(vRequest, aCt).ConfigureAwait(false);
        var vStatus = (int)vResponse.StatusCode;
        var vJson = await vResponse.Content.ReadAsStringAsync(aCt).ConfigureAwait(false);
        try
        {
            var vEnvelope = JsonSerializer.Deserialize<Envelope<T>>(vJson, JsonOptions);
            if (vEnvelope is not null)
            {
                return new AppManagerCall<T>(vEnvelope.Success && vResponse.IsSuccessStatusCode, vStatus, vEnvelope.Message, vEnvelope.Data);
            }
        }
        catch (JsonException vException)
        {
            objLogger.LogWarning(vException, "App Manager returned an answer that could not be read");
        }

        return new AppManagerCall<T>(false, vStatus, vResponse.IsSuccessStatusCode ? "App Manager returned an unreadable answer." : null, default);
    }

    private sealed class Envelope<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Message { get; set; }
    }
}
