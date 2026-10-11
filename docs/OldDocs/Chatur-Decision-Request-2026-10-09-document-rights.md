# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-08 |
| Waiting on | Nothing — answered 2026-10-09. |
| Answer | **Option A**, in the owner's words: "Chatur: decision 1 — go with option A. Add a \"writes documents\" right for files under docs/, give it to the Analyst, Architect and Verifier; source files stay as they are." |

## What happened

Your option A is built: each work step of a process is now handed to the agent that owns the process, and that agent works under its own rights (BRD-159, BRD-160). Running it showed a clash with the rights set in phase 1 and phase 2.

Four processes are owned by agents that may not write files at all: day1-greenfield and amend-docs (the Analyst) and triage-issues and metrics-report (the Verifier). Phase 1 gave the Analyst no "write to a file" right, and phase 2 took it from the Verifier ("the Verifier checks and never writes source"). So when one of their steps asks to write the BRD, a checklist row or a report, Chatur refuses it, as the rights say it must, and the process's check then fails with that reason. Those four processes can never finish today.

The rights only know one kind of writing, "write to a source file". Nothing separates writing a document under `docs/` from changing code.

## What I need you to decide

### 1. How the Analyst and the Verifier may write their documents

| Option | What happens | What it costs |
|---|---|---|
| **A — A separate right for documents** | A new right, "writes documents", covers files under `docs/` only. The Analyst, Architect and Verifier get it; "write to a source file" stays as it is, so the Verifier still never touches code. It shows on Settings ▸ Agents beside the others. | Small: one right, the guard learns the folder, Settings shows one more switch, tests. About half a day. |
| **B — Give them the source-file right** | The Analyst and the Verifier get "write to a source file" back, so they can write anything. | Smallest change, but the Verifier could then edit code, which phase 2 chose to forbid. |
| **C — Hand those four processes to the Flow master** | day1-greenfield, amend-docs, triage-issues and metrics-report are worked by the Flow master instead of their owners. | Small, but the owner shown on Processes stops matching who does the work, and the Flow master writes requirements and verdict reports. |

**My recommendation: A**: it keeps the Verifier away from code and lets each agent write only what its job produces.

## What I do when you answer

- A: add the documents right and its switch, grant it to the Analyst, Architect and Verifier, re-run the four processes live, and re-verify the rows.
- B: grant the source-file right to both, re-run the four processes, re-verify, and re-check phase 2's Agent workspace rows that assume the Verifier cannot write.
- C: move those four processes to the Flow master in the catalogue, re-run them, re-verify.

## Copy this back to me

```
Chatur: decision 1 — go with option A. Add a "writes documents" right for files under docs/, give it to the Analyst, Architect and Verifier; source files stay as they are.
```

```
Chatur: decision 1 — go with option B. Give the Analyst and the Verifier the source-file right.
```

```
Chatur: decision 1 — go with option C. Hand day1-greenfield, amend-docs, triage-issues and metrics-report to the Flow master.
```
