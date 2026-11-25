using System.Security.Cryptography;
using System.Text;

namespace Chatur.Core.Integrations;

/// <summary>
/// The token made for another tool: <c>chatur_pat_{account}_{32 hex}</c>. The account is not a secret —
/// it lets an outside call find the account's own database before the secret part is checked there
/// (each account has its own database, BRD-210). Only a hash of the whole token is ever kept.
/// </summary>
public static class IntegrationToken
{
    private const string Prefix = "chatur_pat_";
    private const int SecretLength = 32;

    /// <summary>The account name used when nobody is signed in.</summary>
    public const string InstallAccount = "install";

    /// <summary>Makes a new token for an account (<see langword="null"/> for the install).</summary>
    /// <param name="aAccount">The signed-in account's id.</param>
    /// <returns>The whole token.</returns>
    public static string Make(string? aAccount) =>
        $"{Prefix}{aAccount ?? InstallAccount}_{Convert.ToHexString(RandomNumberGenerator.GetBytes(SecretLength / 2)).ToLowerInvariant()}";

    /// <summary>Reads which account a token was made for, without checking the secret.</summary>
    /// <param name="aToken">The token as sent.</param>
    /// <param name="aAccount">The account's id; <see langword="null"/> for the install.</param>
    /// <returns><see langword="false"/> when the text is not shaped like a token.</returns>
    public static bool TryReadAccount(string? aToken, out string? aAccount)
    {
        aAccount = null;
        if (aToken is null || !aToken.StartsWith(Prefix, StringComparison.Ordinal) || aToken.Length < Prefix.Length + 1 + 1 + SecretLength)
        {
            return false;
        }

        var vSecretStart = aToken.Length - SecretLength;
        if (aToken[vSecretStart - 1] != '_' || !aToken[vSecretStart..].All(Uri.IsHexDigit))
        {
            return false;
        }

        var vAccount = aToken[Prefix.Length..(vSecretStart - 1)];
        aAccount = vAccount == InstallAccount ? null : vAccount;
        return vAccount.Length > 0;
    }

    /// <summary>The hash kept in place of a token.</summary>
    /// <param name="aToken">The whole token.</param>
    public static string Hash(string aToken) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(aToken)));

    /// <summary>The first and last characters, enough to tell tokens apart without being able to use one.</summary>
    /// <param name="aToken">The whole token.</param>
    public static string Hint(string aToken) => $"{aToken[..(Prefix.Length + 2)]}…{aToken[^4..]}";
}
