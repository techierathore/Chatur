# TrBlazeUI feedback — found while building Chatur

| | |
|---|---|
| App | Chatur |
| Upstream | TrBlazeUI |
| Updated | 2026-10-02 |

## Summary

14 entries: 1 blocking (TR-014, 9 Settings Agents rows), 3 fixed upstream (TR-011, TR-012, TR-013), 10 closed.

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

## Replies from TrBlazeUI

<!-- The upstream team's answers, newest block first. Left in full: this is the record. -->

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
