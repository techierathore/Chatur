using System.Globalization;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Agent.Commands;

namespace Chatur.Core.Measurements;

/// <summary>What <see cref="MetricsCalculator.Compute"/> works out from the text of the three streams (REQ-UI-116 to REQ-UI-118, REQ-UI-120, REQ-NFR-014).</summary>
/// <param name="Totals">Every run in the period, added up.</param>
/// <param name="ByModel">Split by model.</param>
/// <param name="ByAgent">Split by the agent owning the command.</param>
/// <param name="ByCommand">Split by command.</param>
/// <param name="Misses">Misses and first-pass figures.</param>
/// <param name="Runs">Every run in the period, newest first.</param>
/// <param name="UnreadableLines">Lines that are not a JSON object, across the three texts.</param>
public sealed record MetricsComputation(
    RunTotals Totals,
    IReadOnlyList<RunSplitLine> ByModel,
    IReadOnlyList<RunSplitLine> ByAgent,
    IReadOnlyList<RunSplitLine> ByCommand,
    MissFigures Misses,
    IReadOnlyList<RunLine> Runs,
    int UnreadableLines);

/// <summary>
/// Adds up Chatur's own measurement files by TfLens's rules, so the same files give the same totals. Pure: it takes the text
/// of the files and a clock and a time zone, touches no file and no network, so a test can feed it text (REQ-NFR-014).
/// Reads both shapes: TechieFlow's (JSON numbers, <c>ts</c>) and Chatur's own writer's (every value a string, <c>at</c>).
/// </summary>
public static class MetricsCalculator
{
    /// <summary>What a split line, a run or a miss shows when the record does not say.</summary>
    public const string NotMeasuredWord = "not measured";

    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    /// <summary>Adds up the three streams for a period.</summary>
    /// <param name="aRunsText">The text of runs.jsonl, or <see langword="null"/> when there is none.</param>
    /// <param name="aGatesText">The text of gates.jsonl, or <see langword="null"/>.</param>
    /// <param name="aMissesText">The text of misses.jsonl, or <see langword="null"/>.</param>
    /// <param name="aPeriod">The period.</param>
    /// <param name="aNowUtc">The current time, in UTC.</param>
    /// <param name="aZone">The time zone day boundaries are taken in.</param>
    public static MetricsComputation Compute(string? aRunsText, string? aGatesText, string? aMissesText, MetricsPeriod aPeriod, DateTime aNowUtc, TimeZoneInfo aZone)
    {
        var vWindow = WindowOf(aPeriod, aNowUtc, aZone);
        var vUnreadable = 0;
        var vRuns = ReadRuns(aRunsText, vWindow, ref vUnreadable);
        var vGates = ReadObjects(aGatesText, ref vUnreadable);
        var vMisses = ReadObjects(aMissesText, ref vUnreadable);

        var vTotals = TotalsOf(vRuns);
        var vLines = vRuns.OrderByDescending(aRun => aRun.AtUtc.HasValue).ThenByDescending(aRun => aRun.AtUtc).ThenByDescending(aRun => aRun.Line)
            .Select(aRun => aRun.ToLine()).ToList();

        return new MetricsComputation(
            vTotals,
            SplitBy(vRuns, aRun => aRun.Model),
            SplitBy(vRuns, aRun => aRun.Agent),
            SplitBy(vRuns, aRun => aRun.Command),
            MissesOf(vMisses, vGates, vWindow),
            vLines,
            vUnreadable);
    }

