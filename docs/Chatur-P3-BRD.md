# Chatur — Business Requirements — Phase 3: End to end and unattended

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Medium |
| Phase | 3 of 4 |
| Status | Draft |
| Date | 2026-09-21 |

## 1. Summary

Phase 2 makes every command a process. Phase 3 chains them: one flow master command takes a brief and runs the whole chain to "ready for the owner's acceptance test", starting each role as it is needed. Step-by-step working stays the normal way — this is the extra command, not the replacement. What makes it usable is that a run survives the things that stop runs today: a usage limit, a stall, a crash. It waits and resumes, it can be queued, and it never reports complete while work is unfinished, which is the failure the owner already has with TechieFlow's unattended runner. Afterwards there is a report to read: every requirement and its verdict, every miss and whose gap it was, and the models, tokens and time the run used. Model routing also becomes automatic in this phase: a small model inside TechieRag picks the model for each piece of work, and the owner can turn that off. Since 2026-10-08 every work step goes to the agent owning its process, and its checks judge the result on disk (BRD-159 to BRD-179). Since 2026-10-09 a "writes documents" right covers `docs/` only (BRD-180 to BRD-183).

## 2. Screens and flow

One row per routed page this phase adds; dialogs are written exactly as in the phase-1 BRD.

| Screen | Route | Role | Mockup | Fields |
|---|---|---|---|---|
| End-to-end run | `/auto` | Owner | [mockup](mockups/auto-run.html) | brief, project, role now working, step, state, stop |
| Run queue | `/auto/queue` | Owner | [mockup](mockups/run-queue.html) | queued run, project, state, position, started |
| Run report | `/auto/report` | Owner | [mockup](mockups/run-report.html) | requirement, verdict, miss, model, tokens, time |

**Primary journey:**

1. The owner hands a brief to the end-to-end run and starts it.
2. Chatur starts each role as it is needed and shows which one is working and on what.
3. A usage limit, a stall or a crash pauses the run; it waits, resumes and records what happened.
4. The run stops at "ready for the owner's acceptance test" and never says complete before that.
5. The owner reads the report afterwards: every requirement, every miss, and what it cost.

## 3. Requirements

### End-to-end run

One command, a brief in, work ready for the owner's acceptance test out.

- **BRD-118** — A brief in, the whole chain run. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When the owner starts an end-to-end run with a brief on End-to-end run, then Chatur runs the chain of processes without being told each one.
- **BRD-119** — Each role starts when it is needed. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When the chain reaches work another role owns on End-to-end run, then that role is started and the screen names it.
- **BRD-120** — It stops at the acceptance test. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When the chain finishes on End-to-end run, then the run's state reads ready for the owner's acceptance test, and nothing further is run.
- **BRD-121** — What is happening now. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When End-to-end run opens with a run going, then it shows the agent working, the process, the step and the requirement.
- **BRD-122** — Step by step is untouched. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When no run is going on End-to-end run, then every process can still be started on its own from Processes.
- **BRD-123** — A limit is waited out. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When a model reports a usage limit on End-to-end run, then the run pauses, waits and carries on by itself, and the wait is recorded.
- **BRD-124** — A stall or a crash is survived. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When a step stalls or the work crashes on End-to-end run, then Chatur retries it, records the retry, and carries on or stops with a reason.
- **BRD-125** — Never complete while work is open. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When any requirement of the run is still open, then the run cannot be marked complete.

### Run queue

More than one run, waiting its turn, and able to be picked up again.

- **BRD-126** — Queue a run. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When the owner queues a run on Run queue, then it is listed with its project and its position in the queue.
- **BRD-127** — One at a time. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When a run is already going on Run queue, then the next one in the queue does not start until it has ended.
- **BRD-128** — Pause and start again. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When the owner pauses a run and resumes it on Run queue, then it carries on from the step it was paused on.
- **BRD-129** — Take one out. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When the owner removes a queued run on Run queue, then it leaves the queue and is not started.
- **BRD-130** — A resumed run picks the weakest work first. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When a run resumes after a stop on Run queue, then it continues from the open requirement furthest from verified.

### Run report

What the run did, for the owner to read when it is over.

- **BRD-131** — Every requirement and its verdict. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When a run has ended on Run report, then its report lists every requirement it touched with the verdict it was left at.
- **BRD-132** — Every miss, and whose gap it was. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When the run recorded misses on Run report, then the report lists each one with what it was and whose gap it was.
- **BRD-133** — What it cost. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When the report opens, then it shows the tokens, the models used and the time the run took.
- **BRD-134** — Read an earlier one. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When the owner picks an earlier run on Run report, then that run's report is shown instead of the newest.

