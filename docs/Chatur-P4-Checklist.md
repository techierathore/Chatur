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
| REQ-UI-062 | Say what is wrong from inside Chatur | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: sending feedback does not raise an entry or list it; the list panel still shows "What came back No feedback yet Sent entries and their answers appear here." (no row for the sent m` (tests/.artifacts/test-results/feedback-REQ-UI-062-feedba-40ea1-anager-and-listed-as-raised/test-failed-1.png) | [view](#d-req-ui-062) |
| REQ-FN-073 | Raise it against the library it belongs to | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: the Feedback form offers no library picker (only Kind and Message), so an entry cannot be tied to a library; form text: Send feedback Kind Issue Message Send Feedback is kept and` (tests/.artifacts/test-results/feedback-REQ-FN-073-an-ent-822fd-brary-name-the-owner-picked/test-failed-1.png) | [view](#d-req-fn-073) |
| REQ-UI-063 | See what came back | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: no feedback row exists to inspect and the app offers no seam to produce an answered entry from App Manager (list shows the empty state).` (tests/.artifacts/test-results/feedback-REQ-UI-063-an-ans-d236b-e-and-the-answer-in-its-row/test-failed-1.png) | [view](#d-req-ui-063) |
| REQ-FN-074 | Nothing is lost when the network is down | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: the app has no network-loss seam for the server-side App Manager call, and the sent message is not kept anywhere visible (list panel: "What came back No feedback yet Sent entries` (tests/.artifacts/test-results/feedback-REQ-FN-074-feedba-797ed-nt-when-the-network-returns/test-failed-1.png) | [view](#d-req-fn-074) |
| REQ-UI-064 | The licence in force | Needs re-verify | 0% | 2026-11-05 verify: ⚠ render — anchored control "palette-pop" is not on the page on Licence @1280 (tests/.artifacts/verify/screens/licence-1280.png) | [view](#d-req-ui-064) |
| REQ-UI-065 | Too many machines is reported | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: the app offers no stub licence source to make the server send an over-cap warning, and forcing the live test account over its cap would change the shared App Manager account` (tests/.artifacts/test-results/licence-REQ-UI-065-Licence-5ebc1-cap-warning-the-server-sent/test-failed-1.png) | [view](#d-req-ui-065) |
| REQ-UI-066 | Forget a machine | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: no stub licence source exists to hold a device safely (Forget controls on page: 0); forgetting a real device on the shared App Manager would change the account` (tests/.artifacts/test-results/licence-REQ-UI-066-Forgett-29ae7-e-list-and-ends-its-sign-in/test-failed-1.png) | [view](#d-req-ui-066) |
| REQ-FN-075 | A feature switched off is not offered | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: no stub licence source to switch a feature off, and changing the live licence on the shared App Manager is forbidden` (tests/.artifacts/test-results/licence-REQ-FN-075-A-featu-77818-nce-is-not-offered-anywhere/test-failed-1.png) | [view](#d-req-fn-075) |
| REQ-FN-076 | Everything at once | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/all-projects-REQ-FN-076-ev-7e544-and-when-it-was-last-worked/test-failed-1.png) | [view](#d-req-fn-076) |
| REQ-UI-067 | Where each one stands | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/all-projects-REQ-UI-067-a--ac0ca-n-and-how-many-are-verified/test-failed-1.png) | [view](#d-req-ui-067) |
| REQ-FN-077 | Go straight in | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/all-projects-REQ-FN-077-op-ac920-s-it-and-opens-Project-home/test-failed-1.png) | [view](#d-req-fn-077) |
| REQ-FN-078 | The roles follow the developer | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: a second machine under the same account cannot be driven from this harness (the Second test user is a different account, so its roles are separate), and a sync panel on All projec` (tests/.artifacts/test-results/all-projects-REQ-FN-078-ro-1ef43-syncing-through-App-Manager/test-failed-1.png) | [view](#d-req-fn-078) |
| REQ-FN-079 | Work in a separate copy | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: Worktrees draws no "Add a working copy" control (add-worktree), so a copy cannot be added from the screen.` (tests/.artifacts/test-results/worktrees-REQ-FN-079-a-wor-1e9f6-ith-its-path-and-its-branch/test-failed-1.png) | [view](#d-req-fn-079) |
| REQ-FN-080 | Give a run its own copy | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: Worktrees has no control that starts a run in a chosen working copy, so where a run writes its files cannot be observed from the browser.` (tests/.artifacts/test-results/worktrees-REQ-FN-080-a-run-aab39-hat-copy-not-in-the-owner-s/test-failed-1.png) | [view](#d-req-fn-080) |
| REQ-UI-068 | Clear it away | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: not measured: Worktrees draws no "Add a working copy" control (add-worktree), so a copy cannot be added from the screen.` (tests/.artifacts/test-results/worktrees-REQ-UI-068-remov-a50f3-list-and-deletes-its-folder/test-failed-1.png) | [view](#d-req-ui-068) |
| REQ-FN-081 | The actions are published | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/integrations-REQ-FN-081-In-ed651-t-starts-it-and-its-address/test-failed-1.png) | [view](#d-req-fn-081) |
| REQ-FN-082 | Something outside can start one | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/integrations-REQ-FN-082-an-2496c-ocess-and-the-run-is-listed/test-failed-1.png) | [view](#d-req-fn-082) |
| REQ-FN-083 | The verifier drives real windows | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/integrations-REQ-FN-083-th-e472d-w-and-keeps-the-screenshots/test-failed-1.png) | [view](#d-req-fn-083) |
| REQ-UI-069 | Signed downloads | FAIL | 0% | 2026-11-05 verify: ⚠ acceptance — `Error: expect(locator).toBeVisible() failed` (tests/.artifacts/test-results/integrations-REQ-UI-069-a--a2aff-ads-and-the-signature-state/test-failed-1.png) | [view](#d-req-ui-069) |

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
