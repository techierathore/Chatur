# Chatur — Usage Guide

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Large |
| Date | 2026-11-27 |

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

On the Mac, in Terminal:

```
cd /Volumes/MacD/MyCode/Chatur
bash scripts/install-mac.sh
```

It builds the Mac app (Release, signed with the free Mac Catalyst profile of your Apple ID, made by Xcode's tools when missing) and replaces `/Applications/Chatur.app`; open it with Cmd+Space, `Chatur`, Return. Without a key for the GitHub Packages feed on the Mac it uses TrBlazeUI 2.2.5 from nuget.org and TechieRag packed from `../TechieRag`, and says so. The Mac owner's test is the "Mac owner's test" parts below; on the Mac, Chatur's own menus are in the bar under the window's title, and the Mac menu bar at the top holds Quit (Cmd+Q).

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
- **Steps:** 1) Windows key, type `Chatur`, Enter: "Nothing is open yet" shows New project and three Open ways. 2) "Open a folder"; type `C:\1MyCode\Chatur\bin\OwnerTest` (empty); Open. 3) File ▸ New project. 4) Type `blazor` in the search box; click "Blazor Web App"; Solution name `Checking-Chatur`. 5) Create.
- **Expected:** 2) The folder opens at once. 4) The list narrows as you type. "Will be created in" names OwnerTest; under it Solution folder `…\OwnerTest\Checking-Chatur`, Solution file `…\Checking-Chatur\Checking-Chatur.slnx`, Project folder `…\Checking-Chatur\Checking-Chatur`. 5) OwnerTest holds only `Checking-Chatur`, with `Checking-Chatur.slnx` and the project folder inside; nothing is directly in OwnerTest. Agent-run on its own test data folder.
- **Covers:** REQ-FN-310, REQ-UI-126, REQ-UI-127, REQ-NFR-019, REQ-NFR-013, REQ-UI-008

### Owner's test 2: the solution is the root of FILES

- **Sign in as:** your own account
- **Steps:** 1) After test 1 look at FILES. 2) In a terminal: `dotnet new console -n Early -o C:\1MyCode\Chatur\bin\LateSln\Early`. File ▸ Close; open `C:\1MyCode\Chatur\bin\LateSln` as a folder (Open as it is). 3) In that folder run `dotnet new sln --name Late` and `dotnet sln add Early\Early.csproj`; press the refresh button in FILES.
- **Expected:** 1) One root, "Checking-Chatur", with the solution mark; the project below it has the project mark and the play mark; the `.slnx` is not a row beside it. 2) FILES lists `Early`. 3) The root becomes "Late" with Early below it. Agent-run on its own test data folder.
- **Covers:** REQ-UI-125

### Owner's test 3: from nothing, New project in a folder you name

- **Sign in as:** your own account
- **Steps:** 1) File ▸ Close. 2) Press "New project" (under Get started, or on "Nothing is open yet"). 3) Click "Console App". 4) Solution name `FromNothing`, Project name `TryConsole`, Location `C:\1MyCode\Chatur\bin`. 5) Press Create.
- **Expected:** One Location field, empty until you type. Under it Solution folder `C:\1MyCode\Chatur\bin\FromNothing`, Solution file `C:\1MyCode\Chatur\bin\FromNothing\FromNothing.slnx` and Project folder `C:\1MyCode\Chatur\bin\FromNothing\TryConsole`. The folder `bin` does not need a `FromNothing` folder first: Create makes it. After Create `bin` holds the new folder `FromNothing` and nothing else new; inside it are `FromNothing.slnx` and `TryConsole\TryConsole.csproj` with `Program.cs`. The main window is on FromNothing, FILES shows FromNothing with TryConsole below it, and the target picker reads TryConsole.
- **Covers:** REQ-FN-310, REQ-FN-309, REQ-NFR-013

### Owner's test 4: add projects from this machine's templates

- **Sign in as:** your own account
- **Steps:** 1) File ▸ New project. 2) Search `blazor`, click "Blazor Web App", type `TryWeb` in Name, press Create. 3) The same with "Class Library" `TryLib`, then "xUnit Test Project" `TryLib.Tests`.
- **Expected:** The dialog reads "New project in FromNothing"; only the name is typed. It shows "Will be created in" `C:\1MyCode\Chatur\bin\FromNothing`, the solution file "already there", and Project folder `…\FromNothing\TryWeb`. Each project is one folder inside the solution's folder, below the root in FILES.
- **Covers:** REQ-UI-112, REQ-UI-126

