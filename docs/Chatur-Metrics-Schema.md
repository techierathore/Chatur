# Chatur — Metrics Schema

| | |
|---|---|
| App | Chatur |
| Copied from | `.tfcore/telemetry/SCHEMA.md` (TechieFlow's own telemetry schema) |
| Date | 2026-09-22 |

Architecture §5 "Measurements": *"Chatur's own schema lives in this repository and starts as a copy
of TechieFlow's; it must keep matching in every field TfLens reads."* This document is that copy,
trimmed to the fields Chatur itself can measure in phase 1, plus one field TechieFlow's own schema
has no need for: **`tool`**, always `"chatur"` — the field that tells a reader these records came
from Chatur rather than from Claude Code or OpenCode running the framework directly.

## Where the files live

`docs/metrics/` inside **the project Chatur is working in** — not inside Chatur's own repository.
Five files, one per stream: `runs.jsonl`, `gates.jsonl`, `sessions.jsonl`, `commits.jsonl`,
`misses.jsonl`. JSONL — one JSON object per line, append-only. Never rewritten, never compacted,
never sorted in place (REQ-FN-040). The moment any measurement activity starts for a project, all
five files exist, even ones that stay empty of records for a while — an agent session working in a
project the owner just opened should not have to hunt for which of the five files Chatur has
"gotten around to" creating yet.

## One deliberate difference from TechieFlow's schema: every field is a string

TechieFlow's `tf-emit.sh` writes native JSON types — `duration_s` is a number, `routed` is a
boolean. Chatur's `IMeasurementActions.AppendAsync` takes `IReadOnlyDictionary<string, string?>`
(a day-1 decision on the action-layer signature, not a limitation of this schema), so every value
written to a stream is a JSON **string**, including one that looks numeric. A reader that wants
`duration_s` as a number parses the string. This is what keeps REQ-FN-042 honest: turning a
caller-supplied string into a number, a boolean or an array would be a guess about its shape, and
this schema exists to keep Chatur from guessing. An empty string means "not measured" — never `0`,
never `false`, never an invented value.

## Fields on every record

| Field | Notes |
|---|---|
| `v` | Schema version. Always `"1"`. Injected by `AppendAsync`; a caller-supplied value is ignored. |
| `at` | ISO-8601 UTC, e.g. `2026-09-22T10:41:02.0000000Z`. Injected by `AppendAsync`. Named `at` rather than TechieFlow's `ts` — the name `StreamsAsync` (cluster L) reads back for each stream's "newest record" column. |
| `kind` | The record's kind. Runs, gates, sessions and commits each accept exactly one value (below); misses accepts three. When a stream has exactly one valid kind, `AppendAsync` fills it in for a caller who leaves it out. |
| `app` | The project's own name — read from the `Project` row `AppendAsync` was called for, never supplied by the caller. |
| `tool` | Always `"chatur"` (REQ-FN-041). Injected; a caller-supplied value is ignored. |

## Refusal (REQ-FN-043)

A field name this document does not list, or a value outside a field's closed list below, refuses
the **whole** write — nothing is appended to the stream. The refusal is logged (Serilog) and a row
is written to the `RefusedWrite` table with the reason, so Settings ▸ Measurements can show it
(REQ-UI-035); the session itself carries on (Architecture §5 "Errors" — "a failed measurement write
is logged and never stops the work").

## `runs.jsonl` — `kind: "run"`

| Field | Closed list |
|---|---|
| `cmd`, `mode`, `started`, `ended`, `duration_s`, `reqs_touched`, `reqs_count`, `subagents`, `files_written`, `model`, `tokens_in`, `tokens_out`, `tokens_cache_read`, `tokens_cache_write`, `cost_usd` | — (free text; `reqs_touched`/`subagents` are a caller-joined list, e.g. `"REQ-UI-004,REQ-FN-011"`) |
| `build_result` | `pass` \| `fail` \| `not-run` |

## `gates.jsonl` — `kind: "gate"`

| Field | Closed list |
|---|---|
| `run_id`, `req_id`, `attempt` | — |
| `req_class` | `UI` \| `FN` \| `RAG` \| `NFR` |
| `verdict`, `prior_verdict` | `Verified` \| `Needs re-verify` \| `FAIL` \| `Blocked` \| `Implemented` \| `Done (pre-existing)` |
| `gate` | `build` \| `acceptance` \| `render` \| `assets` \| `visual` \| `mockup-parity` \| `perf` \| `standards` \| `escaped` (empty = nothing failed) |
| `gates_run` | — (a caller-joined list of the gate names above) |
| `failure_class` | `blank-data` \| `zero-rows` \| `overlap` \| `clipped` \| `offscreen` \| `slow-ttfb` \| `slow-load` \| `timeout` \| `exception` \| `assert-fail` \| `naming` \| `build-error` \| `missing-asset` \| `mockup-drift` \| `other` |
| `proof` | `executed` \| `code-audit` |

## `sessions.jsonl` — `kind: "session"`

| Field |
|---|
| `session_id`, `model`, `duration_s`, `input_tokens`, `output_tokens`, `cache_read_tokens`, `cache_creation_tokens`, `cost_usd` |

## `commits.jsonl` — `kind: "commit"`

| Field | Closed list |
|---|---|
| `sha`, `files`, `insertions`, `deletions`, `branch` | — |
| `subject_prefix` | `feat` \| `fix` \| `docs` \| `chore` \| `refactor` \| `test` \| `build` (empty otherwise — never the full subject; a subject leaks project detail) |

## `misses.jsonl` — `kind: "miss"` \| `"miss-fix"` \| `"miss-amend"`

| Field | Closed list |
|---|---|
| `miss_id`, `req_id`, `origin_phase`, `origin_agent`, `origin_run_id`, `found_phase`, `found_run_id`, `what`, `fix_run_id`, `fix_cmd`, `fix_attempt`, `reopened`, `cost_attribution` | — |
| `req_class` | `UI` \| `FN` \| `RAG` \| `NFR` |
| `miss_class` | `missed-requirement` \| `partial-implementation` \| `wrong-behaviour` \| `regression` \| `unspecified-gap` \| `spec-contradiction` \| `scope-creep` \| `hallucinated-api` \| `standards-violation` \| `other` |
| `artifact` | `brd` \| `architecture` \| `uidesign` \| `checklist` \| `devguide` \| `src` \| `tests` \| `config` \| `other` |
| `severity` | `blocker` \| `major` \| `minor` |
| `found_by` | `gate` \| `self-smoke` \| `owner` \| `production` \| `agent-review` \| `library-feedback` |
| `found_gate`, `failure_class` | Same closed lists as `gates.jsonl` `gate` / `failure_class` |
| `sort` | `spec` \| `unsaid` \| `weak-check` \| `ignored` |
| `verdict_after` | `Verified` \| `Needs re-verify` \| `FAIL` \| `deferred` \| `wont-fix` |

## What is not carried over from TechieFlow's schema

Everything `tf-emit.sh`-only injects — `harness` detection, per-run token-window enrichment
(§2.5–§2.7 of the framework schema), `run-void` records, the framework's own `attempt` derivation
helper — is TechieFlow measuring itself running its own tasks. Chatur is not TechieFlow: it has no
`tf-emit.sh`, no `cmd` vocabulary of framework task names beyond what its own agent loop reports,
and no multi-harness detection problem (there is exactly one tool writing these files: Chatur
itself, hence `tool: "chatur"` rather than `harness`). A field from the framework schema that Chatur
has no way to measure yet is simply not in the closed lists above; REQ-FN-042 already covers "not
measured" for every field this document does keep.
