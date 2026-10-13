# Chatur — what to check in the next nightly

| | |
|---|---|
| App | Chatur |
| For | the first nightly built after 2026-10-13 |
| Written | 2026-10-13 |
| Covers | the fixes for the 12 Oct test, then the screens that were never re-checked by hand |

Use each screen for its job. Write **Pass**, **Fail** or a note in the last column. A Fail is a
defect: put the screenshot in a folder under `docs/screenshots/` and paste this, with your folder's name:

Claude Code:
```
/TechieFlow:agents:flow-master *fix-issues Chatur docs/screenshots/<your-folder>
```
OpenCode:
```
/flow-master *fix-issues Chatur docs/screenshots/<your-folder>
```

Before you start, have one model provider key to hand (a cheap one is enough) and a small project
folder that is a git repository.

## 1. Sign in and the start window

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 1.1 | Start Chatur and sign in with your own production App Manager account. | The start window, with your name at the top right. | Phase 1 sign-in | |
| 1.2 | Sign out from your name menu, then sign in with a wrong password. | A plain message that the sign-in failed. You stay on the sign-in screen. | Phase 1 sign-in | |
| 1.3 | On the Start window, click each of New project, Open a solution, Open a folder and Clone a repository. | Each opens the same dialog, set to that way in. Before the fix these four buttons were dead. | REQ-UI-073 | |
| 1.4 | Open your small git project through Open a folder. | The main window opens on that project, files on the left. | REQ-FN-007 | |

## 2. The menu bar (the 12 Oct "Nothing to do yet" defect)

Open every menu and click every item. No menu may show "Nothing to do yet" or any other placeholder.

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 2.1 | **File**: New project, Open a solution, Open a folder, Clone a repository. | The open-project dialog each time. | REQ-UI-073 | |
| 2.2 | **File**: Recent projects. | Your recent projects listed under a "Recent projects" heading (a group, not a side list; see Known problems). Clicking one switches to it. | REQ-UI-073 | |
| 2.3 | Open a file, change it, then **File ▸ Save**, then change two files and **File ▸ Save all**. | Save is dim until something has changed. The changes are on disk afterwards. | REQ-UI-073, REQ-FN-044 | |
| 2.4 | **File ▸ Close project**, then reopen it. **File ▸ Exit** last of all. | Close project returns to the start window. Exit closes Chatur. | REQ-UI-073 | |
| 2.5 | In an open file: **Edit ▸ Undo, Redo, Cut, Copy, Paste**. | Each acts on the file's text. Redo is dim until you have undone something. | REQ-UI-073 | |
| 2.6 | **Edit ▸ Find in files**, then **Edit ▸ Go to file**. | A search box. Find lists files containing the text; Go to file opens the one you pick. | REQ-UI-073 | |
| 2.7 | **View ▸ Files, Chat, Output**: untick each, then tick it again. | That panel hides and comes back. | REQ-UI-074 | |
| 2.8 | **View ▸ Zoom in, Zoom out, Actual size**. | The whole window grows, shrinks and returns to normal. | REQ-UI-073 | |
| 2.9 | **View ▸ Prerequisites, Repository, Appearance**. | Each opens in the second window, not in the main one. | REQ-FN-106 | |
| 2.10 | **Project**: each of the eight items. | Prerequisites and Repository open the second window. Processes, Board, Agent workspace, End-to-end run, Run queue and Run record open as tabs in the main window. | REQ-FN-106 | |
| 2.11 | **Tools ▸ Settings**, then each page under "Go straight to". | The second window opens on Model providers, then on each page you pick. | REQ-FN-106 | |
| 2.11a | In the main window press **Ctrl+,**, also while typing in an open file. | Settings opens in the second window. No comma is typed into the file. Fixed 13 Oct. | REQ-UI-073 | |
| 2.12 | **Help ▸ About Chatur**, then **Help ▸ Check for updates**. | About shows the version and commit. Check for updates opens Prerequisites. | REQ-UI-018, REQ-FN-013 | |

