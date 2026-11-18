# Chatur — Usage Guide

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Large |
| Date | 2026-11-17 |

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
dotnet run --project src/Chatur -f net10.0-windows10.0.19041.0
dotnet run --project src/Chatur.WebHarness --urls http://localhost:5280
```

The fourth line starts the Windows app; sign in as user 1. The fifth serves the same screens at http://localhost:5280 for the browser tests.

## How to test, screen by screen

### First task 1: start the Windows app and sign in

- **Sign in as:** your own account, or user 1
- **Steps:** 1) In PowerShell, type `cd C:\1MyCode\Chatur` and press Enter. 2) Type `dotnet run --project src\Chatur -f net10.0-windows10.0.19041.0` and press Enter. 3) On Sign in, type your email and password and press Sign in.
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
- **Expected:** The card goes and `Greeter\Program.cs` now holds `Console.WriteLine("Good evening from Chatur");`. Output shows `> dotnet run --project "C:\1MyCode\ChaturPractice\Greeter\Greeter.csproj" -c Debug --no-launch-profile`, then `Good evening from Chatur`, then `Process exited.`, and the label beside Output reads "stopped". Once in three runs on 17 Nov the first line was shown twice; pressing Run again showed it once.
- **Covers:** REQ-UI-013, REQ-UI-014

### First task 6: open a file from the start window

- **Sign in as:** your own account, or user 1
- **Steps:** 1) Pick "All projects…" from the project name in the top bar to reach the start window, or start from the empty start. 2) Press "Open a file". 3) In the box "The file", type `C:\1MyCode\ChaturPractice\Greeter\Program.cs`. 4) Press Open.
- **Expected:** The main window opens with Program.cs open in a tab. The project is the nearest solution or repository folder above the file; ChaturPractice has neither, so it is the file's own folder, Greeter.
- **Covers:** REQ-UI-094

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
- Sign out hangs on "Loading Chatur" until Chatur is reopened.
- The default agent, Analyst, explains a code change and does not make it; Flow master makes it.
- TF-034, TF-035, TF-036 (`docs/Chatur-TechieFlow-Feedback.md`): open with the framework's team; none blocks using the app.
- TR-025, TR-026 (`docs/Chatur-TrBlazeUI-Feedback.md`): a submenu's edge distance at 390 px; a picker's list cannot be wider than its button.
- Phase 4 (licence, account, feedback, integrations, working copies, all projects) is not built: those pages are placeholders.

## Platform notes

Windows and Mac Catalyst are equal targets. The Windows app builds only on Windows, the Mac app only on a Mac. Tokens and keys live in Credential Manager on Windows and the Keychain on the Mac; the database and the log live in the user's application-data folder. A copy started by a test keeps its data under `tests/.artifacts/app-data` and never opens the owner's.
