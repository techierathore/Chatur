using System.Text;
using System.Text.Json;
using Chatur.Core.Actions;

namespace Chatur.Core.Estimates;

/// <summary>
/// Writes an estimate as <c>estimates.jsonl</c> records in exactly the framework's shape (schema §2.8), plus
/// Chatur's one extra field <c>tier</c> (BRD-205; docs/Chatur-Metrics-Schema.md): native JSON numbers and
/// objects, <c>ts</c>, and <c>harness: "chatur"</c>. One record per row per step and one total per step,
/// every record of a batch carrying the same <c>ts</c>. Machine QA is never stored added.
/// </summary>
public static class EstimateRecords
{
    /// <summary>The batch's lines, each one JSON object, in the framework's order: the rows' records, then the totals.</summary>
    /// <param name="aEstimate">The estimate.</param>
    /// <param name="aTs">The batch's time, ISO-8601 UTC to the second, e.g. <c>2026-11-10T11:16:00Z</c>.</param>
    /// <exception cref="InvalidOperationException">A record breaks a rule the framework refuses: a figure with basis <c>none</c>, no figure without it, or low &gt; mid &gt; high.</exception>
    public static IReadOnlyList<string> ToLines(PhaseEstimate aEstimate, string aTs)
    {
        var vLines = new List<string>();
        foreach (var vReqId in aEstimate.Rows)
        {
            foreach (var vStep in EstimateSteps.All)
            {
                var vRow = aEstimate.Row(vReqId, vStep);
                var vTotal = aEstimate.Total(vStep);
                vLines.Add(Line(aEstimate, aTs, vReqId, vStep, vRow.TokensOut, vRow.DurationS, vRow.Basis, vRow.BasisRuns, vTotal.Model, vTotal.Tier, null));
            }
        }

        foreach (var vTotal in aEstimate.Totals)
        {
            vLines.Add(Line(aEstimate, aTs, null, vTotal.Step, vTotal.TokensOut, vTotal.DurationS, vTotal.Basis, vTotal.BasisRuns, vTotal.Model, vTotal.Tier, vTotal));
        }

        return vLines;
    }

    /// <summary>The time a batch is stamped with, to the second, as the framework writes it.</summary>
    /// <param name="aUtc">The moment.</param>
    public static string Stamp(DateTime aUtc) => aUtc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>One batch read back: per step, the phase total's figures and the rows that had one.</summary>
    /// <param name="aLines">The lines of <c>estimates.jsonl</c>.</param>
    /// <param name="aTs">The batch's <c>ts</c>.</param>
    /// <returns>The batch, or <see langword="null"/> when no record carries that <c>ts</c>.</returns>
    public static EstimateBatch? Batch(IEnumerable<string> aLines, string aTs)
    {
        var vRecords = EstimateJson.Records(aLines)
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "estimate" && EstimateJson.Text(aRecord, "ts") == aTs)
            .ToList();
        if (vRecords.Count == 0)
        {
            return null;
        }

        var vSteps = new List<EstimateBatchStep>();
        foreach (var vStep in EstimateSteps.All)
        {
            var vOfStep = vRecords.Where(aRecord => EstimateJson.Text(aRecord, "cmd") == vStep).ToList();
            var vTotal = vOfStep.FirstOrDefault(aRecord => IsNull(aRecord, "req_id"));
            var vCovered = vOfStep
                .Where(aRecord => !IsNull(aRecord, "req_id") && EstimateJson.Text(aRecord, "basis") != EstimateBasis.None)
                .Select(aRecord => EstimateJson.Text(aRecord, "req_id"))
                .ToHashSet(StringComparer.Ordinal);
            vSteps.Add(new EstimateBatchStep(
                vStep,
                vTotal.ValueKind == JsonValueKind.Object ? Figure(vTotal, "tokens_out") : null,
                vTotal.ValueKind == JsonValueKind.Object ? Figure(vTotal, "duration_s") : null,
                vCovered));
        }

