# Chatur — Checklist (phase 1)

| | |
|---|---|
| App | Chatur |
| Size | Large |
| Phase | 1 of 4 |

## Goal

Phase 1, the Workbench: Chatur at its smallest but whole. Sign in, open a project from the start window, direct an agent in the main window, approve every change before it reaches a file, build and run, and check the work in yourself. Everything that is not the work — prerequisites, the repository, settings — lives in one other window. This checklist is the whole work list of the phase.

## Verdict convention (REQ-NFR-004)

Every requirement is verified on Mac Catalyst first, then on Windows (BRD-94). Once a row is verified, its Remarks cell names both machines, in the form:

`[REQ-xxx] Mac: <verdict>; Windows: <verdict>`

where `<verdict>` is `Verified` (an executed verify run passed), `PARTIAL — <one-line reason>`, `FAIL — <one-line reason>`, or `not registered (owner)` for the machine this session has no runtime-verification endpoint for. A row earns `Implemented` from a builder's own smoke; only an executed verify run naming both machines writes `Verified`.

Mac Catalyst verification needs `runtimeVerification.appium.maccatalyst` set in `.tfcore/core-config.yaml`, which is commented out as of this phase — the owner has not registered a Mac. Until that changes, no row can honestly carry a Mac verdict past `not registered (owner)`; an absent endpoint degrades to `⚠ STATIC-ONLY`, never a faked pass. REQ-NFR-004 itself stays `PARTIAL` for that reason (see its row below).

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-UI-001 | Sign in with an account | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-001 sign in opens Projects`; Sign in renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-001) |
| REQ-FN-001 | The password never leaves the machine as text | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-001 the password App Manager receives is encrypted, n`; Sign in renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-001) |
| REQ-UI-002 | A refused sign-in says why | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-002 wrong password shows the server message and stays`; Sign in renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-002) |
| REQ-FN-002 | The installation names its device | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-002 the device id survives a restart`; Sign in renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-002) |
| REQ-FN-003 | Staying signed in | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-003 reopening Chatur while signed in opens Projects w`; Sign in renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-003) |
| REQ-FN-004 | Register an account | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-004 register with a valid password creates the accoun`; Register renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-004) |
| REQ-UI-003 | The password rule is visible while typing | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-003 password rule shows which parts are met while typ`; Register renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-003) |
| REQ-FN-005 | A weak password is caught before it is sent | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-005 weak password is refused and the field is marked`; Register renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-005) |
| REQ-UI-004 | An email already in use is reported | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-004 email already in use shows the server message and`; Register renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-004) |
| REQ-UI-005 | Name the folders Chatur searches | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-005 adding a folder in the Project folders dialog lis`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-005) |
| REQ-UI-006 | Chatur lists what it found | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-006 a named folder with projects lists each with name`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-006) |
| REQ-UI-007 | Choose the project to work on | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-007 opening a project from the list opens the Workben`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-007) |
| REQ-FN-006 | The choice survives a restart | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-006 the selected project is still selected after clos`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-006) |
| REQ-UI-008 | Nothing named yet says what to do | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-008 with no folder named Projects shows an empty stat`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-008) |
| REQ-UI-009 | Remove a folder | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-009 removing a folder drops its projects and the fold`; Start renders and looks right @1280/390; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-009) |
| REQ-FN-007 | What the selected project is | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-007 selected project named in the top bar with its br`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-007) |
| REQ-FN-008 | Straight into the work | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-008 a project opens straight onto a conversation with`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-008) |
| REQ-UI-010 | Change project without going back | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-010 picking another project in the top bar makes the `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-010) |
| REQ-FN-009 | Sign out | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-009 signing out returns to Sign in and the stored tok`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-009) |
| REQ-UI-011 | Only targets that fit the machine | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-011 only targets this machine can build are choosable`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-011) |
| REQ-UI-012 | Build the project | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-012 building shows the result as succeeded or failed `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-012) |
| REQ-UI-013 | Run the project | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-013 running the chosen target starts the program and `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-013) |
| REQ-UI-014 | Watch the output live | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-014 a line the program writes appears in the output p`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-014) |
| REQ-UI-015 | Stop everything the run started | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-015 stopping ends every program the run started and R`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-015) |
| REQ-UI-016 | A failed build shows the errors | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-016 a failed build lists each error with its file and`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-016) |
| REQ-FN-010 | The last target is offered first | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-010 returning to Run offers the target used last time`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-010) |
| REQ-FN-011 | Every tool, with the version found | Verified | 100% | 2026-10-03 verify: PASS — test `Prerequisites REQ-FN-011 every tool listed with the version `; Prerequisites renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-011) |
| REQ-UI-017 | A missing tool says how to fix it | Verified | 100% | 2026-10-03 verify: PASS — test `Prerequisites REQ-UI-017 a missing tool shows the command th`; Prerequisites renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-017) |
| REQ-UI-018 | Which Chatur this is | Verified | 100% | 2026-10-03 verify: PASS — test `Prerequisites REQ-UI-018 Doctor shows Chatur own version and`; Prerequisites renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-018) |
| REQ-FN-012 | Homebrew tools are found on a Mac | Needs re-verify | 75% | 2026-10-03 verify: not verified — every test named REQ-FN-012 was skipped; screen Prerequisites renders and looks right; acceptance not measured (1 test(s) skipped, none ran); mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-012) |
| REQ-FN-013 | A newer build is offered | Verified | 100% | 2026-10-03 verify: PASS — test `Prerequisites REQ-FN-013 a newer nightly is offered with a d`; Prerequisites renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-013) |
| REQ-FN-014 | Connect a source with a pasted key | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-014 a pasted-key provider is listed as connected with`; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-014) |
| REQ-FN-015 | Connect a local model | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-015 a local model with its web address is listed as c`; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-015) |
| REQ-FN-016 | Sign in to a ChatGPT subscription | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-016 browser sign-in shows a sign-in code and Cancel s`; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-016) |
| REQ-FN-017 | Secrets go to the machine's own store | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-017 a pasted key never appears on the page or in the `; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-017) |
| REQ-UI-019 | Check that a source answers | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-019 testing a provider shows on its row whether it an`; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-019) |
| REQ-UI-020 | Remove a source | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-020 removing a provider takes it out of the list`; Settings / providers renders and looks right @1280/390; matches its mockup; mockup-parity: 5 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-020) |
| REQ-FN-018 | A tier for each role | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-018 the owner can set a role tier on the Routing page`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-018) |
| REQ-FN-019 | A tier for each kind of work | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-019 the owner can set a kind of work to a tier on the`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-019) |
| REQ-FN-020 | Order the fallback chain | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-020 the owner can reorder the models in a tier on the`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-020) |
| REQ-FN-021 | A limited model steps aside | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-021 a limited model steps aside to the next in the ch`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-021) |
| REQ-FN-022 | Repeated failed fixes climb a tier | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-022 repeated failed builds after an approved change c`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-022) |
| REQ-FN-023 | Routing is remembered | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-023 routing settings are as they were left after reop`; Settings / routing renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-023) |
| REQ-FN-024 | Choose who to talk to | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-024 opening a project shows the agent Chatur chose an`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-024) |
| REQ-FN-025 | Ask and be answered | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-025 sending a message about the selected project show`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-025) |
| REQ-FN-026 | Which model answered | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-026 a reply names the model that produced it`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-026) |
| REQ-FN-027 | The reply arrives as it is written | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-027 the reply text grows in the conversation before t`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-027) |
| REQ-UI-021 | What it cost | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-021 a finished reply shows the tokens it used and the`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-021) |
| REQ-UI-022 | Watch what the agent is doing | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-022 a file read by the agent appears in the activity `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-022) |
| REQ-UI-023 | Stop the work | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-023 Stop stops the agent mid-reply, the activity pane`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-023) |
| REQ-UI-024 | Ask me first, or go ahead | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-024 the mode set on the session is kept with the sess`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-024) |
| REQ-UI-025 | Roles come from the database | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-025 roles tab shows a role's new wording after it is `; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-025) |
| REQ-UI-026 | Rights at a glance | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-026 each role shows what it may do, its commands and `; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-026) |
| REQ-FN-028 | Take the roles to the other machine | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-028 export writes one file holding every role, right,`; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-028) |
| REQ-FN-029 | Bring them in | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-029 importing the exported file brings back the same `; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-029) |
| REQ-UI-027 | Change what a role is | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-027 edited wording, commands and rules are saved and `; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-027) |
| REQ-UI-028 | Set the role's model tier | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-028 a tier set on the roles tab is the tier the routi`; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-028) |
| REQ-UI-029 | Every save keeps the one before | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-029 every save adds a version to the history with its`; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-029) |
| REQ-UI-030 | A role may only do what its rights allow | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-030 a role that cannot change code is refused, and th`; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-030) |
| REQ-FN-030 | Only the verifier marks work verified | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-030 (precondition reachable from Settings) only the v`; Settings / agents renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-030) |
| REQ-UI-031 | What has been worked on | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-031 Sessions lists each session with its project, rol`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-031) |
| REQ-FN-031 | Pick up where it stopped | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-031 continuing a session opens the Workbench with tha`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-031) |
| REQ-FN-032 | A session keeps its record | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-032 a reopened session keeps every message and refusa`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-032) |
| REQ-UI-032 | See the change itself | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-032 a proposed edit shows the file with its old and n`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-032) |
| REQ-FN-033 | Approve it | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-033 approving a change writes the new text to the fil`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-033) |
| REQ-FN-034 | Reject it | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-034 rejecting a change leaves the file untouched and `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-034) |
| REQ-FN-035 | Ask me first means ask | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-035 in ask me first no file is written until the owne`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-035) |
| REQ-FN-036 | Everything the agent asks for passes the guards | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-036 a refused request never reaches the tool: an agen`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-036) |
| REQ-UI-033 | A model never runs source control | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-033 a model asking to run a source-control command is`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-033) |
| REQ-UI-034 | A correction is written down | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-034 the corrections page lists a correction with what`; Settings / corrections renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-034) |
| REQ-FN-037 | Keep it | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-037 keeping a correction leaves it in force and moves`; Settings / corrections renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-037) |
| REQ-FN-038 | Undo it | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-038 undoing a correction brings the wording before it`; Settings / corrections renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-038) |
| REQ-FN-039 | Kept corrections feed the next build | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-039 exporting the seed data includes every kept corre`; Settings / corrections renders and looks right @1280/390; matches its mockup; mockup-parity: 1 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-039) |
| REQ-FN-040 | The five files | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-040 a session turn writes the five metrics files and `; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-040) |
| REQ-FN-041 | Chatur signs its work | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-041 a record Chatur writes names the tool chatur`; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-041) |
| REQ-FN-042 | What was not measured stays empty | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-042 a number that was not measured is written empty, `; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-042) |
| REQ-FN-043 | An unknown field is refused | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-043 one unknown field refuses the whole record and th`; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-fn-043) |
| REQ-UI-035 | A failed write never stops the work | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-035 when a measurement file cannot be written, the se`; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-035) |
| REQ-UI-036 | What has been written | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-036 each stream shows how many records it holds and t`; Settings / measurements renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-036) |
| REQ-UI-037 | Choose how Chatur looks | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-037 picking a theme, or light or dark, changes every `; Settings / appearance renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-037) |
| REQ-UI-038 | A theme is data, not code | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-038 a theme file added on Settings joins the list and`; Settings / appearance renders and looks right @1280/390; matches its mockup | [view](#d-req-ui-038) |
| REQ-UI-039 | Browse the project | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-039 the files card shows the solution and folders, an`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-039) |
| REQ-UI-040 | Open a file | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-040 opening a file from the tree shows its text in a `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-040) |
| REQ-FN-044 | Edit and save | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-044 changing the text and saving writes the new text `; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-044) |
| REQ-UI-041 | An unsaved tab is marked | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-041 an unsaved tab is marked, and closing it asks bef`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-041) |
| REQ-FN-045 | Open it in the owner's editor | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-045 choosing the owner's editor for a file reaches th`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-045) |
| REQ-FN-046 | Open it with whatever the machine uses | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-046 opening a file with the system default applicatio`; Workbench renders and looks right @1280/390; matches its mockup; mockup-parity: 11 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-046) |
| REQ-UI-042 | What changed | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-042 every changed file is listed with its state and c`; Repository renders and looks right @1280/390; matches its mockup; mockup-parity: 3 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-042) |
| REQ-UI-043 | Check in | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-043 checking in chosen files with a message commits t`; Repository renders and looks right @1280/390; matches its mockup; mockup-parity: 3 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-043) |
| REQ-UI-044 | Push and pull | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-UI-044 push and pull show the result and how far ahead o`; Repository renders and looks right @1280/390; matches its mockup; mockup-parity: 3 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-044) |
| REQ-FN-047 | Switch branch | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-047 switching branch names it in the header and shows`; Repository renders and looks right @1280/390; matches its mockup; mockup-parity: 3 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-047) |
| REQ-FN-048 | An automatic check-in goes to its own branch | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-FN-048 an automatic check-in goes to the run's own branc`; Repository renders and looks right @1280/390; matches its mockup; mockup-parity: 3 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-048) |
| REQ-NFR-001 | Every secret — model key, subscription token, App Manager token — is kept in the operating system's store for secrets, never in the database, a file or a log | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-NFR-001 a connected provider secret stays out of the dat` | [view](#d-req-nfr-001) |
| REQ-NFR-002 | Serilog file logging is wired at startup before anything else can fail | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-NFR-002 a broken startup still leaves a log file holding` | [view](#d-req-nfr-002) |
| REQ-NFR-003 | A database change that has shipped in a nightly build is never edited; a new one is added, and an update keeps the owner's data | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-NFR-003 migrating over existing data keeps every row` | [view](#d-req-nfr-003) |
| REQ-NFR-004 | Every requirement works on Mac Catalyst and on Windows, verified on the Mac first | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-NFR-004 the convention form names both machines` | [view](#d-req-nfr-004) |
| REQ-NFR-005 | Every guard class has unit tests covering what it allows and what it refuses | Verified | 100% | 2026-10-03 verify: PASS — test `REQ-NFR-005 source-control guard allows an ordinary tool(aTo` | [view](#d-req-nfr-005) |
| REQ-NFR-006 | A GitHub Actions workflow has a macOS job that builds the Mac Catalyst `.app` and zips it, and a Windows job that builds the unpackaged, self-contained Windows app (`WindowsPackageType` None) and zips it. Every push to main publishes a `nightly` pre-release with both downloads; a `v*` tag publishes a named release. Version and commit are stamped into the build, so Prerequisites shows them (BRD-29) and the newer-build check finds the release (BRD-31). No code signing (added 2026-09-30) | Needs re-verify | 75% | 2026-10-03 verify: checks pass, defect open — test `REQ-NFR-006 the workflow has a Mac job that zips the app` ⚠ miss 2026-09-30 (MISS-Chatur-20260930-01): scope promised the nightly download but no requirement carried it. | [view](#d-req-nfr-006) |

**Status values:** `Not Started` · `In Progress` · `Implemented` · `Verified` · `Done (pre-existing)` · `Needs re-verify` · `PARTIAL` · `FAIL` · `Blocked` · `Owner-UAT` · `N/A`.

## Page: Sign in (`/sign-in`)

<a id="d-req-ui-001"></a>
- **REQ-UI-001** — Sign in with an account. *BRD:* BRD-1 · *Mockup:* mockups/sign-in.html
  - *Acceptance:* When the owner enters a known email and password on Sign in, then Chatur opens the Projects screen signed in.

<a id="d-req-fn-001"></a>
- **REQ-FN-001** — The password never leaves the machine as text. *BRD:* BRD-2 · *Mockup:* mockups/sign-in.html
  - *Acceptance:* When Chatur sends a sign-in, then the password field carries the encrypted value and the plain password appears nowhere in the request.

<a id="d-req-ui-002"></a>
- **REQ-UI-002** — A refused sign-in says why. *BRD:* BRD-3 · *Mockup:* mockups/sign-in.html
  - *Acceptance:* When the password is wrong on Sign in, then Sign in shows the server's own message and the owner stays on the screen.

<a id="d-req-fn-002"></a>
- **REQ-FN-002** — The installation names its device. *BRD:* BRD-4 · *Mockup:* mockups/sign-in.html
  - *Acceptance:* When Chatur signs in, then it sends the device identifier it generated on first run, and sends the same one every time after.

<a id="d-req-fn-003"></a>
- **REQ-FN-003** — Staying signed in. *BRD:* BRD-5 · *Mockup:* mockups/sign-in.html
  - *Acceptance:* When the owner closes Chatur while signed in and opens it again, then it opens on Projects without asking for the password.

## Page: Register (`/register`)

<a id="d-req-fn-004"></a>
- **REQ-FN-004** — Register an account. *BRD:* BRD-6 · *Mockup:* mockups/register.html — **Implemented**
  - *Acceptance:* When the owner fills in name, email and a password that meets the rule, then the account is created and Chatur opens Projects.
  - *Remarks:* `AccountActions.RegisterAsync` calls `POST /AuthSvc/device-register` through the shared `IAppManagerClient` (cluster B), persists the session and opens `/start`. Smoked with a headless Playwright script (`tests/.artifacts/smoke-c/register-smoke.mjs`) against the WebHarness on port 5283 and the local App Manager at `https://localhost:32769`: filled the form with a strong password and a fresh email, submitted, and the browser landed on `/start` signed in (`tests/.artifacts/smoke-c/register-after-submit.png`, confirmed again from a fresh page load at `tests/.artifacts/smoke-c/start-page-check.png`). The one new account this created, `chatur-yolo-register-202609222248@techierathore.com`, is now in the Test users table (`docs/Chatur-UsageGuide.md`), password in user secrets key `Chatur:TestUsers:Register`. [fix 2026-09-23] Re-smoked, no new account created: the visual gap this row was PARTIAL for was closed by cluster D under REQ-UI-004 (`register-heading`/`register-subtitle`/`field-name`/etc. added, `input-confirm-password` renamed to `input-confirm`). `bash .tfcore/utils/tf-verify-screens.sh --screen register=/register --base http://localhost:5281 --login-path /sign-in --user chatur-yolo-tester@techierathore.com --password <Chatur:TestUsers:Yolo>` now reports `visual OK` at both 1280px and 390px (`tests/.artifacts/verify/screens/register-{1280,390}.png`); the only remaining anchor gap is `all-screens`, the mockup's own cross-screen gallery link with no route in the real app — the same accepted gap `SignIn.razor`'s fully-Verified rows carry.

<a id="d-req-ui-003"></a>
- **REQ-UI-003** — The password rule is visible while typing. *BRD:* BRD-7 · *Mockup:* mockups/register.html — **Implemented**
  - *Acceptance:* When the owner types a password on Register, then the screen shows which parts of the rule are met and which are not.
  - *Remarks:* `Register.razor`'s `RuleStatus` property (`Register.razor.cs`) calls `Chatur.Core.Accounts.PasswordRule.Evaluate(objPassword)` — the exact rule cluster C's `RegisterAsync`/`PasswordRule` also enforce server-side — on every render, so the four rows (`rule-length`, `rule-capital`, `rule-number`, `rule-special`, each a `LucideIcon` check/x plus text coloured `text-alert-success`/`text-destructive`) and the `strength-bars`/`strength-label`/`strength-value` ("N of 4 rules met") update live while typing, with no debounce needed since `@bind-Value` already re-renders per keystroke. The aggregate meter is the real `TrBlazeUI.Components.PasswordStrength` component (`Value`/`MinimumLength` params), not hand-built. Smoked with Playwright against the WebHarness on :5284 (`tests/.artifacts/smoke/smoke-ui-003-004.mjs`, screenshot `tests/.artifacts/smoke/ui003-rules-met.png`): empty password shows every rule unmet; `short` shows all four unmet; `Tflens2026!` shows all four met and "4 of 4 rules met"; removing the `!` flips `rule-special` back to unmet live. Also added the "both fields match" indicator (`confirm-ok`, mockups/register.html) via a `PasswordsMatch` property, screenshot `tests/.artifacts/smoke/ui003-confirm-match.png`.

<a id="d-req-fn-005"></a>
- **REQ-FN-005** — A weak password is caught before it is sent. *BRD:* BRD-8 · *Mockup:* mockups/register.html — **Implemented**
  - *Acceptance:* When the password breaks the rule on Register, then Register refuses to send and marks the field.
  - *Remarks:* `Chatur.Core.Accounts.PasswordRule` (shared with cluster D's live checklist, REQ-UI-003) is checked in `Register.razor.cs` before any call to `IAccountActions.RegisterAsync` — a failing password never reaches the network, and `AccountActions.RegisterAsync` checks the same rule again server-side as a second line of defence. Unit tests: `tests/unit/Chatur.Tests/PasswordRuleTests.cs` (4 cases — full pass, partial pass, empty/null, all pass). Smoked with the same Playwright script as REQ-FN-004: typing `weak` and submitting stayed on `/register` and showed `[data-testid="password-error"]` (`tests/.artifacts/smoke-c/register-weak-password.png`). [fix 2026-09-23] Re-smoked with `tests/.artifacts/smoke-c/register-weak-password-recheck.mjs` (a small equivalent of `register-smoke.mjs`'s weak-password half, updated for the renamed `input-confirm` testid) against WebHarness :5281 signed in as chatur-yolo-tester: submitting `weak`/`weak` stayed on `/register` with `[data-testid="password-error"]` visible (`tests/.artifacts/smoke-c/register-weak-password-recheck-2026-09-23.png`); no account created. The shared Register-screen mockup-anchor gap noted here previously is closed (see REQ-FN-004/REQ-UI-004); `tf-verify-screens.sh --screen register=/register` reports `visual OK` at both 1280 and 390.

<a id="d-req-ui-004"></a>
- **REQ-UI-004** — An email already in use is reported. *BRD:* BRD-9 · *Mockup:* mockups/register.html — **Implemented**
  - *Acceptance:* When the email already has an account on Register, then Register shows the server's message and keeps what was typed.
  - *Remarks:* `Register.razor.cs`'s `HandleSubmitAsync` (cluster C's method) sets `objEmailInUse = true` only in the branch where `IAccountActions.RegisterAsync` itself returns `Succeeded == false` (never for the client-side weak-password or mismatch checks above it), so the email `Field`/`FieldError` shows App Manager's own `SignInResult.ErrorMessage` verbatim plus a "Sign in instead" link to `/sign-in`. The `FieldError` stays mounted via `hidden="@(!objEmailInUse)"` (same pattern as `SignIn.razor`'s `signin-error`) so the anchor is always addressable for the verifier, with `aria-hidden` on the nested link to avoid a false "zero width" finding while hidden. No field is reset on refusal — `@bind-Value` alone already keeps everything typed. Also closed the shared Register-screen visual gap FN-004/FN-005 flagged (checklist rows above): added `register-heading`/`register-subtitle`, wrapped first/last name in `field-name`/`field-first-name`/`field-last-name`, added `field-password`, `field-confirm`, the `confirm-ok` match indicator, `device-note`/`device-name` (`Environment.MachineName`), and renamed `input-confirm-password` → `input-confirm` to match `mockups/register.html`. `bash .tfcore/utils/tf-verify-screens.sh --screen register=/register` now reports `visual OK`; the only remaining render finding is `all-screens`, the mockup's cross-screen gallery link with no equivalent route in the real app — `SignIn.razor`'s fully-`Verified` REQ-UI-001/002 carry the identical, accepted gap. Smoked against the real local App Manager (never the fake): registered with the existing `chatur-owner@techierathore.com` (an already-provisioned test user, never a new account) and got back "A user with this email already exists", stayed on `/register`, and the name/email/password fields were unchanged (`tests/.artifacts/smoke/smoke-ui-003-004.mjs`, screenshots `tests/.artifacts/smoke/ui004-before-submit.png` and `ui004-after-refusal.png`).