### Owner's test 5: the project just added is the one that runs

- **Sign in as:** your own account
- **Steps:** 1) After TryWeb is made, look at the target picker and FILES. 2) After TryLib and TryLib.Tests, look again.
- **Expected:** After TryWeb the picker reads TryWeb and FILES marks it with the play mark. A library and a test project cannot run, so they leave TryWeb chosen.
- **Covers:** REQ-FN-309, REQ-UI-106

### Owner's test 6: run the web app

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing` the picker reads TryWeb; press Run. 2) Press Stop.
- **Expected:** Output opens by itself: `> dotnet build …`, the build's lines, `Now listening on: http://localhost:<port>`, `Opening http://localhost:<port> in the browser`; your browser opens there. The line `Failed to determine the https port for redirect.` is a warning and is not drawn red as an error. Stop writes `Run stopped.`.
- **Covers:** REQ-UI-107, REQ-FN-306

### Owner's test 7: the console app, a broken build and the exit code

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing`: target picker ▸ TryConsole; press Run. 2) FILES ▸ TryConsole ▸ Program.cs; delete the `;` at the end of its line; Ctrl+S; press Run. 3) Put the `;` back; Ctrl+S.
- **Expected:** 1) `Hello, World!`, then `Process exited with code 0.`. 2) `The program was not started.`; the label reads "build failed".
- **Covers:** REQ-UI-107, REQ-UI-108

### Owner's test 8: the test project

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\FromNothing`: target picker ▸ TryLib.Tests; press Run.
- **Expected:** `> dotnet test …`, a passed summary with 0 failed, and `Process exited with code 0.`.
- **Covers:** REQ-UI-107

### Owner's test 9: several projects start together

- **Sign in as:** your own account
- **Steps:** 1) Target picker ▸ "Multiple startup projects…". 2) Tick TryConsole and TryWeb; Save. 3) Press Run. 4) Press Stop.
- **Expected:** 1) Only the projects that can run are listed: TryConsole and TryWeb. 2) The picker reads "2 startup projects"; FILES marks both. 3) Both are built, then started together; every Output line carries its project's name, e.g. `[TryConsole] Hello, World!` and `[TryWeb] Now listening on: …`. 4) Both end: `All 2 projects ended.`.
- **Covers:** REQ-FN-324

### Owner's test 10: the choice is kept, and one project again

- **Sign in as:** your own account
- **Steps:** 1) File ▸ Close; reopen FromNothing from the project switcher. 2) Target picker ▸ TryWeb. 3) Press Run.
- **Expected:** 1) The picker still reads "2 startup projects": the choice is kept for the solution. 2) It reads TryWeb. 3) Only TryWeb runs; its lines carry no project name.
- **Covers:** REQ-FN-324

### Owner's test 11: the file window

- **Sign in as:** your own account
- **Steps:** 1) FILES ▸ TryConsole ▸ Program.cs. 2) Look beside Save. 3) Type a line in the editor. 4) Press the × at the right end of the tab bar (the whole file view), then "Keep editing". 5) Press that × again, then "Close without saving".
- **Expected:** 2) Save, and no other button: nothing opens the file in another program (Chatur is the editor). 4) "Close without saving?" asks; Keep editing leaves the file view and your text as they were. 5) The file view closes and the chat takes the window; the file on disk is as it was.
- **Covers:** REQ-UI-041, REQ-FN-045, REQ-FN-046

### Owner's test 12: a folder with a project and no solution

- **Sign in as:** your own account
- **Steps:** 1) In a terminal: `dotnet new console -n Loose -o C:\1MyCode\Chatur\bin\LooseOne\Loose`. 2) File ▸ Close; "Open a folder"; `C:\1MyCode\Chatur\bin\LooseOne`; Open. 3) Press "Go back", Open again, then "Open as it is".
- **Expected:** 2) "This folder has no solution" names `Loose\Loose.csproj` and asks before anything opens. 3) Go back opens nothing and keeps the path; Open as it is opens the folder.
- **Covers:** REQ-FN-325

