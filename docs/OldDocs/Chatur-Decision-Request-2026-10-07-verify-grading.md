# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-06 |
| Waiting on | Nothing — answered 2026-10-07. |
| Answer | **Option A**, in the owner's words: "Chatur: decision 1 — go with option A. Chatur's verify process writes Verified only when a test carrying the requirement's id passed; otherwise Needs re-verify with the reason." plus "No shortcuts." |

## What happened

Phase 2 gives Chatur its own processes, and one of them verifies a project: when it finishes, each requirement's verdict is written into that project's checklist (the "verdicts land in the checklist" requirement). The requirements and the design say where verdicts go, but not how Chatur decides one. Today the process's build and test steps are placeholders that always pass, so the first version writes `Verified` for every requirement that was `Implemented` with no warning, as long as the run reached the end and you answered yes at its approval stop. The remark it writes says so plainly, but the status still reads `Verified`.

That means a project run through Chatur could show requirements as `Verified` that were never tested. Whether that is acceptable, and what Chatur should do instead, is a product choice, so it is yours.

## What I need you to decide

### 1. When Chatur may write "Verified" into a project's checklist

What must be true before Chatur's own verify process marks a requirement verified?

| Option | What happens | What it costs |
|---|---|---|
| **A — Only on real test results** | The verify process builds the project and runs its tests; a requirement becomes `Verified` only when a test carrying its id passed. A requirement with no passing test is written as `Needs re-verify` with the reason. Your yes at the approval stop is still required before anything is written. | The build and test steps must really run the project's tools and read their results. A day or two of work, and the most honest checklist. |
| **B — Your approval, named as such** | Keep the first version, but write `Owner-approved` instead of `Verified`, so the checklist never claims a test that did not happen. Real testing comes later. | Small. The checklist needs one more status word, and the board shows it. |
| **C — Keep the first version** | `Verified` from a finished run plus your yes, with the remark saying no test stands behind it. | Nothing now. Checklists made by Chatur can overstate what was proven. |

**My recommendation: A**: a verify process that marks untested work as verified defeats its own purpose; if you want something sooner, B is the honest stop-gap.

## What I do when you answer

- A: make the build and test steps run the project's own commands, map passing tests to requirement ids, write `Verified` or `Needs re-verify` accordingly, re-test, and re-verify the row.
- B: add the `Owner-approved` status to the writer, the board and the checklist rules, re-test, and re-verify the row.
- C: nothing changes; the row stays as it is.

## Copy this back to me

```
Chatur: decision 1 — go with option A. Chatur's verify process writes Verified only when a test carrying the requirement's id passed; otherwise Needs re-verify with the reason.
```

```
Chatur: decision 1 — go with option B. Write Owner-approved instead of Verified when a verify run finishes with my approval; real testing comes later.
```

```
Chatur: decision 1 — go with option C. Keep the first version.
```
