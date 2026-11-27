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
last_updated: 2026-11-27
current_phase: Phase 3 of 4 (End to end and unattended) · UAT — handoff done, 94 of 94 verified
last_verified_build: PASS
last_verified_date: 2026-11-27
---

# Chatur — Status

## Where I am

Phase 3 of 4 (End to end and unattended), with phase 4 work added on 27 Nov: what is left on each key on both Measurements screens, and Compare models (one task to two to four models). Both are Verified, along with the six fixes left from 26 Nov. Chatur now uses TechieRag 1.2.1, which prices Claude 5.x calls. The Owner's test is one order of 24 parts and passes in full on the installed app. Chatur has been reinstalled.

## Next command to run

Claude Code:
```
(owner) set current_phase to Released after UAT — no agent command
```
OpenCode:
```
(owner) set current_phase to Released after UAT — no agent command
```
Why: every row in this phase's scope is terminal and handoff has run; waiting on the owner; working docs/Chatur-P3-Checklist.md.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 0 |
| Blocked | 0 |

- None in phase 3. Phase 4: REQ-UI-130 Implemented (asking a provider's balance address; no OpenRouter or DeepSeek key exists to test it live).

## Known blockers

- REQ-UI-130 needs a real OpenRouter or DeepSeek key (`balanceProvider` in the test settings) for its browser test; unit tests with a fake address pass.
- Phase 1, hand steps once per release: REQ-FN-001, REQ-FN-002, REQ-FN-022, REQ-FN-030, REQ-FN-048; REQ-FN-012 needs the Mac; REQ-NFR-006 a published nightly.
- REQ-FN-078 is proven against a stand-in for App Manager only: it keeps no roles or rules yet (AM-002).
- REQ-FN-083 and REQ-UI-069: a real window check needs the Mac or Windows verifier, a real signature the certificates.
- TF-052: the menu walk reads late chat-panel menus as "opens nothing"; a second walk passes.
- TF-054: TfLens has no gate value for a person's own mark; Chatur writes `owner-mark`.
- TR-RAG-010: a cut-off tool call is dropped silently; Chatur asks again with more room.
- Owner, before the next commit: remove the `[verify-seed]` check-ins of 15 Nov in Chatur's own history (TF-037).

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-11-22 | triage-and-fix | 94/94 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-23 | build-phase | 94/94 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-26 | triage-and-fix | 20 rows Verified across phases 1, 3, 4 | docs/Chatur-P4-Checklist.md#requirements-status |
| 2026-11-26 | handoff-phase | 94/94 Verified; ready for the owner's test | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-11-27 | build-phase | 94/94 Verified; 18 of 19 new and reopened rows Verified across phases 1, 2, 4 | docs/Chatur-P3-Checklist.md#requirements-status |

## Library feedback summary

- AppManager: 1 open · 1 closed — docs/Chatur-AppManager-Feedback.md
- TechieFlow: 3 open · 18 fixed upstream, not yet re-checked (TF-008, TF-010, TF-013 …) · 28 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 1 open · 3 fixed upstream, not yet re-checked (TR-RAG-008, TR-RAG-009, TR-RAG-006) · 7 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 5 open · 24 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-22: 68 findings, see the checklist Remarks.

## Deferred / future

- No BRD row: Settings ▸ Account content, hiding output, merging or creating a branch.
- Worktrees: the mockup's "Give the next run its own copy" box and its size line are not built; nothing stands behind them yet.
- Chat sessions carry no cost in `sessions.jsonl`, so Measurements reads "no price" for them; the key panel counts their price.
- The run queue moves only while its tab is open.
- Data kept before 17 Nov stays in the install's own chatur.db, shown to no account.
