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
last_updated: 2026-11-04
current_phase: Phase 3 of 4 (End to end and unattended) · Verify — 11 to verify across phases 1, 2, 56 of 56 verified
last_verified_build: PASS
last_verified_date: 2026-11-04
---

# Chatur — Status

## Where I am

Phase 3 of 4 (End to end and unattended). All 56 phase-3 rows are Verified. Chatur is on TrBlazeUI 2.2.2: File ▸ Recent projects and View ▸ Zoom are real submenus, and chatur.css keeps only the 5px around menus. Register tests no longer create accounts on App Manager; they sign in as test user 1 first. Test passwords come from tests/verify/test-settings.json. REQ-FN-004, REQ-UI-004, REQ-UI-073 and REQ-UI-080 are Verified.

## Next command to run

Claude Code:
```
/TechieFlow:agents:verifier *verify all Chatur --phase all
```
OpenCode:
```
/flow-verifier *verify all Chatur --phase all
```
Why: 11 rows of other phases are built and not verified — phase 1: 3 (REQ-UI-008, REQ-FN-012, REQ-FN-013); phase 2: 8 (REQ-FN-053, REQ-UI-047, REQ-FN-054, REQ-UI-053 …); this phase alone would be: 56 of 56 verified (every row in this phase's scope is terminal and handoff has not run yet); working docs/Chatur-P3-Checklist.md.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 0 |
| Blocked | 0 |

- None

## Known blockers

- TF-018 and TF-021 are fixed upstream (1 Nov) and not yet re-checked; on 4 Nov the verify booted the WSL harness copy without help.
- REQ-FN-004's "register creates the account" is a hand step, once per release; not yet done (checklist entry, 2026-11-04).
- The test provider key (Chatur:TestProvider, OpenCode Go) is refused with 401; the model-answer tests fail until it is renewed.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-28 | build-phase | 32/51 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-10-29 | amend-docs | 32/51 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-10-29 | *verify all Chatur --phase 3 | 51/51 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-10-30 | triage-and-fix | 56/56 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-04 | fix-issues | 56/56 Verified | docs/Chatur-P3-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 1 open · 10 fixed upstream, not yet re-checked (TF-008, TF-010, TF-013 …) · 11 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 3 fixed upstream, not yet re-checked (TR-RAG-008, TR-RAG-009, TR-RAG-006) · 6 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 2 open · 22 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-22: 68 findings, see the checklist Remarks.

## Deferred / future

- No BRD row: account controls, hiding output, merging or creating a branch.
- Submenu arrow order and 390px overflow: TR-023, TR-024 filed upstream.
- A run cut off by closing Chatur resumes when End-to-end run opens.
- With six tabs open, the newest can scroll out of view.
- Top bar says "Signed in" after a failed sign-in check.
- Owner reviews step wording for 11 processes.
- Processes has no change or evidence field.
- Appearance text size; routing's small model unnamed.
- Sign out hangs on "Loading Chatur" until reopened.
- Anthropic tier 3 seeds invalid `claude-haiku-5`.
