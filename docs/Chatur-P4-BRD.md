# Chatur — Business Requirements — Phase 4: More users

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Medium |
| Phase | 4 of 4 |
| Status | Draft |
| Date | 2026-09-21 |

## 1. Summary

The first three phases make Chatur good enough for the owner. This one makes it something another developer can be given. Feedback, issues, licences and feature control go through App Manager, which already does all four, and role and rule data is synced between machines through it rather than carried in a file. Chatur's verifier gains the thing its own build needed all along: it drives the windows of the Mac and Windows applications it has built, so a native head is checked the way a web page already is. The owner, by now working on many projects at once, gets one view across all of them and the ability to work in separate working copies so two runs never fight over the same files. Chatur's actions are offered to other tools, so something outside can start a process. And the installers are signed, which is what turns a download the owner trusts into one a stranger can. BRD-235 to BRD-246 hold the owner's evening test of 21 Nov 2026, BRD-247 to BRD-252 the owner's test of 22 Nov 2026 from the Start menu, and BRD-253 to BRD-275 the owner's request that evening.

## 2. Screens and flow

One row per routed page this phase adds; dialogs are written exactly as in the phase-1 BRD.

| Screen | Route | Role | Mockup | Fields |
|---|---|---|---|---|
| Feedback and issues | `/feedback` | Owner, Licensed developer | [mockup](mockups/feedback.html) | what it is about, wording, state, raised, answer |
| Licence | `/licence` | Owner, Licensed developer | [mockup](mockups/licence.html) | licence, days remaining, devices, features |
| All projects | `/all-projects` | Owner, Licensed developer | [mockup](mockups/all-projects.html) | project, phase, open, verified, last worked |
| Worktrees | `/worktrees` | Owner | [mockup](mockups/worktrees.html) | working copy, branch, run, path, state |
| Integrations | `/integrations` | Owner | [mockup](mockups/integrations.html) | action, what starts it, address, state |
| Measurements | `/measurements` | Owner, Licensed developer | [mockup](mockups/measurements.html) | period, runs, tokens read, tokens written, cost, time, by model, by agent, by command, mistakes, found by, first-pass, estimate and actual, runs list |
| Measurements across projects | `/measurements/all` | Owner, Licensed developer | [mockup](mockups/measurements-all.html) | period, project, runs, tokens read, tokens written, cost, time, mistakes |

**Primary journey:**

1. A developer signs in, sees his licence and how many days it has left.
2. He works on his own projects, seeing them all on one view.
3. A run takes its own working copy, so his own editing is never disturbed.
4. He hits a gap in Chatur, writes it as feedback, and sees the answer come back.
5. Chatur's verifier checks the Mac and Windows windows of the applications he has built.

## 3. Requirements

### Feedback and issues

The gap the owner found while working, reported without leaving the tool.

- **BRD-137** — Say what is wrong from inside Chatur. *Screen:* Feedback and issues · *Mockup:* [mockup](mockups/feedback.html)
  - *Acceptance:* When the owner sends feedback on Feedback and issues, then it is raised through App Manager and appears in the list as raised.
- **BRD-138** — Raise it against the library it belongs to. *Screen:* Feedback and issues · *Mockup:* [mockup](mockups/feedback.html)
  - *Acceptance:* When the entry is about a library on Feedback and issues, then the owner picks that library and the entry carries its name.
- **BRD-139** — See what came back. *Screen:* Feedback and issues · *Mockup:* [mockup](mockups/feedback.html)
  - *Acceptance:* When an entry has been answered on Feedback and issues, then its row shows the state and the answer.
- **BRD-140** — Nothing is lost when the network is down. *Screen:* Feedback and issues · *Mockup:* [mockup](mockups/feedback.html)
  - *Acceptance:* When feedback cannot be sent on Feedback and issues, then it is kept and sent when the network returns.

### Licence

What the signed-in developer is allowed, and on how many machines.

- **BRD-141** — The licence in force. *Screen:* Licence · *Mockup:* [mockup](mockups/licence.html)
  - *Acceptance:* When Licence opens, then the licence's name, state and days remaining are shown.