## Page: Start (`/start`)

<a id="d-req-ui-005"></a>
- **REQ-UI-005** — Name the folders Chatur searches. *BRD:* BRD-10 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner adds a folder in the Project folders dialog, then it appears in the folder list on Projects.

<a id="d-req-ui-006"></a>
- **REQ-UI-006** — Chatur lists what it found. *BRD:* BRD-11 · *Mockup:* mockups/start.html
  - *Acceptance:* When a named folder holds projects on Start, then Projects lists each one with its name, path and when it was last opened.

<a id="d-req-ui-007"></a>
- **REQ-UI-007** — Choose the project to work on. *BRD:* BRD-12 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner opens a project from the list, then the Workbench opens on it and it becomes the selected project.

<a id="d-req-fn-006"></a>
- **REQ-FN-006** — The choice survives a restart. *BRD:* BRD-13 · *Mockup:* mockups/start.html
  - *Acceptance:* When Chatur is closed and opened again on Start, then the same project is still selected.

<a id="d-req-ui-008"></a>
- **REQ-UI-008** — Nothing named yet says what to do. *BRD:* BRD-14 · *Mockup:* mockups/start.html
  - *Acceptance:* When no folder has been named on Start, then Projects shows an empty state with a button that opens the Project folders dialog.

<a id="d-req-ui-009"></a>
- **REQ-UI-009** — Remove a folder. *BRD:* BRD-15 · *Mockup:* mockups/start.html
  - *Acceptance:* When the owner removes a folder in the dialog, then its projects leave the list and the folder is gone after a restart.

