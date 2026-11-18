using System.Globalization;
using System.Text.Json;

namespace Chatur.Core.Estimates;

/// <summary>
/// Reads JSONL records whose values may be native JSON (TechieFlow's records, the estimates stream) or strings
/// (Chatur's own all-string streams). A line that is not a JSON object is skipped, as every reader of the
/// streams does.
/// </summary>
internal static class EstimateJson
{
    /// <summary>The JSON objects among the lines.</summary>
    /// <param name="aLines">The file's lines.</param>
    public static IEnumerable<JsonElement> Records(IEnumerable<string> aLines)
    {
        foreach (var vLine in aLines)
        {
            if (string.IsNullOrWhiteSpace(vLine))
            {
                continue;
            }

            JsonElement vRoot;
            try
            {
                using var vDoc = JsonDocument.Parse(vLine);
                vRoot = vDoc.RootElement.Clone();
            }
            catch (JsonException)
            {
                continue;
            }

            if (vRoot.ValueKind == JsonValueKind.Object)
            {
                yield return vRoot;
            }
        }
    }

    /// <summary>A field as text: a string as it is, a number as written, anything else empty.</summary>
    public static string Text(JsonElement aRecord, string aName) =>
        aRecord.TryGetProperty(aName, out var vValue)
            ? vValue.ValueKind switch
            {
                JsonValueKind.String => vValue.GetString() ?? string.Empty,
                JsonValueKind.Number => vValue.GetRawText(),
                _ => string.Empty
            }
            : string.Empty;

    /// <summary>A field as a number, from a JSON number or a numeric string; <see langword="null"/> otherwise (an empty string is "not measured").</summary>
    public static double? Number(JsonElement aRecord, string aName)
    {
        if (!aRecord.TryGetProperty(aName, out var vValue))
        {
            return null;
        }

        return vValue.ValueKind switch
        {
            JsonValueKind.Number => vValue.GetDouble(),
            JsonValueKind.String when double.TryParse(vValue.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var vParsed) => vParsed,
            _ => null
        };
    }

    /// <summary>A field that is JSON <c>true</c> or the string <c>"true"</c>.</summary>
    public static bool Flag(JsonElement aRecord, string aName) =>
        aRecord.TryGetProperty(aName, out var vValue)
        && (vValue.ValueKind == JsonValueKind.True
            || (vValue.ValueKind == JsonValueKind.String && string.Equals(vValue.GetString(), "true", StringComparison.OrdinalIgnoreCase)));

    /// <summary>A list field: a JSON array of strings, or a caller-joined string (<c>"REQ-UI-004,REQ-FN-011"</c>).</summary>
    public static List<string> List(JsonElement aRecord, string aName)
    {
        if (!aRecord.TryGetProperty(aName, out var vValue))
        {
            return [];
        }

        return vValue.ValueKind switch
        {
            JsonValueKind.Array => vValue.EnumerateArray()
                .Where(aItem => aItem.ValueKind == JsonValueKind.String)
                .Select(aItem => aItem.GetString() ?? string.Empty)
                .Where(aItem => aItem.Length > 0)
                .ToList(),
            JsonValueKind.String => (vValue.GetString() ?? string.Empty)
                .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            _ => []
        };
    }

    /// <summary>An ISO-8601 time as UTC, or <see langword="null"/>.</summary>
    public static DateTime? Time(string aText) =>
        aText.Length > 0 && DateTime.TryParse(aText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var vParsed)
            ? vParsed
            : null;

    /// <summary>
    /// A run's wall seconds: the timestamps win over a stored <c>duration_s</c>, and an impossible span is no
    /// duration (the framework's <c>_dur</c>).
    /// </summary>
    public static double? Duration(JsonElement aRecord)
    {
        var vStarted = Time(Text(aRecord, "started"));
        var vEnded = Time(Text(aRecord, "ended"));
        if (vStarted is { } vFrom && vEnded is { } vTo)
        {
            var vSpan = Math.Floor((vTo - vFrom).TotalSeconds);
            return vSpan > 0 ? vSpan : null;
        }

        return Number(aRecord, "duration_s") is { } vStored && vStored > 0 ? vStored : null;
    }

    /// <summary>
    /// The run's output tokens when they were measured: a number in <c>tokens_out</c> and a <c>tokens_scope</c>
    /// that is not <c>none</c>. Chatur's own records carry no <c>tokens_scope</c> and write an empty
    /// <c>tokens_out</c> when nothing was measured.
    /// </summary>
    public static double? MeasuredTokensOut(JsonElement aRecord) =>
        Text(aRecord, "tokens_scope") == "none" ? null : Number(aRecord, "tokens_out");

    /// <summary>How many rows a run touched: <c>reqs_count</c> when it is a whole number, else the length of the list.</summary>
    public static int RowCount(JsonElement aRecord, List<string> aReqs) =>
        Number(aRecord, "reqs_count") is { } vCount && vCount == Math.Floor(vCount) ? (int)vCount : aReqs.Count;
}
