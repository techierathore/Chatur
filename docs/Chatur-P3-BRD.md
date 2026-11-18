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

Phase 2 makes every command a process. Phase 3 chains them: one flow master command takes a brief and runs the whole chain to "ready for the owner's acceptance test", starting each role as it is needed. Step-by-step working stays the normal way. What makes it usable is that a run survives the things that stop runs today: a usage limit, a stall, a crash. It waits and resumes, it can be queued, and it never reports complete while work is unfinished, which is the failure the owner already has with TechieFlow's unattended runner. Afterwards there is a report to read: every requirement and its verdict, every miss and whose gap it was, and the models, tokens and time the run used. Model routing also becomes automatic in this phase: a small model inside TechieRag picks the model for each piece of work, and the owner can turn that off. Since 2026-10-08 every work step goes to the agent owning its process, and its checks judge the result on disk (BRD-159 to BRD-179). Since 2026-10-09 a "writes documents" right covers `docs/` only (BRD-180 to BRD-183). From 2026-11-10, estimate before, actual after (BRD-192 to BRD-209). Per-account data: BRD-210 to BRD-216.

## 2. Screens and flow

One row per routed page this phase adds; dialogs are written exactly as in the phase-1 BRD.

| Screen | Route | Role | Mockup | Fields |
|---|---|---|---|---|
| End-to-end run | `/auto` | Owner | [mockup](mockups/auto-run.html) | brief, project, role now working, step, state, stop |
| Run queue | `/auto/queue` | Owner | [mockup](mockups/run-queue.html) | queued run, project, state, position, started |
| Run report | `/auto/report` | Owner | [mockup](mockups/run-report.html) | requirement, verdict, miss, model, tokens, time, estimate, actual, within / above / below |

**Primary journey:**

1. The owner hands a brief to the end-to-end run and starts it.
2. Chatur starts each role as it is needed and shows which one is working and on what.
3. A usage limit, a stall or a crash pauses the run; it waits, resumes and records what happened.
4. The run stops at "ready for the owner's acceptance test" and never says complete before that.
5. The owner reads the report afterwards: every requirement, every miss, and what it cost beside what it was estimated to cost.

Before step 1, and before any single build, verify or fix started from Processes, Chatur shows what the run should cost and starts only when the owner confirms (BRD-192 to BRD-194).

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

### Estimate before, actual after

Added 2026-11-10 on the owner's ask, a mandatory feature. Before a build, a verify, a fix or a whole brief-to-verified run starts, Chatur shows what it should cost, and afterwards it sets that estimate beside what the run actually cost. TechieFlow v1.3.0 does the same thing with a script (`*estimate-phase`, `tf-estimate.sh`, framework schema §2.8 and §6); Chatur does it in its own code from the same rules, so the two give the same figures from the same records. No script is called and no task file is read while Chatur runs.

**The rule, in code, with no model asked.** For each row in scope and each line of work, Chatur takes the earlier live runs of that process that touched at least one row of the same kind (REQ-UI, REQ-FN, REQ-RAG, REQ-NFR) and were run on the model that will run this step. From each run it takes output tokens per row and seconds per row (the run's figure divided by the rows it touched). Low, middle and high are the first quartile, the median and the third quartile of those figures, interpolated between neighbours and rounded to whole numbers half to even, exactly as `tf-estimate.sh` does. The lines of work are build (`build-phase`), machine QA (`verify-phase` plus `fix-issues`, added for display and never stored added), and after handoff (`triage`: `triage-issues` and, in a sister project's records, `triage-and-fix`). A phase total is the sum of the row figures that have one, and says how many rows it covers. The owner's own testing is never estimated and prints "not measured".

**Which runs count, in order (owner, 10 Nov 2026).** The first of these with at least three runs gives the figure: (1) this project's runs on the step's model; (2) those plus the sister projects' runs on the step's model, labelled "from sister projects"; (3) this project's runs on any model, labelled "from all models"; (4) those plus the sister projects' runs on any model, labelled "from all models" and "from sister projects". None with three: no precedent, never a number, over the runs of the widest pool tried. A project with no run of its own and sister projects that have some is estimated from theirs; "No estimate yet" shows only when no step has a figure from any pool (BRD-208).

