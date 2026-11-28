# Chatur — Checklist (phase 3)

| | |
|---|---|
| App | Chatur |
| Size | Medium |
| Phase | 3 of 4 |

## Goal

Phase 3, the unattended run: one command takes a brief and runs the whole chain to ready-for-acceptance, starting each agent as it is needed. A run survives a usage limit, a stall and a crash, can be queued and resumed, and never reports complete while a requirement is open. Afterwards there is a report to read. Since 2026-10-08 (owner decision, option A) every process step that is not a check or an owner stop is handed to the agent owning its process, and each process's checks judge the result on disk (REQ-FN-084 to REQ-FN-102, REQ-UI-070, REQ-UI-071). Since 2026-10-09 a "writes documents" right covers docs/ only (REQ-FN-103 to REQ-FN-105, REQ-UI-072). Since 2026-11-10: estimates (REQ-UI-083 to 090, REQ-FN-107 to 116).

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-FN-061 | A brief in, the whole chain run | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-061 a second run is refused while one is going on the`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-061) |
| REQ-UI-055 | Each role starts when it is needed | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-055 the planned chain lists the eight processes and t`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-055) |
| REQ-FN-062 | It stops at the acceptance test | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-062 the finished chain reads ready for acceptance and`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-062) |
| REQ-UI-056 | What is happening now | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-056 a run that is going shows the agent, the process,`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-056) |
| REQ-FN-063 | Step by step is untouched | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-063 with no run going every process can still be star`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-063) |
| REQ-FN-064 | A limit is waited out | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-064 a rate limit from a provider is recognised as a u`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-064) |
| REQ-FN-065 | A stall or a crash is survived | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-065 a stalled step is stopped, retried and recorded a`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-065) |
| REQ-FN-066 | Never complete while work is open | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-066 a run that has not finished its chain cannot be m`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-066) |
| REQ-FN-067 | Queue a run | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-067 a brief shorter than the minimum is refused`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-067) |
| REQ-UI-057 | One at a time | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-057 the next run waits while one is going and starts `; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-057) |
| REQ-FN-068 | Pause and start again | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-068 a paused run resumes the same run it was paused o`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-068) |
| REQ-FN-069 | Take one out | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-069 a removed waiting run leaves the queue and is nev`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-069) |
| REQ-FN-070 | A resumed run picks the weakest work first | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-070 a resumed run continues from the open requirement`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-070) |
| REQ-UI-058 | Every requirement and its verdict | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-058 the report of a finished run reads verdicts, wait`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-058) |
| REQ-UI-059 | Every miss, and whose gap it was | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-059 the report lists each miss with what it was and w`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-059) |
| REQ-UI-060 | What it cost | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-060 a run with nothing recorded reports cost as not r`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-060) |
| REQ-UI-061 | Read an earlier one | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-061 earlier runs are listed newest first and a run of`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-061) |
| REQ-FN-071 | A small model picks the model | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-071 the owner turns automatic routing on and it is re`; Settings / routing renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 4 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-071) |
| REQ-FN-072 | The owner can take it back | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-072 the owner turns automatic routing off and the tie`; Settings / routing renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 4 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-072) |
| REQ-FN-084 | A step is handed to its agent | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-084 a project with only a step session still gets the`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-084) |
| REQ-FN-085 | The agent keeps to its rights | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-085 a tool the agent's rights withhold is refused and`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-085) |
| REQ-FN-086 | The agent works on its own tier | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-086 the handed step names the model routed to its age`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-086) |
| REQ-FN-087 | The step waits for the agent and records what it did | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-087 the step ends only when the agent finishes, recor`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-087) |
| REQ-FN-088 | The checks judge what is on disk | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-088 a failed check stops the run at that step with th`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-088) |
| REQ-FN-089 | A real usage limit is waited out | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-089 a handed step's usage limit is waited out, the st`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-089) |
| REQ-FN-090 | Stop ends the agent's work | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-090 stop during a handed step ends the agent's work a`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-090) |
| REQ-FN-091 | An unattended step does not wait on approvals | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-091 an unattended handed step writes without approval`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-091) |
| REQ-UI-070 | Process run shows the agent at work | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-070 Process run shows the agent, its model, and the f`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-070) |
| REQ-UI-071 | The agent's output in the conversation | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-071 the agent's messages and tool calls appear in the`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-071) |
| REQ-FN-100 | day1-greenfield is done when its documents are | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-100 a missing brief fails the check`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-100) |
| REQ-FN-092 | split-brd is done when every requirement is in one phase | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-092 a one-phase project passes on its one BRD and che`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-092) |
| REQ-FN-093 | build-phase is done when the open rows are built | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-093 build-phase passes only when every open row is Im`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-093) |
| REQ-FN-094 | fix-issues is done when every issue is fixed | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-094 fix-issues passes only when no row is left FAIL o`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-094) |
| REQ-FN-095 | handoff-phase is done when the phase is closed in the documents | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-095 handoff-phase passes only when no row is open and`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-095) |
| REQ-FN-096 | refresh-status is done when the status matches the checklists | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-096 refresh-status passes only when the status writte`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-096) |
| REQ-FN-101 | deploy-checklist is done when the release list is whole | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-101 deploy-checklist passes only when the list writte`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-101) |
| REQ-FN-102 | mockups is done when every screen is drawn and linked | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-102 mockups passes only when every screen is drawn, a`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-102) |
| REQ-FN-097 | amend-docs is done when the documents agree | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-097 amend-docs passes only when touched documents cha`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-097) |
| REQ-FN-098 | triage-issues is done when every report has a row | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-098 triage-issues passes only when every report has a`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-098) |
| REQ-FN-099 | metrics-report is done when the report matches the records | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-099 metrics-report passes only when the report's tota`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-099) |
| REQ-FN-103 | A right to write documents | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-103 a role with only write-docs may write a new folde`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-103) |
| REQ-FN-104 | The Analyst, the Architect and the Verifier hold it | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-104 every source-file right is as before(aRole: "arch`; Settings / agents renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-104) |
| REQ-UI-072 | The right is shown where rights are listed | Verified | 100% | 2026-11-22 verify: PASS — test `REQ-UI-072 Settings > Agents lists "Writes documents" among `; Settings / agents renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-072) |
| REQ-FN-105 | A handed step writes its documents but not source | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-105 a handed step writes its documents but a source w`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-105) |
| REQ-UI-073 | The menu bar's menus do their jobs | Implemented | 100% | [REQ-UI-073] 2026-11-27 build-phase (Mac): Tools ▸ Settings shows ⌘, on a Mac (it read Ctrl+,); the menu spec expects ⌘, on a Mac; the headless Mac walk of every menu, light and dark, passes (tests/.artifacts/mac-walk/results.json); needs a re-verify | [d](#d-req-ui-073) |
| REQ-UI-074 | Close the conversation and bring it back | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-074 closing the conversation gives the file view the `; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-074) |
| REQ-FN-106 | Settings and the other pages open from the main window | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-106 REQ-FN-011 REQ-UI-017 REQ-UI-018 REQ-FN-012 REQ-F`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-fn-106) |
| REQ-NFR-007 | The app icon is the approved Chatur mark | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-NFR-007 icon colour is brand amber and splash colour sta` | [view](#d-req-nfr-007) |
| REQ-NFR-008 | The splash screen is the approved glyph | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-NFR-008 icon and splash SVGs are byte-identical to the b` | [view](#d-req-nfr-008) |
| REQ-UI-078 | The mark sits beside the wordmark | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-078 the mark is the white glyph on a 20 px amber squa`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-078) |
| REQ-NFR-009 | The web harness carries the icon, made by a script | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-NFR-009 App.razor links favicon.ico, icon-192.png and ic` | [view](#d-req-nfr-009) |
| REQ-UI-079 | Type @ to attach a project file in the chat | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-079 typing @ lists the project files, typing narrows `; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-079) |
| REQ-UI-080 | Controls keep the mockup's inner spacing | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-080 a focused field on New project keeps its text ins`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-080) |
| REQ-UI-081 | Labels carry no trailing dots | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-081 no menu, button, label or placeholder on any scre`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-081) |
| REQ-UI-082 | Every button does something | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-082 the toolbar Find, once always greyed out, opens F`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-082) |
| REQ-NFR-010 | Tests never touch the owner's database | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-NFR-010 other web view arguments do not mark a test run` | [d](#d-req-nfr-010) |
| REQ-UI-083 | An estimate before a process starts | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-083 REQ-UI-089 Start on build-phase, verify-phase and`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-083) |
| REQ-UI-084 | An estimate before an end-to-end run starts | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-084 an end-to-end run writes its estimate at the buil`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-084) |
| REQ-UI-085 | An estimate on a queued run | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-085 a queued run that starts carries the estimate it `; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-085) |
| REQ-FN-107 | Worked out from this project's own runs, row kind by row kind | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-107 quartiles interpolate between neighbours and roun`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-107) |
| REQ-FN-108 | Sister projects fill in, and the estimate says so | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-086 REQ-FN-108 a sister project is listed with its ru`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-108) |
| REQ-UI-086 | The owner names the sister projects | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-086 REQ-FN-108 a sister project is listed with its ru`; Settings / measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-086) |
| REQ-FN-109 | No precedent, never a number | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-109 F3 after handoff reads no precedent, basis none o`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-109) |
| REQ-FN-110 | No model judgement in the estimate | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-110 F1 estimated twice with no provider reachable mat`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-110) |
| REQ-FN-111 | Made for the model and tier that will run the step | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-111 the estimate names the routed model claude-opus-5`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-111) |
| REQ-FN-112 | Changing the tier changes the estimate before the start | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-112 moving build to tier 2 changes the next estimate `; Settings / routing renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 4 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-112) |
| REQ-UI-087 | Process run shows the actual so far beside the estimate | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-087 a running process records the tokens each model c`; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-087) |
| REQ-UI-088 | The run report sets estimate beside actual | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-088 6,000 and 12,000 read within, 12,001 above, 5,999`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-088) |
| REQ-FN-113 | At handoff, the phase against its estimate | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-113 handoff sets the phase's batch beside F6's runs: `; Process run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-113) |
| REQ-FN-114 | Estimates are written in TechieFlow's own shape | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-114 confirming F1 writes REQ-FN-301's build record eq`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-114) |
| REQ-FN-115 | An estimate is never mixed with a measurement | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-115 confirming an estimate leaves runs.jsonl, the che`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-115) |
| REQ-UI-089 | The owner's own testing reads "not measured" | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-089 the owner's own testing reads not measured and is`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 13 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-089) |
| REQ-UI-090 | A project with no run records says "no estimate yet" | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-108 REQ-UI-090 a project with no runs of its own is e`; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-090) |
| REQ-FN-116 | Runs that cannot be divided among rows are left out | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-116 F4 leaves out undividable runs, equals F1 over 5 `; Processes renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-116) |
| REQ-UI-091 | What one account makes or opens, another never sees | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-091 REQ-UI-008 REQ-FN-006 a second account on the ins`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-091) |
| REQ-FN-302 | A chat is never continued by another account | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-302 REQ-FN-008 a chat started by one account is never`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-302) |
| REQ-UI-092 | A provider and its key belong to the account that added it | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-092 REQ-FN-017 one account's provider key is never of`; Settings / providers renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 4 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-092) |
| REQ-FN-303 | Run history belongs to the account that ran it | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-303 run history belongs to the account that ran it`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 9 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-303) |
| REQ-UI-093 | Data kept before accounts belongs to nobody | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-093 data kept before accounts belongs to nobody and i`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-093) |
| REQ-UI-094 | Open a file from Start | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-094 Open a file opens the folder that holds it, with `; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-094) |
| REQ-UI-095 | Signing in as another account leaves nothing of the last one on screen | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-095 another account signing in drops the open project`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-095) |
| REQ-UI-096 | A folder Chatur cannot read never stops it | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-096 a folder that cannot be read is marked not readab`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-096) |
| REQ-UI-097 | A last project that cannot open leaves Chatur on Start, saying why | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-097 a last project that cannot be read is not reopene`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-097) |
| REQ-UI-098 | Each way in has an icon that says what it does | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-098 each way in on Start shows a centred icon for its`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-098) |
| REQ-UI-099 | The project list closes back to where it was opened from | Verified | 100% | 2026-11-22 verify: PASS — test `REQ-UI-099 closing the project list with its close button or`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-099) |
| REQ-UI-100 | Open a solution browses for a solution file | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-100 Browse in Open a solution asks for a solution fil`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-100) |
| REQ-UI-101 | One branch picker, at the foot of the files | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-101 the branch picker lists local and remote branches`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-101) |
| REQ-FN-304 | Uncommitted changes are asked about before a branch switch | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-304 putting changes aside stashes them and switches t`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-304) |
| REQ-UI-102 | The agent picker shows one chosen agent, ticked | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-102 the agent the closed picker names carries the tic`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-102) |
| REQ-UI-103 | Hidden files stay hidden until the person asks | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-103 hidden files stay hidden until the account shows `; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-103) |
| REQ-UI-104 | Stop shows only while something runs | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-104 the toolbar shows no Stop while nothing builds or`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-104) |
| REQ-UI-105 | A solution's runnable projects are its run targets | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-UI-105 an .slnx solution offers its runnable projects`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-105) |
| REQ-UI-106 | The project to run is set from three places and remembered | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-UI-106 setting the project to run from the picker, the P`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-106) |
| REQ-FN-305 | File says what it closes | Verified | 100% | 2026-11-21 verify: PASS — test `REQ-FN-305 File says Close solution with a solution open and`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 14 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-305) |

**Status values:** `Not Started` · `In Progress` · `Implemented` · `Verified` · `Done (pre-existing)` · `Needs re-verify` · `PARTIAL` · `FAIL` · `Blocked` · `Owner-UAT` · `N/A`.

## Page: End-to-end run (`/auto`)

<a id="d-req-fn-061"></a>
- **REQ-FN-061** — A brief in, the whole chain run. *BRD:* BRD-118 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When the owner starts an end-to-end run with a brief on End-to-end run, then Chatur runs the chain of processes without being told each one.

<a id="d-req-ui-055"></a>
- **REQ-UI-055** — Each role starts when it is needed. *BRD:* BRD-119 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When the chain reaches work another role owns on End-to-end run, then that role is started and the screen names it.

<a id="d-req-fn-062"></a>
- **REQ-FN-062** — It stops at the acceptance test. *BRD:* BRD-120 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When the chain finishes on End-to-end run, then the run's state reads ready for the owner's acceptance test, and nothing further is run.

<a id="d-req-ui-056"></a>
- **REQ-UI-056** — What is happening now. *BRD:* BRD-121 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When End-to-end run opens with a run going, then it shows the agent working, the process, the step and the requirement.

<a id="d-req-fn-063"></a>
- **REQ-FN-063** — Step by step is untouched. *BRD:* BRD-122 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When no run is going on End-to-end run, then every process can still be started on its own from Processes.

<a id="d-req-fn-064"></a>
- **REQ-FN-064** — A limit is waited out. *BRD:* BRD-123 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When a model reports a usage limit on End-to-end run, then the run pauses, waits and carries on by itself, and the wait is recorded.

<a id="d-req-fn-065"></a>
- **REQ-FN-065** — A stall or a crash is survived. *BRD:* BRD-124 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When a step stalls or the work crashes on End-to-end run, then Chatur retries it, records the retry, and carries on or stops with a reason.

<a id="d-req-fn-066"></a>
- **REQ-FN-066** — Never complete while work is open. *BRD:* BRD-125 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When any requirement of the run is still open, then the run cannot be marked complete.

<a id="d-req-fn-089"></a>
- **REQ-FN-089** — A real usage limit is waited out. *BRD:* BRD-164 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When a handed step's model reports a usage limit on End-to-end run, then the run waits, resumes that step by itself, and records the wait against it.

<a id="d-req-fn-091"></a>
- **REQ-FN-091** — An unattended step does not wait on approvals. *BRD:* BRD-166 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When a handed step runs on End-to-end run, then the agent's changes are written without waiting for approval, while its rights and the source-control refusal still apply.

<a id="d-req-ui-084"></a>
- **REQ-UI-084** — An estimate before an end-to-end run starts. *BRD:* BRD-193 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When the owner fills in a brief for a project holding fixture F1 on End-to-end run, then the estimate shows before Start, each line naming its model and tier.

<a id="d-req-fn-303"></a>
- **REQ-FN-303** — Run history belongs to the account that ran it. *BRD:* BRD-213 · *Mockup:* mockups/auto-run.html
  - *Acceptance:* When a second account opens End-to-end run, Run queue or Run report on an install where a first account has runs, then none of the first account's runs is listed.

## Page: Run queue (`/auto/queue`)

<a id="d-req-fn-067"></a>
- **REQ-FN-067** — Queue a run. *BRD:* BRD-126 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When the owner queues a run on Run queue, then it is listed with its project and its position in the queue.

<a id="d-req-ui-057"></a>
- **REQ-UI-057** — One at a time. *BRD:* BRD-127 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When a run is already going on Run queue, then the next one in the queue does not start until it has ended.

<a id="d-req-fn-068"></a>
- **REQ-FN-068** — Pause and start again. *BRD:* BRD-128 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When the owner pauses a run and resumes it on Run queue, then it carries on from the step it was paused on.

<a id="d-req-fn-069"></a>
- **REQ-FN-069** — Take one out. *BRD:* BRD-129 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When the owner removes a queued run on Run queue, then it leaves the queue and is not started.

<a id="d-req-fn-070"></a>
- **REQ-FN-070** — A resumed run picks the weakest work first. *BRD:* BRD-130 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When a run resumes after a stop on Run queue, then it continues from the open requirement furthest from verified.

<a id="d-req-ui-085"></a>
- **REQ-UI-085** — An estimate on a queued run. *BRD:* BRD-194 · *Mockup:* mockups/run-queue.html
  - *Acceptance:* When the owner queues a run for fixture F1 on Run queue, then its estimate shows before Queue it is pressed, and the waiting run keeps that estimate.

## Page: Run report (`/auto/report`)

<a id="d-req-ui-058"></a>
- **REQ-UI-058** — Every requirement and its verdict. *BRD:* BRD-131 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When a run has ended on Run report, then its report lists every requirement it touched with the verdict it was left at.

<a id="d-req-ui-059"></a>
- **REQ-UI-059** — Every miss, and whose gap it was. *BRD:* BRD-132 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When the run recorded misses on Run report, then the report lists each one with what it was and whose gap it was.

<a id="d-req-ui-060"></a>
- **REQ-UI-060** — What it cost. *BRD:* BRD-133 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When the report opens, then it shows the tokens, the models used and the time the run took.

<a id="d-req-ui-061"></a>
- **REQ-UI-061** — Read an earlier one. *BRD:* BRD-134 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When the owner picks an earlier run on Run report, then that run's report is shown instead of the newest.

<a id="d-req-ui-088"></a>
- **REQ-UI-088** — The run report sets estimate beside actual. *BRD:* BRD-203 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When a run estimated at 6,000/9,000/12,000 tokens ends on Run report, then the actual reads within at 6,000 and 12,000, above at 12,001, below at 5,999.

<a id="d-req-fn-115"></a>
- **REQ-FN-115** — An estimate is never mixed with a measurement. *BRD:* BRD-206 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When fixture F1 is estimated and the run ends on Run report, then runs.jsonl, the checklist and PROJECT-STATUS hold no estimate figure, and no screen shows a ratio.

<a id="d-req-ui-089"></a>
- **REQ-UI-089** — The owner's own testing reads "not measured". *BRD:* BRD-207 · *Mockup:* mockups/run-report.html
  - *Acceptance:* When any estimate table shows on Processes, End-to-end run, Run queue, Process run or Run report, then the owner's testing line reads "not measured", never a number.

## Page: Settings

<a id="d-req-fn-071"></a>
- **REQ-FN-071** — A small model picks the model. *BRD:* BRD-135 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When automatic routing is on, then each piece of work is given a model chosen by the small model inside TechieRag.

<a id="d-req-fn-072"></a>
- **REQ-FN-072** — The owner can take it back. *BRD:* BRD-136 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the owner turns automatic routing off on Settings, then the tiers and chains he set are used again.

<a id="d-req-fn-103"></a>
- **REQ-FN-103** — A right to write documents. *BRD:* BRD-180 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When an agent with only "writes documents" writes a path that resolves outside docs/, even through "..", on Process run, then the write is refused with the reason.

<a id="d-req-fn-104"></a>
- **REQ-FN-104** — The Analyst, the Architect and the Verifier hold it. *BRD:* BRD-181 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner opens each role on Settings, then the Analyst, Architect and Verifier hold "writes documents", the Flow master does not, and every source-file right is as before.

<a id="d-req-ui-072"></a>
- **REQ-UI-072** — The right is shown where rights are listed. *BRD:* BRD-182 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner opens a role on Settings or its Agent workspace, then "writes documents" is listed with the other rights, held or withheld.

<a id="d-req-ui-086"></a>
- **REQ-UI-086** — The owner names the sister projects. *BRD:* BRD-197 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When the owner adds a sister project on Settings, then it is listed with its run count, and a folder without docs/metrics/runs.jsonl is refused with the reason.

<a id="d-req-fn-112"></a>
- **REQ-FN-112** — Changing the tier changes the estimate before the start. *BRD:* BRD-201 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the owner moves build to tier 2 on Settings, then F5 reads 2,000/3,000/4,000 tokens "from all models" for claude-sonnet-5-5 without a restart; a started run keeps its own.

<a id="d-req-ui-092"></a>
- **REQ-UI-092** — A provider and its key belong to the account that added it. *BRD:* BRD-212 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When a second account opens Providers on Settings after a first account connected one, then the list is empty and the first account's key is never read for it.

## Page: Process run

<a id="d-req-fn-084"></a>
- **REQ-FN-084** — A step is handed to its agent. *BRD:* BRD-159 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a run reaches a work step on Process run, then a session starts with the process's agent, given the step's wording, the run and the project.

<a id="d-req-fn-085"></a>
- **REQ-FN-085** — The agent keeps to its rights. *BRD:* BRD-160 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a handed step's agent asks for a tool its rights withhold on Process run, then the tool is refused and the refusal is recorded on the step.

<a id="d-req-fn-086"></a>
- **REQ-FN-086** — The agent works on its own tier. *BRD:* BRD-161 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a handed step starts on Process run, then its agent's session uses the model routed to that agent's tier, and the step names that model.

<a id="d-req-fn-087"></a>
- **REQ-FN-087** — The step waits for the agent and records what it did. *BRD:* BRD-162 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a handed step's agent finishes on Process run, then the step ends only then, recording the files written, the commands run and the agent's final message.

<a id="d-req-fn-088"></a>
- **REQ-FN-088** — The checks judge what is on disk. *BRD:* BRD-163 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a check follows a handed step on Process run, then it judges the project's files on disk, and if it fails the run stops there with the reason.

<a id="d-req-fn-090"></a>
- **REQ-FN-090** — Stop ends the agent's work. *BRD:* BRD-165 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When the owner presses Stop during a handed step on Process run or End-to-end run, then the agent's session ends and the step reads stopped by the owner.

<a id="d-req-ui-070"></a>
- **REQ-UI-070** — Process run shows the agent at work. *BRD:* BRD-167 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When Process run opens on a handed step, then it shows the agent, its model, and the files written, commands run and refusals so far.

<a id="d-req-ui-071"></a>
- **REQ-UI-071** — The agent's output in the conversation. *BRD:* BRD-168 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a handed step is being worked on Process run, then the agent's messages and tool calls appear in the conversation panel as they happen, under that agent's name.

<a id="d-req-fn-100"></a>
- **REQ-FN-100** — day1-greenfield is done when its documents are. *BRD:* BRD-169 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When day1-greenfield's checks run on Process run, then they pass only when the brief, BRD, Architecture, UI design, mockups and checklist exist and pass the document rules.

<a id="d-req-fn-092"></a>
- **REQ-FN-092** — split-brd is done when every requirement is in one phase. *BRD:* BRD-170 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When split-brd's checks run on Process run, then they pass only when every BRD id lands in exactly one phase and each phase has its BRD and checklist.

<a id="d-req-fn-093"></a>
- **REQ-FN-093** — build-phase is done when the open rows are built. *BRD:* BRD-171 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When build-phase's checks run on Process run, then they pass only when Chatur's own build has no warning, the tests pass and every open row is Implemented.

<a id="d-req-fn-094"></a>
- **REQ-FN-094** — fix-issues is done when every issue is fixed. *BRD:* BRD-172 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When fix-issues's checks run on Process run, then they pass only when Chatur's own build has no warning, the failed rows' tests pass and each is Implemented again.

<a id="d-req-fn-095"></a>
- **REQ-FN-095** — handoff-phase is done when the phase is closed in the documents. *BRD:* BRD-173 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When handoff-phase's checks run on Process run, then they pass only when no row of the phase is open and the status and phases files record the hand-off.

<a id="d-req-fn-096"></a>
- **REQ-FN-096** — refresh-status is done when the status matches the checklists. *BRD:* BRD-174 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When refresh-status's checks run on Process run, then they pass only when the status document written in the run counts each status as the checklists do.

<a id="d-req-fn-101"></a>
- **REQ-FN-101** — deploy-checklist is done when the release list is whole. *BRD:* BRD-175 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When deploy-checklist's checks run on Process run, then they pass only when the deployment checklist written in the run has every section, and every item names who does it.

<a id="d-req-fn-102"></a>
- **REQ-FN-102** — mockups is done when every screen is drawn and linked. *BRD:* BRD-176 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When mockups's checks run on Process run, then they pass only when every screen has an anchored mockup and a UI design section, and every UI row's mockup exists.

<a id="d-req-fn-097"></a>
- **REQ-FN-097** — amend-docs is done when the documents agree. *BRD:* BRD-177 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When amend-docs's checks run on Process run, then they pass only when touched documents changed, no BRD id was renumbered or deleted, and each new item has a checklist row.

<a id="d-req-fn-098"></a>
- **REQ-FN-098** — triage-issues is done when every report has a row. *BRD:* BRD-178 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When triage-issues's checks run on Process run, then they pass only when every report is a reopened or new checklist row and nothing under src or tests changed.

<a id="d-req-fn-099"></a>
- **REQ-FN-099** — metrics-report is done when the report matches the records. *BRD:* BRD-179 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When metrics-report's checks run on Process run, then they pass only when the report written in the run has totals equal to those recomputed from the run records.

<a id="d-req-fn-105"></a>
- **REQ-FN-105** — A handed step writes its documents but not source. *BRD:* BRD-183 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When an agent with only "writes documents" works a handed step on Process run, then its docs/ writes succeed and a source write is refused and recorded on the step.

<a id="d-req-ui-087"></a>
- **REQ-UI-087** — Process run shows the actual so far beside the estimate. *BRD:* BRD-202 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a build-phase run on F1's three rows has spent 4,500 tokens and 400 s on Process run, then those show beside 6,000/9,000/12,000 and 720/900/1,080 s, uncombined.

<a id="d-req-fn-113"></a>
- **REQ-FN-113** — At handoff, the phase against its estimate. *BRD:* BRD-204 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When handoff-phase runs for fixture F6 on Process run, then build shows 6,000/9,000/12,000 beside 10,500 tokens, apportioned, within, and "1 run named no row" stands apart.

## Workbench

- <a id="d-req-ui-073"></a> **REQ-UI-073** — The menu bar's menus do their jobs. *BRD:* BRD-184 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens File, Edit, View or Help on Workbench, then every drawn item works and none says 'Nothing to do yet'.

- <a id="d-req-ui-074"></a> **REQ-UI-074** — Close the conversation and bring it back. *BRD:* BRD-185 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner closes the conversation panel on Workbench, then the file view takes the window, and View ▸ Chat brings the conversation back.

- <a id="d-req-fn-106"></a> **REQ-FN-106** — Settings and the other pages open from the main window. *BRD:* BRD-186 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner picks Settings, a settings page, Prerequisites or Repository from the main window's menus, then the secondary window opens on that page.

- <a id="d-req-ui-079"></a> **REQ-UI-079** (BRD-230) Type @ to attach a project file in the chat · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner types @ in the chat box on Workbench, then the project's files are listed, narrowing as they type, and a pick puts its path in the message.

- <a id="d-req-ui-080"></a> **REQ-UI-080** (BRD-231) Controls keep the mockup's inner spacing · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens any dialog or page, then every input, button and list row keeps the mockup's inner padding and no text or control touches its border.

- <a id="d-req-ui-081"></a> **REQ-UI-081** (BRD-232) Labels carry no trailing dots · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens any menu, button or label in Chatur, then its text ends without three dots.

- <a id="d-req-ui-082"></a> **REQ-UI-082** (BRD-233) Every button does something · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner presses any enabled button on any screen, then it does its job; no button exists only for looks.

- <a id="d-req-nfr-010"></a> **REQ-NFR-010** (BRD-234) Tests never touch the owner's database
  - *Acceptance:* When the desktop or web tests run, then the app keeps its database under tests/.artifacts and refuses the owner's application-data database.

<a id="d-req-fn-302"></a>
- **REQ-FN-302** — A chat is never continued by another account. *BRD:* BRD-211 · *Mockup:* mockups/main.html
  - *Acceptance:* When a second account opens on Workbench a folder a first account chatted in, then its conversation is empty and never says "Continuing after".

<a id="d-req-ui-095"></a>
- **REQ-UI-095** — Signing in as another account leaves nothing of the last one on screen. *BRD:* BRD-216 · *Mockup:* mockups/main.html
  - *Acceptance:* When one account signs out in a window and another signs in there, then no project, open file, chat or output of the first account is shown.

<a id="d-req-ui-096"></a>
- **REQ-UI-096** — A folder Chatur cannot read never stops it. *BRD:* BRD-217 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person opens on Workbench a folder holding one Windows will not list, then that folder shows as not readable and every other entry still shows.

<a id="d-req-ui-101"></a>
- **REQ-UI-101** — One branch picker, at the foot of the files. *BRD:* BRD-222 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person opens the branch at the foot of Files on Workbench, then local and remote branches are listed and choosing one switches to it.

<a id="d-req-fn-304"></a>
- **REQ-FN-304** — Uncommitted changes are asked about before a branch switch. *BRD:* BRD-223 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person picks another branch on Workbench with uncommitted changes, then Chatur first offers to bring them along, put them aside, or stay.

<a id="d-req-ui-102"></a>
- **REQ-UI-102** — The agent picker shows one chosen agent, ticked. *BRD:* BRD-224 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person opens the agent picker on Workbench, then the agent the closed picker names carries the tick in the list, and no entry says "suggested".

<a id="d-req-ui-103"></a>
- **REQ-UI-103** — Hidden files stay hidden until the person asks. *BRD:* BRD-225 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project opens on Workbench, then dot-named files and .vs are hidden until View ▸ Show hidden files, which the account keeps.

<a id="d-req-ui-104"></a>
- **REQ-UI-104** — Stop shows only while something runs. *BRD:* BRD-226 · *Mockup:* mockups/main.html
  - *Acceptance:* When nothing is building or running on Workbench, then the toolbar shows no Stop; while a build or run goes, Stop shows and ends it.

<a id="d-req-ui-105"></a>
- **REQ-UI-105** — A solution's runnable projects are its run targets. *BRD:* BRD-227 · *Mockup:* mockups/main.html
  - *Acceptance:* When a solution with .NET projects opens on Workbench, then the target picker lists its runnable projects, and choosing one and pressing Run runs that project.

<a id="d-req-ui-106"></a>
- **REQ-UI-106** — The project to run is set from three places and remembered. *BRD:* BRD-228 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person sets the project to run from the picker, Project menu or Files on Workbench, then that account is offered it next time.

<a id="d-req-fn-305"></a>
- **REQ-FN-305** — File says what it closes. *BRD:* BRD-229 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person opens File on Workbench, then it says Close solution with a solution open and Close folder with a folder open, never Close project.

## Page: Start

<a id="d-req-nfr-007"></a>
- **REQ-NFR-007** — The app icon is the approved Chatur mark. *BRD:* BRD-188 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner starts the built Windows app on Start, then the taskbar and the title bar show the amber Chatur tile with its white glyph.

<a id="d-req-nfr-008"></a>
- **REQ-NFR-008** — The splash screen is the approved glyph. *BRD:* BRD-189 · *Mockup:* mockups/start.html
  - *Acceptance:* When the three icon and splash SVGs under Resources are compared with the brand folder's files, then each is byte-identical and the splash colour stays `#1E1B16`.

