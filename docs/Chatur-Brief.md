# Chatur — Brief

| | |
|---|---|
| App | Chatur |
| Date | 2026-09-21 |

## What it is

Chatur is a small development environment for Mac and Windows where one person builds software by directing AI agents. You chat with agents that each play a role: analyst, architect, flow master, verifier and the library specialists. They write the documents, write the code, build it, run it and test it, and you review and approve. It replaces the owner's TechieFlow framework and the coding tools it sits on. It is a complete application of its own and talks to the AI models directly. Its roles, rules and process steps are data inside Chatur, so when one of them proves wrong during work, Chatur corrects it on the spot and logs the correction for later review. Nothing is carried by hand to a second repository.

## Who it is for

First, the owner: one developer with about twenty projects, working on a Mac and a Windows machine, who has used spec-driven development for a year. Two pains. Each gap in TechieFlow means writing feedback, carrying it to the framework repository, fixing it, redeploying and coming back, and this round trip wastes the most time. And there is no light tool that builds and runs his code on both machines: Rider is heavy and Visual Studio is opened only to press Run. Later, other developers who work the same way, signed in and licensed through the owner's App Manager service.

## Must do

### Phase 1, step 1: the app, sign-in, build and run

1. The owner installs Chatur on his real Mac and his Windows machine from a download that every change to the main branch produces, and his data survives each update.
2. The owner signs in, registers and signs out through App Manager.
3. Chatur finds the owner's projects in the folders he names, and he picks one to work on.
4. The owner builds, runs and stops the selected project with a run target that fits the machine, watches the output live, and stopping ends every program the run started.
5. A Doctor screen shows each tool a project needs, its found version and the fix when missing, and Chatur's own version and commit. On a Mac started from Finder, Chatur still finds tools installed with Homebrew.

### Phase 1, step 2: models, routing and chat

6. The owner connects several model sources: a pasted key, a local model, and a ChatGPT subscription sign-in, with secrets kept in the operating system's store for secrets.
7. The owner sets which model each role and each kind of work uses, in tiers, with a fallback chain. When a model is limited or unavailable Chatur moves to the next in the chain and shows which model answered, and after repeated failed fixes it moves the work to a stronger model.
8. The owner chats with a role he chooses, about the selected project, and sees the tokens used.
9. Roles, their rights, commands, rules and process steps are loaded from Chatur's own database, not from text files.

### Phase 1, step 3: the agent and change review

10. An agent reads, searches, edits and creates files, runs commands, and builds and runs the project.
11. Each role can do only what its rights allow: the analyst reads code but cannot change it, the flow master can do everything and can call the other roles, and only the verifier can mark work verified.
12. Every action an agent asks for passes guards first, and a model never runs a source-control command by its own choice.
13. The owner watches a live list of what the agent is doing, can stop it, and chooses "ask me first" or "go ahead" for a session that is saved and can be continued later.
14. The owner sees each change as before and after, and approves or rejects it.
15. When a role, rule or process step proves wrong, Chatur corrects it in its database and logs what changed and why, as a miss the owner can review, keep or undo.
16. From the first agent session, every project worked in Chatur gets the same measurement files TechieFlow writes today (runs, requirement verdicts, misses, sessions and check-ins, in `docs/metrics`), with the same fields and meanings and with `chatur` as the name of the tool, so TfLens tracks the project with no change.
17. The owner exports the role and rule data from one machine and imports it on the other, and approved corrections become the starting data of the next Chatur build.

### Phase 1, step 4: the editor and source control

18. The owner browses the project in a solution and folder tree, opens files in tabs, makes quick edits and saves, and can open any file in his own editor or with the system's default application.
19. The owner sees what changed in the project, checks in, pushes, pulls and switches branches from a source-control screen.

### Phase 2: the project workflow

20. Every TechieFlow command exists as a Chatur process with the same fixed steps, checks and owner stops, reading and writing the same project documents, so a project can move across one at a time.
21. Work is routed to the right role by requirement type, each role has its own view, and the owner sees every requirement and its status on a board.

### Phase 3: end to end, and unattended

22. One extra flow master command takes a brief and runs the whole chain to "ready for the owner's acceptance test", starting each role as needed, while step-by-step working stays the normal way.
23. A run continues unattended through limits, stalls and crashes, can be queued and resumed, never reports complete while work is unfinished, and leaves a report the owner reads afterwards.
24. Model routing becomes automatic: a small model inside TechieRag picks the model for each piece of work.

### Phase 4: more users

25. Feedback, issues, licences, feature control and syncing of role data go through App Manager, Chatur's verifier checks the windows of Mac and Windows apps it has built, and there is one view across all projects, work in separate working copies, Chatur's actions offered to other tools, and signed installers.

## Out of scope

Chatur never starts, wraps or depends on Claude Code, OpenCode, Codex or TechieFlow's scripts, and puts nothing into a project's folder. No Claude subscription sign-in, which Anthropic does not allow. Claude is used with a key. The editor is for quick edits: no debugger, no property grid, no package manager screens. A bug in Chatur's own program is not self-corrected. It follows the feedback process and arrives as the next build. No payments. No phone or web version of Chatur. No installer signing before phase 4. The owner tests Chatur itself on both real machines from step 1. The technical decisions are already taken and are in `docs/Chatur-Technical-Decisions.md`. Day one records them and does not reopen them.

## Open questions

- Can the owner use Chatur with no internet, given that sign-in goes through App Manager?
- Which App Manager server address and application identity does Chatur use?
- Which run targets belong in step 1 beyond web, Mac and Windows: Android emulator, iOS simulator, plain dotnet and node programs?
