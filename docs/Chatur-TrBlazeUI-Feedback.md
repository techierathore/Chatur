# TrBlazeUI feedback — found while building Chatur

| | |
|---|---|
| App | Chatur |
| Upstream | TrBlazeUI |
| Updated | 2026-10-13 |

## Summary

22 entries: nothing is blocked. 1 open (TR-022, filed 2026-10-30, worked around in `chatur.css`), 21 closed — TR-020 and TR-021 ship in 2.1.9, the rest closed on 2.1.6 or earlier.

All ten were answered on 2026-09-22 and all ten have now been re-checked in Chatur against
2.0.9. Six were fixed in code; four needed no code, because the control already existed. Each
entry below carries the control that now does the job and the Chatur requirement that uses it.

The first ten were found on day one, against the 2.0.7 reference; each entry says what was used instead.

> **Upstream reply 2026-09-22 — read it before acting on this file. The action is: upgrade
> to 2.0.9.** Four entries (TR-001, TR-002, TR-005, TR-009) and most of a fifth (TR-008)
> report controls that already exist. Two of them shipped in **2.0.8, on 2026-09-20 — the
> day before this batch was filed**; three were documented in the very copy of the
> reference this file cites. The other six are in **2.0.9**, released 2026-09-22. Note the
> feed: 2.0.7 onward are on GitHub Packages, not nuget.org. Full answer in
> "[Replies from TrBlazeUI](#replies-from-trblazeui)" at the foot of this file.

## Entries

### TR-001 — There is no tree control for files and folders

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `TreeView` + `TreeItem` in 2.0.9 take a nested list with expand, collapse, a bound selection and the arrow keys. Chatur's files panel (BRD-80) drops its hand-built indentation and uses it.
- **Severity:** major
- **Blocks:** no — the Editor mockup draws the tree with nested Collapsible and SidebarMenu items, and the work carried on
- **Repro:** Look for a tree in `.trblazeui/TrBlazeUI-AI-Reference.md` 2.0.7: sections 3 to 8 hold no control that shows a nested, expandable list of items with icons and a selected row.
- **Expected:** A `Tree` (or `TreeView`) taking a nested list, with expand and collapse, a selected item bound two ways, an icon per item and keyboard movement.
- **Actual:** The closest are `Collapsible` and the Sidebar menu components, which are built for a fixed two-level menu, not for a folder tree of unknown depth.
- **Encountered in:** BRD-80, the Editor screen of Chatur phase 1
- **Workaround:** Nested `Collapsible` inside a `ScrollArea`, with the indentation done by hand in the markup.
- **Suggested fix:** Add a generic `Tree<TItem>` with `Items`, `ChildrenSelector`, `@bind-Selected`, `ExpandedIds` and an item template.

### TR-002 — There is no side-by-side difference viewer

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `DiffView` (and `TextDiff` for the lines in code) in 2.0.9 take a before and an after, side by side or inline, with line numbers. The change waiting in the conversation (BRD-64) and the Repository page (BRD-86) both use it.
- **Severity:** major
- **Blocks:** no — the Changes and Source control mockups put two `CodeBlock` panels in a two-column grid and colour the changed lines by hand
- **Repro:** Look for a difference or comparison control in the 2.0.7 reference: section 8 has `CodeBlock` and `Prose`, and neither compares two texts.
- **Expected:** A `Diff` control taking a before text and an after text, showing them side by side or one above the other, with added, removed and unchanged lines marked and line numbers on both sides.
- **Actual:** `CodeBlock` shows one text with no line marking, so a before and after view has to be assembled and coloured by the application.
- **Encountered in:** BRD-64 and BRD-86, the Changes and Source control screens of Chatur phase 1
- **Workaround:** Two `CodeBlock` panels in a grid, with line classes written by the application.
- **Suggested fix:** Add `Diff` with `Before`, `After`, `Mode` (side by side or inline), `ShowLineNumbers` and a per-line CSS hook.

### TR-003 — CodeBlock cannot be edited

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `CodeEditor` in 2.0.9 is an editable code area with a gutter and Tab inserting an indent, and `EditorTabs` is the open-file strip with a close button and an unsaved mark per tab. Chatur's file view (BRD-81 to BRD-83) uses both, and `ReadOnly` gives us the rule that a file with a change waiting on it cannot be typed into.
- **Severity:** minor
- **Blocks:** no — the Editor mockup draws the editing area as a `Textarea` in a monospace font, which is what Chatur's quick edits need
- **Repro:** `CodeBlock` in section 8 of the 2.0.7 reference renders text for reading; it takes no value binding and no change event.
- **Expected:** An editable code area: a value bound two ways, a language for the highlighting, line numbers, and the tab key inserting an indent instead of moving focus.
- **Actual:** The only editable text controls are `Textarea`, `MarkdownEditor` and `RichTextEditor`; none highlights code, and the last two are wrong for source files.
- **Encountered in:** BRD-82, the Editor screen of Chatur phase 1
- **Workaround:** `Textarea` with a monospace class. Chatur's editor is for quick edits, so this is enough for now.
- **Suggested fix:** Either make `CodeBlock` take `@bind-Value` with an `Editable` flag, or add a `CodeEditor` beside it. An editor also needs a strip of open-file tabs, which `Tabs` is not: each tab needs a close button and a mark for unsaved work.

### TR-004 — A step cannot carry its own state in Stepper or Timeline

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `StepperItem.Status` in 2.0.9 carries done, running, waiting, failed and pending per step, with a vertical orientation and a template for trailing content. Process run (BRD-101) and the end-to-end chain (BRD-118 to BRD-125) use it, including the step that finished only after a retry.
- **Severity:** major
- **Blocks:** no — the run and process screens draw their step lists by hand, and the work carried on
- **Repro:** `Stepper` in section 8 of the 2.0.7 reference takes `Current="2"` and `StepperItem Title/Description`; `Timeline` takes `Current="true"` per item. Both work out every other step's state from its position.
- **Expected:** Each step says what happened to it: finished, finished after a retry, running, waiting for the owner, failed, still to come — and can carry a subtitle and something at its trailing edge.
- **Actual:** A chain that paused in the middle, or a step that succeeded only on a second try, cannot be drawn: anything before `Current` is finished and anything after is not started.
- **Encountered in:** BRD-101 and BRD-118 to BRD-125, the Process run and End-to-end run screens
- **Workaround:** Hand-built rows with a badge per row, using the `.steps` and `.step` classes.
- **Suggested fix:** `StepperItem Status="Done|Running|Waiting|Failed|Pending"`, `Orientation="Vertical"`, and a template for trailing content.

### TR-005 — There is no small switch for a table cell

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `Switch Size="SwitchSize.Small"` inside a `DataTableColumn` `CellTemplate` was already there — it is in the reference this file cited. The features on Licence (BRD-144), the actions on Integrations (BRD-152) and the rights on the Agents page use it.
- **Severity:** minor
- **Blocks:** no — the Licence and Integrations mockups use a hand-reset button with the `.toggle` classes
- **Repro:** `Switch` in section 5 of the 2.0.7 reference is a form control sized and laid out for a form row, with its label beside it.
- **Expected:** A small, label-less switch that sits in a table cell as the row's on and off, at the height of the row.
- **Actual:** Dropped into a cell, the form-sized switch makes the row taller than every other row, and its label slot is wasted.
- **Encountered in:** BRD-144 and BRD-152, the features list and the published actions
- **Workaround:** The `.toggle` classes on a button with the browser's own styling reset.
- **Suggested fix:** A `Size` of `Small` on `Switch` with no label slot, or say in the catalogue which control belongs in a table cell.

### TR-006 — There is nothing to show that an answer is still arriving

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `Typing` in 2.0.9 sits inside a message while its text grows, and `Progress Indeterminate` covers the bar case. The reply still arriving (BRD-47) uses it.
- **Severity:** minor
- **Blocks:** no — the Chat mockup draws three dots by hand, and the work carried on
- **Repro:** In the 2.0.7 reference, `Skeleton` is a still placeholder for content that has not loaded and `Progress` needs a number. Neither says "this is being written now".
- **Expected:** A small indicator for work that is under way with no known end — three moving dots, or a bar that does not claim a percentage — that can sit inside a message while its text grows.
- **Actual:** `Spinner` is the nearest, and it reads as "the screen is busy", not as "the answer is still coming".
- **Encountered in:** BRD-47, the Chat screen of Chatur phase 1
- **Workaround:** Three inline dots in the muted colour, inside the message being written.
- **Suggested fix:** Add `Typing` (or an `Indeterminate` mode on `Progress`) that can be placed inline.

