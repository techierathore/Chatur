# Chatur — UI Design — Phase 3: End to end and unattended

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 3 of 4 |

## Screens

Each of these opens as a tab in the one window, with the conversation still pinned to its right.

### Screen: End-to-end run (`/auto`)

**Mockup:** [mockups/auto-run.html](mockups/auto-run.html) · **Roles:** Owner · **BRD:** BRD-118 to BRD-125

| Region | Control | Shows or binds |
|---|---|---|
| `brief-panel` | Card + Select + Textarea + Button | the project and the brief the run begins from, with a line saying step-by-step working is untouched |
| `running-now` | Card + Progress + Button ×2 | the agent working, the process, the step, the requirement, how long, how far, Pause and Stop |
| `chain-panel` | DataTable | the eight processes with the agent that runs each — four finished, one running, three to come, one marked as retried |
| `limit-note` | Alert (info) | the usage limit at 11:40, the 22 minutes waited, and the run carrying on by itself |
| `retry-note` | Alert (warning) | step 4 stalled once and was retried, and the retry is in the run record |
| `not-complete` | Card | 27 requirements still open, and that a run never reports complete while one is |

| Field | Type | Required | Validation |
|---|---|---|---|
| Project | select | yes | one of the projects Chatur knows |
| Brief | textarea | yes | at least a sentence; anything shorter is refused with "say what should be true when the run finishes" |

**Dialogs opened here:** Stop this run — a confirmation

**States:** empty: nothing running — one line offering the brief above, the bar and the two buttons hidden, and the chain shown as eight still to come · loading: the state reads "starting", the bar sweeps, the step reads "reading the checklist", and Pause and Stop wait a few seconds · error: a process that throws turns the state to stopped, names the process, the step and the last command, and offers to retry from that step; the brief stays filled

### Screen: Run queue (`/auto/queue`)

**Mockup:** [mockups/run-queue.html](mockups/run-queue.html) · **Roles:** Owner · **BRD:** BRD-126 to BRD-130

| Region | Control | Shows or binds |
|---|---|---|
| `queue-heading` | Badge + Button | one running and three waiting, Queue a run, and the way across to the run going now |
| `queue-table` | DataTable | five rows: position, project, the brief in short, the state, when it started, how long, and Pause, Resume and Remove |
| `queue-legend` | text | which action applies to which state, so a disabled button is never a mystery |
| `how-queue-behaves` | Card | one at a time; a paused run carries on from its step; a resumed run takes the open requirement furthest from verified |
| `removed-note` | Panel | a removed run is never started, and what it already did stays in its report |

| Field | Type | Required | Validation |
|---|---|---|---|
| — | — | — | queueing a run opens the same brief form, so the same two rules hold; a second brief on a project already queued warns, it is not refused |

**Dialogs opened here:** Queue a run — the brief form · Remove — a confirmation, since it cannot be undone

**States:** empty: nothing queued — a blank panel offering Queue a run, and the count reading "nothing running" · loading: five placeholder rows, the count reading as dots, every action disabled · error: the queue cannot be read — a warning above the table saying these are the last runs Chatur saw, with Check again; one broken run instead shows as lost, with Remove still live

### Screen: Run report (`/auto/report`)

**Mockup:** [mockups/run-report.html](mockups/run-report.html) · **Roles:** Owner · **BRD:** BRD-131 to BRD-134

| Region | Control | Shows or binds |
|---|---|---|
| `run-picker` | Select + Button | five earlier runs, newest first, each naming its project, its time and how it ended |
| `report-tiles` | Card ×5 | requirements touched, verified, misses, tokens, time |
| `requirements-table` | DataTable | eight of the forty-two: id, title, agent, verdict and when, with the counts in the footer |
| `misses-table` | DataTable | what it was, whose gap it was, the requirement it happened on, and whether it was fixed; a library miss goes to that library's feedback file |
| `cost-table` | DataTable | model, tier, calls, tokens in, tokens out and time, with a total row |
| `waiting-note` | Panel | the 22 minutes spent waiting on a usage limit cost nothing and are not in the total |

| Field | Type | Required | Validation |
|---|---|---|---|
| Which run | select | yes | one of the runs on record; the newest is chosen when the view opens |

**Dialogs opened here:** none

**States:** empty: no run has finished on this project — the tiles read as dashes and one line points at the end-to-end run · loading: skeleton tiles, the tables keeping their headers, the picker disabled until the record is read · error: a record missing or half written — a warning saying the run was stopped before its record was closed, the cost table showing only what was recorded, and the total marked partial

## Where the rest lives

| What | Where |
|---|---|
| The window, the theme and the design system | [phase 1 UI design](Chatur-UIDesign.md) |
| The click-through flow across every phase | [phase 1 UI design](Chatur-UIDesign.md) |
| The Settings screen whose routing tab this phase adds to | [phase 1 UI design](Chatur-UIDesign.md) |
| This phase's requirements | [Chatur-P3-BRD.md](Chatur-P3-BRD.md) |
