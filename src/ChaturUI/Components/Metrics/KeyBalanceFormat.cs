using System.Globalization;
using Chatur.Core.Actions;

namespace ChaturUI.Components.Metrics;

/// <summary>The words and amounts of <c>keys-panel</c> (mockups/measurements.html; REQ-UI-129, REQ-FN-326).</summary>
public static class KeyBalanceFormat
{
    /// <summary>What an amount with no basis reads in the Left column.</summary>
    public const string NotKnown = "not known";

    /// <summary>The "How it is known" pill's words.</summary>
    /// <param name="aKind">How what is left is known.</param>
    public static string Source(KeyBalanceKind aKind) => aKind switch
    {
        KeyBalanceKind.AskedOfProvider => "asked of the provider",
        KeyBalanceKind.OwnCount => "Chatur's own count",
        _ => NotKnown,
    };

    /// <summary>The small line under the provider's name: its connector and how it signs in, e.g. "Anthropic · key".</summary>
    /// <param name="aLine">The line.</param>
    public static string Kind(KeyBalanceLine aLine)
    {
        var vConnector = aLine.Connector switch
        {
            "OpenAICompatible" => "OpenAI-compatible",
            "ChatGptSubscription" => "ChatGPT",
            _ => aLine.Connector,
        };
        var vSignIn = aLine.SignInMethod switch
        {
            "Key" => "key",
            "Subscription" => "subscription",
            "None" => "local",
            _ => aLine.SignInMethod.ToLowerInvariant(),
        };
        return $"{vConnector} · {vSignIn}";
    }

    /// <summary>An amount in its currency ("$12.40", "8.21 CNY"), or "—" when there is none.</summary>
    /// <param name="aAmount">The amount.</param>
    /// <param name="aCurrency">Its currency code.</param>
    public static string Money(decimal? aAmount, string aCurrency)
    {
        if (aAmount is not { } vAmount)
        {
            return "—";
        }

        var vText = Math.Abs(vAmount).ToString("N2", CultureInfo.InvariantCulture);
        var vSign = vAmount < 0 ? "-" : string.Empty;
        return string.Equals(aCurrency, "USD", StringComparison.OrdinalIgnoreCase) ? $"{vSign}${vText}" : $"{vSign}{vText} {aCurrency}";
    }

    /// <summary>What is left, or "not known".</summary>
    /// <param name="aLine">The line.</param>
    public static string Left(KeyBalanceLine aLine) => aLine.Left is null ? NotKnown : Money(aLine.Left, aLine.Currency);
}
