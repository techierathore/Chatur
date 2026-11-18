# Chatur — Development Metrics

**Report date 2026-11-18.** Project type `app`. Schema v1. Every figure below comes from `bash .tfcore/telemetry/tf-metrics.sh --report .`, `--phases .` and `--estimates .`; nothing was worked out by hand. All records are live, none were rebuilt afterwards, so there is no backfilled column anywhere on this page. All records are one project type (`app`), so nothing is pooled across types. On 18 Nov the streams, this run's section and sections 1 to 3 were refreshed; sections 4 to 7 read as of 16 Nov, and the script's own printout holds today's detail for them.

## Streams

| Stream | Records |
|---|---|
| `runs.jsonl` | 194 (0 voided) |
| `gates.jsonl` | 3,926 (0 backfilled, 0 malformed) |
| `sessions.jsonl` | 32 sessions after 15 duplicate snapshots were collapsed |
| `commits.jsonl` | 27 |
| `misses.jsonl` | 800 miss + 799 miss-fix + 6 review + 2 miss-amend |
| `estimates.jsonl` | 0 |

## This run — triage-and-fix started 2026-11-17T22:56:46Z

**The owner's night paste of 18 Nov:** register test user 2, walk two live accounts, re-verify the three two-account rows and REQ-FN-017, fix the doubled `dotnet run` line, then the full verify of phases 1 to 3.

| Miss | Row | What | Found | Fix | Verdict after |
|---|---|---|---|---|---|
| MISS-Chatur-20261117-02 | REQ-UI-014 | The first Run after approving a change showed the `dotnet run` line twice | 27.2k tokens out, 2.0 min | 138.4k tokens out, shared 7 ways | Verified |
| MISS-Chatur-20261117-03 | REQ-NFR-010 | The desktop test read the owner's own database to prove it untouched | 0.3k tokens out, 0.2 min (cause already known) | shared as above | Verified |
| MISS-Chatur-20261117-04 | REQ-UI-095 | The second window kept the first account's Model providers after another account signed in | not measured by the tool (about 2 min) | shared as above | Verified |

**Found by the full verify and logged by it** (MISS-Chatur-20261118-01 to -13): REQ-UI-018 read "unknown" for the commit because the separate copy has no `.git`; twelve rows on Settings ▸ Routing, Settings ▸ Appearance and Agent workspace fail only the mockup check's badge rule, which flags controls measured the same on both sides (TF-045, Blocked). Fixed inside the run without a miss of their own: the Process run estimate cell's border, a fixture project with no repository of its own (REQ-FN-007), two specs that signed in as test user 2 when they meant the owner, and a sign-in helper that took any signed-in account for the one asked for.

## 1. First-pass rate

| Provenance | project_type | REQs scored | Rate |
|---|---|---|---|
| Live | app | 221 | 26% |

No REQ was left out for carrying rebuilt history (0 excluded).

## 2. Gate catch distribution

### Live · `app` — 2,020 failures

| Gate | Caught | Share |
|---|---|---|
| acceptance | 131 | 6% |
| render | 880 | 44% |
| visual | 59 | 3% |
| mockup-parity | 861 | 43% |
| **escaped** — no gate caught it | 89 | 4% |

Gates added partway through the stream, read against the records that actually ran them:

| Gate | Since | Ran on | Caught | Catch rate |
|---|---|---|---|---|
| perf | 2026-08-10 | 0 | 0 | not yet run on this data |
| assets | 2026-08-31 | 2,409 | 0 | 0% |
| mockup-parity | 2026-08-31 | 2,725 | 849 | 31% |
| test-users | 2026-11-01 | 685 | 0 | 0% |

The mockup-parity share above (42% of failures) is not the same thing as its catch rate (31% of the records it ran on).

## 3. Escape rate

| Provenance | project_type | Escape rate (from `gates.jsonl`) |
|---|---|---|
| Live | app | 43% |

There are 89 escape records today: the three of this run's own findings (REQ-UI-014, REQ-NFR-010, REQ-UI-095). The 17 Nov report had 86.

## 4. Throughput and rework — poolable