- **BRD-142** — Too many machines is reported. *Screen:* Licence · *Mockup:* [mockup](mockups/licence.html)
  - *Acceptance:* When the account is over the licence's device cap on Licence, then Licence shows the warning the server sent.
- **BRD-143** — Forget a machine. *Screen:* Licence · *Mockup:* [mockup](mockups/licence.html)
  - *Acceptance:* When the owner forgets a device on Licence, then it leaves the device list and its sign-in no longer works.
- **BRD-144** — A feature switched off is not offered. *Screen:* Licence · *Mockup:* [mockup](mockups/licence.html)
  - *Acceptance:* When a feature is off for this licence on Licence, then it is not offered anywhere in Chatur.

### All projects

The owner's twenty projects, on one screen.

- **BRD-145** — Everything at once. *Screen:* All projects · *Mockup:* [mockup](mockups/all-projects.html)
  - *Acceptance:* When All projects opens, then every project Chatur knows is listed with its phase and when it was last worked.
- **BRD-146** — Where each one stands. *Screen:* All projects · *Mockup:* [mockup](mockups/all-projects.html)
  - *Acceptance:* When a project has requirements on All projects, then its row shows how many are open and how many are verified.
- **BRD-147** — Go straight in. *Screen:* All projects · *Mockup:* [mockup](mockups/all-projects.html)
  - *Acceptance:* When the owner opens a project from the view, then it becomes the selected project and Project home opens.
- **BRD-148** — The roles follow the developer. *Screen:* All projects · *Mockup:* [mockup](mockups/all-projects.html)
  - *Acceptance:* When roles and rules are changed on one machine, then the other machine has them after syncing through App Manager.

### Worktrees

Two runs, two working copies, no fight over the same files.

- **BRD-149** — Work in a separate copy. *Screen:* Worktrees · *Mockup:* [mockup](mockups/worktrees.html)
  - *Acceptance:* When the owner adds a working copy for a branch on Worktrees, then it is listed with its path and its branch.
- **BRD-150** — Give a run its own copy. *Screen:* Worktrees · *Mockup:* [mockup](mockups/worktrees.html)
  - *Acceptance:* When a run is started in a working copy, then every file it changes is in that copy and not in the owner's.
- **BRD-151** — Clear it away. *Screen:* Worktrees · *Mockup:* [mockup](mockups/worktrees.html)
  - *Acceptance:* When the owner removes a working copy on Worktrees, then it leaves the list and its folder is gone.

### Integrations

Chatur's own actions, offered to whatever else the owner uses.

- **BRD-152** — The actions are published. *Screen:* Integrations · *Mockup:* [mockup](mockups/integrations.html)
  - *Acceptance:* When Integrations opens, then every action Chatur offers is listed with what starts it and its address.
- **BRD-153** — Something outside can start one. *Screen:* Integrations · *Mockup:* [mockup](mockups/integrations.html)
  - *Acceptance:* When an outside tool calls a published action on Integrations, then the matching process starts and the run is listed.
- **BRD-154** — The verifier drives real windows. *Screen:* Integrations · *Mockup:* [mockup](mockups/integrations.html)
  - *Acceptance:* When the verifier checks a Mac or Windows application Chatur built on Integrations, then it drives that application's own window and keeps the screenshots.
- **BRD-155** — Signed downloads. *Screen:* Integrations · *Mockup:* [mockup](mockups/integrations.html)
  - *Acceptance:* When a release is published on Integrations, then the Mac and Windows downloads are signed and the screen shows the signature's state.

### The owner's evening test, 21 Nov (Workbench and Settings)

Added 2026-11-21 from fifteen minutes of the owner's own use (his notes of that evening and the day's log). Running code is the biggest requirement of this IDE: a web application ran with no output and no browser. The project switcher did not switch, agents did not say who they are, a reply did not say which model answered, and New project in a solution made a new solution instead. BRD-239 replaces the 20 Nov decision of "the 20 last opened, then All projects". BRD-245 and BRD-246 are under Non-functional requirements. These screens are phase 1's and phase 3's; the items live here because phase 3 is at its 100.

