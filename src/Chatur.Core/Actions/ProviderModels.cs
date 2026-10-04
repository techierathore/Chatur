namespace Chatur.Core.Actions;

/// <summary>Whether a provider has been shown to answer (REQ-UI-019).</summary>
public enum ProviderState
{
    /// <summary>The provider has not been tested since it was added.</summary>
    NotTestedYet,

    /// <summary>The provider answered the last time it was tested.</summary>
    Connected,

    /// <summary>The provider failed the last time it was tested.</summary>
    Failed
}

/// <summary>One row of the Model providers table (REQ-FN-014, REQ-FN-015, REQ-FN-016).</summary>
/// <param name="ProviderId">The row's identity.</param>
/// <param name="Name">The name the owner gave the provider.</param>
/// <param name="Connector">Which connector this provider uses, e.g. <c>"Anthropic"</c>, <c>"Ollama"</c>.</param>
/// <param name="SignInMethod">How this provider authenticates: pasted key, browser sign-in, or none.</param>
/// <param name="BaseUrl">The provider's web address.</param>
/// <param name="State">Whether the provider has been shown to answer.</param>
public sealed record ModelProvider(
    int ProviderId,
    string Name,
    string Connector,
    string SignInMethod,
    string BaseUrl,
    ProviderState State);

/// <summary>The result of testing a provider (REQ-UI-019).</summary>
/// <param name="Succeeded">Whether the provider answered.</param>
/// <param name="Message">The provider's own message when it did not answer.</param>
/// <param name="Models">The models the provider reports it can serve.</param>
public sealed record ProviderTestResult(bool Succeeded, string? Message, IReadOnlyList<string> Models);

/// <summary>
/// A model a provider serves, in a tier, available to be added to a routing tier's fallback chain
/// (REQ-FN-014, REQ-FN-015, REQ-FN-020).
/// </summary>
/// <param name="ModelId">The row's identity — what a <c>RoutingTier</c> orders by.</param>
/// <param name="ProviderId">The provider that serves this model.</param>
/// <param name="ProviderName">The provider's name, for display beside the model.</param>
/// <param name="Tier">The tier this model belongs to, 1 to 3.</param>
/// <param name="Identifier">The model's own identifier, e.g. <c>"claude-sonnet-5"</c>.</param>
public sealed record ModelSummary(int ModelId, int ProviderId, string ProviderName, int Tier, string Identifier);

/// <summary>
/// What the Add a provider dialog shows the owner so they can authorise a subscription sign-in
/// (REQ-FN-016, REQ-UI-019; the <c>signin-code-panel</c> of <c>mockups/settings-providers.html</c>).
/// </summary>
/// <param name="VerificationUrl">The vendor's page the owner opens to authorise Chatur; the UI opens it in the browser and also shows it.</param>
/// <param name="UserCode">The one-time code the owner types on that page.</param>
/// <param name="ExpiresAt">When the code stops working (OpenAI's last 15 minutes).</param>
public sealed record SignInCodePrompt(string VerificationUrl, string UserCode, DateTimeOffset ExpiresAt);

/// <summary>
/// A vendor's own stated terms for using a consumer subscription from another app, to show before
/// sign-in (REQ-FN-016). Facts recorded by TechieRag with the date they were checked, not Chatur's rules.
/// </summary>
/// <param name="Permitted">Whether the vendor permits a third-party app to use the subscription this way.</param>
/// <param name="Terms">The terms as text, in the vendor's own words where it has them.</param>
/// <param name="AppliesTo">Who the terms cover, e.g. individual plans.</param>
/// <param name="CheckedOn">The date the terms were last checked against the vendor's live documentation.</param>
public sealed record SubscriptionTermsInfo(bool Permitted, string Terms, string AppliesTo, DateOnly CheckedOn);