### Owner's test 13: the solution takes the project in, and the new one runs

- **Sign in as:** your own account
- **Steps:** 1) With LooseOne open: File ▸ New project; "Console App"; Solution name `Gathered`, Project name `Second`. 2) Press Create.
- **Expected:** 1) "…\LooseOne has a project and no solution. Create makes the solution there and adds it to it: Loose\Loose.csproj". 2) LooseOne holds `Gathered.slnx`, `Loose` and `Second`. FILES has one root, "Gathered", with Loose and Second below it, and Second, just added, is the one that runs. A class library added this way would not run, so it would not be chosen.
- **Covers:** REQ-UI-124, REQ-FN-309, REQ-UI-125

### Owner's test 14: your model key, for the parts that ask a model

- **Sign in as:** your own account
- **Steps:** 1) Tools ▸ Settings (Ctrl+,): Model providers opens in the second window. 2) If your Anthropic provider is listed as connected, close the window: nothing to do. 3) Else press "Add a provider"; Name `Anthropic`; Connector Anthropic; paste your key; press Test, then Add; close the window.
- **Expected:** 2) or 3) The table shows your Anthropic provider, connected, with claude-opus-5-5, claude-sonnet-5-5 and claude-haiku-5-5. Each account keeps its own keys.
- **Covers:** REQ-FN-014, REQ-FN-017, REQ-UI-019

### Owner's test 15: in an empty folder, the Analyst's own commands

- **Sign in as:** your own account (the key of part 14 connected)
- **Steps:** 1) Make and open the empty folder `C:\1MyCode\Chatur\bin\BrainTest`, as in test 1. 2) Model picker ▸ your model; agent picker ▸ Analyst. 3) Type `Don't you have brain-storm command ?`; Send. 4) `*help`; Send. 5) `*yolo`; Send. 6) Look at BrainTest in Explorer.
- **Expected:** The greeting lists 12 commands. 3) "Yes." with `*brainstorm {topic}`, named "Chatur (no model)". 4) The 12 commands as a numbered list, names drawn as code, no `**`. 5) "YOLO is on" with **Go ahead** in bold. 6) BrainTest is still empty: chatting that never reached a model made no file.
- **Covers:** REQ-FN-308, REQ-FN-311, REQ-UI-114, REQ-FN-040

### Owner's test 16: the chat follows its newest message

- **Sign in as:** your own account
- **Steps:** 1) In test 15, after `*help` and again after `*yolo`, look at the conversation without scrolling.
- **Expected:** The newest message is in view each time, though the agent's greeting above it is tall.
- **Covers:** REQ-UI-128

### Owner's test 17: the Analyst brainstorms with you, then writes the brief

- **Sign in as:** your own account
- **Steps:** 1) In `C:\1MyCode\Chatur\bin\BrainTest`, type `*brainstorm a small to-do app for one person`; Send. 2) Answer its questions (one person, desktop, small, focused, yes save it; ideas: due dates, a today view, quick add). 3) Say "That is enough ideas, save the session". 4) `*create-project-brief BrainTest`; Send; say "take the sensible reading and write the brief now" if it asks.
- **Expected:** 3) `docs\brainstorming-session-results.md` appears in FILES. 4) The reply ends "Chatur: *create-project-brief is done — docs/BrainTest-Brief.md is written"; FILES shows it, dated today.
- **Covers:** REQ-FN-312, REQ-UI-128

### Owner's test 18: the sessions are written under the project

- **Sign in as:** your own account
- **Steps:** 1) After test 17 open `docs\metrics\sessions.jsonl` in BrainTest from FILES.
- **Expected:** One or more lines, each with `"harness":"chatur"`, the session's id, the model, and `input_tokens` and `output_tokens` as numbers: the format TfLens reads. No other file is in `docs\metrics`.
- **Covers:** REQ-FN-040

### Owner's test 19: the measurements screens

- **Sign in as:** your own account
- **Steps:** 1) In the project switcher at the top pick `BrainTest` (nothing to type). 2) Project ▸ Measurements. 3) Period ▸ Last 7 days, then All history. 4) Click the first line; Esc. 5) Save as zip ▸ Save ▸ Close.
- **Expected:** 2) Seven tiles: the chat's sessions counted and their tokens, the same as the chat's own token count; a split by your model; Misses with first-pass rate; a missing figure reads "not measured", a cost "no price". 4) The line is a chat session: command `chat`, model, tokens, time. 5) A .zip in BrainTest.
- **Covers:** REQ-UI-115 to REQ-UI-120, REQ-FN-314, REQ-NFR-014, REQ-NFR-020, REQ-FN-040