## Page: Workbench (`/`)

<a id="d-req-fn-007"></a>
- **REQ-FN-007** — What the selected project is. *BRD:* BRD-16 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project is selected on Workbench, then the toolbar names it and its branch, and its files are listed down the left.

<a id="d-req-fn-008"></a>
- **REQ-FN-008** — Straight into the work. *BRD:* BRD-17 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project opens, then the conversation with a role is already there, with no further step to reach it.

<a id="d-req-ui-010"></a>
- **REQ-UI-010** — Change project without going back. *BRD:* BRD-18 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner picks another project in the top bar, then the Workbench and every other screen show the new project.

<a id="d-req-fn-009"></a>
- **REQ-FN-009** — Sign out. *BRD:* BRD-19 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner signs out on Workbench, then Chatur returns to Sign in and the stored token no longer opens the app.

<a id="d-req-ui-011"></a>
- **REQ-UI-011** — Only targets that fit the machine. *BRD:* BRD-20 · *Mockup:* mockups/main.html
  - *Acceptance:* When Run opens, then only a target this machine can build can be chosen, and one needing another machine is shown as unavailable.

<a id="d-req-ui-012"></a>
- **REQ-UI-012** — Build the project. *BRD:* BRD-21 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner builds on Workbench, then Run shows the result as succeeded or failed with the time it took.