**The model a step runs on.** The first model of the tier routing gives the step. With automatic routing on, the estimate is still made for that model, and the screen says that automatic routing may choose another; the actual always names the model that ran. The estimate record's `model` is that model and its optional `tier` is that tier (BRD-205, [the metrics schema](Chatur-Metrics-Schema.md)).

**A brief with no checklist yet.** An end-to-end run from a brief has no rows before day-1 and split-brd have run. Before Start the estimate says so; the run writes its estimate when it reaches the build step, without stopping. In step-by-step mode the estimate is shown at that step before its Start (BRD-193).

**Fixtures for the acceptance lines.** The checklist rows REQ-UI-083 to REQ-UI-090 and REQ-FN-107 to REQ-FN-116 name these; each is a unit test's input. Each is a set of run records in a test project named `Fixture`, whose phase 2 checklist holds REQ-FN-301, REQ-FN-302 and REQ-FN-303. Every run is live, measured, and on `claude-opus-5-5` unless the row says otherwise.

| Fixture | Run records in | Expected out |
|---|---|---|
| F1 | Five `build-phase` runs on REQ-FN rows outside phase 2: A 2 rows, 4,000 tokens out, 600 s · B 1 row, 3,000, 240 s · C 2 rows, 10,000, 900 s · D 4 rows, 4,000, 480 s · E 4 rows, 16,000, 1,440 s | Each REQ-FN row, build: tokens 2,000 · 3,000 · 4,000; time 240 · 300 · 360 s; basis repo, 5 runs. Phase total over 3 rows: 6,000 · 9,000 · 12,000 tokens; 720 · 900 · 1,080 s |
| F2 | Two `build-phase` runs here on REQ-UI rows (1,000 tokens and 100 s per row; 3,000 and 300) and two in sister project S1 (2,000 and 200; 6,000 and 500) | A REQ-UI row, build: tokens 1,750 · 2,500 · 3,750; time 175 · 250 · 350 s; basis family, 4 runs; the screen names S1 |
| F3 | One `triage-issues` run here on a REQ-FN row; S1 holds one more | After handoff: no precedent, basis none, 2 runs; both figures null |
| F4 | F1, plus one run with no row named, one with no measured output tokens, one backfilled, one voided, and one that touched REQ-FN-301 | The same figures as F1 from the same 5 runs; every record of the batch carries `basis_note: "retro"` and the screen says "estimated after the fact" |
| F5 | F1 with runs B and D on `claude-sonnet-5-5`; tier 1 is `claude-opus-5-5`, tier 2 `claude-sonnet-5-5` | Build on tier 1: tokens 3,000 · 4,000 · 4,500; time 330 · 360 · 405 s; basis repo, 3 runs. Build on tier 2: only 2 runs on `claude-sonnet-5-5`, so all models: tokens 2,000 · 3,000 · 4,000; time 240 · 300 · 360 s; basis repo, 5 runs, labelled "from all models" |
| F6 | F1's phase total as the estimate, then live `build-phase` runs R1 (REQ-FN-301, REQ-FN-302 and REQ-FN-999 outside the phase; 9,000 tokens, 900 s), R2 (REQ-FN-303; 3,000, 300 s), R3 (REQ-FN-302; 1,500, 150 s), and a `verify-phase` run R4 that named no row (2,000 tokens, 120 s) | Build actual 10,500 tokens and 1,050 s, labelled apportioned, within the range; "1 run named no row — 2,000 tokens, 2 m", in no figure. With only R2 and R3: "insufficient data (n=2)" |

**The five open points, answered by the owner on 10 Nov 2026** (written into the rules above):

1. *The `model` field* holds the model that will run the step. The tier goes into the record as one extra optional field, `tier`, described in [the metrics schema](Chatur-Metrics-Schema.md).
2. *History by model:* same model first. Fewer than three runs on that model: runs on all models, labelled "from all models". Only when that also has fewer than three does the row read "no precedent".
3. *Automatic routing on:* the estimate is for the first model of the step's tier, and the screen says automatic routing may choose another. The actual always names the model that ran.
4. *A brief with no checklist yet:* the run writes its estimate when it reaches the build step, without stopping. In step-by-step mode it is shown at that step before Start.
5. *No runs of its own:* the sister projects' runs are used, labelled "from sister projects". "No estimate yet" only when neither has runs.

