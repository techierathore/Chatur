using TechieRag.Abstractions;

namespace Chatur.Core.Agent;

/// <summary>
/// One model of a connected provider, resolved from the <c>Provider</c>/<c>Model</c> tables, with
/// enough to build a TechieRag <see cref="ILlmProvider"/> for it (REQ-FN-025).
/// </summary>
/// <param name="ModelId">The row's identity in the <c>Model</c> table.</param>
/// <param name="ProviderId">The owning row's identity in the <c>Provider</c> table.</param>
/// <param name="ProviderName">The name the owner gave the provider, for an error message.</param>
/// <param name="Connector">Which TechieRag connector this provider uses, e.g. <c>"Anthropic"</c>, <c>"Ollama"</c>.</param>
/// <param name="BaseUrl">The provider's web address; used by a connector that is not key-only.</param>
/// <param name="SecretName">The name the provider's key is filed under in <see cref="Platform.ISecretStore"/>, or <see langword="null"/> for a local model that needs none.</param>
/// <param name="ModelIdentifier">The model's own identifier at the provider, e.g. <c>"claude-sonnet-4-5"</c>.</param>
public sealed record ConnectedModel(
    int ModelId,
    int ProviderId,
    string ProviderName,
    string Connector,
    string BaseUrl,
    string? SecretName,
    string ModelIdentifier);

/// <summary>
/// Builds the TechieRag <see cref="ILlmProvider"/> for one connected model (Architecture §7 "Agent
/// loop --&gt; Models --&gt; TechieRag"). A seam so the agent loop can be unit-tested with a fake chat
/// client instead of a real TechieRag builder and a real network call (Coding Standards
/// §Testability).
/// </summary>
public interface IAgentLlmProviderFactory
{
    /// <summary>
    /// Builds the LLM provider for one connected model, reading its secret (if any) from the secret
    /// store.
    /// </summary>
    /// <param name="aModel">The model to build a provider for.</param>
    /// <param name="aCt">A token that cancels the build.</param>
    Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default);
}