<a id="d-req-ui-078"></a>
- **REQ-UI-078** — The mark sits beside the wordmark. *BRD:* BRD-190 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner opens Start, a bare page, the main or the secondary window, then the logo is the white glyph on a 20 px amber square.

<a id="d-req-nfr-009"></a>
- **REQ-NFR-009** — The web harness carries the icon, made by a script. *BRD:* BRD-191 · *Mockup:* mockups/start.html
  - *Acceptance:* When the web harness page loads, then it links `favicon.ico` (32 px), `icon-192.png` and `icon-512.png`, each loads, and each is what the repository's script under `scripts/` renders from `Chatur.png`.

e glyph.

<a id="d-req-nfr-008"></a>
- **REQ-NFR-008** — The splash screen is the approved glyph. *BRD:* BRD-189 · *Mockup:* mockups/start.html
  - *Acceptance:* When the three icon and splash SVGs under Resources are compared with the brand folder's files, then each is byte-identical and the splash colour stays `#1E1B16`.

<a id="d-req-ui-078"></a>
- **REQ-UI-078** — The mark sits beside the wordmark. *BRD:* BRD-190 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner opens Start, a bare page, the main or the secondary window, then the logo is the white glyph on a 20 px amber square.

<a id="d-req-nfr-009"></a>
- **REQ-NFR-009** — The web harness carries the icon, made by a script. *BRD:* BRD-191 · *Mockup:* mockups/start.html
  - *Acceptance:* When the web harness page loads, then it links `favicon.ico` (32 px), `icon-192.png` and `icon-512.png`, each loads, and each is what the repository's script under `scripts/` renders from `Chatur.png`.

