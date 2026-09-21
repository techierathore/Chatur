# Chatur — Technical decisions

| | |
|---|---|
| App | Chatur |
| Date | 2026-09-21 |

Companion to `docs/Chatur-Brief.md`. These are already taken by the owner. Day one records them in the Architecture and does not reopen them.

## Platform and layout

- .NET 10. A MAUI Blazor Hybrid host for Mac Catalyst and Windows, equal from the first release.
- Project names: `Chatur` (the host), `ChaturUI` (the Razor class library, written without a dot), `Chatur.Core`, `Chatur.Tests`.
- Every screen is in `ChaturUI`, using TrBlazeUI.
- Every behaviour is in `Chatur.Core`, with no screens in it, behind one action layer.
- xUnit tests that run on macOS and on Windows.

## Models

- Models are called through the owner's TechieRag library. Chatur never starts another company's coding tool.
- What TechieRag lacks is added in the TechieRag repository, not in Chatur: sign-in other than a key, the OpenAI Responses style, live streaming of tool calls, a fallback chain of any length, and pause and resume of a flow for the owner's approval.
- The ChatGPT browser sign-in itself (the local listener, and storing the token in Keychain or Credential Manager) belongs to Chatur.
- A provider is one connector, one sign-in method and one web address, so a new vendor is a new row of data.
- Model routing starts from the logic of TechieFlow's `routing.yaml`: tiers, fallback chains, a tier per role and per kind of work, and a move to a stronger model after repeated failed fixes.

## Data

- SQLite with Dapper and DbUp in the user's application-data folder, never beside the program. No EF Core.
- A database change that has shipped in a nightly build is never edited. A new one is added.
- The fixed steps and checks of a process are code. The wording, rules, rights, commands and model tiers of roles are database rows with history, seeded from data that ships with each build.
- Every correction Chatur makes to those rows is logged with what changed and why, and can be kept or undone.
- Role and rule data can be exported and imported. Approved corrections go back into the seed data of the next build.

## Accounts

- Sign-in, registration and sign-out use App Manager as `docs/AppManager-api-usage-guide.md` describes. Feedback, issues, licences and feature control use it in the last phase.

## Agent rules

- Check-ins: an AI model never runs a source-control command by its own choice, because a model that can commit, reset or push freely can lose work. A guard refuses it. Check-ins still happen, in two ways. The owner uses Chatur's source-control screen (changes, check in, push, pull, branches). And a process can have a fixed "check in" step that Chatur's own code carries out, for example after each requirement group is verified during an end-to-end run. Those automatic check-ins go to a separate branch made for the run, with the requirement ids in the message. Pushing, and merging into main, stay with the owner.
- Every tool request passes through guard classes that have unit tests.
- Chatur reads and writes the same project documents TechieFlow uses (`docs/{App}-BRD.md`, `{App}-Checklist.md`, `PROJECT-STATUS.md`, `docs/metrics/*.jsonl`) and needs nothing installed in a project's folder.

## Metrics

- Chatur writes the same measurement files TechieFlow writes, into each project's `docs/metrics` folder: `runs.jsonl`, `gates.jsonl`, `misses.jsonl`, `sessions.jsonl` and `commits.jsonl`, each only ever added to, never edited.
- Chatur has its own metrics schema, kept and managed in the Chatur repository. It starts as a copy of TechieFlow's `.tfcore/telemetry/SCHEMA.md` as it stands on the day it is copied. From then on the two are managed separately, and Chatur does not depend on TechieFlow's file. They must keep matching in every field and meaning that TfLens reads, so TfLens shows a Chatur project and a TechieFlow project the same way. A change to Chatur's schema that would break that match is not made without the matching change on the other side.
- In the field that names the tool that did the work, Chatur always writes `chatur`. TfLens reads that field as a value, not from a fixed list.
- The same rules carry over: a number that was not measured is written as empty, never guessed; a record with an unknown field or value is refused when written; one defect is one miss and every miss says whose gap it was; money is worked out when reporting, never stored; a failed write never stops the work.
- Chatur owns its agent loop, so tokens, model, tier, time and the count of helper agents are measured directly, not read back from another tool's logs.
- `docs/{App}-Misses.md` and the metrics report are rebuilt from the files, as today.
- Corrections Chatur makes to its own roles and rules are logged in Chatur's database in the same miss shape, so they can be reviewed the same way.
- Writing starts in phase 1 step 3, with the first agent session.

## Release

- GitHub Actions: a Mac job (the Mac Catalyst .app, zipped) and a Windows job (unpackaged, self-contained, zipped, `WindowsPackageType` None).
- Every push to main publishes a nightly pre-release with both downloads. A version tag publishes a named release. Version and commit are stamped into the build. No code signing before the last phase.

## How phase 1 is built

- Phase 1 is one phase built in four steps, in the order the brief gives. Each step ends as a nightly build the owner uses on real work before the next step begins.
- Every requirement is verified on the Mac first, then on Windows.
- How Chatur's own screens are tested while it is being built. The automatic screen checks use a browser with no window, and a browser can only look at web pages, not inside a Mac or Windows app window. Chatur's screens are web-style screens that live in `ChaturUI`, so the solution includes a small web project, used only for testing, that shows the same screens in a browser. The automatic checks run there on every build. The Windows app window is also checked automatically through the Windows test bridge this repository already has. The Mac app window is checked automatically once the owner's Mac is set up as a test machine and its address is put in `core-config.yaml`. Until then, what only shows in the real Mac app is caught by the owner using the nightly build on the Mac.
