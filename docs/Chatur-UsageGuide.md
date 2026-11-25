# Chatur — Usage Guide

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Large |
| Date | 2026-11-22 |

## Test users

| # | User | Password source | Role | Exists |
|---|---|---|---|---|
| 1 | chatur-owner@techierathore.com | `testUsers.Owner.password` in `tests/verify/test-settings.json` (git-ignored; the tests' secret settings) | User (the owner's own account shape) | yes |
| 2 | chatur-second@techierathore.com | `testUsers.Second.password` in `tests/verify/test-settings.json` (git-ignored; the tests' secret settings) | User (a second account: the two-account checks) | yes (registered 18 Nov) |
| 3 | chatur-yolo-tester@techierathore.com | `testUsers.Yolo.password` in `tests/verify/test-settings.json` (git-ignored; the tests' secret settings) | User (unattended runs) | no |

A verify test never creates, deletes or changes an account on a shared AppManager; the "register creates an account" path is verified by hand once per release and recorded in the checklist with the date, and the automated row checks only the form's validation and the "email already in use" path using test user 1. The Register test signs in as test user 1 before it submits anything; the test users are registered only by the owner, with `node .tfcore/utils/tf-register-test-users.mjs`.

Chatur signs in through the production App Manager (`https://appmgrapi.techierathore.com`, application id 1). The test model provider is the `testProvider` block of the same settings file. No setting comes from an environment variable.

## Execution guide

Prerequisites: .NET 10 SDK with the MAUI workload, Windows 10 19041 or newer for the Windows app, Node 20 with Playwright for the browser tests, a NuGet credential for the owner's GitHub Packages feed, `AppManager:ApiKey` in user secrets.

```
git clone <repo>
dotnet restore Chatur.sln
dotnet build Chatur.sln
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\install-windows.ps1
dotnet run --project src/Chatur.WebHarness --urls http://localhost:5280
```

The fourth line installs the Windows app to `%LOCALAPPDATA%\Programs\Chatur` with a Start menu entry "Chatur" (run it again to replace it in place); start Chatur from the Start menu and sign in as user 1. The fifth serves the same screens at http://localhost:5280 for the browser tests.

### The agents and their commands

Type a command with its star in the agent's conversation. In the examples `MyApp` stands for your project's name, and `{App}` in a file name is replaced by it.

#### Analyst

| Command | What it does | Example to type | Where the result appears |
|---|---|---|---|
| `*help` | Lists these commands, numbered. | `*help` | In the chat |
| `*day1-brownfield` | Day 1 for a project that already has code: reads the code and its documents and writes the Architecture, BRD, Coding Standards, checklist, status, UsageGuide and DevGuide. | `*day1-brownfield MyApp` | A file: `docs/{App}-BRD.md` |
| `*day1-greenfield` | Day 1 for a new project: brief, BRD, Architecture, mockups, Coding Standards, checklist and status, as a process run with its checks. | `*day1-greenfield MyApp` | A run on the Processes page |
| `*mockups` | Draws the UI design and one HTML mockup per screen, as a process run with its checks. | `*mockups MyApp` | A run on the Processes page |
| `*split-brd` | Splits the BRD into the checklist (and into phases when the project is large), as a process run with its checks. | `*split-brd MyApp` | A run on the Processes page |
| `*brainstorm` | Holds a brainstorming session with you, one technique at a time, and saves it to docs/brainstorming-session-results.md. | `*brainstorm a habit tracker` | A file: `docs/brainstorming-session-results.md` |
| `*amend-docs` | Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks. | `*amend-docs MyApp add a search box` | A run on the Processes page |
| `*create-project-brief` | Writes docs/{App}-Brief.md, the one page day 1 reads: what it is, who it is for, must do, out of scope, open questions. | `*create-project-brief MyApp` | A file: `docs/{App}-Brief.md` |
| `*doc-out` | Writes the whole document drafted in this conversation to its file. | `*doc-out` | A file: `docs/…` |
| `*research-prompt` | Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md. | `*research-prompt a habit tracker` | A file: `docs/{App}-Research-Prompt.md` |
| `*yolo` | Turns Go ahead on or off for this conversation: on, nothing waits for your approval. | `*yolo` | In the chat |
| `*exit` | Says goodbye and closes this conversation. | `*exit` | In the chat |

#### Architect

| Command | What it does | Example to type | Where the result appears |
|---|---|---|---|
| `*help` | Lists these commands, numbered. | `*help` | In the chat |
| `*doc-out` | Writes the whole document drafted in this conversation to its file. | `*doc-out` | A file: `docs/…` |
| `*research` | Writes a deep research prompt for a topic to docs/{App}-Research-Prompt.md. | `*research a habit tracker` | A file: `docs/{App}-Research-Prompt.md` |
| `*yolo` | Turns Go ahead on or off for this conversation: on, nothing waits for your approval. | `*yolo` | In the chat |
| `*exit` | Says goodbye and closes this conversation. | `*exit` | In the chat |
| `*architect` | Designs the solution structure and the data model and writes docs/{App}-Architecture.md (Chatur's own, kept from phase 1). | `*architect MyApp` | A file: `docs/{App}-Architecture.md` |
| `*create-doc` | Writes a document from a template (Chatur's own, kept from phase 1). | `*create-doc architecture` | A file: `docs/…` |

#### Flow master

| Command | What it does | Example to type | Where the result appears |
|---|---|---|---|
| `*develop-end-to-end` | Takes the project to the owner's acceptance test unattended, step after step; --follow says where it is, --stop ends it, --resume carries it on. | `*develop-end-to-end MyApp` | A run on the End-to-end run page |
| `*develop-status` | Says whether an end-to-end run is going, its step, its requirements and its tokens. | `*develop-status MyApp` | In the chat |
| `*estimate-phase` | Estimates the phase's tokens and time from the runs so far. | `*estimate-phase MyApp` | In the chat |
| `*build-phase` | Builds every open row of the phase's checklist, as a process run with its checks. | `*build-phase MyApp` | A run on the Processes page |
| `*fix-issues` | Works through reported bugs, fixes them and re-checks, as a process run with its checks. | `*fix-issues MyApp docs/issues` | A run on the Processes page |
| `*triage-issues` | Logs reported bugs against the checklist without changing code, as a process run with its checks. | `*triage-issues MyApp docs/issues/bug-1.md` | A run on the Processes page |
| `*log-miss` | Records one thing that was missed, in your words, in docs/metrics/misses.jsonl and docs/{App}-Misses.md. | `*log-miss MyApp "the Run button stayed grey"` | A file: `docs/metrics/misses.jsonl` |
| `*triage-and-fix` | Logs the reported bugs, then fixes them: a triage run first, the fix run queued after it. | `*triage-and-fix MyApp docs/issues/bug-1.md` | A run on the Processes page |
| `*amend-docs` | Folds a change into the BRD, Architecture, mockups and checklist in place, as a process run with its checks. | `*amend-docs MyApp add a search box` | A run on the Processes page |
| `*devguide` | Writes the developer's map from each screen to the code behind it. | `*devguide MyApp` | A file: `docs/{App}-DevGuide.md` |
| `*productguide` | Writes the end user's manual, screen by screen. | `*productguide MyApp` | A file: `docs/{App}-ProductGuide.md` |
| `*handoff-phase` | Finishes the UsageGuide and DevGuide and closes the phase, as a process run with its checks. | `*handoff-phase MyApp` | A run on the Processes page |
| `*deploy-checklist` | Writes the deployment checklist for one hosting target, as a process run with its checks. | `*deploy-checklist MyApp azure-pipelines.yml` | A run on the Processes page |
| `*refresh-status` | Rewrites PROJECT-STATUS.md from the checklists, as a process run with its checks. | `*refresh-status MyApp` | A run on the Processes page |
| `*metrics` | Writes the metrics report from docs/metrics, as a process run with its checks. | `*metrics MyApp` | A run on the Processes page |
| `*generate-html` | Turns one markdown file into an HTML page beside it. | `*generate-html docs/MyApp-BRD.md` | A file: `docs/…`, an HTML page beside the markdown |
| `*render-workflow-docs` | Turns the BRDs, the Architecture and PROJECT-STATUS.md into HTML pages beside them. | `*render-workflow-docs MyApp` | A file: `docs/…`, an HTML page beside each document |
| `*yolo` | Turns Go ahead on or off for this conversation: on, nothing waits for your approval. | `*yolo` | In the chat |
| `*help` | Lists these commands, numbered. | `*help` | In the chat |
| `*exit` | Says goodbye and closes this conversation. | `*exit` | In the chat |

#### Verifier

| Command | What it does | Example to type | Where the result appears |
|---|---|---|---|
| `*verify` | Checks every row in scope against the running application and writes the verdicts, as a process run with its checks. | `*verify all MyApp` | A run on the Processes page |
| `*report` | Shows the status and remarks of the rows in scope (ui, functional, all, or ids) from the checklists, running nothing. | `*report all MyApp` | In the chat |
| `*help` | Lists these commands, numbered. | `*help` | In the chat |
| `*exit` | Says goodbye and closes this conversation. | `*exit` | In the chat |
| `*triage-issues` | Sorts what has been reported into the checklist it belongs to, as a process run (Chatur's own, kept from phase 2). | `*triage-issues docs/issues/bug-1.md` | A run on the Processes page |
| `*metrics-report` | Reads the run records and says what they add up to, as a process run (Chatur's own, kept from phase 2). | `*metrics-report` | A run on the Processes page |
| `*log-miss` | Records one thing that went wrong, in your words (Chatur's own, kept from phase 2). | `*log-miss "the Run button stayed grey"` | A file: `docs/metrics/misses.jsonl` |

## How to test, screen by screen

### First task 1: start the Windows app and sign in

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Press the Windows key, type `Chatur` and press Enter (the Start menu entry). 2) On Sign in, type your email and password and press Sign in.
- **Expected:** A window named Chatur opens with your name at the top right. What you open belongs to your account. An account that has opened nothing, your common account included, sees "Nothing is open yet" with three buttons: Open a folder, Open a solution, Open a file. No project, file or chat, and nothing another account opened. An account with history sees "Open recent" with its own projects.
- **Covers:** REQ-UI-001, REQ-UI-008, REQ-UI-091, REQ-UI-093

### First task 2: open the practice folder

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Press "Open a folder" (on the start window; from the main window it is File ▸ Open a folder). 2) In the box "The folder", type `C:\1MyCode\ChaturPractice`. 3) Press Open.
- **Expected:** The main window opens on the folder: the top bar names ChaturPractice, FILES shows Greeter, and "Greeter · Debug" stands beside Build. The chat on the right says "Nothing said yet. Ask Analyst about this project below." and, under the message box, "No session has been started for this project yet": a chat another account had here is never shown or continued.
- **Covers:** REQ-UI-008, REQ-FN-007, REQ-FN-008, REQ-FN-302

### First task 3: connect the Anthropic key for this account

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Tools ▸ Settings (Ctrl+,). The Model providers page opens in a second window. 2) Press "Add a provider". 3) Name: `Anthropic`. Connector: Anthropic. Paste your key in the key box. 4) Press Test. 5) Press Add. 6) Close the second window.
- **Expected:** The list starts empty for a new account: each account keeps its own keys. Test says "Answered" and names a model. After Add, the table shows Anthropic, connected, with claude-opus-5-5, claude-sonnet-5-5 and claude-haiku-5-5. Skip this block when Anthropic is already connected for this account.
- **Covers:** REQ-FN-014, REQ-FN-017, REQ-UI-019, REQ-UI-092