<a id="d-req-ui-091"></a>
- **REQ-UI-091** — What one account makes or opens, another never sees. *BRD:* BRD-210 · *Mockup:* mockups/start-empty.html
  - *Acceptance:* When a second account signs in on Start where a first account opened projects and chatted, then Start is empty and Switch project lists none of the first's.

<a id="d-req-ui-093"></a>
- **REQ-UI-093** — Data kept before accounts belongs to nobody. *BRD:* BRD-214 · *Mockup:* mockups/start-empty.html
  - *Acceptance:* When an account signs in on Start where projects, chats and runs were kept before accounts, then it is shown none of them and none is deleted.

<a id="d-req-ui-094"></a>
- **REQ-UI-094** — Open a file from Start. *BRD:* BRD-215 · *Mockup:* mockups/start-empty.html
  - *Acceptance:* When the person chooses Open a file on Start and gives a file, then the Workbench opens on the folder holding it, with that file open in a tab.

<a id="d-req-ui-097"></a>
- **REQ-UI-097** — A last project that cannot open leaves Chatur on Start, saying why. *BRD:* BRD-218 · *Mockup:* mockups/start.html
  - *Acceptance:* When the last project cannot open as Chatur starts on Start, then one plain line says which project and why, and nothing stops.

<a id="d-req-ui-098"></a>
- **REQ-UI-098** — Each way in has an icon that says what it does. *BRD:* BRD-219 · *Mockup:* mockups/start.html
  - *Acceptance:* When the person looks at the ways in on Start, then each tile shows a centred icon for its action: branch, solution, folder, file, plus.

