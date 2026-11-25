using System.Net;
using System.Text;
using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Feedback;
using Chatur.Tests.Support;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Chatur.Tests.Feedback;

/// <summary>
/// Tests for <see cref="AppManagerFeedbackSource"/> against a fake <see cref="HttpMessageHandler"/> in
/// App Manager's own envelope shape (docs/AppManager-api-usage-guide.md §3.6): no real call is made.
/// </summary>
public sealed class AppManagerFeedbackSourceTests
{
    /// <summary>When an entry is raised, then the library name leads the title and the description, and the bearer token goes with it (REQ-UI-062, REQ-FN-073).</summary>
    [Fact(DisplayName = "REQ-FN-073 the library name leads the issue title sent to App Manager")]
    public async Task LibraryNameLeadsTheTitle()
    {
        var vHandler = new FakeHandler(_ => Json("""{"success":true,"data":{"issueId":5,"issueNumber":"chatur-005"}}"""));
        var vSource = await CreateAsync(vHandler);

        var vRaised = await vSource.RaiseAsync(new FeedbackRemoteRequest("TrBlazeUI", FeedbackSeverity.Blocker, "A header covers a row", "Details of the problem.", null));

        Assert.Equal("chatur-005", vRaised.RemoteNumber);
        Assert.Contains("\"title\":\"[TrBlazeUI] A header covers a row\"", vHandler.Body);
        Assert.Contains("\"priority\":\"Critical\"", vHandler.Body);
        Assert.Equal("Bearer at", vHandler.Authorization);
    }

    /// <summary>When App Manager cannot be reached or answers with a server error, then the source says unreachable so the entry is kept (REQ-FN-074).</summary>
    [Fact(DisplayName = "REQ-FN-074 a network failure and a server error are both unreachable")]
    public async Task NetworkFailureIsUnreachable()
    {
        var vDown = await CreateAsync(new FakeHandler(_ => throw new HttpRequestException("no network")));
        var vError = await CreateAsync(new FakeHandler(_ => Json("{}", HttpStatusCode.BadGateway)));
        var vRequest = new FeedbackRemoteRequest("Chatur", FeedbackSeverity.Major, "A title", "Details of the problem.", null);

        await Assert.ThrowsAsync<FeedbackUnreachableException>(() => vDown.RaiseAsync(vRequest));
        await Assert.ThrowsAsync<FeedbackUnreachableException>(() => vError.RaiseAsync(vRequest));
    }

    /// <summary>When App Manager refuses with its own message, then that message is carried (REQ-UI-062).</summary>
    [Fact(DisplayName = "REQ-UI-062 App Manager's refusal is carried in its own words")]
    public async Task RefusalCarriesAppManagersWords()
    {
        var vSource = await CreateAsync(new FakeHandler(_ => Json("""{"success":false,"message":"Title is required"}""", HttpStatusCode.BadRequest)));

        var vException = await Assert.ThrowsAsync<FeedbackRefusedException>(() =>
            vSource.RaiseAsync(new FeedbackRemoteRequest("Chatur", FeedbackSeverity.Minor, "A title", "Details of the problem.", null)));

        Assert.Equal("Title is required", vException.Message);
    }

    /// <summary>When the access token has run out, then a new one is fetched, kept and the call tried once more.</summary>
    [Fact(DisplayName = "REQ-UI-062 an expired access token is refreshed and the call retried")]
    public async Task ExpiredTokenIsRefreshed()
    {
        var vCalls = 0;
        var vHandler = new FakeHandler(_ => ++vCalls == 1 ? Json("{}", HttpStatusCode.Unauthorized) : Json("""{"success":true,"data":{"issueId":1,"issueNumber":"chatur-001"}}"""));
        var vSecrets = new InMemorySecretStore();
        var vSource = await CreateAsync(vHandler, vSecrets);

        await vSource.RaiseAsync(new FeedbackRemoteRequest("Chatur", FeedbackSeverity.Minor, "A title", "Details of the problem.", null));

        Assert.Equal("Bearer new-at", vHandler.Authorization);
        Assert.Equal("new-at", await vSecrets.ReadAsync(AccountSecretNames.AccessToken));
    }

    /// <summary>When an issue has a public comment, then it is the answer; when it is resolved, it is finished; internal comments never show (REQ-UI-063).</summary>
    [Fact(DisplayName = "REQ-UI-063 the latest public comment is the answer and resolved is finished")]
    public async Task LatestPublicCommentIsTheAnswer()
    {
        var vSource = await CreateAsync(new FakeHandler(_ => Json(
            """{"success":true,"data":{"issueId":1,"status":"Resolved","comments":[{"comment":"Looking","isInternal":false},{"comment":"Fixed in 2.1.4","isInternal":false},{"comment":"secret","isInternal":true}]}}""")));

        var vState = await vSource.ReadAsync("1");

        Assert.NotNull(vState);
        Assert.True(vState.IsFinished);
        Assert.Equal("Fixed in 2.1.4", vState.Answer);
    }

    private static async Task<AppManagerFeedbackSource> CreateAsync(HttpMessageHandler aHandler, InMemorySecretStore? aSecrets = null)
    {
        var vSecrets = aSecrets ?? new InMemorySecretStore();
        await vSecrets.SaveAsync(AccountSecretNames.AccessToken, "at");
        await vSecrets.SaveAsync(AccountSecretNames.RefreshToken, "rt");
        var vConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["AppManager:ApplicationId"] = "1" }).Build();
        return new AppManagerFeedbackSource(
            new HttpClient(aHandler) { BaseAddress = new Uri("https://appmanager.test/") },
            vSecrets,
            new FakeAppManager(),
            vConfiguration);
    }

    private static HttpResponseMessage Json(string aJson, HttpStatusCode aStatus = HttpStatusCode.OK) =>
        new(aStatus) { Content = new StringContent(aJson, Encoding.UTF8, "application/json") };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> aRespond) : HttpMessageHandler
    {
        public string Body { get; private set; } = string.Empty;

        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt)
        {
            Body = aRequest.Content is null ? string.Empty : await aRequest.Content.ReadAsStringAsync(aCt);
            Authorization = aRequest.Headers.Authorization?.ToString();
            return aRespond(aRequest);
        }
    }

    private sealed class FakeAppManager : IAppManagerClient
    {
        public Task<string> GetPublicKeyAsync(CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<AppManagerAuthResult> DeviceLoginAsync(string aEmail, string aEncryptedPassword, AppManagerDeviceInfo aDevice, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<AppManagerAuthResult> DeviceRegisterAsync(string aEmail, string aEncryptedPassword, string aFirstName, string aLastName, AppManagerDeviceInfo aDevice, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<AppManagerAuthResult> RefreshAsync(string aRefreshToken, CancellationToken aCt = default) =>
            Task.FromResult(new AppManagerAuthResult(true, null, null, null, null, null, "new-at", "new-rt", null));

        public Task LogoutAsync(string? aRefreshToken, bool aLogoutAllDevices, CancellationToken aCt = default) => Task.CompletedTask;
    }
}
