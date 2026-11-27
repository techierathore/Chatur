using TechieRag.Abstractions;

namespace Chatur.Core.Measurements;

/// <summary>
/// Writes one <c>KeyUsage</c> row per model call Chatur makes through a connected provider, with the price TechieRag
/// gives for it, so Measurements can count what a key has used since a date the person typed (REQ-UI-131, REQ-FN-326;
/// Architecture decisions log 2026-11-27). Every provider built by <see cref="Agent.IAgentLlmProviderFactory"/> is
/// watched already; a caller that builds its own TechieRag provider calls <see cref="Watch"/> on it, or
/// <see cref="Record"/> once per call.
/// </summary>
public interface IKeyUsageRecorder
{
    /// <summary>
    /// Records every call <paramref name="aProvider"/> completes from now on, against <paramref name="aProviderId"/>.
    /// A recording that fails never fails the call.
    /// </summary>
    /// <param name="aProvider">The TechieRag provider whose completed calls are counted.</param>
    /// <param name="aProviderId">The <c>Provider</c> row the calls are made through.</param>
    /// <param name="aModelIdentifier">The model's own identifier at the provider, e.g. <c>"claude-sonnet-5-5"</c>.</param>
    void Watch(ILlmProvider aProvider, int aProviderId, string aModelIdentifier);

    /// <summary>
    /// Records one call. Its price is TechieRag's for the model; with no price (TechieRag has none for the model, or the
    /// tokens were estimated because the provider sent no usage) the row's cost is left empty, never guessed.
    /// </summary>
    /// <param name="aProviderId">The <c>Provider</c> row the call was made through.</param>
    /// <param name="aModelIdentifier">The model's own identifier at the provider.</param>
    /// <param name="aTokensIn">Input tokens the provider reported.</param>
    /// <param name="aTokensOut">Output tokens the provider reported.</param>
    /// <param name="aIsEstimated">Whether the token counts were estimated because the provider sent none.</param>
    void Record(int aProviderId, string aModelIdentifier, int aTokensIn, int aTokensOut, bool aIsEstimated);
}