        var vFirst = vRecords[0];
        return new EstimateBatch(
            EstimateJson.Text(vFirst, "app"),
            (int)(EstimateJson.Number(vFirst, "phase") ?? 0),
            aTs,
            vRecords.Any(aRecord => EstimateJson.Text(aRecord, "basis_note") == "retro"),
            vRecords.Where(aRecord => !IsNull(aRecord, "req_id")).Select(aRecord => EstimateJson.Text(aRecord, "req_id")).Distinct(StringComparer.Ordinal).ToList(),
            vSteps);
    }

    /// <summary>
    /// The batch a phase's reconciliation shows: the latest forecast for the phase; a phase estimated only after the
    /// fact shows its latest retro batch (framework schema §2.8: a hindsight estimate never replaces a forecast).
    /// </summary>
    /// <param name="aLines">The lines of <c>estimates.jsonl</c>.</param>
    /// <param name="aPhase">The phase.</param>
    /// <returns>The batch's <c>ts</c>, or <see langword="null"/> when the phase has none.</returns>
    public static string? LatestBatchTs(IEnumerable<string> aLines, int aPhase)
    {
        var vOfPhase = EstimateJson.Records(aLines)
            .Where(aRecord => EstimateJson.Text(aRecord, "kind") == "estimate" && EstimateJson.Number(aRecord, "phase") == aPhase)
            .ToList();
        var vRetroBatches = vOfPhase
            .Where(aRecord => EstimateJson.Text(aRecord, "basis_note") == "retro")
            .Select(aRecord => EstimateJson.Text(aRecord, "ts"))
            .ToHashSet(StringComparer.Ordinal);
        var vForecast = vOfPhase.Select(aRecord => EstimateJson.Text(aRecord, "ts")).Where(aTs => !vRetroBatches.Contains(aTs)).Max(StringComparer.Ordinal);
        return vForecast ?? vRetroBatches.Max(StringComparer.Ordinal);
    }

    private static string Line(
        PhaseEstimate aEstimate,
        string aTs,
        string? aReqId,
        string aStep,
        EstimateFigure? aTokens,
        EstimateFigure? aDuration,
        string aBasis,
        int aBasisRuns,
        string? aModel,
        int? aTier,
        StepTotal? aTotal)
    {
        var vHasFigure = aTokens is not null && aDuration is not null;
        if ((aBasis == EstimateBasis.None) == vHasFigure || (aTokens is null) != (aDuration is null))
        {
            throw new InvalidOperationException($"An estimate record for {aReqId ?? "the phase total"} ({aStep}) has basis \"{aBasis}\" and {(vHasFigure ? "a figure" : "no figure")}; the framework refuses it.");
        }

        foreach (var vFigure in new[] { aTokens, aDuration }.OfType<EstimateFigure>())
        {
            if (vFigure.Low < 0 || vFigure.Low > vFigure.Mid || vFigure.Mid > vFigure.High)
            {
                throw new InvalidOperationException($"An estimate record for {aReqId ?? "the phase total"} ({aStep}) has low {vFigure.Low}, mid {vFigure.Mid}, high {vFigure.High}; low must not exceed mid, nor mid high.");
            }
        }

        using var vBuffer = new MemoryStream();
        using (var vWriter = new Utf8JsonWriter(vBuffer))
        {
            vWriter.WriteStartObject();
            vWriter.WriteNumber("v", 1);
            vWriter.WriteString("ts", aTs);
            vWriter.WriteString("kind", "estimate");
            vWriter.WriteString("app", aEstimate.App);
            vWriter.WriteNumber("phase", aEstimate.Phase);
            WriteText(vWriter, "req_id", aReqId);
            vWriter.WriteString("cmd", aStep);
            WriteFigure(vWriter, "tokens_out", aTokens);
            WriteFigure(vWriter, "duration_s", aDuration);
            vWriter.WriteString("basis", aBasis);
            vWriter.WriteNumber("basis_runs", aBasisRuns);
            WriteText(vWriter, "model", aModel);
            if (aTier is { } vTier)
            {
                vWriter.WriteNumber("tier", vTier);
            }

            vWriter.WriteString("harness", "chatur");
            if (aTotal is not null)
            {
                vWriter.WriteNumber("rows_n", aTotal.RowsN);
                vWriter.WriteNumber("rows_no_precedent_n", aTotal.RowsNoPrecedentN);
            }

            if (aEstimate.Retro)
            {
                vWriter.WriteString("basis_note", "retro");
            }

            vWriter.WriteEndObject();
        }

        return Encoding.UTF8.GetString(vBuffer.ToArray());
    }

    private static void WriteText(Utf8JsonWriter aWriter, string aName, string? aValue)
    {
        if (aValue is null)
        {
            aWriter.WriteNull(aName);
        }
        else
        {
            aWriter.WriteString(aName, aValue);
        }
    }

    private static void WriteFigure(Utf8JsonWriter aWriter, string aName, EstimateFigure? aFigure)
    {
        if (aFigure is null)
        {
            aWriter.WriteNull(aName);
            return;
        }

        aWriter.WriteStartObject(aName);
        aWriter.WriteNumber("low", aFigure.Low);
        aWriter.WriteNumber("mid", aFigure.Mid);
        aWriter.WriteNumber("high", aFigure.High);
        aWriter.WriteEndObject();
    }

    private static bool IsNull(JsonElement aRecord, string aName) =>
        !aRecord.TryGetProperty(aName, out var vValue) || vValue.ValueKind == JsonValueKind.Null;

    private static EstimateFigure? Figure(JsonElement aRecord, string aName) =>
        aRecord.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.Object
        && EstimateJson.Number(vValue, "low") is { } vLow
        && EstimateJson.Number(vValue, "mid") is { } vMid
        && EstimateJson.Number(vValue, "high") is { } vHigh
            ? new EstimateFigure((long)vLow, (long)vMid, (long)vHigh)
            : null;
}

/// <summary>One step of a written batch.</summary>
/// <param name="Step">One of <see cref="EstimateSteps.All"/>.</param>
/// <param name="TokensOut">The phase total's output tokens, or <see langword="null"/>.</param>
/// <param name="DurationS">The phase total's seconds, or <see langword="null"/>.</param>
/// <param name="Covered">The rows that had a figure for this step.</param>
public sealed record EstimateBatchStep(string Step, EstimateFigure? TokensOut, EstimateFigure? DurationS, IReadOnlySet<string> Covered);

/// <summary>A batch read back from <c>estimates.jsonl</c>.</summary>
/// <param name="App">The project's name as written.</param>
/// <param name="Phase">The phase.</param>
/// <param name="Ts">The batch's <c>ts</c>.</param>
/// <param name="Retro">Its records carry <c>basis_note: "retro"</c>.</param>
/// <param name="Rows">The rows it estimated.</param>
/// <param name="Steps">One per step.</param>
public sealed record EstimateBatch(string App, int Phase, string Ts, bool Retro, IReadOnlyList<string> Rows, IReadOnlyList<EstimateBatchStep> Steps);
