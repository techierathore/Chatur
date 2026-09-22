# Chatur — UI Design — Phase 4: More users

| | |
|---|---|
| App | Chatur |
| Kind | app |
| Size | Small |
| Phase | 4 of 4 |

## Screens

Each of these opens as a tab in the one window, with the conversation still pinned to its right.

### Screen: Feedback and issues (`/feedback`)

**Mockup:** [mockups/feedback.html](mockups/feedback.html) · **Roles:** Owner, Licensed developer · **BRD:** BRD-137 to BRD-140

| Region | Control | Shows or binds |
|---|---|---|
| `feedback-form` | Select ×2 + Input + Textarea + Switch | what it is about, how bad it is, the title, the detail, and whether to attach this session |
| `feedback-destination` | text | where this entry will go, named before it is sent |
| `feedback-table` | DataTable | six entries: id, what it is about, title, when raised, the state, the answer in short |
| `feedback-queued-note` | Alert (info) | the two held since the network went at 09:12, and that they go by themselves |
| `feedback-routing` | Panel | everything goes through App Manager; an entry about a library reaches that library's own team |

| Field | Type | Required | Validation |
|---|---|---|---|
| About | select | yes | Chatur itself, TrBlazeUI, TechieRag or App Manager; never blank |
| Severity | select | yes | blocker, major or minor; major to begin with |
| Title | text | yes | 4 to 80 characters, one line |
| Detail | textarea | yes | at least 20 characters |
| Attach this session | switch | no | on by default |

**Dialogs opened here:** none — the field that fails is named under the form, never in a dialog

**States:** empty: nothing raised — one line saying the form above is the whole of it · loading: the refresh spins and the rows dim, keeping their values · error: App Manager unreachable — the note says when it last tried and offers Try again, and the form still takes entries and queues them

### Screen: Licence (`/licence`)

**Mockup:** [mockups/licence.html](mockups/licence.html) · **Roles:** Owner, Licensed developer · **BRD:** BRD-141 to BRD-144

| Region | Control | Shows or binds |
|---|---|---|
| `licence-panel` | Card + Progress | the name, the state, the expiry, the days left, the seats, the key with a copy button, and the days used |
| `licence-warning` | Alert (warning) | the server's own message about two devices against a cap of one |
| `devices-table` | DataTable | each machine: platform, app version, last seen, sign-ins, its state, and Forget and Block |
| `features-panel` | Switch ×6 | the features this licence carries, two off |
| `features-note` | Panel | a feature that is off is drawn nowhere — no menu entry, no tab, no button |

| Field | Type | Required | Validation |
|---|---|---|---|
| — | — | — | nothing is typed; Forget on the machine you are using warns that you sign in again here, and Block on a blocked machine reads Unblock |

**Dialogs opened here:** Forget this device · Block this device — both confirm first

**States:** empty: one machine only — no warning row and a single row in the table · loading: the recheck spins, the pills go grey, the bar keeps its width · error: App Manager unreachable — the state reads when it was last checked and that Chatur is working offline, the licence stays usable for its grace days, and no switch is touched

### Screen: All projects (`/all-projects`)

**Mockup:** [mockups/all-projects.html](mockups/all-projects.html) · **Roles:** Owner, Licensed developer · **BRD:** BRD-145 to BRD-148

| Region | Control | Shows or binds |
|---|---|---|
| `projects-controls` | Input + Select + Tabs | the search, the sort, and Cards or Table |
| `projects-tiles` | Card ×4 | projects, open requirements, verified, sessions this week |
| `projects-table` | DataTable | six projects: folder, phase, requirements, open, verified, last worked, state, and Open |
| `sync-panel` | Card + Button | when agents and rules last synced, the two machines and their state, and Sync now |
| `sync-note` | Panel | a change on one machine reaches the other at the next sync |

| Field | Type | Required | Validation |
|---|---|---|---|
| Search | search | no | free text; a folder that cannot be read is greyed and says so rather than being dropped |
| Sort | select | no | last worked on, name, or open requirements |

**Dialogs opened here:** none

**States:** empty: no projects — a blank state offering Add a project; a search matching nothing says so and offers to clear it · loading: the tiles read as dashes and the last rows stay, dimmed · error: a failed sync marks that machine and offers Try again; the project rows are read locally and a sync failure never touches them

### Screen: Worktrees (`/worktrees`)

**Mockup:** [mockups/worktrees.html](mockups/worktrees.html) · **Roles:** Owner · **BRD:** BRD-149 to BRD-151

| Region | Control | Shows or binds |
|---|---|---|
| `worktrees-table` | DataTable | four copies: folder, branch, who is using it, its state, when it was made, and Remove |
| `add-worktree-dialog` | Dialog | the branch, the folder, and whether the next run takes its own copy |
| `isolation-note` | Panel | every file a run changes stays in its own copy; each copy has its own branch; two runs never share one |
| `worktree-facts` | Card | how many copies, how much disk, the oldest, how many are stale |

| Field | Type | Required | Validation |
|---|---|---|---|
| Branch | select or typed | yes | a branch already checked out elsewhere offers that copy instead of making a second |
| Folder | text | yes | must be empty and outside the project folder; one with files in it says so and Add stays inert |
| Give the next run its own copy | switch | no | on by default |

**Dialogs opened here:** Add a working copy

**States:** empty: only the owner's own folder — one line saying so, with the same dialog · loading: a copy being made appears at once, marked, with Remove disabled · error: no disk, or a branch that has gone — the row says it could not be made with the reason from the repository, and offers Try again; Remove on a copy a run holds is refused and names the run

### Screen: Integrations (`/integrations`)

**Mockup:** [mockups/integrations.html](mockups/integrations.html) · **Roles:** Owner · **BRD:** BRD-152 to BRD-155

| Region | Control | Shows or binds |
|---|---|---|
| `actions-table` | DataTable + Switch | six actions: what it does, what starts it, its address, its state, and its switch; two are off |
| `example-call` | CodeBlock | one call and the answer it gives, with a copy button; the token is shown once, and an action that is off answers as missing |
| `verifier-windows` | Card + DataTable | the verifier driving the windows of the Mac and Windows applications Chatur built, and the last three checks with their screenshots |
| `downloads-panel` | DataTable | the Mac and the Windows download: what signed each, whether it was notarised, and when |

| Field | Type | Required | Validation |
|---|---|---|---|
| Action on or off | switch | no | turning one on with no token made warns first |
| Address | read-only | — | Chatur owns it; it is never typed |

**Dialogs opened here:** Make a token — shown once and never again

**States:** empty: no token yet — the example panel says to make one to see a call that works · loading: a switch shows a half state while App Manager is told, and goes back if it refuses · error: App Manager unreachable — a row saying the actions are still served but their state has not reached App Manager, with the switches left alone

## Where the rest lives

| What | Where |
|---|---|
| The window, the theme and the design system | [phase 1 UI design](Chatur-UIDesign.md) |
| The click-through flow across every phase | [phase 1 UI design](Chatur-UIDesign.md) |
| This phase's requirements | [Chatur-P4-BRD.md](Chatur-P4-BRD.md) |
