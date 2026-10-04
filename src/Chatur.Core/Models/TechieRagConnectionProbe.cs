using Chatur.Core.Actions;
using Microsoft.Extensions.Logging;
using TechieRag;

namespace Chatur.Core.Models;

/// <summary>
/// <see cref="ILlmConnectionProbe"/> built on <see cref="TechieRagBuilder"/> — direct LLM access, no
/// RAG (the "Direct LLM Access" pattern in <c>.techierag/TechieRag-AI-Reference.md</c>). Every model
/// call goes through TechieRag (Architecture §7); Chatur never talks to a provider's own SDK.
/// </summary>
public sealed class TechieRagConnectionProbe : ILlmConnectionProbe
{
    /// <summary>The connectors this probe knows how to reach, and each one's default probe model.</summary>
    private static readonly IReadOnlyDictionary<string, string> DefaultModelByConnector = new Dictionary<string, string>
    {
        ["Anthropic"] = "claude-sonnet-5",
        ["OpenAI"] = "gpt-5",
        ["Google"] = "gemini-2.5-flash",
        ["Ollama"] = "llama3.2"
    };

    private readonly ILogger<TechieRagConnectionProbe> objLogger;

    /// <summary>Creates the probe.</summary>
    /// <param name="aLogger">Where a failed probe is logged (Architecture §5 "Errors").</param>
    public TechieRagConnectionProbe(ILogger<TechieRagConnectionProbe> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public async Task<ProviderTestResult> ProbeAsync(string aConnector, string aBaseUrl, string? aSecret, CancellationToken aCt = default)
    {
        if (aConnector == "OpenAICompatible")
        {
            return await ProbeCompatibleAsync(aBaseUrl, aSecret, aCt).ConfigureAwait(false);
        }

        if (!DefaultModelByConnector.TryGetValue(aConnector, out var vModel))
        {
            return new ProviderTestResult(false, $"Chatur does not know the connector \"{aConnector}\".", Array.Empty<string>());
        }

        try
        {
            var vBuilder = new TechieRagBuilder();
            switch (aConnector)
            {
                case "Anthropic":
                    vBuilder.UseAnthropicLlm(aSecret ?? string.Empty, vModel);
                    break;
                case "OpenAI":
                    vBuilder.UseOpenAICompatibleLlm(aBaseUrl, aSecret ?? string.Empty, vModel);
                    break;
                case "Google":
                    vBuilder.UseGeminiLlm(aSecret ?? string.Empty, vModel);
                    break;
                case "Ollama":
                    vBuilder.UseOllamaLlm(aBaseUrl, vModel);
                    break;
            }

            var vRag = vBuilder.Build();
            var vLlm = vRag.GetLlmProvider();
            if (vLlm is null)
            {
                return new ProviderTestResult(false, "No LLM provider was configured.", Array.Empty<string>());
            }

            await vLlm.CompleteAsync("Reply with the single word: ready.", null, aCt).ConfigureAwait(false);
            return new ProviderTestResult(true, null, new[] { vLlm.ModelName });
        }
        catch (Exception aEx)
        {
            objLogger.LogWarning(aEx, "Provider probe failed for connector {Connector}.", aConnector);
            return new ProviderTestResult(false, aEx.Message, Array.Empty<string>());
        }
    }

    /// <summary>
    /// Tests an OpenAI-compatible service by reading its model list: there is no model name Chatur
    /// could assume for an arbitrary service, and the list both proves the address and key and names
    /// the models the owner can route to. No prompt is sent.
    /// </summary>
    private async Task<ProviderTestResult> ProbeCompatibleAsync(string aBaseUrl, string? aSecret, CancellationToken aCt)
    {
        if (!Uri.TryCreate(aBaseUrl, UriKind.Absolute, out _))
        {
            return new ProviderTestResult(false, "An OpenAI-compatible provider needs its full web address, e.g. https://example.com/v1.", Array.Empty<string>());
        }

        try
        {
            var vModels = await OpenAICompatibleConnection.ListModelsAsync(aBaseUrl, aSecret, aCt).ConfigureAwait(false);
            return vModels.Count == 0
                ? new ProviderTestResult(false, "The service answered but listed no models.", vModels)
                : new ProviderTestResult(true, null, vModels);
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException || !aCt.IsCancellationRequested)
        {
            objLogger.LogWarning(aEx, "Provider probe failed for connector {Connector}.", "OpenAICompatible");
            return new ProviderTestResult(false, aEx.Message, Array.Empty<string>());
        }
    }
}