| Metric | Value |
|---|---|
| Runs total | 183 (verify-phase 95, build-phase 22, triage-and-fix 21, fix-issues 19, amend-docs 12, day1-greenfield 7, metrics-report 3, log-miss 2, handoff-phase 1, facilitate-brainstorming-session 1) |
| Rework ratio (fix-mode ÷ build-phase runs) | 236% |
| Batch size — median REQs per build-phase run | 15.5 |
| REQ throughput — median REQs/hour | 33.95 |
| Sessions / total tokens | 30 / 13,438,055 |
| Tokens per `Verified` REQ | 7,826.5 |
| Commit cadence | 1.93 commits per active day (27 commits over 14 days) |

**Money.** No dollar amount was billed through Claude Code. The script's list price, the published rate applied to the recorded tokens, is $1,855.90 over 157 priced runs ($1.08 per Verified REQ). That is a price, not a bill.

## 5. Misses — what was missed, who missed it, what the fix cost

| Metric | Value |
|---|---|
| Misses logged | 782 (38 open, 744 resolved, 0 wont-fix) |
| Design-miss share (`unspecified-gap`) | 4% |
| Miss-stream escape share | 12% |
| Found by | gate 647 · owner 94 · agent-review 35 · self-smoke 3 |

The miss stream's escape share (12%) is shown next to the §3 escape rate (41%), not merged with it. The two come from different records and use different definitions.

**Miss classes.** You can add these counts together.

| Class | n |
|---|---|
| partial-implementation | 400 |
| regression | 305 |
| wrong-behaviour | 33 |
| unspecified-gap | 29 |
| spec-contradiction | 6 |
| missed-requirement | 4 |
| other | 2 |

**Why it was missed.** 96 of 779 misses were assessed. None of the misses predate the field. 8 escapes carry no `why_missed`; `tf-emit.sh --amend <miss_id> why_missed <value>` is how to complete them.

| Practice | n |
|---|---|
| insufficient-verify-method | 79 |
| missing-checklist-item | 10 |
| other | 5 |
| dependency-not-declared | 1 |
| instruction-ignored | 1 |

**Whose gap it was** (`sort`, assessed on 109 of 779): weak-check 84 · spec 19 · unsaid 5 · ignored 1.

**Day-1 reviews.** 6 review records (all `day1-review`) made 31 corrections. Tokens to produce the reviewed output: 531,548.3 per review (n=6). Tokens to apply the corrections: 486,687.7 per review (n=6).

**Amendments.** 1 field was completed by a miss-amend record. No orphan fix, orphan amend or withdrawn miss is on the stream.

### 5a. Attribution — `linked` records only

**397 of 779 misses are attributed. 382 are left out** because they name a phase that no run record backs. All three of this run's misses are linked.

| By | Counts |
|---|---|
| Origin phase | build-phase 234 · fix-issues 163 |
| Origin agent | trblazeui 341 · tf-builder 30 · flow-master 26 |
| Origin model | claude-opus-5-5 249 · claude-sonnet-5-5 148 |

**These counts show what happened, not what caused it.** Which model gets the hard work is not random, so a model at the top of this list may simply have been given the hardest work. Treat it as something to look into, not as a ranking to route on.

### 5b. Rework cost — measured and apportioned stay separate

| | Fix records | Output tokens per miss |
|---|---|---|
| **Measured** (`sole`) | 2 | insufficient data (n=2) |
| Apportioned (`shared:n`, split equally — **not a measurement**) | 587 | 3,604.4 |
| Unattributable (`none`) | 201 | — |

Every counted fix record carries its tokens (0 unrecorded in either column). The apportioned figure was 2,879.7 over 519 records on 14 Nov. A change in it shows how the cost was shared out, not that fixing got cheaper or dearer.

**Cost to find each miss, beside its cost to fix.** Finding cost per miss: 11,478.8 tokens out and 2.6 minutes (n=6 measured, 53 not measured, 720 logged before 2026-11-05 when the field began). The six measured misses and this run's other two:

