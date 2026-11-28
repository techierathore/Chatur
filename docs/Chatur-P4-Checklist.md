# Chatur — Checklist (phase 4)

| | |
|---|---|
| App | Chatur |
| Size | Medium |
| Phase | 4 of 4 |

## Goal

Phase 4, more users: feedback, issues, licences and feature control through App Manager, agents and rules synced between machines, one view across every project, a working copy per run, Chatur's actions offered to other tools, and signed downloads. This checklist is the whole work list of the phase.

## Requirements Status

| ID | Requirement | Status | % | Remarks | Details |
|----|-------------|--------|---|---------|---------|
| REQ-UI-062 | Say what is wrong from inside Chatur | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-062 an expired access token is refreshed and the call`; Feedback and issues renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-062) |
| REQ-FN-073 | Raise it against the library it belongs to | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-073 the library name leads the issue title sent to Ap`; Feedback and issues renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-073) |
| REQ-UI-063 | See what came back | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-063 the latest public comment is the answer and resol`; Feedback and issues renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-063) |
| REQ-FN-074 | Nothing is lost when the network is down | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-074 a network failure and a server error are both unr`; Feedback and issues renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-074) |
| REQ-UI-064 | The licence in force | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-064 Licence shows the licence name, state and days re`; Licence renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-064) |
| REQ-UI-065 | Too many machines is reported | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-065 Licence shows the over-cap warning the server sen`; Licence renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-065) |
| REQ-UI-066 | Forget a machine | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-066 Forgetting a device removes it from the list and `; Licence renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-066) |
| REQ-FN-075 | A feature switched off is not offered | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-075 A feature switched off for the licence is not off`; Licence renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-075) |
| REQ-FN-076 | Everything at once | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-076 every project Chatur knows is listed with its pha`; All projects renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-076) |
| REQ-UI-067 | Where each one stands | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-067 a project row shows how many requirements are ope`; All projects renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-067) |
| REQ-FN-077 | Go straight in | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-077 opening a project from the view selects it and op`; All projects renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-077) |
| REQ-FN-078 | The roles follow the developer | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-078 roles and rules changed on one machine are on the`; All projects renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-078) |
| REQ-FN-079 | Work in a separate copy | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-079 a working copy added for a branch is listed with `; Worktrees renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-079) |
| REQ-FN-080 | Give a run its own copy | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-080 a run started in a working copy changes files onl`; Worktrees renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-080) |
| REQ-UI-068 | Clear it away | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-068 removing a working copy takes it off the list and`; Worktrees renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-068) |
| REQ-FN-081 | The actions are published | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-081 the owner turns an action off and an unbuilt one `; Integrations renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-081) |
| REQ-FN-082 | Something outside can start one | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-082 no token, a wrong token or an action that is off `; Integrations renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-082) |
| REQ-FN-083 | The verifier drives real windows | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-083 a window check is listed with its screenshots and`; Integrations renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-083) |
| REQ-UI-069 | Signed downloads | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-069 the newest release lists signed Mac and Windows d`; Integrations renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-069) |
| REQ-UI-107 | Run shows the build and the program's output live, in an Output panel that opens by itself | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-UI-107 the https port warning is a warning, not an error`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-107) |
| REQ-FN-306 | A web project opens in the browser once it is up | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-306 the listening line and the profile give the brows`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-306) |
| REQ-UI-108 | A build that fails says so with its errors; a program that ends shows its exit code | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-108 a run whose build fails says so with its errors a`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-108) |
| REQ-FN-307 | Picking a project in the switcher switches to it | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-307 picking the other project of the same name in the`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-307) |
| REQ-UI-109 | The switcher lists every project, scrolls, and filters | Verified | 100% | 2026-11-23 verify: not measured — 1 test(s) named REQ-UI-109 passed and 1 skipped (`REQ-UI-109 the project switcher lists every projec`: skipped); a skipped test is part of the row's acceptance, so it is not measured until that test runs; screen Workbench renders and looks right; Verified on 2026-11-22 stands, not re-measured; mockup-parity: 16 sample box(es) not measured, the app drew… | [view](#d-req-ui-109) |
| REQ-UI-110 | The bar clearly names the project open now | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-110 the bar reads "Project:" and the name of the proj`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-110) |
| REQ-FN-308 | An agent greets the person with what it is for and its commands, taken from its real command list | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-308 asked whether it has a brainstorm command, the an`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-308) |
| REQ-UI-111 | Every reply shows which model answered, and on Automatic why | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-111 a reply names its model, and on Automatic says wh`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-111) |
| REQ-UI-113 | Routing says in plain words how Automatic chooses | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-113 the Routing page says in plain words how Automati`; Settings / routing renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 4 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-113) |
| REQ-UI-112 | New project is one dialog in every state and always comes from this machine's templates | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 REQ-FN-310 REQ-FN-309 REQ-UI-112 REQ-UI-127 with`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-112) |
| REQ-NFR-011 | A closed window leaves no errors behind | Verified | 100% | 2026-11-23 verify: not observable — NFR row with no perf-budget and no unit test carrying its id; Verified on 2026-11-22 stands, not re-measured | [view](#d-req-nfr-011) |
| REQ-NFR-012 | Chatur installs for the user on Windows | Verified | 100% | [REQ-NFR-012] Windows Verified 2026-11-22. 2026-11-27: the Mac install beside it, scripts/install-mac.sh — Release, signed with the free Mac Catalyst profile, into /Applications/Chatur.app; it starts to Sign in (tests/.artifacts/mac/installed-start.png); the Mac build runs on the interpreter (TR-030) | [view](#d-req-nfr-012) |
| REQ-FN-309 | After New project, Run has a target | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-FN-309 a project just added becomes the project that run`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-fn-309) |
| REQ-FN-310 | New project with no solution open makes the solution and its first project from a template, and opens it | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-FN-310 a project folder that already holds files is refu`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-fn-310) |
| REQ-FN-311 | Every agent carries every command its TechieFlow agent has, done in Chatur | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-311 an unknown command is answered from the list`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-fn-311) |
| REQ-UI-114 | A reply is drawn as formatted text | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-114 a reply's markdown is drawn as formatted text and`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-ui-114) |
| REQ-FN-312 | In an empty folder the Analyst brainstorms with the person and writes the brief | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-312 *create-project-brief guides the agent until docs`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [d](#d-req-fn-312) |
| REQ-NFR-013 | The owner's test and its tests start from the two states a person really meets | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-013 the New project tests and the installed app's te` | [d](#d-req-nfr-013) |
| REQ-UI-115 | Measurements, in the main menu, for the open project, read from its own docs/metrics files and nothing else | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-UI-115 the report is read from the project's own files a`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-115) |
| REQ-UI-116 | Pick the period | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-116 each period counts only the records dated inside `; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-116) |
| REQ-UI-117 | Runs, tokens, cost and time, split by model, by agent and by command | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-117 splits by model, agent and command, with the agen`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-117) |
| REQ-UI-118 | Mistakes: how many, who found each, the rows, and first-time passes | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-118 misses fold amends and voids, take status from th`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-118) |
| REQ-UI-119 | Estimate beside actual for each phase, with the estimate table that exists | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-119 the estimate region shows the shared estimate tab`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-119) |
| REQ-UI-120 | One line per run, which opens | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-120 run lines carry rows touched, the raw record and `; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-120) |
| REQ-FN-313 | The same totals across every project | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-313 across projects gives one line per project and ad`; Measurements across projects renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-313) |
| REQ-FN-314 | Save the open project's measurements as one zip TfLens imports | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-314 export writes a zip of docs/metrics/<file> entrie`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-314) |
| REQ-UI-121 | Settings ▸ Measurements stays the state of the files and links to the new screen | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-121 Settings Measurements keeps the stream tiles and `; Settings / measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-121) |
| REQ-FN-315 | *day1-brownfield runs to its end from the chat | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-315 *day1-brownfield in a project with code writes th`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-315) |
| REQ-FN-316 | *doc-out writes the document drafted in the conversation | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-316 *doc-out writes the whole document drafted in the`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-316) |
| REQ-FN-317 | The Analyst's *research-prompt writes the research prompt | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-317 the Analyst *research-prompt writes docs/{App}-Re`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-317) |
| REQ-FN-318 | *devguide writes the developer's guide | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-318 *devguide writes docs/{App}-DevGuide.md`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-318) |
| REQ-FN-319 | *productguide writes the end user's manual | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-319 *productguide writes docs/{App}-ProductGuide.md`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-319) |
| REQ-UI-122 | *develop-end-to-end started from the chat starts the end-to-end run | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-122 a brief the run engine refuses is answered in the`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-122) |
| REQ-FN-320 | *develop-status says where the end-to-end run is | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-320 *develop-status says whether the run is going, it`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-320) |
| REQ-FN-321 | *estimate-phase gives the phase's estimate | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-321 *estimate-phase in a project with a checklist giv`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-321) |
| REQ-FN-322 | The Architect's *research writes the research prompt | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-FN-322 the Architect *research writes docs/{App}-Researc`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-322) |
| REQ-UI-123 | A command that starts a process run says so and links to the run | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-UI-123 a command that starts a process run says so and i`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 16 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-123) |
| REQ-NFR-014 | Every number on the measurements screens is computed by code and equals the total added up again from the files | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-NFR-014 every figure equals an independent re-addition o`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-nfr-014) |
| REQ-NFR-015 | The UsageGuide has "The agents and their commands", one table per agent, from the one command list in the code | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-NFR-015 the generated guide and README text name every c` | [view](#d-req-nfr-015) |
| REQ-NFR-016 | The README names the four agents and their commands in short and links to the guide's section | Verified | 100% | 2026-11-23 verify: PASS — test `REQ-NFR-016 the README lists each agent's commands and links` | [view](#d-req-nfr-016) |
| REQ-NFR-017 | The owner's test has a part for the measurements screens and one for typing a command of each agent | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-017 the owner's test keeps its eight parts and adds ` | [view](#d-req-nfr-017) |
| REQ-UI-124 | A folder with projects and no solution gets a solution that takes them in | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 REQ-UI-124 a folder with projects and no solutio`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-124) |
| REQ-FN-323 | A folder that has a solution gets the new project added to that solution | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 REQ-FN-323 a folder with a solution gets the pro`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-323) |
| REQ-UI-125 | After Create, FILES keeps the folder that was open as its root | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 REQ-FN-310 REQ-UI-126 REQ-UI-125 REQ-FN-309 with`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-125) |
| REQ-UI-126 | New project shows where things will be made before it makes them | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-UI-126 the plan names the solution file and the project `; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-126) |
| REQ-NFR-018 | New project's tests compare the exact paths on disk for each starting point | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 the New project tests compare the exact files on` | [view](#d-req-nfr-018) |
| REQ-NFR-019 | The owner's test makes its folders under this repository's bin folder and starts with the empty-folder case | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-019 the owner's test starts with the empty-folder ca` | [view](#d-req-nfr-019) |
| REQ-UI-127 | Search the project types in New project | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-018 REQ-FN-310 REQ-FN-309 REQ-UI-112 REQ-UI-127 with`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-127) |
| REQ-FN-324 | Several projects start together, a saved choice per solution | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-FN-324 several startup projects are chosen once, kept fo`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-324) |
| REQ-FN-325 | A folder with projects and no solution is said so and asked about before it opens | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-FN-325 opening a folder that has a project and no soluti`; Start renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 6 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-325) |
| REQ-UI-128 | The chat moves to the newest message after a long reply or a command such as *yolo | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-UI-128 after a long reply the conversation is at its new`; Workbench renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 15 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-128) |
| REQ-NFR-020 | The owner's test names no path in the measurements part, and a real model call proves what Measurements shows | Verified | 100% | 2026-11-26 verify: PASS — test `REQ-NFR-020 the owner's test picks the project in the app fo` | [view](#d-req-nfr-020) |
| REQ-UI-129 | What is left on each key, on both Measurements screens | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-UI-129 a local model has no key to count`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-129) |
| REQ-UI-130 | Ask the provider, where it has an address that tells its balance or its limit | Implemented | 75% | 2026-11-27 verify: not measured — 3 unit test(s) named REQ-UI-130 passed but do not count while its on-app test was skipped: skipped: test-settings.json has no balanceProvider (a real OpenRouter or DeepSeek key), so no provider can be asked; screen Measurements renders and looks right; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts… | [view](#d-req-ui-130) |
| REQ-UI-131 | A limit typed for a key, counted against what Chatur used | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-UI-131 a typed limit is counted against Chatur's own pri`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-ui-131) |
| REQ-FN-326 | No number reads "not known", never a guess | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-326 an OpenRouter key with no limit leaves what is le`; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-fn-326) |
| REQ-FN-327 | Pick two to four connected models and give them one task on the open project | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-327 Run starts each picked model on the task`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-327) |
| REQ-FN-328 | A read-only question needs no copy and changes nothing | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-328 a question reads the open project and changes not`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-328) |
| REQ-FN-329 | A task that changes files gives each model its own copy under the project's bin folder, removed afterwards | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-329 docs/metrics written during the run is not a chan`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-329) |
| REQ-UI-132 | Time, tokens, cost and the answer or changed files, side by side | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-UI-132 each model has its time, tokens and cost or no pr`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-132) |
| REQ-UI-133 | Mark each model's result right or wrong | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-UI-133 a mark is kept and shows again when the compariso`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-ui-133) |
| REQ-FN-330 | Comparisons and marks are kept in docs/metrics in TfLens's shape and open again | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-FN-330 runs and marks are kept in docs/metrics in TfLens`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-fn-330) |
| REQ-NFR-021 | Compare models is proven with a real read-only question on two models | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-NFR-021 REQ-FN-328 REQ-UI-132 REQ-FN-330 two real models`; Compare models renders and looks right @1280/390 in light and dark mode; matches its mockup | [view](#d-req-nfr-021) |
| REQ-NFR-022 | The owner's test has parts for the keys and for Compare models, and runs in one order | Verified | 100% | 2026-11-27 verify: PASS — test `REQ-NFR-022 the owner's test is one order and has parts for `; Measurements renders and looks right @1280/390 in light and dark mode; matches its mockup; mockup-parity: 2 sample box(es) not measured, the app drew no data there (a tests/verify/seed/ script puts it in the drawn state) | [view](#d-req-nfr-022) |

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

