namespace Chatur.Core.Actions;

/// <summary>
/// Connecting, testing and removing model providers (Architecture §7 "Models"; page Settings ▸
/// Model providers). Every secret goes to <see cref="Platform.ISecretStore"/>, never to the
/// database (REQ-FN-017).
/// </summary>
public interface IProviderActions
{
    /// <summary>Every connected provider (REQ-UI-019, REQ-UI-020).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ModelProvider>> ListAsync(CancellationToken aCt = default);

    /// <summary>
    /// Adds a provider that signs in with a pasted key (REQ-FN-014, REQ-FN-017).
    /// </summary>
    /// <param name="aName">A name the owner will recognise.</param>
    /// <param name="aConnector">Which connector this provider uses: <c>"Anthropic"</c>, <c>"OpenAI"</c>,
    /// <c>"Google"</c>, or <c>"OpenAICompatible"</c> — for the last, the models are read from the
    /// service's own model list, so an add that cannot reach it fails.</param>
    /// <param name="aBaseUrl">The provider's web address.</param>
    /// <param name="aKey">The key, filed in the operating system's secret store and never in the database.</param>
    /// <param name="aCt">A token that cancels the add.</param>
    Task<ModelProvider> AddWithKeyAsync(string aName, string aConnector, string aBaseUrl, string aKey, CancellationToken aCt = default);

    /// <summary>
    /// Adds a local model that needs no sign-in (REQ-FN-015).
    /// </summary>
    /// <param name="aName">A name the owner will recognise.</param>
    /// <param name="aBaseUrl">The local server's web address.</param>
    /// <param name="aCt">A token that cancels the add.</param>
    Task<ModelProvider> AddLocalAsync(string aName, string aBaseUrl, CancellationToken aCt = default);

    /// <summary>
    /// Adds a ChatGPT subscription provider through OpenAI's device-code sign-in (REQ-FN-016). The
    /// call stays open while the owner signs in: <paramref name="aOnSignInCode"/> is called once with
    /// the page and code, the UI shows the code (the <c>signin-code-panel</c> of
    /// <c>mockups/settings-providers.html</c>) and opens the browser, then returns; the add completes
    /// when the owner has authorised the code, and fails — saving nothing — when the code expires, the
    /// vendor refuses, or <paramref name="aCt"/> is cancelled (the panel's Cancel). The signed-in
    /// session is kept in the operating system's secret store, so the owner signs in once per device;
    /// adding it again while it exists signs in again and reuses the row. Show
    /// <see cref="SubscriptionTerms"/> before starting.
    /// </summary>
    /// <param name="aName">A name the owner will recognise.</param>
    /// <param name="aConnector">Which subscription connector: <see cref="Models.ProviderActions.ChatGptSubscriptionConnector"/> (<c>"ChatGptSubscription"</c>), the only one TechieRag offers.</param>
    /// <param name="aOnSignInCode">Called once, before the add completes, with the verification page and one-time code.</param>
    /// <param name="aCt">A token that cancels the add and abandons the sign-in.</param>
    /// <exception cref="ArgumentException">The name is empty or the connector is not a subscription connector.</exception>
    /// <exception cref="TechieRag.Llm.SubscriptionSignInException">The code expired or the vendor refused; its <c>Code</c> says which.</exception>
    Task<ModelProvider> AddSubscriptionAsync(string aName, string aConnector, Func<SignInCodePrompt, CancellationToken, Task> aOnSignInCode, CancellationToken aCt = default);

    /// <summary>
    /// The vendor's stated terms for using a subscription from another app, with the date they were
    /// checked, to show before <see cref="AddSubscriptionAsync"/> (REQ-FN-016). Read from TechieRag's
    /// catalog; no network call.
    /// </summary>
    /// <param name="aConnector">A subscription connector, as for <see cref="AddSubscriptionAsync"/>.</param>
    /// <returns>The terms, or <see langword="null"/> when the connector is not a subscription connector.</returns>
    SubscriptionTermsInfo? SubscriptionTerms(string aConnector);

    /// <summary>
    /// Calls the provider once and reports what came back (REQ-UI-019).
    /// </summary>
    /// <param name="aProviderId">The provider to test.</param>
    /// <param name="aCt">A token that cancels the test.</param>
    Task<ProviderTestResult> TestAsync(int aProviderId, CancellationToken aCt = default);

    /// <summary>
    /// Calls a provider that has not been saved yet, from the Add a provider dialog's Test button,
    /// and reports what answered (REQ-UI-019, mockups/settings-providers.html "Nothing is saved yet").
    /// Nothing is written to the database or the secret store.
    /// </summary>
    /// <param name="aConnector">Which connector to use: <c>"Anthropic"</c>, <c>"OpenAI"</c>, <c>"OpenAICompatible"</c>, <c>"Google"</c> or <c>"Ollama"</c>.</param>
    /// <param name="aBaseUrl">The provider's web address.</param>
    /// <param name="aKey">The pasted key, or <see langword="null"/> for a local model.</param>
    /// <param name="aCt">A token that cancels the test.</param>
    Task<ProviderTestResult> TestUnsavedAsync(string aConnector, string aBaseUrl, string? aKey, CancellationToken aCt = default);

    /// <summary>
    /// Removes a provider and deletes its secret from the store (REQ-UI-020).
    /// </summary>
    /// <param name="aProviderId">The provider to remove.</param>
    /// <param name="aCt">A token that cancels the removal.</param>
    Task RemoveAsync(int aProviderId, CancellationToken aCt = default);

    /// <summary>
    /// Every model discovered across every connected provider, so routing can choose one (REQ-FN-014,
    /// REQ-FN-015, REQ-FN-020).
    /// </summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ModelSummary>> ListModelsAsync(CancellationToken aCt = default);
}
