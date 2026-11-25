# Chatur — Misses

| | |
|---|---|
| App | Chatur |
| Count | 896 logged: 95 open, 801 fixed, 0 will not fix |
| Source | `docs/metrics/misses.jsonl`, one row per miss record. Rewritten by `tf-misses-md.sh` on every new record. Never edit it: a wrong row is corrected by a new record. |
| Updated | 2026-11-23 |

**Whose gap** answers the four questions of the miss protocol: **the app's spec** did not say it, so the checklist line is fixed; **the framework never said it**, so one requirement line and a check are added; **the check was too weak** (a review, or a script that did not fire), so the check is fixed; **said and ignored**, so the rule becomes a hook or is deleted. **not sorted** means the record predates the sort or nobody has answered yet; `bash .tfcore/utils/tf-emit.sh --amend <miss> sort <spec|unsaid|weak-check|ignored>` completes it.

## Open (95)

| Miss | Found | Whose gap | What went wrong |
|---|---|---|---|
| MISS-Chatur-20261123-22 (REQ-NFR-019) | 2026-11-23 by owner | the app's spec | Your guide tells me to make my test folders beside my projects (C:\1MyCode\OwnerTest, EmptyOne, BrainTest); nothing that belongs to testing Chatur is made beside my projects or on the root of a drive |
| MISS-Chatur-20261123-21 (REQ-NFR-013) | 2026-11-23 by owner | the check was too weak | Your owner's test 6 says 'Solution folder already reads EmptyOne' and passed, and I still got this |
| MISS-Chatur-20261123-20 (REQ-UI-124) | 2026-11-23 by owner | the app's spec | In the same folder my first project, OwnerTest, has a project file and no solution |
| MISS-Chatur-20261123-19 (REQ-UI-125) | 2026-11-23 by owner | the app's spec | After Create, FILES showed a solution folder I did not expect; the folder I opened must stay the root of FILES |
| MISS-Chatur-20261123-18 (REQ-UI-112) | 2026-11-23 by owner | the app's spec | The dialog's folder fields are confusing: with a folder open the folder was a field to type, and nothing showed the paths that would exist |
| MISS-Chatur-20261123-17 (REQ-FN-040) | 2026-11-23 by owner | the app's spec | Chatur wrote docs/metrics with five files into my folder, and I had asked for nothing |
| MISS-Chatur-20261123-16 (REQ-FN-310) | 2026-11-23 by owner | the app's spec | I opened the empty folder ChaturOwnerTest, chose New project, name Checking-Chatur, and Chatur made ChaturOwnerTest/Checking-Chatur/Checking-Chatur.slnx and the project in ChaturOwnerTest/Checking-Chatur/Checking-Chatur: a folder inside a folder I never asked for |
| MISS-Chatur-20261123-01 (REQ-FN-313) | 2026-11-23 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261120-41 (REQ-UI-095) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-40 (REQ-UI-090) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-39 (REQ-UI-089) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-38 (REQ-UI-088) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-37 (REQ-UI-087) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-36 (REQ-UI-085) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-35 (REQ-UI-084) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-34 (REQ-UI-083) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-33 (REQ-UI-082) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-32 (REQ-UI-081) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-31 (REQ-UI-080) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-30 (REQ-UI-079) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-29 (REQ-UI-074) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-28 (REQ-UI-073) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-27 (REQ-UI-072) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-26 (REQ-UI-071) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-25 (REQ-UI-070) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-24 (REQ-UI-061) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-23 (REQ-UI-060) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-22 (REQ-UI-059) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-21 (REQ-UI-058) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-20 (REQ-UI-057) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-19 (REQ-UI-056) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-18 (REQ-UI-055) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-17 (REQ-UI-041) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-16 (REQ-UI-040) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-14 (REQ-UI-033) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-13 (REQ-UI-032) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-12 (REQ-UI-031) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-11 (REQ-UI-024) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-10 (REQ-UI-023) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-09 (REQ-UI-022) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-08 (REQ-UI-021) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-07 (REQ-UI-016) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-04 (REQ-UI-013) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-03 (REQ-UI-012) | 2026-11-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-13 (REQ-FN-060) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-12 (REQ-FN-059) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-11 (REQ-UI-054) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-10 (REQ-FN-058) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-09 (REQ-UI-053) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-08 (REQ-UI-038) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-07 (REQ-UI-037) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-06 (REQ-FN-023) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-05 (REQ-FN-021) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-04 (REQ-FN-020) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-03 (REQ-FN-019) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-02 (REQ-FN-018) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-01 (REQ-UI-018) | 2026-11-18 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-07 (REQ-UI-038) | 2026-11-13 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-06 (REQ-UI-037) | 2026-11-13 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-38 | 2026-10-22 by agent-review | not sorted | OpenCode Go models stream reasoning_content before content and TechieRag drops those deltas, so each Go chat/completions model was probed with a one-line prompt; minimax-m3 was the first whose first delta carried content, and the test provider was switched to it. |
| MISS-Chatur-20261021-38 (REQ-FN-002) | 2026-10-21 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-36 (REQ-FN-001) | 2026-10-21 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-29 (REQ-FN-030) | 2026-10-21 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-13 (REQ-FN-012) | 2026-10-21 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-04 (REQ-FN-002) | 2026-10-21 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-02 (REQ-FN-001) | 2026-10-21 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-88 (REQ-FN-048) | 2026-10-20 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-56 (REQ-FN-030) | 2026-10-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-38 (REQ-FN-022) | 2026-10-20 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-26 (REQ-FN-012) | 2026-10-20 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-91 (REQ-UI-069) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-90 (REQ-FN-083) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-89 (REQ-FN-082) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-88 (REQ-FN-081) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-87 (REQ-UI-068) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-86 (REQ-FN-080) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-85 (REQ-FN-079) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-84 (REQ-FN-078) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-83 (REQ-FN-077) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-82 (REQ-UI-067) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-81 (REQ-FN-076) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-80 (REQ-FN-075) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-79 (REQ-UI-066) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-78 (REQ-UI-065) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-77 (REQ-UI-064) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-76 (REQ-FN-074) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-75 (REQ-UI-063) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-74 (REQ-FN-073) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-73 (REQ-UI-062) | 2026-10-19 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261019-53 (REQ-FN-048) | 2026-10-19 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-07 (REQ-FN-022) | 2026-10-11 by gate | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261004-06 (REQ-NFR-006) | 2026-10-04 by gate | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-03 | 2026-10-02 by agent-review | not sorted | The Settings account page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-02 | 2026-10-02 by agent-review | not sorted | The Start mockup draws four active Get started buttons, but the app keeps them disabled and no row says which is right. |
| MISS-Chatur-20260930-36 (REQ-FN-012) | 2026-09-30 by gate | not sorted | no sentence recorded (partial-implementation, src) |

## Fixed (801)

