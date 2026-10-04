using Chatur.Core.Actions;

namespace Chatur.Core.Models;

/// <summary>
/// Calls a provider once, through TechieRag, and reports whether it answered (Architecture §7
/// "Models" — "every model call goes through it"). A seam so <see cref="ProviderActions"/> can be
/// unit-tested without a real network call (Coding Standards §Testability).
/// </summary>
public interface ILlmConnectionProbe
{
    /// <summary>
    /// Builds a minimal TechieRag client for one provider and asks it for a single reply.
    /// </summary>
    /// <param name="aConnector">Which connector to use, e.g. <c>"Anthropic"</c>, <c>"OpenAI"</c>, <c>"Google"</c>, <c>"Ollama"</c>.</param>
    /// <param name="aBaseUrl">The provider's web address — read for a local or OpenAI-compatible connector, ignored otherwise.</param>
    /// <param name="aSecret">The key, or <see langword="null"/> for a connector that needs none.</param>
    /// <param name="aCt">A token that cancels the call.</param>
    Task<ProviderTestResult> ProbeAsync(string aConnector, string aBaseUrl, string? aSecret, CancellationToken aCt = default);
}