### Settings

The routing tab of the phase-1 Settings screen gains the automatic choice the brief asks for last.

- **BRD-135** — A small model picks the model. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-routing.html)
  - *Acceptance:* When automatic routing is on, then each piece of work is given a model chosen by the small model inside TechieRag.
- **BRD-136** — The owner can take it back. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-routing.html)
  - *Acceptance:* When the owner turns automatic routing off on Settings, then the tiers and chains he set are used again.

### Step work

Added 2026-10-08 on the owner's decision (option A, archived at `docs/OldDocs/Chatur-Decision-Request-2026-10-08-process-step-work.md`). A *work step* is a step that is not a check or an owner stop; it is handed to the agent that owns its process (the Agent column of the process catalogue), and the process's checks judge what that agent left on disk. The agent works under its role's rights and its routed tier, or automatic routing's choice when that is on. The Process run screen is phase 2's; this phase adds what it shows for a handed step. A run started from Processes keeps the owner's session mode; an end-to-end run does not stop for per-change approval, as its owner stops are already answered by the run.

- **BRD-159** — A step is handed to its agent. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a run reaches a work step on Process run, then a session starts with the process's agent, given the step's wording, the run and the project.
- **BRD-160** — The agent keeps to its rights. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a handed step's agent asks for a tool its rights withhold on Process run, then the tool is refused and the refusal is recorded on the step.
- **BRD-161** — The agent works on its own tier. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a handed step starts on Process run, then its agent's session uses the model routed to that agent's tier, and the step names that model.
- **BRD-162** — The step waits for the agent and records what it did. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a handed step's agent finishes on Process run, then the step ends only then, recording the files written, the commands run and the agent's final message.
- **BRD-163** — The checks judge what is on disk. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a check follows a handed step on Process run, then it judges the project's files on disk, and if it fails the run stops there with the reason.
- **BRD-164** — A real usage limit is waited out. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When a handed step's model reports a usage limit on End-to-end run, then the run waits, resumes that step by itself, and records the wait against it.
- **BRD-165** — Stop ends the agent's work. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When the owner presses Stop during a handed step on Process run or End-to-end run, then the agent's session ends and the step reads stopped by the owner.
- **BRD-166** — An unattended step does not wait on approvals. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When a handed step runs on End-to-end run, then the agent's changes are written without waiting for approval, while its rights and the source-control refusal still apply.
- **BRD-167** — Process run shows the agent at work. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When Process run opens on a handed step, then it shows the agent, its model, and the files written, commands run and refusals so far.
- **BRD-168** — The agent's output in the conversation. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a handed step is being worked on Process run, then the agent's messages and tool calls appear in the conversation panel as they happen, under that agent's name.

### What done means for each process

One requirement per process: what its checks verify on disk before the run may go past them. verify-phase is not here; its build and test steps are already real. The table gives what each check reads; the rows are the process catalogue's (`ProcessCatalogue.cs`) and the outputs are those of the TechieFlow command of the same name. A check marked *added* is one the catalogue lacks and must gain, because without it "done" cannot be judged.

| BRD | Process | Check rows | What the checks verify on disk |
|---|---|---|---|
| BRD-169 | day1-greenfield | 3–4, 5, 8 | Brief names an audience, a problem and a measure of success; BRD, Architecture, UI design, a mockup per screen and the checklist exist; every BRD item has an acceptance line and a checklist row, every UI row a mockup that exists, no id twice |
| BRD-170 | split-brd | 2–3, 5 | Phases file gives every phase a BRD range; each phase's BRD and checklist exist; every BRD id in exactly one phase; the phase files together hold every id. A one-phase project passes on its one BRD and checklist |
| BRD-171 | build-phase | 2–3, 4–5, 7–8 | The cluster is every open row of the phase; Chatur runs the build itself (no error, no warning) and the tests itself (all pass); every open row is Implemented and a test carries its id (*added* after row 10) |
| BRD-172 | fix-issues | 3–4, 5–6, 7 | Every row verification left FAIL or Needs re-verify, not only the first; Chatur's own build has no warning; those rows' tests pass; each row is Implemented again |
| BRD-173 | handoff-phase | 1–2 | No row of the phase is open (only Verified, Done (pre-existing), N/A, or Blocked with a reason); PROJECT-STATUS carries a hand-off entry dated that day and the phases file marks the phase done (*added* after row 4) |
| BRD-174 | refresh-status | 2–3 | PROJECT-STATUS was written during the run and its count per status equals the checklists', adding up to their total (*added* after row 4: the catalogue checks the counts before they are written) |
| BRD-175 | deploy-checklist | 2–3, 5 | The deployment checklist was written during the run, with before, deploy, after, rollback, secrets and proven sections; every item a checkbox naming who does it; pipeline, secrets and versions covered |
| BRD-176 | mockups | 2–3 | Every screen of the phase has a mockup file with `data-testid` anchors and a UI design section with its regions and empty, loading and error states; every UI row names a mockup that exists (*added* after row 4) |
| BRD-177 | amend-docs | 2–3, 5 | Every document named as touched changed during the run; no BRD id renumbered or deleted (removed ones struck through); every new BRD item has a checklist row with an acceptance line |
| BRD-178 | triage-issues | 2–3, 4 | Every report read is a reopened row or a new Not Started row with an acceptance line; nothing under `src/` or `tests/` changed during the run |
| BRD-179 | metrics-report | 2–3 | The metrics report was written during the run and its totals of runs, tokens and failures equal those added up again from `docs/metrics/*.jsonl` (*added* after row 4) |

