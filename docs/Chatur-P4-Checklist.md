# Chatur — Checklist (phase 4)

| | |
|---|---|
| App | Chatur |
| Size | Small |
| Phase | 4 of 4 |

## Goal

Phase 4, more users: feedback, issues, licences and feature control through App Manager, agents and rules synced between machines, one view across every project, a working copy per run, Chatur's actions offered to other tools, and signed downloads. This checklist is the whole work list of the phase.

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-UI-062 | Say what is wrong from inside Chatur | Not Started | 0% | — | [view](#d-req-ui-062) |
| REQ-FN-073 | Raise it against the library it belongs to | Not Started | 0% | — | [view](#d-req-fn-073) |
| REQ-UI-063 | See what came back | Not Started | 0% | — | [view](#d-req-ui-063) |
| REQ-FN-074 | Nothing is lost when the network is down | Not Started | 0% | — | [view](#d-req-fn-074) |
| REQ-UI-064 | The licence in force | Not Started | 0% | — | [view](#d-req-ui-064) |
| REQ-UI-065 | Too many machines is reported | Not Started | 0% | — | [view](#d-req-ui-065) |
| REQ-UI-066 | Forget a machine | Not Started | 0% | — | [view](#d-req-ui-066) |
| REQ-FN-075 | A feature switched off is not offered | Not Started | 0% | — | [view](#d-req-fn-075) |
| REQ-FN-076 | Everything at once | Not Started | 0% | — | [view](#d-req-fn-076) |
| REQ-UI-067 | Where each one stands | Not Started | 0% | — | [view](#d-req-ui-067) |
| REQ-FN-077 | Go straight in | Not Started | 0% | — | [view](#d-req-fn-077) |
| REQ-FN-078 | The roles follow the developer | Not Started | 0% | — | [view](#d-req-fn-078) |
| REQ-FN-079 | Work in a separate copy | Not Started | 0% | — | [view](#d-req-fn-079) |
| REQ-FN-080 | Give a run its own copy | Not Started | 0% | — | [view](#d-req-fn-080) |
| REQ-UI-068 | Clear it away | Not Started | 0% | — | [view](#d-req-ui-068) |
| REQ-FN-081 | The actions are published | Not Started | 0% | — | [view](#d-req-fn-081) |
| REQ-FN-082 | Something outside can start one | Not Started | 0% | — | [view](#d-req-fn-082) |
| REQ-FN-083 | The verifier drives real windows | Not Started | 0% | — | [view](#d-req-fn-083) |
| REQ-UI-069 | Signed downloads | Not Started | 0% | — | [view](#d-req-ui-069) |

**Status values:** `Not Started` · `In Progress` · `Implemented` · `Verified` · `Done (pre-existing)` · `Needs re-verify` · `PARTIAL` · `FAIL` · `Blocked` · `Owner-UAT` · `N/A`.

## Page: Feedback and issues (`/feedback`)

<a id="d-req-ui-062"></a>
- **REQ-UI-062** — Say what is wrong from inside Chatur. *BRD:* BRD-137 · *Mockup:* mockups/feedback.html
  - *Acceptance:* When the owner sends feedback on Feedback and issues, then it is raised through App Manager and appears in the list as raised.

<a id="d-req-fn-073"></a>
- **REQ-FN-073** — Raise it against the library it belongs to. *BRD:* BRD-138 · *Mockup:* mockups/feedback.html
  - *Acceptance:* When the entry is about a library on Feedback and issues, then the owner picks that library and the entry carries its name.

<a id="d-req-ui-063"></a>
- **REQ-UI-063** — See what came back. *BRD:* BRD-139 · *Mockup:* mockups/feedback.html
  - *Acceptance:* When an entry has been answered on Feedback and issues, then its row shows the state and the answer.

<a id="d-req-fn-074"></a>
- **REQ-FN-074** — Nothing is lost when the network is down. *BRD:* BRD-140 · *Mockup:* mockups/feedback.html
  - *Acceptance:* When feedback cannot be sent on Feedback and issues, then it is kept and sent when the network returns.

## Page: Licence (`/licence`)

<a id="d-req-ui-064"></a>
- **REQ-UI-064** — The licence in force. *BRD:* BRD-141 · *Mockup:* mockups/licence.html
  - *Acceptance:* When Licence opens, then the licence's name, state and days remaining are shown.

<a id="d-req-ui-065"></a>
- **REQ-UI-065** — Too many machines is reported. *BRD:* BRD-142 · *Mockup:* mockups/licence.html
  - *Acceptance:* When the account is over the licence's device cap on Licence, then Licence shows the warning the server sent.

<a id="d-req-ui-066"></a>
- **REQ-UI-066** — Forget a machine. *BRD:* BRD-143 · *Mockup:* mockups/licence.html
  - *Acceptance:* When the owner forgets a device on Licence, then it leaves the device list and its sign-in no longer works.

<a id="d-req-fn-075"></a>
- **REQ-FN-075** — A feature switched off is not offered. *BRD:* BRD-144 · *Mockup:* mockups/licence.html
  - *Acceptance:* When a feature is off for this licence on Licence, then it is not offered anywhere in Chatur.

## Page: All projects (`/all-projects`)

<a id="d-req-fn-076"></a>
- **REQ-FN-076** — Everything at once. *BRD:* BRD-145 · *Mockup:* mockups/all-projects.html
  - *Acceptance:* When All projects opens, then every project Chatur knows is listed with its phase and when it was last worked.

<a id="d-req-ui-067"></a>
- **REQ-UI-067** — Where each one stands. *BRD:* BRD-146 · *Mockup:* mockups/all-projects.html
  - *Acceptance:* When a project has requirements on All projects, then its row shows how many are open and how many are verified.

<a id="d-req-fn-077"></a>
- **REQ-FN-077** — Go straight in. *BRD:* BRD-147 · *Mockup:* mockups/all-projects.html
  - *Acceptance:* When the owner opens a project from the view, then it becomes the selected project and Project home opens.

<a id="d-req-fn-078"></a>
- **REQ-FN-078** — The roles follow the developer. *BRD:* BRD-148 · *Mockup:* mockups/all-projects.html
  - *Acceptance:* When roles and rules are changed on one machine, then the other machine has them after syncing through App Manager.

## Page: Worktrees (`/worktrees`)

<a id="d-req-fn-079"></a>
- **REQ-FN-079** — Work in a separate copy. *BRD:* BRD-149 · *Mockup:* mockups/worktrees.html
  - *Acceptance:* When the owner adds a working copy for a branch on Worktrees, then it is listed with its path and its branch.

<a id="d-req-fn-080"></a>
- **REQ-FN-080** — Give a run its own copy. *BRD:* BRD-150 · *Mockup:* mockups/worktrees.html
  - *Acceptance:* When a run is started in a working copy, then every file it changes is in that copy and not in the owner's.

<a id="d-req-ui-068"></a>
- **REQ-UI-068** — Clear it away. *BRD:* BRD-151 · *Mockup:* mockups/worktrees.html
  - *Acceptance:* When the owner removes a working copy on Worktrees, then it leaves the list and its folder is gone.

## Page: Integrations (`/integrations`)

<a id="d-req-fn-081"></a>
- **REQ-FN-081** — The actions are published. *BRD:* BRD-152 · *Mockup:* mockups/integrations.html
  - *Acceptance:* When Integrations opens, then every action Chatur offers is listed with what starts it and its address.

<a id="d-req-fn-082"></a>
- **REQ-FN-082** — Something outside can start one. *BRD:* BRD-153 · *Mockup:* mockups/integrations.html
  - *Acceptance:* When an outside tool calls a published action on Integrations, then the matching process starts and the run is listed.

<a id="d-req-fn-083"></a>
- **REQ-FN-083** — The verifier drives real windows. *BRD:* BRD-154 · *Mockup:* mockups/integrations.html
  - *Acceptance:* When the verifier checks a Mac or Windows application Chatur built on Integrations, then it drives that application's own window and keeps the screenshots.

<a id="d-req-ui-069"></a>
- **REQ-UI-069** — Signed downloads. *BRD:* BRD-155 · *Mockup:* mockups/integrations.html
  - *Acceptance:* When a release is published on Integrations, then the Mac and Windows downloads are signed and the screen shows the signature's state.
