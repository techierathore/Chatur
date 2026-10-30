# Chatur — Development Metrics

**Report date 2026-10-30.** Project type `app`. Schema v1. Every figure below comes from `bash .tfcore/telemetry/tf-metrics.sh --report . --json` and `--phases .`; nothing was worked out by hand. All records are live, none were rebuilt afterwards, so there is no backfilled column anywhere on this page. All records are one project type (`app`), so nothing is pooled across types.

## Streams

| Stream | Records | Span (UTC) |
|---|---|---|
| `runs.jsonl` | 131 | 2026-09-19 → 2026-10-29 |
| `gates.jsonl` | 3,005 (0 backfilled, 0 malformed) | 2026-09-30 → 2026-10-29 |
| `sessions.jsonl` | 36 lines, 22 sessions after 14 duplicate snapshots were collapsed | 2026-09-19 → 2026-10-29 |
| `commits.jsonl` | 25 (0 duplicates) | 2026-09-19 → 2026-10-26 |
| `misses.jsonl` | 690 miss + 272 miss-fix + 6 review | 2026-09-19 → 2026-10-29 |

The commit hook is installed on this clone, so the commit count is not understated by a missing hook.

## This run — triage-and-fix started 2026-10-29T19:09:30Z

The owner found 9 defects after every gate had passed them. Each was logged as a miss and as an escape (`gate:"escaped"`):

| REQ | Miss | Kind | Who missed it | Fix record verdict |
|---|---|---|---|---|
| REQ-UI-023 | MISS-Chatur-20261029-01 | wrong-behaviour (code) | fix-issues / trblazeui, linked | **no fix record** — open |
| REQ-FN-025 | MISS-Chatur-20261029-02 | wrong-behaviour (code) | fix-issues / trblazeui, linked | **no fix record** — open |
| REQ-UI-042 | MISS-Chatur-20261029-03 | wrong-behaviour (code) | fix-issues / trblazeui, linked | **no fix record** — open |
| REQ-UI-039 | MISS-Chatur-20261029-04 | wrong-behaviour (code) | build-phase / flow-master, inferred | **no fix record** — open |
| REQ-UI-079 | MISS-Chatur-20261029-05 | unspecified-gap (spec) | day1-greenfield / analyst, inferred | Verified |
| REQ-UI-080 | MISS-Chatur-20261029-06 | unspecified-gap (spec) | day1-greenfield / analyst, inferred | Verified |
| REQ-UI-081 | MISS-Chatur-20261029-07 | unspecified-gap (spec) | day1-greenfield / analyst, inferred | Verified |
| REQ-UI-082 | MISS-Chatur-20261029-08 | unspecified-gap (spec) | day1-greenfield / analyst, inferred | Verified |
| REQ-NFR-010 | MISS-Chatur-20261029-09 | unspecified-gap (spec) | day1-greenfield / analyst, inferred | Verified |

REQ-UI-079 to 082 were first issued by `tf-triage.sh` as REQ-UI-1000 to 1003 and renumbered (TechieFlow feedback TF-021). No record in any stream still carries the old ids.

**What the verify passes after the fix found.**
- The pass started 20:39:20 verified all five new spec rows on their first attempt, plus REQ-UI-073.
- The pass started 21:10:31 failed REQ-FN-025, REQ-UI-023, REQ-UI-039, REQ-UI-042 and REQ-FN-045 again at the render gate. That opened four new misses (-10 to -13) on the four owner-found REQs. Their class is `partial-implementation`, which differs from the owner's `wrong-behaviour`, so the collapse rule opened new records instead of reusing the old ones.

**Fix records.** The run wrote 13 `miss-fix` records. Seven carry `Verified`: the five spec rows above, plus older misses on REQ-UI-037 and REQ-UI-073. Six carry `Needs re-verify`: the four new render misses (-10 to -13), plus older misses on REQ-UI-047 and REQ-FN-045. All 13 share one token window (385,303 output tokens, `shared:13`). So their cost is apportioned, not measured.

**Bottom line.** Five of the nine owner-found defects are closed. The other four (REQ-UI-023, REQ-FN-025, REQ-UI-039, REQ-UI-042) are still failing, and their original owner-found misses (-01 to -04) have no fix record against them.

## 1. First-pass rate

| Provenance | project_type | REQs scored | First-pass | Rate |
|---|---|---|---|---|
| Live | app | 196 | 38 | 19% |

No REQ carries backfilled history, so none are left out of this rate.

## 2. Gate catch distribution

### Live · `app` — 1,777 failures

| Gate | Caught | Share |
|---|---|---|
| acceptance | 97 | 5% |
| render | 841 | 47% |
| visual | 5 | 0% |
| mockup-parity | 769 | 43% |
| **escaped** — no gate caught it | 65 | 4% |

Gates added partway through the stream, read against the records that actually ran them:

| Gate | Since | Ran on | Caught | Catch rate |
|---|---|---|---|---|
| perf | 2026-08-10 | 0 | 0 | not yet run on this data |
| assets | 2026-08-31 | 2,022 | 0 | 0% |
| mockup-parity | 2026-08-31 | 2,221 | 769 | 35% |

The mockup-parity share above (43% of failures) is not the same thing as its catch rate (35% of the records it ran on).

## 3. Escape rate

| Provenance | project_type | Escape rate (from `gates.jsonl`) |
|---|---|---|
| Live | app | 38% |

There are 65 escape records. 9 of them come from this run. Four of the nine had a prior verdict of `Needs re-verify` and five were new rows. None of this run's escapes reopened a REQ that was `Verified`.

## 4. Throughput and rework — poolable

| Metric | Value |
|---|---|
| Runs total | 131 (verify-phase 71, build-phase 19, fix-issues 17, amend-docs 11, day1-greenfield 7, triage-and-fix 2, log-miss 2, metrics-report 1, facilitate-brainstorming-session 1) |
| Rework ratio (fix-mode ÷ build-phase runs) | 153% |
| Batch size — median REQs per build-phase run | 13 |
| REQ throughput — median REQs/hour | 33.91 |
| Sessions / total tokens | 22 / 10,862,826 |
| Tokens per `Verified` REQ | 8,845.9 |
| Commit cadence | 1.92 commits per active day (25 commits over 13 days) |

**Money.** No dollar amount was billed through Claude Code. Its records carry `cost_usd: null` because the transcript holds no cost. The script's list price, which is the published rate applied to the recorded tokens, is $1,636.28 over 123 priced runs ($1.33 per Verified REQ). That is a price, not a bill. The only measured money is the OpenCode plan allowance in §6.

## 5. Misses — what was missed, who missed it, what the fix cost

| Metric | Value |
|---|---|
| Misses logged | 690 (429 open, 261 resolved, 0 wont-fix) |
| Design-miss share (`unspecified-gap`) | 4% |
| Miss-stream escape share | 10% |
| Found by | gate 582 · owner 72 · agent-review 33 · self-smoke 3 |

The miss stream's escape share (10%) is shown next to the §3 escape rate (38%), not merged with it. The two come from different records and use different definitions.

**Miss classes.** You can add these counts together.

| Class | n |
|---|---|
| partial-implementation | 366 |
| regression | 263 |
| unspecified-gap | 28 |
| wrong-behaviour | 21 |
| spec-contradiction | 6 |
| missed-requirement | 4 |
| other | 2 |

**Why it was missed.** 82 of 690 misses were assessed. None of the misses predate the field, and no escape is missing it.

| Practice | n |
|---|---|
| insufficient-verify-method | 65 |
| missing-checklist-item | 10 |
| other | 5 |
| dependency-not-declared | 1 |
| instruction-ignored | 1 |

**Whose gap it was** (`sort`, assessed on 86 of 690): weak-check 62 · spec 18 · unsaid 5 · ignored 1.

### 5a. Attribution — `linked` records only

**311 of 690 misses are attributed. 379 are left out** because they name a phase that no run record backs. That includes this run's six day-1 and build-phase misses.

| By | Counts |
|---|---|
| Origin phase | build-phase 167 · fix-issues 144 |
| Origin agent | trblazeui 266 · flow-master 25 · tf-builder 20 |
| Origin model | claude-opus-5-5 218 · claude-sonnet-5-5 93 |

**These counts show what happened, not what caused it.** Which model gets the hard work is not random, so a model at the top of this list may simply have been given the hardest work. Treat it as something to look into, not as a ranking to route on.

### 5b. Rework cost — measured and apportioned stay separate

| | Fix records | Output tokens per miss |
|---|---|---|
| **Measured** (`sole`) | 2 | insufficient data (n=2) |
| Apportioned (`shared:n`, split equally — **not a measurement**) | 77 | 16,566.7 |
| Unattributable (`none`) | 193 | — |

No measured dollars exist for any fix. This run's 13 fix records are all apportioned.

## 6. Effort per phase — time, tokens, model, fan-out

Covers **131 live run records**. Token-window coverage: `tree` 110 · `main` 16 · `none` 5. The 5 unmeasured runs are left out of the token figures, not counted as zero. Three runs have no elapsed time.

| Phase | Runs | Wall clock (total / median) | Output tokens | Share of output | Tokens measured on |
|---|---|---|---|---|---|
| build-phase | 19 | 34h14m / 1h00m | 7.1M | 50% | 19 of 19 |
| day1-greenfield | 7 | 27h10m / 37m31s | 3.0M | 21% | 7 of 7 |
| verify-phase | 71 | 44h13m / 29m21s | 2.2M | 16% | 67 of 71 |
| fix-issues | 17 | 8h25m / 6m14s | 929.4k | 7% | 17 of 17 |
| triage-and-fix | 2 | 1h29m / 44m58s | 387.4k | 3% | 2 of 2 |
| amend-docs | 11 | 1h59m / 9m04s | 332.6k | 2% | 11 of 11 |
| facilitate-brainstorming-session | 1 | 1h02m / — | 69.0k | 0% | 1 of 1 |
| log-miss | 2 | 0m46s / 0m23s | 8.0k | 0% | 1 of 2 |
| metrics-report | 1 | 0m35s / — | 4.6k | 0% | 1 of 1 |