### First task 4: ask the Flow master for one small change

- **Sign in as:** your own account, or user 1
- **Steps:** 1) In the chat, click the model picker (it says "Automatic") and choose `claude-sonnet-5-5`. 2) Click the picker that says "Analyst" and choose "Flow master". 3) Leave "Ask first" as it is. 4) Type: `Change the greeting in Greeter/Program.cs so the program prints Good evening from Chatur.` 5) Press Send.
- **Expected:** A reply labelled Flow master with claude-sonnet-5-5. A step reads `Read "Greeter/Program.cs"`, and a red "refused" line says the edit waits for your approval in "ask me first": expected. Within seconds a card Greeter/Program.cs appears with Open, Approve and Reject, old line red, new line green. Nothing is written yet.
- **Covers:** REQ-FN-024, REQ-UI-022, REQ-UI-024

### First task 5: approve the change and press Run

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Press Approve on the card. 2) Press Run at the top left. 3) Read the Output strip at the bottom.
- **Expected:** The card goes and `Greeter\Program.cs` now holds `Console.WriteLine("Good evening from Chatur");`. Output shows `> dotnet build …`, the build's own lines, `Built in …s — starting it`, `> dotnet run --project "C:\1MyCode\ChaturPractice\Greeter\Greeter.csproj" -c Debug --no-build --no-launch-profile`, then `Good evening from Chatur`, then `Process exited with code 0.`, and the label beside Output reads "ended, exit code 0".
- **Covers:** REQ-UI-013, REQ-UI-014

