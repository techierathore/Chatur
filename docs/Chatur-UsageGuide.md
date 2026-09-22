# Chatur — Usage Guide

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Large |
| Date | 2026-09-22 |

## Test users

Chatur signs in through App Manager, so a test user is a real App Manager account on the Chatur application. None of these exists yet; they are made once, at the start of phase 1 step 1, and never invented again by an agent.

| # | User | Password source | Role | Exists |
|---|---|---|---|---|
| 1 | chatur-owner@techierathore.com | user secrets key `Chatur:TestUsers:Owner` | User (the owner's own account shape) | no |
| 2 | chatur-second@techierathore.com | user secrets key `Chatur:TestUsers:Second` | User (a second machine on the same account) | no |
| 3 | chatur-yolo-tester@techierathore.com | user secrets key `Chatur:TestUsers:Yolo` | User (unattended runs) | no |

## Execution guide

Prerequisites: .NET 10 SDK, the MAUI workloads, Node 20 or newer with Playwright for the browser checks, and a NuGet credential for the owner's GitHub Packages feed (TrBlazeUI and TechieRag live there, not on nuget.org). A Mac builds the Mac Catalyst head; a Windows machine builds the Windows head.

```
dotnet restore Chatur.sln
dotnet run --project src/ChaturDb
dotnet build Chatur.sln -c Debug
dotnet run --project src/Chatur.WebHarness
dotnet build src/Chatur -c Debug -f net10.0-maccatalyst
dotnet build src/Chatur -c Debug -f net10.0-windows10.0.19041.0
```

Open http://localhost:5280 for the web harness — the views in a browser, which is how the automated screen checks see them — and sign in as user 1. The Mac and Windows heads are the real application and are started from the build output.

Commands that depend on projects not yet built are marked in the roadmap: `ChaturDb` exists from step 1, `Chatur.WebHarness` from step 1, and the MAUI heads from step 1; nothing here waits on a later step.

## How to test, screen by screen

### Sign in

- **Sign in as:** user 1
- **Steps:** 1) Start Chatur. 2) Type the email and password. 3) Leave "remember this device" on and press Sign in.
- **Expected:** The start window opens. Quitting and starting again does not ask for the password. A wrong password shows App Manager's own message and nothing else.
- **Covers:** REQ-UI-001, REQ-UI-002, REQ-FN-001, REQ-FN-002, REQ-FN-003

### Register

- **Sign in as:** nobody — this makes user 2
- **Steps:** 1) From Sign in, choose "Create an account". 2) Fill in the name, the email and a password that breaks the rule. 3) Correct it and submit.
- **Expected:** The rule list marks what is missing while typing, the weak password is refused before anything is sent, and the finished account lands in the start window.
- **Covers:** REQ-FN-004, REQ-UI-003, REQ-FN-005, REQ-FN-006

### Start window

- **Sign in as:** user 1
- **Steps:** 1) Read the recent list. 2) Open a folder that holds a solution. 3) Come back and use "Clone a repository" on a small GitHub repository.
- **Expected:** Recent projects list newest first with their paths; opening one lands in the main window with its files down the left; the clone finishes and opens.
- **Covers:** REQ-UI-004 to REQ-UI-008, REQ-FN-007, REQ-FN-008

### Main window

- **Sign in as:** user 1
- **Steps:** 1) Ask the chosen agent for a small change. 2) Watch the steps. 3) Approve the change. 4) Press Run.
- **Expected:** The agent is already chosen with a reason beside the box; every step appears as it happens; the file is read-only until the change is settled; approving writes it; Run streams the output.
- **Covers:** REQ-UI-009 to REQ-UI-020, REQ-FN-009 to REQ-FN-024

### Prerequisites

- **Sign in as:** user 1
- **Steps:** 1) Project ▸ Prerequisites. 2) Read the tool list. 3) Copy a fix for a missing tool.
- **Expected:** Every tool the project needs, with the version found or "not found" and a command ready to copy; Chatur's own version and commit; the update offer when a newer nightly exists.
- **Covers:** REQ-UI-016 to REQ-UI-018, REQ-FN-012, REQ-FN-013

### Settings

- **Sign in as:** user 1
- **Steps:** 1) Tools ▸ Settings. 2) Add a provider with a pasted key and test it. 3) Reorder a tier on Routing. 4) Change the theme on Appearance.
- **Expected:** The provider answers and its models appear in routing; the chain keeps the new order after a restart; the theme repaints every window at once and survives a restart; no secret is in the database or the log.
- **Covers:** REQ-UI-021 to REQ-UI-033, REQ-FN-025 to REQ-FN-036, REQ-UI-044, REQ-FN-048

### Repository

- **Sign in as:** user 1
- **Steps:** 1) Make a change and approve it. 2) Open the Repository. 3) Tick two files, write a message with a requirement id and check in. 4) Push.
- **Expected:** The changed files list with their diffs; the check-in commits only the ticked files; the ahead count moves; an agent asking to run a source-control command is refused and shown.
- **Covers:** REQ-UI-041 to REQ-UI-044, REQ-FN-044 to REQ-FN-048

### An empty folder

- **Sign in as:** user 1
- **Steps:** 1) Start window ▸ New empty folder. 2) Ask the agent to think a product through. 3) Let it propose a brief.
- **Expected:** Build and Run are dead because there is nothing to build; the conversation has the whole window; the Analyst is suggested; nothing is written until the proposal is approved.
- **Covers:** REQ-UI-009, REQ-UI-010, REQ-FN-009, REQ-FN-010

## Automated tests

```
dotnet test Chatur.Tests
npx playwright test tests/playwright --config tests/playwright/chatur.config.js
```

The xUnit suite covers `Chatur.Core` — every guard class for what it allows and what it refuses, the routing chain and its fallback, the measurement writer's refusals, and the agent loop. The Playwright suite drives `Chatur.WebHarness` and checks every view against its mockup at 1440 and 820 in both themes. The Windows head is driven through the Windows test bridge; the Mac head once the Mac is registered in `core-config.yaml`.

## Known limitations

- The Mac app window is not checked automatically until the owner's Mac is registered in `core-config.yaml`; until then a Mac-only fault is caught by the owner using the nightly build (BRD-94).
- Sign-in needs App Manager to be reachable the first time on each machine. Whether a stored token lets Chatur work with no internet afterwards is an open question in the BRD.
- Phase 1 has no debugger, no property grid and no package-manager screens; the editor is for quick edits (BRD scope).
- TechieRag must gain sign-in other than a key, the OpenAI Responses style, streamed tool calls, a fallback chain of any length, and pause-and-resume before phase 1 step 2 — `docs/Chatur-TechieRag-Feedback.md` when the first entry is filed.
- Installers are not signed before phase 4 (BRD-155).

## Platform notes

Mac Catalyst and Windows are equal from the first release, and every requirement is verified on the Mac first, then on Windows (BRD-94). The Mac Catalyst head builds only on a Mac; the Windows head only on Windows. Tokens live in the Keychain on the Mac and Credential Manager on Windows, and the database and the log file live in the user's application-data folder on both.