Figures checked on 10 Nov 2026: F1 to F5 against `tf-estimate.sh --dry-run --json` (F5 per model by giving the script only that model's runs) and F6 against `tf-metrics.sh --estimates` on the same records. Every figure the analyst worked out by hand matched; none was changed. F5's tier 2 line changed only because of answer 2.

- **BRD-192** — An estimate before a process starts. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When the owner presses Start on build-phase, verify-phase or fix-issues for fixture F1 on Processes, then every row and the phase total show, and nothing runs until confirmed.
- **BRD-193** — An estimate before an end-to-end run starts. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When the owner fills in a brief for a project holding fixture F1 on End-to-end run, then the estimate shows before Start, each line naming its model and tier.
- **BRD-194** — An estimate on a queued run. *Screen:* Run queue · *Mockup:* [mockup](mockups/run-queue.html)
  - *Acceptance:* When the owner queues a run for fixture F1 on Run queue, then its estimate shows before Queue it is pressed, and the waiting run keeps that estimate.
- **BRD-195** — Worked out from this project's own runs, row kind by row kind. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F1 is estimated on Processes, then each REQ-FN row's build reads 2,000/3,000/4,000 tokens and 240/300/360 s over 5 runs, and the three-row total 6,000/9,000/12,000 tokens.
- **BRD-196** — Sister projects fill in, and the estimate says so. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F2 is estimated on Processes, then a REQ-UI row's build reads 1,750/2,500/3,750 tokens and 175/250/350 s, basis family over 4 runs, naming S1.
- **BRD-197** — The owner names the sister projects. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-measurements.html)
  - *Acceptance:* When the owner adds a sister project on Settings, then it is listed with its run count, and a folder without docs/metrics/runs.jsonl is refused with the reason.
- **BRD-198** — No precedent, never a number. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F3 is estimated on Processes, then after handoff reads "no precedent", basis none over 2 runs, with no figure shown or written.
- **BRD-199** — No model judgement in the estimate. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F1 is estimated twice on Processes with no model provider reachable, then both results match to the last digit and no model call is made.
- **BRD-200** — Made for the model and tier that will run the step. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F5 is estimated with build on tier 1, then build reads 3,000/4,000/4,500 tokens and 330/360/405 s over 3 runs, naming claude-opus-5-5 and tier 1.
- **BRD-201** — Changing the tier changes the estimate before the start. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-routing.html)
  - *Acceptance:* When the owner moves build to tier 2 on Settings, then F5 reads 2,000/3,000/4,000 tokens "from all models" for claude-sonnet-5-5 without a restart; a started run keeps its own.
- **BRD-202** — Process run shows the actual so far beside the estimate. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a build-phase run on F1's three rows has spent 4,500 tokens and 400 s on Process run, then those show beside 6,000/9,000/12,000 and 720/900/1,080 s, uncombined.
- **BRD-203** — The run report sets estimate beside actual. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When a run estimated at 6,000/9,000/12,000 tokens ends on Run report, then the actual reads within at 6,000 and 12,000, above at 12,001, below at 5,999.
- **BRD-204** — At handoff, the phase against its estimate. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When handoff-phase runs for fixture F6 on Process run, then build shows 6,000/9,000/12,000 beside 10,500 tokens, apportioned, within, and "1 run named no row" stands apart.
- **BRD-205** — Estimates are written in TechieFlow's own shape. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When the owner confirms a start for fixture F1 on Processes, then REQ-FN-301's build record in estimates.jsonl equals the Metrics Schema example, ts aside.
- **BRD-206** — An estimate is never mixed with a measurement. *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When fixture F1 is estimated and the run ends on Run report, then runs.jsonl, the checklist and PROJECT-STATUS hold no estimate figure, and no screen shows a ratio.
- **BRD-207** — The owner's own testing reads "not measured". *Screen:* Run report · *Mockup:* [mockup](mockups/run-report.html)
  - *Acceptance:* When any estimate table shows on Processes, End-to-end run, Run queue, Process run or Run report, then the owner's testing line reads "not measured", never a number.
