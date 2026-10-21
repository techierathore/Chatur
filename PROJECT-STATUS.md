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
last_updated: 2026-10-21
current_phase: Phase 1 of 4 (Workbench) · Build — 7 not built, 62 to fix, 26 of 98 verified
last_verified_build: PASS
last_verified_date: 2026-10-21
---

# Chatur — Status

## Where I am

Web-harness verify finished across all four phases. The user-secrets path fix cleared the 20 settings- and workbench-conversation FAIL rows caused by the Windows-only secret path; their acceptance tests now pass. Seven rows still FAIL, and 62 rows carry render or mockup-parity defects that need a build pass.

## Next command to run

Claude Code:
```
/TechieFlow:agents:flow-master *build-phase Chatur
```
OpenCode:
```
/flow-master *build-phase Chatur
```
Clear the 7 FAIL and 62 defect rows in phase 1 before the next verify.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 65 |
| Blocked | 0 |
| FAIL | 7 |

- [ ] REQ-FN-004 — Register an account (FAIL)
- [ ] REQ-UI-011 — Only targets that fit the machine (FAIL)
- [ ] REQ-UI-012 — Build the project (FAIL)
- [ ] REQ-UI-015 — Stop everything the run started (FAIL)
- [ ] REQ-UI-017 — A missing tool says how to fix it (FAIL)
- [ ] REQ-UI-023 — Stop the work (FAIL)
- [ ] REQ-UI-037 — Choose how Chatur looks (FAIL)
- [ ] REQ-FN-001 — The password never leaves the machine as text (Needs re-verify)
- [ ] REQ-FN-002 — The installation names its device (Needs re-verify)
- [ ] REQ-FN-003 — Staying signed in (Needs re-verify)
- (62 more open rows in docs/Chatur-Checklist.md)

## Known blockers

- Web harness verify has no seeded project state, so Workbench and project-scoped screens render empty; seed scripts need an opened project first.
- Phase 4 secondary-window screens use mockup anchors (`palette-pop`) that do not exist in the web implementation.
- The unattended test user `chatur-yolo-tester` has no account on the production App Manager, so sign-in falls back to the Owner test user.
- `tf-doc-tests.sh` reports one flaky unit-test failure (`REQ-UI-048 tests stopped at their time limit grade as could not run`) under full-suite load; the same test passes in isolation.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-20 | fix-issues | 38/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-21 | verify-phase --scope all --head maccatalyst | 11/98 Verified (phase 1 only) | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-21 | verify-phase all --head maccatalyst | 6/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-21 | verify-phase --scope all --head web | 25/98 Verified (phase 1) | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-21 | verify-phase --scope all --head web | 26/98 Verified | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 3 open · 2 fixed upstream, not yet re-checked (TF-008, TF-010) · 10 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 1 fixed upstream, not yet re-checked (TR-RAG-006) · 5 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 0 open · 21 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-21: 120 warnings, 0 failures; see the checklist Remarks.

## Deferred / future

- Placeholder logo; owner supplies the real one.
- No BRD row: account controls, hiding output, merging a process branch.
- Submenus: TR-020 fixed upstream, in the release after 2.1.6.
- A run cut off by closing Chatur resumes only when End-to-end run opens.
- With six tabs open, the newest can scroll out of view.
- Top bar says "Signed in" after a failed sign-in check.
- Owner reviews step wording for 11 processes.
- Processes has no change or evidence field.
- Appearance text size; routing's small model unnamed.