<a id="d-req-ui-099"></a>
- **REQ-UI-099** — The project list closes back to where it was opened from. *BRD:* BRD-220 · *Mockup:* mockups/start.html
  - *Acceptance:* When the person opens the project list from File ▸ Recent projects and presses its close button or Escape on Start, then the Workbench they came from shows again, unchanged.

<a id="d-req-ui-100"></a>
- **REQ-UI-100** — Open a solution browses for a solution file. *BRD:* BRD-221 · *Mockup:* mockups/start.html
  - *Acceptance:* When the person presses Browse in Open a solution on Start, then a file picker opens that shows .sln and .slnx files, and the file chosen fills the path.

## Page: Processes

<a id="d-req-ui-083"></a>
- **REQ-UI-083** — An estimate before a process starts. *BRD:* BRD-192 · *Mockup:* mockups/processes.html
  - *Acceptance:* When the owner presses Start on build-phase, verify-phase or fix-issues for fixture F1 on Processes, then every row and the phase total show, and nothing runs until confirmed.

<a id="d-req-fn-107"></a>
- **REQ-FN-107** — Worked out from this project's own runs, row kind by row kind. *BRD:* BRD-195 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F1 is estimated on Processes, then each REQ-FN row's build reads 2,000/3,000/4,000 tokens and 240/300/360 s over 5 runs, and the three-row total 6,000/9,000/12,000 tokens.