## The owner's evening test, 21 Nov (Workbench, Settings)

<a id="d-req-ui-107"></a>
- **REQ-UI-107** — Run shows the build and the program's output live, in an Output panel that opens by itself. *BRD:* BRD-235 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person presses Run on Workbench, then Output opens by itself and shows the build and then the program's own lines as they come, error lines marked.

<a id="d-req-fn-306"></a>
- **REQ-FN-306** — A web project opens in the browser once it is up. *BRD:* BRD-236 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person runs a web project on Workbench, then once it is listening the browser opens at its launch-settings or listening address.

<a id="d-req-ui-108"></a>
- **REQ-UI-108** — A build that fails says so with its errors; a program that ends shows its exit code. *BRD:* BRD-237 · *Mockup:* mockups/main.html
  - *Acceptance:* When a run's build fails on Workbench, then Output says the build failed and lists each error with file and line; when the program ends, Output shows its exit code.

<a id="d-req-fn-307"></a>
- **REQ-FN-307** — Picking a project in the switcher switches to it. *BRD:* BRD-238 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person picks another project in the switcher on Workbench, then its files, its chat and its run targets show, and the switcher names it.

<a id="d-req-ui-109"></a>
- **REQ-UI-109** — The switcher lists every project, scrolls, and filters. *BRD:* BRD-239 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person opens the switcher on Workbench, then every project of theirs is listed, the list scrolls, a filter narrows it, and no "All projects" shows.

