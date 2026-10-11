using System.Globalization;
using System.Text.Json;
using Chatur.Core.Actions;

namespace Chatur.Core.RunReports;

/// <summary>
/// Builds run reports from a project's <c>docs/metrics</c> streams: <c>runs.jsonl</c> says what ran,
/// <c>gates.jsonl</c> the verdict each requirement was left at, <c>misses.jsonl</c> the misses, all tied to a
/// run by the time it started (BRD-131 to BRD-134). A figure a record does not carry stays empty.
/// </summary>
public static class RunReportReader
{
    private static readonly HashSet<string> OwnerArtifacts =
        new(StringComparer.OrdinalIgnoreCase) { "brd", "uidesign", "architecture", "checklist", "devguide" };

    private static readonly HashSet<string> OwnerMissClasses =
        new(StringComparer.OrdinalIgnoreCase) { "unspecified-gap", "spec-contradiction" };

    /// <summary>Every run in <c>runs.jsonl</c>, newest first.</summary>
    /// <param name="aProjectName">The project's name.</param>
    /// <param name="aRunsLines">The lines of <c>runs.jsonl</c>.</param>
    public static IReadOnlyList<RunReportEntry> Runs(string aProjectName, IEnumerable<string> aRunsLines) =>
        Records(aRunsLines)
            .Select(aRecord => ToEntry(aProjectName, aRecord))
            .OfType<RunReportEntry>()
            .GroupBy(aEntry => aEntry.Key)
            .Select(aGroup => aGroup.Last())
            .OrderByDescending(aEntry => aEntry.StartedUtc)
            .ToList();

    /// <summary>The report of one run, or <see langword="null"/> when <c>runs.jsonl</c> does not hold it.</summary>
    /// <param name="aProjectName">The project's name.</param>
    /// <param name="aRunKey">The run's key (its start time as written).</param>
    /// <param name="aRunsLines">The lines of <c>runs.jsonl</c>.</param>
    /// <param name="aGatesLines">The lines of <c>gates.jsonl</c>.</param>
    /// <param name="aMissesLines">The lines of <c>misses.jsonl</c>.</param>
    /// <param name="aItems">The project's checklist rows, for titles and owners.</param>
    public static RunReportView? Report(
        string aProjectName,
        string aRunKey,
        IEnumerable<string> aRunsLines,
        IEnumerable<string> aGatesLines,
        IEnumerable<string> aMissesLines,
        IReadOnlyList<RequirementItem> aItems)
    {
        var vRecord = Records(aRunsLines)
            .Where(aRecord => ToEntry(aProjectName, aRecord)?.Key == aRunKey)
            .Select(aRecord => (JsonElement?)aRecord)
            .LastOrDefault();
        if (vRecord is not { } vRun || ToEntry(aProjectName, vRun) is not { } vEntry)
        {
            return null;
        }

        var vGates = Records(aGatesLines).Where(aRecord => Text(aRecord, "run_id") == aRunKey).ToList();
        var vTouched = StringList(vRun, "reqs_touched");
        var vIds = vTouched.Concat(vGates.Select(aGate => Text(aGate, "req_id"))).Where(aId => aId.Length > 0).Distinct().ToList();
        var vRequirements = vIds.Select(aId => Requirement(aId, vGates, aItems)).ToList();

        var vMissRecords = Records(aMissesLines).ToList();
        var vMisses = vMissRecords
            .Where(aRecord => Text(aRecord, "kind") is "miss" or ""
                && (Text(aRecord, "found_run_id") == aRunKey || Text(aRecord, "origin_run_id") == aRunKey))
            .GroupBy(aRecord => Text(aRecord, "miss_id"))
            .Select(aGroup => Miss(aGroup.Last(), vMissRecords))
            .ToList();

        var vTokensIn = Number(vRun, "tokens_in");
        var vTokensOut = Number(vRun, "tokens_out");
        long? vTotal = vTokensIn is null && vTokensOut is null ? null : (vTokensIn ?? 0) + (vTokensOut ?? 0);
        var vDuration = Number(vRun, "duration_s")
            ?? (vEntry.EndedUtc is { } vEnded ? (long)Math.Max(0, (vEnded - vEntry.StartedUtc).TotalSeconds) : null);

        return new RunReportView(vEntry, vRequirements, vMisses, Costs(vRun, vTokensIn, vTokensOut, vDuration), vTotal, vDuration, Number(vRun, "waited_s"), vTokensIn, vTokensOut);
    }

