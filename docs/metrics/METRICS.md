# Chatur — Development Metrics

Report date 2026-09-19. Project type `app`. Every figure comes from `tf-metrics.sh --report` and `--phases`; nothing was worked out by hand. All records are live (none reconstructed afterwards).

## Streams

| Stream | Records | Span |
|---|---|---|
| `runs.jsonl` | 2 | 2026-09-19 05:57 → 07:06 UTC |
| `gates.jsonl` | 0 | — |
| `sessions.jsonl` | 0 | — |
| `commits.jsonl` | 0 | — |
| `misses.jsonl` | 0 miss + 0 miss-fix, 1 review | 2026-09-19 |

First-pass rate, gate catch distribution and escape rate are not reported: no requirement has been built or verified, so there is no record behind them (insufficient data, n=0).

## 1. Runs

| Metric | Value |
|---|---|
| Runs total | 2 (day1-greenfield = 2, both stage 1) |
| Rework ratio | insufficient data (n=0 build-phase runs) |
| Requirement throughput, batch size | no data: nothing built yet |
| Commits | 0 |

## 2. Owner review of day-1

| Metric | Value |
|---|---|
| Reviews recorded | 1 (day1-review) |
| Corrections the owner gave | 6 |
| Output tokens to produce the reviewed documents | 108,020 (n=1) |
| Output tokens to apply the corrections | 524,156 (n=1) |

With one review there is no average to give (insufficient data, n=1). The two numbers above are the single measured pair: correcting cost about 4.9 times what producing did, because the corrections changed the project from one phase of 25 requirements to six phases of 164 requirements with 24 mockups.

## 3. Effort per phase

Both runs had a computable token window (measured on 2 of 2 runs).

| Phase | Runs | Wall clock | Output tokens | Input | Cache read | Cache write | Share of output |
|---|---|---|---|---|---|---|---|
| day1-greenfield | 2 | 30 min 05 s (median 15 min 02 s, max 21 min 45 s) | 632,176 | 902 | 56,628,452 | 2,142,397 | 100% |

Per run:

| Run started (UTC) | Wall clock | Output tokens | Token scope | Helpers measured |
|---|---|---|---|---|
| 05:57:55, first pass | 8 min 20 s | 108,020 | main (helpers not looked at) | not observed |
| 06:44:23, redo after review | 21 min 45 s | 524,156 | tree (helpers included) | 6 |

**By model (output tokens):** claude-fable-5-1 625,843 (99%) over 2 runs · claude-opus-5 6,333 (1%) over 1 run.

**Helpers (fan-out):** observed on 1 of 2 runs. On that run 6 helpers were started and wrote 300,500 output tokens, 57% of the observed output. The first run's window was `main` scope, so its helpers were never looked at; its 0 means "not looked", not "none". The redo run declared 1 helper type (`general-purpose`) and the harness counted 6 helper runs; the counted figure is the right one.

**Work recorded:** 189 requirement touches, 60 files written. Tokens out per run: insufficient data (n=2).

**Money.** Both runs were paid for by subscription, so no dollar amount was measured and none is reported as a cost. The script's list-price comparison, the published rate applied to the recorded tokens, is $72.41 over the 2 runs. That is a price, not a bill.

Reading note: effort per phase describes what a phase is, not how well it went. Day-1 on a Large project costs more than day-1 on a Small one by construction.

## 4. What is missing

- `sessions.jsonl` is empty: the session hook has not written a record for this project yet, so session counts and total session tokens are absent.
- No build, verify, gate, miss or commit records exist. Sections for them were left out instead of printing zeroes.
- This short exchange after the second run (the owner's decision to start fresh, and this report) is outside both run records; this report's own run record is appended after it is written.
