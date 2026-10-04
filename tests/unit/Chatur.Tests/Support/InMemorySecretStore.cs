using Chatur.Core.Platform;

namespace Chatur.Tests.Support;

/// <summary>
/// An in-memory <see cref="ISecretStore"/> for tests — the real stores are the Windows Credential
/// Manager and the Mac Keychain, neither reachable from a unit test (Coding Standards §Testability).
/// </summary>
public sealed class InMemorySecretStore : ISecretStore
{
    private readonly Dictionary<string, string> objSecrets = new();

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
        objSecrets.Remove(aName);
        return Task.CompletedTask;
    }

    /// <summary>Whether a secret is currently filed under <paramref name="aName"/> — for test assertions.</summary>
    public bool Contains(string aName) => objSecrets.ContainsKey(aName);
}