- **BRD-208** — A project with no run records says "no estimate yet". *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When a process is started on Processes for a project with no usable run anywhere, then it reads "No estimate yet", Start works, and no error is logged.
- **BRD-209** — Runs that cannot be divided among rows are left out. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When fixture F4 is estimated on Processes, then the figures equal F1's over the same 5 runs, and the batch carries basis_note "retro".

### Each account has its own Chatur

Added 2026-11-17 from the owner's first sign-in with his common App Manager account (`docs/screenshots/2026-11-17-first-use/Home-Screan-Broken.png`): an account that had never used Chatur opened another account's project, its files, its chat and its project list. Everything a person makes or opens in Chatur belongs to the account signed in: projects and named folders, the last open project, open files, chat sessions and their records, run history, providers and their keys in the operating system's store, routing and the other settings. A second account on the same install sees none of it. What stays with the install: the device identifier App Manager knows this machine by (BRD-4), and the sign-in that is remembered on this machine (BRD-5). Data kept before this change belongs to nobody: it stays where it was, nothing is deleted, and no account is shown it.

- **BRD-210** — What one account makes or opens, another never sees. *Screen:* Start · *Mockup:* [mockup](mockups/start-empty.html)
  - *Acceptance:* When a second account signs in on Start where a first account opened projects and chatted, then Start is empty and Switch project lists none of the first's.
- **BRD-211** — A chat is never continued by another account. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When a second account opens on Workbench a folder a first account chatted in, then its conversation is empty and never says "Continuing after".
- **BRD-212** — A provider and its key belong to the account that added it. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-providers.html)
  - *Acceptance:* When a second account opens Providers on Settings after a first account connected one, then the list is empty and the first account's key is never read for it.
- **BRD-213** — Run history belongs to the account that ran it. *Screen:* End-to-end run · *Mockup:* [mockup](mockups/auto-run.html)
  - *Acceptance:* When a second account opens End-to-end run, Run queue or Run report on an install where a first account has runs, then none of the first account's runs is listed.
- **BRD-214** — Data kept before accounts belongs to nobody. *Screen:* Start · *Mockup:* [mockup](mockups/start-empty.html)
  - *Acceptance:* When an account signs in on Start where projects, chats and runs were kept before accounts, then it is shown none of them and none is deleted.
- **BRD-215** — Open a file from Start. *Screen:* Start · *Mockup:* [mockup](mockups/start-empty.html)
  - *Acceptance:* When the person chooses Open a file on Start and gives a file, then the Workbench opens on the folder holding it, with that file open in a tab.
- **BRD-216** — Signing in as another account leaves nothing of the last one on screen. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When one account signs out in a window and another signs in there, then no project, open file, chat or output of the first account is shown.

## 4. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-11-18.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P3-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| End-to-end run | 12 | 12 | 0 | Done |
| Run queue | 6 | 6 | 0 | Done |
| Run report | 7 | 7 | 0 | Done |
| Settings | 8 | 8 | 0 | Done |
| Process run | 22 | 22 | 0 | Done |
| Workbench | 10 | 10 | 0 | Done |
| Start | 7 | 7 | 0 | Done |
| Processes | 9 | 9 | 0 | Done |

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
| The Start, Workbench and Settings items BRD-210 to BRD-216 narrow to one account (BRD-11, BRD-13, BRD-14, BRD-17, BRD-35) | [phase 1 BRD](Chatur-BRD.md) |
| The Processes screen whose Start BRD-192 puts an estimate in front of | [phase 2 BRD](Chatur-P2-BRD.md) |
| Settings ▸ Measurements, where BRD-197's sister projects are named | [phase 1 BRD](Chatur-BRD.md) |
| The rules the estimate copies (`tf-estimate.sh`, the estimates stream, the four separations) | `.tfcore/telemetry/SCHEMA.md` §2.8 and §6, and [the metrics schema](Chatur-Metrics-Schema.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P3-Checklist.md](Chatur-P3-Checklist.md) |