- **BRD-235** — Run shows the build and the program's output live, in an Output panel that opens by itself. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person presses Run on Workbench, then Output opens by itself and shows the build and then the program's own lines as they come, error lines marked.
- **BRD-236** — A web project opens in the browser once it is up. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person runs a web project on Workbench, then once it is listening the browser opens at its launch-settings or listening address.
- **BRD-237** — A build that fails says so with its errors; a program that ends shows its exit code. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When a run's build fails on Workbench, then Output says the build failed and lists each error with file and line; when the program ends, Output shows its exit code.
- **BRD-238** — Picking a project in the switcher switches to it. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person picks another project in the switcher on Workbench, then its files, its chat and its run targets show, and the switcher names it.
- **BRD-239** — The switcher lists every project, scrolls, and filters. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person opens the switcher on Workbench, then every project of theirs is listed, the list scrolls, a filter narrows it, and no "All projects" shows.
- **BRD-240** — The bar clearly names the project open now. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When a project is open on Workbench, then the switcher reads "Project:" and its name, and its list ticks that same project as "open now".
- **BRD-241** — An agent greets the person with what it is for and its commands, taken from its real command list. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person chooses an agent on Workbench or asks what commands it has, then the answer lists every command of its real list.
  - *Changed 2026-11-22:* the Analyst greeted with two commands and denied having brainstorm; the list is now Chatur's own, the same as the TechieFlow agent's (BRD-249).
- **BRD-242** — Every reply shows which model answered, and on Automatic why. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When an agent replies on Workbench, then the reply names the model that answered; with routing on Automatic, a few words say why that model was picked.
- **BRD-243** — Routing says in plain words how Automatic chooses. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-routing.html)
  - *Acceptance:* When the person opens Routing on Settings, then a plain paragraph says how Automatic chooses between the listed models, with one example.
- **BRD-244** — New project is one dialog in every state and always comes from this machine's templates. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person chooses New project in any state on Workbench, then one dialog lists this machine's templates, shows where things will be made, and adds to an open solution.
  - *Changed 2026-11-22:* with no solution open, New project made an empty solution and showed no templates (BRD-247).
  - *Changed 2026-11-23:* the location is no longer a field to type when a folder is open; the dialog shows the folder and the two paths it will make (BRD-279).

### The owner's test of 22 Nov (Start, Workbench)

Added 2026-11-22 from the owner's test of the installed Chatur, started from the Start menu. Starting from nothing, there was no New project on "Nothing is open yet"; New project from an empty folder made an empty solution with no templates, and Run then said "No target found". The Analyst greeted with two commands, said it had no brainstorm, and its reply showed raw `**` marks. Every agent now carries every command of its TechieFlow agent, done by Chatur itself. BRD-252 is under Non-functional requirements.

- **BRD-247** — New project with no solution open makes the solution and its first project from a template, and opens it. *Screen:* Start · *Mockup:* [mockup](mockups/start-empty.html)
  - *Acceptance:* When the person creates a project with no solution open on Start, then the solution file is made in that folder, the project one folder under, and it opens.
  - *Changed 2026-11-23:* a folder that was open and not empty got a second folder named after the solution inside it, with the solution and the project in there. The folder that is open, or the one picked, is now always the solution's folder.
- **BRD-248** — After New project, Run has a target. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person creates a project with New project on Workbench, then Run's target list names the new project without anything more, and "No target found" does not stay.
- **BRD-249** — Every agent carries every command its TechieFlow agent has, done in Chatur. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types any TechieFlow command of an agent to that agent on Workbench, then Chatur runs it itself and *help lists it.
- **BRD-250** — A reply is drawn as formatted text. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When an agent replies with bold, a list, code or a table on Workbench, then the reply shows them formatted and no raw ** or | marks are left.
- **BRD-251** — In an empty folder the Analyst brainstorms with the person and writes the brief. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person asks the Analyst on Workbench, in an empty folder, to brainstorm and then write the brief, then the brief's file is written under docs/.

### Chatur's own measurements, the agents' commands (owner, 22 Nov 2026 evening)

Added 2026-11-22. TfLens reaches only repositories it can see; these numbers are wanted inside Chatur for any project, private or offline, read from its own `docs/metrics` files with no network call, in TfLens's words. The agents' command tables go into the UsageGuide and the README from the command list in code. Nine commands not yet run end to end are each run to their end. BRD-261, BRD-263, BRD-264 and BRD-275 are under Non-functional requirements.