### Owner's test 20: across projects

- **Sign in as:** your own account
- **Steps:** 1) Click "Measurements across projects". 2) Click "Measurements" under Corrections, then its link.
- **Expected:** 1) One line per project, BrainTest among them. 2) Back on Project measurements.
- **Covers:** REQ-FN-313, REQ-UI-121

### Owner's test 21: what is left on each key

- **Sign in as:** your own account
- **Steps:** 1) With BrainTest picked, Project ▸ Measurements; find "What is left on each key". 2) On your Anthropic line press "Type a limit"; Limit `5.00`; leave Counting from as it is; Save. 3) Click "Measurements across projects". 4) Back on Project measurements: "Change limit", then "Remove the limit".
- **Expected:** 1) One line per connected provider; Anthropic reads "not known": a normal key has no balance, and no limit is typed. 2) "Chatur's own count": Limit $5.00, Used a few cents (the calls since install), Left $5.00 less that. 3) The same line. 4) "not known" again.
- **Covers:** REQ-UI-129, REQ-UI-131, REQ-FN-326, REQ-NFR-022

### Owner's test 22: one question to two models

- **Sign in as:** your own account
- **Steps:** 1) Second window ▸ "Compare models". 2) Tick only `claude-haiku-5-5`; Run. 3) Also tick `claude-sonnet-5-5`; Kind ▸ "Question, changes nothing". 4) Task: `Which files are in the docs folder of this project? Answer in one sentence.` 5) Run. 6) Right if a card names the files in FILES ▸ docs, else Wrong. 7) Reopen Compare models; click the first Past comparison.
- **Expected:** 2) "One model is not a comparison — pick at least two." 5) Two cards side by side: time, tokens, cost, answer; no file changed. 7) Answers and marks again; `docs\metrics\runs.jsonl` has two `compare-models` lines.
- **Covers:** REQ-FN-327, REQ-FN-328, REQ-UI-132, REQ-UI-133, REQ-FN-330, REQ-NFR-021, REQ-NFR-022

### Owner's test 23: one task that changes files, on two models

- **Sign in as:** your own account
- **Steps:** 1) On Compare models, with `claude-haiku-5-5` and `claude-sonnet-5-5` ticked, Kind ▸ "Task that changes files". 2) Task: `Add a file docs/notes.md with the one line: Made in Compare models.` 3) Press Run and wait for both cards. 4) Look at FILES ▸ docs in BrainTest, and in Explorer at `C:\1MyCode\Chatur\bin\BrainTest\bin`.
- **Expected:** 3) Each card lists its changed files, `docs/notes.md` added with its line, beside its time, tokens and cost. 4) BrainTest has no `docs\notes.md`: each model worked in its own copy. The copies were made under BrainTest's own bin folder and are gone; no `chatur-compare` folder is left there.
- **Covers:** REQ-FN-329, REQ-UI-132, REQ-NFR-022

### Owner's test 24: one command to each agent

- **Sign in as:** your own account (the key of part 14 connected)
- **Steps:** 1) In BrainTest, before each agent: agent picker ▸ the agent; new chat; your model; `*yolo` (not the Verifier, which has none). 2) Analyst: `*research-prompt note-taking apps for one person that work offline`. 3) Architect: `*research which local database suits a desktop to-do app for one person`. 4) Flow master: `*develop-status BrainTest`. 5) Verifier: `*report all BrainTest`.
- **Expected:** 2) `docs\BrainTest-Research-Prompt.md` appears in FILES. 3) The same file is written again. 4) "No end-to-end run", or its step, rows and tokens. 5) "This project has no checklist yet, so there is nothing to report." (BrainTest has no checklist).
- **Covers:** REQ-FN-317, REQ-FN-322, REQ-FN-320, REQ-NFR-017

### Mac owner's test 1: an empty folder, New project, and the disk

