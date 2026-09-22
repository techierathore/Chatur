# Chatur — UI Design — Phase 2: Project workflow

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 2 of 4 |

## Screens

Each of these opens as a tab in the one window, with the conversation still pinned to its right. Nothing here is a page of its own.

### Screen: Processes (`/processes`)

**Mockup:** [mockups/processes.html](mockups/processes.html) · **Roles:** Owner · **BRD:** BRD-96 to BRD-100

| Region | Control | Shows or binds |
|---|---|---|
| `processes-heading` | Button ×2 | `open-last-run`, `reload-processes` |
| `process-table` | DataTable | twelve rows: the process, what it does, `shape-*` as steps · checks · owner stops, the agent with when it last ran, and a Start link |
| `state-verify-phase` | Badge | the one running now reads "running now" and its link reads Watch |
| `steps-table` | DataTable | verify-phase's seven steps, each with a pill marking it a step, a check, or a place it stops for the owner |
| `steps-are-code-note` | Panel | the steps and the checks are code; only the wording comes from the database |
| `what-a-process-touches` | Panel | the BRD, the checklist and the status document, and nothing else put in the folder |

| Field | Type | Required | Validation |
|---|---|---|---|
| — | — | — | read-only apart from Start |

**Dialogs opened here:** none

**States:** empty: no process registered for this project — one dim row saying so, with Reload still live · loading: the twelve rows stay, with dashes where the last run will be · error: the database cannot be read — both panels give way to one line naming the file, and the Start links go

### Screen: Process run (`/processes/{id}/run`)

**Mockup:** [mockups/process-run.html](mockups/process-run.html) · **Roles:** Owner · **BRD:** BRD-101 to BRD-106

| Region | Control | Shows or binds |
|---|---|---|
| `run-heading` | Badge + Button | the process, the project, when it started, how long, the state, `pause-run`, `stop-run`, `back-to-processes` |
| `run-steps-panel` | Stepper | seven steps: three done with their results, one running with a bar, one waiting for the owner, two to come |
| `question-panel` | Card + Button ×3 | the question, and Yes, No or Answer later, with a line saying nothing further runs until it is answered |
| `failed-check` | Alert | the check that failed on an earlier run, the step it stopped on, and a link to that run |
| `run-output` | CodeBlock | the current step's output, with a copy button |
| `what-this-run-wrote` | Card | the runs record with the tool named `chatur`, the gates, the misses and the verdicts, linking to Measurements |

| Field | Type | Required | Validation |
|---|---|---|---|
| The owner's answer | three-way choice | yes, to leave the stop | Yes and No cannot be taken back for this run; Answer later can |

**Dialogs opened here:** Stop this run — a confirmation

**States:** empty: a process with no owner stop hides the question panel altogether · loading: before the first step reports, every row reads "to come" and the state reads "starting" · error: a step that throws turns its row red, the state reads "stopped", and Pause becomes a link back to Processes

### Screen: Requirements board (`/board`)

**Mockup:** [mockups/board.html](mockups/board.html) · **Roles:** Owner · **BRD:** BRD-107 to BRD-112

| Region | Control | Shows or binds |
|---|---|---|
| `board-filters` | Select ×3 + Input + Button | phase, status, agent, a search, and Reset |
| `status-counts` | Badge row | all 148, planned 27, in progress 6, verified 112, blocked 3 |
| `board-table` | DataTable | ten rows: the id with its type pill, the title, the agent with its phase, a status select, the remark |
| `req-detail` | Card | REQ-UI-007's acceptance line, remarks, screen, agent, verdict and when it was last verified |
| `board-source-note` | Panel | the board reads and writes the project's own checklist; an outside edit shows when it is opened again |

| Field | Type | Required | Validation |
|---|---|---|---|
| Search | search | no | 2 to 40 characters; an id such as REQ-UI-007, or words from a title |
| Status | select | yes | Planned, In progress, Verified or Blocked; never blank |

**Dialogs opened here:** none

**States:** empty: filters that match nothing — one dim line, with Reset live and the project totals still shown · loading: the counts read as dashes and ten placeholder rows stand in · error: the checklist is missing or malformed — the source note names the file and the line, and every status select is disabled so nothing is written back

### Screen: Agent workspace (`/workspace/{agent}`)

**Mockup:** [mockups/agent-workspace.html](mockups/agent-workspace.html) · **Roles:** Owner · **BRD:** BRD-113 to BRD-117

| Region | Control | Shows or binds |
|---|---|---|
| `agent-picker` | Button row | the four agents, the chosen one marked, each with its open-session count |
| `rights-panel` | Badge + Panel | what this agent may do, then what it may not, greyed, with a line saying an action it has no right to is not offered here at all |
| `open-work-table` | DataTable | six requirements: id, title, status and a Start button |
| `commands-panel` | Button ×4 | the agent's own commands, each with one line of what it does |
| `sessions-panel` | Card | the sessions still open, linking to the window, with a line saying changing agent leaves them open |

| Field | Type | Required | Validation |
|---|---|---|---|
| Agent | choice | yes | one of the agents in the database; an unknown one falls back to the first |

**Dialogs opened here:** none

**States:** empty: nothing open for this agent — one dim line, and the sessions panel collapses to its note · loading: the rights come first, from the agent's own definition; the work and the sessions follow · error: the agent's definition cannot be read — the pane is one line naming the agent and the file, with no command buttons, so nothing runs half-configured

## Where the rest lives

| What | Where |
|---|---|
| The window, the theme and the design system | [phase 1 UI design](Chatur-UIDesign.md) |
| The click-through flow across every phase | [phase 1 UI design](Chatur-UIDesign.md) |
| This phase's requirements | [Chatur-P2-BRD.md](Chatur-P2-BRD.md) |