- **BRD-253** — Measurements, in the main menu, for the open project, read from its own docs/metrics files and nothing else. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person chooses Project ▸ Measurements on Measurements, then the open project's figures show, read only from its docs/metrics files, with no network call.
- **BRD-254** — Pick the period. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person picks Today, 7 days, 30 days, All or two dates on Measurements, then every figure on the screen counts only the records inside that period.
- **BRD-255** — Runs, tokens, cost and time, split by model, by agent and by command. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person picks a period on Measurements, then runs, tokens, the cost or "no price", and time show, each split by model, agent and command.
- **BRD-256** — Mistakes: how many, who found each, the rows, and first-time passes. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person picks a period on Measurements, then the misses show with who found each and their rows, and how many rows passed the first time.
- **BRD-257** — Estimate beside actual for each phase, with the estimate table that exists. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When Measurements opens on a project with an estimate, then the phase's estimate stands beside its actual in the same table Process run and Run record show.
- **BRD-258** — One line per run, which opens. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person opens a run's line on Measurements, then its command, model, tokens, time, result and the rows it touched are shown.
- **BRD-259** — The same totals across every project. *Screen:* Measurements across projects · *Mockup:* [mockup](mockups/measurements-all.html)
  - *Acceptance:* When the person opens Measurements across projects, then one line per project gives runs, tokens, cost, time and mistakes for the period, read from each project's docs/metrics files.
- **BRD-260** — Save the open project's measurements as one zip TfLens imports. *Screen:* Measurements · *Mockup:* [mockup](mockups/measurements.html)
  - *Acceptance:* When the person presses Save as zip on Measurements, then one zip of the project's docs/metrics files is saved, in the shape TfLens imports.
- **BRD-262** — Settings ▸ Measurements stays the state of the files and links to the new screen. *Screen:* Settings · *Mockup:* [mockup](mockups/settings-measurements.html)
  - *Acceptance:* When the person opens Measurements on Settings, then it still shows the state of the six files and a link opens the Measurements screen.
- **BRD-265** — *day1-brownfield runs to its end from the chat. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*day1-brownfield {App}` to the Analyst on Workbench in a project with code, then its BRD and Architecture are written under docs/.
- **BRD-266** — *doc-out writes the document drafted in the conversation. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person drafts a document with the Analyst and types `*doc-out` on Workbench, then the whole document is written to its file.
- **BRD-267** — The Analyst's *research-prompt writes the research prompt. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*research-prompt {topic}` to the Analyst on Workbench, then docs/{App}-Research-Prompt.md is written.
- **BRD-268** — *devguide writes the developer's guide. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*devguide {App}` to the Flow master on Workbench, then docs/{App}-DevGuide.md is written.
- **BRD-269** — *productguide writes the end user's manual. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*productguide {App}` to the Flow master on Workbench, then docs/{App}-ProductGuide.md is written.
- **BRD-270** — *develop-end-to-end started from the chat starts the end-to-end run. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*develop-end-to-end {App} {brief}` to the Flow master on Workbench, then an end-to-end run starts and the reply links to its page.
- **BRD-271** — *develop-status says where the end-to-end run is. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*develop-status {App}` to the Flow master on Workbench, then the reply says whether a run is going, its step, its open rows and its tokens.
- **BRD-272** — *estimate-phase gives the phase's estimate. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*estimate-phase {App}` to the Flow master on Workbench, then the reply gives the phase's tokens and time, low, likely and high, or why none.
- **BRD-273** — The Architect's *research writes the research prompt. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types `*research {topic}` to the Architect on Workbench, then docs/{App}-Research-Prompt.md is written.
- **BRD-274** — A command that starts a process run says so and links to the run. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When a command typed on Workbench starts a process run, then the reply says so and its link opens that run's page.

### The owner's test of 23 Nov (Workbench)

Added 2026-11-23 from the owner's test of the installed Chatur. He opened a folder, chose New project, and got the solution and the project inside a second folder named after the solution: the folder was not empty, because a chat in it the day before had made Chatur write `docs/metrics` there (phase 1 BRD-74, changed the same day). One rule now holds for every starting point: the folder that is open, or the one picked, is the solution's folder. BRD-280 and BRD-281 are under Non-functional requirements.

