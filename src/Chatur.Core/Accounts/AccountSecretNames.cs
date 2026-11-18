namespace Chatur.Core.Accounts;

/// <summary>
/// The names App Manager's tokens are filed under in <see cref="Platform.ISecretStore"/> — never the
/// tokens themselves (Coding Standards "Secrets go to the operating system's store"; REQ-FN-003,
/// REQ-FN-009). Both the code that persists a token on sign-in (REQ-FN-003, cluster B) and the code
/// that removes it on sign-out (REQ-FN-009, cluster G) file it under exactly these names.
/// </summary>
public static class AccountSecretNames
{
    /// <summary>The name App Manager's access token is filed under.</summary>
    public const string AccessToken = "AppManager.AccessToken";

    /// <summary>The name App Manager's refresh token is filed under.</summary>
    public const string RefreshToken = "AppManager.RefreshToken";

    /// <summary>
    /// The name the signed-in owner's App Manager user id is filed under, so
    /// <c>CurrentUserAsync</c> can rebuild <see cref="Actions.CurrentUser"/> after a restart
    /// (REQ-FN-003) without a network call.
    /// </summary>
    public const string UserId = "AppManager.UserId";

    /// <summary>The name the signed-in owner's email address is filed under (REQ-FN-003).</summary>
    public const string Email = "AppManager.Email";

    /// <summary>The name the signed-in owner's display name is filed under (REQ-FN-003).</summary>
    public const string DisplayName = "AppManager.DisplayName";

    /// <summary>
    /// The name the access token's expiry (round-trip UTC, <see cref="DateTime.ToString(string)"/>
    /// with <c>"O"</c>) is filed under, so a restart can tell whether it must refresh before use
    /// (REQ-FN-003).
    /// </summary>
    public const string TokenExpiresAtUtc = "AppManager.TokenExpiresAtUtc";

    /// <summary>
    /// The name one account's own secret — a provider key, a subscription session — is filed under:
    /// <c>chatur.account.&lt;user id&gt;.&lt;name&gt;</c>, so two accounts on one install never share or
    /// overwrite a secret (BRD-212). With no account (a host or test with no sign-in) it is the name as
    /// given, as it was before accounts had secrets of their own; a name kept that way is never read
    /// for an account.
    /// </summary>
    /// <param name="aUserId">The account's App Manager user id, or <see langword="null"/>.</param>
    /// <param name="aName">The secret's own name, e.g. <c>chatur.provider.3</c>.</param>
    public static string ForAccount(string? aUserId, string aName)
    {
        if (string.IsNullOrWhiteSpace(aUserId))
        {
            return aName;
        }

        var vRest = aName.StartsWith("chatur.", StringComparison.Ordinal) ? aName["chatur.".Length..] : aName;
        return $"chatur.account.{Data.SqliteConnectionFactory.AccountFolderName(aUserId)}.{vRest}";
    }
}