- **Sign in as:** your own account
- **Steps:** 1) Cmd+Space, type `Chatur`, Return: "Nothing is open yet". 2) Open a folder; type `/Volumes/MacD/MyCode/Chatur/bin/OwnerTest` (empty); Open. 3) In Chatur's own menu bar, under the title bar: File ▸ New project. 4) Search `blazor`; click "Blazor Web App"; Solution name `Checking-Chatur`. 5) Create.
- **Expected:** As Windows part 1, with `/` paths: Solution folder `…/OwnerTest/Checking-Chatur`, Solution file `…/Checking-Chatur/Checking-Chatur.slnx`, Project folder `…/Checking-Chatur/Checking-Chatur`; nothing directly in OwnerTest.
- **Covers:** REQ-FN-310, REQ-UI-126, REQ-UI-127, REQ-NFR-019, REQ-NFR-013, REQ-UI-008

### Mac owner's test 2: the solution is the root of FILES

- **Sign in as:** your own account
- **Steps:** 1) After test 1 look at FILES. 2) In Terminal: `dotnet new console -n Early -o /Volumes/MacD/MyCode/Chatur/bin/LateSln/Early`. File ▸ Close; open `/Volumes/MacD/MyCode/Chatur/bin/LateSln` as a folder (Open as it is). 3) In Terminal: `cd /Volumes/MacD/MyCode/Chatur/bin/LateSln`, `dotnet new sln --name Late`, `dotnet sln add Early/Early.csproj`; press the refresh button in FILES.
- **Expected:** 1) One root, "Checking-Chatur", solution mark; its project has the project and play marks. 2) FILES lists `Early`. 3) The root becomes "Late" with Early below it.
- **Covers:** REQ-UI-125

### Mac owner's test 3: from nothing, New project in a folder you name

- **Sign in as:** your own account
- **Steps:** 1) File ▸ Close. 2) Press "New project". 3) Click "Console App". 4) Solution name `FromNothing`, Project name `TryConsole`, Location `/Volumes/MacD/MyCode/Chatur/bin`. 5) Create.
- **Expected:** Solution folder `/Volumes/MacD/MyCode/Chatur/bin/FromNothing`, Solution file `…/FromNothing/FromNothing.slnx`, Project folder `…/FromNothing/TryConsole`. Create makes `FromNothing`; inside are `FromNothing.slnx` and `TryConsole/TryConsole.csproj` with `Program.cs`. The main window is on FromNothing; the target picker reads TryConsole.
- **Covers:** REQ-FN-310, REQ-FN-309, REQ-NFR-013

### Mac owner's test 4: add projects from this machine's templates

- **Sign in as:** your own account
- **Steps:** 1) File ▸ New project. 2) Search `blazor`, click "Blazor Web App", Name `TryWeb`, Create. 3) The same with "Class Library" `TryLib`, then "xUnit Test Project" `TryLib.Tests`.
- **Expected:** "New project in FromNothing"; only the name is typed; "Will be created in" `/Volumes/MacD/MyCode/Chatur/bin/FromNothing`, the solution file "already there", Project folder `…/FromNothing/TryWeb`. Each project is one folder below the root in FILES.
- **Covers:** REQ-UI-112, REQ-UI-126

### Mac owner's test 5: the project just added is the one that runs

- **Sign in as:** your own account
- **Steps:** 1) After TryWeb is made, look at the target picker and FILES. 2) After TryLib and TryLib.Tests, look again.
- **Expected:** After TryWeb the picker reads TryWeb and FILES gives it the play mark. A library and a test project cannot run, so TryWeb stays chosen.
- **Covers:** REQ-FN-309, REQ-UI-106

### Mac owner's test 6: run the web app

- **Sign in as:** your own account
- **Steps:** 1) In FromNothing, with the picker on TryWeb, press Run. 2) Press Stop.
- **Expected:** Output opens: `> dotnet build …`, the build's lines, `Now listening on: http://localhost:<port>`, `Opening http://localhost:<port> in the browser`; your default browser (Safari or another) opens there. `Failed to determine the https port for redirect.` is not red. Stop writes `Run stopped.`.
- **Covers:** REQ-UI-107, REQ-FN-306

### Mac owner's test 7: the console app, a broken build and the exit code

