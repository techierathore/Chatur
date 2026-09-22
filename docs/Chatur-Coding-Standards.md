# Chatur — Coding Standards

| | |
|---|---|
| App | Chatur |
| Stack answer set | dotnet (`.tfcore/templates/stack-defaults/dotnet.md`) |
| Date | 2026-09-22 |

## Standards applied

| File | Applies | Notes |
|---|---|---|
| `.tfcore/standards/coding-standards-core.md` | yes | every project |
| `.tfcore/standards/coding-standards-dotnet.md` | yes | from the dotnet answer set: names, database, projects and files, code, tests, logging, testability, security |

Per-project choices the stack file leaves open:

| Choice | Decision |
|---|---|
| Instance-field prefix | `obj` — `private readonly ILogger<X> objLogger;`. The stack file's default, taken as it stands |
| Test names | Short PascalCase, no underscores; the whole scenario goes in the XML `<summary>` |
| Rendering mode | None chosen. Every view is rendered by a `BlazorWebView` in the MAUI host, which is interactive by nature; the same components render in `Chatur.WebHarness` for the browser checks |

## Project rules

Rules that hold in this project only. Empty is a valid answer.

| Rule | Why | Since |
|---|---|---|
| A view is a `.razor` file with its own `.razor.cs` code-behind. Never markup and logic in one file, and never one page holding several tabs | A settings page has to be readable, testable and changeable on its own (owner, 2026-09-22) | 2026-09-22 |
| Views live in `ChaturUI`, behaviour in `Chatur.Core` behind one action layer. A view never reaches past the action layer, and `Chatur.Core` never references a view | The screens are checked in a browser against `Chatur.WebHarness`; behaviour is checked without a window | 2026-09-21 |
| No control is hand-built when TrBlazeUI 2.0.9 has one. `TreeView`, `DiffView`, `CodeEditor`, `EditorTabs`, `LogView`, `Typing`, `NavList`, `SortableList`, `DataTable` row selection and the small `Switch` all exist | Ten of these were hand-built in the mockups before the library shipped them; the code must not inherit that | 2026-09-22 |
| A theme is a file of OKLCH values, never colours written into a component | A fifth theme has to be addable without a new build (BRD-157) | 2026-09-22 |
| A model never runs a source-control command. The guard that refuses it is a class with its own unit tests | A model that can commit, reset or push freely can lose work | 2026-09-21 |
| Anything TrBlazeUI, TechieRag or App Manager lacks is reported in that library's feedback file and the feature waits. Never a workaround in Chatur | The round trip to a second repository is the waste this product exists to remove | 2026-09-21 |
| A migration that has shipped in a nightly build is never edited; a new one is added | The owner is already running that build with data in it | 2026-09-21 |
| Secrets go to the operating system's store. The database holds only the name a secret is filed under | BRD-91 | 2026-09-21 |

## Enforcement

- **Editor configuration:** `.editorconfig` at the repository root carries the machine-checkable subset — naming, `var` use, file-scoped namespaces, analyzer severities.
- **Analyzers:** the .NET SDK analyzers at their default severity, plus `TreatWarningsAsErrors` on every project.
- **Verifier checks:** the greps listed in the stack file's Enforcement section, plus these for this project: a `.razor` file with a `@code {` block that holds more than a field declaration (the logic belongs in the code-behind); a raw `<input`, `<select`, `<button` or `<table` in a `.razor` file under `ChaturUI` (TrBlazeUI has a control for each); a colour literal — `#`, `rgb(` or `oklch(` — anywhere outside a theme file; and any use of `git` outside `Chatur.Core`'s source-control module.
