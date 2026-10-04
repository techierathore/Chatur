using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.Prerequisites;

/// <summary>
/// <see cref="IGitHubReleaseClient"/> over the public GitHub REST API, for the
/// <c>techierathore/Chatur</c> repository the release pipeline publishes to (Architecture §6,
/// "Release by GitHub Actions"; REQ-FN-013). Every path — network failure, repository not found,
/// repository with no releases yet — is handled by returning <see langword="null"/> honestly; none
/// of them is a fault worth throwing over.
/// </summary>
public sealed class GitHubReleaseClient : IGitHubReleaseClient
{
    private const string ReleasesPath = "repos/techierathore/Chatur/releases?per_page=1";

    private readonly HttpClient objHttpClient;
    private readonly ILogger<GitHubReleaseClient> objLogger;

    /// <summary>Creates the client.</summary>
    /// <param name="aHttpClient">A client whose <c>BaseAddress</c> is the GitHub API root.</param>
    /// <param name="aLogger">Where an unreachable GitHub or an unexpected answer is logged.</param>
    public GitHubReleaseClient(HttpClient aHttpClient, ILogger<GitHubReleaseClient> aLogger)
    {
        objHttpClient = aHttpClient;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<GitHubRelease?> LatestReleaseAsync(CancellationToken aCt = default)
    {
        HttpResponseMessage vResponse;
        try
        {
            vResponse = await objHttpClient.GetAsync(ReleasesPath, aCt).ConfigureAwait(false);
        }
        catch (Exception vEx) when (vEx is not OperationCanceledException)
        {
            objLogger.LogWarning(vEx, "Could not reach GitHub to check for a newer Chatur build.");
            return null;
        }

        if (vResponse.StatusCode == HttpStatusCode.NotFound)
        {
            objLogger.LogInformation(
                "No release found for techierathore/Chatur yet — the repository or its releases do not exist.");
            return null;
        }

        if (!vResponse.IsSuccessStatusCode)
        {
            objLogger.LogWarning("GitHub answered {Status} when checking for a newer Chatur build.", vResponse.StatusCode);
            return null;
        }

        await using var vStream = await vResponse.Content.ReadAsStreamAsync(aCt).ConfigureAwait(false);
        using var vDocument = await JsonDocument.ParseAsync(vStream, cancellationToken: aCt).ConfigureAwait(false);
        var vReleases = vDocument.RootElement;
        if (vReleases.ValueKind != JsonValueKind.Array || vReleases.GetArrayLength() == 0)
        {
            objLogger.LogInformation("No release found for techierathore/Chatur yet.");
            return null;
        }

        return ParseRelease(vReleases[0]);
    }

    private static GitHubRelease ParseRelease(JsonElement aRelease)
    {
        var vAssets = new List<GitHubReleaseAsset>();
        if (aRelease.TryGetProperty("assets", out var vAssetsElement) && vAssetsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var vAsset in vAssetsElement.EnumerateArray())
            {
                var vName = vAsset.TryGetProperty("name", out var vNameElement) ? vNameElement.GetString() : null;
                var vUrl = vAsset.TryGetProperty("browser_download_url", out var vUrlElement) ? vUrlElement.GetString() : null;
                if (vName is not null && vUrl is not null)
                {
                    vAssets.Add(new GitHubReleaseAsset(vName, vUrl));
                }
            }
        }

        return new GitHubRelease(
            aRelease.TryGetProperty("tag_name", out var vTag) ? vTag.GetString() ?? string.Empty : string.Empty,
            aRelease.TryGetProperty("target_commitish", out var vCommitish) ? vCommitish.GetString() : null,
            aRelease.TryGetProperty("published_at", out var vPublished) && vPublished.ValueKind == JsonValueKind.String
                ? vPublished.GetDateTime()
                : null,
            vAssets);
    }
}
