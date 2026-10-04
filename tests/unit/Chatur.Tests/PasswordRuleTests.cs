using Chatur.Core.Accounts;
using Xunit;

namespace Chatur.Tests;

/// <summary>Tests for <see cref="PasswordRule"/> (REQ-FN-005).</summary>
public sealed class PasswordRuleTests
{
    /// <summary>
    /// When a password has at least 8 characters, an uppercase letter, a digit and a special
    /// character, then every part of the rule is reported met.
    /// </summary>
    [Fact]
    public void EvaluateAcceptsAPasswordMeetingEveryPart()
    {
        var vStatus = PasswordRule.Evaluate("Tflens2026!");

        Assert.True(vStatus.HasLength);
        Assert.True(vStatus.HasCapital);
        Assert.True(vStatus.HasNumber);
        Assert.True(vStatus.HasSpecial);
        Assert.True(vStatus.MeetsRule);
    }

    /// <summary>
    /// When a password has no special character, then that one part is reported unmet and the whole
    /// rule is not met — matching the mockup's "rule-special" row.
    /// </summary>
    [Fact]
    public void EvaluateCatchesAMissingSpecialCharacter()
    {
        var vStatus = PasswordRule.Evaluate("Tflens2026");

        Assert.True(vStatus.HasLength);
        Assert.True(vStatus.HasCapital);
        Assert.True(vStatus.HasNumber);
        Assert.False(vStatus.HasSpecial);
        Assert.False(vStatus.MeetsRule);
    }

    /// <summary>
    /// When a password is short, has no capital and no digit, then only the special-character part
    /// can be met, and the rule overall is not met.
    /// </summary>
    [Fact]
    public void EvaluateCatchesEveryOtherMissingPart()
    {
        var vStatus = PasswordRule.Evaluate("weak!");

        Assert.False(vStatus.HasLength);
        Assert.False(vStatus.HasCapital);
        Assert.False(vStatus.HasNumber);
        Assert.True(vStatus.HasSpecial);
        Assert.False(vStatus.MeetsRule);
    }

    /// <summary>
    /// When no password was typed at all, then the rule treats it as empty rather than throwing.
    /// </summary>
    [Fact]
    public void EvaluateTreatsNullAsEmpty()
    {
        var vStatus = PasswordRule.Evaluate(null);

        Assert.False(vStatus.MeetsRule);
    }
}
