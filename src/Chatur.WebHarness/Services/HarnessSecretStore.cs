using System.Collections.Concurrent;
using Chatur.Core.Platform;

namespace Chatur.WebHarness.Services;

/// <summary>
/// An in-process, in-memory secret store for the browser checks — honestly named for what it is,
/// never a stand-in for the real Credential Manager or Keychain store the MAUI head uses
/// (Foundation brief "harness platform services").
/// </summary>
public sealed class HarnessSecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> objSecrets = new();

    /// <inheritdoc />
    public Task SaveAsync(string aName, string aSecret, CancellationToken aCt = default)
    {
        objSecrets[aName] = aSecret;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> ReadAsync(string aName, CancellationToken aCt = default) =>
        Task.FromResult(objSecrets.TryGetValue(aName, out var vSecret) ? vSecret : null);

    /// <inheritdoc />
    public Task DeleteAsync(string aName, CancellationToken aCt = default)
    {
        objSecrets.TryRemove(aName, out _);
        return Task.CompletedTask;
    }
}