- **BRD-276** — A folder with projects and no solution gets a solution that takes them in. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person creates a project on Workbench in a folder with projects and no solution, then the dialog says so, and the new solution there holds them all.
- **BRD-277** — A folder that has a solution gets the new project added to that solution. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person creates a project on Workbench with a folder open that holds a solution, then the project is added to that solution and no second solution is made.
- **BRD-278** — After Create, FILES keeps the folder that was open as its root. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person creates a project on Workbench with a folder open, then FILES lists that folder's solution file and project folders, with no folder wrapped around them.
- **BRD-279** — New project shows where things will be made before it makes them. *Screen:* Workbench · *Mockup:* [mockup](mockups/main.html)
  - *Acceptance:* When the person types the names in New project on Workbench, then the dialog shows the folder and the full paths of the solution file and the project folder.

## 4. Non-functional requirements

| Id | Area | Requirement | Measure |
|---|---|---|---|
| BRD-245 | Reliability | A closed window leaves no errors behind | When the person closes the Settings window and switches project on Workbench, then the day's log holds no unobserved task exception |
| BRD-246 | Install | Chatur installs for the user on Windows | When the owner runs the install script on Windows, then Chatur opens from its Start menu entry and keeps the owner's data |
| BRD-252 | Testing | The owner's test and its tests start from the two states a person really meets | When the owner's test and its tests run, then they start from nothing open after install and from an empty folder, never a hand-made solution |
| BRD-261 | Correctness | Every number on the measurements screens is computed by code and equals the total added up again from the files | When a test adds up the fixture's files again on Measurements, then every figure equals it, unmeasured ones read "not measured", and the words are TfLens's |
| BRD-263 | Documentation | The UsageGuide has "The agents and their commands", one table per agent, from the one command list in the code | When a test compares the UsageGuide with the command list on Workbench, then each agent's table names every command once, with what, example and where, and nothing else |
| BRD-264 | Documentation | The README names the four agents and their commands in short and links to the guide's section | When a test compares the README with the command list on Workbench, then every command is named once under its agent and no other command is named |
| BRD-275 | Testing | The owner's test has a part for the measurements screens and one for typing a command of each agent | When the owner follows the Owner's test on Workbench, then one part walks the measurements screens, one types a command to each agent, and the eight parts stay |
| BRD-280 | Testing | New project's tests compare the exact paths on disk for each starting point | When the tests of New project run, then for each of the four starting points every path made on disk is compared with the one wanted. |
| BRD-281 | Testing | The owner's test makes its folders under this repository's bin folder and starts with the empty-folder case | When the owner follows the Owner's test, then every folder it makes is under this repository's bin folder, and part 1 is the empty-folder case. |

## 5. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-11-23.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P4-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| Feedback and issues | 4 | 4 | 0 | Done |
| Licence | 4 | 4 | 0 | Done |
| All projects | 4 | 4 | 0 | Done |
| Worktrees | 3 | 3 | 0 | Done |
| Integrations | 4 | 4 | 0 | Done |
| The owner's evening test, 21 Nov (Workbench, Settings) | 12 | 12 | 0 | Done |
| The owner's test of 22 Nov (Start, Workbench) | 6 | 6 | 0 | Done |
| Measurements | 7 | 7 | 0 | Done |
| Measurements across projects | 1 | 0 | 1 | In progress |
| Settings | 1 | 1 | 0 | Done |
| Workbench | 10 | 10 | 0 | Done |
| Non-functional | 4 | 4 | 0 | Done |

## 6. Where the rest lives

| What | Where |
|---|---|
| Scope, users and roles, the context diagram | [phase 1 BRD](Chatur-BRD.md) |
| Non-functional requirements for the whole application | [phase 1 BRD](Chatur-BRD.md) |
| Constraints, assumptions and risks | [phase 1 BRD](Chatur-BRD.md) |
| The Start, Workbench and Settings screens BRD-235 to BRD-252 add to | [phase 1 BRD](Chatur-BRD.md), [phase 3 BRD](Chatur-P3-BRD.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P4-Checklist.md](Chatur-P4-Checklist.md) |