<a id="d-req-ui-110"></a>
- **REQ-UI-110** — The bar clearly names the project open now. *BRD:* BRD-240 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project is open on Workbench, then the switcher reads "Project:" and its name, and its list ticks that same project as "open now".

<a id="d-req-fn-308"></a>
- **REQ-FN-308** — An agent greets the person with what it is for and its commands, taken from its real command list. *BRD:* BRD-241 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person chooses an agent on Workbench or asks what commands it has, then the answer lists every command of its real list.

<a id="d-req-ui-111"></a>
- **REQ-UI-111** — Every reply shows which model answered, and on Automatic why. *BRD:* BRD-242 · *Mockup:* mockups/main.html
  - *Acceptance:* When an agent replies on Workbench, then the reply names the model that answered; with routing on Automatic, a few words say why that model was picked.

<a id="d-req-ui-113"></a>
- **REQ-UI-113** — Routing says in plain words how Automatic chooses. *BRD:* BRD-243 · *Mockup:* mockups/settings-routing.html
  - *Acceptance:* When the person opens Routing on Settings, then a plain paragraph says how Automatic chooses between the listed models, with one example.

<a id="d-req-ui-112"></a>
- **REQ-UI-112** — New project is one dialog in every state and always comes from this machine's templates. *BRD:* BRD-244 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person chooses New project in any state on Workbench, then one dialog lists this machine's templates, shows where things will be made, and adds to an open solution.