<a id="d-req-fn-108"></a>
- **REQ-FN-108** — Sister projects fill in, and the estimate says so. *BRD:* BRD-196 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F2 is estimated on Processes, then a REQ-UI row's build reads 1,750/2,500/3,750 tokens and 175/250/350 s, basis family over 4 runs, naming S1.

<a id="d-req-fn-109"></a>
- **REQ-FN-109** — No precedent, never a number. *BRD:* BRD-198 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F3 is estimated on Processes, then after handoff reads "no precedent", basis none over 2 runs, with no figure shown or written.

<a id="d-req-fn-110"></a>
- **REQ-FN-110** — No model judgement in the estimate. *BRD:* BRD-199 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F1 is estimated twice on Processes with no model provider reachable, then both results match to the last digit and no model call is made.

<a id="d-req-fn-111"></a>
- **REQ-FN-111** — Made for the model and tier that will run the step. *BRD:* BRD-200 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F5 is estimated with build on tier 1, then build reads 3,000/4,000/4,500 tokens and 330/360/405 s over 3 runs, naming claude-opus-5-5 and tier 1.

<a id="d-req-fn-114"></a>
- **REQ-FN-114** — Estimates are written in TechieFlow's own shape. *BRD:* BRD-205 · *Mockup:* mockups/processes.html
  - *Acceptance:* When the owner confirms a start for fixture F1 on Processes, then REQ-FN-301's build record in estimates.jsonl equals the Metrics Schema example, ts aside.

<a id="d-req-ui-090"></a>
- **REQ-UI-090** — A project with no run records says "no estimate yet". *BRD:* BRD-208 · *Mockup:* mockups/processes.html
  - *Acceptance:* When a process is started on Processes for a project with no usable run anywhere, then it reads "No estimate yet", Start works, and no error is logged.

<a id="d-req-fn-116"></a>
- **REQ-FN-116** — Runs that cannot be divided among rows are left out. *BRD:* BRD-209 · *Mockup:* mockups/processes.html
  - *Acceptance:* When fixture F4 is estimated on Processes, then the figures equal F1's over the same 5 runs, and the batch carries basis_note "retro".


