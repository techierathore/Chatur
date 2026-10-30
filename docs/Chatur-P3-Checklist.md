# Chatur — Checklist (phase 3)

| | |
|---|---|
| App | Chatur |
| Size | Medium |
| Phase | 3 of 4 |

## Goal

Phase 3, the unattended run: one command takes a brief and runs the whole chain to ready-for-acceptance, starting each agent as it is needed. A run survives a usage limit, a stall and a crash, can be queued and resumed, and never reports complete while a requirement is open. Afterwards there is a report to read. Since 2026-10-08 (owner decision, option A) every process step that is not a check or an owner stop is handed to the agent owning its process, and each process's checks judge the result on disk (REQ-FN-084 to REQ-FN-102, REQ-UI-070, REQ-UI-071). Since 2026-10-09 a "writes documents" right covers docs/ only (REQ-FN-103 to REQ-FN-105, REQ-UI-072). This checklist is the whole work list of the phase.

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-FN-061 | A brief in, the whole chain run | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-061 the registered end-to-end run starts and the back`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-061) |
| REQ-UI-055 | Each role starts when it is needed | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-055 the planned chain lists the eight processes and t`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-055) |
| REQ-FN-062 | It stops at the acceptance test | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-062 the finished chain reads ready for acceptance and`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-062) |
| REQ-UI-056 | What is happening now | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-056 a run that is going shows the agent, the process,`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-056) |
| REQ-FN-063 | Step by step is untouched | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-063 with no run going every process can still be star`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-063) |
| REQ-FN-064 | A limit is waited out | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-064 a rate limit from a provider is recognised as a u`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-064) |
| REQ-FN-065 | A stall or a crash is survived | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-065 a stalled step is stopped, retried and recorded a`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-065) |
| REQ-FN-066 | Never complete while work is open | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-066 a run that has not finished its chain cannot be m`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-066) |
| REQ-FN-067 | Queue a run | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-067 a brief shorter than the minimum is refused`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-067) |
| REQ-UI-057 | One at a time | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-057 the next run waits while one is going and starts `; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-057) |
| REQ-FN-068 | Pause and start again | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-068 a paused run resumes the same run it was paused o`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-068) |
| REQ-FN-069 | Take one out | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-069 a removed waiting run leaves the queue and is nev`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-069) |
| REQ-FN-070 | A resumed run picks the weakest work first | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-070 a resume takes the blocked requirement before par`; Run queue renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-070) |
| REQ-UI-058 | Every requirement and its verdict | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-058 the report of a finished run reads verdicts, wait`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-058) |
| REQ-UI-059 | Every miss, and whose gap it was | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-059 the report lists each miss with what it was and w`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-059) |
| REQ-UI-060 | What it cost | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-060 a run with nothing recorded reports cost as not r`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-060) |
| REQ-UI-061 | Read an earlier one | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-061 earlier runs are listed newest first and a run of`; Run report renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-061) |
| REQ-FN-071 | A small model picks the model | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-071 the owner turns automatic routing on and it is re` | [view](#d-req-fn-071) |
| REQ-FN-072 | The owner can take it back | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-072 the owner turns automatic routing off and the tie` | [view](#d-req-fn-072) |
| REQ-FN-084 | A step is handed to its agent | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-084 a project with only a step session still gets the` | [view](#d-req-fn-084) |
| REQ-FN-085 | The agent keeps to its rights | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-085 a tool the agent's rights withhold is refused and` | [view](#d-req-fn-085) |
| REQ-FN-086 | The agent works on its own tier | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-086 the handed step names the model routed to its age` | [view](#d-req-fn-086) |
| REQ-FN-087 | The step waits for the agent and records what it did | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-087 the step ends only when the agent finishes, recor` | [view](#d-req-fn-087) |
| REQ-FN-088 | The checks judge what is on disk | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-088 a failed check stops the run at that step with th` | [view](#d-req-fn-088) |
| REQ-FN-089 | A real usage limit is waited out | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-089 a handed step's usage limit is waited out, the st`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-089) |
| REQ-FN-090 | Stop ends the agent's work | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-090 stop during a handed step ends the agent's work a` | [view](#d-req-fn-090) |
| REQ-FN-091 | An unattended step does not wait on approvals | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-091 an unattended handed step writes without approval`; End-to-end run renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-091) |
| REQ-UI-070 | Process run shows the agent at work | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-070 Process run shows the agent, its model, and the f` | [view](#d-req-ui-070) |
| REQ-UI-071 | The agent's output in the conversation | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-071 the agent's messages and tool calls appear in the` | [view](#d-req-ui-071) |
| REQ-FN-100 | day1-greenfield is done when its documents are | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-100 a missing brief fails the check` | [view](#d-req-fn-100) |
| REQ-FN-092 | split-brd is done when every requirement is in one phase | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-092 a one-phase project passes on its one BRD and che` | [view](#d-req-fn-092) |
| REQ-FN-093 | build-phase is done when the open rows are built | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-093 build-phase passes only when every open row is Im` | [view](#d-req-fn-093) |
| REQ-FN-094 | fix-issues is done when every issue is fixed | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-094 fix-issues passes only when no row is left FAIL o` | [view](#d-req-fn-094) |
| REQ-FN-095 | handoff-phase is done when the phase is closed in the documents | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-095 handoff-phase passes only when no row is open and` | [view](#d-req-fn-095) |
| REQ-FN-096 | refresh-status is done when the status matches the checklists | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-096 refresh-status passes only when the status writte` | Status | n |" or "Status: n". | [view](#d-req-fn-096) |
| REQ-FN-101 | deploy-checklist is done when the release list is whole | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-101 deploy-checklist passes only when the list writte` | [view](#d-req-fn-101) |
| REQ-FN-102 | mockups is done when every screen is drawn and linked | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-102 mockups passes only when every screen is drawn, a` | [view](#d-req-fn-102) |
| REQ-FN-097 | amend-docs is done when the documents agree | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-097 amend-docs passes only when touched documents cha` | [view](#d-req-fn-097) |
| REQ-FN-098 | triage-issues is done when every report has a row | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-098 triage-issues passes only when every report has a` | [view](#d-req-fn-098) |
| REQ-FN-099 | metrics-report is done when the report matches the records | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-099 metrics-report passes only when the report's tota` | [view](#d-req-fn-099) |
| REQ-FN-103 | A right to write documents | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-103 a role with only write-docs may write a new folde` | [view](#d-req-fn-103) |
| REQ-FN-104 | The Analyst, the Architect and the Verifier hold it | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-104 every source-file right is as before(aRole: "arch` | [view](#d-req-fn-104) |
| REQ-UI-072 | The right is shown where rights are listed | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-072 Settings > Agents lists "Writes documents" among ` | [view](#d-req-ui-072) |
| REQ-FN-105 | A handed step writes its documents but not source | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-105 a handed step writes its documents but a source w` | [view](#d-req-fn-105) |
| REQ-UI-073 | The menu bar's menus do their jobs | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-UI-073 File, Edit, View and Help each hold their items a` | [d](#d-req-ui-073) |
| REQ-UI-074 | Close the conversation and bring it back | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-074 closing the conversation gives the file view the ` | [d](#d-req-ui-074) |
| REQ-FN-106 | Settings and the other pages open from the main window | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-FN-106 REQ-FN-011 REQ-UI-017 REQ-UI-018 REQ-FN-012 REQ-F` | [d](#d-req-fn-106) |
| REQ-NFR-007 | The app icon is the approved Chatur mark | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-NFR-007 icon colour is brand amber and splash colour sta` | [view](#d-req-nfr-007) |
| REQ-NFR-008 | The splash screen is the approved glyph | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-NFR-008 icon and splash SVGs are byte-identical to the b` | [view](#d-req-nfr-008) |
| REQ-UI-078 | The mark sits beside the wordmark | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-UI-078 the mark is the white glyph on a 20 px amber squa` | [view](#d-req-ui-078) |
| REQ-NFR-009 | The web harness carries the icon, made by a script | Verified | 100% | 2026-10-29 verify: PASS — test `REQ-NFR-009 App.razor links favicon.ico, icon-192.png and ic` | [view](#d-req-nfr-009) |
| REQ-UI-079 | Type @ to attach a project file in the chat | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-UI-079 typing @ lists the project files, typing narrows ` | [d](#d-req-ui-079) |
| REQ-UI-080 | Controls keep the mockup's inner spacing | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-UI-080 a focused field on New project keeps its text ins` | [d](#d-req-ui-080) |
| REQ-UI-081 | Labels carry no trailing dots | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-UI-081 no menu, button, label or placeholder on any scre` | [d](#d-req-ui-081) |
| REQ-UI-082 | Every button does something | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-UI-082 the toolbar Find, once always greyed out, opens F` | [d](#d-req-ui-082) |
| REQ-NFR-010 | Tests never touch the owner's database | Verified | 100% | 2026-10-30 verify: PASS — test `REQ-NFR-010 the desktop head under test keeps its database u` | [d](#d-req-nfr-010) |

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



## Workbench

- <a id="d-req-ui-073"></a> **REQ-UI-073** — The menu bar's menus do their jobs. *BRD:* BRD-184 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens File, Edit, View or Help on Workbench, then every drawn item works and none says 'Nothing to do yet'.

- <a id="d-req-ui-074"></a> **REQ-UI-074** — Close the conversation and bring it back. *BRD:* BRD-185 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner closes the conversation panel on Workbench, then the file view takes the window, and View ▸ Chat brings the conversation back.

- <a id="d-req-fn-106"></a> **REQ-FN-106** — Settings and the other pages open from the main window. *BRD:* BRD-186 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner picks Settings, a settings page, Prerequisites or Repository from the main window's menus, then the secondary window opens on that page.

- <a id="d-req-ui-079"></a> **REQ-UI-079** (BRD-pending) Type @ to attach a project file in the chat · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner types @ in the chat box on Workbench, then the project's files are listed, narrowing as they type, and a pick puts its path in the message.

- <a id="d-req-ui-080"></a> **REQ-UI-080** (BRD-pending) Controls keep the mockup's inner spacing · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens any dialog or page, then every input, button and list row keeps the mockup's inner padding and no text or control touches its border.

- <a id="d-req-ui-081"></a> **REQ-UI-081** (BRD-pending) Labels carry no trailing dots · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens any menu, button or label in Chatur, then its text ends without three dots.

- <a id="d-req-ui-082"></a> **REQ-UI-082** (BRD-pending) Every button does something · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner presses any enabled button on any screen, then it does its job; no button exists only for looks.

- <a id="d-req-nfr-010"></a> **REQ-NFR-010** (BRD-pending) Tests never touch the owner's database
  - *Acceptance:* When the desktop or web tests run, then the app keeps its database under tests/.artifacts and refuses the owner's application-data database.

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
