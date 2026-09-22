# Chatur — Checklist (phase 3)

| | |
|---|---|
| App | Chatur |
| Size | Small |
| Phase | 3 of 4 |

## Goal

Phase 3, the unattended run: one command takes a brief and runs the whole chain to ready-for-acceptance, starting each agent as it is needed. A run survives a usage limit, a stall and a crash, can be queued and resumed, and never reports complete while a requirement is open. Afterwards there is a report to read. This checklist is the whole work list of the phase.

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-FN-061 | A brief in, the whole chain run | Not Started | 0% | — | [view](#d-req-fn-061) |
| REQ-UI-055 | Each role starts when it is needed | Not Started | 0% | — | [view](#d-req-ui-055) |
| REQ-FN-062 | It stops at the acceptance test | Not Started | 0% | — | [view](#d-req-fn-062) |
| REQ-UI-056 | What is happening now | Not Started | 0% | — | [view](#d-req-ui-056) |
| REQ-FN-063 | Step by step is untouched | Not Started | 0% | — | [view](#d-req-fn-063) |
| REQ-FN-064 | A limit is waited out | Not Started | 0% | — | [view](#d-req-fn-064) |
| REQ-FN-065 | A stall or a crash is survived | Not Started | 0% | — | [view](#d-req-fn-065) |
| REQ-FN-066 | Never complete while work is open | Not Started | 0% | — | [view](#d-req-fn-066) |
| REQ-FN-067 | Queue a run | Not Started | 0% | — | [view](#d-req-fn-067) |
| REQ-UI-057 | One at a time | Not Started | 0% | — | [view](#d-req-ui-057) |
| REQ-FN-068 | Pause and start again | Not Started | 0% | — | [view](#d-req-fn-068) |
| REQ-FN-069 | Take one out | Not Started | 0% | — | [view](#d-req-fn-069) |
| REQ-FN-070 | A resumed run picks the weakest work first | Not Started | 0% | — | [view](#d-req-fn-070) |
| REQ-UI-058 | Every requirement and its verdict | Not Started | 0% | — | [view](#d-req-ui-058) |
| REQ-UI-059 | Every miss, and whose gap it was | Not Started | 0% | — | [view](#d-req-ui-059) |
| REQ-UI-060 | What it cost | Not Started | 0% | — | [view](#d-req-ui-060) |
| REQ-UI-061 | Read an earlier one | Not Started | 0% | — | [view](#d-req-ui-061) |
| REQ-FN-071 | A small model picks the model | Not Started | 0% | — | [view](#d-req-fn-071) |
| REQ-FN-072 | The owner can take it back | Not Started | 0% | — | [view](#d-req-fn-072) |

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