- **Sign in as:** your own account
- **Steps:** 1) Target picker ▸ TryConsole; Run. 2) FILES ▸ TryConsole ▸ Program.cs; delete the `;` at the end of its line; press Save (beside the tabs); Run. 3) Put the `;` back; press Save.
- **Expected:** 1) `Hello, World!`, then `Process exited with code 0.`. 2) `The program was not started.`; the label reads "build failed".
- **Covers:** REQ-UI-107, REQ-UI-108

### Mac owner's test 8: the test project

- **Sign in as:** your own account
- **Steps:** 1) Target picker ▸ TryLib.Tests; press Run.
- **Expected:** `> dotnet test …`, a passed summary with 0 failed, and `Process exited with code 0.`.
- **Covers:** REQ-UI-107

### Mac owner's test 9: several projects start together

- **Sign in as:** your own account
- **Steps:** 1) Target picker ▸ "Multiple startup projects…". 2) Tick TryConsole and TryWeb; Save. 3) Run. 4) Stop.
- **Expected:** 1) Only TryConsole and TryWeb are listed. 2) The picker reads "2 startup projects". 3) Both build, then start; lines read `[TryConsole] Hello, World!` and `[TryWeb] Now listening on: …`. 4) `All 2 projects ended.`.
- **Covers:** REQ-FN-324

### Mac owner's test 10: the choice is kept, and one project again

- **Sign in as:** your own account
- **Steps:** 1) File ▸ Close; reopen FromNothing from the project switcher. 2) Target picker ▸ TryWeb. 3) Run, then Stop.
- **Expected:** 1) The picker still reads "2 startup projects". 2) It reads TryWeb. 3) Only TryWeb runs; its lines carry no project name.
- **Covers:** REQ-FN-324

### Mac owner's test 11: the file window

- **Sign in as:** your own account
- **Steps:** 1) FILES ▸ TryConsole ▸ Program.cs. 2) Look beside Save. 3) Type a line in the editor. 4) Press the × at the right end of the tab bar, then "Keep editing". 5) Press that × again, then "Close without saving".
- **Expected:** 2) Save and no other button. 4) "Close without saving?" asks; Keep editing changes nothing. 5) The file view closes, the chat takes the window, and the file on disk is unchanged.
- **Covers:** REQ-UI-041, REQ-FN-045, REQ-FN-046

### Mac owner's test 12: a folder with a project and no solution

- **Sign in as:** your own account
- **Steps:** 1) In Terminal: `dotnet new console -n Loose -o /Volumes/MacD/MyCode/Chatur/bin/LooseOne/Loose`. 2) File ▸ Close; Open a folder; `/Volumes/MacD/MyCode/Chatur/bin/LooseOne`; Open. 3) Press "Go back", Open again, then "Open as it is".
- **Expected:** 2) "This folder has no solution" names `Loose/Loose.csproj` and asks first. 3) Go back opens nothing and keeps the path; Open as it is opens the folder.
- **Covers:** REQ-FN-325

### Mac owner's test 13: the solution takes the project in, and the new one runs

- **Sign in as:** your own account
- **Steps:** 1) With LooseOne open: File ▸ New project; "Console App"; Solution name `Gathered`, Project name `Second`. 2) Create.
- **Expected:** 1) "…/LooseOne has a project and no solution. Create makes the solution there and adds it to it: Loose/Loose.csproj". 2) LooseOne holds `Gathered.slnx`, `Loose` and `Second`; FILES has one root, "Gathered", with Loose and Second; Second is the one that runs.
- **Covers:** REQ-UI-124, REQ-FN-309, REQ-UI-125

### Mac owner's test 14: your model key, for the parts that ask a model

- **Sign in as:** your own account
- **Steps:** 1) Tools ▸ Settings (Cmd+,): Model providers opens in the second window. 2) If Anthropic is listed as connected, close the window (red button, top left). 3) Else "Add a provider"; Name `Anthropic`; Connector Anthropic; paste your key; Test, then Add; close the window.
- **Expected:** The table shows Anthropic, connected, with claude-opus-5-5, claude-sonnet-5-5 and claude-haiku-5-5. The key goes to the Mac's Keychain.
- **Covers:** REQ-FN-014, REQ-FN-017, REQ-UI-019

### Mac owner's test 15: in an empty folder, the Analyst's own commands