    /// <summary>
    /// The misses <c>misses.jsonl</c> recorded between two times (a run's window), with whose gap each was.
    /// A miss's own <c>ts</c> places it; a record with no readable time is left out.
    /// </summary>
    /// <param name="aMissesLines">The lines of <c>misses.jsonl</c>.</param>
    /// <param name="aFromUtc">The start of the window.</param>
    /// <param name="aToUtc">The end of the window.</param>
    public static IReadOnlyList<RunReportMiss> MissesBetween(IEnumerable<string> aMissesLines, DateTime aFromUtc, DateTime aToUtc)
    {
        var vAll = Records(aMissesLines).ToList();
        return vAll
            .Where(aRecord => Text(aRecord, "kind") is "miss" or ""
                && Time(Text(aRecord, "ts")) is { } vAt && vAt >= aFromUtc && vAt <= aToUtc)
            .GroupBy(aRecord => Text(aRecord, "miss_id"))
            .Select(aGroup => Miss(aGroup.Last(), vAll))
            .ToList();
    }

    /// <summary>
    /// Tokens and models from the <c>runs.jsonl</c> records that started inside a run's window. Only what a
    /// record carries is counted: a figure no record carries stays <see langword="null"/>. The rows add up to the
    /// returned totals, with whatever a record did not split by model in a row of its own, so a Total row and a
    /// tokens tile built from these cannot contradict each other.
    /// </summary>
    /// <param name="aRunsLines">The lines of <c>runs.jsonl</c>.</param>
    /// <param name="aFromUtc">The start of the window.</param>
    /// <param name="aToUtc">The end of the window.</param>
    public static (IReadOnlyList<RunReportCost> Rows, long? TokensIn, long? TokensOut) CostBetween(IEnumerable<string> aRunsLines, DateTime aFromUtc, DateTime aToUtc)
    {
        var vOut = new Dictionary<string, long>();
        var vIn = new Dictionary<string, long>();
        var vSeconds = new Dictionary<string, long>();
        var vTier = new Dictionary<string, string>();
        var vOrder = new List<string>();
        long vUnsplitIn = 0;
        long vUnsplitOut = 0;
        var vAnyIn = false;
        var vAnyOut = false;

        foreach (var vRecord in Records(aRunsLines))
        {
            if (Time(Text(vRecord, "started")) is not { } vAt || vAt < aFromUtc || vAt > aToUtc)
            {
                continue;
            }

            var vTokensIn = Number(vRecord, "tokens_in");
            var vTokensOut = Number(vRecord, "tokens_out");
            var vModel = Text(vRecord, "model");
            var vMap = new Dictionary<string, long>();
            if (vRecord.TryGetProperty("model_tokens_out", out var vMapElement) && vMapElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var vEntry in vMapElement.EnumerateObject().Where(aEntry => ToLong(aEntry.Value) is not null))
                {
                    vMap[vEntry.Name] = ToLong(vEntry.Value)!.Value;
                }
            }

            if (vMap.Count == 0 && vModel.Length > 0 && vTokensOut is not null)
            {
                vMap[vModel] = vTokensOut.Value;
            }

            if (vTokensIn is null && vTokensOut is null && vMap.Count == 0)
            {
                continue;
            }

            foreach (var (vName, vValue) in vMap)
            {
                Add(vOut, vOrder, vName, vValue);
                vAnyOut = true;
                if (vName == vModel && Text(vRecord, "tier") is { Length: > 0 } vRecordTier)
                {
                    vTier.TryAdd(vName, vRecordTier);
                }
            }

            var vSingle = vMap.Count <= 1 && vModel.Length > 0;
            if (vTokensIn is { } vInValue)
            {
                vAnyIn = true;
                if (vSingle)
                {
                    Add(vIn, vOrder, vModel, vInValue);
                }
                else
                {
                    vUnsplitIn += vInValue;
                }
            }

            if (vSingle && Number(vRecord, "duration_s") is { } vDuration)
            {
                Add(vSeconds, vOrder, vModel, vDuration);
            }

            if (vTokensOut is { } vOutValue && vOutValue > vMap.Values.Sum())
            {
                vUnsplitOut += vOutValue - vMap.Values.Sum();
                vAnyOut = true;
            }
        }

