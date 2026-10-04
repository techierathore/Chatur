namespace Chatur.Core.Measurements;

/// <summary>
/// The closed vocabulary <c>docs/Chatur-Metrics-Schema.md</c> documents — copied from
/// <c>.tfcore/telemetry/SCHEMA.md</c> and trimmed to what Chatur itself can measure. Used by
/// <see cref="MeasurementActions"/> to refuse a field or a value the schema does not know
/// (REQ-FN-043) and to leave anything not measured empty (REQ-FN-042). Every value is a string —
/// see the schema document's "One deliberate difference from TechieFlow's schema".
/// </summary>
internal static class MeasurementSchema
{
    /// <summary>The five stream file names Chatur writes (REQ-FN-040), in no particular order.</summary>
    public static readonly IReadOnlyList<string> StreamFileNames = new[]
    {
        "runs.jsonl", "gates.jsonl", "sessions.jsonl", "commits.jsonl", "misses.jsonl"
    };

    /// <summary>Every field a stream recognises, beyond the four common ones (<c>v</c>, <c>ts</c>, <c>kind</c>, <c>app</c>, <c>tool</c>).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> KnownFieldsByStream = new Dictionary<string, IReadOnlySet<string>>
    {
        ["runs.jsonl"] = new HashSet<string>
        {
            "cmd", "mode", "started", "ended", "duration_s", "reqs_touched", "reqs_count",
            "subagents", "files_written", "build_result", "model", "tokens_in", "tokens_out",
            "tokens_cache_read", "tokens_cache_write", "cost_usd"
        },
        ["gates.jsonl"] = new HashSet<string>
        {
            "run_id", "req_id", "req_class", "attempt", "verdict", "gate", "gates_run",
            "failure_class", "prior_verdict", "proof"
        },
        ["sessions.jsonl"] = new HashSet<string>
        {
            "session_id", "model", "duration_s", "input_tokens", "output_tokens",
            "cache_read_tokens", "cache_creation_tokens", "cost_usd"
        },
        ["commits.jsonl"] = new HashSet<string>
        {
            "sha", "files", "insertions", "deletions", "subject_prefix", "branch"
        },
        ["misses.jsonl"] = new HashSet<string>
        {
            "miss_id", "req_id", "req_class", "miss_class", "artifact", "severity", "origin_phase",
            "origin_agent", "origin_run_id", "found_by", "found_phase", "found_gate", "found_run_id",
            "failure_class", "what", "sort", "fix_run_id", "fix_cmd", "fix_attempt", "verdict_after",
            "reopened", "cost_attribution"
        }
    };

    /// <summary>The <c>kind</c> values each stream accepts.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> KindsByStream = new Dictionary<string, IReadOnlySet<string>>
    {
        ["runs.jsonl"] = new HashSet<string> { "run" },
        ["gates.jsonl"] = new HashSet<string> { "gate" },
        ["sessions.jsonl"] = new HashSet<string> { "session" },
        ["commits.jsonl"] = new HashSet<string> { "commit" },
        ["misses.jsonl"] = new HashSet<string> { "miss", "miss-fix", "miss-amend" }
    };

    /// <summary>Closed lists for fields whose value is not free text, checked wherever the field appears.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> EnumValuesByField = new Dictionary<string, IReadOnlySet<string>>
    {
        ["req_class"] = new HashSet<string> { "UI", "FN", "RAG", "NFR" },
        ["verdict"] = new HashSet<string> { "Verified", "Needs re-verify", "FAIL", "Blocked", "Implemented", "Done (pre-existing)" },
        ["prior_verdict"] = new HashSet<string> { "Verified", "Needs re-verify", "FAIL", "Blocked", "Implemented", "Done (pre-existing)" },
        ["gate"] = new HashSet<string> { "build", "acceptance", "render", "assets", "visual", "mockup-parity", "perf", "standards", "escaped" },
        ["found_gate"] = new HashSet<string> { "build", "acceptance", "render", "assets", "visual", "mockup-parity", "perf", "standards", "escaped" },
        ["failure_class"] = new HashSet<string>
        {
            "blank-data", "zero-rows", "overlap", "clipped", "offscreen", "slow-ttfb", "slow-load",
            "timeout", "exception", "assert-fail", "naming", "build-error", "missing-asset", "mockup-drift", "other"
        },
        ["proof"] = new HashSet<string> { "executed", "code-audit" },
        ["build_result"] = new HashSet<string> { "pass", "fail", "not-run" },
        ["subject_prefix"] = new HashSet<string> { "feat", "fix", "docs", "chore", "refactor", "test", "build" },
        ["miss_class"] = new HashSet<string>
        {
            "missed-requirement", "partial-implementation", "wrong-behaviour", "regression",
            "unspecified-gap", "spec-contradiction", "scope-creep", "hallucinated-api", "standards-violation", "other"
        },
        ["artifact"] = new HashSet<string> { "brd", "architecture", "uidesign", "checklist", "devguide", "src", "tests", "config", "other" },
        ["severity"] = new HashSet<string> { "blocker", "major", "minor" },
        ["found_by"] = new HashSet<string> { "gate", "self-smoke", "owner", "production", "agent-review", "library-feedback" },
        ["sort"] = new HashSet<string> { "spec", "unsaid", "weak-check", "ignored" },
        ["verdict_after"] = new HashSet<string> { "Verified", "Needs re-verify", "FAIL", "deferred", "wont-fix" }
    };

    /// <summary>
    /// Checks a stream name and its fields against the schema (REQ-FN-043).
    /// </summary>
    /// <param name="aStreamFileName">The canonical stream file name, e.g. <c>"runs.jsonl"</c>.</param>
    /// <param name="aFields">The caller's fields.</param>
    /// <param name="aReason">Why the write is refused, or <see langword="null"/> when it is not.</param>
    /// <returns><see langword="true"/> when the write may proceed.</returns>
    public static bool TryValidate(string aStreamFileName, IReadOnlyDictionary<string, string?> aFields, out string? aReason)
    {
        if (!KnownFieldsByStream.TryGetValue(aStreamFileName, out var vKnownFields))
        {
            aReason = $"\"{aStreamFileName}\" is not one of Chatur's five measurement streams.";
            return false;
        }

        foreach (var vField in aFields.Keys)
        {
            if (vField is "v" or "at" or "app" or "tool")
            {
                continue; // injected — a caller-supplied value here is silently overridden, not refused.
            }

            if (vField != "kind" && !vKnownFields.Contains(vField))
            {
                aReason = $"\"{vField}\" is not a field {aStreamFileName} knows.";
                return false;
            }

            var vValue = aFields[vField];
            if (string.IsNullOrEmpty(vValue))
            {
                continue; // empty is "not measured" (REQ-FN-042), never checked against a closed list.
            }

            if (vField == "kind")
            {
                if (!KindsByStream[aStreamFileName].Contains(vValue))
                {
                    aReason = $"\"{vValue}\" is not a kind {aStreamFileName} accepts.";
                    return false;
                }

                continue;
            }

            if (EnumValuesByField.TryGetValue(vField, out var vAllowedValues) && !vAllowedValues.Contains(vValue))
            {
                aReason = $"\"{vValue}\" is not a value \"{vField}\" accepts.";
                return false;
            }
        }

        aReason = null;
        return true;
    }
}