## 3. Settings: make the chat able to answer (the first fix)

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 3.1 | **Model providers**: add a provider by pasting its key. | It is listed. The key is not shown in full anywhere. | REQ-FN-014 | |
| 3.2 | Check that the provider answers. | A clear yes, or a clear reason why not. | REQ-UI-019 | |
| 3.3 | Remove the **OpenCode Go** provider. My test added it to your real setup on 13 Oct, so this removes it and tests Remove at the same time. | It disappears and does not come back after a restart. | REQ-UI-020, REQ-FN-017 | |
| 3.4 | If you use one: connect a local model, or sign in to a ChatGPT subscription. | It is listed and answers the check. | REQ-FN-015, REQ-FN-016 | |
| 3.5 | **Routing**: set a tier for a role and for a kind of work, reorder the fallback list, then restart Chatur. | Your choices are still there after the restart. | REQ-FN-018 to REQ-FN-020, REQ-FN-023 | |
| 3.6 | **Agents**: open a role, change its wording and its tier, and save. Then look at its history. | The change is kept, and the earlier version is listed. Its rights are shown at a glance. | REQ-UI-025 to REQ-UI-030 | |
| 3.7 | **Agents**: export the roles to a file, then import that file. | The roles come back unchanged. | REQ-FN-028, REQ-FN-029 | |
| 3.8 | **Corrections** and **Measurements**: open each. | Real entries from your work, or a plain "nothing yet". No made-up sample rows. | REQ-UI-034, REQ-FN-037 to REQ-FN-043, REQ-UI-035, REQ-UI-036 | |
| 3.9 | **Appearance**: pick another theme. | The whole app changes, in both windows, and stays changed after a restart. | REQ-UI-037, REQ-UI-038 | |

## 4. The chat

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 4.1 | Ask a real question about your project. | An answer that appears as it is written, not all at once at the end. | REQ-FN-025, REQ-FN-027 | |
| 4.2 | Look under the answer. | Which model answered and what it cost. | REQ-FN-026, REQ-UI-021 | |
| 4.3 | Ask for something long, then press Stop. | The answer stops at once. | REQ-UI-023 | |
| 4.4 | Close the chat panel with its own close button. Then bring it back with **View ▸ Chat**. | The panel goes, then comes back holding the same conversation. | REQ-UI-074 | |

## 5. Prerequisites and Repository

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 5.1 | **Prerequisites**. | Every tool with the version found. A missing tool says how to fix it. Which Chatur this is. | REQ-FN-011, REQ-UI-017, REQ-UI-018 | |
| 5.2 | **Repository** after changing a file. | What changed, file by file. | REQ-UI-042 | |
| 5.3 | Check in the change, then push and pull. | The commit is made with your message; push and pull report success or a plain reason. | REQ-UI-043, REQ-UI-044 | |
| 5.4 | Switch to another branch and back. | The files change to match the branch. | REQ-FN-047 | |

## 6. Phase 2 screens

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 6.1 | **Processes**: pick a process, click the pencil on one step, change its wording and save. | The new wording shows. The old wording is kept as a correction under Settings ▸ Corrections. | REQ-UI-075 | |
| 6.2 | **Board** on your project. | Every requirement with its status, read from the checklist. | REQ-UI-049 | |
| 6.3 | Rename or move the project's folder outside Chatur, then open **Board**. | A plain message that the folder is missing, not an error or an empty board. | REQ-UI-049 | |
| 6.4 | **Board**: change a requirement's status, then open the checklist file. | The file shows the same change. | REQ-FN-057 | |
| 6.5 | Start a process on your project and watch it run until it reaches a step that waits for you. | While it runs, the step it is on says running and the finished steps show their result. At the stop it waits and does nothing more until you answer. | REQ-FN-050 to REQ-FN-054 | |
| 6.5a | With the run waiting, close Chatur and start it again. Open Project ▸ Processes ▸ Last run and answer. | The run is still waiting on the same step and carries on from there, not from the start. | REQ-FN-055 | |
| 6.6 | **Agent workspace**: switch to another agent mid-conversation. | The conversation carries on with the new agent, nothing lost. | REQ-FN-058 to REQ-FN-060 | |

