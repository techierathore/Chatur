using System.Text.Json;
using Chatur.Core.Platform;
using TechieRag.Abstractions;
using TechieRag.Models;

namespace Chatur.Core.Models;

/// <summary>
/// TechieRag's <see cref="ISubscriptionSessionStore"/> over <see cref="ISecretStore"/> (REQ-FN-016,
/// REQ-FN-017): a subscription sign-in session carries bearer tokens, so it is filed in the operating
/// system's own store — Windows Credential Manager or the Mac Keychain — and never in the database or
/// a file. The database keeps only <see cref="SecretNameFor"/>'s name. TechieRag calls
/// <see cref="LoadAsync"/> before the first model call, <see cref="SaveAsync"/> after a sign-in and
/// after every token refresh, and <see cref="ClearAsync"/> when the vendor rejects the session, so the
/// owner signs in once per device.
/// </summary>
public sealed class SecretSubscriptionSessionStore : ISubscriptionSessionStore
{
    private readonly ISecretStore objSecrets;

    /// <summary>Creates the store.</summary>
    /// <param name="aSecrets">The operating system's own secret store.</param>
    public SecretSubscriptionSessionStore(ISecretStore aSecrets)
    {
        objSecrets = aSecrets;
    }

    /// <summary>
    /// The name a connector's session is filed under in <see cref="ISecretStore"/> — also what the
    /// <c>Provider</c> row's <c>SecretName</c> holds for a subscription provider.
    /// </summary>
    /// <param name="aConnectorName">TechieRag's catalog connector, e.g. <c>chatgpt-subscription</c>.</param>
    public static string SecretNameFor(string aConnectorName) => $"chatur.subscription.{aConnectorName}";

    /// <inheritdoc />
    public async Task<SubscriptionSession?> LoadAsync(string connectorName, CancellationToken cancellationToken = default)
    {
        var vJson = await objSecrets.ReadAsync(SecretNameFor(connectorName), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vJson))
        {
            return null;
        }

        try
        {
            var vStored = JsonSerializer.Deserialize<StoredSession>(vJson);
            return vStored is null || string.IsNullOrEmpty(vStored.AccessToken)
                ? null
                : new SubscriptionSession
                {
                    ConnectorName = vStored.ConnectorName ?? connectorName,
                    AccessToken = vStored.AccessToken,
                    RefreshToken = vStored.RefreshToken,
                    IdToken = vStored.IdToken,
                    AccountId = vStored.AccountId,
                    ExpiresAt = vStored.ExpiresAt,
                };
        }
        catch (JsonException)
        {
            // An unreadable entry is the same as no sign-in: the owner is asked to sign in again.
            return null;
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(SubscriptionSession session, CancellationToken cancellationToken = default)
    {
        var vStored = new StoredSession(
            session.ConnectorName,
            session.AccessToken,
            session.RefreshToken,
            session.IdToken,
            session.AccountId,
            session.ExpiresAt);
        return objSecrets.SaveAsync(SecretNameFor(session.ConnectorName), JsonSerializer.Serialize(vStored), cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(string connectorName, CancellationToken cancellationToken = default) =>
        objSecrets.DeleteAsync(SecretNameFor(connectorName), cancellationToken);

    /// <summary>The session's own fields as they are written to the secret store.</summary>
    private sealed record StoredSession(
        string? ConnectorName,
        string? AccessToken,
        string? RefreshToken,
        string? IdToken,
        string? AccountId,
        DateTimeOffset? ExpiresAt);
}
