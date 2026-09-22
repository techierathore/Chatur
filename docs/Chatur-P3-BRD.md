# Chatur — Business Requirements — Phase 3: End to end and unattended

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 3 of 4 |
| Status | Draft |
| Date | 2026-09-21 |

## 1. Summary

Phase 2 makes every command a process. Phase 3 chains them: one flow master command takes a brief and runs the whole chain to "ready for the owner's acceptance test", starting each role as it is needed. Step-by-step working stays the normal way — this is the extra command, not the replacement. What makes it usable is that a run survives the things that stop runs today: a usage limit, a stall, a crash. It waits and resumes, it can be queued, and it never reports complete while work is unfinished, which is the failure the owner already has with TechieFlow's unattended runner. Afterwards there is a report to read: every requirement and its verdict, every miss and whose gap it was, and the models, tokens and time the run used. Model routing also becomes automatic in this phase: a small model inside TechieRag picks the model for each piece of work, and the owner can turn that off.

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

## 4. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-09-22.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P3-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| End-to-end run | 8 | 0 | 8 | Planned |
| Run queue | 5 | 0 | 5 | Planned |
| Run report | 4 | 0 | 4 | Planned |
| Settings | 2 | 0 | 2 | Planned |

## 5. Where the rest lives

| What | Where |
|---|---|
| Scope, users and roles, the context diagram | [phase 1 BRD](Chatur-BRD.md) |
| Non-functional requirements for the whole application | [phase 1 BRD](Chatur-BRD.md) |
| Constraints, assumptions and risks | [phase 1 BRD](Chatur-BRD.md) |
| The Settings screen whose routing tab this phase adds to | [phase 1 BRD](Chatur-BRD.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P3-Checklist.md](Chatur-P3-Checklist.md) |