Tokens out per run: insufficient data (n<3) for triage-and-fix, log-miss, metrics-report and facilitate-brainstorming-session.

### 6a. Which model did the work

| Phase | Model | Output tokens | Share | Runs |
|---|---|---|---|---|
| build-phase | claude-sonnet-5 | 2.8M | 40% | 1 |
| build-phase | claude-opus-5-5 | 2.2M | 30% | 19 |
| build-phase | claude-sonnet-5-5 | 2.1M | 30% | 16 |
| day1-greenfield | claude-opus-5 | 2.4M | 79% | 6 |
| day1-greenfield | claude-fable-5-1 | 625.8k | 21% | 2 |
| verify-phase | claude-opus-5-5 | 1.3M | 60% | 40 |
| verify-phase | claude-fable-5-1 | 469.7k | 21% | 6 |
| verify-phase | claude-sonnet-5-5 | 347.2k | 16% | 2 |
| verify-phase | opencode-go/kimi-k2.7-code | 42.8k | 2% | 19 |
| verify-phase | claude-haiku-5-5 | 31.9k | 1% | 1 |
| fix-issues | claude-opus-5-5 | 709.1k | 76% | 15 |
| fix-issues | claude-sonnet-5-5 | 166.1k | 18% | 2 |
| fix-issues | claude-fable-5-1 | 54.1k | 6% | 1 |
| triage-and-fix | claude-opus-5-5 | 387.4k | 100% | 2 |
| amend-docs | claude-opus-5-5 | 332.6k | 100% | 11 |

The same warning applies here: this shows what happened, not what caused it. A phase that costs more is mostly a fact about what that phase does.

Routing: every phase reports on-tier 0. Each run whose model is known ran on a model other than its tier's declared model (observed, never enforced). Measured money exists only for the 19 OpenCode verify runs: $12.15 of a monthly plan allowance. That is a meter, not an invoice, and it is not added to any Claude figure.

### 6b. Helper agents (fan-out) — measured, on its own denominator

| Phase | Runs observed | Spawns (total / median / max) | Runs that fanned out | Output in helpers | Helper share |
|---|---|---|---|---|---|
| build-phase | 19 of 19 | 94 / 4 / 21 | 17 | 5.0M | 70% |
| day1-greenfield | 6 of 7 | 25 / 4.5 / 6 | 5 | 1.3M | 45% |
| verify-phase | 59 of 71 | 31 / 0 / 12 | 20 | 365.1k | 22% |
| triage-and-fix | 2 of 2 | 2 / 1.0 / 2 | 1 | 154.5k | 40% |
| fix-issues | 13 of 17 | 2 / 0 / 2 | 1 | 139.7k | 18% |
| amend-docs | 9 of 11 | 5 / 1 / 1 | 5 | 88.5k | 39% |
| log-miss | 0 of 2 | — | — | — | — |

Every run outside the "observed" column was a non-`tree` window. Such a window never read the helper transcripts, so a 0 there means "not looked at", not "none ran".

**Declared vs measured.** The counts the agents typed in differ from the counts the harness measured: build-phase declared 34 and measured 94, day1-greenfield 20 and 25, fix-issues 4 and 2. The measured count is the right one. The gap shows that tasks under-report their own helpers.

## 7. What is missing, and what the script and streams flagged

- **Measured rework cost per miss**: insufficient data (n=2 `sole` records).
- **perf gate**: it has never run on this project (0 records), so it has no catch rate.
- **Owner-found misses -01 to -04 have no fix record.** The fix records for those four REQs went to the later render misses (-10 to -13), and all of those are `Needs re-verify`. The four original misses count as open.
- **A second `triage-and-fix` run record** (started 21:43:04, 6 seconds, no REQs, 2,136 output tokens) sits after the real run. It looks like a stray closing emit. It is counted in the triage-and-fix figures above. Only the owner can decide to void it (`tf-emit.sh --void-run`).
- **`failure_class:"menu"`** appears on 165 miss records, including this run's render misses. It is not in the schema's closed list (SCHEMA §3.3).
- **Attribution gaps**: 379 misses are not linked to a run record, including this run's five spec misses (day-1 by the analyst) and the REQ-UI-039 misses.
- **List price**: 1 fix-issues run and 2 verify-phase runs had no rate for their model, so they are outside the list price (not priced at zero).
- **This report's own run record was not written.** For this request the owner ruled out any write to a `.jsonl` stream, so the closing `runs.jsonl` record the task normally adds was skipped.
