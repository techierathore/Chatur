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
last_updated: 2026-10-24
current_phase: Phase 1 of 4 (Workbench) · Build — 68 to fix, 26 of 98 verified
last_verified_build: PASS
last_verified_date: 2026-10-24
---

# Chatur — Status

## Where I am

Phase 1 of 4 (Workbench). Chatur is on TechieRag 1.1.7 and the test model is Kimi K2.7 Code. A web verify on 24 Oct passed every chat test; REQ-UI-034 is now Verified (26 of 98). The other 72 rows fail render or mockup checks on Workbench, Routing, Repository and Settings. The Mac head can only measure Sign in (TF-016).

## Next command to run

Claude Code:
```
/TechieFlow:agents:flow-master *build-phase Chatur
```
OpenCode:
```
/flow-master *build-phase Chatur
```
Clear the render and mockup-parity gaps on Workbench and Settings so the 11 chat rows can reach Verified.

## Open requirements

| Status | Count |
|---|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 71 |
| Blocked | 1 |

- [ ] REQ-UI-001 — Sign in with an account (Needs re-verify)
- [ ] REQ-FN-001 — The password never leaves the machine as text (Needs re-verify)
- [ ] REQ-UI-002 — A refused sign-in says why (Needs re-verify)
- [ ] REQ-FN-002 — The installation names its device (Needs re-verify)
- [ ] REQ-FN-003 — Staying signed in (Needs re-verify)
- [ ] REQ-FN-004 — Register an account (Needs re-verify)
- [ ] REQ-FN-005 — A weak password is caught before it is sent (Needs re-verify)
- [ ] REQ-FN-007 — What the selected project is (Needs re-verify)
- [ ] REQ-FN-008 — Straight into the work (Needs re-verify)
- (62 more open rows in docs/Chatur-Checklist.md)

## Known blockers

- REQ-UI-017 is desktop-head only and remains blocked on the web harness.
- The Workbench Project menu lacks the seeded fixture targets because the fixture project has not been built.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-24 | verify-phase --scope all --head web | 26/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-22 | verify-phase --scope all --head web | 25/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-22 | verify-phase | 25/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-22 | verify-phase --scope REQ-FN-021,REQ-FN-026,REQ-FN-027,REQ-FN-033,REQ-FN-034,REQ-FN-035,REQ-FN-036,REQ-UI-019,REQ-UI-021,REQ-UI-022,REQ-UI-032 --head web | 25/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-24 | fix-issues | 25/98 Verified | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 4 open · 2 fixed upstream, not yet re-checked (TF-008, TF-010) · 10 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 2 open · 1 fixed upstream, not yet re-checked (TR-RAG-006) · 6 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 0 open · 21 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-22: 68 findings, see the checklist Remarks.

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
