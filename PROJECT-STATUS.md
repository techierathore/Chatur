---
project: Chatur
last_updated: 2026-10-04
current_phase: Phase 1 of 4 (Workbench) · Build — 1 not built, 96 of 98 verified
last_verified_build: PASS
last_verified_date: 2026-10-04
---

# Chatur — Status

## Where I am

Phase 1 of 4 (Workbench), build nearly done. 96 of 98 rows are Verified. The release pipeline now builds both downloads and publishes `v0.1.0-nightly` from the pushed commit, but the downloaded Windows build never leaves "Loading Chatur…": it has no App Manager address. One decision is waiting for you: which App Manager released builds sign in to (docs/Chatur-Decision-Request.md).

## Next command to run

Claude Code:
```
/TechieFlow:agents:flow-master *build-phase Chatur
```
OpenCode:
```
/flow-master *build-phase Chatur
```
Why: 1 rows are not built yet: REQ-NFR-006; working docs/Chatur-Checklist.md.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 1 |
| Blocked | 0 |
| FAIL | 1 |

- [ ] REQ-NFR-006 — The release workflow builds and publishes the Mac and Windows zips (FAIL)
- [ ] REQ-FN-012 — Homebrew tools are found on a Mac (Needs re-verify)

## Known blockers

- REQ-NFR-006: the nightly release holds both zips built from the pushed commit, and the build carries version 0.1.0-nightly and that commit, but the downloaded app hangs on its loading screen because no App Manager address is configured for released builds. Waiting on the owner's decision; the hang itself is a fault to fix either way.
- REQ-FN-012 needs Chatur started from Finder on a Mac; no Mac is registered for automatic checks.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-03 | verify-phase | 25/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-03 | verify-phase | 34/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-03 | verify-phase | 77/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-03 | build-phase | 96/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-04 | verify-phase | 96/98 Verified | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 0 open · 7 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 5 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 0 open · 14 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-04: no standards script exists yet; nothing measured.

## Deferred / future

- Production hosting and secrets: the App Manager address for released builds is now needed (decision request).
- The logo is a placeholder; the owner supplies the real one.
- No BRD row yet: the Settings account page controls, Start's other ways in, hiding the output strip, merging a process branch.
- Start's "Get started" buttons are disabled; the mockup draws them active.
- Text size and line spacing on the appearance page (deferred 2026-10-03).