- **BRD-169** — day1-greenfield is done when its documents are. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When day1-greenfield's checks run on Process run, then they pass only when the brief, BRD, Architecture, UI design, mockups and checklist exist and pass the document rules.
- **BRD-170** — split-brd is done when every requirement is in one phase. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When split-brd's checks run on Process run, then they pass only when every BRD id lands in exactly one phase and each phase has its BRD and checklist.
- **BRD-171** — build-phase is done when the open rows are built. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When build-phase's checks run on Process run, then they pass only when Chatur's own build has no warning, the tests pass and every open row is Implemented.
- **BRD-172** — fix-issues is done when every issue is fixed. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When fix-issues's checks run on Process run, then they pass only when Chatur's own build has no warning, the failed rows' tests pass and each is Implemented again.
- **BRD-173** — handoff-phase is done when the phase is closed in the documents. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When handoff-phase's checks run on Process run, then they pass only when no row of the phase is open and the status and phases files record the hand-off.
- **BRD-174** — refresh-status is done when the status matches the checklists. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When refresh-status's checks run on Process run, then they pass only when the status document written in the run counts each status as the checklists do.
- **BRD-175** — deploy-checklist is done when the release list is whole. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When deploy-checklist's checks run on Process run, then they pass only when the deployment checklist written in the run has every section, and every item names who does it.
- **BRD-176** — mockups is done when every screen is drawn and linked. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When mockups's checks run on Process run, then they pass only when every screen has an anchored mockup and a UI design section, and every UI row's mockup exists.
- **BRD-177** — amend-docs is done when the documents agree. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When amend-docs's checks run on Process run, then they pass only when touched documents changed, no BRD id was renumbered or deleted, and each new item has a checklist row.
- **BRD-178** — triage-issues is done when every report has a row. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When triage-issues's checks run on Process run, then they pass only when every report is a reopened or new checklist row and nothing under src or tests changed.
- **BRD-179** — metrics-report is done when the report matches the records. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When metrics-report's checks run on Process run, then they pass only when the report written in the run has totals equal to those recomputed from the run records.

### Document rights

Added 2026-10-09 on the owner's decision (option A, archived at `docs/OldDocs/Chatur-Decision-Request-2026-10-09-document-rights.md`). Phase 1's rights know one kind of writing, "write to a source file", which the Analyst never had and phase 2 took from the Verifier, so the processes they own (day1-greenfield, amend-docs, triage-issues, metrics-report) could not write their documents. A new right, "writes documents", covers files under the project's `docs/` folder and nothing else. A path counts as a document only once it is resolved: one that leaves `docs/` through `..`, a link or an absolute path is not a document. Writing anywhere else still needs "write to a source file"; holding that right also covers `docs/`, so either right lets an agent write a document. The Settings screen is phase 1's and Agent workspace is phase 2's; this phase adds one right to what both list.

- **BRD-180** — A right to write documents. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When an agent with only "writes documents" writes a path that resolves outside docs/, even through "..", on Process run, then the write is refused with the reason.
- **BRD-181** — The Analyst, the Architect and the Verifier hold it. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-agents.html)
  - *Acceptance:* When the owner opens each role on Settings, then the Analyst, Architect and Verifier hold "writes documents", the Flow master does not, and every source-file right is as before.
- **BRD-182** — The right is shown where rights are listed. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-agents.html)
  - *Acceptance:* When the owner opens a role on Settings or its Agent workspace, then "writes documents" is listed with the other rights, held or withheld.
- **BRD-183** — A handed step writes its documents but not source. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When an agent with only "writes documents" works a handed step on Process run, then its docs/ writes succeed and a source write is refused and recorded on the step.

