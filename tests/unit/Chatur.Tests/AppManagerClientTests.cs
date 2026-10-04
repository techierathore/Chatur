using System.Net;
using System.Text;
using Chatur.Core.Accounts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="AppManagerClient"/> against a fake <see cref="HttpMessageHandler"/> — no
/// real network call, just App Manager's own envelope shape (docs/AppManager-api-usage-guide.md §1)
/// parsed the way REQ-FN-001/REQ-FN-002/REQ-FN-003 need it.
/// </summary>
public sealed class AppManagerClientTests
{
    /// <summary>
    /// When <c>AuthSvc/public-key</c> is asked for twice, then it is fetched fresh both times —
    /// never cached — because the local development App Manager's key is ephemeral and a cached key
    /// would encrypt against one the server has already replaced.
    /// </summary>
    [Fact]
    public async Task GetPublicKeyAsyncNeverCachesAcrossCalls()
    {
        var vHandler = new FakeHandler(_ => JsonResponse(
            """{"success":true,"data":{"publicKey":"-----BEGIN PUBLIC KEY-----\nabc\n-----END PUBLIC KEY-----","algorithm":"RSA-OAEP-256","encoding":"base64"},"message":null}"""));
        var vClient = CreateClient(vHandler);

        var vFirst = await vClient.GetPublicKeyAsync();
        var vSecond = await vClient.GetPublicKeyAsync();

        Assert.Contains("BEGIN PUBLIC KEY", vFirst);
        Assert.Equal(vFirst, vSecond);
        Assert.Equal(2, vHandler.CallCount);
    }

    /// <summary>
    /// When <c>AuthSvc/device-login</c> refuses with App Manager's own <c>message</c>, then
    /// <see cref="AppManagerAuthResult.ErrorMessage"/> carries that message verbatim (REQ-UI-002).
    /// </summary>
    [Fact]
    public async Task DeviceLoginAsyncReturnsAppManagersOwnRefusalMessage()
    {
        var vHandler = new FakeHandler(_ => JsonResponse(
            """{"success":false,"error":"INVALID_CREDENTIALS","message":"Invalid email or password","statusCode":401}""",
            HttpStatusCode.Unauthorized));
        var vClient = CreateClient(vHandler);

        var vResult = await vClient.DeviceLoginAsync("owner@example.com", "encrypted", new AppManagerDeviceInfo("device-1", null, null, null, null));

        Assert.False(vResult.Success);
        Assert.Equal("Invalid email or password", vResult.ErrorMessage);
    }

    /// <summary>
    /// When <c>AuthSvc/device-login</c> succeeds, then the device id sent on the request body is
    /// exactly the one <see cref="AppManagerDeviceInfo"/> carried (REQ-FN-002), and the returned
    /// tokens and user fields come from the response's <c>data</c>.
    /// </summary>
    [Fact]
    public async Task DeviceLoginAsyncSendsTheDeviceIdAndParsesTheTokens()
    {
        var vHandler = new FakeHandler(aRequest => JsonResponse(
            """{"success":true,"data":{"userId":123,"email":"owner@example.com","firstName":"The","lastName":"Owner","accessToken":"at","refreshToken":"rt","tokenExpiresAt":"2026-01-26T14:00:00Z"},"message":"Login successful"}"""));
        var vClient = CreateClient(vHandler);

        var vResult = await vClient.DeviceLoginAsync("owner@example.com", "encrypted", new AppManagerDeviceInfo("device-42", "Test box", "Desktop", "Windows", "1.0.0"));

        Assert.True(vResult.Success);
        Assert.Equal(123, vResult.UserId);
        Assert.Equal("at", vResult.AccessToken);
        Assert.Equal("rt", vResult.RefreshToken);
        Assert.Contains("\"deviceId\":\"device-42\"", vHandler.LastRequestBody);
    }

    private static AppManagerClient CreateClient(HttpMessageHandler aHandler)
    {
        var vHttp = new HttpClient(aHandler) { BaseAddress = new Uri("https://appmanager.test/") };
        return new AppManagerClient(vHttp, NullLogger<AppManagerClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string aJson, HttpStatusCode aStatusCode = HttpStatusCode.OK) =>
        new(aStatusCode) { Content = new StringContent(aJson, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> objRespond;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> aRespond) => objRespond = aRespond;

        public int CallCount { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
        {
            CallCount++;
            LastRequestBody = aRequest.Content is null ? null : await aRequest.Content.ReadAsStringAsync(aCt);
            return objRespond(aRequest);
        }
    }
}
