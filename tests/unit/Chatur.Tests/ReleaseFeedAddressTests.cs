using Chatur.Core.Prerequisites;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="ReleaseFeedAddress"/> and the MAUI workload probe (REQ-FN-013, REQ-FN-011).</summary>
public sealed class ReleaseFeedAddressTests
{
    private static IConfiguration Configured(string? aValue) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(aValue is null ? [] : [new KeyValuePair<string, string?>(ReleaseFeedAddress.ConfigurationKey, aValue)])
            .Build();

    /// <summary>When nothing is configured, then the release check goes to the public GitHub API.</summary>
    [Fact]
    public void ResolveDefaultsToTheGitHubApi() =>
        Assert.Equal(new Uri("https://api.github.com/"), ReleaseFeedAddress.Resolve(Configured(null)));

    /// <summary>When an address is configured without a trailing slash, then it is used and the slash is added so relative paths resolve under it.</summary>
    [Fact]
    public void ResolveUsesTheConfiguredAddress() =>
        Assert.Equal(new Uri("http://localhost:18952/"), ReleaseFeedAddress.Resolve(Configured("http://localhost:18952")));

    /// <summary>When the configured value is not an absolute address, then the default is used rather than a broken client.</summary>
    [Fact]
    public void ResolveFallsBackWhenTheValueIsNotAnAddress() =>
        Assert.Equal(new Uri("https://api.github.com/"), ReleaseFeedAddress.Resolve(Configured("not a url")));

    /// <summary>When only the Android MAUI workload is installed, then the MAUI workload row is not "installed", because Android alone cannot build the desktop heads.</summary>
    [Fact]
    public void MauiProbeIgnoresTheAndroidOnlyWorkload()
    {
        const string vOutput = "Installed Workload Id      Manifest Version\n---------\nmaui-android               10.0.20/10.0.100      SDK 10.0.300\n";
        Assert.Null(ToolProbeCatalog.MauiWorkload.ParseVersion(vOutput));
    }

    /// <summary>When the maui workload is installed, then the MAUI workload row reads "installed".</summary>
    [Fact]
    public void MauiProbeFindsTheMauiWorkload()
    {
        const string vOutput = "Installed Workload Id      Manifest Version\n---------\nmaui                       10.0.20/10.0.100      SDK 10.0.300\n";
        Assert.Equal("installed", ToolProbeCatalog.MauiWorkload.ParseVersion(vOutput));
    }
}