    /// <summary>The agent a command belongs to, by Chatur's own command list; <c>"not measured"</c> when it is in none.</summary>
    /// <param name="aCommand">The run's <c>cmd</c>.</param>
    public static string AgentOf(string? aCommand)
    {
        var vCmd = (aCommand ?? string.Empty).Trim().TrimStart('*');
        if (vCmd.Length == 0)
        {
            return NotMeasuredWord;
        }

        var vOwner = vCmd.ToLowerInvariant() switch
        {
            "amend-docs" => "analyst",
            "verify-phase" or "verify" => "verifier",
            "triage-issues" or "log-miss" or "metrics" or "metrics-report" or "triage-and-fix" => "flow-master",
            _ => null
        };

        if (vOwner is null)
        {
            foreach (var vRole in AgentCommandCatalogue.RoleCodes)
            {
                if (AgentCommandCatalogue.ForRole(vRole).Any(aCommandRow =>
                        string.Equals(aCommandRow.Code, vCmd, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(aCommandRow.Target, vCmd, StringComparison.OrdinalIgnoreCase)))
                {
                    vOwner = vRole;
                    break;
                }
            }
        }

        return vOwner is null ? NotMeasuredWord : AgentCommandCatalogue.DisplayNameOf(vOwner);
    }

    /// <summary>The first and last instant (UTC) a period counts; both <see langword="null"/> for all history. The end is exclusive.</summary>
    /// <param name="aPeriod">The period.</param>
    /// <param name="aNowUtc">The current time, in UTC.</param>
    /// <param name="aZone">The time zone day boundaries are taken in.</param>
    public static (bool Bounded, DateTime? StartUtc, DateTime? EndUtc) WindowOf(MetricsPeriod aPeriod, DateTime aNowUtc, TimeZoneInfo aZone)
    {
        var vToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(aNowUtc, DateTimeKind.Utc), aZone));
        switch (aPeriod.Kind)
        {
            case MetricsPeriodKind.Today:
                return (true, StartOfDay(vToday, aZone), StartOfDay(vToday.AddDays(1), aZone));
            case MetricsPeriodKind.Last7Days:
                return (true, StartOfDay(vToday.AddDays(-6), aZone), StartOfDay(vToday.AddDays(1), aZone));
            case MetricsPeriodKind.Last30Days:
                return (true, StartOfDay(vToday.AddDays(-29), aZone), StartOfDay(vToday.AddDays(1), aZone));
            case MetricsPeriodKind.Between:
                var vFrom = aPeriod.From;
                var vTo = aPeriod.To;
                if (vFrom is { } vF && vTo is { } vT && vF > vT)
                {
                    (vFrom, vTo) = (vT, vF);
                }

                return (true, vFrom is { } vStart ? StartOfDay(vStart, aZone) : null, vTo is { } vEnd ? StartOfDay(vEnd.AddDays(1), aZone) : null);
            default:
                return (false, null, null);
        }
    }

    private static DateTime StartOfDay(DateOnly aDay, TimeZoneInfo aZone)
    {
        var vLocal = aDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        // A midnight skipped by a clock change takes the offset just after it.
        var vOffset = aZone.IsInvalidTime(vLocal) ? aZone.GetUtcOffset(vLocal.AddHours(1)) : aZone.GetUtcOffset(vLocal);
        return DateTime.SpecifyKind(vLocal - vOffset, DateTimeKind.Utc);
    }

    private static bool InWindow((bool Bounded, DateTime? StartUtc, DateTime? EndUtc) aWindow, DateTime? aAtUtc)
    {
        if (!aWindow.Bounded)
        {
            return true;
        }

        if (aAtUtc is not { } vAt)
        {
            return false;
        }

        return (aWindow.StartUtc is not { } vStart || vAt >= vStart) && (aWindow.EndUtc is not { } vEnd || vAt < vEnd);
    }

    // ---- reading -------------------------------------------------------------------------------------------------------

    private sealed record Rec(int Line, string Text, JsonElement Json);

    private static List<Rec> ReadObjects(string? aText, ref int aUnreadable)
    {
        var vResult = new List<Rec>();
        if (aText is null)
        {
            return vResult;
        }

        var vLines = aText.Split('\n');
        for (var vIndex = 0; vIndex < vLines.Length; vIndex++)
        {
            var vText = vLines[vIndex].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(vText))
            {
                continue;
            }

            try
            {
                using var vDoc = JsonDocument.Parse(vText);
                if (vDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    vResult.Add(new Rec(vIndex + 1, vText, vDoc.RootElement.Clone()));
                    continue;
                }
            }
            catch (JsonException)
            {
                // Counted below; never guessed at.
            }

            aUnreadable++;
        }

        return vResult;
    }

    private static string? Str(JsonElement aObj, string aName)
    {
        if (!aObj.TryGetProperty(aName, out var vValue))
        {
            return null;
        }

        var vText = vValue.ValueKind switch
        {
            JsonValueKind.String => vValue.GetString(),
            JsonValueKind.Number => vValue.GetRawText(),
            _ => null
        };
        return string.IsNullOrWhiteSpace(vText) ? null : vText.Trim();
    }

    private static decimal? Num(JsonElement aObj, string aName)
    {
        if (!aObj.TryGetProperty(aName, out var vValue))
        {
            return null;
        }

        if (vValue.ValueKind == JsonValueKind.Number)
        {
            return vValue.TryGetDecimal(out var vDecimal) ? vDecimal : null;
        }

        if (vValue.ValueKind == JsonValueKind.String
            && decimal.TryParse(vValue.GetString()?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var vParsed))
        {
            return vParsed;
        }

        return null;
    }

    private static long? Whole(JsonElement aObj, string aName) => Num(aObj, aName) is { } vNumber ? (long)Math.Round(vNumber, MidpointRounding.AwayFromZero) : null;

    private static DateTime? ParseDate(string? aText) =>
        aText is not null && DateTimeOffset.TryParse(aText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var vParsed) ? vParsed.UtcDateTime : null;

    private static string? DateText(JsonElement aObj) => Str(aObj, "ts") ?? Str(aObj, "at");

    private static DateTime? DateOf(JsonElement aObj) => ParseDate(Str(aObj, "ts")) ?? ParseDate(Str(aObj, "at"));

    private static bool IsTrue(JsonElement aObj, string aName) =>
        aObj.TryGetProperty(aName, out var vValue)
        && (vValue.ValueKind == JsonValueKind.True
            || (vValue.ValueKind == JsonValueKind.String && string.Equals(vValue.GetString(), "true", StringComparison.OrdinalIgnoreCase)));

    // ---- runs ----------------------------------------------------------------------------------------------------------

    private sealed class RunFacts
    {
        public int Line { get; init; }

        public DateTime? AtUtc { get; init; }

        public string Command { get; init; } = NotMeasuredWord;

        public string? Mode { get; init; }

        public string Agent { get; init; } = NotMeasuredWord;

        public string Model { get; init; } = NotMeasuredWord;

        public bool TokensMeasured { get; init; }

        public long? TokensIn { get; init; }

        public long? TokensOut { get; init; }

        public long? CacheRead { get; init; }

        public long? CacheWrite { get; init; }

        public decimal? CostUsd { get; init; }

        public long? WallClockS { get; init; }

        public string? Result { get; init; }

        public IReadOnlyList<string> RowsTouched { get; init; } = [];

        public string Raw { get; init; } = string.Empty;

        public RunLine ToLine() => new(Line, AtUtc, Command, Mode, Agent, Model, TokensIn, TokensOut, CacheRead, CacheWrite, CostUsd, WallClockS, Result, RowsTouched, Raw);
    }

    private static List<RunFacts> ReadRuns(string? aText, (bool Bounded, DateTime? StartUtc, DateTime? EndUtc) aWindow, ref int aUnreadable)
    {
        var vRecords = ReadObjects(aText, ref aUnreadable);

        var vVoided = new HashSet<(string, string)>();
        foreach (var vRecord in vRecords.Where(aRec => Str(aRec.Json, "kind") == "run-void"))
        {
            if (Str(vRecord.Json, "started") is { } vStarted)
            {
                vVoided.Add((Str(vRecord.Json, "cmd") ?? string.Empty, vStarted));
            }
        }

        var vResult = new List<RunFacts>();
        foreach (var vRecord in vRecords.Where(aRec => Str(aRec.Json, "kind") == "run"))
        {
            var vJson = vRecord.Json;
            var vCmd = Str(vJson, "cmd");
            var vStartedText = Str(vJson, "started");
            if (vStartedText is not null && vVoided.Contains((vCmd ?? string.Empty, vStartedText)))
            {
                continue;
            }

            var vAt = DateOf(vJson);
            if (!InWindow(aWindow, vAt))
            {
                continue;
            }

            var vScope = Str(vJson, "tokens_scope");
            var vOut = Whole(vJson, "tokens_out");
            var vMeasured = !string.Equals(vScope, "none", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(vScope, "absent", StringComparison.OrdinalIgnoreCase)
                && vOut is not null;

            vResult.Add(new RunFacts
            {
                Line = vRecord.Line,
                AtUtc = vAt,
                Command = vCmd ?? NotMeasuredWord,
                Mode = Str(vJson, "mode"),
                Agent = AgentOf(vCmd),
                Model = Str(vJson, "model") ?? NotMeasuredWord,
                TokensMeasured = vMeasured,
                TokensIn = vMeasured ? Whole(vJson, "tokens_in") : null,
                TokensOut = vMeasured ? vOut : null,
                CacheRead = vMeasured ? Whole(vJson, "tokens_cache_read") : null,
                CacheWrite = vMeasured ? Whole(vJson, "tokens_cache_write") : null,
                CostUsd = Num(vJson, "cost_usd"),
                WallClockS = WallClockOf(vJson, vAt),
                Result = Str(vJson, "build_result"),
                RowsTouched = RowsOf(vJson),
                Raw = Indent(vRecord.Text)
            });
        }

        return vResult;
    }

    private static long? WallClockOf(JsonElement aJson, DateTime? aAtUtc)
    {
        var vStarted = ParseDate(Str(aJson, "started"));
        var vEnded = ParseDate(Str(aJson, "ended"));
        // TfLens's RunDuration: the span between started and ended when both are there and positive, else the stored duration_s, else started to ts.
        if (vStarted is { } vFrom && vEnded is { } vTo && (vTo - vFrom).TotalSeconds > 0)
        {
            return (long)Math.Round((vTo - vFrom).TotalSeconds, MidpointRounding.AwayFromZero);
        }

        if (Num(aJson, "duration_s") is > 0 and { } vStored)
        {
            return (long)Math.Round(vStored, MidpointRounding.AwayFromZero);
        }

        if (vStarted is { } vBegin && ParseDate(Str(aJson, "ts")) is { } vTs && (vTs - vBegin).TotalSeconds > 0)
        {
            return (long)Math.Round((vTs - vBegin).TotalSeconds, MidpointRounding.AwayFromZero);
        }

        return null;
    }

    private static IReadOnlyList<string> RowsOf(JsonElement aJson)
    {
        if (!aJson.TryGetProperty("reqs_touched", out var vValue))
        {
            return [];
        }

        if (vValue.ValueKind == JsonValueKind.Array)
        {
            return vValue.EnumerateArray().Where(aItem => aItem.ValueKind == JsonValueKind.String)
                .Select(aItem => aItem.GetString()!.Trim()).Where(aItem => aItem.Length > 0).ToList();
        }

        if (vValue.ValueKind == JsonValueKind.String)
        {
            return vValue.GetString()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [];
    }

    private static string Indent(string aJsonLine)
    {
        try
        {
            using var vDoc = JsonDocument.Parse(aJsonLine);
            return JsonSerializer.Serialize(vDoc.RootElement, IndentedOptions);
        }
        catch (JsonException)
        {
            return aJsonLine;
        }
    }

    // ---- totals and splits ---------------------------------------------------------------------------------------------

    private static RunTotals TotalsOf(IReadOnlyCollection<RunFacts> aRuns)
    {
        var vPriced = aRuns.Where(aRun => aRun.CostUsd is not null).ToList();
        return new RunTotals(
            aRuns.Count,
            SumOf(aRuns, aRun => aRun.TokensIn),
            SumOf(aRuns, aRun => aRun.TokensOut),
            SumOf(aRuns, aRun => aRun.CacheRead),
            SumOf(aRuns, aRun => aRun.CacheWrite),
            new MeasuredCost(vPriced.Count == 0 ? null : vPriced.Sum(aRun => aRun.CostUsd!.Value), vPriced.Count, aRuns.Count - vPriced.Count),
            SumOf(aRuns, aRun => aRun.WallClockS));
    }

    private static MeasuredSum SumOf(IReadOnlyCollection<RunFacts> aRuns, Func<RunFacts, long?> aPick)
    {
        var vValues = aRuns.Select(aPick).Where(aValue => aValue is not null).Select(aValue => aValue!.Value).ToList();
        return new MeasuredSum(vValues.Count == 0 ? null : vValues.Sum(), vValues.Count, aRuns.Count - vValues.Count);
    }

    private static IReadOnlyList<RunSplitLine> SplitBy(IEnumerable<RunFacts> aRuns, Func<RunFacts, string> aKey) =>
        aRuns.GroupBy(aKey, StringComparer.Ordinal)
            .Select(aGroup => new RunSplitLine(aGroup.Key, TotalsOf(aGroup.ToList())))
            .OrderByDescending(aLine => aLine.Totals.TokensOut.Value ?? 0)
            .ThenByDescending(aLine => aLine.Totals.Runs)
            .ThenBy(aLine => aLine.Key, StringComparer.Ordinal)
            .ToList();

    // ---- misses and first pass -----------------------------------------------------------------------------------------

    private static MissFigures MissesOf(List<Rec> aMisses, List<Rec> aGates, (bool Bounded, DateTime? StartUtc, DateTime? EndUtc) aWindow)
    {
        var vVoided = aMisses.Where(aRec => Str(aRec.Json, "kind") == "miss-void")
            .Select(aRec => Str(aRec.Json, "miss_id")).Where(aId => aId is not null).Select(aId => aId!).ToHashSet(StringComparer.Ordinal);

        // One miss per miss_id: the earliest by ts, as TfLens dedupes.
        var vFirst = new Dictionary<string, Rec>(StringComparer.Ordinal);
        foreach (var vRecord in aMisses.Where(aRec => Str(aRec.Json, "kind") == "miss"))
        {
            var vId = Str(vRecord.Json, "miss_id");
            if (vId is null || vVoided.Contains(vId))
            {
                continue;
            }

            if (!vFirst.TryGetValue(vId, out var vKept) || string.CompareOrdinal(DateText(vRecord.Json), DateText(vKept.Json)) < 0)
            {
                vFirst[vId] = vRecord;
            }
        }

        var vLines = new List<MissLine>();
        foreach (var (vId, vRecord) in vFirst)
        {
            var vAt = DateOf(vRecord.Json);
            if (!InWindow(aWindow, vAt))
            {
                continue;
            }

            var vWhat = Str(vRecord.Json, "what");
            if (vWhat is null)
            {
                // An amend completes a record, never alters a fact: it fills "what" only when the miss left it empty.
                vWhat = aMisses.Where(aRec => Str(aRec.Json, "kind") == "miss-amend" && Str(aRec.Json, "miss_id") == vId && Str(aRec.Json, "field") == "what")
                    .OrderBy(aRec => DateText(aRec.Json), StringComparer.Ordinal)
                    .Select(aRec => Str(aRec.Json, "value")).FirstOrDefault(aValue => aValue is not null);
            }

            vLines.Add(new MissLine(
                vId,
                Str(vRecord.Json, "req_id"),
                Str(vRecord.Json, "found_by"),
                Str(vRecord.Json, "found_gate"),
                Str(vRecord.Json, "miss_class"),
                Str(vRecord.Json, "severity"),
                vWhat,
                StatusOf(aMisses, vId),
                vAt));
        }

        vLines = vLines.OrderByDescending(aLine => aLine.AtUtc.HasValue).ThenByDescending(aLine => aLine.AtUtc).ThenBy(aLine => aLine.MissId, StringComparer.Ordinal).ToList();

        var vByFoundBy = vLines.GroupBy(aLine => aLine.FoundBy ?? NotMeasuredWord, StringComparer.Ordinal)
            .Select(aGroup => new KeyValuePair<string, int>(aGroup.Key, aGroup.Count()))
            .OrderByDescending(aPair => aPair.Value).ThenBy(aPair => aPair.Key, StringComparer.Ordinal).ToList();
        var vByRow = vLines.Where(aLine => aLine.ReqId is not null).GroupBy(aLine => aLine.ReqId!, StringComparer.Ordinal)
            .Select(aGroup => new KeyValuePair<string, int>(aGroup.Key, aGroup.Count()))
            .OrderByDescending(aPair => aPair.Value).ThenBy(aPair => aPair.Key, StringComparer.Ordinal).ToList();

        var (vGraded, vFirstPass) = FirstPassOf(aGates, aWindow);
        return new MissFigures(vLines, vByFoundBy, vByRow, vGraded, vFirstPass);
    }

    private static string StatusOf(List<Rec> aMisses, string aMissId)
    {
        var vLatest = aMisses.Where(aRec => Str(aRec.Json, "kind") == "miss-fix" && Str(aRec.Json, "miss_id") == aMissId)
            .OrderBy(aRec => DateText(aRec.Json), StringComparer.Ordinal)
            .ThenBy(aRec => Num(aRec.Json, "fix_attempt") ?? 0)
            .LastOrDefault();
        var vVerdict = vLatest is null ? null : Str(vLatest.Json, "verdict_after");
        if (string.Equals(vVerdict, "Verified", StringComparison.OrdinalIgnoreCase))
        {
            return "Verified";
        }

        return string.Equals(vVerdict, "wont-fix", StringComparison.OrdinalIgnoreCase) ? "wont-fix" : "open";
    }

    private static (int Graded, int FirstPass) FirstPassOf(List<Rec> aGates, (bool Bounded, DateTime? StartUtc, DateTime? EndUtc) aWindow)
    {
        var vSeen = new Dictionary<(string, string), int>();
        var vGraded = new HashSet<(string, string)>();
        var vFirstPass = new HashSet<(string, string)>();

        // The attempt a record does not carry is its order within its row (TfLens GateAttempt): by ts, then run_id.
        foreach (var vRecord in aGates.Where(aRec => Str(aRec.Json, "kind") == "gate" && !IsTrue(aRec.Json, "backfilled"))
                     .OrderBy(aRec => DateText(aRec.Json) ?? string.Empty, StringComparer.Ordinal)
                     .ThenBy(aRec => Str(aRec.Json, "run_id") ?? string.Empty, StringComparer.Ordinal))
        {
            if (Str(vRecord.Json, "req_id") is not { } vReq)
            {
                continue;
            }

            var vKey = (Str(vRecord.Json, "app") ?? string.Empty, vReq);
            var vOrdinal = vSeen.GetValueOrDefault(vKey) + 1;
            vSeen[vKey] = vOrdinal;

            if (!InWindow(aWindow, DateOf(vRecord.Json)))
            {
                continue;
            }

            vGraded.Add(vKey);
            var vAttempt = Num(vRecord.Json, "attempt") ?? vOrdinal;
            if (vAttempt == 1 && Str(vRecord.Json, "verdict") == "Verified")
            {
                vFirstPass.Add(vKey);
            }
        }

        return (vGraded.Count, vFirstPass.Count);
    }
}