## 7. Phase 3 screens

Every row here was re-tested on 13 Oct by its own tests, opening each screen from the Project menu,
and each screen was opened in the Windows app. Test them by use all the same.

| # | Do this | You should see | Rows | Result |
|---|---|---|---|---|
| 7.1 | **End-to-end run** with a short brief on a throwaway project. | Each role starts in turn, and the run stops at the acceptance test for you. | REQ-FN-061, REQ-UI-055, REQ-FN-062 | |
| 7.2 | Watch it while it runs. | What is happening now, step by step, and the agent's output in the chat. | REQ-UI-056, REQ-UI-070, REQ-UI-071 | |
| 7.3 | **Run queue**: queue two runs, pause, resume, take one out. | Only one runs at a time; pause and remove do what they say. | REQ-FN-067 to REQ-FN-069, REQ-UI-057 | |
| 7.4 | **Run record** of the finished run. | Every requirement with its verdict, every miss, and what it cost. An earlier run can be opened. | REQ-UI-058 to REQ-UI-061 | |
| 7.5 | Close Chatur mid-run, start it again, then open **Project ▸ End-to-end run**. | The same run carries on from the step it had reached. Its record has a line saying it was picked up again. It does not carry on until you open End-to-end run (see Known problems). | REQ-FN-061, REQ-FN-065 | |

## Known problems — do not report these again

| Problem | What it affects | Does it block or break anything |
|---|---|---|
| An end-to-end run cut off by closing Chatur carries on only once End-to-end run is opened for that project, not when Chatur starts | Unattended runs after a restart or crash | It waits for you instead of carrying on by itself. Your call whether it should start by itself |
| With six or more tabs open, the tab strip scrolls and the newest tab can be out of view | Finding the tab you just opened | No |
| TR-020: TrBlazeUI's menu bar had no submenu. **Fixed upstream 13 Oct**, ships in the TrBlazeUI release after 2.1.6, not on the feed yet | Until then Recent projects and Zoom are groups inside File and View, not side lists. Re-checked here by upgrading and moving both to `MenubarSub` | No |
| TR-021: a disabled menu item was not announced as disabled. **Fixed upstream 13 Oct**, same release | Until then screen readers read dimmed items as available. Re-checked here by the menu tests' `toBeDisabled()` after the upgrade | No |
| TF-012: verify counted a row as tested when its id appeared in another test's sample data. Open; prompt below | REQ-FN-053 and REQ-FN-055 were Verified untested until 13 Oct; now tested for real | No |
| The top bar says "Signed in" after a failed sign-in check | What the top bar tells you | No, but it is wrong |
| TR-RAG-006: the small-model router. **Fixed upstream 9 Oct** in TechieRag 1.1.5, which Chatur uses | Automatic routing (REQ-FN-071, REQ-FN-072) has never run against a live model. Re-checked here by the entry's live run once a local or cheap small model is set up | Not tested live |
| Nothing has been run on a Mac: Homebrew tools from Finder (REQ-FN-012) | The Mac build | Unknown until run on a Mac |
| TF-010: Mac zoom reached only the last window opened. **Fixed upstream 12 Oct** | Mac zoom. Re-checked here by `tf-verify-zoom.sh` on a Mac head | Unknown until run on a Mac |
| Test users Second and Yolo do not exist on production App Manager | Tests sign in as Owner | No |
| The logo is a placeholder | Looks only | No |

The prompt that gets TF-012 fixed, for the TechieFlow agent:

```
Read C:\1MyCode\Chatur\docs\Chatur-TechieFlow-Feedback.md, entry TF-012 (open, filed 2026-10-13). tf-verify-tests reads a requirement id anywhere in a test's reported name, so an xUnit theory whose InlineData holds "REQ-FN-053" passed REQ-FN-053. Read ids only from the leading REQ tokens of a name, drop a theory case's argument list, and flag a row whose only passing test is about another row. Reply in that file with the version carrying the fix.
```