### TR-007 — There is no control for the output of a command

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `LogView` in 2.0.9 takes lines as they arrive, marks each ordinary, success, warning or failure, holds its height and follows the newest line until the reader scrolls up. The output strip (BRD-23) and Process run (BRD-101) use it.
- **Severity:** major
- **Blocks:** no — Run and Process run use a hand-built panel, and the work carried on
- **Repro:** The 2.0.7 reference has `CodeBlock` for a block of code and `Prose` for rendered HTML. Neither takes lines as they arrive, colours a line by what it is, or holds a height and follows the newest line.
- **Expected:** A panel for the output of a running command: lines added one at a time, each able to read as ordinary, a warning or a failure, a height it keeps, and a follow-the-end setting the reader can turn off while scrolling back.
- **Actual:** `CodeBlock` takes one finished string with no per-line hook, so a build's output has to be assembled and coloured by the application and re-rendered on every line.
- **Encountered in:** BRD-23 and BRD-101, the Run and Process run screens
- **Workaround:** A hand-built panel in a monospace font with a class per line, in `chatur.css`.
- **Suggested fix:** Add `LogView` taking lines with a level each, plus `MaxHeight` and `Follow`.

### TR-008 — DataTable cannot let the user choose rows

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `DataTable SelectionMode="DataTableSelectionMode.Multiple"` with `@bind-SelectedItems` gives the choosing column, the choose-all and the count. Choosing which files to check in (BRD-87) uses it.
- **Severity:** major
- **Blocks:** no — the Source control mockup puts a plain checkbox in the first cell, and the work carried on
- **Repro:** `DataTable` in section 6 of the 2.0.7 reference renders rows from a list. It documents `@bind-SelectedItems` in the binding table, but no column, no header control and no way to turn choosing on.
- **Expected:** A choosing column: a box on each row, a choose-all in the header that also shows a part-chosen state, the chosen rows bound two ways, and a count the page can read to label its own button.
- **Actual:** The box, its label for a screen reader, the choose-all, the count and the enabling of the submit button are all left to the application, so every table that needs choosing writes them again.
- **Encountered in:** BRD-87, checking in the chosen files on Source control
- **Workaround:** A plain checkbox in the first cell, with the count written by hand.
- **Suggested fix:** `SelectionMode` on `DataTable` with a real choosing column, plus a documented `SelectedCount`.

### TR-009 — There is no list whose order the user sets

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `SortableList` was already there, with real move-up and move-down buttons rather than dragging. The three fallback chains on Routing (BRD-40) use it.
- **Severity:** minor
- **Blocks:** no — the routing tab builds its chain from cards and buttons, and the work carried on
- **Repro:** The 2.0.7 reference has `SortableList` in section 8, which reorders by dragging. There is no list that also offers move-up and move-down buttons, or that owns its items' positions.
- **Expected:** An ordered list where each row carries its position, a way up, a way down and a way out, and the order is bound two ways — dragging is not enough on its own, because a chain of three models is quicker to fix with two buttons.
- **Actual:** The position number, both buttons, the remove and the reordering are all written by the application, so every screen with a priority order writes them again.
- **Encountered in:** BRD-40, the fallback chain on the routing tab
- **Workaround:** A card per tier holding rows of badge and buttons.
- **Suggested fix:** `OrderedList<TItem>` with `@bind-Items`, buttons as well as dragging, and a position shown per row.

### TR-010 — A list cannot drive a detail pane

- **Closed 2026-09-22** — re-checked against TrBlazeUI 2.0.9 on GitHub Packages. `NavList<TItem>` in 2.0.9 is a list of multi-line rows that drives a detail pane, with an item template and a bound selection. The agents list beside its editor (BRD-53 and BRD-56) uses it, and the same control in a grid answers the theme cards on Appearance (BRD-156).
- **Severity:** major
- **Blocks:** no — the roles tab hand-builds its left-hand list, and the work carried on
- **Repro:** `DataTable` renders rows but they cannot act as navigation that chooses what the rest of the screen shows; `Tabs` carries a label per item and no more.
- **Expected:** A list where each row holds several lines — a name, some badges, a couple of values — one row is chosen, the choice is bound two ways, and the arrow keys move between rows.
- **Actual:** The rows are anchors laid out by hand, so the chosen state, the keyboard and the layout are the application's to get right every time.
- **Encountered in:** BRD-53 and BRD-56, the roles tab, which is a list beside the role being edited
- **Workaround:** Hand-built anchors with an inline layout and a chosen class.
- **Suggested fix:** A `ListDetail` or a `NavList<TItem>` with an item template and `@bind-Selected`. This is the commonest shape in the whole product and it has no control.

### TR-011 — DataTable rows, its header row and its choose-all cell cannot carry an attribute

> ✅ **Closed 2026-10-02** — re-checked here: Upgraded to TrBlazeUI 2.1.1 (GitHub Packages). Moved changes-head, history-head and process-branches-head to DataTable HeaderRowAttributes, choose-all to SelectAllAttributes, provider-{name} and agent-tier-{role} to RowAttributes; removed the tier-cell wrapper. Measured on the running app 2026-10-02: provider-opencode-go and agent-tier-analyst are <tr> rows; changes-head, history-head and process-branches-head are <tr> in <thead>; choose-all is the control itself (role=checkbox, one-page grid). All 96 acceptance tests pass.

- **Status:** open, filed 2026-09-30
- **Severity:** minor
- **Blocks:** no — Chatur puts the test hook on a cell's own content instead, and the work carried on. Only the verifier's check that every mockup control is on the page cannot find these four hooks.
- **Repro:** TrBlazeUI 2.0.9 reference §6 "DataTable" and the "Every public component accepts arbitrary HTML attributes" note: attributes passed to `DataTable` land on the table, `DataTableColumn` renders none, and there is no parameter for a row, the `<thead>` row, or the choose-all cell that `SelectionMode="Multiple"` draws.
- **Expected:** A way to name those parts, e.g. `RowAttributes="Func<TData, IReadOnlyDictionary<string, object>>"` (so each row can carry `data-testid="change-row-{path}"`), `HeaderRowAttributes`, and a `SelectAllAttributes` (or a fixed, documented `data-testid` on the choose-all box).
- **Actual:** Chatur's mockups anchor `changes-head`, `choose-all`, `history-head` and `process-branches-head` on the Repository screen, and `provider-{name}` / `agent-tier-{role}` on a row; none can be put where the mockup puts it.
- **Encountered in:** REQ-UI-042, REQ-UI-043, REQ-UI-028 and the Repository history and process-branch cards (BRD-86 to BRD-90)
- **Workaround:** the hook sits on a wrapper inside a cell (`agent-tier-{role}` on the tier cell); the header and choose-all hooks are not placed.
- **Suggested fix:** `RowAttributes`, `HeaderRowAttributes` and `SelectAllAttributes` parameters on `DataTable`.

### TR-012 — ToggleGroup's chosen item cannot take another colour

> ✅ **Closed 2026-10-02** — re-checked here: Set OnVariant=ToggleOnVariant.Card on the Workbench mode toggle and the Sign in remember toggle, both on a muted track (bg-muted). Measured 2026-10-02 on TrBlazeUI 2.1.1: chosen item background oklch(0.222 0.012 65) equals --card exactly on both; the track is oklch(0.262 0.015 65).

- **Status:** open, filed 2026-10-01
- **Severity:** minor
- **Blocks:** no — the mode toggle works; only its chosen colour differs from the mockup.
- **Repro:** TrBlazeUI 2.0.9 `ToggleGroup`: the chosen item is fixed to `bg-accent`, and `data-[state=on]:bg-card` (or any on-state class) is not in the shipped CSS.
- **Expected:** a parameter for the chosen item's look, e.g. `OnVariant` (`Accent`, `Card`, `Primary`), or the on-state classes shipped.
- **Actual:** with a theme whose `--accent` is a soft tint, the chosen "Ask first" reads as a tinted pill where the mockup draws a plain card segment.
- **Encountered in:** REQ-UI-024 (Workbench mode toggle), and the Sign in "remember this device" toggle
- **Workaround:** none; the colour stays the library's.
- **Suggested fix:** an `OnVariant` parameter on `ToggleGroup`.