### First task 6: open a file from the start window

- **Sign in as:** your own account, or user 1
- **Steps:** 1) File ▸ Recent projects ▸ Project list to reach the start window, or start from the empty start. 2) Press "Open a file". 3) In the box "The file", type `C:\1MyCode\ChaturPractice\Greeter\Program.cs`. 4) Press Open.
- **Expected:** The main window opens with Program.cs open in a tab. The project is the nearest solution or repository folder above the file; ChaturPractice has neither, so it is the file's own folder, Greeter.
- **Covers:** REQ-UI-094

### Owner's test 1: an empty folder, New project, and the disk

- **Sign in as:** your own account
- **Steps:** 1) Make the empty folder `C:\1MyCode\Chatur\bin\OwnerTest`. 2) Windows key, type `Chatur`, Enter. 3) "Open a folder"; type that folder; Open. 4) File ▸ New project; "Blazor Web App"; Solution name `Checking-Chatur`. 5) Create. 6) Look in Explorer.
- **Expected:** 2) "Nothing is open yet" offers New project and three Open ways. 4) "Will be created in" names OwnerTest, beside "Change location"; then Solution file `…\OwnerTest\Checking-Chatur.slnx` and Project folder `…\OwnerTest\Checking-Chatur`. 6) OwnerTest holds `Checking-Chatur.slnx` and `Checking-Chatur\Checking-Chatur.csproj`; no `docs`, no second folder inside. FILES shows the same. Agent-run on the installed app, own test data folder, 23 Nov.
- **Covers:** REQ-FN-310, REQ-UI-125, REQ-UI-126, REQ-NFR-019, REQ-NFR-013, REQ-UI-008

