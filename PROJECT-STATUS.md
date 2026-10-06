---
project: Chatur
last_updated: 2026-10-06
current_phase: Phase 1 of 4 (Workbench) · Verify — 1 to verify, 96 of 98 verified
last_verified_build: PASS
last_verified_date: 2026-10-06
---

# Chatur — Status

## Where I am

Phase 1 of 4 (Workbench), verifying. 96 of 98 rows are Verified. The pipeline now writes the App Manager address and the installed-app key into both downloads, every App Manager call sends the installation's device id, and a failed sign-in check shows its reason instead of hanging. REQ-NFR-006 waits on the next nightly signing in with the owner's installed-app key.

## Next command to run

Claude Code:
```
/TechieFlow:agents:verifier *verify functional Chatur
```
OpenCode:
```
/flow-verifier *verify functional Chatur
```
Why: 1 rows are built and not verified: REQ-NFR-006; 1 more could not be measured (every test skipped) and need the owner: REQ-FN-012; working docs/Chatur-Checklist.md.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 1 |
| Needs re-verify | 1 |
| Blocked | 0 |

- [ ] REQ-NFR-006 — The release workflow builds and publishes the Mac and Windows zips (Implemented)
- [ ] REQ-FN-012 — Homebrew tools are found on a Mac (Needs re-verify)

## Known blockers

- REQ-NFR-006: the owner's key must be an installed-app key from App Manager (no secret); after the next push the downloaded nightly is signed in and checked. Sign-in with an installed-app key has not been run on this machine, whose development App Manager has no such key for Chatur.
- REQ-FN-012 needs Chatur started from Finder on a Mac; no Mac is registered for automatic checks.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-03 | verify-phase | 34/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-03 | verify-phase | 77/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-03 | build-phase | 96/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-04 | verify-phase | 96/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-06 | build-phase | 96/98 Verified | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 0 open · 7 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 5 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 0 open · 14 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-06: no standards script exists yet; nothing measured.

## Deferred / future

- The logo is a placeholder; the owner supplies the real one.
- No BRD row yet: the Settings account page controls, Start's other ways in, hiding the output strip, merging a process branch.
- Start's "Get started" buttons are disabled; the mockup draws them active.
- Text size and line spacing on the appearance page (deferred 2026-10-03).
- The top bar reads "Signed in" while the sign-in check has failed.