<a id="d-req-ui-013"></a>
- **REQ-UI-013** — Run the project. *BRD:* BRD-22 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner runs the chosen target on Workbench, then the program starts and Run shows it as running.

<a id="d-req-ui-014"></a>
- **REQ-UI-014** — Watch the output live. *BRD:* BRD-23 · *Mockup:* mockups/main.html
  - *Acceptance:* When the program writes a line on Workbench, then it appears in the output panel while the program is still running.

<a id="d-req-ui-015"></a>
- **REQ-UI-015** — Stop everything the run started. *BRD:* BRD-24 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner stops the run on Workbench, then every program it started ends and Run shows it as stopped.

<a id="d-req-ui-016"></a>
- **REQ-UI-016** — A failed build shows the errors. *BRD:* BRD-25 · *Mockup:* mockups/main.html
  - *Acceptance:* When a build fails on Workbench, then the output strip lists each error with its file and line, and opening one opens that file.

<a id="d-req-fn-010"></a>
- **REQ-FN-010** — The last target is offered first. *BRD:* BRD-26 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner returns to Run for a project on Workbench, then the target he used last time is already chosen.

<a id="d-req-fn-024"></a>
- **REQ-FN-024** — Choose who to talk to. *BRD:* BRD-44 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project opens, then Chatur has already chosen the agent that suits its state, and says beside the box why.

