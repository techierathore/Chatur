# Chatur — Business Requirements — Phase 2: Project workflow

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 2 of 4 |
| Status | Draft |
| Date | 2026-09-21 |

## 1. Summary

Phase 1 gives the owner an agent he can direct. Phase 2 gives that agent the way of working he already has: every TechieFlow command becomes a Chatur process with the same fixed steps, the same checks and the same owner stops, reading and writing the same project documents. A project can therefore move from TechieFlow to Chatur one command at a time, with nothing to carry across. The steps and checks of a process are code, so they cannot drift; the wording of each step comes from the database, so Chatur can correct it and the owner can see what changed. Work is routed to the agent that owns it by the type of requirement, each agent gets its own view of the project, and a board shows every requirement and where it stands. This is the phase that makes Chatur a replacement for the framework rather than a better chat window.

## 2. Screens and flow

One row per routed page this phase adds; dialogs are written exactly as in the phase-1 BRD.

| Screen | Route | Role | Mockup | Fields |
|---|---|---|---|---|
| Processes | `/processes` | Owner | [mockup](mockups/processes.html) | process, what it does, steps, checks, owner stops, last run |
| Process run | `/processes/{id}/run` | Owner | [mockup](mockups/process-run.html) | step, state, check result, owner stop, output |
| Requirements board | `/board` | Owner | [mockup](mockups/board.html) | requirement, type, agent, phase, status, remarks |
| Agent workspace | `/workspace/{agent}` | Owner | [mockup](mockups/agent-workspace.html) | agent, open work, commands, rights, recent sessions |

**Primary journey:**

1. The owner opens Processes for the selected project and picks the command he would have typed in TechieFlow.
2. The process runs, showing the step it is on, and stops where the owner has to answer.
3. A check that fails stops the run and says which check and why.
4. When it finishes, the verdicts are in the project's checklist and the run is in the measurement files.
5. The owner opens the board, sees every requirement with its status, and opens the next agent's workspace to carry on.

## 3. Requirements

### Processes

Every TechieFlow command, as something Chatur runs rather than something a harness types.

- **BRD-96** — The commands are all there. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When Processes opens, then every TechieFlow command is listed with what it does and when it was last run.
- **BRD-97** — What a process will do. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When the owner opens a process, then its steps, its checks and the points it stops for him are shown in order.
- **BRD-98** — The wording comes from the database. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When a step's wording is changed in the database, then Processes shows the new wording without any file being edited.
- **BRD-99** — Start it on this project. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When the owner starts a process on Processes, then Process run opens for the selected project and the first step begins.
- **BRD-100** — It works on the project's own documents. *Screen:* Processes · *Mockup:* [mockup](mockups/processes.html)
  - *Acceptance:* When a process writes on Processes, then it writes the project's own BRD, checklist and status documents and puts nothing else in the folder.

### Process run

One run of one process, watched as it goes.

- **BRD-101** — Where it has got to. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When Process run opens on a running process, then the current step is marked and finished steps show their result.
- **BRD-102** — It waits for the owner. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When the run reaches an owner stop on Process run, then it pauses and does nothing further until the owner answers.
- **BRD-103** — A failed check stops it. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a check fails on Process run, then the run stops on that step and shows which check failed and why.
- **BRD-104** — The run is measured. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a run ends on Process run, then a record for it is in the project's runs file with the tool named as `chatur`.
- **BRD-105** — The verdicts land in the checklist. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When a process that verifies finishes on Process run, then each requirement's verdict is in the project's checklist table.
- **BRD-106** — Carry on later. *Screen:* Process run · *Mockup:* [mockup](mockups/process-run.html)
  - *Acceptance:* When the owner answers a paused run after a restart on Process run, then it continues from that step, not from the beginning.

### Requirements board

Every requirement of the project in one place, with who owns it and where it stands.

- **BRD-107** — Everything, with its status. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When the board opens, then every requirement of the project is shown with its id, its title and its status.
- **BRD-108** — Routed to the right agent. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When a requirement's type says who owns it on Requirements board, then the board shows that agent against it.
- **BRD-109** — Narrow it down. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When the owner filters by phase, status or agent on Requirements board, then only the matching requirements are listed.
- **BRD-110** — Read one properly. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When the owner opens a requirement, then its acceptance line, its remarks and its screen are shown.
- **BRD-111** — The checklist is the truth. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When the project's checklist file changes outside Chatur on Requirements board, then the board shows the new statuses when it is opened again.
- **BRD-112** — A change on the board is a change in the file. *Screen:* Requirements board · *Mockup:* [mockup](mockups/board.html)
  - *Acceptance:* When the owner changes a status on the board, then the project's checklist file holds that status.

### Agent workspace

Each agent's own view of the project, holding only what that agent may do.

- **BRD-113** — A view of its own. *Screen:* Agent workspace · *Mockup:* [mockup](mockups/agent-workspace.html)
  - *Acceptance:* When the owner opens an agent's workspace, then it shows that agent's work on the selected project and nothing else.
- **BRD-114** — Rights decide what is offered. *Screen:* Agent workspace · *Mockup:* [mockup](mockups/agent-workspace.html)
  - *Acceptance:* When an agent lacks a right on Agent workspace, then the action needing it is not offered in its workspace.
- **BRD-115** — What is waiting. *Screen:* Agent workspace · *Mockup:* [mockup](mockups/agent-workspace.html)
  - *Acceptance:* When an agent owns open requirements, then its workspace lists them with their ids and statuses.
- **BRD-116** — Start its own command. *Screen:* Agent workspace · *Mockup:* [mockup](mockups/agent-workspace.html)
  - *Acceptance:* When the owner starts a command from a workspace, then it runs as that agent, on the selected project.
- **BRD-117** — Change agent without losing the thread. *Screen:* Agent workspace · *Mockup:* [mockup](mockups/agent-workspace.html)
  - *Acceptance:* When the owner moves to another agent's workspace on Agent workspace, then the session he left is still open.

## 4. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-09-22.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P2-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| Processes | 5 | 0 | 5 | Planned |
| Process run | 6 | 0 | 6 | Planned |
| Requirements board | 6 | 0 | 6 | Planned |
| Agent workspace | 5 | 0 | 5 | Planned |

## 5. Where the rest lives

| What | Where |
|---|---|
| Scope, users and roles, the context diagram | [phase 1 BRD](Chatur-BRD.md) |
| Non-functional requirements for the whole application | [phase 1 BRD](Chatur-BRD.md) |
| Constraints, assumptions and risks | [phase 1 BRD](Chatur-BRD.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P2-Checklist.md](Chatur-P2-Checklist.md) |
