# Chatur — UI Design — Phase 3: End to end and unattended

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Medium |
| Phase | 3 of 4 |

## Screens

Each of these opens as a tab in the one window, with the conversation still pinned to its right.

**Added to Process run, a phase 2 screen (2026-10-08, BRD-159 to BRD-179).** Mockup: [mockups/process-run.html](mockups/process-run.html). A work step (not a check or an owner stop) is handed to the agent that owns the process. Process run gains one region for it, and the conversation panel carries the agent's work; nothing else on the screen changes. End-to-end run needs no new control: `running-now` already names the agent and its model, and its Stop now also ends the handed agent's session.

| Region | Control | Shows or binds |
|---|---|---|
| `handed-step` | Card + Badge + Alert (warning) + Button | the agent working the step, its model and tier, the task it was given (run, step, wording, project), the files written and commands run so far, any refused tool, its final message when it finishes, and Stop, which ends the agent's session and the run |
| `msg-handed-step` | conversation message | the handed session's messages and tool calls, live, under the agent's name with the run and step |

States of `handed-step`: empty — a step that is not handed (a check, an owner stop, verify-phase's own steps) shows no region · loading — the agent is being started: the state reads "starting", the lists are empty and Stop is live · error — the agent's session ends in an error or stalls: the region turns to the warning colour with the reason, and the stall-and-retry rule (BRD-124) applies.

**Added to Settings ▸ Agents, a phase 1 screen, and Agent workspace, a phase 2 screen (2026-10-09, BRD-180 to BRD-183).** Mockups: [mockups/settings-agents.html](mockups/settings-agents.html), [mockups/agent-workspace.html](mockups/agent-workspace.html). One new right, "writes documents" (files under `docs/` only). Nothing else on either screen changes.

| Region | Control | Shows or binds |
|---|---|---|
| `rights-panel` › `right-writes-docs` (Settings ▸ Agents) | Switch + label + state | one more line in the Rights panel, "Writes documents", drawn like the others; on for the Analyst, the Architect and the Verifier, off for the Flow master |
| `rights-panel` › `right-docs` (Agent workspace) | Pill (held) or blocked line (withheld) | "write a document under docs/" among the rights held, or among the withheld lines for an agent without it |

States: unchanged — the Rights panel's own empty, loading and error states apply to the new line.

**Added to Processes, a phase 2 screen (2026-11-10, BRD-192, BRD-195 to BRD-200, BRD-205, BRD-208, BRD-209).** Mockup: [mockups/processes.html](mockups/processes.html). Start on build-phase, verify-phase or fix-issues now opens an estimate first. Every estimate table in Chatur has the same columns: line of work, model and tier where steps differ, output tokens low · middle · high, time low · middle · high, and where the runs came from. Start writes the estimate batch; Cancel writes nothing.

| Region | Control | Shows or binds |
|---|---|---|
| `estimate-panel` | Card + DataTable + Details + Button ×2 | opens when Start is pressed on build-phase, verify-phase or fix-issues: the model and tier it was made for with a link to Routing, the phase total per line of work, each row's figures under "Each of the rows", how many rows have no precedent and are left out, "estimated after the fact" when runs on these rows already exist, and Start or Cancel |
| `estimate-none-yet` | Alert (info) | "No estimate yet" when no usable run exists anywhere it may look; Start still works |

States of the estimate: empty: "No estimate yet", never an error · loading: the table header with dashes while run records are read; Start stays enabled, since an estimate never holds a run up · error: an unreadable run record is skipped and counted under the table; a sister folder that has gone is left out and named

**Added to Process run, a phase 2 screen (2026-11-10, BRD-202, BRD-204, BRD-207).** Mockup: [mockups/process-run.html](mockups/process-run.html). The estimate the run started with, beside what it has spent.

| Region | Control | Shows or binds |
|---|---|---|
| `run-against-estimate` | Card + DataTable | tokens and time so far beside the estimate the run started with, for the model running the step; two columns, never combined |
| `handoff-estimate` | Card + DataTable | on handoff-phase: per line of work the estimate's middle and range beside the actual, apportioned where runs also worked rows outside the phase, "insufficient data (n=…)" under three runs, your own testing "not measured", and the runs that named no row counted apart and never shared out |

States of the estimate: empty: a run started before estimates existed shows "no estimate for this run" · loading: dashes in the So far column · error: a run record half written marks the actual partial

**Added to Settings ▸ Measurements and Settings ▸ Routing, phase 1 screens (2026-11-10, BRD-197, BRD-201).** Mockups: [mockups/settings-measurements.html](mockups/settings-measurements.html), [mockups/settings-routing.html](mockups/settings-routing.html). A sister project must hold `docs/metrics/runs.jsonl` and is refused with the reason otherwise; Add opens a folder picker.

| Region | Control | Shows or binds |
|---|---|---|
| `sister-panel` (Measurements) | Card + list + Button | sister projects by name, folder and run count, Remove on each, and Add a sister project |
| `stream-estimates`, `rule-estimate` (Measurements) | DataTable row + text | the sixth stream, `estimates.jsonl`, counted like the others, and the rule that an estimate never mixes with a measurement |
| `routing-estimate-note` (Routing) | Alert (info) | a tier change changes the estimate shown before the next run; a started run keeps its own |

States of the estimate: empty: "No sister projects" with Add · loading: the list with dashes for run counts · error: a folder that has gone shows as missing, with Remove

### Screen: End-to-end run (`/auto`)

**Mockup:** [mockups/auto-run.html](mockups/auto-run.html) · **Roles:** Owner · **BRD:** BRD-118 to BRD-125

| Region | Control | Shows or binds |
|---|---|---|
| `brief-panel` | Card + Select + Textarea + Button | the project and the brief the run begins from, with a line saying step-by-step working is untouched |
| `run-estimate-panel` (BRD-193) | Card + DataTable | before Start: each line of work with its model and tier, tokens and time as low · middle · high, and the runs' source; "no precedent" and "not measured" where they apply |
| `running-now` | Card + Progress + Button ×2 | the agent working, the process, the step, the requirement, how long, how far, Pause and Stop; `running-against-estimate` sets the tokens and time so far beside the estimate's low · middle · high (BRD-202) |
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
| `queue-estimate-panel` (BRD-194) | Card + DataTable + Button ×2 | before a run joins the queue: the same estimate table as End-to-end run, with Queue it and Cancel; a waiting run keeps the estimate it was queued with |
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
| `estimate-actual-panel` (BRD-203, BRD-206, BRD-207) | DataTable | beside What it cost: per line of work, the model and tier, the estimate as low · middle · high and the actual in two columns, tokens over time, each marked within, above or below; no ratio and no percentage; your own testing reads "not measured" in both |
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
| The brand mark on every screen (BRD-190): the glyph on a fixed `#B45F14` 20 px square, not the theme colour | [chatur.css](mockups/chatur.css) `.logo`, [the glyph](mockups/brand/chatur-glyph.svg) |
| This phase's requirements | [Chatur-P3-BRD.md](Chatur-P3-BRD.md) |