<a id="d-req-fn-025"></a>
- **REQ-FN-025** — Ask and be answered. *BRD:* BRD-45 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner sends a message about the selected project on Workbench, then a reply from the chosen role appears in the conversation.

<a id="d-req-fn-026"></a>
- **REQ-FN-026** — Which model answered. *BRD:* BRD-46 · *Mockup:* mockups/main.html
  - *Acceptance:* When a reply arrives on Workbench, then it names the model that produced it.

<a id="d-req-fn-027"></a>
- **REQ-FN-027** — The reply arrives as it is written. *BRD:* BRD-47 · *Mockup:* mockups/main.html
  - *Acceptance:* When a reply is being produced on Workbench, then its text grows in the conversation before the reply is finished.

<a id="d-req-ui-021"></a>
- **REQ-UI-021** — What it cost. *BRD:* BRD-48 · *Mockup:* mockups/main.html
  - *Acceptance:* When a reply is finished on Workbench, then the Workbench shows the tokens it used and the running total for the session.

<a id="d-req-ui-022"></a>
- **REQ-UI-022** — Watch what the agent is doing. *BRD:* BRD-49 · *Mockup:* mockups/main.html
  - *Acceptance:* When the agent reads a file, edits one or runs a command on Workbench, then that step appears in the activity panel as it happens.