### TR-013 — ToggleGroup's DisposeAsync lets a "task was canceled" error escape

> ✅ **Closed 2026-10-02** — re-checked here: Full verification on TrBlazeUI 2.1.1 (96 acceptance tests, every screen, live model) on 2026-10-02, then searched the server log app-5280.log: 0 'A task was canceled' rendering errors, 0 mentions of ToggleGroup, and none for any other control.

- **Status:** open, filed 2026-10-01
- **Severity:** minor
- **Blocks:** no — seen when a page closes or reloads, when the connection is ending anyway; no lost work traced to it.
- **Repro:** TrBlazeUI 2.0.9, Blazor Server: close or reload a page holding a `ToggleGroup`. The log shows "Unhandled exception rendering component: A task was canceled" at `ToggleGroup\`1.DisposeAsync()` → `JSObjectReference.DisposeAsync()`, then "Unhandled exception in circuit". 18 times in one verification run of Chatur.
- **Expected:** a component's `DisposeAsync` swallows `JSDisconnectedException` and `TaskCanceledException` from its own JS module dispose, as the Blazor guidance asks.
- **Actual:** the exception escapes and is reported as unhandled for the circuit.
- **Encountered in:** REQ-UI-024 (Workbench mode toggle), the Sign in "remember this device" toggle
- **Workaround:** none.
- **Suggested fix:** wrap the module dispose in `try { … } catch (JSDisconnectedException) { } catch (TaskCanceledException) { }`.

### TR-014 — A switch that is off cannot be given a visible border

> ✅ **Closed 2026-10-03** — re-checked here: 2026-10-03: upgraded TrBlazeUI.Components and Icons.Lucide 2.1.1 -> 2.1.2 (newest on the GitHub Packages feed; no later release exists), set Outlined=true on the Rights switches in SettingsAgents.razor (no border class was present). tf-mockup-parity.sh --screen settings-agents=/settings/agents: PASS, 0 findings at 1280 and 390 (no border-width finding). The off switch measures 2px solid in --border on a muted track with a grey thumb.

- **Status:** fixed upstream 2026-10-03 (`Switch.Outlined`, unreleased — see the 2026-10-03 reply), filed 2026-10-02
- **Severity:** minor
- **Blocks:** no — the Rights switches on Settings ▸ Agents work and read clearly; only the off-state outline differs from the mockup.
- **Repro:** TrBlazeUI 2.1.1 `Switch`: the root carries `border-transparent`, and `border-input` / `border-border` come earlier in `trblazeui.css`, so a `Class="border border-input"` on the switch loses to it. Chatur has no Tailwind build, so a class the shipped CSS does not already order after `border-transparent` (or an `!` variant) cannot be added.
- **Expected:** an off switch draws a visible rule in `--input`, as the mockup's `.sw` does (a solid 1px `var(--line)` border when off, transparent when on), or `Switch` takes a parameter/variant for it, e.g. `Outlined`.
- **Actual:** the off switch has a 1px transparent border, so `tf-mockup-parity` reports "border style differs — mockup solid, app none" for `switch-changes-code`, `switch-runs-commands` and `switch-marks-verified` at both widths.
- **Encountered in:** REQ-UI-026 (Settings ▸ Agents, Rights panel)
- **Workaround:** none; a colour written into the component would break "a theme is a file of OKLCH values", and an inline style is not allowed.
- **Suggested fix:** draw the off-state border with `border-input` on the switch root (`data-[state=unchecked]:border-input`) or add a `Variant`.

### TR-015 — A tab in EditorTabs cannot carry its own attributes, such as a test id

> ✅ **Closed 2026-10-07** — re-checked here: TrBlazeUI 2.1.3, `TabAttributes` on the main window's EditorTabs gives `tab-processes`, `tab-board`, `tab-process-run`, `tab-agent-workspace` and `tab-<file name without extension>`. `tf-verify-screens.sh` on the four phase 2 screens at 1280 and 390: render OK, visual OK, no missing `tab-*` anchor. workbench-files.spec.ts and all six phase 2 specs pass (48 of 48).

- **Status:** fixed upstream 2026-10-07 (`EditorTabs.TabAttributes`, in 2.1.3 — see the 2026-10-07 reply), filed 2026-10-06
- **Severity:** minor
- **Blocks:** yes — the screens work and tests find a tab by its label, but the verifier's screen check needs the mockups' `tab-*` ids on the page, so all 22 phase 2 rows (REQ-FN-049..060, REQ-UI-045..054) cannot reach Verified until a tab can carry its id.
- **Repro:** TrBlazeUI 2.1.2 `EditorTabs Items="@objTabs"`: each item renders one tab, and there is no parameter that adds attributes to a tab's element.
- **Expected:** a way to give each tab its own attributes from its item, like `DataTable.RowAttributes`, so a tab can carry `data-testid="tab-processes"` as Chatur's mockups draw.
- **Actual:** the mockups' `tab-processes`, `tab-board`, `tab-process-run` and `tab-agent-workspace` anchors cannot exist on the page, so the screen check lists them as missing.
- **Encountered in:** REQ-UI-045, REQ-UI-047, REQ-UI-049, REQ-UI-053 (phase 2 screens as tabs in the main window)
- **Workaround:** none; tests locate a tab by its visible label.
- **Suggested fix:** add `TabAttributes` (`Func<TItem, IReadOnlyDictionary<string, object>?>`) to `EditorTabs`, applied to each tab's root element.

### TR-016 — A step in Stepper cannot show an icon in its marker

> ✅ **Closed 2026-10-07** — re-checked here: TrBlazeUI 2.1.3, each Process run step uses `StepperItem.Icon` (check, spinning loader-circle, pause, x, clock). Screenshots run-stopped-390.png and run-verdicts-1280.png show the vector marks in the circles; `tf-mockup-parity.sh` no longer reports "mockup carries an icon here" for `run-step-1` to `run-step-7`.

- **Status:** fixed upstream 2026-10-07 (`StepperItem.Icon`, in 2.1.3 — see the 2026-10-07 reply), filed 2026-10-06
- **Severity:** minor
- **Blocks:** no — Process run reads correctly: each step's marker is a filled circle with a tick, a cross or a number, named for a screen reader, and the state pill beside it says the same in words.
- **Repro:** TrBlazeUI 2.1.2 `StepperItem Status="StepStatus.Done"`: the marker (`data-slot="stepper-item-marker"`) is a text glyph (`✓`, `✕`, a number). There is no slot or parameter that puts a `LucideIcon` in it.
- **Expected:** an `Icon` slot on `StepperItem` that replaces the glyph, so a step can carry the same vector tick, spinner or pause the mockup draws at the start of each row (`mockups/process-run.html`, `run-step-*`).
- **Actual:** `tf-mockup-parity.sh` reports "mockup carries an icon here; the app does not" for `run-step-1` to `run-step-7` at both widths.
- **Encountered in:** REQ-FN-052, REQ-UI-047 (Process run, the steps panel)
- **Workaround:** none; an icon placed beside the marker would draw two marks for one state.
- **Suggested fix:** add `Icon` (`RenderFragment?`) to `StepperItem`, drawn inside the marker in place of the glyph when given, with the marker's accessible name unchanged.

### TR-017 — Badge has no soft red variant, and a text colour in Class cannot override a variant's

> ✅ **Closed 2026-10-07** — re-checked here: TrBlazeUI 2.1.3, the stopped state and a failed step use `BadgeVariant.Danger` (soft red pill in run-stopped-390.png); the Board's Blocked count is `Class="text-destructive"` on a Secondary badge with the inner span removed, and reads red. Note: Chatur's theme tint tokens (`--alert-*-bg` and `-foreground`) stay. With them removed, `tf-mockup-parity.sh` reports Success and Warning pills as neutral (count-verified, right-*, run-step-*), because the library's own tints are paler than the mockup's 16 to 18 percent hue.

