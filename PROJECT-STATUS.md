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
     checklist Remarks; the rest lives in the telemetry streams.
     Next command to run: exactly two code blocks, one line each, labelled Claude Code then OpenCode,
     so either can be copied on its own. No prose about the technical work. -->

---
project: Chatur
last_updated: 2026-10-21
current_phase: Phase 1 of 4 (Workbench) · Verify — blocked at Sign in; 11/98 verified
last_verified_build: PASS
last_verified_date: 2026-10-21
---

# Chatur — Status

## Where I am

Phase 1 verify ran on the Mac Catalyst head. Build is green and the app boots. The 19 Oct code fixes are in. Only Sign-in rows and NFR unit tests verified; every other screen is unreachable because the test-user password and App Manager key are not on this Mac. Phases 2–4 were not graded due to the same blocker plus a framework crash.

## Next command to run

Claude Code:
```
/TechieFlow:agents:flow-master *verify-phase Chatur --scope all --head maccatalyst
```
OpenCode:
```
/flow-master *verify-phase Chatur --scope all --head maccatalyst
```
Run after adding `Chatur:TestUsers:Owner` and `AppManager:ApiKey` to this Mac's dotnet user secrets.

## Open requirements

| Status | Count |
|---|---|
| Not Started | 0 |
| In Progress | 0 |
| Implemented | 0 |
| Needs re-verify | 87 |
| Blocked | 0 |

- [ ] REQ-FN-004 — Register an account (Needs re-verify)
- [ ] REQ-FN-005 — A weak password is caught before it is sent (Needs re-verify)
- [ ] REQ-FN-006 — The choice survives a restart (Needs re-verify)
- [ ] REQ-FN-007 — What the selected project is (Needs re-verify)
- [ ] REQ-FN-008 — Straight into the work (Needs re-verify)
- [ ] REQ-FN-009 — Sign out (Needs re-verify)
- [ ] REQ-FN-010 — The last target is offered first (Needs re-verify)
- [ ] REQ-FN-011 — Every tool, with the version found (Needs re-verify)
- [ ] REQ-FN-012 — Homebrew tools are found on a Mac (Needs re-verify)
- [ ] REQ-FN-013 — A newer build is offered (Needs re-verify)
- (77 more open rows in docs/Chatur-Checklist.md)

## Known blockers

- The UsageGuide test-user password for `chatur-owner@techierathore.com` and `AppManager:ApiKey` are not in this Mac's dotnet user secrets (`dotnet user-secrets list` is empty). Without them the Mac Catalyst app stays on Sign in.
- `tf-verify-verdict.py` crashes with `UnboundLocalError: bad` when the first graded screen is unreachable on a Blazor Hybrid Mac head. Filed as TF-015.
- Unit test `RunVerdictTests.TestsThatRunPastTheirLimitCannotVerifyAnything` (REQ-UI-048) fails consistently on this Mac (not flaky) and makes `tf-doc-tests.sh` fail; it is unrelated to the 19 Oct findings and is named for triage.

## Verification log

Last five passes; older passes live in `docs/metrics/gates.jsonl`.

| Date | Phase | Result | Status table |
|---|---|---|---|
| 2026-10-13 | fix-issues | 47/47 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-10-19 | verify-phase | 28/47 Verified | docs/Chatur-P3-Checklist.md#requirements-status |
| 2026-10-20 | log-miss | 38/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-20 | fix-issues | 38/98 Verified | docs/Chatur-Checklist.md#requirements-status |
| 2026-10-21 | verify-phase --scope all --head maccatalyst | 11/98 Verified (phase 1 only) | docs/Chatur-Checklist.md#requirements-status |

## Library feedback summary

- TechieFlow: 3 open (TF-013, TF-014, TF-015) · 2 fixed upstream, not yet re-checked (TF-008, TF-010) · 10 closed — docs/Chatur-TechieFlow-Feedback.md
- TechieRag: 0 open · 1 fixed upstream, not yet re-checked (TR-RAG-006) · 5 closed — docs/Chatur-TechieRag-Feedback.md
- TrBlazeUI: 0 open · 21 closed — docs/Chatur-TrBlazeUI-Feedback.md

## Standards compliance

- Last check 2026-10-21: 124 warnings, 0 failures; see the checklist Remarks.

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