- **Sign in as:** your own account (the key of part 14 connected)
- **Steps:** 1) Make and open the empty folder `/Volumes/MacD/MyCode/Chatur/bin/BrainTest`, as in test 1. 2) Model picker ▸ `claude-sonnet-5-5`; agent picker ▸ Analyst. 3) Type `Don't you have brain-storm command ?`; Send. 4) `*help`; Send. 5) `*yolo`; Send. 6) Look at BrainTest in Finder.
- **Expected:** The greeting lists 12 commands. 3) "Yes." with `*brainstorm {topic}`, "Chatur (no model)". 4) The 12 commands, numbered, as code, no `**`. 5) "YOLO is on" with **Go ahead** in bold. 6) BrainTest is still empty.
- **Covers:** REQ-FN-308, REQ-FN-311, REQ-UI-114, REQ-FN-040

### Mac owner's test 16: the chat follows its newest message

- **Sign in as:** your own account
- **Steps:** 1) In test 15, after `*help` and again after `*yolo`, look at the conversation without scrolling.
- **Expected:** The newest message is in view each time.
- **Covers:** REQ-UI-128

### Mac owner's test 17: the Analyst brainstorms with you, then writes the brief

- **Sign in as:** your own account
- **Steps:** 1) In BrainTest, with `claude-sonnet-5-5` and Analyst: `*brainstorm a small to-do app for one person`; Send. 2) Answer: one person, desktop, small, focused, yes save it; ideas: due dates, a today view, quick add. 3) `That is enough ideas, save the session`. 4) `*create-project-brief BrainTest`; Send; if it asks, `take the sensible reading and write the brief now`.
- **Expected:** 3) `docs/brainstorming-session-results.md` appears in FILES. 4) The reply ends "Chatur: *create-project-brief is done — docs/BrainTest-Brief.md is written".
- **Covers:** REQ-FN-312, REQ-UI-128

### Mac owner's test 18: the sessions are written under the project

- **Sign in as:** your own account
- **Steps:** 1) Open `docs/metrics/sessions.jsonl` in BrainTest from FILES.
- **Expected:** Lines with `"harness":"chatur"`, the session's id, the model, and `input_tokens` and `output_tokens` as numbers. No other file in `docs/metrics`.
- **Covers:** REQ-FN-040

### Mac owner's test 19: the measurements screens

- **Sign in as:** your own account
- **Steps:** 1) Project switcher ▸ `BrainTest`. 2) Project ▸ Measurements. 3) Period ▸ Last 7 days, then All history. 4) Click the first line; Esc. 5) Save as zip ▸ Save ▸ Close.
- **Expected:** 2) Seven tiles; the chat's sessions and tokens; a split by model; Misses with first-pass rate; "not measured" and "no price" where nothing is known. 4) A chat line: command `chat`, model, tokens, time. 5) A .zip in BrainTest.
- **Covers:** REQ-UI-115 to REQ-UI-120, REQ-FN-314, REQ-NFR-014, REQ-NFR-020, REQ-FN-040

### Mac owner's test 20: across projects

- **Sign in as:** your own account
- **Steps:** 1) Click "Measurements across projects". 2) Click "Measurements" under Corrections, then its link.
- **Expected:** 1) One line per project, BrainTest among them. 2) Back on Project measurements.
- **Covers:** REQ-FN-313, REQ-UI-121

### Mac owner's test 21: what is left on each key

- **Sign in as:** your own account
- **Steps:** 1) BrainTest, Project ▸ Measurements, "What is left on each key". 2) On Anthropic: "Type a limit"; Limit `5.00`; Save. 3) "Measurements across projects". 4) Back: "Change limit", then "Remove the limit".
- **Expected:** 1) Anthropic reads "not known". 2) "Chatur's own count": Limit $5.00, Used a few cents, Left the rest. 3) The same line. 4) "not known" again.
- **Covers:** REQ-UI-129, REQ-UI-131, REQ-FN-326, REQ-NFR-022

### Mac owner's test 22: one question to two models

