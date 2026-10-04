# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-03 |
| Waiting on | 1 decision. The design picture has not been changed. |

## What happened

On the Register screen there is a box listing the password rules (at least eight characters, a capital letter, a number, and so on), with a strength bar above it. The design picture for Register marks that box and the bar as appearing only once you start typing a password. Until yesterday the app showed them straight away, on the empty form, every rule shown as "not met".

The check that compares each screen with its design picture flagged that difference. Yesterday a builder changed the app to match the picture: the box and the bar now stay hidden until the first character is typed, and then update with every key. Nothing else about Register changed. A weak password is still refused when you press the button, and the field is still marked. Which way is right is a choice about how the screen should feel, so it is yours. The design picture has not been touched.

## What I need you to decide

### 1. When the password rules appear on Register

Should someone registering see the password rules before they start typing, or only once they start?

| Option | What happens | What it costs |
|---|---|---|
| **A — Only once typing starts** | The form opens clean. The rules and the strength bar appear with the first character and update as you type. This is how the app works now and how the design picture draws it. | Nothing to do. Someone sees the rules only after they begin, not before. |
| **B — From the start** | The rules show on the empty form, all "not met", and tick off as you type. | The app change from yesterday is undone, and the design picture is amended to show the rules on the empty form, so the screen check stops flagging it. About an hour. |

**My recommendation: A**: the screen and its picture already agree, and the rules still show for the whole time a password is being typed.

## What I do when you answer

- A: nothing changes. The question is closed and Register's rows are re-checked in the next check run.
- B: the design picture is amended through a documents change, the app change is undone, then Register is rebuilt and re-checked.

## Copy this back to me

```
Chatur: decision 1 — go with option A. Keep the password rules hidden on Register until the first character is typed.
```

```
Chatur: decision 1 — go with option B. Show the password rules on the empty Register form: amend the Register design picture through *amend-docs, undo the app change that hides them, then rebuild and re-check Register.
```
