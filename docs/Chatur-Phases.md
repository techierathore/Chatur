# Chatur — Phases

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Large |
| Date | 2026-09-21 |

## Phases

Phase 1 is one phase built in four steps, in the order the brief gives, each step ending as a nightly build the owner uses on real work before the next step begins. Step 1 brings up Sign in, Register, Start, Prerequisites and the window itself — menu bar, toolbar, the files down the left, and the build and run controls. Step 2 fills the conversation on the right and adds the providers, routing, agents and appearance tabs of Settings. Step 3 adds the agent itself — what it is doing, the changes waiting for approval, the sessions, and the corrections and measurements tabs. Step 4 adds the file tabs with their editing and the Repository view.

The shell was settled on the owner's reviews of 2026-09-21 and 2026-09-22: one window with a menu bar, files on the left, tabs in the middle and the conversation on the right. Settings holds everything that is not the work, in seven pages. BRD-156 and BRD-157 were added then, and BRD-158 (the release pipeline) on 2026-09-30, so phase 1 carries two ranges.

| Phase | Name | Screens | BRD range | Status |
|---|---|---|---|---|
| 1 | Workbench | Sign in, Register, Start, Workbench, Prerequisites, Settings, Repository | BRD-1 to BRD-95, BRD-156 to BRD-158 | done |
| 2 | Project workflow | Processes, Process run, Requirements board, Agent workspace | BRD-96 to BRD-117, BRD-187 to BRD-187 | done |
| 3 | End to end and unattended | End-to-end run, Run queue, Run report | BRD-118 to BRD-136, BRD-159 to BRD-186 | done |
| 4 | More users | Feedback and issues, Licence, All projects, Worktrees, Integrations | BRD-137 to BRD-155 | planned |

Phase 1 closed on 2026-10-06 with 97 of 98 rows Verified, the nightly download included. REQ-FN-012 (Homebrew tools found when Chatur starts from Finder on a Mac) stays open in the phase 1 checklist until a Mac can test it; the owner chose to move on to phase 2 rather than mark it N/A.

Phase 2 closed on 2026-10-07 with 22 of 22 rows Verified, on TrBlazeUI 2.1.4. Chatur's own verify process writes Verified only when a test carrying the requirement's id passed (owner decision 1, option A).

Phase 3 carries two ranges. On 2026-10-08 the owner chose to hand every process step to the agent that owns it, adding BRD-159 to BRD-179; on 2026-10-09 a "writes documents" right added BRD-180 to BRD-183 (both decisions archived in `docs/OldDocs/`). BRD-137 to BRD-155 stay with phase 4.

Phase 3 closed on 2026-10-09 with 42 of 44 rows Verified, every process step handed to the agent that owns it. Automatic routing (REQ-FN-071, REQ-FN-072) stays Blocked until TechieRag publishes its small-model router (TR-RAG-006).

On 2026-10-13 the owner's test of the 12 Oct nightly found the main window mostly a shell: placeholder menus, Settings never opening, a chat that could not answer or close. Verify had loaded routes directly and never clicked a menu (TF-011). 50 phase 1 rows and 1 phase 2 row were reopened, BRD-184 to BRD-186 added to phase 3 and BRD-187 to phase 2; all were fixed and verified the same day.