<a id="d-req-ui-023"></a>
- **REQ-UI-023** — Stop the work. *BRD:* BRD-50 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner presses stop on Workbench, then the agent stops, the activity panel says so, and no further step is taken.

<a id="d-req-ui-024"></a>
- **REQ-UI-024** — Ask me first, or go ahead. *BRD:* BRD-51 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner sets the session's mode on Workbench, then the mode is kept with the session and shown while it runs.

<a id="d-req-ui-031"></a>
- **REQ-UI-031** — What has been worked on. *BRD:* BRD-61 · *Mockup:* mockups/main.html
  - *Acceptance:* When Sessions opens, then each session shows its project, role, mode, when it started and its state.

<a id="d-req-fn-031"></a>
- **REQ-FN-031** — Pick up where it stopped. *BRD:* BRD-62 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner continues a session on Workbench, then the Workbench opens with that conversation, its role and its mode.

<a id="d-req-fn-032"></a>
- **REQ-FN-032** — A session keeps its record. *BRD:* BRD-63 · *Mockup:* mockups/main.html
  - *Acceptance:* When a session is opened again after a restart on Workbench, then every message, step and refusal is still there in order.

<a id="d-req-ui-032"></a>
- **REQ-UI-032** — See the change itself. *BRD:* BRD-64 · *Mockup:* mockups/main.html
  - *Acceptance:* When the agent proposes an edit on Workbench, then Changes shows the file with its old and new text side by side.

<a id="d-req-fn-033"></a>
- **REQ-FN-033** — Approve it. *BRD:* BRD-65 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner approves a change on Workbench, then the file on disk holds the new text and the row moves to approved.

<a id="d-req-fn-034"></a>
- **REQ-FN-034** — Reject it. *BRD:* BRD-66 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner rejects a change on Workbench, then the file on disk is untouched and the row moves to rejected.

<a id="d-req-fn-035"></a>
- **REQ-FN-035** — Ask me first means ask. *BRD:* BRD-67 · *Mockup:* mockups/main.html
  - *Acceptance:* When the session is in "ask me first", then no file is written until the owner has approved that change.

<a id="d-req-fn-036"></a>
- **REQ-FN-036** — Everything the agent asks for passes the guards. *BRD:* BRD-68 · *Mockup:* mockups/main.html
  - *Acceptance:* When the agent asks to use any tool on Workbench, then the guards decide first, and a refused request never reaches the tool.

<a id="d-req-ui-033"></a>
- **REQ-UI-033** — A model never runs source control. *BRD:* BRD-69 · *Mockup:* mockups/main.html
  - *Acceptance:* When a model asks to run a source-control command on Workbench, then the guard refuses it and the refusal is shown in the activity.

<a id="d-req-ui-039"></a>
- **REQ-UI-039** — Browse the project. *BRD:* BRD-80 · *Mockup:* mockups/main.html
  - *Acceptance:* When the files card opens, then it shows the project's solution and folders, and a folder can be opened and closed.

<a id="d-req-ui-040"></a>
- **REQ-UI-040** — Open a file. *BRD:* BRD-81 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens a file from the tree, then its text appears in a new tab named after the file.

<a id="d-req-fn-044"></a>
- **REQ-FN-044** — Edit and save. *BRD:* BRD-82 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner changes the text and saves on Workbench, then the file on disk holds the new text.

<a id="d-req-ui-041"></a>
- **REQ-UI-041** — An unsaved tab is marked. *BRD:* BRD-83 · *Mockup:* mockups/main.html
  - *Acceptance:* When a tab has unsaved changes on Workbench, then it is marked, and closing it asks before the change is lost.

<a id="d-req-fn-045"></a>
- **REQ-FN-045** — Open it in the owner's editor. *BRD:* BRD-84 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner chooses his own editor for a file on Workbench, then that program opens with the file.

<a id="d-req-fn-046"></a>
- **REQ-FN-046** — Open it with whatever the machine uses. *BRD:* BRD-85 · *Mockup:* mockups/main.html
  - *Acceptance:* When the owner opens a file with the system's default application, then the machine's own choice of program opens it.

## Page: Prerequisites (`/prerequisites`)

<a id="d-req-fn-011"></a>
- **REQ-FN-011** — Every tool, with the version found. *BRD:* BRD-27 · *Mockup:* mockups/prerequisites.html
  - *Acceptance:* When Doctor opens for a project, then each tool it needs is listed with the version found or "not found".

<a id="d-req-ui-017"></a>
- **REQ-UI-017** — A missing tool says how to fix it. *BRD:* BRD-28 · *Mockup:* mockups/prerequisites.html
  - *Acceptance:* When a tool is missing from the machine, then its row on Prerequisites shows the command that installs it, ready to copy.

<a id="d-req-ui-018"></a>
- **REQ-UI-018** — Which Chatur this is. *BRD:* BRD-29 · *Mockup:* mockups/prerequisites.html
  - *Acceptance:* When Doctor opens, then it shows Chatur's own version and the commit the build came from.

<a id="d-req-fn-012"></a>
- **REQ-FN-012** — Homebrew tools are found on a Mac. *BRD:* BRD-30 · *Mockup:* mockups/prerequisites.html
  - *Acceptance:* When Chatur is started from Finder on a Mac, then Doctor still finds tools installed with Homebrew.