### Owner's test 2: from nothing, New project in a folder you name

- **Sign in as:** your own account
- **Steps:** 1) File ▸ Close. 2) Press "New project" (under Get started, or on "Nothing is open yet"). 3) Click "Console App". 4) Solution name `FromNothing`, Project name `TryConsole`, Location `C:\1MyCode\Chatur\bin\FromNothing`. 5) Press Create.
- **Expected:** One Location field, empty until you type. Under it Solution file `C:\1MyCode\Chatur\bin\FromNothing\FromNothing.slnx` and Project folder `C:\1MyCode\Chatur\bin\FromNothing\TryConsole`. After Create exactly those exist, with `TryConsole\TryConsole.csproj` and `Program.cs`; the main window is on FromNothing and the target picker reads TryConsole.
- **Covers:** REQ-FN-310, REQ-FN-309, REQ-NFR-013

### Owner's test 3: add projects from this machine's templates

- **Sign in as:** your own account
- **Steps:** 1) File ▸ New project. 2) Click "Blazor Web App", type `TryWeb` in Name, press Create. 3) The same with "Class Library" `TryLib` and "xUnit Test Project" `TryLib.Tests`.
- **Expected:** The dialog reads "New project in FromNothing"; only the name is typed. It shows "Will be created in `C:\1MyCode\Chatur\bin\FromNothing`", the solution file "already there", and Project folder `C:\1MyCode\Chatur\bin\FromNothing\TryWeb`. Each project is one folder under FromNothing, in FILES and in the target picker.
- **Covers:** REQ-UI-112, REQ-UI-126

### Owner's test 4: run the web app

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing`: target picker ▸ TryWeb; press Run. 2) Press Stop.
- **Expected:** Output opens by itself: `> dotnet build …`, the build's lines, `Now listening on: http://localhost:<port>`, `Opening http://localhost:<port> in the browser`; your browser opens there. Stop writes `Run stopped.`.
- **Covers:** REQ-UI-107, REQ-FN-306

### Owner's test 5: the console app, a broken build and the exit code

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing`: target picker ▸ TryConsole; press Run. 2) FILES ▸ TryConsole ▸ Program.cs; delete the `;` at the end of its line; Ctrl+S; press Run. 3) Put the `;` back; Ctrl+S.
- **Expected:** 1) `Hello, World!`, then `Process exited with code 0.`. 2) `The program was not started.`; the label reads "build failed".
- **Covers:** REQ-UI-107, REQ-UI-108

### Owner's test 6: the test project

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing`: target picker ▸ TryLib.Tests; press Run.
- **Expected:** `> dotnet test …`, a passed summary with 0 failed, and `Process exited with code 0.`.
- **Covers:** REQ-UI-107

