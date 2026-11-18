# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-11-17 |
| Waiting on | 1 decision. Nothing has been changed yet. |

## What happened

Each account now has its own Chatur: its own projects, chats, runs and model keys. To prove that on screen I need two real accounts signing in one after the other on the same copy of Chatur. Only one test account exists on App Manager: the owner test user. The second test user listed in the usage guide (chatur-second) is refused by App Manager with "Invalid email or password", so it has never been registered.

The usage guide says test users are registered only by you, with the register script, so I did not create one. Everything else is done and checked: the code tests show two accounts never see each other's data, and on the Windows app a new account opened an empty start even with older data on the machine. What is missing is the same thing shown with two live accounts, on screen. Three requirements wait on it: "what one account makes or opens, another never sees", "a chat is never continued by another account" and "signing in as another account leaves nothing of the last one on screen".

## What I need you to decide

### 1. A second test account on App Manager

Register test user 2 once, so the two-account checks can run on screen.

| Option | What happens | What it costs |
|---|---|---|
| **A — register test user 2** | You run the register script once; the browser test and the Windows walk then sign in as two real accounts, and the three waiting requirements can be checked fully | One command on your side, about a minute |
| **B — leave it** | The three requirements stay at "built", proven by the code tests only | Nothing now; the on-screen proof of the second account stays open |

**My recommendation: A** — it is the only way to see, on screen, that a second account starts empty, which is exactly what you found broken.

## What I do when you answer

1. With A: I set test user 2 as present in the test settings file and run the two-account browser test.
2. I walk the Windows app again with both accounts: the first opens a folder and chats, the second must see the empty start.
3. I re-check the three waiting requirements and update the status page. About 30 minutes.

## Copy this back to me

```
Chatur: decision 1 — go with option A. I ran node .tfcore/utils/tf-register-test-users.mjs; test user 2 now exists. Re-run the two-account checks.
```
