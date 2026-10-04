using System.Linq;
using Chatur.Core.Actions;
using Chatur.Core.Models;
using Chatur.Tests.Support;
using TechieRag.Llm;
using Xunit;

namespace Chatur.Tests.Models;

/// <summary>Tests for <see cref="ProviderActions"/> against a real, migrated, temporary database.</summary>
public sealed class ProviderActionsTests : MigratedDatabaseFixture
{
    /// <summary>
    /// When a provider is added with a pasted key, then it is listed connected, its key is in the
    /// secret store and never as a value on the row, and its catalog models can be chosen in routing
    /// (REQ-FN-014, REQ-FN-017).
    /// </summary>
    [Fact]
    public async Task AddWithKeyAsyncFilesTheSecretAndSeedsModels()
    {
        var vSecrets = new InMemorySecretStore();
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());

        var vProvider = await vActions.AddWithKeyAsync("My Anthropic", "Anthropic", "https://api.anthropic.com", "sk-ant-secret");

        Assert.Equal(ProviderState.Connected, vProvider.State);
        Assert.True(vSecrets.Contains($"chatur.provider.{vProvider.ProviderId}"));

        var vModels = await vActions.ListModelsAsync();
        Assert.Contains(vModels, aModel => aModel.ProviderId == vProvider.ProviderId && aModel.Identifier == "claude-sonnet-5");
    }

    /// <summary>
    /// When an OpenAI-compatible provider is added, then the models it lists itself are the ones
    /// seeded for routing, since Chatur has no catalog for an arbitrary service (REQ-FN-014).
    /// </summary>
    [Fact]
    public async Task AddWithKeyAsyncSeedsACompatibleServicesOwnModels()
    {
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());

        var vProvider = await vActions.AddWithKeyAsync("OpenCode Go", "OpenAICompatible", "https://opencode.ai/zen/go/v1", "sk-go");

        var vModels = await vActions.ListModelsAsync();
        var vIdentifiers = vModels.Where(aModel => aModel.ProviderId == vProvider.ProviderId).Select(aModel => aModel.Identifier);
        Assert.Equal(new[] { "a-model" }, vIdentifiers);
    }

    /// <summary>
    /// When an OpenAI-compatible provider does not answer, then the add fails and nothing is saved,
    /// neither the row nor the key (REQ-FN-014, REQ-FN-017).
    /// </summary>
    [Fact]
    public async Task AddWithKeyAsyncSavesNothingWhenACompatibleServiceDoesNotAnswer()
    {
        var vSecrets = new InMemorySecretStore();
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe { Succeeds = false }, new FakeSubscriptionSignIn());

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddWithKeyAsync("Broken", "OpenAICompatible", "https://example.invalid/v1", "sk-x"));

        Assert.Empty(await vActions.ListAsync());
    }

    /// <summary>
    /// When the Add a provider dialog tests unsaved values, then the probe is called with them and
    /// nothing is saved (REQ-UI-019).
    /// </summary>
    [Fact]
    public async Task TestUnsavedAsyncSavesNothing()
    {
        var vProbe = new FakeLlmConnectionProbe();
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), vProbe, new FakeSubscriptionSignIn());

        var vResult = await vActions.TestUnsavedAsync("OpenAICompatible", "https://opencode.ai/zen/go/v1", "sk-go");

        Assert.True(vResult.Succeeded);
        Assert.Equal("OpenAICompatible", vProbe.LastConnector);
        Assert.Empty(await vActions.ListAsync());
    }

    /// <summary>When a local model is added, then it needs no key and is listed connected (REQ-FN-015).</summary>
    [Fact]
    public async Task AddLocalAsyncNeedsNoSecret()
    {
        var vSecrets = new InMemorySecretStore();
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());

        var vProvider = await vActions.AddLocalAsync("My Ollama", "http://localhost:11434");

        Assert.Equal("None", vProvider.SignInMethod);
        Assert.Equal(ProviderState.Connected, vProvider.State);
    }

    /// <summary>
    /// When a ChatGPT subscription is added, then the sign-in code is handed to the caller before the
    /// add completes (so the dialog can show it and open the browser), the provider is listed
    /// connected with the subscription sign-in method, its default model is seeded for routing, the
    /// row holds only the name the session is filed under, and no tokens are written to the database
    /// (REQ-FN-016, REQ-FN-017).
    /// </summary>
    [Fact]
    public async Task AddSubscriptionAsyncShowsTheCodeThenConnectsTheProvider()
    {
        var vSecrets = new InMemorySecretStore();
        var vSignIn = new FakeSubscriptionSignIn { SessionStore = new SecretSubscriptionSessionStore(vSecrets) };
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe(), vSignIn);
        SignInCodePrompt? vShown = null;
        var vProviderCountWhenShown = -1;

        var vProvider = await vActions.AddSubscriptionAsync(
            "My ChatGPT",
            ProviderActions.ChatGptSubscriptionConnector,
            async (aPrompt, aCt) =>
            {
                vShown = aPrompt;
                vProviderCountWhenShown = (await vActions.ListAsync(aCt)).Count;
            });

        Assert.Equal(FakeSubscriptionSignIn.Prompt, vShown);
        Assert.Equal(0, vProviderCountWhenShown);
        Assert.Equal(ProviderState.Connected, vProvider.State);
        Assert.Equal("Subscription", vProvider.SignInMethod);
        Assert.Equal(1, vSignIn.CallCount);

        var vModels = await vActions.ListModelsAsync();
        Assert.Contains(vModels, aModel => aModel.ProviderId == vProvider.ProviderId && aModel.Identifier == vSignIn.LastModelIdentifier);

        var vSecretName = SecretSubscriptionSessionStore.SecretNameFor("chatgpt-subscription");
        Assert.True(vSecrets.Contains(vSecretName));
        using var vConnection = CreateFactory().OpenConnection();
        var vRowSecretName = Dapper.SqlMapper.QuerySingle<string>(vConnection, "SELECT SecretName FROM Provider WHERE ProviderId = @ProviderId;", new { vProvider.ProviderId });
        Assert.Equal(vSecretName, vRowSecretName);
    }

    /// <summary>
    /// When the sign-in fails after the code was shown (the code expired, the vendor refused), then the
    /// exception reaches the caller and nothing is saved, not the row and not a model (REQ-FN-016).
    /// </summary>
    [Fact]
    public async Task AddSubscriptionAsyncSavesNothingWhenTheSignInFails()
    {
        var vSignIn = new FakeSubscriptionSignIn { Failure = new SubscriptionSignInException(SubscriptionSignInException.CodeExpired, "The code expired.") };
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), vSignIn);

        var vException = await Assert.ThrowsAsync<SubscriptionSignInException>(
            () => vActions.AddSubscriptionAsync("My ChatGPT", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask));

        Assert.Equal(SubscriptionSignInException.CodeExpired, vException.Code);
        Assert.Empty(await vActions.ListAsync());
        Assert.Empty(await vActions.ListModelsAsync());
    }

    /// <summary>
    /// When the owner cancels the sign-in panel, then the cancellation reaches the sign-in and nothing
    /// is saved (REQ-FN-016).
    /// </summary>
    [Fact]
    public async Task AddSubscriptionAsyncSavesNothingWhenTheOwnerCancels()
    {
        var vSignIn = new FakeSubscriptionSignIn { Failure = new OperationCanceledException() };
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), vSignIn);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => vActions.AddSubscriptionAsync("My ChatGPT", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask));

        Assert.Empty(await vActions.ListAsync());
    }

    /// <summary>
    /// When a subscription is added again while its row exists, then the owner signs in again and the
    /// same row is reused rather than duplicated (REQ-FN-016).
    /// </summary>
    [Fact]
    public async Task AddSubscriptionAsyncReusesTheExistingRow()
    {
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());
        var vFirst = await vActions.AddSubscriptionAsync("My ChatGPT", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask);

        var vSecond = await vActions.AddSubscriptionAsync("Again", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask);

        Assert.Equal(vFirst.ProviderId, vSecond.ProviderId);
        Assert.Single(await vActions.ListAsync());
    }

    /// <summary>When the connector is not a subscription connector or the name is empty, then the add is refused before any sign-in starts (REQ-FN-016).</summary>
    [Fact]
    public async Task AddSubscriptionAsyncRefusesAnUnknownConnectorOrAnEmptyName()
    {
        var vSignIn = new FakeSubscriptionSignIn();
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), vSignIn);

        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddSubscriptionAsync("X", "Anthropic", (_, _) => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentException>(() => vActions.AddSubscriptionAsync(" ", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask));

        Assert.Equal(0, vSignIn.CallCount);
    }

    /// <summary>
    /// When a subscription provider is tested, then it is connected exactly while a session is filed in
    /// the secret store and failed once it is gone — no model call is spent on it (REQ-UI-019, REQ-FN-016).
    /// </summary>
    [Fact]
    public async Task TestAsyncChecksTheSignedInSessionForASubscription()
    {
        var vSecrets = new InMemorySecretStore();
        var vProbe = new FakeLlmConnectionProbe();
        var vSignIn = new FakeSubscriptionSignIn { SessionStore = new SecretSubscriptionSessionStore(vSecrets) };
        var vActions = new ProviderActions(CreateFactory(), vSecrets, vProbe, vSignIn);
        var vProvider = await vActions.AddSubscriptionAsync("My ChatGPT", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask);

        var vSignedIn = await vActions.TestAsync(vProvider.ProviderId);
        await vSecrets.DeleteAsync(SecretSubscriptionSessionStore.SecretNameFor("chatgpt-subscription"));
        var vSignedOut = await vActions.TestAsync(vProvider.ProviderId);

        Assert.True(vSignedIn.Succeeded);
        Assert.False(vSignedOut.Succeeded);
        Assert.Null(vProbe.LastConnector);
    }

    /// <summary>When a subscription provider is removed, then its filed session is deleted with it (REQ-UI-020, REQ-FN-017).</summary>
    [Fact]
    public async Task RemoveAsyncDeletesTheSubscriptionSession()
    {
        var vSecrets = new InMemorySecretStore();
        var vSignIn = new FakeSubscriptionSignIn { SessionStore = new SecretSubscriptionSessionStore(vSecrets) };
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe(), vSignIn);
        var vProvider = await vActions.AddSubscriptionAsync("My ChatGPT", ProviderActions.ChatGptSubscriptionConnector, (_, _) => Task.CompletedTask);

        await vActions.RemoveAsync(vProvider.ProviderId);

        Assert.False(vSecrets.Contains(SecretSubscriptionSessionStore.SecretNameFor("chatgpt-subscription")));
    }

    /// <summary>When the terms are asked for, then the ChatGPT connector's own recorded terms come back and any other connector has none (REQ-FN-016).</summary>
    [Fact]
    public void SubscriptionTermsComeFromTheCatalogForChatGptOnly()
    {
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());

        var vTerms = vActions.SubscriptionTerms(ProviderActions.ChatGptSubscriptionConnector);

        Assert.NotNull(vTerms);
        Assert.False(string.IsNullOrWhiteSpace(vTerms.Terms));
        Assert.Null(vActions.SubscriptionTerms("Anthropic"));
    }

    /// <summary>When a probe succeeds, then the provider's row moves to connected (REQ-UI-019).</summary>
    [Fact]
    public async Task TestAsyncMarksTheProviderConnectedOnSuccess()
    {
        var vProbe = new FakeLlmConnectionProbe { Succeeds = true };
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), vProbe, new FakeSubscriptionSignIn());
        var vProvider = await vActions.AddLocalAsync("My Ollama", "http://localhost:11434");

        var vResult = await vActions.TestAsync(vProvider.ProviderId);

        Assert.True(vResult.Succeeded);
        var vAfter = (await vActions.ListAsync()).Single(aRow => aRow.ProviderId == vProvider.ProviderId);
        Assert.Equal(ProviderState.Connected, vAfter.State);
    }

    /// <summary>When a probe fails, then the provider's row moves to failed and carries the message (REQ-UI-019).</summary>
    [Fact]
    public async Task TestAsyncMarksTheProviderFailedOnFailure()
    {
        var vProbe = new FakeLlmConnectionProbe { Succeeds = false };
        var vActions = new ProviderActions(CreateFactory(), new InMemorySecretStore(), vProbe, new FakeSubscriptionSignIn());
        var vProvider = await vActions.AddLocalAsync("My Ollama", "http://localhost:11434");

        var vResult = await vActions.TestAsync(vProvider.ProviderId);

        Assert.False(vResult.Succeeded);
        Assert.NotNull(vResult.Message);
        var vAfter = (await vActions.ListAsync()).Single(aRow => aRow.ProviderId == vProvider.ProviderId);
        Assert.Equal(ProviderState.Failed, vAfter.State);
    }

    /// <summary>When a provider is removed, then it leaves the list and its secret is deleted (REQ-UI-020).</summary>
    [Fact]
    public async Task RemoveAsyncDeletesTheProviderAndItsSecret()
    {
        var vSecrets = new InMemorySecretStore();
        var vActions = new ProviderActions(CreateFactory(), vSecrets, new FakeLlmConnectionProbe(), new FakeSubscriptionSignIn());
        var vProvider = await vActions.AddWithKeyAsync("My Anthropic", "Anthropic", "https://api.anthropic.com", "sk-ant-secret");
        var vSecretName = $"chatur.provider.{vProvider.ProviderId}";

        await vActions.RemoveAsync(vProvider.ProviderId);

        Assert.DoesNotContain(await vActions.ListAsync(), aRow => aRow.ProviderId == vProvider.ProviderId);
        Assert.False(vSecrets.Contains(vSecretName));
    }
}
