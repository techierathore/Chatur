# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-06 |
| Waiting on | 1 decision. Nothing has been changed yet. |

## What happened

Your push built a new nightly (commit ca10bf8), and the release now holds both downloads. I downloaded the Windows one and ran it. It no longer hangs: it opens Sign in, reaches your live App Manager at appmgrapi.techierathore.com with the downloadable-app key, and asks it to sign in. The live App Manager answered "Invalid email or password" for Chatur's owner test user, and Chatur showed that message on Sign in, as it should.

The test users in the Usage Guide were made on the development App Manager on this machine, which is a different server with its own accounts. The last step of the check — signing in and seeing this build's version and commit on Prerequisites — needs one account that exists on your live App Manager. Making accounts there is your call.

## What I need you to decide

### 1. A test account on your live App Manager

Which account should the check of a downloaded Chatur sign in with?

| Option | What happens | What it costs |
|---|---|---|
| **A — Register the owner test user on the live App Manager** | I register chatur-owner@techierathore.com, with the password already kept in this machine's user secrets, through Chatur's own Register screen in the downloaded build. The Usage Guide notes that this account also exists on the live server. | One real account on your live App Manager, in the Chatur application. A few minutes. |
| **B — You give me an account** | You create a test account on the live App Manager yourself and add its password to the user secrets under the name you choose; the Usage Guide records it. | Your time to create it; nothing is created by me. |

**My recommendation: A**: it uses the account every other check already uses, and it goes through Chatur's own Register screen, which is itself worth proving on the live server.

## What I do when you answer

- A: register the account through the downloaded nightly, note it in the Usage Guide test users table, then sign in, check Prerequisites shows 0.1.0 · nightly and commit ca10bf8, and write the verdict for the release requirement.
- B: once the account and its password are in the user secrets, do the same sign-in and check with it.

## Copy this back to me

```
Chatur: decision 1 — go with option A. Register chatur-owner@techierathore.com on the live App Manager through the downloaded nightly, record it in the Usage Guide, and finish the release check.
```

```
Chatur: decision 1 — go with option B. I created a test account on the live App Manager and put its password in the user secrets under Chatur:TestUsers:Live; record it in the Usage Guide and finish the release check.
```
