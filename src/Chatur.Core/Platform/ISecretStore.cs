namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over the operating system's own secret store — Windows Credential Manager on
/// Windows, the Keychain on Mac Catalyst (Architecture §5, Coding Standards "Secrets go to the
/// operating system's store"). <see cref="Chatur.Core"/> never holds a secret itself: the database
/// keeps only the name a secret is filed under, and that name is what every method here takes.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Writes a secret to the store, replacing any secret already filed under the same name.
    /// </summary>
    /// <param name="aName">The name the secret is filed under — never the secret's own value.</param>
    /// <param name="aSecret">The secret value.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task SaveAsync(string aName, string aSecret, CancellationToken aCt = default);

    /// <summary>
    /// Reads a secret back from the store.
    /// </summary>
    /// <param name="aName">The name the secret is filed under.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    /// <returns>The secret, or <see langword="null"/> when nothing is filed under that name.</returns>
    Task<string?> ReadAsync(string aName, CancellationToken aCt = default);

    /// <summary>
    /// Removes a secret from the store. A name that was never filed is not an error.
    /// </summary>
    /// <param name="aName">The name the secret is filed under.</param>
    /// <param name="aCt">A token that cancels the delete.</param>
    Task DeleteAsync(string aName, CancellationToken aCt = default);
}