<a id="d-req-nfr-011"></a>
- **REQ-NFR-011** — A closed window leaves no errors behind. *BRD:* BRD-245
  - *Acceptance:* When the person closes the Settings window and switches project on Workbench, then the day's log holds no unobserved task exception.

<a id="d-req-nfr-012"></a>
- **REQ-NFR-012** — Chatur installs for the user on Windows. *BRD:* BRD-246
  - *Acceptance:* When the owner runs the install script on Windows, then Chatur opens from its Start menu entry and keeps the owner's data.


## The owner's test of 22 Nov (Start, Workbench)

<a id="d-req-fn-309"></a>
- **REQ-FN-309** — After New project, Run has a target. *BRD:* BRD-248 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person adds a project that can run with New project on Workbench, then it is the project that runs; a library added later leaves that choice.

<a id="d-req-fn-310"></a>
- **REQ-FN-310** — New project with no solution open makes the solution and its first project from a template, and opens it. *BRD:* BRD-247 · *Mockup:* mockups/start-empty.html
  - *Acceptance:* When the person creates a project with no solution on Start, then the solution gets a folder of its own in the chosen location and each project one inside that.

<a id="d-req-fn-311"></a>
- **REQ-FN-311** — Every agent carries every command its TechieFlow agent has, done in Chatur. *BRD:* BRD-249 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types any TechieFlow command of an agent to that agent on Workbench, then Chatur runs it itself and *help lists it.

