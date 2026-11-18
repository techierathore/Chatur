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
last_updated: 2026-11-18
current_phase: Phase 3 of 4 (End to end and unattended) · Verify — 1 to verify across phases 1, handoff done, 81 of 81 verified
last_verified_build: PASS
last_verified_date: 2026-11-18
---

# Chatur — Status

## Where I am

Phase 3 of 4 (End to end and unattended): 81 of 81 Verified after a full verify of phases 1 to 3 on 18 Nov. Phase 1 has 83 of 98 Verified, phase 2 18 of 23: twelve rows wait only on a fault in the mockup check (TF-045), and REQ-UI-018 needs a build that carries its commit. Two live accounts were walked on the Windows app. Phase 4 is not built.

## Next command to run

Claude Code:
```
/TechieFlow:agents:verifier *verify ui Chatur --phase 1
```
OpenCode:
```
/flow-verifier *verify ui Chatur --phase 1
```
Why: 1 rows of other phases are built and not verified — phase 1: 1 (REQ-UI-018); this phase alone would be: handoff done, 81 of 81 verified (every row in this phase's scope is terminal and handoff has run; waiting on the owner); working docs/Chatur-P3-Checklist.md.

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

- TF-045 (mockup check): Blocked until fixed upstream — REQ-FN-018 to 021, REQ-FN-023, REQ-UI-037, REQ-UI-038 (phase 1) and REQ-UI-053, REQ-FN-058, REQ-UI-054, REQ-FN-059, REQ-FN-060 (phase 2).
- REQ-UI-018 shows the build's commit only on a build made in the repository or a nightly; the separate copy has no `.git`.
- Hand steps, once per release: REQ-FN-001, REQ-FN-002, REQ-FN-022, REQ-FN-030, REQ-FN-048; REQ-FN-012 needs the Mac; REQ-NFR-006 needs a published nightly.
- Owner, before the next commit: the `[verify-seed]` check-ins and branches written into this repository on 15 Nov (TF-037); remove them by hand.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-11-14 | triage-and-fix | 74/74 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-15 | triage-and-fix + handoff | 74/74 Verified, ready for the owner's test | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-16 | triage-and-fix | 74/74 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-18 | triage-and-fix | 78/81 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-18 | triage-and-fix | 81/81 Verified | docs/Chatur-P3-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 2 open · 14 fixed upstream, not yet re-checked (TF-008, TF-010, TF-013 …) · 24 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 3 fixed upstream, not yet re-checked (TR-RAG-008, TR-RAG-009, TR-RAG-006) · 6 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 2 open · 24 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-22: 68 findings, see the checklist Remarks.

## Deferred / future

- No BRD row: Settings ▸ Account content, hiding output, merging or creating a branch.
- The run queue moves only while its tab is open.
- Feedback and Licence screens wait for phase 4 (REQ-UI-062, REQ-UI-064).
- Once on 18 Nov an end-to-end run's step stopped with "Unable to find the specified file"; not reproduced since (note on REQ-FN-061).
- Data kept before 17 Nov stays in the install's own chatur.db, shown to no account.
- Owner reviews step wording for 11 processes.
