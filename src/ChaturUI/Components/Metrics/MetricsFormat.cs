using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using ChaturUI.Components.Estimates;

namespace ChaturUI.Components.Metrics;

/// <summary>
/// How the Measurements screens write a figure (REQ-UI-117). Only wording and layout of numbers the actions already
/// computed: a figure no record measured reads "not measured", a cost no run carried reads "no price" (BRD-261).
/// </summary>
public static class MetricsFormat
{
    /// <summary>What a figure nobody measured reads (TfLens's word).</summary>
    public const string NotMeasured = "not measured";

    /// <summary>What a cost no run carried reads.</summary>
    public const string NoPrice = "no price";

    /// <summary>A count with thousands separators, or "not measured".</summary>
    /// <param name="aValue">The figure.</param>
    public static string Count(long? aValue) => aValue is { } vValue ? vValue.ToString("N0", CultureInfo.InvariantCulture) : NotMeasured;

    /// <summary>A sum, or "not measured" when no record gave it.</summary>
    /// <param name="aSum">The sum.</param>
    public static string Sum(MeasuredSum aSum) => Count(aSum.Value);

    /// <summary>The wall clock, or "not measured".</summary>
    /// <param name="aSeconds">Seconds.</param>
    public static string Wall(long? aSeconds) => aSeconds is { } vSeconds ? EstimateLines.Duration(vSeconds) : NotMeasured;

    /// <summary>The wall clock of a sum, or "not measured".</summary>
    /// <param name="aSum">The sum, in seconds.</param>
    public static string Wall(MeasuredSum aSum) => Wall(aSum.Value);

    /// <summary>US dollars as recorded, or "no price".</summary>
    /// <param name="aUsd">The cost.</param>
    public static string Usd(decimal? aUsd) => aUsd is { } vUsd
        ? "$" + vUsd.ToString(vUsd != 0 && Math.Abs(vUsd) < 0.01m ? "N4" : "N2", CultureInfo.InvariantCulture)
        : NoPrice;

    /// <summary>A cost, or "no price".</summary>
    /// <param name="aCost">The cost.</param>
    public static string Usd(MeasuredCost aCost) => Usd(aCost.Usd);

    /// <summary>"3 runs no price" beside a partial sum; none when every run carried a price or none did (the figure itself then says it).</summary>
    /// <param name="aCost">The cost.</param>
    public static string? CostNote(MeasuredCost aCost) => aCost.Usd is not null && aCost.NoPrice > 0
        ? $"{aCost.NoPrice} run{(aCost.NoPrice == 1 ? string.Empty : "s")} no price"
        : null;

    /// <summary>"2 not measured" beside a sum some records could not give; none otherwise.</summary>
    /// <param name="aSum">The sum.</param>
    public static string? SumNote(MeasuredSum aSum) => aSum.Value is not null && aSum.NotMeasured > 0 ? $"{aSum.NotMeasured} not measured" : null;

    /// <summary>When a record was made, in this machine's time.</summary>
    /// <param name="aUtc">The time.</param>
    public static string When(DateTime? aUtc) => aUtc is { } vUtc
        ? vUtc.ToLocalTime().ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture)
        : NotMeasured;

    /// <summary>The plain gloss for a raw <c>found_by</c> value (UI design, Measurements), or none for a value it does not know.</summary>
    /// <param name="aFoundBy">The raw value.</param>
    public static string? FoundByGloss(string? aFoundBy) => aFoundBy switch
    {
        "gate" => "a check",
        "self-smoke" => "the builder's own check",
        "agent-review" => "a reviewing agent",
        "owner" => "you",
        "production" => "in use after release",
        "library-feedback" => "a library's team",
        _ => null,
    };

    /// <summary>The first-pass rate as a whole percent, or <see langword="null"/> when no row was graded.</summary>
    /// <param name="aGraded">Rows graded.</param>
    /// <param name="aFirstPass">Rows that passed the first time.</param>
    public static string? FirstPassRate(int aGraded, int aFirstPass) => aGraded == 0
        ? null
        : Math.Round(100.0 * aFirstPass / aGraded, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + "%";

    /// <summary>A key for a <c>data-testid</c>.</summary>
    /// <param name="aText">Any text.</param>
    public static string Slug(string aText) => Regex.Replace(aText.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
}