<a id="d-req-ui-114"></a>
- **REQ-UI-114** — A reply is drawn as formatted text. *BRD:* BRD-250 · *Mockup:* mockups/main.html
  - *Acceptance:* When an agent replies with bold, a list, code or a table on Workbench, then the reply shows them formatted and no raw ** or | marks are left.

<a id="d-req-fn-312"></a>
- **REQ-FN-312** — In an empty folder the Analyst brainstorms with the person and writes the brief. *BRD:* BRD-251 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person asks the Analyst on Workbench, in an empty folder, to brainstorm and then write the brief, then the brief's file is written under docs/.

<a id="d-req-nfr-013"></a>
- **REQ-NFR-013** — The owner's test and its tests start from the two states a person really meets. *BRD:* BRD-252
  - *Acceptance:* When the owner's test and its tests run, then they start from nothing open after install and from an empty folder, never a hand-made solution.

## Page: Measurements (`/measurements`)

<a id="d-req-ui-115"></a>
- **REQ-UI-115** — Measurements, in the main menu, for the open project, read from its own docs/metrics files and nothing else. *BRD:* BRD-253 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person picks a project and chooses Project ▸ Measurements on Measurements, then its figures show, chat sessions included, read only from its docs/metrics files.

<a id="d-req-ui-116"></a>
- **REQ-UI-116** — Pick the period. *BRD:* BRD-254 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person picks Today, 7 days, 30 days, All or two dates on Measurements, then every figure on the screen counts only the records inside that period.

<a id="d-req-ui-117"></a>
- **REQ-UI-117** — Runs, tokens, cost and time, split by model, by agent and by command. *BRD:* BRD-255 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person picks a period on Measurements, then runs, tokens, the cost or "no price", and time show, each split by model, agent and command.