- **Status:** fixed upstream 2026-10-07 (`BadgeVariant.Danger`, and a `Class` text colour now replaces the variant's, in 2.1.3 — see the 2026-10-07 reply), filed 2026-10-06
- **Severity:** minor
- **Blocks:** no — a failed or stopped state shows as `Destructive`, a solid red pill, and reads clearly; only its weight differs from the mockup's tinted pill.
- **Repro:** TrBlazeUI 2.1.2 `Badge`: `Success`, `Info` and `Warning` paint a tinted surface with text in the hue (they mirror `AlertVariant`), but there is no matching `Danger`; the only red is `Destructive`, a solid fill. `Class="text-destructive"` on a `Secondary` badge renders both `text-secondary-foreground` and `text-destructive` and the first wins, so a coloured label cannot be had that way either.
- **Expected:** `BadgeVariant.Danger` painting `--alert-danger-bg` with `--alert-danger-foreground`, the same as the other three, so the status vocabulary is complete (the mockups' `.pill.bad`).
- **Actual:** the stopped state on Process run uses `Destructive`; the Blocked count on Board wraps its label in a coloured `<span>` inside a `Secondary` badge.
- **Encountered in:** REQ-UI-047 (Process run state pill), REQ-UI-049 (Board status counts)
- **Workaround:** none for the pill; the Blocked count's label colour is set on an inner `<span>`, which is ordinary markup, not a replacement for a control.
- **Suggested fix:** add `Danger` to `BadgeVariant` with `border-alert-danger/30 bg-alert-danger-bg text-alert-danger-foreground`, and let `cn()` treat `text-*` colour classes as one group so a `Class` colour replaces the variant's.

### TR-018 — A tab's close button draws an svg icon where the mockup draws a × glyph

> ✅ **Closed 2026-10-07** — re-checked here: 2026-10-07: upgraded TrBlazeUI.Components and Icons.Lucide 2.1.3 -> 2.1.4 and set <CloseContent>×</CloseContent> on the main window's EditorTabs (EditorArea.razor). tf-mockup-parity.sh on Processes, Process run, Requirements board and Agent workspace: PASS, 0 findings at 1280 and 390 (no tab-* icon finding). workbench-files.spec.ts 9/9 pass.

- **Status:** fixed upstream 2026-10-07 (`EditorTabs.CloseContent`, in 2.1.4 — see the later 2026-10-07 reply), filed 2026-10-07
- **Severity:** minor
- **Blocks:** yes — the close button works and is named for a screen reader, but the mockup comparison fails on every tab, so none of the 22 phase 2 rows (REQ-FN-049..060, REQ-UI-045..054) can reach Verified while the tab's close mark differs from the mockups' ×.
- **Repro:** TrBlazeUI 2.1.3 `EditorTabs` on the main window; `tf-mockup-parity.sh` for Processes, Process run, Board and Agent workspace.
- **Expected:** the tab matches the mockups' `tab-*` anchors, which end in a text × (`Processes×`).
- **Actual:** parity reports "app carries an icon the mockup does not" for `tab-processes`, `tab-process-run`, `tab-board` and `tab-agent-workspace` at 1280, because the close button holds an svg. This appeared only once TR-015 let the tabs carry their ids.
- **Encountered in:** REQ-UI-045, REQ-UI-047, REQ-UI-049, REQ-UI-053
- **Workaround:** none; the close button is the library's own. The mockups could instead draw the svg.
- **Suggested fix:** add `EditorTabs.CloseContent` (`RenderFragment?`, default the current svg) so an app can draw the close mark as its mockup does, e.g. a text `×`.

### TR-019 — A DataTable column cannot be hidden below a screen width

> ✅ **Closed 2026-10-11** — re-checked here: 2026-10-11: upgraded to TrBlazeUI 2.1.6; DataTableColumn.HideBelow and DataTableBreakpoint are in TrBlazeUI.Components.dll/.xml and sm:table-cell in the shipped CSS. Run queue Started/Time use HideBelow=Sm; tests/verify/run-queue.spec.ts (4/4 pass) shows them at 1280, hides them at 390 with Pause/Resume/Remove in view and no sideways scroll.

> ⏳ **Re-checked 2026-10-09, not yet released** — TrBlazeUI 2.1.5 (the release after 2.1.4 on the feed) carries no `HideBelow`: neither its reference nor `TrBlazeUI.Components.dll` contains the name. Chatur stays on 2.1.4 and re-checks on the next release.

- **Status:** fixed upstream 2026-10-09 (`DataTableColumn.HideBelow`, in the release after 2.1.4 — see the 2026-10-09 reply), filed 2026-10-07
- **Severity:** minor
- **Blocks:** no — the Run queue reads fully at 1280 and at 390 it scrolls sideways inside its card, as the mockup's table does; but a phone-width reader must scroll to reach Pause, Resume and Remove, which a hidden Started/Time pair would keep in view.
- **Repro:** TrBlazeUI 2.1.4 `DataTable` with seven columns (Run queue, `/auto/queue`) in a tab about 430 px wide, and on a 390 px screen. `DataTableColumn.Visible` is a fixed initial value and `CellClass`/`HeaderClass` accept `hidden`, but no `sm:table-cell` (or any `table-cell` variant) is in the shipped `trblazeui.css`, so a column hidden by `hidden` cannot come back at a wider screen.
- **Expected:** a column parameter such as `HideBelow="DataTableBreakpoint.Sm"`, or the `table-cell` display utility with its responsive variants in the bundle.
- **Actual:** the only way to drop a column at 390 is to drop it at every width.
- **Encountered in:** REQ-UI-057
- **Workaround:** none; the table keeps every column and scrolls inside its card at 390 (`MinWidth="420px"`).
- **Suggested fix:** ship `table-cell` plus `sm:`/`md:` variants, or add a responsive visibility parameter on `DataTableColumn`.

### TR-020 — Menubar has no submenu

> ✅ **Fixed upstream, in 2.1.9** — `MenubarSub`, `MenubarSubTrigger`, `MenubarSubContent`, and the same for `DropdownMenu` and `ContextMenu`. Tests `tests/verify/req-ui-050.spec.ts`.

- **Status:** fixed upstream 2026-10-13 (`MenubarSub`, `MenubarSubTrigger`, `MenubarSubContent`, and the same for `DropdownMenu` and `ContextMenu`, in 2.1.9 — see the 2026-10-13 reply), filed 2026-10-13
- **Severity:** minor
- **Blocks:** no — the items work; only the way they are grouped differs from the mockup.
- **Repro:** TrBlazeUI 2.1.6 `Menubar`: the parts are `MenubarMenu`, `MenubarTrigger`, `MenubarContent`, `MenubarItem`, `MenubarCheckboxItem`, `MenubarLabel`, `MenubarSeparator` and `MenubarShortcut`. There is no `MenubarSub`, `MenubarSubTrigger` or `MenubarSubContent`. Checked again on 2026-10-13 against the library's own source (`src/TrBlazeUI.Components/Components/Menubar`, `DropdownMenu`, `ContextMenu`): none of the three menus has a sub part. The only nesting in the library and its demo is the Sidebar (`SidebarMenuSub`, `SidebarCollapsibleMenuItem`), which is side navigation, not a list that opens from a menu bar.
- **Expected:** a submenu part, as `DropdownMenu` sits beside its sub-parts in shadcn/ui, so File ▸ Recent projects ▸ and View ▸ Zoom ▸ open a second list beside the first, as `mockups/main.html` draws them (a `▸` at the right of the row).
- **Actual:** no way to nest a list inside a menu item.
- **Encountered in:** REQ-UI-073
- **Workaround:** Recent projects and Zoom are a labelled group of items inside the menu (`MenubarLabel` then the items), so every item is still there and does its job; the `▸` is not drawn.
- **Suggested fix:** add `MenubarSub`, `MenubarSubTrigger` and `MenubarSubContent`.

### TR-021 — A disabled MenubarItem is not announced as disabled

> ✅ **Fixed upstream, in 2.1.9** — a disabled `MenubarItem`, `MenubarCheckboxItem` and `ContextMenuItem` renders `aria-disabled="true"` and `data-disabled`. Tests `tests/verify/req-ui-050.spec.ts`.

- **Status:** fixed upstream 2026-10-13 (`aria-disabled="true"` and `data-disabled` on a disabled `MenubarItem`, `MenubarCheckboxItem` and `ContextMenuItem`, in 2.1.9 — see the 2026-10-13 reply), filed 2026-10-13
- **Severity:** minor
- **Blocks:** no — a disabled item looks dim and takes no click.
- **Repro:** `<MenubarItem Disabled="true">Save</MenubarItem>` renders `<div role="menuitem" class="… pointer-events-none opacity-50" tabindex="-1">`: no `aria-disabled`, no `data-disabled`.
- **Expected:** `aria-disabled="true"` (and `data-disabled`), so a screen reader says the item is unavailable and a test can tell it from an enabled one without reading a class name.
- **Actual:** the only sign is the `pointer-events-none` class; Playwright's `toBeDisabled()` reports such an item as enabled.
- **Encountered in:** REQ-UI-073 (File ▸ Save, Edit ▸ Redo)
- **Workaround:** the verify spec reads the `pointer-events-none` class.
- **Suggested fix:** set `aria-disabled` on a disabled `MenubarItem` and `MenubarCheckboxItem`.

### TR-022 — A focused field's ring stands off its border and runs into the label above; menu items are tighter than the design

- **Status:** open, filed 2026-10-30
- **Severity:** minor
- **Blocks:** no — Chatur overrides it in one shared stylesheet (`src/ChaturUI/wwwroot/chatur.css`) until a fix ships.
- **Repro:** TrBlazeUI 2.1.6 `<Label>Project name</Label><Input />` inside a `Dialog`, then focus the input. `Input` carries `focus-visible:ring-2 focus-visible:ring-offset-2`, so a 4px band is drawn outside the border: it touches the label above and comes 4px closer to the dialog edge (the owner's screenshot `docs/screenshots/2026-10-29-first-use/New-Project-Incorrect-Paddings.png`). `DropdownMenuItem` / `MenubarItem` are `px-2 py-1.5` (8px across) and their content `p-1`, against shadcn-style designs that use 10px and 5px.
- **Expected:** the ring sits on the border (`ring-offset-0`, or an inset ring), and a `Label` placed before a field keeps a gap of its own; menu items with 10px across.
- **Actual:** the focused field looks as if it has no room around it, which the owner reported as missing padding on every dialog.
- **Encountered in:** REQ-UI-080
- **Workaround:** `chatur.css` sets `--tw-ring-offset-width: 0` on focused fields, a 6px gap after a label, and 5px / 6px 10px on menus and lists.
- **Suggested fix:** drop `ring-offset-2` from `Input`, `Textarea` and `SelectTrigger` (or make it a parameter), and widen menu items to `px-2.5`.

## Replies from TrBlazeUI

<!-- The upstream team's answers, newest block first. Left in full: this is the record. -->

### 2026-10-13 — TR-020 and TR-021 are fixed; they ship in the release after 2.1.6

Both are fixed and tested in the library on the day they were filed. They ship in the next release
after 2.1.6 on GitHub Packages (`https://nuget.pkg.github.com/techierathore/index.json`). The
release number is the owner's to set when the release is cut, so this reply does not name one.
When this reply was written, the fixes were built and verified but the release had not been cut
yet. If the feed still stops at 2.1.6, wait; keep the labelled groups and the class check until
then.

| Entry | State | What you get |
|---|---|---|
| **TR-020** Menubar has no submenu | fixed, for all three menus | `MenubarSub` holds a `MenubarSubTrigger` (the row, with a ▸ at its right edge) and a `MenubarSubContent` (the list that opens beside it). A `MenubarSub` goes inside a `MenubarContent` or inside another `MenubarSubContent`, so submenus nest to any depth. `DropdownMenuSub`/`…SubTrigger`/`…SubContent` and `ContextMenuSub`/`…SubTrigger`/`…SubContent` work the same way. |
| **TR-021** a disabled MenubarItem is not announced | fixed | A disabled `MenubarItem` or `MenubarCheckboxItem` now renders `aria-disabled="true"` and `data-disabled`, so a screen reader says it is unavailable and Playwright's `toBeDisabled()` passes. `ContextMenuItem` had the same gap and is fixed too; `DropdownMenuItem` already rendered both. |

```razor
<Menubar>
    <MenubarMenu>
        <MenubarTrigger>File</MenubarTrigger>
        <MenubarContent>
            <MenubarItem OnClick="NewProject">New project</MenubarItem>
            <MenubarSub>
                <MenubarSubTrigger data-testid="menu-recent">Recent projects</MenubarSubTrigger>
                <MenubarSubContent data-testid="menu-recent-list">
                    @foreach (var vProject in objRecent)
                    {
                        <MenubarItem OnClick="@(() => OpenProject(vProject))">@vProject.Name</MenubarItem>
                    }
                </MenubarSubContent>
            </MenubarSub>
            <MenubarSeparator />
            <MenubarItem Disabled="@(!objHasChanges)">Save<MenubarShortcut>Ctrl+S</MenubarShortcut></MenubarItem>
        </MenubarContent>
    </MenubarMenu>
    <MenubarMenu>
        <MenubarTrigger>View</MenubarTrigger>
        <MenubarContent>
            <MenubarSub>
                <MenubarSubTrigger>Zoom</MenubarSubTrigger>
                <MenubarSubContent>
                    <MenubarItem OnClick="ZoomIn">Zoom in</MenubarItem>
                    <MenubarItem OnClick="ZoomOut">Zoom out</MenubarItem>
                </MenubarSubContent>
            </MenubarSub>
        </MenubarContent>
    </MenubarMenu>
</Menubar>
```

```ts
// TR-021: the class check can go.
await expect(page.getByRole('menuitem', { name: 'Save' })).toBeDisabled();
```

Things to know before you use them:

1. **Escape closes one level, not the whole menu.** In a submenu, ArrowLeft or Escape closes that
   list and puts the focus back on its row; a second Escape closes the menu. ArrowRight or Enter on
   a ▸ row opens its list and focuses the first enabled item. ArrowRight on an ordinary row still
   moves to the next menu of the bar. Choosing an item at any depth runs it and closes the whole
   menu.
2. **Hover opens a submenu; pointing at a sibling closes it.** Moving the mouse off the list does
   not close it, so the diagonal path from the row to the list is safe. A click on the row opens it
   too, which is what a touch screen gets.
3. **At phone width the list may open on the left.** It opens on the right and flips to the left
   when the window has no room. At 390 px, File ▸ Recent projects ▸ More ▸ puts the third list on
   the left, over part of the File list. If `mockups/main.html` draws something else at 390, tell
   us what.
4. **The ▸ is text, at the right of the row.** It is an `aria-hidden` `<span>`, so the row's
   accessible name stays "Recent projects". `Chevron` replaces it, for example with
   `<LucideIcon Name="chevron-right" Size="14" />`, if your mockup draws an icon.
5. **The three menu panels no longer clip.** `MenubarContent`, `DropdownMenuContent` and
   `ContextMenuContent` dropped `overflow-hidden` so a submenu can sit outside them. A long menu
   that must scroll needs `Class="max-h-80 overflow-y-auto"`, and then it cannot hold a submenu.
6. **Test hooks go on the parts.** `data-testid` on `MenubarSubTrigger` lands on the row
   (`role="menuitem"`, `aria-haspopup="menu"`, `aria-expanded`); on `MenubarSubContent` it lands on
   the list (`role="menu"`), which is in the page only while it is open.
7. **Your agent's reference describes both.** After the upgrade, the build refreshes
   `.trblazeui/TrBlazeUI-AI-Reference.md`: "Which control do I use for…" has a row for each, the
   Menubar section has the submenu parts and their parameters, and "Which version added what" has an
   "After 2.1.6" row. `CHANGELOG.md` has the full entry.

#### What we need from you

Upgrade once the release after 2.1.6 is on the feed. Check that
`.trblazeui/TrBlazeUI-AI-Reference.md` names `MenubarSubTrigger`; if it does not, the upgrade has
not landed yet.

- **TR-020:** in the main window's menu bar (REQ-UI-073), replace the labelled Recent projects and
  Zoom groups with `MenubarSub`, as in the sample above. Run `tf-mockup-parity` on the main window
  at 1280 and 390 with File ▸ Recent projects and View ▸ Zoom open, and check from the keyboard
  that ArrowRight opens each list and ArrowLeft and Escape close it. Then close the entry:
  `bash .tfcore/utils/tf-feedback.sh Chatur --close TR-020 "<what you ran and what it showed>"`.
- **TR-021:** in the REQ-UI-073 verify spec, replace the `pointer-events-none` class check on
  File ▸ Save and Edit ▸ Redo with `toBeDisabled()`, and check that an enabled item still passes
  `toBeEnabled()`. Then close it:
  `bash .tfcore/utils/tf-feedback.sh Chatur --close TR-021 "<what you ran and what it showed>"`.

Tell us what does not fit.

---

### 2026-10-09 — TR-019 is fixed; it ships in the release after 2.1.4

Fixed and tested in the library. It ships in the next release after 2.1.4 (**2.1.5** by the usual
count) on GitHub Packages (`https://nuget.pkg.github.com/techierathore/index.json`). When this reply
was written, the fix was built and verified but the release had not been cut yet. If the feed still
stops at 2.1.4, wait; do not drop the Started and Time columns at every width.

| Entry | State | What you get |
|---|---|---|
| **TR-019** a DataTable column cannot be hidden below a screen width | fixed | `DataTableColumn` gains `HideBelow`, a `DataTableBreakpoint?`: `Sm` (640 px), `Md` (768 px), `Lg` (1024 px) or `Xl` (1280 px). Below that width the column's header cell and every body cell are hidden; at or above it they show again. Left out, the column shows at every width, so nothing you have changes until you opt in. |

```razor
<DataTableColumn TData="QueueRun" TValue="DateTime" Property="@(r => r.Started)"
                 Header="Started" HideBelow="DataTableBreakpoint.Sm" />
<DataTableColumn TData="QueueRun" TValue="string" Property="@(r => r.Elapsed)"
                 Header="Time" HideBelow="DataTableBreakpoint.Sm" />
```

Things to know before you use it:

1. **The breakpoint is the screen width, not the tab's.** It is a CSS media query. At 390 px a
   `Sm` column is gone; in a 430 px tab on a 1280 px screen it still shows, because the screen is
   wide. If the Run queue must also drop columns inside a narrow tab on a wide screen, tell us and we
   will look at a container-width version.
2. **Use the parameter, not `hidden` in `CellClass`.** `HideBelow` puts the same classes on the
   header cell and on the body cells. The shipped `trblazeui.css` now also carries
   `sm:`/`md:`/`lg:`/`xl:table-cell`, which it never did before.
3. **The table still owns the column.** Sorting, the search filter and the column chooser behave as
   before; `Visible="false"` still hides a column at every width.
4. **Your agent's reference describes it.** After the upgrade, the build refreshes
   `.trblazeui/TrBlazeUI-AI-Reference.md`: "Which control do I use for…" has a row for this, and the
   `DataTableColumn` table has the parameter. `CHANGELOG.md` has the full entry.

#### What we need from you

Upgrade once the release is on the feed, set `HideBelow="DataTableBreakpoint.Sm"` on the Run queue's
Started and Time columns, and check `/auto/queue` at 390 (Pause, Resume and Remove in view without
scrolling sideways) and at 1280 (all seven columns). Then close this entry here
(`bash .tfcore/utils/tf-feedback.sh Chatur --close TR-019 "<what you ran and what it showed>"`) or
tell us what does not fit.

---

### 2026-10-07, later the same day — TR-018 is fixed; it ships in 2.1.4

Fixed and tested in the library on the day it was filed. It ships in **2.1.4** on GitHub Packages
(`https://nuget.pkg.github.com/techierathore/index.json`). When this reply was written, the fix was
built and verified but the release had not been cut yet. If the feed still stops at 2.1.3, wait for
2.1.4; do not change the mockups to draw the svg.

| Entry | State | What you get |
|---|---|---|
| **TR-018** a tab's close button draws an svg where the mockup draws × | fixed | `EditorTabs` gains `CloseContent`, a `RenderFragment`. What you put in it is drawn inside every tab's close button in place of the svg. Left out, the svg is drawn as before, so nothing you have changes until you opt in. |

```razor
<EditorTabs Items="@objTabs" @bind-ActiveId="objActiveTab"
            TabAttributes="@(t => new Dictionary<string, object> { ["data-testid"] = $"tab-{t.Id}" })">
    <CloseContent>×</CloseContent>
</EditorTabs>
```

Things to know before you use it:

1. **Only the button's content changes.** The close button keeps `data-slot="editor-tab-close"`, its
   accessible name `aria-label="Close <label>"`, its hover and focus classes and its click. So put a
   plain text `×` in `CloseContent`, as the mockup's `.tab .x` holds; do not give it a label of its
   own and do not wrap it in a button. Your existing selector `[data-slot="editor-tab-close"]` still
   finds it.
2. **One mark for the whole strip.** `CloseContent` is drawn on every tab. There is no per-tab
   version, because every mockup tab draws the same ×; tell us if a tab needs a different one.
3. **The glyph takes the surrounding font size.** The svg was a fixed 12px; a text × is drawn at the
   font size the strip inherits, inside the same 20px button. If it reads too large or too small
   against the mockup, size it yourself: `<CloseContent><span class="text-xs">×</span></CloseContent>`.
4. **Your agent's reference describes it.** After the upgrade, the build refreshes
   `.trblazeui/TrBlazeUI-AI-Reference.md`: "Which version added what" has a 2.1.3 row for TR-015 to
   TR-017 and an "After 2.1.3" row for this, "Which control do I use for…" has a row for the close
   mark, and the `EditorTabs` table has the parameter. `CHANGELOG.md` has the full list.

#### What we need from you

Upgrade to 2.1.4 once it is on the feed, add `<CloseContent>×</CloseContent>` to the main window's
`EditorTabs`, and run your screen check and `tf-mockup-parity` again on Processes, Process run, Board
and Agent workspace at 1280 and 390. Then close this entry here
(`bash .tfcore/utils/tf-feedback.sh Chatur --close TR-018 "<what you ran and what it showed>"`) or
tell us what does not fit.

---

### 2026-10-07 — TR-015, TR-016 and TR-017 are fixed in 2.1.3

All three are fixed and tested in the library. They ship in **2.1.3** on GitHub Packages
(`https://nuget.pkg.github.com/techierathore/index.json`). When this reply was written, 2.1.3 was
built and checked but the release had not been cut yet. If the feed still stops at 2.1.2, wait for
2.1.3; do not work around TR-015.

| Entry | State | What you get |
|---|---|---|
| **TR-015** a tab cannot carry its own attributes | fixed | `EditorTabs` gains `TabAttributes`, a function of each tab's `EditorTabItem`. What it returns is put on that tab's `<li>`, the same way `DataTable.RowAttributes` works. |
| **TR-016** no icon in a step's marker | fixed | `StepperItem` gains `Icon`. It is drawn inside the marker in place of the glyph or number. |
| **TR-017** no soft red badge; a `Class` text colour loses | fixed, both halves | `BadgeVariant.Danger` paints `--alert-danger-bg` with `--alert-danger-foreground`, built the same way as `Success`, `Info` and `Warning`. A text colour in `Class` now replaces the variant's own. |

```razor
<EditorTabs Items="@objTabs" @bind-ActiveId="objActiveTab"
            TabAttributes="@(t => new Dictionary<string, object> { ["data-testid"] = $"tab-{t.Id}" })" />

<StepperItem Title="Build" Status="StepStatus.Running">
    <Icon><LucideIcon Name="loader-circle" Class="animate-spin" /></Icon>
</StepperItem>

<Badge Variant="BadgeVariant.Danger">Stopped</Badge>
<Badge Variant="BadgeVariant.Secondary" Class="text-destructive">Blocked 3</Badge>
```

Things to know before you use them:

1. **TR-015: the hook is on the `<li>`, which holds two buttons.** `tab-processes` is the whole tab,
   the label button and the close button together. A test that clicks the hook clicks the middle of
   the tab, which is the label. To be exact, click
   `[data-testid="tab-processes"] [data-slot="editor-tab-label"]`, or the close button with
   `[data-slot="editor-tab-close"]`. A `class` entry is added to the tab's classes, not swapped for
   them. Leave out `data-slot`, `data-tab-id`, `data-active` and `data-dirty`, because the strip
   owns them. The function runs on every render, so a changed id shows straight away.
2. **TR-016: the icon replaces the glyph and nothing else.** The marker keeps its circle, its status
   colour and its accessible name (`role="img"`, named "Done", "Running" and so on), so do not give
   the icon its own label. An `svg` inside the marker is sized to 1rem. Pick an icon that says the
   same thing as the `Status`; a spinner on a done step shows two answers for one state.
3. **TR-017: the `Class` fix is library-wide, and two controls look different because of it.** The
   class merger only treated one-word colours (`text-destructive`) as colours, so
   `text-secondary-foreground` was never replaced and the stylesheet order decided which colour
   won. It now treats `text-secondary-foreground`, `text-alert-danger-foreground` and
   `text-destructive/80` as colours too, so the later one wins. We compared every element's classes
   on all 125 demo pages before and after the change, and two things changed: an `Outline`
   `Toggle`/`ToggleGroupItem` now shows `--accent-foreground` text on hover instead of
   `--muted-foreground`, and the **selected row of a `NavList`** now shows `--accent-foreground`
   text instead of inheriting its parent's colour. Chatur uses `NavList` on the agents list and
   the Appearance theme cards, so look at the selected row there after upgrading. If your mockup
   draws that text in another colour, tell us.
4. **You can drop the Blocked count's inner `<span>`.** `Class="text-destructive"` on the
   `Secondary` badge now does what you wanted.
5. **Your agent's reference describes all of it.** After the upgrade, the build refreshes
   `.trblazeui/TrBlazeUI-AI-Reference.md` from the package. It has rows for 2.1.1, 2.1.2 and 2.1.3
   in "Which version added what", four new rows in "Which control do I use for…" (a tab's test
   hook, a step icon, the soft red pill, a label colour in `Class`), and the new parameters in the
   `EditorTabs`, `StepperItem` and `Badge` tables. `CHANGELOG.md` has the full list.

#### What we need from you

Upgrade to 2.1.3 once it is on the feed. Put the `tab-*` ids on through `TabAttributes`, put
`Icon` on the Process run steps, switch the stopped pill to `Danger`, and run your screen check
and `tf-mockup-parity` again on Process run, Board and the tabbed main window. Then close each
entry here or tell us what does not fit.

---

### 2026-10-03 — TR-014 is fixed in the library; not released yet

| Entry | State | What you get |
|---|---|---|
| **TR-014** an off switch cannot be given a visible border | fixed (unreleased; in the library's `[Unreleased]` changelog, ships in the next version after 2.1.0) | `Switch` gains `Outlined` (bool, default false). With it set, an off switch draws the mockup's `.sw` look: a `bg-muted` track (`--soft`), a solid border in `--border` (`--line`) and a `bg-muted-foreground` thumb (`--faint`). When on, it looks like any other switch and the border is transparent. A switch without `Outlined` is unchanged. |

Why it also changes the track and the thumb, not only the border: every Chatur theme sets
`--input` equal to `--border`, and the off track is `bg-input`, so a border in either colour on
that track would be invisible. The library's test now checks that the off border colour differs
from the track colour.

```razor
<Switch @bind-Checked="objCanChangeCode" Outlined="true" data-testid="switch-changes-code" />
```

Two things to know: the border is 2px (`border-2`, the switch's existing width, kept so its size
and thumb travel do not move), while the mockup's `.sw` draws 1px; if `tf-mockup-parity` compares
width as well as style it will report that. And drop `Class="border border-input"` from the three
Rights switches: it never took effect and is no longer needed.

### 2026-10-02 — TR-011, TR-012 and TR-013 are fixed in 2.1.0, which is out

All three are fixed in the library and tested there. **They are in 2.1.0, released 2026-10-02
on GitHub Packages** (`https://nuget.pkg.github.com/techierathore/index.json`), which is the
source Chatur uses. Upgrade to 2.1.0 and re-check them. (This reply was first written on
2026-10-01, before the release, and said the fixes were not published yet. They are now.)

| Entry | State | What you get |
|---|---|---|
| **TR-011** attributes on rows, the header row and the choose-all control | fixed | `DataTable` gains the three parameters you asked for: `RowAttributes` (a function of the row's item), `HeaderRowAttributes` and `SelectAllAttributes`. |
| **TR-012** the chosen item's colour | fixed | `ToggleGroup` gains `OnVariant`: `ToggleOnVariant.Accent` (the default, and the look it has today), `Card` or `Primary`. The classes behind `Card` and `Primary` are now in the shipped stylesheet. |
| **TR-013** "A task was canceled" from `ToggleGroup.DisposeAsync` | fixed | `ToggleGroup` now swallows `TaskCanceledException` as well as `JSDisconnectedException`, in `DisposeAsync` and around its first-render script import. |

#### TR-011 — how to use it

```razor
<DataTable TData="ChangedFile" Data="@objFiles"
           SelectionMode="DataTableSelectionMode.Multiple"
           RowAttributes="@(f => new Dictionary<string, object> { ["data-testid"] = $"change-row-{f.Path}" })"
           HeaderRowAttributes="ChangesHead"
           SelectAllAttributes="ChooseAll">
    …
</DataTable>

@code {
    private static readonly IReadOnlyDictionary<string, object> ChangesHead =
        new Dictionary<string, object> { ["data-testid"] = "changes-head" };

    private static readonly IReadOnlyDictionary<string, object> ChooseAll =
        new Dictionary<string, object> { ["data-testid"] = "choose-all" };
}
```

Four things worth knowing before you move your hooks:

1. **`SelectAllAttributes` lands on the control the user operates, and that control changes
   shape.** When every row fits on one page it is a checkbox (`role="checkbox"`). When the grid
   is paged it is a menu button holding a picture of a checkbox. Your `choose-all` hook finds
   the right one either way, but a test that expects `role="checkbox"` on it will only be right
   for a one-page grid.
2. **There is also a fixed hook that needs no parameter.** The choose-all control's wrapper
   always carries `data-slot="datatable-select-all"`. You asked for either the parameter or a
   fixed hook; you have both.
3. **A `class` entry is added to the part's own classes**, not swapped for them.
4. **The three are read when the grid paints.** The grid does not repaint while its `Data`
   reference is unchanged, so if what `RowAttributes` returns changes for rows already on
   screen, call `Refresh()` on the grid.

`DataTableColumn` still renders no element, so `provider-{name}` and `agent-tier-{role}` go on
the row through `RowAttributes`, not on a column.

#### TR-012 — how to use it

```razor
<ToggleGroup TValue="string" @bind-Value="objMode" Joined="true" AllowDeselect="false"
             OnVariant="ToggleOnVariant.Card" Class="bg-muted" AriaLabel="Mode">
    <ToggleGroupItem TValue="string" Value="@("ask")">Ask first</ToggleGroupItem>
    <ToggleGroupItem TValue="string" Value="@("auto")">Automatic</ToggleGroupItem>
</ToggleGroup>
```

`Card` paints the chosen item with `--card` and `--card-foreground`; `Primary` with `--primary`
and `--primary-foreground`. One thing to check against your mockup: **on a page whose background
is the same colour as its cards, a `Card` segment looks the same chosen or not.** The example
puts the group on `bg-muted` for that reason. If your mockup draws the track differently, tell
us what it draws.

`OnVariant` sits on the group and applies to all of its items. A class of your own such as
`data-[state=on]:bg-card` still does nothing — only the three looks are in the stylesheet.

#### TR-013 — what we found

We reproduced it. A `ToggleGroup` makes two calls to the browser when it is disposed. If the
page closes or reloads between them, the second call is never answered, the server gives up on
it after one minute, and that cancellation was escaping. Before the fix, one run here logged the
error 44 times; after it, none, with the page made to stop answering at exactly that point.

**2.1.0 fixes `ToggleGroup` only.** The same catch was missing from the dispose of 17 other
controls, among them `NavList`, `TreeView`, `ScrollArea`, `CodeEditor`, `Select` and the sidebar,
all of which Chatur uses. Those were fixed on 2026-10-02, after 2.1.0 was cut, and arrive with
the release after it. So on 2.1.0 your log may still show "A task was canceled" naming one of
those controls. That is known and already fixed in source; you do not need to file it. One case
cannot be fixed in this library: the framework's own `Virtualize`, which
`CommandVirtualizedGroup` renders, throws the same error from its own dispose.

#### What we need from you

Upgrade to 2.1.0 from GitHub Packages. Move the four hooks onto `HeaderRowAttributes`,
`SelectAllAttributes` and `RowAttributes`, set `OnVariant` on the mode toggle and the "remember
this device" toggle, run your verification again, and check the log for the error naming
`ToggleGroup`. Then close each entry here or tell us what does not fit.

---

### 2026-09-22 — all ten answered; six needed code, four did not

Thank you — this was a useful batch, and the four entries we are pushing back on are the
most useful part of it, because they point at our documentation rather than at our code.

**First, a numbering problem.** This batch starts again at TR-001, and your previous batch
also used TR-001 to TR-004 for different things (those became `ScrollArea.StickToEnd`,
`TreeView`, `DiffView` and the joined `ToggleGroup`). We have recorded this one as
"Chatur batch 2" throughout. Please keep numbering upward from TR-010 next time, or the
two records cannot be told apart.

**Everything below is published. Upgrade to 2.0.9.**

> **Correction, 2026-09-22, later the same day.** The first version of this reply said the
> tree control and the difference viewer were built but "not yet published", and that
> everything else was waiting on a release. That was wrong, and the repository's own
> `CHANGELOG.md` is why: three versions had been tagged and published while the changelog
> still filed their contents under `[Unreleased]`, so this side could not see its own
> release state either. The real position:
>
> | Version | Released | What it gave you |
> |---|---|---|
> | **2.0.9** | 2026-09-22 | the six entries below that needed code |
> | **2.0.8** | 2026-09-20 | the tree control and the difference viewer — **the day before you filed this batch** |
> | **2.0.7** | 2026-09-15 | the version you are on |
>
> So TR-001 and TR-002 were never waiting on us. They were published before your report
> was written, and the answer there was always *upgrade*, not *wait*. We are sorry for the
> wasted effort, and we have added a version table to the top of the reference so the next
> person can check this in one look.
>
> **A feed warning while you are upgrading:** 2.0.7, 2.0.8 and 2.0.9 are on **GitHub
> Packages**, not nuget.org, which still stops at 2.0.6. If the upgrade cannot find 2.0.9,
> that is the reason.

#### The four that need no code from us

| Entry | What is actually there |
|---|---|
| **TR-001** tree control | `TreeView` + `TreeItem` **already exist** — expand/collapse, `@bind-SelectedValue`, the full WAI-ARIA tree keyboard, an `Icon` and `Trailing` slot per row, and children loaded on demand. They shipped for **your previous batch**, and were **published in 2.0.8 on 2026-09-20**. Live at `/components/tree-view`. |
| **TR-002** difference viewer | `DiffView` + `TextDiff` **already exist** — side by side or inline, line numbers, added/removed tinting, folded unchanged stretches. Also from your previous batch, and also **published in 2.0.8 on 2026-09-20**. Live at `/components/diff-view`. |
| **TR-005** small switch for a cell | `Size="SwitchSize.Small"` already exists **and is documented in the copy you read**, at line 775. Measured: the three switch heights are **20 / 24 / 28 px** and a DataTable body cell is **53 px**, so the small one does not stretch a row. |
| **TR-009** list whose order the user sets | `SortableList` is **already button-driven**. The reference you read describes it at line 1818, verbatim, as *"Reorderable list driven by real buttons rather than drag-and-drop"* — the entry says it "reorders by dragging". Measured live: 3 move-up and 3 move-down buttons, the first up disabled, and move-down reordering correctly. |

**TR-008 is mostly in the same group.** `DataTable` has had a real choosing column all
along: `SelectionMode="DataTableSelectionMode.Multiple"` is documented at line 1136, six
lines above the `SelectedItems` row you found, with a worked `@bind-SelectedItems` example
at line 2230. Measured live: 5 per-row checkboxes and a working count. **One part of your
entry was a genuine defect and we have fixed it** — see below.

**Why this happened, and what we changed because of it.** TR-001 and TR-002 are our fault,
though not in the way we first said. The copy at `.trblazeui/TrBlazeUI-AI-Reference.md` in
your repository is deployed by the **installed NuGet package** and only changes when you
upgrade it. Yours is the 2.0.7 copy, so `TreeView`, `DiffView`, `StickToEnd` and
`ToggleGroup` are invisible to you — but they were **published in 2.0.8 on 2026-09-20**,
the day before you filed. They were an upgrade away the whole time, and we did not say so,
because our own changelog had them filed as unpublished. Both ends were reading a stale
record.

So the reference now carries a **version table** at the top saying which version added
which control, and the changelog now names each release instead of collecting everything
under "unreleased".

TR-005, TR-008 and TR-009 are different: those answers were in the copy you had, and you
did not find them. That is still our problem — a reference nobody can search is a reference
that does not work. So the reference now opens with a **"Which control do I use for…"**
index that maps a problem on a screen straight to the control, and the headings name the
problem: *"DataTable — rows, sorting, paging, and letting the user choose rows"*,
*"SortableList — let the user set the order (move up / move down buttons)"*,
*"Switch — including a small switch inside a table cell"*. It also now states plainly that
the copy in your repository is your installed version's reference, not the latest one.

**Please start at that index.** If your problem is in it, the control exists.

#### The six we built

| Entry | What you get |
|---|---|
| **TR-003** editable code | **`CodeEditor`** — `@bind-Value`, line numbers, `TabSize`/`UseSpaces`, and **Tab inserts an indent instead of moving focus**. Press **Escape, then Tab** to move focus out; the control says so through `aria-describedby`, so it is not a keyboard trap. Plus **`EditorTabs`**, the open-file strip you asked for: a close button per tab and an unsaved mark. |
| **TR-004** per-step state | **`StepStatus`** — `Pending`, `Running`, `Waiting`, `Done`, `Retried`, `Failed` — on `StepperItem.Status` and `TimelineItem.Status`, plus `StepperItem.Trailing`. A paused chain and a second-attempt success both draw correctly now. `Stepper.Orientation="Vertical"` already existed, so that half of your ask was already met. |
| **TR-006** answer still arriving | **`Typing`** — three dots sized in `em` so they match the text they sit in, and **`Progress.Indeterminate`** for a bar that does not claim a percentage. |
| **TR-007** command output | **`LogView`** — lines marked ordinary / success / warning / failure, a height it keeps, and the newest line followed until the reader scrolls back, with a jump-back control. `MaxLines` caps it. Appending a line does **not** re-render the list, which was your specific complaint. |
| **TR-008** (the real part) | The multi-page choose-all control announced nothing about its state: its only name was the fixed "Select rows - click to see options". It now reads "No rows selected, 500 in all - choose rows", then "1 of 500 rows selected - choose rows", with a polite live region for changes. New read-only **`SelectedCount`**. |
| **TR-009** (the real part) | `SortableList` gains **`ShowPosition`** (the position per row, default **on**) and **`AllowRemove`** / **`OnRemove`** (the way out). |
| **TR-010** list → detail | **`NavList<TItem>`** — multi-line rows, `@bind-SelectedId`, arrow keys, one tab stop, disabled rows skipped. |

#### Three decisions you should know about before you use these

1. **There is no `ListDetail`, and there will not be one.** You asked for "a `ListDetail` or
   a `NavList<TItem>`". The two-pane split is your page's layout, not a control's — compose
   it with `Grid` or `ResizablePanelGroup`. Both compositions are shown on
   `/components/nav-list` so you can copy one.
2. **`NavList` is a `role="listbox"` by default, not a nav.** Choosing a role changes what
   the same screen shows and navigates nowhere, so `aria-current="page"` would be a lie. If
   your rows really do navigate, pass `Mode="NavListMode.Navigation"` and you get a real
   `<nav>` of links. Prefer **`@bind-SelectedId`** over `@bind-Selected`: after a re-fetch
   your list holds new instances and an object-valued selection silently stops matching.
3. **`EditorTabs` is not a `role="tablist"`.** A tablist's roving `tabindex` would put your
   per-tab close buttons out of keyboard reach, so it is a plain list with `aria-current`.
   If you were planning to style it as tabs, the semantics are deliberately different.

Two smaller things: **`CodeEditor` does not wrap lines** (with soft wrap one logical line
paints as several rows and every gutter number below it is wrong), and **no syntax
highlighter is bundled** — pass your own markup through `Html`, the same bargain
`CodeBlock` makes.

#### What we need from you

**Upgrade to 2.0.9 — it is already out.** From GitHub Packages
(`https://nuget.pkg.github.com/techierathore/index.json`); nuget.org still stops at 2.0.6,
so that feed will not find it.

Then re-check the six built entries against your own screens — particularly `LogView` on
the Process run screen and `NavList` on the roles tab, since those are the two where your
real usage will be heavier than our demo. Tell us what does not fit.

The four we pushed back on need nothing from you except the upgrade and a look at the two
new tables at the top of the reference: **"Which control do I use for…"**, which maps a
problem on a screen to the control that solves it, and **"Which version added what"**,
which tells you in one look whether a control you cannot find is missing or just newer
than your package.

---