### Owner's test 7: a folder with a project and no solution

- **Sign in as:** your own account
- **Steps:** 1) In a terminal: `dotnet new console -n Loose -o C:\1MyCode\Chatur\bin\LooseOne\Loose`. 2) File ▸ Close; "Open a folder"; `C:\1MyCode\Chatur\bin\LooseOne`; Open. 3) File ▸ New project; click "Class Library"; Solution name `Gathered`, Project name `Second`. 4) Press Create.
- **Expected:** 3) The dialog says "This folder has a project and no solution. Create makes the solution here and adds it to it: Loose\Loose.csproj". 4) LooseOne holds `Gathered.slnx`, `Loose` and `Second`; the solution lists both projects; the target picker reads Loose.
- **Covers:** REQ-UI-124, REQ-FN-309

### Owner's test 8: in an empty folder, the Analyst's own commands

- **Sign in as:** your own account (a model provider connected; else First task 3)
- **Steps:** 1) Make and open the empty folder `C:\1MyCode\Chatur\bin\BrainTest`, as in test 1. 2) Model picker ▸ your model; agent picker ▸ Analyst. 3) Type `Don't you have brain-storm command ?`; Send. 4) `*help`; Send. 5) `*yolo`; Send. 6) Look at BrainTest in Explorer.
- **Expected:** The greeting lists 12 commands, brainstorm among them. 3) "Yes." with `*brainstorm {topic}`, named "Chatur (no model)". 4) The 12 commands as a numbered list, names drawn as code, no `**`. 5) "YOLO is on" with **Go ahead** in bold. 6) BrainTest is still empty: chatting made no file in it.
- **Covers:** REQ-FN-308, REQ-FN-311, REQ-UI-114, REQ-FN-040