| Miss | Found | Closed | Whose gap | What went wrong |
|---|---|---|---|---|
| MISS-Chatur-20261123-15 (REQ-NFR-017) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | Owner's test 10 typed *yolo to the Verifier, which has no *yolo, and parts 7 and 10 could not run on their own start. |
| MISS-Chatur-20261123-14 (REQ-UI-107) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | The chosen run target, model and conversation showed only a tick icon, not aria-checked or aria-selected; a click on the chosen one read as a dead item and the chat pickers after it failed the screen check. |
| MISS-Chatur-20261123-13 (REQ-UI-109) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | File > Recent projects listed the open project as a live item that did nothing when clicked. |
| MISS-Chatur-20261123-12 (REQ-UI-115) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | The measurements mockups anchored the sample data and dialogs without data-tf-state or data-tf-sample marks, and the screen had no seed, so the screen check found every region empty. |
| MISS-Chatur-20261123-11 (REQ-UI-117) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | Measurements: the Rows column wrapped one letter a line and ran off the window, the across table hid its last columns, and two side-menu entries were both called Measurements. |
| MISS-Chatur-20261123-10 (REQ-FN-312) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | The brainstorm test checked the brief's date in UTC while Chatur writes the local date, so it failed after local midnight. |
| MISS-Chatur-20261123-09 (REQ-FN-082) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | The Integrations test relied on a project left open by earlier tests; with none open Chatur rightly answered 400. |
| MISS-Chatur-20261123-08 (REQ-FN-312) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | Test cleanup deleted sessions but not their messages, so a new conversation in a new project showed a removed project's messages. |
| MISS-Chatur-20261123-07 (REQ-FN-318) | 2026-11-23 by gate | 2026-11-23 by build-phase | not sorted | Chatur set no output limit on agent calls, so the edit-file call writing a whole guide was cut off at max_tokens and dropped without a word; *devguide stopped four times with nothing written (TR-RAG-010). |
| MISS-Chatur-20261123-06 (REQ-UI-122) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | Test cleanup left AutoRunProcess rows behind, so a later end-to-end run failed on a UNIQUE key when SQLite reused the run id. |
| MISS-Chatur-20261123-05 (REQ-FN-318) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | The agent's limit of 8 tool calls per reply was too low for a guide that reads several files first. |
| MISS-Chatur-20261123-04 (REQ-FN-315) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | Agents had no way to list a folder, so *day1-brownfield could not find the code it had to read. |
| MISS-Chatur-20261123-03 (REQ-FN-317) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | In *research-prompt the Analyst announced a step and made no tool call, so the reply ended with nothing written. |
| MISS-Chatur-20261123-02 (REQ-UI-122) | 2026-11-23 by self-smoke | 2026-11-23 by build-phase | not sorted | *develop-end-to-end with a brief under 40 characters threw an ArgumentException that froze the chat instead of answering in the reply. |
| MISS-Chatur-20261122-09 (REQ-UI-008) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | Nothing is open yet offers Open folder, Open a solution and Open a file but no New project, so the owner's test 1 could not be followed |
| MISS-Chatur-20261122-08 (REQ-NFR-013) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | The owner's test and its tests start from the two states a person really meets |
| MISS-Chatur-20261122-07 (REQ-FN-312) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | In an empty folder the Analyst brainstorms with the person and writes the brief |
| MISS-Chatur-20261122-06 (REQ-UI-114) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | A reply is drawn as formatted text |
| MISS-Chatur-20261122-05 (REQ-FN-311) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | Every agent carries every command its TechieFlow agent has, done in Chatur |
| MISS-Chatur-20261122-04 (REQ-FN-310) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | New project with no solution open makes the solution and its first project from a template, and opens it |
| MISS-Chatur-20261122-03 (REQ-FN-309) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the app's spec | After New project, Run has a target |
| MISS-Chatur-20261122-02 (REQ-FN-308) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the check was too weak | The Analyst greeted with only create-project-brief and split-brd, and when asked for brainstorm said it has none; TechieFlow's Analyst has ten commands |
| MISS-Chatur-20261122-01 (REQ-UI-112) | 2026-11-22 by owner | 2026-11-22 by triage-and-fix | the check was too weak | File > New project with a folder open shows 'Makes a folder with a new, empty solution' with only a name and a folder: no templates, no framework, no first project |
| MISS-Chatur-20261121-04 (REQ-FN-040) | 2026-11-21 by owner | 2026-11-21 by log-miss | the check was too weak | Measurement write refused for sessions.jsonl: Could not create docs/metrics: Cannot create MyTasks.sln (a solution's path taken for a folder; found in the owner's log) |
| MISS-Chatur-20261121-03 (REQ-FN-308) | 2026-11-21 by owner | 2026-11-21 by log-miss | the app's spec | When I choose or change an agent, it greets me with what it is for and the commands it has. I asked the analyst and got nothing of the kind. |
| MISS-Chatur-20261121-02 (REQ-NFR-011) | 2026-11-21 by owner | 2026-11-21 by log-miss | the app's spec | Your log of my test holds a burst of Unobserved task exception errors at 20:36:46 today. Find what caused them and fix it. |
| MISS-Chatur-20261121-01 (REQ-UI-064) | 2026-11-21 by agent-review | 2026-11-21 by log-miss | the check was too weak | The phase 4 mockups were drawn without the marks the other phases' mockups carry (pop-ups open, mockup-only palette, a drawn-open dialog) and still showed 'later' pills on the phase 4 pages, so the screen check asked a fresh page for menu contents and sample rows it could never show. Marks added and |
| MISS-Chatur-20261120-53 (REQ-NFR-010) | 2026-11-20 by owner | 2026-11-20 by log-miss | the framework never said it | The test runs left about 80 projects in the test account: workbench-menus and the Start-folder test made dated project folders in the shared workbench-a fixture and never removed them, five a run. Found in about 15 minutes by grouping the account's projects by folder. |
| MISS-Chatur-20261120-52 (REQ-UI-010) | 2026-11-20 by owner | 2026-11-20 by log-miss | the check was too weak | The project switcher list (256 px) wrapped a long project name onto two lines; now one line per name, cut with an ellipsis, the whole name on hover, and at most 20 projects so it opens at once. Found in about 5 minutes from the markup. |
| MISS-Chatur-20261120-51 (REQ-UI-101) | 2026-11-20 by owner | 2026-11-20 by log-miss | the check was too weak | Every item in the branch menu was greyed out on 6 of 10 phase 3 screens: the branches were read with git only on the click that opened the menu, over a second on /mnt/c, so a menu read at once held only the disabled placeholders. Found in about 10 minutes from the code. |
| MISS-Chatur-20261120-50 (REQ-UI-073) | 2026-11-20 by agent-review | 2026-11-20 by log-miss | the check was too weak | Two project scans at once (the Start window and the menu bar's list) both inserted the same new folder and the second failed on the unique path, crashing Start. Found in about 10 minutes, when the background list of the menu fix ran beside Start on the 200-project test. |
| MISS-Chatur-20261120-49 (REQ-UI-073) | 2026-11-20 by owner | 2026-11-20 by log-miss | the check was too weak | File and Edit opened only some of the time with about 80 projects: File's pointer-down rescanned every project folder on disk on the page's event path (Edit waited on the editor the same way), so the opening click queued behind it and the test's second click shut the menu. Found in about 45 minutes |
| MISS-Chatur-20261120-48 (REQ-UI-106) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-47 (REQ-UI-105) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-46 (REQ-UI-104) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-45 (REQ-UI-103) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-44 (REQ-UI-102) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-43 (REQ-UI-101) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-42 (REQ-UI-096) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-15 (REQ-UI-039) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-06 (REQ-UI-015) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-05 (REQ-UI-014) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-02 (REQ-UI-011) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261120-01 (REQ-UI-010) | 2026-11-20 by gate | 2026-11-21 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261118-21 (REQ-FN-024) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | The agent picker: the closed picker says Analyst but the open list marks nothing as chosen (no tick, only the word suggested); the mockup draws a tick on the chosen agent |
| MISS-Chatur-20261118-20 (REQ-UI-010) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | The project list opened from the project switcher (All projects) has no way back: no close button and Escape does nothing, so the only way out is to open a project again |
| MISS-Chatur-20261118-19 (REQ-UI-008) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | Icons on the start screen and the project list: the five tiles use the same brown folder and file shapes and say nothing; the small glyph sits in the tile's corner, not its centre, and Open a solution shows a folder, not the solution mark the mockup draws |
| MISS-Chatur-20261118-18 (REQ-UI-073) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | Open a solution, Browse opens a folder picker; it must open a file picker that shows .sln and .slnx files (the dialog asks the folder picker for every way in except Open a file) |
| MISS-Chatur-20261118-17 (REQ-UI-011) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | No target found on a solution with .NET projects: a solution's saved path is its .sln file, the target search asks whether that path is a folder, it is not, so nothing is searched and the picker stays empty |
| MISS-Chatur-20261118-16 (REQ-UI-094) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | On the project list, Open a file then picking a file stopped Chatur |
| MISS-Chatur-20261118-15 (REQ-FN-006) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | Every start since stops at the same line, because Chatur reopens the last project and the files panel reads its tree again with nothing to catch the failure |
| MISS-Chatur-20261118-14 (REQ-UI-039) | 2026-11-18 by owner | 2026-11-18 by triage-and-fix | the check was too weak | Open a file then picking a file stopped Chatur at FileActions.cs line 82 (Directory.EnumerateDirectories in BuildNodes): the tree read every folder below the opened one at once and a folder Windows will not list threw out of it |
| MISS-Chatur-20261117-04 (REQ-UI-095) | 2026-11-17 by owner | 2026-11-18 by triage-and-fix | the check was too weak | In the two-account walk, the Model providers window the first account opened stayed open after its sign-out and still listed the first account's provider when the second account signed in. Cause: the second window keeps the page it loaded under the account that opened it; nothing closes or reloads i |
| MISS-Chatur-20261117-03 (REQ-NFR-010) | 2026-11-17 by owner | 2026-11-18 by triage-and-fix | the check was too weak | The desktop test proves the owner's database is untouched by reading that database's own timestamp in the owner's data folder, so every desktop test, REQ-FN-017's among them, opens a folder the owner says is never opened. |
| MISS-Chatur-20261117-02 (REQ-UI-014) | 2026-11-17 by owner | 2026-11-18 by triage-and-fix | the check was too weak | The first Run after approving a change showed the dotnet run line twice in the Output strip. Cause: a cancelled output subscription still delivers a line it had already read; neither the stream nor the strip checks the subscription is current before adding it. |
| MISS-Chatur-20261117-01 (REQ-FN-006) | 2026-11-17 by owner | 2026-11-17 by triage-and-fix | the app's spec | A new account at its first sign-in opened another account's project ChaturCheck with its files, a chat it never had and the line Continuing after Analyst's last session; Switch project listed ChaturCheck and SmokeProj. Cause: no table, no setting and no secret name carries the account, so everything |
| MISS-Chatur-20261116-04 | 2026-11-16 by owner | 2026-11-16 by log-miss | the framework never said it | The Repository screen seed wrote four empty [verify-seed] check-ins onto Chatur's own main branch (and its run/ and feature/board branches), because the test project's .git had been half deleted by the 7-day artifact sweep and git then found Chatur's repository above it; the seeds now refuse to writ |
| MISS-Chatur-20261116-03 (REQ-FN-009) | 2026-11-16 by owner | 2026-11-16 by triage-and-fix | the check was too weak | On the Windows app, Sign out chosen in the main window (a project open) leaves the window on 'Loading Chatur' until the app is closed and opened again; from the start window it works. Cause, read in the code and seen on the running app: Sign out reloads the page, the app first clears the old page aw |
| MISS-Chatur-20261116-02 (REQ-FN-047) | 2026-11-16 by owner | 2026-11-16 by triage-and-fix | the check was too weak | The Repository screen's branch menu read 'No branches found', every item disabled, once in the screen check of 15 Nov (1280 wide, light), then listed its branches a moment later on the same page. Cause, two parts. One: the screen can be drawn and its branch menu opened while its git reads are still |
| MISS-Chatur-20261116-01 (REQ-FN-024) | 2026-11-16 by owner | 2026-11-16 by triage-and-fix | the check was too weak | An earlier chat reply takes the name of whichever agent is chosen now. Cause, read in the code: a chat keeps one agent for the whole conversation (Session.RoleId), choosing another agent overwrites it (AgentActions.SetSessionRoleAsync), a stored reply (SessionEvent) has no agent of its own, and the |
| MISS-Chatur-20261115-03 (REQ-FN-024) | 2026-11-15 by owner | 2026-11-16 by triage-and-fix | the app's spec | On the Windows app, a reply written by Analyst is labelled Flow master after the agent picker is changed in the same conversation: every earlier reply takes the name of the agent chosen now. Cause: the chat names each reply from the open session's role (src/ChaturUI/Shell/ConversationPanel.razor lin |
| MISS-Chatur-20261115-02 (REQ-UI-049) | 2026-11-15 by owner | 2026-11-15 by triage-and-fix | the check was too weak | Requirements board, Processes and the three phase 3 screens: the same shape difference as on Settings / routing; the mockups draw Find, Reload, Reset, the filters and the Start buttons fully rounded and the app draws a 6 px corner |
| MISS-Chatur-20261115-01 (REQ-UI-055) | 2026-11-15 by owner | 2026-11-15 by triage-and-fix | the check was too weak | End-to-end run is blank on the first screen check after a test run. Cause: the web test copy keeps every page a test closed alive on the server for three minutes, and the app's state is one object for the whole copy, so when the screen check's seed changes the selected project every closed page relo |
| MISS-Chatur-20261114-10 (REQ-FN-061) | 2026-11-14 by owner | 2026-11-14 by triage-and-fix | the check was too weak | With 50 projects the Project list under 'Start from a brief' runs past the bottom of the window with no scroll, so the later projects cannot be picked (1280 and 390) |
| MISS-Chatur-20261114-09 (REQ-FN-057) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-08 (REQ-UI-052) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-07 (REQ-UI-051) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-06 (REQ-FN-056) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-05 (REQ-UI-050) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-04 (REQ-UI-049) | 2026-11-14 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261114-03 (REQ-FN-043) | 2026-11-14 by owner | 2026-11-14 by triage-and-fix | the check was too weak | No screen can hand the measurement writer an unknown field, so its on-app test was a permanent skip and the refusal was never seen on the running app |
| MISS-Chatur-20261114-02 (REQ-UI-008) | 2026-11-14 by owner | 2026-11-14 by triage-and-fix | the check was too weak | Its test could never start from 'no folder named': the web harness names its fixture folder on every start (Chatur.WebHarness Program.cs EnsureDefaultProjectAsync), so the test always skipped |
| MISS-Chatur-20261114-01 (REQ-FN-018) | 2026-11-14 by owner | 2026-11-15 by triage-and-fix | the check was too weak | Each agent's tier list opens 160 px wide and every option wraps onto two lines ('Tier 1 — the / strongest'); the mockup's list is at least 220 px with one line per option |
| MISS-Chatur-20261113-39 (REQ-NFR-002) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | The Windows desktop head could not start: Chatur.Core restored Microsoft.Data.Sqlite 10.0.15 under the floating 10.* while the MAUI head kept 10.0.14 |
| MISS-Chatur-20261113-38 (REQ-FN-013) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | Prerequisites stayed on Checking for 100 s when the release feed did not answer, and a feed timeout escaped as a cancellation |
| MISS-Chatur-20261113-37 (REQ-UI-036) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | Settings Measurements did not list the estimates.jsonl stream or the estimate rule the design draws |
| MISS-Chatur-20261113-36 (REQ-UI-085) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | The queue's new Estimate column pushed the row actions out of the queue card at 1280 px |
| MISS-Chatur-20261113-35 (REQ-FN-109) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | A no-precedent estimate total named 0 runs on screen instead of the runs it found |
| MISS-Chatur-20261113-34 (REQ-UI-083) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | On Processes, after Start then Cancel every Start button stayed disabled until the page was reloaded |
| MISS-Chatur-20261113-33 (REQ-UI-087) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | End-to-end run drew no 'Against the estimate' line on a going run, which the design asks for under BRD-202 |
| MISS-Chatur-20261113-32 (REQ-UI-087) | 2026-11-13 by owner | 2026-11-13 by log-miss | the check was too weak | Process run hid the so-far card while the run was working: the kept estimate was read only after the run stopped, so the tokens so far were never seen mid-run |
| MISS-Chatur-20261113-31 (REQ-UI-089) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-30 (REQ-FN-115) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-29 (REQ-UI-088) | 2026-11-13 by gate | 2026-11-13 by fix-issues | the check was too weak | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-28 (REQ-UI-084) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-27 (REQ-FN-091) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-26 (REQ-FN-089) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-25 (REQ-UI-061) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-24 (REQ-UI-060) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-23 (REQ-UI-059) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-22 (REQ-UI-058) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-21 (REQ-FN-066) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-20 (REQ-FN-065) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-19 (REQ-FN-064) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-18 (REQ-FN-063) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-17 (REQ-UI-056) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-16 (REQ-FN-062) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-15 (REQ-UI-055) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-14 (REQ-FN-061) | 2026-11-13 by gate | 2026-11-13 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-13 (REQ-FN-057) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-12 (REQ-UI-052) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-11 (REQ-UI-051) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-10 (REQ-FN-056) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-09 (REQ-UI-050) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-08 (REQ-UI-049) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261113-05 (REQ-FN-023) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-04 (REQ-FN-021) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-03 (REQ-FN-020) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-02 (REQ-FN-019) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261113-01 (REQ-FN-018) | 2026-11-13 by gate | 2026-11-15 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261110-07 (REQ-UI-087) | 2026-11-10 by agent-review | 2026-11-13 by triage-and-fix | not sorted | The Process run screen must show the tokens a running process has spent so far, but no part of Chatur records a running token count, so only elapsed time can be shown beside the estimate. |
| MISS-Chatur-20261110-06 (REQ-FN-019) | 2026-11-10 by agent-review | 2026-11-15 by triage-and-fix | not sorted | A process step picks its model from the owning role's tier and the lowest model id, ignoring the work tier set on Settings > Routing and the order of the tier's chain, so with a work tier set the estimate names a different model from the one that runs. |
| MISS-Chatur-20261110-05 (REQ-UI-049) | 2026-11-10 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Root cause: the Board phase list and the Processes chat history in the mockups are project data but were not marked data-tf-sample, so the menu check required them in the app; the agent picker's Agents item is a real command the app lacked; Process run's Pause has no BRD row. |
| MISS-Chatur-20261110-04 (REQ-UI-048) | 2026-11-10 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Root cause: the verdict writer graded only the first newest-phase checklist by name; with two phase 2 checklists (Fixture-P2 before WbAFilesApp-P2) the row's own checklist got no verdict. |
| MISS-Chatur-20261110-03 (REQ-UI-035) | 2026-11-10 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Root cause: the secondary window's title bar does not shrink; at 390px the title Measurements plus theme, palette and Close push Close 8px past the window edge. |
| MISS-Chatur-20261110-02 (REQ-FN-061) | 2026-11-10 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Root cause: the build-result and provider-state pills used TrBlazeUI's solid Destructive badge (white on the dark theme's red, 2.8 : 1); the mockup draws a soft red pill (.pill.bad), which TrBlazeUI ships as BadgeVariant.Danger. |
| MISS-Chatur-20261110-01 (REQ-FN-018) | 2026-11-10 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Root cause: the web harness seeded a provider written in code (OpenCode Go, OpenAICompatible, the Go address, two kimi models) and took only the key from user secrets; that key was refused, so the Routing seed and 13 live-model rows failed. test-settings.json's testProvider block was never read. |
| MISS-Chatur-20261104-30 (REQ-FN-091) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-29 (REQ-FN-089) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-28 (REQ-UI-061) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-27 (REQ-UI-060) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-26 (REQ-UI-059) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-25 (REQ-UI-058) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-24 (REQ-FN-070) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-23 (REQ-FN-069) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-22 (REQ-FN-068) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-21 (REQ-UI-057) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-20 (REQ-FN-067) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-19 (REQ-FN-066) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-18 (REQ-FN-065) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-17 (REQ-FN-064) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-16 (REQ-FN-063) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-15 (REQ-UI-056) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-14 (REQ-FN-062) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-13 (REQ-UI-055) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-12 (REQ-FN-061) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261104-11 (REQ-FN-060) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-10 (REQ-FN-059) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-09 (REQ-UI-054) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-08 (REQ-FN-058) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-07 (REQ-UI-053) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-06 (REQ-FN-054) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-05 (REQ-UI-047) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-04 (REQ-FN-053) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-03 (REQ-UI-037) | 2026-11-04 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261104-02 (REQ-UI-004) | 2026-11-04 by owner | 2026-11-04 by fix-issues | the check was too weak | Owner finding 1 Nov 2026: the email-already-in-use test submitted Register without first signing in with the same address in the same test (new rule: a Register submit only after a successful sign-in with that address). |
| MISS-Chatur-20261104-01 (REQ-FN-004) | 2026-11-04 by owner | 2026-11-04 by fix-issues | the check was too weak | Owner finding 1 Nov 2026: tests/verify/signin-register.spec.ts registered chatur-verify-<Date.now()>@techierathore.com on the production AppManager on every verify run (REQ-FN-004 line 96, weak-password test line 137); the owner deleted the pile of accounts by hand. A verify test never creates an ac |
| MISS-Chatur-20261029-13 (REQ-UI-042) | 2026-10-29 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261029-12 (REQ-UI-039) | 2026-10-29 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261029-11 (REQ-UI-023) | 2026-10-29 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261029-10 (REQ-FN-025) | 2026-10-29 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261029-09 (REQ-NFR-010) | 2026-10-29 by owner | 2026-10-29 by fix-issues | the app's spec | Tests never touch the owner's database |
| MISS-Chatur-20261029-08 (REQ-UI-082) | 2026-10-29 by owner | 2026-10-29 by fix-issues | the app's spec | Every button does something |
| MISS-Chatur-20261029-07 (REQ-UI-081) | 2026-10-29 by owner | 2026-10-29 by fix-issues | the app's spec | Labels carry no trailing dots |
| MISS-Chatur-20261029-06 (REQ-UI-080) | 2026-10-29 by owner | 2026-10-29 by fix-issues | the app's spec | Controls keep the mockup's inner spacing |
| MISS-Chatur-20261029-05 (REQ-UI-079) | 2026-10-29 by owner | 2026-10-29 by fix-issues | the app's spec | Type @ to attach a project file in the chat |
| MISS-Chatur-20261029-04 (REQ-UI-039) | 2026-10-29 by owner | 2026-11-10 by triage-and-fix | the check was too weak | The Files panel has no Refresh button, so files made outside Chatur do not show. |
| MISS-Chatur-20261029-03 (REQ-UI-042) | 2026-10-29 by owner | 2026-11-10 by triage-and-fix | the check was too weak | The Repository page gives an error on the ChaturCheck project (a folder with no git repository). |
| MISS-Chatur-20261029-02 (REQ-FN-025) | 2026-10-29 by owner | 2026-11-10 by triage-and-fix | the check was too weak | Pressing Enter in the chat box goes to the next line instead of sending; the box should behave like Cursor or Claude Code (Enter sends, Shift+Enter is a new line). |
| MISS-Chatur-20261029-01 (REQ-UI-023) | 2026-10-29 by owner | 2026-11-10 by triage-and-fix | the check was too weak | The chat's Stop button is always visible and does nothing; it must show only while a reply or an agent step runs, stop it, and disappear after. |
| MISS-Chatur-20261028-01 | 2026-10-28 by agent-review | 2026-10-28 by amend-docs | the app's spec | The auto-run, run-queue and run-report mockups draw sample branches, chat titles, model names and earlier runs in the shared menus without marking them as sample data, so the render check fails all 19 screen rows on data no real project has. |
| MISS-Chatur-20261026-01 (REQ-NFR-001) | 2026-10-26 by owner | 2026-10-26 by fix-issues | said and ignored | The web harness wrote a real provider key as a string literal into src/Chatur.WebHarness/Program.cs (the SaveAsync call under Chatur:TestProvider:Key); the file was in a public repository and the key had to be revoked. |
| MISS-Chatur-20261022-37 (REQ-FN-046) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-36 (REQ-FN-045) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-35 (REQ-UI-041) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-34 (REQ-FN-044) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-33 (REQ-UI-040) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-32 (REQ-UI-039) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-31 (REQ-UI-036) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-30 (REQ-UI-035) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-29 (REQ-FN-042) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-28 (REQ-FN-041) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-27 (REQ-FN-040) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-26 (REQ-UI-034) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-25 (REQ-UI-033) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-24 (REQ-FN-036) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-23 (REQ-FN-035) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-22 (REQ-FN-034) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-21 (REQ-FN-033) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-20 (REQ-UI-032) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-19 (REQ-FN-032) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-18 (REQ-FN-031) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-17 (REQ-UI-031) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-16 (REQ-UI-024) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-15 (REQ-UI-023) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-14 (REQ-UI-022) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-13 (REQ-UI-021) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-12 (REQ-FN-027) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-11 (REQ-FN-026) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-10 (REQ-FN-025) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-09 (REQ-FN-024) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-08 (REQ-FN-010) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-07 (REQ-UI-016) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-06 (REQ-UI-014) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-05 (REQ-UI-013) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-04 (REQ-FN-009) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-03 (REQ-UI-010) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-02 (REQ-FN-008) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261022-01 (REQ-FN-007) | 2026-10-22 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-99 (REQ-FN-035) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-98 (REQ-FN-034) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-97 (REQ-FN-033) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-96 (REQ-UI-032) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-95 (REQ-FN-032) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-94 (REQ-FN-031) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-93 (REQ-UI-031) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-92 (REQ-UI-024) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-91 (REQ-UI-023) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-90 (REQ-UI-022) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-89 (REQ-UI-021) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-88 (REQ-FN-027) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-87 (REQ-FN-026) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-86 (REQ-FN-025) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-85 (REQ-FN-024) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-84 (REQ-FN-010) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-83 (REQ-UI-016) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-82 (REQ-UI-014) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-81 (REQ-UI-013) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-80 (REQ-FN-009) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-79 (REQ-UI-010) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-78 (REQ-FN-008) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-77 (REQ-FN-007) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-76 (REQ-UI-074) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-75 (REQ-UI-073) | 2026-10-21 by gate | 2026-10-29 by fix-issues | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-74 (REQ-UI-074) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-73 (REQ-UI-073) | 2026-10-21 by gate | 2026-11-04 by fix-issues | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-72 (REQ-FN-091) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-71 (REQ-FN-089) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-70 (REQ-UI-061) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-69 (REQ-UI-060) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-68 (REQ-UI-059) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-67 (REQ-UI-058) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-66 (REQ-FN-070) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-65 (REQ-FN-069) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-64 (REQ-FN-068) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-63 (REQ-UI-057) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-62 (REQ-FN-067) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-61 (REQ-FN-066) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-60 (REQ-FN-065) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-59 (REQ-FN-064) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-58 (REQ-FN-063) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-57 (REQ-UI-056) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-56 (REQ-FN-062) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-55 (REQ-UI-055) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-54 (REQ-FN-061) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-53 (REQ-UI-075) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-52 (REQ-FN-057) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-51 (REQ-UI-052) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-50 (REQ-UI-051) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-49 (REQ-FN-056) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-48 (REQ-UI-050) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-47 (REQ-UI-049) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-46 (REQ-FN-055) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-45 (REQ-FN-052) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-44 (REQ-FN-051) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-43 (REQ-FN-050) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-42 (REQ-UI-046) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-41 (REQ-UI-045) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-40 (REQ-FN-049) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-39 (REQ-FN-003) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-37 (REQ-UI-002) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-35 (REQ-UI-001) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-34 (REQ-UI-048) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-33 (REQ-FN-039) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-32 (REQ-FN-038) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-31 (REQ-FN-037) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-30 (REQ-UI-034) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-28 (REQ-UI-030) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-27 (REQ-UI-029) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-26 (REQ-UI-028) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-25 (REQ-UI-027) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-24 (REQ-FN-029) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-23 (REQ-FN-028) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-22 (REQ-UI-026) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-21 (REQ-UI-025) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-20 (REQ-UI-020) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-19 (REQ-UI-019) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-18 (REQ-FN-017) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-17 (REQ-FN-016) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-16 (REQ-FN-015) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-15 (REQ-FN-014) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-14 (REQ-FN-013) | 2026-10-21 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-12 (REQ-UI-018) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-113 (REQ-FN-046) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-112 (REQ-FN-045) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-111 (REQ-UI-041) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-110 (REQ-FN-044) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-11 (REQ-UI-017) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-109 (REQ-UI-040) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-108 (REQ-UI-039) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-107 (REQ-UI-036) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-106 (REQ-UI-035) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-105 (REQ-FN-042) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-104 (REQ-FN-041) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-103 (REQ-FN-040) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-102 (REQ-UI-034) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-101 (REQ-UI-033) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-100 (REQ-FN-036) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-10 (REQ-FN-011) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-09 (REQ-UI-004) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-08 (REQ-FN-005) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-07 (REQ-UI-003) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-06 (REQ-FN-004) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261021-05 (REQ-FN-003) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-03 (REQ-UI-002) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261021-01 (REQ-UI-001) | 2026-10-21 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-87 (REQ-FN-047) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-86 (REQ-UI-044) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-85 (REQ-UI-043) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-84 (REQ-UI-042) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-83 (REQ-FN-046) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-82 (REQ-FN-045) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-81 (REQ-UI-041) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-80 (REQ-FN-044) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-79 (REQ-UI-040) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-78 (REQ-UI-039) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-77 (REQ-UI-038) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-76 (REQ-UI-037) | 2026-10-20 by gate | 2026-10-29 by fix-issues | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-75 (REQ-UI-036) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-74 (REQ-UI-035) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-73 (REQ-FN-043) | 2026-10-20 by gate | 2026-11-14 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-72 (REQ-FN-042) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-71 (REQ-FN-041) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-70 (REQ-FN-040) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-69 (REQ-FN-039) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-68 (REQ-FN-038) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-67 (REQ-FN-037) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-66 (REQ-UI-034) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-65 (REQ-UI-033) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-64 (REQ-FN-036) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-63 (REQ-FN-035) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-62 (REQ-FN-034) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-61 (REQ-FN-033) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-60 (REQ-UI-032) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-59 (REQ-FN-032) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-58 (REQ-FN-031) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-57 (REQ-UI-031) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-55 (REQ-UI-030) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-54 (REQ-UI-029) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-53 (REQ-UI-028) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-52 (REQ-UI-027) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-51 (REQ-FN-029) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-50 (REQ-FN-028) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-49 (REQ-UI-026) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-48 (REQ-UI-025) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-47 (REQ-UI-024) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-46 (REQ-UI-023) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-45 (REQ-UI-022) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-44 (REQ-UI-021) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-43 (REQ-FN-027) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-42 (REQ-FN-026) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-41 (REQ-FN-025) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-40 (REQ-FN-024) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-39 (REQ-FN-023) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-37 (REQ-FN-021) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-36 (REQ-FN-020) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-35 (REQ-FN-019) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-34 (REQ-FN-018) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-33 (REQ-UI-020) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-32 (REQ-UI-019) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-31 (REQ-FN-017) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-30 (REQ-FN-016) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-29 (REQ-FN-015) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-28 (REQ-FN-014) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-27 (REQ-FN-013) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-25 (REQ-UI-018) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-24 (REQ-UI-017) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-23 (REQ-FN-011) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-22 (REQ-FN-010) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-21 (REQ-UI-016) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-20 (REQ-UI-015) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-19 (REQ-UI-014) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-18 (REQ-UI-013) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-17 (REQ-UI-012) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-16 (REQ-UI-011) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-15 (REQ-FN-009) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-14 (REQ-UI-010) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-13 (REQ-FN-008) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-12 (REQ-FN-007) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-11 (REQ-UI-009) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-10 (REQ-UI-008) | 2026-10-20 by gate | 2026-11-14 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-09 (REQ-FN-006) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-08 (REQ-UI-007) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-07 (REQ-UI-006) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-06 (REQ-UI-005) | 2026-10-20 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261020-05 (REQ-UI-004) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-04 (REQ-FN-005) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-03 (REQ-UI-003) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-02 (REQ-FN-004) | 2026-10-20 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261020-01 (REQ-NFR-004) | 2026-10-20 by owner | 2026-11-10 by triage-and-fix | the check was too weak | the app quits on start on Mac Catalyst: the DbUp console logger throws PlatformNotSupportedException and hides a migration error |
| MISS-Chatur-20261019-99 (REQ-UI-047) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-98 (REQ-FN-053) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-97 (REQ-FN-052) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-96 (REQ-FN-051) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-95 (REQ-FN-050) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-94 (REQ-UI-046) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-93 (REQ-UI-045) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-92 (REQ-FN-049) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-72 (REQ-FN-091) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-71 (REQ-FN-089) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-70 (REQ-UI-061) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-69 (REQ-UI-060) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-68 (REQ-UI-059) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-67 (REQ-UI-058) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-66 (REQ-FN-070) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-65 (REQ-FN-069) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-64 (REQ-FN-068) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-63 (REQ-UI-057) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-62 (REQ-FN-067) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-61 (REQ-FN-066) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-60 (REQ-FN-065) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-59 (REQ-FN-064) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-58 (REQ-FN-063) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-57 (REQ-UI-056) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-56 (REQ-FN-062) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-55 (REQ-UI-055) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-54 (REQ-FN-061) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-52 (REQ-FN-047) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-51 (REQ-UI-044) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-50 (REQ-UI-043) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-49 (REQ-UI-042) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-48 (REQ-FN-046) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-47 (REQ-FN-045) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-46 (REQ-UI-041) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-45 (REQ-FN-044) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-44 (REQ-UI-040) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-43 (REQ-UI-039) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-42 (REQ-UI-038) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-41 (REQ-UI-037) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-40 (REQ-UI-036) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-39 (REQ-UI-035) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-38 (REQ-FN-043) | 2026-10-19 by gate | 2026-11-14 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-37 (REQ-FN-042) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-36 (REQ-FN-041) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-35 (REQ-FN-040) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-34 (REQ-UI-033) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-33 (REQ-FN-036) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-32 (REQ-FN-035) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-31 (REQ-FN-034) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-30 (REQ-FN-033) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-29 (REQ-UI-032) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-28 (REQ-FN-032) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-27 (REQ-FN-031) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-26 (REQ-UI-031) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-25 (REQ-UI-024) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-24 (REQ-UI-023) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-23 (REQ-UI-022) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-22 (REQ-UI-021) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-21 (REQ-FN-027) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-20 (REQ-FN-026) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-19 (REQ-FN-025) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-18 (REQ-FN-024) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-17 (REQ-FN-010) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-16 (REQ-UI-016) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-15 (REQ-UI-015) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-14 (REQ-UI-014) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-13 (REQ-UI-013) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-12 (REQ-UI-012) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-114 (REQ-UI-075) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-113 (REQ-FN-060) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-112 (REQ-FN-059) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-111 (REQ-UI-054) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-110 (REQ-FN-058) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-11 (REQ-UI-011) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-109 (REQ-UI-053) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-108 (REQ-FN-057) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-107 (REQ-UI-052) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-106 (REQ-UI-051) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-105 (REQ-FN-056) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-104 (REQ-UI-050) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-103 (REQ-UI-049) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-102 (REQ-FN-055) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-101 (REQ-UI-048) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-100 (REQ-FN-054) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-10 (REQ-FN-009) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-09 (REQ-UI-010) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-08 (REQ-FN-008) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-07 (REQ-FN-007) | 2026-10-19 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-06 (REQ-UI-009) | 2026-10-19 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-05 (REQ-UI-008) | 2026-10-19 by gate | 2026-11-14 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-04 (REQ-FN-006) | 2026-10-19 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-03 (REQ-UI-007) | 2026-10-19 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-02 (REQ-UI-006) | 2026-10-19 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261019-01 (REQ-UI-005) | 2026-10-19 by gate | 2026-11-13 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261013-56 (REQ-FN-053) | 2026-10-13 by agent-review | 2026-10-13 by fix-issues | the check was too weak | REQ-FN-053 and REQ-FN-055 were Verified on a unit test about reading ids from test names, whose sample data held their ids; neither had a test of its own (TF-012). |
| MISS-Chatur-20261013-55 (REQ-UI-073) | 2026-10-13 by agent-review | 2026-10-13 by fix-issues | the check was too weak | Tools > Settings shows Ctrl+, as its shortcut but nothing handled the key, so pressing it did nothing. |
| MISS-Chatur-20261013-54 (REQ-UI-023) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | After a finished reply the conversation shows 'Stopped by the owner.' under it although nobody pressed Stop: the activity panel draws every activity step of the session beneath the newest reply instead of in time order, so a stop recorded on 12 Oct appears under today's answer (owner screenshots, ni |
| MISS-Chatur-20261013-53 (REQ-UI-075) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the app's spec | Edit a process |
| MISS-Chatur-20261013-52 (REQ-FN-106) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the app's spec | Settings and the other pages open from the main window |
| MISS-Chatur-20261013-51 (REQ-UI-074) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the app's spec | Close the conversation and bring it back |
| MISS-Chatur-20261013-50 (REQ-UI-073) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the app's spec | The menu bar's menus do their jobs |
| MISS-Chatur-20261013-49 (REQ-UI-049) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | Board says 'No project is open' while SmokeProj is open; the real cause is that the project's folder cannot be found, and the Agent workspace then shows 0 requirements with no reason (owner, nightly 12 Oct). Verify passed it on a seeded project whose folder exists. |
| MISS-Chatur-20261013-48 (REQ-FN-044) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The File, Edit and View menus show only a disabled 'Nothing to do yet' (MenuBar.razor lines 48, 54, 60) and Help only a disabled 'About Chatur'; the UI Design and main.html draw New/Open/Clone/Save/Close, Undo/Cut/Copy/Paste/Find, Files/Chat/Output/Prerequisites/Repository/Appearance (owner, nightly |
| MISS-Chatur-20261013-47 (REQ-FN-007) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The File, Edit and View menus show only a disabled 'Nothing to do yet' (MenuBar.razor lines 48, 54, 60) and Help only a disabled 'About Chatur'; the UI Design and main.html draw New/Open/Clone/Save/Close, Undo/Cut/Copy/Paste/Find, Files/Chat/Output/Prerequisites/Repository/Appearance (owner, nightly |
| MISS-Chatur-20261013-46 (REQ-UI-021) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The chat can never answer in the desktop app: no model provider can be configured because Settings does not open, so every message gets 'No model provider is configured yet' (owner, nightly 12 Oct). Verify passed it in the web harness with a provider seeded from user secrets, never through Settings. |
| MISS-Chatur-20261013-45 (REQ-FN-027) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The chat can never answer in the desktop app: no model provider can be configured because Settings does not open, so every message gets 'No model provider is configured yet' (owner, nightly 12 Oct). Verify passed it in the web harness with a provider seeded from user secrets, never through Settings. |
| MISS-Chatur-20261013-44 (REQ-FN-026) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The chat can never answer in the desktop app: no model provider can be configured because Settings does not open, so every message gets 'No model provider is configured yet' (owner, nightly 12 Oct). Verify passed it in the web harness with a provider seeded from user secrets, never through Settings. |
| MISS-Chatur-20261013-43 (REQ-FN-025) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | The chat can never answer in the desktop app: no model provider can be configured because Settings does not open, so every message gets 'No model provider is configured yet' (owner, nightly 12 Oct). Verify passed it in the web harness with a provider seeded from user secrets, never through Settings. |
| MISS-Chatur-20261013-42 (REQ-FN-048) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-41 (REQ-FN-047) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-40 (REQ-UI-044) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-39 (REQ-UI-043) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-38 (REQ-UI-042) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-37 (REQ-UI-038) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-36 (REQ-UI-037) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-35 (REQ-UI-036) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-34 (REQ-UI-035) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-33 (REQ-FN-043) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-32 (REQ-FN-042) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-31 (REQ-FN-041) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-30 (REQ-FN-040) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-29 (REQ-FN-039) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-28 (REQ-FN-038) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-27 (REQ-FN-037) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-26 (REQ-UI-034) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-25 (REQ-FN-030) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-24 (REQ-UI-030) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-23 (REQ-UI-029) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-22 (REQ-UI-028) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-21 (REQ-UI-027) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-20 (REQ-FN-029) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-19 (REQ-FN-028) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-18 (REQ-UI-025) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-17 (REQ-FN-023) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-16 (REQ-FN-022) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-15 (REQ-FN-021) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-14 (REQ-FN-020) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-13 (REQ-FN-019) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-12 (REQ-FN-018) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-11 (REQ-UI-020) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-10 (REQ-UI-019) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-09 (REQ-FN-017) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-08 (REQ-FN-016) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-07 (REQ-FN-015) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-06 (REQ-FN-014) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-05 (REQ-FN-013) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-04 (REQ-FN-012) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-03 (REQ-UI-018) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-02 (REQ-UI-017) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261013-01 (REQ-FN-011) | 2026-10-13 by owner | 2026-10-13 by fix-issues | the check was too weak | In the desktop app the menu item that opens this screen does nothing: Settings…, Prerequisites… and Repository… open no window (owner, nightly 12 Oct; reproduced on the Windows head 13 Oct). The secondary window is never opened — IWindowService.ShowSecondaryWindow has no caller. Verify passed it bec |
| MISS-Chatur-20261011-08 (REQ-FN-023) | 2026-10-11 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-06 (REQ-FN-021) | 2026-10-11 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-05 (REQ-FN-020) | 2026-10-11 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-04 (REQ-FN-019) | 2026-10-11 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-03 (REQ-FN-018) | 2026-10-11 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261011-02 (REQ-FN-072) | 2026-10-11 by agent-review | 2026-11-10 by triage-and-fix | the app's spec | The routing mockup still draws automatic routing as a disabled switch marked later, while REQ-FN-071 and REQ-FN-072 need it working. |
| MISS-Chatur-20261011-01 (REQ-FN-071) | 2026-10-11 by agent-review | 2026-11-10 by triage-and-fix | the app's spec | The BRD and the routing mockup never say which model is the small model that picks, so the builder used the first model of the lowest tier. |
| MISS-Chatur-20261008-24 (REQ-UI-071) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | the app's spec | The conversation reopened an agent's step session as the owner's own, showing the step task as You and clipping its long text at 390. |
| MISS-Chatur-20261008-23 (REQ-FN-091) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-22 (REQ-FN-089) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-21 (REQ-FN-084) | 2026-10-08 by agent-review | 2026-10-09 by amend-docs | not sorted | Phase 1 rights give the Analyst and the Verifier no file-writing right, so the processes they own in phase 3 (day1-greenfield, amend-docs, triage-issues, metrics-report) cannot write their documents when handed to them. |
| MISS-Chatur-20261008-20 (REQ-FN-084) | 2026-10-08 by agent-review | 2026-11-10 by triage-and-fix | not sorted | The BRD does not define which documents count as touched for amend-docs' check. |
| MISS-Chatur-20261008-19 (REQ-FN-084) | 2026-10-08 by agent-review | 2026-11-10 by triage-and-fix | not sorted | The BRD does not define tokens and failures for metrics-report's recomputed totals. |
| MISS-Chatur-20261008-18 (REQ-FN-084) | 2026-10-08 by agent-review | 2026-11-10 by triage-and-fix | not sorted | The BRD does not say which files the per-process checks read for issue reports (docs/issues/ was taken as the reports folder) for triage-issues. |
| MISS-Chatur-20261008-17 (REQ-UI-061) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-16 (REQ-UI-060) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-15 (REQ-UI-059) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-14 (REQ-UI-058) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-13 (REQ-FN-070) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-12 (REQ-FN-069) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-11 (REQ-FN-068) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-10 (REQ-UI-057) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-09 (REQ-FN-067) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-08 (REQ-FN-066) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-07 (REQ-FN-065) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-06 (REQ-FN-064) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-05 (REQ-FN-063) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-04 (REQ-UI-056) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-03 (REQ-FN-062) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-02 (REQ-UI-055) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261008-01 (REQ-FN-061) | 2026-10-08 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261007-12 (REQ-UI-060) | 2026-10-08 by agent-review | 2026-11-10 by triage-and-fix | the framework never said it | The BRD never said how a run's time is measured when it waits out a usage limit, so the report counts the run's time from start to end less the waits, and shows tokens only from runs.jsonl records inside its window. |
| MISS-Chatur-20261007-11 (REQ-UI-061) | 2026-10-08 by agent-review | 2026-11-10 by triage-and-fix | the framework never said it | The BRD never said which runs the Run report lists once Chatur carries out its own end-to-end runs, so it lists the project's end-to-end runs newest first and, beside them, runs recorded as auto in runs.jsonl. |
| MISS-Chatur-20261007-10 (REQ-FN-061) | 2026-10-07 by agent-review | 2026-11-10 by triage-and-fix | the framework never said it | The auto-run mockup names create-project-brief and devguide in its chain, which are not among Chatur's twelve processes, so the chain uses day1-greenfield and mockups in their places. |
| MISS-Chatur-20261007-09 (REQ-FN-061) | 2026-10-07 by agent-review | 2026-11-10 by triage-and-fix | the framework never said it | The BRD never said what an owner stop inside the chain does in an unattended run, so the run answers it yes itself and writes that into its record; only the acceptance test is left to the owner. |
| MISS-Chatur-20261007-08 (REQ-UI-060) | 2026-10-07 by agent-review | 2026-11-10 by triage-and-fix | the framework never said it | The BRD never said where a run's models, calls, requirement verdicts and waiting time are recorded, so the report reads the project's runs, gates and misses streams and shows a dash where a figure is not there. |
| MISS-Chatur-20261007-07 (REQ-FN-070) | 2026-10-07 by self-smoke | 2026-11-10 by triage-and-fix | not sorted | The BRD does not say what a paused run does when another run is going at resume, or how Implemented and In Progress rank beside blocked, partial and untouched. |
| MISS-Chatur-20261007-06 (REQ-UI-047) | 2026-10-07 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261007-05 (REQ-UI-047) | 2026-10-07 by agent-review | 2026-11-10 by triage-and-fix | not sorted | The Process run mockup put the owner stop at step 5 and drew step 4 still running while it waited; the Processes mockup puts the stop at step 6. |
| MISS-Chatur-20261007-04 (REQ-UI-048) | 2026-10-07 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261007-03 (REQ-FN-052) | 2026-10-07 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261007-02 (REQ-FN-052) | 2026-10-07 by self-smoke | 2026-11-10 by triage-and-fix | the app's spec | The BRD gives the eleven processes other than verify-phase no real step work, so once the always-passing executor was removed their steps can only say they are not automated yet. |
| MISS-Chatur-20261007-01 (REQ-UI-048) | 2026-10-07 by self-smoke | 2026-11-10 by triage-and-fix | the app's spec | The BRD does not say what percentage a Needs re-verify verdict carries, or whether a skipped test, a flaky test or a test command that could not run may still verify a requirement. |
| MISS-Chatur-20261006-27 (REQ-UI-026) | 2026-10-06 by agent-review | 2026-10-13 by fix-issues | not sorted | Phase 2 added five agent rights to the database, and Settings Agents still shows only the original four, so a role's full rights are no longer visible. |
| MISS-Chatur-20261006-26 (REQ-FN-060) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-25 (REQ-FN-059) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-24 (REQ-UI-054) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-23 (REQ-FN-058) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-22 (REQ-UI-053) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-21 (REQ-FN-057) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-20 (REQ-UI-052) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-19 (REQ-UI-051) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-18 (REQ-FN-056) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-17 (REQ-UI-050) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-16 (REQ-UI-049) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-15 (REQ-FN-055) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-14 (REQ-UI-048) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-13 (REQ-FN-054) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-12 (REQ-UI-047) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-11 (REQ-FN-053) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-10 (REQ-FN-052) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-09 (REQ-FN-051) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-08 (REQ-FN-050) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-07 (REQ-UI-046) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-06 (REQ-UI-045) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-05 (REQ-FN-049) | 2026-10-06 by gate | 2026-11-10 by triage-and-fix | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261006-04 (REQ-UI-048) | 2026-10-06 by agent-review | 2026-11-10 by triage-and-fix | not sorted | BRD-105 says each requirement's verdict lands in the checklist but not where a verdict comes from, so a run grades every Implemented requirement without a recorded defect on its checks and the owner's approval. |
| MISS-Chatur-20261006-03 (REQ-NFR-006) | 2026-10-06 by owner | 2026-10-06 by fix-issues | not sorted | I told the owner to add an App Manager secret, but a downloadable app must use an installed-app key with no secret and send its device id on every call, as the updated App Manager guide says. |
| MISS-Chatur-20261006-02 (REQ-NFR-006) | 2026-10-06 by agent-review | 2026-10-06 by build-phase | not sorted | The downloaded nightly hung on Loading Chatur because it had no App Manager address and the sign-in check swallowed the error. |
| MISS-Chatur-20261006-01 (REQ-UI-018) | 2026-10-06 by agent-review | 2026-10-06 by build-phase | not sorted | A released build showed its version as 0.1 and offered itself as a newer nightly, because it read the version from the app's own file and compared a short commit with a full one. |
| MISS-Chatur-20261004-05 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build selected Xcode through an alias folder, and the asset compiler could not find the macOS SDK there. |
| MISS-Chatur-20261004-04 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build picked Xcode 26.5 from the workload's name, but that workload needs Xcode 26.6, which its own version file states. |
| MISS-Chatur-20261004-03 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac app could not build: it references the ChaturDb console project, which a self-contained Mac app may not reference, and the Mac target had never been compiled before the push. |
| MISS-Chatur-20261004-02 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The second Mac build fix chose the Xcode from the newest Mac workload folder on the runner instead of the pinned one, so the build still failed. |
| MISS-Chatur-20261004-01 (REQ-NFR-006) | 2026-10-04 by owner | 2026-10-04 by fix-issues | not sorted | The Mac build on GitHub failed because the workflow took the newest .NET workload, which needs an Xcode the runner does not have. |
| MISS-Chatur-20261003-25 (REQ-FN-039) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-24 (REQ-FN-038) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-23 (REQ-FN-037) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-22 (REQ-UI-009) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-21 (REQ-UI-008) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-20 (REQ-UI-007) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-19 (REQ-UI-006) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-18 (REQ-UI-005) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-17 (REQ-FN-043) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-16 (REQ-FN-042) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-15 (REQ-FN-041) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-14 (REQ-FN-040) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-13 (REQ-FN-039) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-12 (REQ-FN-038) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-11 (REQ-FN-037) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261003-10 (REQ-UI-034) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-09 (REQ-FN-023) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-08 (REQ-FN-021) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-07 (REQ-FN-020) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-06 (REQ-FN-019) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-05 (REQ-FN-018) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-04 (REQ-FN-017) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-03 (REQ-FN-016) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-02 (REQ-FN-015) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261003-01 (REQ-FN-014) | 2026-10-03 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-66 (REQ-FN-019) | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Routing mockup anchors the items of every tier menu as if the menus were always open, without marking them as another state, so the page can never show them on first view. |
| MISS-Chatur-20261002-65 (REQ-UI-038) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-64 (REQ-UI-037) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-63 (REQ-UI-036) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-62 (REQ-UI-035) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-61 (REQ-FN-043) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-60 (REQ-FN-042) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-59 (REQ-FN-041) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-58 (REQ-FN-040) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-57 (REQ-FN-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-56 (REQ-UI-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-55 (REQ-UI-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-54 (REQ-UI-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-53 (REQ-UI-027) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-52 (REQ-FN-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-51 (REQ-FN-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-50 (REQ-UI-026) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-49 (REQ-UI-025) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-48 (REQ-FN-023) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-47 (REQ-FN-021) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-46 (REQ-FN-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-45 (REQ-FN-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-44 (REQ-FN-018) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-43 (REQ-UI-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-42 (REQ-UI-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-41 (REQ-FN-017) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-40 (REQ-FN-016) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-39 (REQ-FN-015) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-38 (REQ-FN-014) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-37 (REQ-UI-009) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-36 (REQ-UI-008) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-35 (REQ-FN-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-34 (REQ-UI-007) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-33 (REQ-UI-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-32 (REQ-UI-005) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20261002-31 (REQ-FN-004) | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Register test creates a new real App Manager account every time it runs, instead of using a UsageGuide test user. |
| MISS-Chatur-20261002-30 | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Appearance mockup draws a text panel with font size and line height controls that no requirement asks for and no action supports. |
| MISS-Chatur-20261002-29 (REQ-UI-037) | 2026-10-02 by agent-review | 2026-10-03 by amend-docs | not sorted | The Appearance mockup anchors the light, dark and follow-system choices inside a closed menu without marking them as another state, so the page can never show them on first view. |
| MISS-Chatur-20261002-28 (REQ-UI-038) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-27 (REQ-UI-037) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-26 (REQ-UI-036) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-25 (REQ-UI-035) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-24 (REQ-UI-034) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-23 (REQ-UI-030) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-22 (REQ-UI-029) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-21 (REQ-UI-028) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-20 (REQ-UI-027) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-19 (REQ-UI-026) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-18 (REQ-UI-025) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-17 (REQ-UI-020) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-16 (REQ-UI-019) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-15 (REQ-UI-009) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-14 (REQ-UI-008) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-13 (REQ-UI-007) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-12 (REQ-UI-006) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-11 (REQ-UI-005) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-10 (REQ-UI-004) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-09 (REQ-UI-003) | 2026-10-02 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (regression, src) |
| MISS-Chatur-20261002-08 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings routing page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-07 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings measurements page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-06 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings corrections page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-05 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings appearance page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-04 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Settings agents page is missing controls its mockup draws, and no verify ever drove Settings to notice it. |
| MISS-Chatur-20261002-01 | 2026-10-02 by agent-review | 2026-10-03 by build-phase | not sorted | The Measurements mockup draws a Where they are written panel that no checklist row asks for, so it was never built. |
| MISS-Chatur-20261001-01 (REQ-UI-037) | 2026-10-01 by gate | 2026-10-02 by amend-docs | the app's spec | The UIDesign says Amber and dark are the default look, but every mockup draws Indigo and light by default (docs/mockups/chatur.css), so the screen comparison reports a colour difference on every accented control. |
| MISS-Chatur-20260930-97 | 2026-09-30 by gate | 2026-10-01 by amend-docs | the app's spec | None of the 27 mockups mark popup contents, sample rows or later-phase links as state-only or mockup-only, so the render check reports about 210 controls as missing on every screen and no screen row can reach Verified until the mockups are annotated. |
| MISS-Chatur-20260930-96 (REQ-FN-048) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-95 (REQ-FN-047) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-94 (REQ-UI-044) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-93 (REQ-UI-043) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-92 (REQ-UI-042) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-91 (REQ-FN-046) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-90 (REQ-FN-045) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-89 (REQ-UI-041) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-88 (REQ-FN-044) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-87 (REQ-UI-040) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-86 (REQ-UI-039) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-85 (REQ-UI-038) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-84 (REQ-UI-037) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-83 (REQ-UI-036) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-82 (REQ-UI-035) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-81 (REQ-FN-043) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-80 (REQ-FN-042) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-79 (REQ-FN-041) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-78 (REQ-FN-040) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-77 (REQ-FN-039) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-76 (REQ-FN-038) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-75 (REQ-FN-037) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-74 (REQ-UI-034) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-73 (REQ-UI-033) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-72 (REQ-FN-036) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-71 (REQ-FN-035) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-70 (REQ-FN-034) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-69 (REQ-FN-033) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-68 (REQ-UI-032) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-67 (REQ-FN-032) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-66 (REQ-FN-031) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-65 (REQ-UI-031) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-64 (REQ-FN-030) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-63 (REQ-UI-030) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-62 (REQ-UI-029) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-61 (REQ-UI-028) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-60 (REQ-UI-027) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-59 (REQ-FN-029) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-58 (REQ-FN-028) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-57 (REQ-UI-026) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-56 (REQ-UI-025) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-55 (REQ-UI-024) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-54 (REQ-UI-023) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-53 (REQ-UI-022) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-52 (REQ-UI-021) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-51 (REQ-FN-027) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-50 (REQ-FN-026) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-49 (REQ-FN-025) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-48 (REQ-FN-024) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-47 (REQ-FN-023) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-46 (REQ-FN-022) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-45 (REQ-FN-021) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-44 (REQ-FN-019) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-43 (REQ-FN-018) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-42 (REQ-UI-020) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-41 (REQ-UI-019) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-40 (REQ-FN-017) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-39 (REQ-FN-016) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-38 (REQ-FN-015) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-37 (REQ-FN-013) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-35 (REQ-UI-018) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-34 (REQ-UI-017) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-33 (REQ-FN-011) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-32 (REQ-FN-010) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-31 (REQ-UI-016) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-30 (REQ-UI-015) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-29 (REQ-UI-014) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-28 (REQ-UI-013) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-27 (REQ-UI-012) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-26 (REQ-UI-011) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-25 (REQ-FN-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-24 (REQ-UI-010) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-23 (REQ-FN-008) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-22 (REQ-FN-007) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-21 (REQ-UI-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-20 (REQ-UI-008) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-19 (REQ-FN-006) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-18 (REQ-UI-007) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-17 (REQ-UI-006) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-16 (REQ-UI-005) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-15 (REQ-UI-004) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-14 (REQ-FN-005) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-13 (REQ-UI-003) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-12 (REQ-FN-004) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-11 (REQ-FN-003) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-10 (REQ-FN-002) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-09 (REQ-UI-002) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-08 (REQ-FN-001) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-07 (REQ-UI-001) | 2026-09-30 by gate | 2026-10-03 by build-phase | not sorted | no sentence recorded (partial-implementation, src) |
| MISS-Chatur-20260930-06 (REQ-UI-009) | 2026-09-30 by gate | 2026-10-03 by build-phase | the check was too weak | Removing a project folder failed with a raw FOREIGN KEY database error once any project in it had been opened, because its projects were deleted outright while sessions still pointed at them. |
| MISS-Chatur-20260930-05 (REQ-FN-040) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | Nothing in Chatur ever writes a measurement: MeasurementActions.AppendAsync has no caller, so an agent session leaves no docs/metrics files in the project. |
| MISS-Chatur-20260930-04 (REQ-UI-034) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | No part of Chatur ever records a correction: there is no agent tool that changes a role, rule or step wording and logs it, so the Corrections page can only ever be empty. |
| MISS-Chatur-20260930-03 (REQ-FN-020) | 2026-09-30 by gate | 2026-10-01 by build-phase | the check was too weak | Routing rows (role tiers, work tiers, chain order, remembered) were marked Implemented at 100% while the Routing page had no controls at all, only three empty tier cards. |
| MISS-Chatur-20260930-02 (REQ-FN-014) | 2026-09-30 by agent-review | 2026-10-01 by build-phase | the check was too weak | Connecting a provider was marked Implemented and live-smoked, but the Add a provider button was disabled, the dialog was never built, and the OpenAI-compatible connector was refused, so the owner could not connect any model. |
| MISS-Chatur-20260930-01 (REQ-NFR-006) | 2026-09-30 by owner | 2026-10-06 by fix-issues | the app's spec | BRD §2 Scope and Technical-Decisions 'Release' promised a nightly download from every push to main, but no BRD requirement carried the release pipeline, so no checklist row built or verified it (added as BRD-158 / REQ-NFR-006 on 2026-09-30). |
