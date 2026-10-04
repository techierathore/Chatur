using Chatur.Core.Actions;
using Chatur.Core.Models;

namespace Chatur.Tests.Support;

/// <summary>
/// A scripted <see cref="ILlmConnectionProbe"/> — the real one calls a provider over the network
/// through TechieRag, which a unit test never does (Coding Standards §Testability).
/// </summary>
public sealed class FakeLlmConnectionProbe : ILlmConnectionProbe
{
    /// <summary>Whether the next call to <see cref="ProbeAsync"/> reports success.</summary>
    public bool Succeeds { get; set; } = true;

    /// <summary>The connector <see cref="ProbeAsync"/> was last called with.</summary>
    public string? LastConnector { get; private set; }

    /// <inheritdoc />
    public Task<ProviderTestResult> ProbeAsync(string aConnector, string aBaseUrl, string? aSecret, CancellationToken aCt = default)
    {
        LastConnector = aConnector;
        return Task.FromResult(Succeeds
            ? new ProviderTestResult(true, null, new[] { "a-model" })
            : new ProviderTestResult(false, "The provider refused the connection.", Array.Empty<string>()));
    }
}