<a id="d-req-fn-013"></a>
- **REQ-FN-013** — A newer build is offered. *BRD:* BRD-31 · *Mockup:* mockups/prerequisites.html
  - *Acceptance:* When a newer nightly build exists on Prerequisites, then Doctor says so and links to the download for this machine.

## Page: Settings (`/settings/{page}`)

<a id="d-req-fn-014"></a>
- **REQ-FN-014** — Connect a source with a pasted key. *BRD:* BRD-32 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When the owner adds a provider and pastes a key on Settings, then it is listed as connected and its models can be chosen in routing.

<a id="d-req-fn-015"></a>
- **REQ-FN-015** — Connect a local model. *BRD:* BRD-33 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When the owner adds a local model with its web address on Settings, then it is listed as connected with no key.

<a id="d-req-fn-016"></a>
- **REQ-FN-016** — Sign in to a ChatGPT subscription. *BRD:* BRD-34 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When the owner chooses subscription sign-in, then the browser opens, Chatur shows the sign-in code, and after he enters it the provider is listed as connected.

<a id="d-req-fn-017"></a>
- **REQ-FN-017** — Secrets go to the machine's own store. *BRD:* BRD-35 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When a key or token is saved on Settings, then it is in the operating system's store and Chatur's database holds only the name it is stored under.

<a id="d-req-ui-019"></a>
- **REQ-UI-019** — Check that a source answers. *BRD:* BRD-36 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When the owner tests a provider on Settings, then its row shows whether it answered, and the message when it did not.

<a id="d-req-ui-020"></a>
- **REQ-UI-020** — Remove a source. *BRD:* BRD-37 · *Mockup:* mockups/settings-providers.html
  - *Acceptance:* When the owner removes a provider on Settings, then it leaves the list and its secret is deleted from the store.

<a id="d-req-fn-018"></a>
- **REQ-FN-018** — A tier for each role. *BRD:* BRD-38 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the owner sets a role's tier on Settings, then work started by that role uses a model from that tier.

<a id="d-req-fn-019"></a>
- **REQ-FN-019** — A tier for each kind of work. *BRD:* BRD-39 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the owner sets a kind of work to a tier on Settings, then that setting wins over the role's tier for that work.

<a id="d-req-fn-020"></a>
- **REQ-FN-020** — Order the fallback chain. *BRD:* BRD-40 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the owner reorders the models in a tier, then the new order is the order Chatur tries them in.

<a id="d-req-fn-021"></a>
- **REQ-FN-021** — A limited model steps aside. *BRD:* BRD-41 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the chosen model is limited or unavailable on Settings, then Chatur uses the next in the chain and says which model answered.

<a id="d-req-fn-022"></a>
- **REQ-FN-022** — Repeated failed fixes climb a tier. *BRD:* BRD-42 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the same fix has failed the set number of times on Settings, then Chatur moves that work to a stronger tier and records the move.

<a id="d-req-fn-023"></a>
- **REQ-FN-023** — Routing is remembered. *BRD:* BRD-43 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When Chatur is closed and opened again on Settings, then every tier, chain and role setting is as it was left.

<a id="d-req-ui-025"></a>
- **REQ-UI-025** — Roles come from the database. *BRD:* BRD-52 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When a role's wording is changed in the database, then the roles tab shows the new wording without any file being edited.

<a id="d-req-ui-026"></a>
- **REQ-UI-026** — Rights at a glance. *BRD:* BRD-53 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the roles tab opens, then each role shows what it may do, its commands and the tier it uses.

<a id="d-req-fn-028"></a>
- **REQ-FN-028** — Take the roles to the other machine. *BRD:* BRD-54 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner exports on Settings, then Chatur writes one file holding every role, right, command and rule with its version.

<a id="d-req-fn-029"></a>
- **REQ-FN-029** — Bring them in. *BRD:* BRD-55 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner imports that file on the other machine, then the roles there are the same, with their rights, commands and rules.

<a id="d-req-ui-027"></a>
- **REQ-UI-027** — Change what a role is. *BRD:* BRD-56 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner edits a role's wording, commands or rules and saves on Settings, then the list and the next session use the new text.

<a id="d-req-ui-028"></a>
- **REQ-UI-028** — Set the role's model tier. *BRD:* BRD-57 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner sets a role's tier on the roles tab, then the routing tab shows the same tier for that role.

<a id="d-req-ui-029"></a>
- **REQ-UI-029** — Every save keeps the one before. *BRD:* BRD-58 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the owner saves a change on Settings, then the history list gains a version with the date and what changed.

<a id="d-req-ui-030"></a>
- **REQ-UI-030** — A role may only do what its rights allow. *BRD:* BRD-59 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When the analyst, which cannot change code, asks to edit a file on Settings, then the request is refused and the refusal is shown.

<a id="d-req-fn-030"></a>
- **REQ-FN-030** — Only the verifier marks work verified. *BRD:* BRD-60 · *Mockup:* mockups/settings-agents.html
  - *Acceptance:* When any other role asks to mark a requirement verified on Settings, then the request is refused and nothing is written.

<a id="d-req-ui-034"></a>
- **REQ-UI-034** — A correction is written down. *BRD:* BRD-70 · *Mockup:* mockups/settings-corrections.html
  - *Acceptance:* When Chatur changes an agent, a rule or a step's wording in its database, then the corrections page gains a row with what changed, why and when.

<a id="d-req-fn-037"></a>
- **REQ-FN-037** — Keep it. *BRD:* BRD-71 · *Mockup:* mockups/settings-corrections.html
  - *Acceptance:* When the owner keeps a correction on Settings, then it stays in force and the row moves to kept.

<a id="d-req-fn-038"></a>
- **REQ-FN-038** — Undo it. *BRD:* BRD-72 · *Mockup:* mockups/settings-corrections.html
  - *Acceptance:* When the owner undoes a correction on Settings, then the wording before it returns and the row moves to undone.

