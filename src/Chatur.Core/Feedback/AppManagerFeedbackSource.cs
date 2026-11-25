using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chatur.Core.Accounts;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.Extensions.Configuration;

namespace Chatur.Core.Feedback;

/// <summary>
/// <see cref="IFeedbackSource"/> over App Manager's Issue service (<c>POST /IssueSvc</c>,
/// <c>GET /IssueSvc/{id}</c>; docs/AppManager-api-usage-guide.md §3.6), as the signed-in account. The
/// real app uses this; it is what Chatur does unless a test host swaps it. App Manager has no field
/// for what an entry is about, so the name leads the title (<c>[TrBlazeUI] …</c>) and the description
/// (REQ-FN-073).
/// </summary>
public sealed class AppManagerFeedbackSource : IFeedbackSource
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient objHttp;
    private readonly ISecretStore objSecrets;
    private readonly IAppManagerClient objAppManager;
    private readonly int? objApplicationId;

    /// <summary>Creates the source.</summary>
    /// <param name="aHttp">The client configured with App Manager's address and API key headers.</param>
    /// <param name="aSecrets">Where the signed-in session's tokens are kept.</param>
    /// <param name="aAppManager">Asked for a new access token when the old one has run out.</param>
    /// <param name="aConfiguration">Where <c>AppManager:ApplicationId</c> is read.</param>
    public AppManagerFeedbackSource(HttpClient aHttp, ISecretStore aSecrets, IAppManagerClient aAppManager, IConfiguration aConfiguration)
    {
        objHttp = aHttp;
        objSecrets = aSecrets;
        objAppManager = aAppManager;
        objApplicationId = int.TryParse(aConfiguration["AppManager:ApplicationId"], out var vId) && vId > 0 ? vId : null;
    }

    /// <inheritdoc />
    public async Task<FeedbackRemoteRaised> RaiseAsync(FeedbackRemoteRequest aRequest, CancellationToken aCt = default)
    {
        var vBody = new Dictionary<string, object?>
        {
            ["title"] = $"[{aRequest.About}] {aRequest.Title}",
            ["description"] = $"About: {aRequest.About}\n\n{aRequest.Detail}",
            ["type"] = "Feedback",
            ["priority"] = aRequest.Severity switch
            {
                FeedbackSeverity.Blocker => "Critical",
                FeedbackSeverity.Major => "High",
                _ => "Low"
            },
            ["environment"] = aRequest.Environment is null ? null : new { appVersion = aRequest.Environment, deviceType = "Desktop" }
        };
        if (!objHttp.DefaultRequestHeaders.Contains("X-Api-Key") && objApplicationId.HasValue)
        {
            vBody["applicationId"] = objApplicationId.Value;
        }

        var vResponse = await SendAuthorisedAsync(() => new HttpRequestMessage(HttpMethod.Post, "IssueSvc") { Content = JsonContent.Create(vBody, options: JsonOptions) }, aCt).ConfigureAwait(false);
        var vEnvelope = await ReadAsync<IssueData>(vResponse, aCt).ConfigureAwait(false);
        if (!vEnvelope.Success || vEnvelope.Data is null)
        {
            throw new FeedbackRefusedException(vEnvelope.Message ?? "App Manager refused the entry.");
        }

        return new FeedbackRemoteRaised(
            vEnvelope.Data.IssueId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            vEnvelope.Data.IssueNumber ?? vEnvelope.Data.IssueId.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public async Task<FeedbackRemoteState?> ReadAsync(string aRemoteId, CancellationToken aCt = default)
    {
        var vResponse = await SendAuthorisedAsync(() => new HttpRequestMessage(HttpMethod.Get, $"IssueSvc/{Uri.EscapeDataString(aRemoteId)}"), aCt).ConfigureAwait(false);
        if (vResponse.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var vEnvelope = await ReadAsync<IssueData>(vResponse, aCt).ConfigureAwait(false);
        if (!vEnvelope.Success || vEnvelope.Data is null)
        {
            throw new FeedbackUnreachableException(vEnvelope.Message ?? "App Manager did not answer.");
        }

        var vAnswer = vEnvelope.Data.Comments?.LastOrDefault(aComment => !aComment.IsInternal && !string.IsNullOrWhiteSpace(aComment.Comment))?.Comment;
        var vStatus = vEnvelope.Data.Status ?? string.Empty;
        return new FeedbackRemoteState(
            vStatus.Equals("Resolved", StringComparison.OrdinalIgnoreCase) || vStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase),
            vAnswer);
    }

    private async Task<HttpResponseMessage> SendAuthorisedAsync(Func<HttpRequestMessage> aBuild, CancellationToken aCt)
    {
        try
        {
            var vResponse = await SendOnceAsync(aBuild, await objSecrets.ReadAsync(AccountSecretNames.AccessToken, aCt).ConfigureAwait(false), aCt).ConfigureAwait(false);
            if (vResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                return vResponse;
            }

            var vRefreshToken = await objSecrets.ReadAsync(AccountSecretNames.RefreshToken, aCt).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(vRefreshToken))
            {
                throw new FeedbackRefusedException("Sign in again to send feedback.");
            }

            var vRefreshed = await objAppManager.RefreshAsync(vRefreshToken, aCt).ConfigureAwait(false);
            if (!vRefreshed.Success || vRefreshed.AccessToken is null)
            {
                throw new FeedbackRefusedException("Sign in again to send feedback.");
            }

            await objSecrets.SaveAsync(AccountSecretNames.AccessToken, vRefreshed.AccessToken, aCt).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(vRefreshed.RefreshToken))
            {
                await objSecrets.SaveAsync(AccountSecretNames.RefreshToken, vRefreshed.RefreshToken, aCt).ConfigureAwait(false);
            }

            return await SendOnceAsync(aBuild, vRefreshed.AccessToken, aCt).ConfigureAwait(false);
        }
        catch (HttpRequestException vException)
        {
            throw new FeedbackUnreachableException("App Manager could not be reached.", vException);
        }
        catch (TaskCanceledException vException) when (!aCt.IsCancellationRequested)
        {
            throw new FeedbackUnreachableException("App Manager did not answer in time.", vException);
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(Func<HttpRequestMessage> aBuild, string? aAccessToken, CancellationToken aCt)
    {
        if (string.IsNullOrWhiteSpace(aAccessToken))
        {
            throw new FeedbackRefusedException("Sign in again to send feedback.");
        }

        using var vRequest = aBuild();
        vRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aAccessToken);
        var vResponse = await objHttp.SendAsync(vRequest, aCt).ConfigureAwait(false);
        if ((int)vResponse.StatusCode >= 500)
        {
            throw new FeedbackUnreachableException($"App Manager answered {(int)vResponse.StatusCode}.");
        }

        return vResponse;
    }

    private static async Task<ApiEnvelope<T>> ReadAsync<T>(HttpResponseMessage aResponse, CancellationToken aCt)
    {
        var vJson = await aResponse.Content.ReadAsStringAsync(aCt).ConfigureAwait(false);
        try
        {
            if (JsonSerializer.Deserialize<ApiEnvelope<T>>(vJson, JsonOptions) is { } vEnvelope)
            {
                return vEnvelope;
            }
        }
        catch (JsonException)
        {
            // Falls through to the refusal below.
        }

        return new ApiEnvelope<T> { Success = false, Message = $"App Manager returned an unreadable response ({(int)aResponse.StatusCode})." };
    }

    private sealed class ApiEnvelope<T>
    {
        public bool Success { get; set; }

        public T? Data { get; set; }

        public string? Message { get; set; }
    }

    private sealed class IssueData
    {
        public long IssueId { get; set; }

        public string? IssueNumber { get; set; }

        public string? Status { get; set; }

        public List<IssueComment>? Comments { get; set; }
    }

    private sealed class IssueComment
    {
        public string? Comment { get; set; }

        public bool IsInternal { get; set; }
    }
}