- **Sign in as:** your own account
- **Steps:** 1) Second window ▸ "Compare models". 2) Tick only `claude-haiku-5-5`; Run. 3) Also tick `claude-sonnet-5-5`; Kind ▸ "Question, changes nothing". 4) Task: `Which files are in the docs folder of this project? Answer in one sentence.` 5) Run. 6) Right if a card names the files in FILES ▸ docs, else Wrong. 7) Reopen Compare models; click the first Past comparison.
- **Expected:** 2) "One model is not a comparison — pick at least two." 5) Two cards: time, tokens, cost, answer. 7) Answers and marks again; `docs/metrics/runs.jsonl` has two `compare-models` lines.
- **Covers:** REQ-FN-327, REQ-FN-328, REQ-UI-132, REQ-UI-133, REQ-FN-330, REQ-NFR-021, REQ-NFR-022

### Mac owner's test 23: one task that changes files, on two models

- **Sign in as:** your own account
- **Steps:** 1) Compare models, `claude-haiku-5-5` and `claude-sonnet-5-5` ticked, Kind ▸ "Task that changes files". 2) Task: `Add a file docs/notes.md with the one line: Made in Compare models.` 3) Run; wait for both cards. 4) Look at FILES ▸ docs, and in Finder at `/Volumes/MacD/MyCode/Chatur/bin/BrainTest/bin`.
- **Expected:** 3) Each card lists `docs/notes.md` added. 4) BrainTest has no `docs/notes.md`, and no `chatur-compare` folder is left in its bin.
- **Covers:** REQ-FN-329, REQ-UI-132, REQ-NFR-022

### Mac owner's test 24: one command to each agent

- **Sign in as:** your own account (the key of part 14 connected)
- **Steps:** 1) In BrainTest, before each agent: agent picker ▸ the agent; new chat; model `claude-sonnet-5-5`; `*yolo` (not the Verifier). 2) Analyst: `*research-prompt note-taking apps for one person that work offline`. 3) Architect: `*research which local database suits a desktop to-do app for one person`. 4) Flow master: `*develop-status BrainTest`. 5) Verifier: `*report all BrainTest`.
- **Expected:** 2) `docs/BrainTest-Research-Prompt.md` appears. 3) The same file is written again. 4) "No end-to-end run", or its step, rows and tokens. 5) "This project has no checklist yet, so there is nothing to report."
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
- **Expected:** An unsaved tab is marked and asks before closing, and so does closing the whole file view. Build and Run stream their lines into Output; Stop ends everything the run started. The reply names its model and tokens. The chat closes and comes back.
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
- **Steps:** 1) Pick a project that has `docs\metrics` in the project switcher. 2) Project ▸ Measurements; pick each period; open a run line; Save as zip. 3) "Measurements across projects" in the side list; click a project's name.
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
- The Mac app runs on the .NET interpreter, not precompiled: precompiled, its screens never appeared (TR-030 in `docs/Chatur-TrBlazeUI-Feedback.md`). It starts a little slower.
- A Mac built without the GitHub Packages key uses TrBlazeUI 2.2.5 and TechieRag 1.3.0 instead of 2.2.3 and 1.2.1; add the key to the Mac's NuGet settings to build the pinned versions.
- The default agent, Analyst, explains a code change and does not make it; Flow master makes it.
- TF-052 (`docs/Chatur-TechieFlow-Feedback.md`): late in a long verify the screen check reads three chat menus as "opens nothing"; they open by hand. Open with the framework's team; it does not block using the app.
- TR-025, TR-026 (`docs/Chatur-TrBlazeUI-Feedback.md`): a submenu's edge distance at 390 px; a picker's list cannot be wider than its button. TR-029: no read-only markdown view in TrBlazeUI; Chatur draws replies with Markdig.
- The screen check cannot drive the Windows app by address (its web view reloads); the Windows app is checked by its own test files, which click through it.
- A message for a window that has just closed is written to the log as one information line, not as an error (REQ-NFR-011).
- The install script reads the App Manager key from this project's user secrets; without it, it stops and says so.

## Platform notes

Windows and Mac Catalyst are equal targets. The Mac app is not sandboxed (it runs dotnet, a shell and your programs), and when started from Finder or Spotlight it adds `/etc/paths`, `/etc/paths.d`, Homebrew and `/usr/local/share/dotnet` to its PATH, so `dotnet`, `git` and `node` are found. The Windows app builds only on Windows, the Mac app only on a Mac. Tokens and keys live in Credential Manager on Windows and the Keychain on the Mac; the database and the log live in the user's application-data folder. A copy started by a test keeps its data under `tests/.artifacts/app-data` and never opens the owner's.