<a id="d-req-ui-118"></a>
- **REQ-UI-118** — Mistakes: how many, who found each, the rows, and first-time passes. *BRD:* BRD-256 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person picks a period on Measurements, then the misses show with who found each and their rows, and how many rows passed the first time.

<a id="d-req-ui-119"></a>
- **REQ-UI-119** — Estimate beside actual for each phase, with the estimate table that exists. *BRD:* BRD-257 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When Measurements opens on a project with an estimate, then the phase's estimate stands beside its actual in the same table Process run and Run record show.

<a id="d-req-ui-120"></a>
- **REQ-UI-120** — One line per run, which opens. *BRD:* BRD-258 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person opens a run's line on Measurements, then its command, model, tokens, time, result and the rows it touched are shown.

<a id="d-req-fn-314"></a>
- **REQ-FN-314** — Save the open project's measurements as one zip TfLens imports. *BRD:* BRD-260 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person presses Save as zip on Measurements, then one zip of the project's docs/metrics files is saved, in the shape TfLens imports.

<a id="d-req-ui-129"></a>
- **REQ-UI-129** — What is left on each key, on both Measurements screens. *BRD:* BRD-287 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person opens Measurements or Measurements across projects, then each connected provider has one line saying what is left on its key and how that is known.

<a id="d-req-ui-130"></a>
- **REQ-UI-130** — Ask the provider, where it has an address that tells its balance or its limit. *BRD:* BRD-288 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When a connected provider has an address for its balance or limit on Measurements, then Chatur asks it and the line shows the amount left and when it was asked.

<a id="d-req-ui-131"></a>
- **REQ-UI-131** — A limit typed for a key, counted against what Chatur used. *BRD:* BRD-289 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When the person types a limit and start date for a key on Measurements, then its line shows the limit, Chatur's own cost since that date, and what is left.

<a id="d-req-fn-326"></a>
- **REQ-FN-326** — No number reads "not known", never a guess. *BRD:* BRD-290 · *Mockup:* mockups/measurements.html
  - *Acceptance:* When a key has no address and no limit typed, or its address does not answer, on Measurements, then its line reads "not known" and says why.

## Page: Measurements across projects (`/measurements/all`)

<a id="d-req-fn-313"></a>
- **REQ-FN-313** — The same totals across every project. *BRD:* BRD-259 · *Mockup:* mockups/measurements-all.html
  - *Acceptance:* When the person opens Measurements across projects, then one line per project gives runs, tokens, cost, time and mistakes for the period, read from each project's docs/metrics files.

## Page: Settings

<a id="d-req-ui-121"></a>
- **REQ-UI-121** — Settings ▸ Measurements stays the state of the files and links to the new screen. *BRD:* BRD-262 · *Mockup:* mockups/settings-measurements.html
  - *Acceptance:* When the person opens Measurements on Settings, then it still shows the state of the six files and a link opens the Measurements screen.

## Page: Workbench

<a id="d-req-fn-315"></a>
- **REQ-FN-315** — *day1-brownfield runs to its end from the chat. *BRD:* BRD-265 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*day1-brownfield {App}` to the Analyst on Workbench in a project with code, then its BRD and Architecture are written under docs/.

<a id="d-req-fn-316"></a>
- **REQ-FN-316** — *doc-out writes the document drafted in the conversation. *BRD:* BRD-266 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person drafts a document with the Analyst and types `*doc-out` on Workbench, then the whole document is written to its file.

<a id="d-req-fn-317"></a>
- **REQ-FN-317** — The Analyst's *research-prompt writes the research prompt. *BRD:* BRD-267 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*research-prompt {topic}` to the Analyst on Workbench, then docs/{App}-Research-Prompt.md is written.

<a id="d-req-fn-318"></a>
- **REQ-FN-318** — *devguide writes the developer's guide. *BRD:* BRD-268 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*devguide {App}` to the Flow master on Workbench, then docs/{App}-DevGuide.md is written.

<a id="d-req-fn-319"></a>
- **REQ-FN-319** — *productguide writes the end user's manual. *BRD:* BRD-269 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*productguide {App}` to the Flow master on Workbench, then docs/{App}-ProductGuide.md is written.

