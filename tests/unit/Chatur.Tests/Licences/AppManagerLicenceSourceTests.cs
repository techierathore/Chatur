using System.Net;
using System.Text;
using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Licences;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests.Licences;

/// <summary>
/// Tests for <see cref="AppManagerLicenceClient"/> against a fake handler: the requests are the ones
/// docs/AppManager-api-usage-guide.md names, and the answers parse (REQ-UI-064, REQ-UI-066).
/// </summary>
public sealed class AppManagerLicenceSourceTests
{
    /// <summary>
    /// When the licences are asked for, then the request is a bearer GET of LicenseSvc and the licence's
    /// name, status and days remaining are read from the answer (REQ-UI-064).
    /// </summary>
    [Fact]
    public async Task GetLicencesAsyncSendsABearerGetAndParsesTheLicence()
    {
        var vHandler = new Handler(HttpStatusCode.OK, """{"success":true,"data":[{"licenseName":"Professional","licenseKey":"K","status":"Active","daysRemaining":365}],"message":"ok"}""");
        var vClient = Create(vHandler);

        var vCall = await vClient.GetLicencesAsync("token");

        Assert.True(vCall.Ok);
        Assert.Equal("Professional", vCall.Data!.Single().LicenseName);
        Assert.Equal(365, vCall.Data!.Single().DaysRemaining);
        Assert.Equal(HttpMethod.Get, vHandler.Method);
        Assert.Equal("/LicenseSvc", vHandler.Path);
        Assert.Equal("Bearer token", vHandler.Authorization);
    }

    /// <summary>
    /// When a device is forgotten, then the request is a bearer DELETE of that device's App Manager id (REQ-UI-066).
    /// </summary>
    [Fact]
    public async Task ForgetDeviceAsyncSendsADeleteForTheDevice()
    {
        var vHandler = new Handler(HttpStatusCode.OK, """{"success":true,"data":null,"message":"Device forgotten and its sessions revoked"}""");
        var vClient = Create(vHandler);

        var vCall = await vClient.ForgetDeviceAsync("token", 332);

        Assert.True(vCall.Ok);
        Assert.Equal(HttpMethod.Delete, vHandler.Method);
        Assert.Equal("/AuthSvc/devices/332", vHandler.Path);
    }

    /// <summary>
    /// When App Manager refuses the token, then the status is 401 so the source can refresh and try again.
    /// </summary>
    [Fact]
    public async Task UnauthorizedAnswerKeepsItsStatus()
    {
        var vClient = Create(new Handler(HttpStatusCode.Unauthorized, string.Empty));

        var vCall = await vClient.GetDevicesAsync("old");

        Assert.False(vCall.Ok);
        Assert.Equal(401, vCall.Status);
    }

    /// <summary>
    /// When the sign-in carried a device-cap report, then it is kept in the shared state, and a new account clears it.
    /// </summary>
    [Fact]
    public void DeviceCapReportIsKeptForTheAccountOnly()
    {
        var vState = new AppState();
        vState.SetCurrentUser(new CurrentUser("1", "a@b.c", "A"));
        vState.SetDeviceCap(new DeviceCapReport(2, 1, true, "warn", "K"));
        Assert.True(vState.DeviceCap!.IsOverCap);

        vState.SetCurrentUser(new CurrentUser("2", "d@e.f", "D"));

        Assert.Null(vState.DeviceCap);
    }

    private static AppManagerLicenceClient Create(Handler aHandler) =>
        new(new HttpClient(aHandler) { BaseAddress = new Uri("https://example.test/") }, NullLogger<AppManagerLicenceClient>.Instance);

    private sealed class Handler : HttpMessageHandler
    {
        private readonly HttpStatusCode objStatus;
        private readonly string objBody;

        public Handler(HttpStatusCode aStatus, string aBody)
        {
            objStatus = aStatus;
            objBody = aBody;
        }

        public HttpMethod? Method { get; private set; }

        public string? Path { get; private set; }

        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
        {
            Method = aRequest.Method;
            Path = aRequest.RequestUri!.AbsolutePath;
            Authorization = aRequest.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(objStatus) { Content = new StringContent(objBody, Encoding.UTF8, "application/json") });
        }
    }
}
