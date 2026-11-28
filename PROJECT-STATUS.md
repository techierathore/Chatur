<!-- tf-schema
  doc: project-status
  file: PROJECT-STATUS.md
  header: project, last_updated, current_phase, last_verified_build, last_verified_date
  section: Where I am | required | max 80
  section: Next command to run | required | max 60
  section: Open requirements | required | max 200
  section: Known blockers | required | max 150
  section: Verification log | required | max 250
  section: Library feedback summary | required | max 60
  section: Standards compliance | required | max 60
  section: Deferred / future | required | max 100
    max-lines: 120
    target-lines: 60
    rule: next-command-blocks
    rule: verification-log
    rule: open-requirements-max-10
    -->
<!-- Authoring notes (agent only; never visible text).
      A one-page snapshot of the CURRENT state, rewritten in place, never appended to. Exactly the
      eight sections above, in this order, with their word limits; guard-status.sh refuses any other
      heading and any write over 120 lines; tf-doc-check.sh refuses over-long sections. What a run did
      is not written here: a run adds ONE Verification-log row (at most five kept) and updates the
      checklist Remarks; the rest lives in the telemetry streams. -->

---
project: Chatur
last_updated: 2026-11-28
current_phase: Phase 1 of 4 (Workbench) · Verify — 10 to verify across phases 1, 3, 4, 8 to verify, 88 of 96 verified, 2 not applicable
last_verified_build: not-run
last_verified_date: never
---

# Chatur — Status

## Where I am

Phase 1 of 4 (Workbench) has Mac rows open again. On 27 Nov Chatur was built and installed on the Mac for the first time (`scripts/install-mac.sh`, signed, `/Applications/Chatur.app`). It starts to Sign in. Mac-only faults are fixed: PATH, the Mac test, a Release crash, rendering, the menu shortcut and the Xcode check. 720 unit tests pass on the Mac. The Mac owner's test is in the UsageGuide. A verify on the Mac is next.

## Next command to run

Claude Code:
```
/TechieFlow:agents:verifier *verify all Chatur --phase all
```
OpenCode:
```
/flow-verifier *verify all Chatur --phase all
```
Why: 2 rows of other phases are built and not verified — phase 3: 1 (REQ-UI-073); phase 4: 1 (REQ-UI-130); and in this phase: 8 rows are built and not verified: REQ-FN-001, REQ-FN-002, REQ-FN-011, REQ-FN-012, REQ-FN-022, REQ-FN-030 …; working docs/Chatur-Checklist.md.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 1 |
| Needs re-verify | 7 |
| Blocked | 0 |

- [ ] REQ-FN-011 — Every tool, with the version found (Implemented)
- [ ] REQ-FN-001 — The password never leaves the machine as text (Needs re-verify)
- [ ] REQ-FN-002 — The installation names its device (Needs re-verify)
- [ ] REQ-FN-012 — Homebrew tools are found on a Mac (Needs re-verify)
- [ ] REQ-FN-022 — Repeated failed fixes climb a tier (Needs re-verify)
- [ ] REQ-FN-030 — Only the verifier marks work verified (Needs re-verify)
- [ ] REQ-FN-048 — An automatic check-in goes to its own branch (Needs re-verify)
- [ ] REQ-NFR-006 — The nightly workflow builds the Mac and Windows apps (Needs re-verify)

## Known blockers

- This Mac has no `tests/verify/test-settings.json`, so no agent can sign in to the installed app; the 24 Mac owner's test parts wait for the owner.
- This Mac has no key for the GitHub Packages feed: the Mac build uses TrBlazeUI 2.2.5 and TechieRag 1.3.0 (from `../TechieRag`) instead of 2.2.3 and 1.2.1.
- TR-030: precompiled, a Mac Release build never renders; the Mac build runs on the interpreter.
- REQ-UI-130 needs a real OpenRouter or DeepSeek key for its browser test.
- Phase 1 hand steps once per release: REQ-FN-001, REQ-FN-002, REQ-FN-022, REQ-FN-030, REQ-FN-048; REQ-NFR-006 a published nightly.
- REQ-FN-078 is proven against a stand-in for App Manager only (AM-002).
- TF-052, TF-054, TR-RAG-010 stay open; they block nothing.
- Owner, before the next commit: remove the `[verify-seed]` check-ins of 15 Nov (TF-037).

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-11-23 | build-phase | 94/94 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-26 | triage-and-fix | 20 rows Verified across phases 1, 3, 4 | docs/Chatur-P4-Checklist.md#requirements-status |
| 2026-11-26 | handoff-phase | 94/94 Verified; ready for the owner's test | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-27 | build-phase | 94/94 Verified; 18 of 19 new and reopened rows Verified across phases 1, 2, 4 | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-28 | build-phase | 88/96 Verified, 2 N/A | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- AppManager: 1 open · 1 closed — docs/Chatur-AppManager-Feedback.md
- TechieFlow: 3 open · 18 fixed upstream, not yet re-checked (TF-008, TF-010, TF-013 …) · 28 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 1 open · 3 fixed upstream, not yet re-checked (TR-RAG-008, TR-RAG-009, TR-RAG-006) · 7 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 6 open · 24 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-22: 68 findings, see the checklist Remarks.

## Deferred / future

- Eleven faults the Mac screen walk found that are not Mac-only (harness sample project under `src/`, Settings ▸ Account placeholder, ⌘B/⌘R labels with no key behind them, light Amber contrast and others): `tests/.artifacts/mac-walk/results.json`; for `*triage-issues`.
- No BRD row: Settings ▸ Account content, hiding output, merging or creating a branch.
- Worktrees: the "Give the next run its own copy" box is not built.
- Chat sessions carry no cost in `sessions.jsonl`.
- The run queue moves only while its tab is open.