### The main window, as the owner uses it

Added 2026-10-13 from the owner's test of the 12 Oct nightly (`docs/screenshots/NightlyBuildIssues`). The main window's menu bar drew File, Edit and View with nothing in them, the Settings window never opened, and the conversation could not be closed. The Workbench screen is phase 1's; these items make its menu bar and its windows do what the UI Design and `mockups/main.html` already draw, so they are phase 3's open work rather than a phase of their own.

- **BRD-184** — The menu bar's menus do their jobs. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the owner opens File, Edit, View or Help on Workbench, then every drawn item works and none says "Nothing to do yet".
- **BRD-185** — Close the conversation and bring it back. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the owner closes the conversation panel on Workbench, then the file view takes the window, and View ▸ Chat brings the conversation back.
- **BRD-186** — Settings and the other pages open from the main window. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the owner picks Settings, a settings page, Prerequisites or Repository from the main window's menus, then the secondary window opens on that page.

### Brand assets

Added 2026-10-27 on the owner's ask, once the Chatur logo set was approved (27 Oct, 13:20). The approved files live in the owner's brand folder, `MyAiChats/3-Resources/Brand/logos/`: `Chatur-appicon.svg` (the amber `#B45F14` tile with the white glyph), `Chatur-appiconfg.svg` (the glyph alone, on transparent), `Chatur-horizontal.svg` and `Chatur-horizontal.png` (glyph and wordmark), and `Chatur.png` (the tile at 1024 px). They are copied into the repository byte for byte and never redrawn. Resizetizer makes every icon and splash size; no PNG is made by hand under `Resources/`, and bin and obj are cleaned before the first build after the swap. The mark's square is always the brand amber `#B45F14`, whatever theme is chosen, because a brand mark that changes colour with the theme is no longer the brand. The menu bar's rules for a tight window (the wordmark hidden at 980 px and below, the logo at 600 px and below) are unchanged.

- **BRD-188** — The app icon is the approved Chatur mark. *Screen:* Start · *Mockup:* [mockup](mockups/start.html)
  - *Acceptance:* When the owner starts the built Windows app on Start, then the taskbar and the title bar show the amber Chatur tile with its white glyph.
- **BRD-189** — The splash screen is the approved glyph. *Screen:* Start · *Mockup:* [mockup](mockups/start.html)
  - *Acceptance:* When the three icon and splash SVGs under Resources are compared with the brand folder's files, then each is byte-identical and the splash colour stays `#1E1B16`.
- **BRD-190** — The mark sits beside the wordmark. *Screen:* Start · *Mockup:* [mockup](mockups/start.html)
  - *Acceptance:* When the owner opens Start, a bare page, the main or the secondary window, then the logo is the white glyph on a 20 px amber square.
- **BRD-191** — The web harness carries the icon, made by a script. *Screen:* Start · *Mockup:* [mockup](mockups/start.html)
  - *Acceptance:* When the web harness page loads, then it links `favicon.ico` (32 px), `icon-192.png` and `icon-512.png`, each loads, and each is what the repository's script under `scripts/` renders from `Chatur.png`.

## 4. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-10-30.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P3-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| End-to-end run | 10 | 10 | 0 | Done |
| Run queue | 5 | 5 | 0 | Done |
| Run report | 4 | 4 | 0 | Done |
| Settings | 5 | 5 | 0 | Done |
| Process run | 20 | 20 | 0 | Done |
| Workbench | 8 | 8 | 0 | Done |
| Start | 4 | 4 | 0 | Done |

## 5. Where the rest lives

| What | Where |
|---|---|
| Scope, users and roles, the context diagram | [phase 1 BRD](Chatur-BRD.md) |
| Non-functional requirements for the whole application | [phase 1 BRD](Chatur-BRD.md) |
| Constraints, assumptions and risks | [phase 1 BRD](Chatur-BRD.md) |
| The Settings screen whose routing tab this phase adds to | [phase 1 BRD](Chatur-BRD.md) |
| The Process run screen whose handed steps this phase adds to | [phase 2 BRD](Chatur-P2-BRD.md) |
| The rights on Settings ▸ Agents (BRD-53, BRD-59) and on Agent workspace (BRD-114) that BRD-180 to BRD-183 add to | [phase 1 BRD](Chatur-BRD.md), [phase 2 BRD](Chatur-P2-BRD.md) |
| The Start screen whose logo BRD-188 to BRD-191 change | [phase 1 BRD](Chatur-BRD.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P3-Checklist.md](Chatur-P3-Checklist.md) |