        var vRows = vOrder
            .Select(aName => new RunReportCost(
                aName,
                vTier.GetValueOrDefault(aName, string.Empty),
                null,
                vIn.TryGetValue(aName, out var vInTokens) ? vInTokens : null,
                vOut.TryGetValue(aName, out var vOutTokens) ? vOutTokens : null,
                vSeconds.TryGetValue(aName, out var vTime) ? vTime : null))
            .ToList();
        if (vUnsplitIn > 0 || vUnsplitOut > 0)
        {
            vRows.Add(new RunReportCost("Not split by model", string.Empty, null, vUnsplitIn > 0 ? vUnsplitIn : null, vUnsplitOut > 0 ? vUnsplitOut : null, null));
        }

        long? vTotalIn = vAnyIn ? vRows.Sum(aRow => aRow.TokensIn ?? 0) : null;
        long? vTotalOut = vAnyOut ? vRows.Sum(aRow => aRow.TokensOut ?? 0) : null;
        return (vRows, vTotalIn, vTotalOut);
    }

    private static void Add(Dictionary<string, long> aTotals, List<string> aOrder, string aName, long aValue)
    {
        aTotals[aName] = aTotals.GetValueOrDefault(aName) + aValue;
        if (!aOrder.Contains(aName))
        {
            aOrder.Add(aName);
        }
    }

    private static RunReportRequirement Requirement(string aId, List<JsonElement> aGates, IReadOnlyList<RequirementItem> aItems)
    {
        var vGate = aGates.LastOrDefault(aGate => Text(aGate, "req_id") == aId);
        var vItem = aItems.FirstOrDefault(aItem => string.Equals(aItem.Id, aId, StringComparison.OrdinalIgnoreCase));
        var vType = aId.Split('-') is { Length: > 1 } vParts ? vParts[1] : string.Empty;
        var vAgent = vItem?.AgentName ?? Board.RequirementAgents.ForType(vType).Name;
        return new RunReportRequirement(
            aId,
            vItem?.Title ?? string.Empty,
            vAgent,
            vGate.ValueKind == JsonValueKind.Object ? Text(vGate, "verdict") : string.Empty,
            vGate.ValueKind == JsonValueKind.Object ? Time(Text(vGate, "ts")) : null);
    }

    private static RunReportMiss Miss(JsonElement aMiss, List<JsonElement> aAll)
    {
        var vId = Text(aMiss, "miss_id");
        var vFixed = vId.Length > 0 && aAll.Any(aOther =>
            Text(aOther, "miss_id") == vId
            && (Text(aOther, "kind") == "miss-fix" || Text(aOther, "verdict_after") == "Verified"));
        var vWhat = Text(aMiss, "what");
        var vClass = Text(aMiss, "miss_class");
        var vGate = Text(aMiss, "found_gate");
        var vNote = string.Join(" · ", new[] { vClass, vGate }.Where(aPart => aPart.Length > 0));
        return new RunReportMiss(
            vId,
            vWhat.Length > 0 ? vWhat : $"A {vClass.Replace('-', ' ')} was found{(vGate.Length > 0 ? $" at the {vGate} gate" : string.Empty)}.",
            vWhat.Length > 0 ? vNote : string.Empty,
            OwnerOf(aMiss),
            Text(aMiss, "req_id"),
            vFixed);
    }

    private static MissOwner OwnerOf(JsonElement aMiss)
    {
        if (Text(aMiss, "found_by") == "library-feedback")
        {
            return Text(aMiss, "req_class") == "RAG" ? MissOwner.TechieRag : MissOwner.TrBlazeUI;
        }

        return OwnerArtifacts.Contains(Text(aMiss, "artifact")) || OwnerMissClasses.Contains(Text(aMiss, "miss_class"))
            ? MissOwner.OwnerBrief
            : MissOwner.Chatur;
    }

    private static IReadOnlyList<RunReportCost> Costs(JsonElement aRun, long? aIn, long? aOut, long? aSeconds)
    {
        var vModel = Text(aRun, "model");
        var vTier = Text(aRun, "tier");
        if (aRun.TryGetProperty("model_tokens_out", out var vMap) && vMap.ValueKind == JsonValueKind.Object && vMap.EnumerateObject().Any())
        {
            var vModels = vMap.EnumerateObject().ToList();
            var vOnly = vModels.Count == 1;
            return vModels
                .Select(aEntry => new RunReportCost(
                    aEntry.Name,
                    aEntry.Name == vModel ? vTier : string.Empty,
                    null,
                    vOnly ? aIn : null,
                    ToLong(aEntry.Value),
                    vOnly ? aSeconds : null))
                .ToList();
        }

        return vModel.Length > 0 ? [new RunReportCost(vModel, vTier, null, aIn, aOut, aSeconds)] : [];
    }

    private static RunReportEntry? ToEntry(string aProjectName, JsonElement aRecord)
    {
        var vKey = Text(aRecord, "started");
        var vStarted = Time(vKey);
        return vKey.Length == 0 || vStarted is null
            ? null
            : new RunReportEntry(vKey, aProjectName, Text(aRecord, "cmd"), vStarted.Value, Time(Text(aRecord, "ended")));
    }

    private static IEnumerable<JsonElement> Records(IEnumerable<string> aLines)
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

    private static string Text(JsonElement aRecord, string aName) =>
        aRecord.ValueKind == JsonValueKind.Object && aRecord.TryGetProperty(aName, out var vValue)
            ? vValue.ValueKind switch
            {
                JsonValueKind.String => vValue.GetString() ?? string.Empty,
                JsonValueKind.Number => vValue.GetRawText(),
                _ => string.Empty
            }
            : string.Empty;

    private static long? Number(JsonElement aRecord, string aName) =>
        aRecord.TryGetProperty(aName, out var vValue) ? ToLong(vValue) : null;

    private static long? ToLong(JsonElement aValue) => aValue.ValueKind switch
    {
        JsonValueKind.Number when aValue.TryGetInt64(out var vLong) => vLong,
        JsonValueKind.Number => (long)aValue.GetDouble(),
        JsonValueKind.String when double.TryParse(aValue.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var vDouble) => (long)vDouble,
        _ => null
    };

    private static List<string> StringList(JsonElement aRecord, string aName)
    {
        if (!aRecord.TryGetProperty(aName, out var vValue))
        {
            return [];
        }

        return vValue.ValueKind switch
        {
            JsonValueKind.Array => vValue.EnumerateArray().Select(aItem => aItem.GetString() ?? string.Empty).Where(aItem => aItem.Length > 0).ToList(),
            JsonValueKind.String => (vValue.GetString() ?? string.Empty).Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList(),
            _ => []
        };
    }

    private static DateTime? Time(string aText) =>
        DateTime.TryParse(aText, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var vParsed)
            ? vParsed
            : null;
}
