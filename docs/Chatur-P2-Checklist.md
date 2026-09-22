# Chatur — Checklist (phase 2)

| | |
|---|---|
| App | Chatur |
| Size | Small |
| Phase | 2 of 4 |

## Goal

Phase 2, the project workflow: every TechieFlow command becomes a Chatur process with the same fixed steps, the same checks and the same owner stops, reading and writing the project's own documents. Work is routed to the agent that owns it, each agent gets its own view, and a board shows every requirement and where it stands. This checklist is the whole work list of the phase.

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-FN-049 | The commands are all there | Not Started | 0% | — | [view](#d-req-fn-049) |
| REQ-UI-045 | What a process will do | Not Started | 0% | — | [view](#d-req-ui-045) |
| REQ-UI-046 | The wording comes from the database | Not Started | 0% | — | [view](#d-req-ui-046) |
| REQ-FN-050 | Start it on this project | Not Started | 0% | — | [view](#d-req-fn-050) |
| REQ-FN-051 | It works on the project's own documents | Not Started | 0% | — | [view](#d-req-fn-051) |
| REQ-FN-052 | Where it has got to | Not Started | 0% | — | [view](#d-req-fn-052) |
| REQ-FN-053 | It waits for the owner | Not Started | 0% | — | [view](#d-req-fn-053) |
| REQ-UI-047 | A failed check stops it | Not Started | 0% | — | [view](#d-req-ui-047) |
| REQ-FN-054 | The run is measured | Not Started | 0% | — | [view](#d-req-fn-054) |
| REQ-UI-048 | The verdicts land in the checklist | Not Started | 0% | — | [view](#d-req-ui-048) |
| REQ-FN-055 | Carry on later | Not Started | 0% | — | [view](#d-req-fn-055) |
| REQ-UI-049 | Everything, with its status | Not Started | 0% | — | [view](#d-req-ui-049) |
| REQ-UI-050 | Routed to the right agent | Not Started | 0% | — | [view](#d-req-ui-050) |
| REQ-FN-056 | Narrow it down | Not Started | 0% | — | [view](#d-req-fn-056) |
| REQ-UI-051 | Read one properly | Not Started | 0% | — | [view](#d-req-ui-051) |
| REQ-UI-052 | The checklist is the truth | Not Started | 0% | — | [view](#d-req-ui-052) |
| REQ-FN-057 | A change on the board is a change in the file | Not Started | 0% | — | [view](#d-req-fn-057) |
| REQ-UI-053 | A view of its own | Not Started | 0% | — | [view](#d-req-ui-053) |
| REQ-FN-058 | Rights decide what is offered | Not Started | 0% | — | [view](#d-req-fn-058) |
| REQ-UI-054 | What is waiting | Not Started | 0% | — | [view](#d-req-ui-054) |
| REQ-FN-059 | Start its own command | Not Started | 0% | — | [view](#d-req-fn-059) |
| REQ-FN-060 | Change agent without losing the thread | Not Started | 0% | — | [view](#d-req-fn-060) |

**Status values:** `Not Started` · `In Progress` · `Implemented` · `Verified` · `Done (pre-existing)` · `Needs re-verify` · `PARTIAL` · `FAIL` · `Blocked` · `Owner-UAT` · `N/A`.

## Page: Processes (`/processes`)

<a id="d-req-fn-049"></a>
- **REQ-FN-049** — The commands are all there. *BRD:* BRD-96 · *Mockup:* mockups/processes.html
  - *Acceptance:* When Processes opens, then every TechieFlow command is listed with what it does and when it was last run.

<a id="d-req-ui-045"></a>
- **REQ-UI-045** — What a process will do. *BRD:* BRD-97 · *Mockup:* mockups/processes.html
  - *Acceptance:* When the owner opens a process, then its steps, its checks and the points it stops for him are shown in order.

<a id="d-req-ui-046"></a>
- **REQ-UI-046** — The wording comes from the database. *BRD:* BRD-98 · *Mockup:* mockups/processes.html
  - *Acceptance:* When a step's wording is changed in the database, then Processes shows the new wording without any file being edited.

<a id="d-req-fn-050"></a>
- **REQ-FN-050** — Start it on this project. *BRD:* BRD-99 · *Mockup:* mockups/processes.html
  - *Acceptance:* When the owner starts a process on Processes, then Process run opens for the selected project and the first step begins.

<a id="d-req-fn-051"></a>
- **REQ-FN-051** — It works on the project's own documents. *BRD:* BRD-100 · *Mockup:* mockups/processes.html
  - *Acceptance:* When a process writes on Processes, then it writes the project's own BRD, checklist and status documents and puts nothing else in the folder.

## Page: Process run (`/processes/{id}/run`)

<a id="d-req-fn-052"></a>
- **REQ-FN-052** — Where it has got to. *BRD:* BRD-101 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When Process run opens on a running process, then the current step is marked and finished steps show their result.

<a id="d-req-fn-053"></a>
- **REQ-FN-053** — It waits for the owner. *BRD:* BRD-102 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When the run reaches an owner stop on Process run, then it pauses and does nothing further until the owner answers.

<a id="d-req-ui-047"></a>
- **REQ-UI-047** — A failed check stops it. *BRD:* BRD-103 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a check fails on Process run, then the run stops on that step and shows which check failed and why.

<a id="d-req-fn-054"></a>
- **REQ-FN-054** — The run is measured. *BRD:* BRD-104 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a run ends on Process run, then a record for it is in the project's runs file with the tool named as `chatur`.

<a id="d-req-ui-048"></a>
- **REQ-UI-048** — The verdicts land in the checklist. *BRD:* BRD-105 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When a process that verifies finishes on Process run, then each requirement's verdict is in the project's checklist table.

<a id="d-req-fn-055"></a>
- **REQ-FN-055** — Carry on later. *BRD:* BRD-106 · *Mockup:* mockups/process-run.html
  - *Acceptance:* When the owner answers a paused run after a restart on Process run, then it continues from that step, not from the beginning.

## Page: Requirements board (`/board`)

<a id="d-req-ui-049"></a>
- **REQ-UI-049** — Everything, with its status. *BRD:* BRD-107 · *Mockup:* mockups/board.html
  - *Acceptance:* When the board opens, then every requirement of the project is shown with its id, its title and its status.

<a id="d-req-ui-050"></a>
- **REQ-UI-050** — Routed to the right agent. *BRD:* BRD-108 · *Mockup:* mockups/board.html
  - *Acceptance:* When a requirement's type says who owns it on Requirements board, then the board shows that agent against it.

<a id="d-req-fn-056"></a>
- **REQ-FN-056** — Narrow it down. *BRD:* BRD-109 · *Mockup:* mockups/board.html
  - *Acceptance:* When the owner filters by phase, status or agent on Requirements board, then only the matching requirements are listed.

<a id="d-req-ui-051"></a>
- **REQ-UI-051** — Read one properly. *BRD:* BRD-110 · *Mockup:* mockups/board.html
  - *Acceptance:* When the owner opens a requirement, then its acceptance line, its remarks and its screen are shown.

<a id="d-req-ui-052"></a>
- **REQ-UI-052** — The checklist is the truth. *BRD:* BRD-111 · *Mockup:* mockups/board.html
  - *Acceptance:* When the project's checklist file changes outside Chatur on Requirements board, then the board shows the new statuses when it is opened again.

<a id="d-req-fn-057"></a>
- **REQ-FN-057** — A change on the board is a change in the file. *BRD:* BRD-112 · *Mockup:* mockups/board.html
  - *Acceptance:* When the owner changes a status on the board, then the project's checklist file holds that status.

## Page: Agent workspace (`/workspace/{agent}`)

<a id="d-req-ui-053"></a>
- **REQ-UI-053** — A view of its own. *BRD:* BRD-113 · *Mockup:* mockups/agent-workspace.html
  - *Acceptance:* When the owner opens an agent's workspace, then it shows that agent's work on the selected project and nothing else.

<a id="d-req-fn-058"></a>
- **REQ-FN-058** — Rights decide what is offered. *BRD:* BRD-114 · *Mockup:* mockups/agent-workspace.html
  - *Acceptance:* When an agent lacks a right on Agent workspace, then the action needing it is not offered in its workspace.

<a id="d-req-ui-054"></a>
- **REQ-UI-054** — What is waiting. *BRD:* BRD-115 · *Mockup:* mockups/agent-workspace.html
  - *Acceptance:* When an agent owns open requirements, then its workspace lists them with their ids and statuses.

<a id="d-req-fn-059"></a>
- **REQ-FN-059** — Start its own command. *BRD:* BRD-116 · *Mockup:* mockups/agent-workspace.html
  - *Acceptance:* When the owner starts a command from a workspace, then it runs as that agent, on the selected project.

<a id="d-req-fn-060"></a>
- **REQ-FN-060** — Change agent without losing the thread. *BRD:* BRD-117 · *Mockup:* mockups/agent-workspace.html
  - *Acceptance:* When the owner moves to another agent's workspace on Agent workspace, then the session he left is still open.
