# Chatur — Business Requirements — Phase 4: More users

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 4 of 4 |
| Status | Draft |
| Date | 2026-09-21 |

## 1. Summary

The first three phases make Chatur good enough for the owner. This one makes it something another developer can be given. Feedback, issues, licences and feature control go through App Manager, which already does all four, and role and rule data is synced between machines through it rather than carried in a file. Chatur's verifier gains the thing its own build needed all along: it drives the windows of the Mac and Windows applications it has built, so a native head is checked the way a web page already is. The owner, by now working on many projects at once, gets one view across all of them and the ability to work in separate working copies so two runs never fight over the same files. Chatur's actions are offered to other tools, so something outside can start a process. And the installers are signed, which is what turns a download the owner trusts into one a stranger can.

## 2. Screens and flow

One row per routed page this phase adds; dialogs are written exactly as in the phase-1 BRD.

| Screen | Route | Role | Mockup | Fields |
|---|---|---|---|---|
| Feedback and issues | `/feedback` | Owner, Licensed developer | [mockup](mockups/feedback.html) | what it is about, wording, state, raised, answer |
| Licence | `/licence` | Owner, Licensed developer | [mockup](mockups/licence.html) | licence, days remaining, devices, features |
| All projects | `/all-projects` | Owner, Licensed developer | [mockup](mockups/all-projects.html) | project, phase, open, verified, last worked |
| Worktrees | `/worktrees` | Owner | [mockup](mockups/worktrees.html) | working copy, branch, run, path, state |
| Integrations | `/integrations` | Owner | [mockup](mockups/integrations.html) | action, what starts it, address, state |

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

## 4. Development status

Written by the status gate after every build, verify and handoff; not by hand.

**Snapshot as of 2026-09-22.** Live per-requirement status: `PROJECT-STATUS.md` and the Requirements Status table in `docs/Chatur-P4-Checklist.md`.

| Screen | Requirements | Verified | Open | Status |
|---|---|---|---|---|
| Feedback and issues | 4 | 0 | 4 | Planned |
| Licence | 4 | 0 | 4 | Planned |
| All projects | 4 | 0 | 4 | Planned |
| Worktrees | 3 | 0 | 3 | Planned |
| Integrations | 4 | 0 | 4 | Planned |

## 5. Where the rest lives

| What | Where |
|---|---|
| Scope, users and roles, the context diagram | [phase 1 BRD](Chatur-BRD.md) |
| Non-functional requirements for the whole application | [phase 1 BRD](Chatur-BRD.md) |
| Constraints, assumptions and risks | [phase 1 BRD](Chatur-BRD.md) |
| Every phase, its screens and its BRD range | [Chatur-Phases.md](Chatur-Phases.md) |
| This phase's work list | [Chatur-P4-Checklist.md](Chatur-P4-Checklist.md) |
