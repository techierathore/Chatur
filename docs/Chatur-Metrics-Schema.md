# Chatur — Metrics Schema

| | |
|---|---|
| App | Chatur |
| Copied from | `.tfcore/telemetry/SCHEMA.md` (TechieFlow's own telemetry schema) |
| Date | 2026-11-26 |

Architecture §5 "Measurements": *"Chatur's own schema lives in this repository and starts as a copy
of TechieFlow's; it must keep matching in every field TfLens reads."* This document is that copy,
trimmed to the fields Chatur itself can measure. Since 2026-11-26 a record is written in
**TechieFlow's own shape**, so TfLens reads Chatur's files unchanged (owner's test of 25 Nov 2026):
the field that tells a reader the record came from Chatur rather than from Claude Code or OpenCode
is TechieFlow's own `harness`, always `"chatur"`.

## Where the files live

`docs/metrics/` inside **the project Chatur is working in** — not inside Chatur's own repository.
Five files, one per stream: `runs.jsonl`, `gates.jsonl`, `sessions.jsonl`, `commits.jsonl`,
`misses.jsonl`, and since 2026-11-10 a sixth, `estimates.jsonl` (below). JSONL — one JSON object per line, append-only. Never rewritten, never compacted,
never sorted in place (REQ-FN-040). A record is made for something that happened — a model call, a
command, a build — and the folder and the one stream's file are made for it if they are not there
(since 2026-11-26; the rule of 23 Nov wrote a chat turn only where the folder already was, and a
real chat in a new folder left nothing). A folder that is only opened, and a chat that never reaches
a model (Chatur's own `*help`), write nothing, and no file is ever made empty.

A chat session is written once per turn with **what the session has used in all**, as TfLens reads
it: of the records with one `session_id`, the one with the most `output_tokens` stands. The
`session_id` is `chatur-<start time>-<session number>`, the same for the same session whoever reads
the project's files.

## Types: the same as TechieFlow's

`IMeasurementActions.AppendAsync` still takes text for every field (the action-layer signature),
and the writer types it as TechieFlow's `SCHEMA.md` does (this replaces the earlier "every field is
a string", which TfLens could not read: it takes a count only from a JSON number and a date only
from `ts`):

| Kind of field | Written as |
|---|---|
| Counts: `duration_s`, `reqs_count`, `files_written`, `tokens_in`, `tokens_out`, `tokens_cache_read`, `tokens_cache_write`, `input_tokens`, `output_tokens`, `cache_read_tokens`, `cache_creation_tokens`, `attempt`, `files`, `insertions`, `deletions`, `fix_attempt` | a JSON number, whole (`12.4` is written `12`); text that is not a number refuses the write |
| `cost_usd` | a JSON number, or `null` |
| `reqs_touched`, `subagents` | an array of strings, from a comma-joined text; `[]` when empty |
| `reopened` | `true` or `false` |
| Everything else | a string |

A field not measured is `null` (REQ-FN-042), never `0` and never an invented value.

## Fields on every record

| Field | Notes |
|---|---|
| `v` | Schema version, the number `1`. Injected by `AppendAsync`; a caller-supplied value is ignored. |
| `ts` | ISO-8601 UTC to the second with a `Z`, e.g. `2026-11-26T10:41:02Z`. Injected by `AppendAsync`. Records written before 2026-11-26 carry `at` instead, and still read. |
| `kind` | The record's kind. Runs, gates, sessions and commits each accept exactly one value (below); misses accepts three. When a stream has exactly one valid kind, `AppendAsync` fills it in for a caller who leaves it out. |
| `app` | The project's own name — read from the `Project` row `AppendAsync` was called for, never supplied by the caller. |
| `project_type` | `"app"`, with `project_type_inferred: true`: Chatur does not know the project's type and says it guessed. |
| `harness` | Always `"chatur"` (REQ-FN-041). Injected; a caller-supplied value is ignored. Before 2026-11-26 the field was `tool`. |

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
| `gate` | `build` \| `acceptance` \| `render` \| `assets` \| `visual` \| `mockup-parity` \| `perf` \| `standards` \| `escaped` \| `owner-mark` (empty = nothing failed) |
| `gates_run` | — (a caller-joined list of the gate names above; written as a JSON array, as TechieFlow types it) |
| `failure_class` | `blank-data` \| `zero-rows` \| `overlap` \| `clipped` \| `offscreen` \| `slow-ttfb` \| `slow-load` \| `timeout` \| `exception` \| `assert-fail` \| `naming` \| `build-error` \| `missing-asset` \| `mockup-drift` \| `other` |
| `proof` | `executed` \| `code-audit` |

`owner-mark` is Chatur's own value (added 2026-11-27, REQ-UI-133): TechieFlow's list has no gate for a person's own
judgement. TfLens has been told (docs/Chatur-TechieFlow-Feedback.md TF-054).

### Compare models (added 2026-11-27, REQ-FN-330)

Each model of a comparison writes one `runs.jsonl` record when it ends: `cmd` `compare-models`, `mode` `question` or
`change`, `started` and `ended` to the millisecond (so two models started together still have a `started` each),
`duration_s`, `model`, `tokens_in`, `tokens_out`, `cost_usd` (TechieRag's own price, `null` when it gave none) and, for
`change`, `files_written` (how many files the model changed in its copy). A person's mark on that model is a
`gates.jsonl` record: `run_id` is the model's run `started`, `verdict` `Verified` for right and `FAIL` for wrong,
`gate` `owner-mark` on wrong and empty on right (TechieFlow: a pass has no failing gate), `gates_run` `["owner-mark"]`.
A changed mark is a new record; the latest one for a `run_id` stands. A mark carries no `req_id`, so it never counts in
the first-pass figure.

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

## `estimates.jsonl` — `kind: "estimate"` (added 2026-11-10, BRD-205)

The estimates stream was already written in TechieFlow's shape before 2026-11-26, and every other stream now is too. It is written in exactly the shape of TechieFlow's schema §2.8, so TfLens reads Chatur's estimates and TechieFlow's on one page: native JSON numbers and objects, `ts`, and `harness: "chatur"`. Written only by the Estimator, one batch per confirmed start or queue entry, every record of a batch carrying the same `ts`.

```json
{"v":1,"ts":"2026-11-10T11:16:00Z","kind":"estimate","app":"Fixture","phase":2,"req_id":"REQ-FN-301","cmd":"build-phase",
 "tokens_out":{"low":2000,"mid":3000,"high":4000},"duration_s":{"low":240,"mid":300,"high":360},
 "basis":"repo","basis_runs":5,"model":"claude-opus-5-5","tier":1,"harness":"chatur"}
```

| Field | Values |
|---|---|
| `kind` | `estimate` |
| `app`, `phase` | the project's name; the phase whose checklist was estimated |
| `req_id` | the row, or `null` on a phase total |
| `cmd` | `build-phase` \| `verify-phase` \| `fix-issues` \| `triage`; machine QA is shown as verify plus fix and never stored added |
| `tokens_out`, `duration_s` | `{"low","mid","high"}` per row (on a total, the sum of the rows that had one), or `null` with `basis` `none` |
| `basis`, `basis_runs` | `repo` \| `family` \| `none`, and how many runs it rests on |
| `model` | the model that will run the step: the first model of the tier routing gives it, also when automatic routing is on (owner, 10 Nov 2026). In the framework's records this field is the estimating model; Chatur's estimate is made by no model |
| `tier` | optional, a JSON number 1 to 3: the routing tier the step is set to. Chatur's one field beyond the framework's §2.8 (owner, 10 Nov 2026); absent when the step's tier is not known |
| `harness` | always `chatur` |
| `rows_n`, `rows_no_precedent_n` | phase totals only |
| `basis_note` | `retro` only, when runs on the phase's own rows already existed |

Refused, as in the framework: a figure with `basis` `none`, a `null` figure without it, and low > mid or mid > high. An estimate is never written to `runs.jsonl`, never added to, averaged with or divided by a measurement (framework schema §6, fourth separation).

## What is not carried over from TechieFlow's schema

Everything `tf-emit.sh`-only injects — `harness` detection, per-run token-window enrichment
(§2.5–§2.7 of the framework schema), `run-void` records, the framework's own `attempt` derivation
helper — is TechieFlow measuring itself running its own tasks. Chatur is not TechieFlow: it has no
`tf-emit.sh`, no `cmd` vocabulary of framework task names beyond what its own agent loop reports,
and no multi-harness detection problem (there is exactly one tool writing these files: Chatur
itself, hence `tool: "chatur"` rather than `harness`). A field from the framework schema that Chatur
has no way to measure yet is simply not in the closed lists above; REQ-FN-042 already covers "not
measured" for every field this document does keep.