<a id="d-req-ui-122"></a>
- **REQ-UI-122** — *develop-end-to-end started from the chat starts the end-to-end run. *BRD:* BRD-270 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*develop-end-to-end {App} {brief}` to the Flow master on Workbench, then an end-to-end run starts and the reply links to its page.

<a id="d-req-fn-320"></a>
- **REQ-FN-320** — *develop-status says where the end-to-end run is. *BRD:* BRD-271 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*develop-status {App}` to the Flow master on Workbench, then the reply says whether a run is going, its step, its open rows and its tokens.

<a id="d-req-fn-321"></a>
- **REQ-FN-321** — *estimate-phase gives the phase's estimate. *BRD:* BRD-272 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*estimate-phase {App}` to the Flow master on Workbench, then the reply gives the phase's tokens and time, low, likely and high, or why none.

<a id="d-req-fn-322"></a>
- **REQ-FN-322** — The Architect's *research writes the research prompt. *BRD:* BRD-273 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types `*research {topic}` to the Architect on Workbench, then docs/{App}-Research-Prompt.md is written.

<a id="d-req-ui-123"></a>
- **REQ-UI-123** — A command that starts a process run says so and links to the run. *BRD:* BRD-274 · *Mockup:* mockups/main.html
  - *Acceptance:* When a command typed on Workbench starts a process run, then the reply says so and its link opens that run's page.

<a id="d-req-ui-124"></a>
- **REQ-UI-124** — A folder with projects and no solution gets a solution that takes them in. *BRD:* BRD-276 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person creates a project in the open folder that holds projects and no solution, then the dialog says so and the new solution holds them all.

<a id="d-req-fn-323"></a>
- **REQ-FN-323** — A folder that has a solution gets the new project added to that solution. *BRD:* BRD-277 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person creates a project on Workbench with a folder open that holds a solution, then the project is added to that solution and no second solution is made.

<a id="d-req-ui-125"></a>
- **REQ-UI-125** — FILES shows the solution as its root, with its projects below it. *BRD:* BRD-278 · *Mockup:* mockups/main.html
  - *Acceptance:* When a project with a solution is open on Workbench, then FILES shows the solution as its one root with the projects below it and no solution file beside them.

<a id="d-req-ui-126"></a>
- **REQ-UI-126** — New project shows where things will be made before it makes them. *BRD:* BRD-279 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types the names in New project, then the dialog shows the solution's folder, the solution file and the project folder as full paths.

<a id="d-req-ui-127"></a>
- **REQ-UI-127** — Search the project types in New project. *BRD:* BRD-282 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person types in the search box above the project types in New project, then the list narrows to the types that hold every word typed.

<a id="d-req-fn-324"></a>
- **REQ-FN-324** — Several projects start together, a saved choice per solution. *BRD:* BRD-283 · *Mockup:* mockups/main.html
  - *Acceptance:* When the person ticks two or more projects in Multiple startup projects and presses Run, then each is built and started together and Stop ends them all.

<a id="d-req-fn-325"></a>
- **REQ-FN-325** — A folder with projects and no solution is said so and asked about before it opens. *BRD:* BRD-284 · *Mockup:* mockups/start.html
  - *Acceptance:* When the person opens a folder that holds a project and no solution on Start, then Chatur says so and asks whether to open it, before opening.

<a id="d-req-ui-128"></a>
- **REQ-UI-128** — The chat moves to the newest message after a long reply or a command such as *yolo. *BRD:* BRD-285 · *Mockup:* mockups/main.html
  - *Acceptance:* When a long reply or a command such as *yolo makes the conversation longer than its window on Workbench, then the conversation shows its newest message.

## Non-functional

<a id="d-req-nfr-014"></a>
- **REQ-NFR-014** — Every number on the measurements screens is computed by code and equals the total added up again from the files. *BRD:* BRD-261
  - *Acceptance:* When a test adds up the fixture's files again on Measurements, then every figure equals it, unmeasured ones read "not measured", and the words are TfLens's.

