using System.Net;
using Chatur.Core.Prerequisites;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="GitHubReleaseClient"/> (REQ-FN-013), against a faked HTTP handler — no real network.</summary>
public sealed class GitHubReleaseClientTests
{
    /// <summary>
    /// When GitHub answers 200 with an empty array — the real, current state of the Chatur repository,
    /// which has no releases yet — then the client honestly reports no release, never a guess.
    /// </summary>
    [Fact]
    public async Task LatestReleaseAsyncReturnsNullForAnEmptyReleaseList()
    {
        var vClient = ClientReturning(HttpStatusCode.OK, "[]");

        var vRelease = await vClient.LatestReleaseAsync();

        Assert.Null(vRelease);
    }

    /// <summary>When the repository itself does not exist, GitHub answers 404, and that is reported the same honest way.</summary>
    [Fact]
    public async Task LatestReleaseAsyncReturnsNullWhenTheRepositoryIsNotFound()
    {
        var vClient = ClientReturning(HttpStatusCode.NotFound, string.Empty);

        var vRelease = await vClient.LatestReleaseAsync();

        Assert.Null(vRelease);
    }

    /// <summary>When a release exists, then its tag, commit, publish date and assets are all read out.</summary>
    [Fact]
    public async Task LatestReleaseAsyncParsesARealRelease()
    {
        const string vBody = """
        [
          {
            "tag_name": "v0.1.1",
            "target_commitish": "4c7e9b0aa11122233344455566677788899aabb",
            "published_at": "2026-09-20T10:00:00Z",
            "assets": [
              { "name": "Chatur-mac.zip", "browser_download_url": "https://example.test/Chatur-mac.zip" },
              { "name": "Chatur-win.zip", "browser_download_url": "https://example.test/Chatur-win.zip" }
            ]
          }
        ]
        """;
        var vClient = ClientReturning(HttpStatusCode.OK, vBody);

        var vRelease = await vClient.LatestReleaseAsync();

        Assert.NotNull(vRelease);
        Assert.Equal("v0.1.1", vRelease!.TagName);
        Assert.Equal("4c7e9b0aa11122233344455566677788899aabb", vRelease.TargetCommitish);
        Assert.Equal(2, vRelease.Assets.Count);
        Assert.Contains(vRelease.Assets, aAsset => aAsset.Name == "Chatur-mac.zip");
    }

    /// <summary>When the network call itself fails, then the client reports no release rather than throwing.</summary>
    [Fact]
    public async Task LatestReleaseAsyncReturnsNullWhenTheNetworkFails()
    {
        var vHandler = new FakeHandler(_ => throw new HttpRequestException("no route to host"));
        var vHttpClient = new HttpClient(vHandler) { BaseAddress = new Uri("https://api.github.test/") };
        var vClient = new GitHubReleaseClient(vHttpClient, NullLogger<GitHubReleaseClient>.Instance);

        var vRelease = await vClient.LatestReleaseAsync();

        Assert.Null(vRelease);
    }

    private static GitHubReleaseClient ClientReturning(HttpStatusCode aStatus, string aBody)
    {
        var vHandler = new FakeHandler(_ => new HttpResponseMessage(aStatus)
        {
            Content = new StringContent(aBody),
        });
        var vHttpClient = new HttpClient(vHandler) { BaseAddress = new Uri("https://api.github.test/") };
        return new GitHubReleaseClient(vHttpClient, NullLogger<GitHubReleaseClient>.Instance);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> objRespond;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> aRespond)
        {
            objRespond = aRespond;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage aRequest, CancellationToken aCt) =>
            Task.FromResult(objRespond(aRequest));
    }
}