| Miss | Find | Fix (apportioned) |
|---|---|---|
| MISS-Chatur-20261110-01 | 105 s · 3,495 tokens | 319.0 tokens (1/440) |
| MISS-Chatur-20261110-02 | 120 s · 5,565 tokens | 319.0 tokens (1/440) |
| MISS-Chatur-20261110-04 | 120 s · 7,474 tokens | 319.0 tokens (1/440) |
| MISS-Chatur-20261114-02 | 7.9 min · 43.1k tokens | 23.4k tokens (1/11) |
| MISS-Chatur-20261114-10 | 1.5 min · 6.7k tokens | 23.5k tokens (1/11) |
| MISS-Chatur-20261115-03 | 0.2 min · 2.5k tokens | no fix yet |
| MISS-Chatur-20261115-01 | not measured | 7.6k tokens (1/23) |
| MISS-Chatur-20261115-02 | not measured | 7.6k tokens (1/23) |

No fix cost here is measured: each is an equal share of one run's window. No measured dollars exist for any fix.

## 6. Effort per phase — time, tokens, model, fan-out

Covers **179 live run records**. Token-window coverage: `tree` 131 · `main` 38 · `none` 10. The unmeasured runs are left out of the token figures, not counted as zero. 176 of 179 records have a usable elapsed time; three have none.

| Phase | Runs | Wall clock (total / median) | Output tokens | Share of output | Tokens measured on |
|---|---|---|---|---|---|
| build-phase | 22 | 35h45m / 59m12s | 7.8M | 45% | 21 of 22 |
| verify-phase | 94 | 58h26m / 27m22s | 3.7M | 21% | 87 of 94 |
| day1-greenfield | 7 | 27h10m / 37m31s | 3.0M | 18% | 7 of 7 |
| triage-and-fix | 19 | 6h27m / 1m35s | 1.1M | 6% | 18 of 19 |
| fix-issues | 19 | 8h39m / 6m14s | 1.0M | 6% | 19 of 19 |
| amend-docs | 12 | 2h19m / 9m52s | 488.5k | 3% | 12 of 12 |
| facilitate-brainstorming-session | 1 | 1h02m / — | 69.0k | 0% | 1 of 1 |
| metrics-report | 3 | 2m21s / 0m52s | 22.4k | 0% | 3 of 3 |
| log-miss | 2 | 0m46s / 0m23s | 8.0k | 0% | 1 of 2 |

Tokens out per run: build-phase 370.6k, day1-greenfield 432.6k, triage-and-fix 60.4k, fix-issues 53.1k, verify-phase 42.1k, amend-docs 40.7k, metrics-report 7.5k. Insufficient data (n=1) for log-miss and facilitate-brainstorming-session.

This run's own records: seven triage-and-fix records and six verify-phase records carry today's date. The fix is recorded in parts around the verify passes it chained, so one fix shows as several short records. The verify passes keep their own records.

### 6a. Which model did the work

| Phase | Model | Output tokens | Share | Runs |
|---|---|---|---|---|
| build-phase | claude-sonnet-5 | 2.8M | 36% | 1 |
| build-phase | claude-opus-5-5 | 2.6M | 34% | 21 |
| build-phase | claude-sonnet-5-5 | 2.3M | 30% | 18 |
| day1-greenfield | claude-opus-5 | 2.4M | 79% | 6 |
| day1-greenfield | claude-fable-5-1 | 625.8k | 21% | 2 |
| verify-phase | claude-opus-5-5 | 2.3M | 62% | 55 |
| verify-phase | claude-sonnet-5-5 | 715.0k | 20% | 4 |
| verify-phase | claude-fable-5-1 | 469.7k | 13% | 6 |
| verify-phase | claude-fable-5-2 | 117.1k | 3% | 5 |
| verify-phase | opencode-go/kimi-k2.7-code | 42.8k | 1% | 19 |
| verify-phase | claude-haiku-5-5 | 31.9k | 1% | 1 |
| triage-and-fix | claude-opus-5-5 | 913.6k | 84% | 12 |
| triage-and-fix | claude-fable-5-2 | 142.9k | 13% | 6 |
| triage-and-fix | claude-sonnet-5-5 | 31.0k | 3% | 1 |
| fix-issues | claude-opus-5-5 | 789.2k | 78% | 17 |
| fix-issues | claude-sonnet-5-5 | 166.1k | 16% | 2 |
| fix-issues | claude-fable-5-1 | 54.1k | 5% | 1 |
| amend-docs | claude-opus-5-5 | 488.5k | 100% | 12 |
| facilitate-brainstorming-session | claude-fable-5-1 | 54.7k | 79% | 1 |
| facilitate-brainstorming-session | claude-opus-5 | 14.3k | 21% | 1 |
| metrics-report | claude-opus-5-5 | 17.9k | 80% | 2 |
| metrics-report | claude-fable-5-1 | 4.6k | 20% | 1 |
| log-miss | claude-opus-5-5 | 8.0k | 100% | 1 |

