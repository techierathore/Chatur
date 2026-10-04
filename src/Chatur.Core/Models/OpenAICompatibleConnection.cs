using System.Net.Http.Headers;
using System.Text.Json;

namespace Chatur.Core.Models;

/// <summary>
/// The one non-model call an OpenAI-compatible service offers: its model list (<c>GET models</c>).
/// No prompt is sent, so it costs nothing; every model call itself still goes through TechieRag
/// (Architecture §7). TechieRag has no "list the provider's models" call
/// (<c>.techierag/TechieRag-AI-Reference.md</c>), and an arbitrary service has no catalog Chatur
/// could know in advance, so the Add a provider dialog's OpenAI-compatible connector reads it here.
/// </summary>
public static class OpenAICompatibleConnection
{
    /// <summary>One pooled handler for every read, so repeated tests never exhaust sockets.</summary>
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    };

    /// <summary>
    /// Reads the service's model list, in the OpenAI shape <c>{"data":[{"id":…}]}</c>.
    /// </summary>
    /// <param name="aBaseUrl">The service's web address, e.g. <c>https://openrouter.ai/api/v1</c>.</param>
    /// <param name="aKey">The bearer key, or <see langword="null"/> for a service that needs none.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>Every model identifier the service offers, in the order it listed them.</returns>
    /// <exception cref="HttpRequestException">The service did not answer with a success code.</exception>
    public static async Task<IReadOnlyList<string>> ListModelsAsync(string aBaseUrl, string? aKey, CancellationToken aCt = default)
    {
        using var vClient = new HttpClient(SharedHandler, disposeHandler: false)
        {
            BaseAddress = new Uri(aBaseUrl.EndsWith('/') ? aBaseUrl : aBaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrEmpty(aKey))
        {
            vClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aKey);
        }

        using var vResponse = await vClient.GetAsync("models", aCt).ConfigureAwait(false);
        var vBody = await vResponse.Content.ReadAsStringAsync(aCt).ConfigureAwait(false);
        if (!vResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"The service answered {(int)vResponse.StatusCode} {vResponse.ReasonPhrase}: {Trim(vBody)}",
                null,
                vResponse.StatusCode);
        }

        return ParseModelIds(vBody);
    }

    /// <summary>Reads the model identifiers out of an OpenAI-shaped model list.</summary>
    /// <param name="aJson">The response body.</param>
    public static IReadOnlyList<string> ParseModelIds(string aJson)
    {
        using var vDocument = JsonDocument.Parse(aJson);
        if (!vDocument.RootElement.TryGetProperty("data", out var vData) || vData.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return vData.EnumerateArray()
            .Select(aModel => aModel.TryGetProperty("id", out var vId) ? vId.GetString() : null)
            .Where(aId => !string.IsNullOrWhiteSpace(aId))
            .Select(aId => aId!)
            .ToList();
    }

    private static string Trim(string aBody) => aBody.Length <= 300 ? aBody : aBody[..300] + "…";
}