### Owner's test 9: the Analyst brainstorms with you, then writes the brief

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\BrainTest`, type `*brainstorm a small to-do app for one person`; Send. 2) Answer its questions (e.g. one person, desktop, small, focused, yes save it; ideas: due dates, a today view, quick add). 3) Say "That is enough ideas, save the session". 4) `*create-project-brief BrainTest`; Send; say "take the sensible reading and write the brief now" if it asks.
- **Expected:** 3) `docs\brainstorming-session-results.md` appears in FILES. 4) The reply ends "Chatur: *create-project-brief is done — docs/BrainTest-Brief.md is written"; FILES shows it, dated today.
- **Covers:** REQ-FN-312

### Owner's test 10: the measurements screens

- **Sign in as:** your own account
- **Steps:** 1) Open a project that has `docs\metrics`: `C:\1MyCode\Chatur`. 2) Project ▸ Measurements. 3) Period ▸ Last 7 days, then All history. 4) Click the first run line; Esc. 5) Save as zip ▸ Save ▸ Close. 6) Click "Measurements across projects". 7) Click "Measurements" under Corrections, then its link.
- **Expected:** 2) Seven tiles, splits, Misses with first-pass rate; a missing figure reads "not measured", a cost "no price". 3) Figures shrink. 4) The run's command, model, tokens, time, result, rows. 5) A .zip in the project folder. 6) One line per project. 7) Back on Project measurements.
- **Covers:** REQ-UI-115 to REQ-UI-121, REQ-FN-313, REQ-FN-314, REQ-NFR-017

### Owner's test 11: one command to each agent

- **Sign in as:** your own account (a model provider connected)
- **Steps:** 1) Before each agent: agent picker ▸ the agent; new chat; your model; `*yolo` (not the Verifier, which has none). 2) Analyst: `*research-prompt note-taking apps for one person that work offline`. 3) Architect: `*research which local database suits a desktop to-do app for one person`. 4) Flow master: `*develop-status <project>`. 5) Verifier: `*report all <project>`.
- **Expected:** 2) `docs\<project>-Research-Prompt.md` appears in FILES. 3) The same file is written again. 4) "No end-to-end run", or its step, rows and tokens. 5) "… row(s) in scope" and a table of rows.
- **Covers:** REQ-FN-317, REQ-FN-322, REQ-FN-320, REQ-NFR-017

### If the key is refused

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Tools ▸ Model providers. 2) Press the remove button on the Anthropic row. 3) Press "Add a provider", choose Anthropic, paste a new key from console.anthropic.com. 4) Press Test before Add.
- **Expected:** A refused key reads "Response status code does not indicate success: 401 (Unauthorized). Provider message: invalid x-api-key" under Test, and nothing is saved until Add. A good key reads "Answered". Then ask again in the chat.
- **Covers:** REQ-UI-019, REQ-UI-020

### Sign in and Register

- **Sign in as:** user 1
- **Steps:** 1) Start Chatur signed out. 2) Type the email and a wrong password, press Sign in. 3) Type the right password. 4) Open "Create an account" and type a password that breaks the rule.
- **Expected:** The wrong password shows App Manager's own message. The right one opens the start window, and the next start does not ask again. Register marks each broken rule while typing and sends nothing until they pass.
- **Covers:** REQ-UI-001 to REQ-UI-003, REQ-FN-001 to REQ-FN-006

### Start window

- **Sign in as:** user 1
- **Steps:** 1) Read the recent list. 2) Search for a project by name. 3) Select a row and press "Remove from the list". 4) Use "Clone a repository" on a small repository.
- **Expected:** Projects list newest first with their paths. Search narrows the list. A removed row leaves the list and its folder stays on disk. The clone finishes and opens in the main window.
- **Covers:** REQ-UI-004 to REQ-UI-010, REQ-FN-007, REQ-FN-008

### Main window

- **Sign in as:** user 1
- **Steps:** 1) Open a file from FILES, change a line, press Save. 2) Press Build. 3) Press Run, then Stop. 4) Ask the agent a question with "Go ahead" chosen. 5) View ▸ Chat off and on.
- **Expected:** An unsaved tab is marked and asks before closing. Build and Run stream their lines into Output; Stop ends everything the run started. The reply names its model and tokens. The chat closes and comes back.
- **Covers:** REQ-UI-011 to REQ-UI-016, REQ-UI-021 to REQ-UI-024, REQ-UI-040, REQ-UI-041, REQ-UI-073, REQ-UI-074

### Prerequisites

- **Sign in as:** user 1
- **Steps:** 1) Project ▸ Prerequisites. 2) Read the tool list. 3) Copy the fix for a missing tool. 4) Press "Check again".
- **Expected:** Every tool the project needs with the version found or "not found" and a command to copy; Chatur's own version; an offer when a newer build exists.
- **Covers:** REQ-FN-011 to REQ-FN-013, REQ-UI-017, REQ-UI-018

### Settings

- **Sign in as:** user 1
- **Steps:** 1) Tools ▸ Routing: change a role's tier, reorder a chain, press "Reset the tiers". 2) Agents: edit an agent's wording and save. 3) Corrections and Measurements: read the tables. 4) Appearance: change the theme and the mode.
- **Expected:** Tier and chain changes are still there after closing and opening Chatur. The agent's new wording is used in its next reply. The theme repaints every window at once. No key appears on any screen.
- **Covers:** REQ-FN-018 to REQ-FN-023, REQ-FN-025 to REQ-FN-043, REQ-UI-025 to REQ-UI-039

### Measurements and Measurements across projects

- **Sign in as:** user 1
- **Steps:** 1) Open a project that has `docs\metrics`. 2) Project ▸ Measurements; pick each period; open a run line; Save as zip. 3) "Measurements across projects" in the side list; click a project's name.
- **Expected:** Figures come only from the project's files; a missing figure reads "not measured", a run without cost "no price". The zip holds `docs/metrics/<file>.jsonl`. Across projects shows one line per project; a name opens its Measurements.
- **Covers:** REQ-UI-115 to REQ-UI-121, REQ-FN-313, REQ-FN-314, REQ-NFR-014

### Repository

- **Sign in as:** user 1
- **Steps:** 1) Approve a change in a project that is a git repository. 2) Project ▸ Repository. 3) Tick the file, write a message, press Check in. 4) Press Push.
- **Expected:** Changed files list with their differences. Only ticked files are checked in. The count to push moves. An agent that asks to run a source-control command is refused and the refusal is shown.
- **Covers:** REQ-UI-042 to REQ-UI-044, REQ-FN-044 to REQ-FN-048

### Processes and a process run

- **Sign in as:** user 1
- **Steps:** 1) Project ▸ Processes. 2) Choose verify-phase and read its steps and its estimate. 3) Press Start. 4) Watch the run tab. 5) Close Chatur mid-run and open it again.
- **Expected:** Twelve processes with their steps. The run tab shows each step as to come, running or done, the agent at work in the chat, and the actual so far beside the estimate. A failed check stops the run. The run carries on after reopening.
- **Covers:** REQ-FN-049 to REQ-FN-055, REQ-UI-045 to REQ-UI-048, REQ-UI-070, REQ-UI-071, REQ-UI-075, REQ-UI-083, REQ-UI-087

### Requirements board

- **Sign in as:** user 1
- **Steps:** 1) Open a project that has a checklist. 2) Project ▸ Board. 3) Narrow by Phase, Status and Agent, then search an id. 4) Open one row. 5) Change a row's status.
- **Expected:** Every requirement with its id, title, agent and status, and the counts above. The opened row shows its acceptance line. The status change is written into the checklist file; an edit made outside Chatur shows after Reload.
- **Covers:** REQ-UI-049 to REQ-UI-052, REQ-FN-056, REQ-FN-057

### Agent workspace

- **Sign in as:** user 1
- **Steps:** 1) Project ▸ Agents. 2) Choose Verifier. 3) Read what is waiting for it. 4) Start one of its commands. 5) Change to another agent and back.
- **Expected:** Each agent has its own view with only the commands its rights allow. The command starts in the chat. The first agent's open session is still there after coming back.
- **Covers:** REQ-UI-053, REQ-UI-054, REQ-FN-058 to REQ-FN-060

### End-to-end run, queue and report

- **Sign in as:** user 1
- **Steps:** 1) Project ▸ End-to-end run. 2) Type a brief, read the estimate, press Start. 3) Press Pause, then start again. 4) Open the Run queue and queue a second run. 5) Open the Run record when the run ends.
- **Expected:** The chain of eight processes names the role at work and stops at the acceptance test. One run goes at a time. The record lists every requirement with its verdict, every miss, the cost, and the estimate beside the actual.
- **Covers:** REQ-FN-061 to REQ-FN-072, REQ-FN-084 to REQ-FN-116, REQ-UI-055 to REQ-UI-061, REQ-UI-084 to REQ-UI-090

## Automated tests

```
dotnet test tests/unit/Chatur.Tests/Chatur.Tests.csproj
npx playwright test
```

The unit tests cover `Chatur.Core`: the guards, routing and its fallback, the measurement writer, the agent loop, processes, the board and the end-to-end engine. The browser tests under `tests/verify` drive the web harness, one test per requirement; `desktop-head.spec.ts` drives the Windows app.

## Known limitations

- REQ-FN-024: after the agent is changed in one conversation, earlier replies take the new agent's name. Choose the agent before the first message.
- REQ-FN-022, REQ-FN-001, REQ-FN-002, REQ-FN-004, REQ-FN-030, REQ-FN-048: checked by hand once per release; steps are in `docs/Chatur-Checklist.md`.
- REQ-FN-012 needs the Mac. REQ-NFR-006 needs a published nightly build.
- The default agent, Analyst, explains a code change and does not make it; Flow master makes it.
- TF-052 (`docs/Chatur-TechieFlow-Feedback.md`): late in a long verify the screen check reads three chat menus as "opens nothing"; they open by hand. Open with the framework's team; it does not block using the app.
- TR-025, TR-026 (`docs/Chatur-TrBlazeUI-Feedback.md`): a submenu's edge distance at 390 px; a picker's list cannot be wider than its button. TR-029: no read-only markdown view in TrBlazeUI; Chatur draws replies with Markdig.
- The screen check cannot drive the Windows app by address (its web view reloads); the Windows app is checked by its own test files, which click through it.
- A message for a window that has just closed is written to the log as one information line, not as an error (REQ-NFR-011).
- The install script reads the App Manager key from this project's user secrets; without it, it stops and says so.

## Platform notes

Windows and Mac Catalyst are equal targets. The Windows app builds only on Windows, the Mac app only on a Mac. Tokens and keys live in Credential Manager on Windows and the Keychain on the Mac; the database and the log live in the user's application-data folder. A copy started by a test keeps its data under `tests/.artifacts/app-data` and never opens the owner's.