The same warning applies here: this shows what happened, not what caused it. A phase that costs more is mostly a fact about what that phase does.

Routing: every phase reports on-tier 0. Each run whose model is known ran on a model other than its tier's declared model (observed, never enforced). Measured money exists only for the 19 OpenCode verify runs: $12.15 of a monthly plan allowance. That is a meter, not an invoice, and it is not added to any Claude figure.

### 6b. Helper agents (fan-out) — measured, on its own denominator

| Phase | Runs observed | Spawns (total / median / max) | Runs that fanned out | Output in helpers | Helper share |
|---|---|---|---|---|---|
| build-phase | 21 of 22 | 99 / 3 / 21 | 19 | 5.2M | 66% |
| day1-greenfield | 6 of 7 | 25 / 4.5 / 6 | 5 | 1.3M | 45% |
| verify-phase | 69 of 94 | 41 / 0 / 12 | 22 | 732.9k | 27% |
| triage-and-fix | 10 of 19 | 3 / 0.0 / 2 | 2 | 185.5k | 28% |
| fix-issues | 13 of 19 | 2 / 0 / 2 | 1 | 139.7k | 18% |
| amend-docs | 9 of 12 | 5 / 1 / 1 | 5 | 88.5k | 39% |
| facilitate-brainstorming-session | 1 of 1 | 1 / 1 / 1 | 1 | 14.3k | 21% |
| metrics-report | 2 of 3 | 0 / 0.0 / 0 | 0 | 0 | 0% |
| log-miss | 0 of 2 | — | — | — | — |

Every run outside the "observed" column was a non-`tree` window; none predates the field. Such a window never read the helper transcripts, so a 0 there means "not looked at", not "none ran".

**Declared vs measured.** The counts the agents typed in differ from the counts the harness measured: build-phase declared 36 and measured 99, day1-greenfield 20 and 25, fix-issues 5 and 2, triage-and-fix 6 and 3. The measured count is the right one. The gap shows that tasks do not report their own helpers accurately.

## 6c. Estimate against actual

`tf-metrics.sh --estimates .` prints: no estimates yet: run *estimate-phase {App} before *build-phase (docs/metrics/estimates.jsonl)

## 7. What is missing, and what the script and streams flagged

- **Measured rework cost per miss**: insufficient data (n=2 `sole` records).
- **Estimates**: none yet, so question 6 has no answer.
- **perf gate**: it has never run on this project (0 records), so it has no catch rate. The test-users gate ran on 672 records and caught nothing.
- **Escapes with no reason recorded**: 8 misses a person found carry no `why_missed`. `tf-emit.sh --amend` completes them.
- **Finding cost wrong or missing for two of this run's misses** (MISS-Chatur-20261115-01 and -02): a script edge, filed as TF-036. The script prints both as not measured.
- **Fix-record cost label does not match how it was split.** This run's fix records each say `cost_attribution: "shared:13"`, and the script prints the share as 1/23. The per-miss share above follows the script. Either way it is apportioned, not measured.
- **Short `triage-and-fix` records.** Seven records ran 20 seconds or less and touched no REQ (29 Oct, 10 Nov, two on 14 Nov, three on 15 Nov). Most are the parts a fix is recorded in around its verify passes. All are counted in the triage-and-fix figures above, which is why that phase's median is 1m35s. No run is voided; only the owner can void one (`tf-emit.sh --void-run`).
- **Attribution gaps**: 382 misses are not linked to a run record.
- **Sessions and commits lag**: `sessions.jsonl` ends 2026-11-14 and `commits.jsonl` ends 2026-10-30, so this run's session and any later commits are not in those counts yet. The commit hook is installed on this machine.
- **List price**: 1 fix-issues run, 6 triage-and-fix runs and 7 verify-phase runs had no rate for their model, so they are outside the list price (not priced at zero).
- **This report's own run record was not written.** This report ran inside a `*triage-and-fix` run, whose own run record and status gate cover it.
