# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-07 |
| Waiting on | Nothing — answered 2026-10-08. |
| Answer | **Option A**, in the owner's words: "Chatur: decision 1 — go with option A. Every process step is handed to the agent that owns it; write those requirements into phase 3 and build them." |

## What happened

Phase 3 gives Chatur an end-to-end run: you type a brief, and Chatur runs the chain of eight processes by itself (day1-greenfield, split-brd, mockups, deploy-checklist, build-phase, verify-phase, fix-issues, refresh-status) and stops for your acceptance test. That works: the chain runs, survives a stalled or crashed step, can be paused, queued and resumed, and never calls itself complete while work is open.

But only one of the eight processes does real work today. verify-phase really builds the project and runs its tests (your decision of 2026-10-07). The other seven move through their steps and record "Not carried out: Chatur does not automate this step yet". No agent is started inside a run and no model is called. So an end-to-end run today produces no documents and no code: it walks the chain and verifies what is already there.

Neither the phase 2 nor the phase 3 requirements say what each step of those seven processes must actually do, so the builders left them as they are rather than invent it (logged as misses on 2026-10-07). Because nothing in a run calls a model, the requirement "a usage limit is waited out" cannot happen for real either; it is built and tested with a stand-in, and stays partial.

## What I need you to decide

### 1. What the other seven processes' steps should do

When a process step says, for example, "write the BRD" or "build the open rows", what should Chatur do?

| Option | What happens | What it costs |
|---|---|---|
| **A — Hand each step to its agent** | Each step starts the agent that owns it, with the step's wording as its task and that agent's rights and tier. The step waits for the agent, records what changed, and the process's checks judge the result. A usage limit is then waited out for real. | The largest piece: new requirements saying what "done" means for each step of each process, then building and testing them. Several days. |
| **B — Close phase 3 as a working chain, real step work later** | Phase 3 finishes as it is: the chain, the queue, the report and the safety rules. The seven processes' real work becomes its own phase, written as requirements first. The usage-limit row stays partial with that reason. | Small now. Until then, an end-to-end run only verifies; it does not build anything. |
| **C — Only the build and fix steps first** | Like A, but only build-phase and fix-issues hand their steps to the Flow master now (they make the code the run is for); the document processes come later. | Medium: about half of A. Runs can build and fix code; documents still need the step-by-step way. |

**My recommendation: B**: an unattended run that changes code needs every step's "done" written down first, or it will do the wrong work without you watching; writing those requirements is the honest next step, and the chain built now is what they will plug into.

## What I do when you answer

- A: write the step-work requirements into a phase 3 addition with the analyst, then build, test and verify them.
- B: finish phase 3 as built, verify it, and add "Real work in the seven processes" to docs/Chatur-Phases.md as its own phase.
- C: write requirements for build-phase and fix-issues steps only, build and verify them, and list the document processes for later.

## Copy this back to me

```
Chatur: decision 1 — go with option B. Close phase 3 as a working chain; make the seven processes' real step work its own phase, written as requirements first.
```

```
Chatur: decision 1 — go with option A. Every process step is handed to the agent that owns it; write those requirements into phase 3 and build them.
```

```
Chatur: decision 1 — go with option C. Only build-phase and fix-issues hand their steps to the Flow master now; documents later.
```