<a id="d-req-nfr-015"></a>
- **REQ-NFR-015** — The UsageGuide has "The agents and their commands", one table per agent, from the one command list in the code. *BRD:* BRD-263
  - *Acceptance:* When a test compares the UsageGuide with the command list on Workbench, then each agent's table names every command once, with what, example and where, and nothing else.

<a id="d-req-nfr-016"></a>
- **REQ-NFR-016** — The README names the four agents and their commands in short and links to the guide's section. *BRD:* BRD-264
  - *Acceptance:* When a test compares the README with the command list on Workbench, then every command is named once under its agent and no other command is named.

<a id="d-req-nfr-017"></a>
- **REQ-NFR-017** — The owner's test has a part for the measurements screens and one for typing a command of each agent. *BRD:* BRD-275
  - *Acceptance:* When the owner follows the Owner's test, then one part walks the measurements screens, picking the project in the app, and one types a command to each agent.

<a id="d-req-nfr-018"></a>
- **REQ-NFR-018** — New project's tests compare the exact paths on disk for each starting point. *BRD:* BRD-280
  - *Acceptance:* When the tests of New project run, then for each of the four starting points every path made on disk is compared with the one wanted.

<a id="d-req-nfr-019"></a>
- **REQ-NFR-019** — The owner's test makes its folders under this repository's bin folder and starts with the empty-folder case. *BRD:* BRD-281
  - *Acceptance:* When the owner follows the Owner's test, then every folder it makes is under this repository's bin folder, and part 1 is the empty-folder case.

<a id="d-req-nfr-020"></a>
- **REQ-NFR-020** — The owner's test names no path in the measurements part, and a real model call proves what Measurements shows. *BRD:* BRD-286
  - *Acceptance:* When the owner follows the Owner's test, then the measurements part picks the project in the app, and its docs/metrics holds the chat's sessions.

<a id="d-req-nfr-021"></a>
- **REQ-NFR-021** — Compare models is proven with a real read-only question on two models. *BRD:* BRD-297
  - *Acceptance:* When the tests ask two connected models a real read-only question on Compare models, then both answers show with time, tokens and cost, and runs.jsonl holds both records.

<a id="d-req-nfr-022"></a>
- **REQ-NFR-022** — The owner's test has parts for the keys and for Compare models, and runs in one order. *BRD:* BRD-298
  - *Acceptance:* When the owner follows the Owner's test on Measurements and Compare models, then one part reads each key, one compares two named models, and all parts run in one order.

## Page: Compare models (`/compare-models`)

<a id="d-req-fn-327"></a>
- **REQ-FN-327** — Pick two to four connected models and give them one task on the open project. *BRD:* BRD-291 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When the person ticks two to four models and types a task on Compare models, then Run starts each; one or five models are refused with the reason.

<a id="d-req-fn-328"></a>
- **REQ-FN-328** — A read-only question needs no copy and changes nothing. *BRD:* BRD-292 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When the kind is "Question, changes nothing" on Compare models, then each model answers from the open project and no file of the project changes.

<a id="d-req-fn-329"></a>
- **REQ-FN-329** — A task that changes files gives each model its own copy under the project's bin folder, removed afterwards. *BRD:* BRD-293 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When the kind is "Task that changes files" on Compare models, then each model works in its own copy under the project's bin folder, removed when the run ends.

<a id="d-req-ui-132"></a>
- **REQ-UI-132** — Time, tokens, cost and the answer or changed files, side by side. *BRD:* BRD-294 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When a comparison ends on Compare models, then one column per model shows its time, tokens in and out, cost or "no price", and its answer or its changed files.

<a id="d-req-ui-133"></a>
- **REQ-UI-133** — Mark each model's result right or wrong. *BRD:* BRD-295 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When the person marks a model's result right or wrong on Compare models, then the mark is kept and shows again when that comparison is opened again.

<a id="d-req-fn-330"></a>
- **REQ-FN-330** — Comparisons and marks are kept in docs/metrics in TfLens's shape and open again. *BRD:* BRD-296 · *Mockup:* mockups/compare-models.html
  - *Acceptance:* When a comparison ends or a mark changes on Compare models, then runs.jsonl holds one compare-models record per model with the mark, and the comparison opens again.

