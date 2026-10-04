namespace Chatur.Core.Accounts;

/// <summary>
/// The password rule App Manager enforces server-side (docs/AppManager-api-usage-guide.md §2.4,
/// §3.1 "Password Requirements"): at least 8 characters, an uppercase letter, a digit and a special
/// character. Evaluated once here so Register's live display (REQ-UI-003, cluster D) and the refusal
/// to send a password that fails it (REQ-FN-005) always agree with what App Manager itself will
/// accept or reject.
/// </summary>
public static class PasswordRule
{
    /// <summary>The special characters App Manager's own rule accepts (mockups/register.html "rule-special").</summary>
    private const string SpecialCharacters = "!?#@$%^&*()_+-=[]{}|;:,.<>/~`\"'\\";

    /// <summary>
    /// Checks a candidate password against every part of the rule.
    /// </summary>
    /// <param name="aPassword">The password typed on Register. <see langword="null"/> is treated as empty.</param>
    /// <returns>Which parts of the rule are met.</returns>
    public static PasswordRuleStatus Evaluate(string? aPassword)
    {
        var vPassword = aPassword ?? string.Empty;
        var vHasLength = vPassword.Length >= 8;
        var vHasCapital = vPassword.Any(char.IsUpper);
        var vHasNumber = vPassword.Any(char.IsDigit);
        var vHasSpecial = vPassword.Any(c => SpecialCharacters.Contains(c));
        return new PasswordRuleStatus(vHasLength, vHasCapital, vHasNumber, vHasSpecial);
    }
}

/// <summary>Which parts of <see cref="PasswordRule"/> a password meets.</summary>
/// <param name="HasLength">At least 8 characters.</param>
/// <param name="HasCapital">Contains an uppercase letter.</param>
/// <param name="HasNumber">Contains a digit.</param>
/// <param name="HasSpecial">Contains one of <see cref="PasswordRule"/>'s special characters.</param>
public sealed record PasswordRuleStatus(bool HasLength, bool HasCapital, bool HasNumber, bool HasSpecial)
{
    /// <summary>Whether every part of the rule is met — the password App Manager will accept.</summary>
    public bool MeetsRule => HasLength && HasCapital && HasNumber && HasSpecial;
}