<a id="d-req-fn-039"></a>
- **REQ-FN-039** — Kept corrections feed the next build. *BRD:* BRD-73 · *Mockup:* mockups/settings-corrections.html
  - *Acceptance:* When the owner exports the seed data on Settings, then every kept correction is in it and every undone one is not.

<a id="d-req-fn-040"></a>
- **REQ-FN-040** — The five files. *BRD:* BRD-74 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When an agent session runs in a project, then runs, gates, misses, sessions and commits files exist in its `docs/metrics` folder.

<a id="d-req-fn-041"></a>
- **REQ-FN-041** — Chatur signs its work. *BRD:* BRD-75 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When Chatur writes a record on Settings, then the field naming the tool reads `chatur`.

<a id="d-req-fn-042"></a>
- **REQ-FN-042** — What was not measured stays empty. *BRD:* BRD-76 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When a number was not measured on Settings, then its field is written empty and never filled with a guess.

<a id="d-req-fn-043"></a>
- **REQ-FN-043** — An unknown field is refused. *BRD:* BRD-77 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When a record carries a field or value the schema does not know on Settings, then the write is refused and the reason is logged.

<a id="d-req-ui-035"></a>
- **REQ-UI-035** — A failed write never stops the work. *BRD:* BRD-78 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When a measurement file cannot be written on Settings, then the session carries on and Measurements shows the failure.

<a id="d-req-ui-036"></a>
- **REQ-UI-036** — What has been written. *BRD:* BRD-79 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When Measurements opens, then each stream shows how many records it holds and the newest one's time.

<a id="d-req-ui-037"></a>
- **REQ-UI-037** — Choose how Chatur looks. *BRD:* BRD-156 · *Mockup:* mockups/settings-appearance.html
  - *Acceptance:* When the owner picks a theme, or light or dark, on the appearance page, then every window changes at once and the choice survives a restart.

<a id="d-req-ui-038"></a>
- **REQ-UI-038** — A theme is data, not code. *BRD:* BRD-157 · *Mockup:* mockups/settings-appearance.html
  - *Acceptance:* When the owner adds a theme file on Settings, then it joins the list and can be chosen, with no new build of Chatur.

## Page: Repository (`/repository`)

<a id="d-req-ui-042"></a>
- **REQ-UI-042** — What changed. *BRD:* BRD-86 · *Mockup:* mockups/repository.html
  - *Acceptance:* When Source control opens, then every changed file is listed with its state, and choosing one shows its old and new text.

<a id="d-req-ui-043"></a>
- **REQ-UI-043** — Check in. *BRD:* BRD-87 · *Mockup:* mockups/repository.html
  - *Acceptance:* When the owner checks in the chosen files with a message, then those files are committed and the list empties of them.

<a id="d-req-ui-044"></a>
- **REQ-UI-044** — Push and pull. *BRD:* BRD-88 · *Mockup:* mockups/repository.html
  - *Acceptance:* When the owner pushes or pulls on Repository, then Source control shows the result and how far ahead or behind the branch now is.

<a id="d-req-fn-047"></a>
- **REQ-FN-047** — Switch branch. *BRD:* BRD-89 · *Mockup:* mockups/repository.html
  - *Acceptance:* When the owner switches to another branch on Repository, then the header names it and the changed files are that branch's.

<a id="d-req-fn-048"></a>
- **REQ-FN-048** — An automatic check-in goes to its own branch. *BRD:* BRD-90 · *Mockup:* mockups/repository.html
  - *Acceptance:* When a process checks in by itself, then the commit is on the run's own branch and its message carries the requirement ids.

## Non-functional

<a id="d-req-nfr-001"></a>
- **REQ-NFR-001** — Every secret — model key, subscription token, App Manager token — is kept in the operating system's store for secrets, never in the database, a file or a log. *BRD:* BRD-91
  - *Acceptance:* When the owner connects a provider and then reads the database and the log, then the secret is in neither.

<a id="d-req-nfr-002"></a>
- **REQ-NFR-002** — Serilog file logging is wired at startup before anything else can fail. *BRD:* BRD-92
  - *Acceptance:* When startup is broken on purpose, then the log file exists on disk and holds the failure.

<a id="d-req-nfr-003"></a>
- **REQ-NFR-003** — A database change that has shipped in a nightly build is never edited; a new one is added, and an update keeps the owner's data. *BRD:* BRD-93
  - *Acceptance:* When the owner makes data on the previous nightly and installs the new one, then every row is still there.

<a id="d-req-nfr-004"></a>
- **REQ-NFR-004** — Every requirement works on Mac Catalyst and on Windows, verified on the Mac first. *BRD:* BRD-94
  - *Acceptance:* When a requirement is verified, then its verdict in the checklist names both the Mac and Windows.

<a id="d-req-nfr-005"></a>
- **REQ-NFR-005** — Every guard class has unit tests covering what it allows and what it refuses. *BRD:* BRD-95
  - *Acceptance:* When the tests run, then the project holds a test per guard for what it allows and what it refuses.

<a id="d-req-nfr-006"></a>
- **REQ-NFR-006** — A GitHub Actions workflow has a macOS job that builds the Mac Catalyst `.app` and zips it, and a Windows job that builds the unpackaged, self-contained Windows app (`WindowsPackageType` None) and zips it. Every push to main publishes a `nightly` pre-release with both downloads; a `v*` tag publishes a named release. Version and commit are stamped into the build, so Prerequisites shows them (BRD-29) and the newer-build check finds the release (BRD-31). No code signing (added 2026-09-30). *BRD:* BRD-158
  - *Acceptance:* When a commit is pushed to main, then the `nightly` pre-release holds a Mac zip and a Windows zip built from that commit, and Prerequisites in that build shows the same version and commit.

