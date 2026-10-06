# Chatur — decisions I need from you

| | |
|---|---|
| App | Chatur |
| Written | 2026-10-04 |
| Waiting on | 1 decision. Nothing has been changed yet. |

## What happened

The release pipeline now builds both downloads, and the `v0.1.0-nightly` release on GitHub holds a Mac zip and a Windows zip made from your latest push. I downloaded the Windows zip and started it. It never gets past "Loading Chatur…". Chatur's own log says why: the downloaded build has no App Manager address. The address it uses on this machine, the local development App Manager, lives only in this machine's development settings, and the address a released build should use was left for after user acceptance testing. So the first thing Chatur does, checking whether you are signed in, fails, and the screen waits forever.

Where a released Chatur signs in is your choice: it decides which App Manager holds the accounts of people who download it, and it needs that server's address and its key for the Chatur application.

## What I need you to decide

### 1. Which App Manager the downloaded Chatur signs in to

A downloaded Chatur needs an App Manager address and the Chatur application's key. Which server should that be, and how should the build get them?

| Option | What happens | What it costs |
|---|---|---|
| **A — Your production App Manager, filled in by the pipeline** | You add two repository secrets on GitHub (the App Manager address and the Chatur application key). The pipeline writes them into the build's settings file, so every download signs in to that server. Nothing secret is stored in the code. | You need an App Manager reachable from the internet with a Chatur application on it. About an hour of work here once the secrets exist. |
| **B — No server yet; the download says so plainly** | The download opens a clear screen: "Sign-in is not set up for this build", with where to put an address in a settings file next to the app. Nobody can sign in to a download until you choose A later. | Small. People who download it cannot use Chatur until A is done. |

**My recommendation: A**. A download that cannot sign in cannot be tried at all, and the pipeline already holds secrets the same way for the package feed.

Whichever you choose, the hang itself is a fault and will be fixed: when sign-in cannot even start, Chatur must show the reason instead of "Loading Chatur…" forever.

## What I do when you answer

- A: the pipeline writes `AppManager:BaseUrl` and `AppManager:ApiKey` from your two secrets into the published settings file for both downloads; the loading hang is fixed; after your next push I download the nightly again, sign in, and check Prerequisites shows the version and commit.
- B: the "not set up" screen is added, the hang is fixed, and the nightly is re-checked up to that screen.

## Copy this back to me

```
Chatur: decision 1 — go with option A. I added the GitHub secrets CHATUR_APPMANAGER_URL and CHATUR_APPMANAGER_KEY; have the pipeline write them into both downloads, fix the loading hang, and re-check the nightly.
```

```
Chatur: decision 1 — go with option B. Make a download without an App Manager address say so on a clear screen, fix the loading hang, and re-check the nightly.
```
