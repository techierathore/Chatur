using Chatur.Core.Models;
using Chatur.Tests.Support;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests.Models;

/// <summary>Tests for <see cref="SecretSubscriptionSessionStore"/> over an in-memory secret store.</summary>
public sealed class SecretSubscriptionSessionStoreTests
{
    private const string Connector = "chatgpt-subscription";

    /// <summary>When a session is saved, then it loads back with every field, from the secret store under the connector's name (REQ-FN-016, REQ-FN-017).</summary>
    [Fact]
    public async Task SaveAsyncThenLoadAsyncRoundTripsTheSession()
    {
        var vSecrets = new InMemorySecretStore();
        var vSut = new SecretSubscriptionSessionStore(vSecrets);
        var vExpires = new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.Zero);

        await vSut.SaveAsync(new SubscriptionSession
        {
            ConnectorName = Connector,
            AccessToken = "access",
            RefreshToken = "refresh",
            IdToken = "id",
            AccountId = "acct",
            ExpiresAt = vExpires,
        });
        var vLoaded = await vSut.LoadAsync(Connector);

        Assert.True(vSecrets.Contains(SecretSubscriptionSessionStore.SecretNameFor(Connector)));
        Assert.NotNull(vLoaded);
        Assert.Equal(Connector, vLoaded.ConnectorName);
        Assert.Equal("access", vLoaded.AccessToken);
        Assert.Equal("refresh", vLoaded.RefreshToken);
        Assert.Equal("id", vLoaded.IdToken);
        Assert.Equal("acct", vLoaded.AccountId);
        Assert.Equal(vExpires, vLoaded.ExpiresAt);
    }

    /// <summary>When a refresh rotates the tokens and saves again, then the newer session replaces the older one (REQ-FN-016).</summary>
    [Fact]
    public async Task SaveAsyncReplacesAnEarlierSession()
    {
        var vSut = new SecretSubscriptionSessionStore(new InMemorySecretStore());
        await vSut.SaveAsync(new SubscriptionSession { ConnectorName = Connector, AccessToken = "old", RefreshToken = "r1" });

        await vSut.SaveAsync(new SubscriptionSession { ConnectorName = Connector, AccessToken = "new", RefreshToken = "r2" });

        var vLoaded = await vSut.LoadAsync(Connector);
        Assert.Equal("new", vLoaded!.AccessToken);
        Assert.Equal("r2", vLoaded.RefreshToken);
    }

    /// <summary>When nothing was saved, or the filed entry cannot be read, then no session is reported so the owner is asked to sign in (REQ-FN-016).</summary>
    [Fact]
    public async Task LoadAsyncReportsNoSessionWhenNothingUsableIsFiled()
    {
        var vSecrets = new InMemorySecretStore();
        var vSut = new SecretSubscriptionSessionStore(vSecrets);

        Assert.Null(await vSut.LoadAsync(Connector));

        await vSecrets.SaveAsync(SecretSubscriptionSessionStore.SecretNameFor(Connector), "not json");
        Assert.Null(await vSut.LoadAsync(Connector));
    }

    /// <summary>When a session is cleared, then it is deleted from the secret store and loads as none (REQ-FN-016).</summary>
    [Fact]
    public async Task ClearAsyncForgetsTheSession()
    {
        var vSecrets = new InMemorySecretStore();
        var vSut = new SecretSubscriptionSessionStore(vSecrets);
        await vSut.SaveAsync(new SubscriptionSession { ConnectorName = Connector, AccessToken = "access" });

        await vSut.ClearAsync(Connector);

        Assert.False(vSecrets.Contains(SecretSubscriptionSessionStore.SecretNameFor(Connector)));
        Assert.Null(await vSut.LoadAsync(Connector));
    }
}
