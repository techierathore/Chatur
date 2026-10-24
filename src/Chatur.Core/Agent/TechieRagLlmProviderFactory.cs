using System.Reflection;
using Chatur.Core.Platform;
using TechieRag;
using TechieRag.Abstractions;
using TechieRag.Llm;

namespace Chatur.Core.Agent;

/// <summary>
/// <see cref="IAgentLlmProviderFactory"/> over a real <see cref="TechieRagBuilder"/> (REQ-FN-025,
/// REQ-FN-026; "Every model call goes through TechieRag"). Every model key is read from
/// <see cref="ISecretStore"/> and never held past this one call (Coding Standards "Secrets go to the
/// operating system's store").
/// </summary>
public sealed class TechieRagLlmProviderFactory : IAgentLlmProviderFactory
{
    private readonly ISecretStore objSecretStore;
    private readonly ISubscriptionSessionStore objSessionStore;

    /// <summary>Creates the factory.</summary>
    /// <param name="aSecretStore">Where a connected provider's key or token is filed.</param>
    /// <param name="aSessionStore">Where a subscription provider's signed-in session is kept (REQ-FN-016).</param>
    public TechieRagLlmProviderFactory(ISecretStore aSecretStore, ISubscriptionSessionStore aSessionStore)
    {
        objSecretStore = aSecretStore;
        objSessionStore = aSessionStore;
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// <paramref name="aModel"/>'s connector is not one TechieRag 1.0.8 ships a builder method for.
    /// Filed in <c>docs/Chatur-TechieRag-Feedback.md</c> rather than worked around here.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A subscription model whose owner is no longer signed in: a model turn has no window to show a
    /// sign-in code in, so the owner signs in again under Settings ▸ Providers instead.
    /// </exception>
    public async Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default)
    {
        var vSecret = aModel.SecretName is null
            ? null
            : await objSecretStore.ReadAsync(aModel.SecretName, aCt).ConfigureAwait(false);

        // Connector names match Models.TechieRagConnectionProbe's own convention (cluster K/L,
        // REQ-UI-019) — "Anthropic", "OpenAI", "Google", "Ollama" — plus a couple of aliases so a
        // provider row spelled either way still resolves.
        var vBuilder = new TechieRagBuilder();
        if (string.Equals(aModel.Connector, "ChatGptSubscription", StringComparison.OrdinalIgnoreCase))
        {
            vBuilder = await UseStoredSubscriptionAsync(vBuilder, aModel, aCt).ConfigureAwait(false);
        }
        else
        {
            vBuilder = aModel.Connector.ToLowerInvariant() switch
            {
                "anthropic" => vBuilder.UseAnthropicLlm(vSecret ?? string.Empty, aModel.ModelIdentifier),
                // Every OpenAI-compatible call names Chatur in User-Agent, and a service that routes
                // by conversation (OpenCode Go's x-opencode-session) gets its session header; the id
                // itself travels per call in LlmCompletionOptions.SessionId (TechieRag 1.0.9,
                // TR-RAG-003 closed).
                "openai" or "openaicompatible" => vBuilder.UseOpenAICompatibleLlm(
                    aModel.BaseUrl,
                    vSecret ?? string.Empty,
                    aModel.ModelIdentifier,
                    ChaturHeaders,
                    SessionHeaderFor(aModel.BaseUrl)),
                "google" or "gemini" or "googlegemini" => vBuilder.UseGeminiLlm(vSecret ?? string.Empty, aModel.ModelIdentifier),
                "ollama" => vBuilder.UseOllamaLlm(aModel.BaseUrl, aModel.ModelIdentifier),
                "lmstudio" => vBuilder.UseLmStudioLlm(aModel.BaseUrl, aModel.ModelIdentifier),
                "azureaifoundry" => vBuilder.UseAzureAIFoundryLlm(aModel.BaseUrl, vSecret ?? string.Empty, aModel.ModelIdentifier, "2024-10-21"),
                _ => throw new NotSupportedException(
                    $"Provider connector \"{aModel.Connector}\" has no TechieRag builder method yet — see docs/Chatur-TechieRag-Feedback.md."),
            };
        }

        var vRag = vBuilder.Build();
        var vProvider = vRag.GetLlmProvider();
        if (vProvider is null)
        {
            throw new InvalidOperationException($"TechieRag returned no LLM provider for \"{aModel.ProviderName}\".");
        }

        return vProvider;
    }

    /// <summary>
    /// Builds a ChatGPT subscription provider from the session saved at sign-in (REQ-FN-016). TechieRag
    /// refreshes and re-saves the session through <see cref="ISubscriptionSessionStore"/> itself; when
    /// the vendor refuses it, the sign-in callback below fails the turn with a message that says
    /// where to sign in again, rather than a code nobody is shown.
    /// </summary>
    private async Task<TechieRagBuilder> UseStoredSubscriptionAsync(TechieRagBuilder aBuilder, ConnectedModel aModel, CancellationToken aCt)
    {
        var vSession = await objSessionStore.LoadAsync(Models.ProviderActions.ChatGptCatalogName, aCt).ConfigureAwait(false);
        if (vSession is null)
        {
            throw new InvalidOperationException(SignInAgainMessage(aModel.ProviderName));
        }

        return aBuilder.UseChatGptSubscriptionLlm(
            (_, _) => Task.FromException(new InvalidOperationException(SignInAgainMessage(aModel.ProviderName))),
            new ChatGptSubscriptionOptions { Model = aModel.ModelIdentifier, SessionStore = objSessionStore });
    }

    /// <summary>The headers Chatur sends on every OpenAI-compatible call: its own name, as OpenCode Go asks of each client.</summary>
    private static readonly IReadOnlyDictionary<string, string> ChaturHeaders = new Dictionary<string, string>
    {
        ["User-Agent"] = $"Chatur/{ChaturVersion()}"
    };

    /// <summary>
    /// The per-conversation session header of the TechieRag connector whose endpoint is
    /// <paramref name="aBaseUrl"/> (e.g. <c>x-opencode-session</c> for OpenCode Go), or
    /// <see langword="null"/> for a service that reads none.
    /// </summary>
    /// <param name="aBaseUrl">The provider's web address as the owner typed it.</param>
    public static string? SessionHeaderFor(string aBaseUrl)
    {
        static string Normalise(string aUrl) => aUrl.Trim().TrimEnd('/').ToLowerInvariant();
        var vBase = Normalise(aBaseUrl);
        return LlmConnectorCatalog.All
            .FirstOrDefault(aConnector => aConnector.Endpoint is { } vEndpoint && Normalise(vEndpoint) == vBase)
            ?.SessionHeader;
    }

    private static string ChaturVersion()
    {
        var vVersion = typeof(TechieRagLlmProviderFactory).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var vPlus = vVersion.IndexOf('+');
        return vPlus < 0 ? vVersion : vVersion[..vPlus];
    }

    private static string SignInAgainMessage(string aProviderName) =>
        $"{aProviderName} is not signed in any more. Sign in again under Settings ▸ Providers.";
}
