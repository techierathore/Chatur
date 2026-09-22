# TrBlazeUI AI Component Reference

> Comprehensive reference for AI agents generating TrBlazeUI-based Blazor UIs.
> TrBlazeUI is a .NET 10 Blazor component library with Tailwind CSS v4 and shadcn/ui design.

---

## Which control do I use for…

Start here. Each row is a problem on a screen and the control that already solves it. If your
problem is in this table, the control exists — do not hand-build it.

| What you need on the screen | Use this | Section |
|---|---|---|
| A tree of files and folders — a nested list that opens and closes, one row selected, arrow keys | `TreeView` + `TreeItem` | §3 |
| Compare two texts — before and after, side by side or inline, with line numbers | `DiffView` (and `TextDiff` when you want the lines in code) | §8 |
| Let the user choose rows in a table — a box per row, a choose-all in the header, a count | `DataTable` with `SelectionMode="DataTableSelectionMode.Multiple"` and `@bind-SelectedItems` | §6 |
| Let the user set the order of a list — move up and move down | `SortableList` (real buttons; there is no dragging) | §8 |
| A switch inside a table cell — on and off per row, at the height of the row | `Switch Size="SwitchSize.Small"` inside a `DataTableColumn` `CellTemplate` | §5 |
| Follow a growing log or chat — keep the newest line in view, stop when the reader scrolls up | `ScrollArea StickToEnd="true"` | §3 |
| Edit a source file — line numbers, and Tab inserting an indent instead of moving focus | `CodeEditor` (and `EditorTabs` for the open-file strip) | §2.0.9 |
| Show the output of a running command — lines marked ordinary, warning or failure | `LogView` | §2.0.9 |
| Show that an answer is still being written — inside the message, while its text grows | `Typing` (or `Progress Indeterminate="true"` for a bar) | §2.0.9 |
| A list of multi-line rows that drives a detail pane beside it | `NavList<TItem>` | §2.0.9 |

---

## CRITICAL: Rules You MUST Follow

These rules are non-negotiable. Violating them produces broken or inconsistent UI.

### ALWAYS Do

1. **ALWAYS use TrBlazeUI components instead of raw HTML** — Use `<Input>` not `<input>`, `<Label>` not `<label>`, `<Button>` not `<button>`, `<Checkbox>` not `<input type="checkbox">`, `<Switch>` not custom toggles
2. **ALWAYS include a complete `@code { }` block** — Every generated page/component must have working C# code with all referenced fields, methods, and event handlers
3. **ALWAYS use `@bind-Value` / `@bind-Checked`** for two-way data binding on form inputs
4. **ALWAYS wrap form inputs with `<Field>` + `<FieldLabel>` + `<FieldContent>`** for consistent labeling, spacing, and validation
5. **ALWAYS use `ToastService`** (injected via `@inject`) for user feedback — never use `alert()` or custom notification divs
6. **ALWAYS use `<Dialog>` / `<Sheet>` / `<AlertDialog>`** for modal/overlay interactions
7. **ALWAYS use Tailwind CSS utility classes** via the `Class` parameter — never inline `style=""` attributes
8. **ALWAYS use `<LucideIcon Name="..." Size="16" />`** for icons — never raw SVG or `<i>` tags
9. **ALWAYS add `@using` statements** at the top of each file for any TrBlazeUI namespaces used (unless they're in `_Imports.razor`)
10. **ALWAYS use `AsChild` pattern** on triggers (`SheetTrigger`, `DialogTrigger`, etc.) to compose with `<Button>` instead of applying raw CSS to the trigger element

### NEVER Do

1. **NEVER use raw `<input>` elements** — Use `<Input Type="InputType.Email">`, `<Input Type="InputType.Password">`, etc.
2. **NEVER use raw `<label>` elements** — Use `<Label For="id">` or `<FieldLabel>`
3. **NEVER use raw `<button>` elements** — Use `<Button>` with appropriate `Variant` and `Size`
4. **NEVER use raw `<input type="checkbox">` elements** — Use `<Checkbox @bind-Checked="...">` or `<Switch @bind-Checked="...">`
5. **NEVER use raw `<select>` elements** — Use `<Select TValue="string">` with `<SelectTrigger>`, `<SelectContent>`, `<SelectItem>`
6. **NEVER use raw `<textarea>` elements** — Use `<Textarea @bind-Value="...">`
7. **NEVER apply button CSS classes directly to trigger elements** — Instead of `<DialogTrigger class="inline-flex items-center ...">`, use `<DialogTrigger AsChild><Button>...</Button></DialogTrigger>`
8. **NEVER forget the `@code` block** — Every page must compile; all fields and methods referenced in markup must be declared
9. **NEVER use `onclick` handlers on raw HTML** — Use `<Button OnClick="HandleClick">`
10. **NEVER use JavaScript `alert()` or `console.log()`** for user feedback — Use `ToastService`

### Guarantees you can rely on

- **Every public component accepts arbitrary HTML attributes.** All 344 public component types in
  `TrBlazeUI.Components` and all 59 in `TrBlazeUI.Primitives` declare
  `[Parameter(CaptureUnmatchedValues = true)]`, so `id`, `style`, `data-*`, `aria-*`, `tabindex`
  and event handlers (`@onkeydown`, `@onfocus`, …) can be passed to any component without it
  throwing. Re-verify with `dotnet run --project tools/splat-audit -- <bin dir>`.
  **Two documented exceptions, which accept the attributes and do not render them** because they
  own no element of their own — they only supply a cascading context: the context roots
  (`Dialog`, `Sheet`, `Popover`, `HoverCard`, `DropdownMenu`, `ContextMenu`, `Drawer`,
  `TooltipProvider`, `ResponsiveNavProvider`, `PortalHost`) and the configuration-only
  `DataTableColumn`. Put the hook on the part that renders the visible element
  (`DialogContent`, `SheetContent`, …). `BreadcrumbList` is a special case: it has no element
  either, but it forwards its attributes onto the `<ol>` that `Breadcrumb` renders.

- **Tailwind utilities work in application markup.** `trblazeui.css` ships the standard Tailwind
  scale (spacing, sizing, grid, flex, typography, colour tokens) with the `sm:`/`md:`/`lg:`/`xl:`/
  `2xl:` responsive variants, not just the utilities the library's own components happen to use.
  Arbitrary values work only when that exact class was present when the bundle was built; consumers
  cannot rely on a new bracket value being generated at runtime. Use the shipped scale
  (`min-w-3xl`, `w-9`, `text-sm`) or a component parameter such as `DataTable.MinWidth`.
  Gradient directions and semantic stops (`from-muted`, `via-primary`, `to-card`) are included.

- **Theming is an ordinary CSS override.** The library declares its own tokens through
  zero-specificity `:where(:root)` / `:where(.dark)` selectors, so an application `theme.css` using
  plain `:root` / `.dark` rules always wins — no `!important`, no layer juggling. The shipped
  defaults are validated as a full contrast matrix (every foreground token against every surface
  token, not just `--background`); re-check your own palette with `tools/token-contrast.py`.

### `Class` vs `class` — capital C or you silently lose the component's styling

This distinction is **not cosmetic**. On any TrBlazeUI component the two spellings take different
code paths and produce different DOM.

| You write | Where it goes | Result |
|-----------|---------------|--------|
| `Class="pb-2"` | the `Class` parameter → `ClassNames.cn(...)` | **Merged.** Tailwind conflict resolution runs: your `pb-2` replaces the built-in `pb-6` and every non-conflicting built-in class survives. |
| `class="pb-2"` | `AdditionalAttributes` → `@attributes` splat | **Replaced.** The component renders `class="@CssClass" @attributes="AdditionalAttributes"`; the splatted `class` lands *after* the explicit one and overwrites the whole string. Every built-in class is gone. |

```razor
@* BAD: lowercase class — CardHeader loses its entire base string, including p-6 padding *@
<CardHeader class="flex flex-row items-center justify-between space-y-0 pb-2">

@* GOOD: capital Class — merges through cn(), keeps p-6, replaces only the conflicting pb-* *@
<CardHeader Class="flex flex-row items-center justify-between space-y-0 pb-2">
```

**ALWAYS use `Class` on a TrBlazeUI component.** Reserve lowercase `class` for raw HTML elements
(`<div>`, `<span>`, `<header>`) — where it is the only correct spelling. The build does not warn:
a lowercase `class` on a component is a perfectly legal splatted attribute, so the only symptom is
a component that renders without its own padding, border, or layout.

A handful of components own no element to splat onto and therefore **accept unmatched attributes
and silently discard them** — the context roots and `DataTableColumn` listed under "Guarantees you
can rely on" above. A `data-testid` or `class` placed there vanishes without an error; put it on
the part that renders the visible element (`DialogContent`, `SheetContent`, …). `BreadcrumbList` is
the one exception: it has no element either but forwards its attributes onto the `<ol>` that
`Breadcrumb` renders — and, uniquely, a lowercase `class` there is now **merged** through `cn()`
rather than splatted over the built-ins (see §Breadcrumb). Use `Class` anyway; it is the documented
route and the one that is consistent across the library.

### Common Anti-Patterns (DO NOT copy these)

```razor
@* BAD: Raw HTML input with manual CSS classes *@
<input id="name" value="John"
       class="col-span-3 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm..." />

@* GOOD: TrBlazeUI Input component *@
<Input Id="name" @bind-Value="name" Class="col-span-3" />
```

```razor
@* BAD: Raw button with manual styling on trigger *@
<DialogTrigger class="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground hover:bg-primary/90 h-10 px-4 py-2">
    Open Dialog
</DialogTrigger>

@* GOOD: AsChild pattern with Button component *@
<DialogTrigger AsChild>
    <Button>Open Dialog</Button>
</DialogTrigger>
```

```razor
@* BAD: Raw checkbox *@
<input type="checkbox" checked class="h-4 w-4" />

@* GOOD: TrBlazeUI Checkbox or Switch *@
<Checkbox @bind-Checked="isEnabled" Id="feature" />
<Switch @bind-Checked="isEnabled" Id="feature" />
```

```razor
@* BAD: Missing @code block — fields not declared *@
<Input @bind-Value="name" />
@* Where is 'name' declared? This won't compile! *@

@* GOOD: Complete with @code block *@
<Input @bind-Value="name" />
@code {
    private string? name;
}
```

---

## 1. Quick Start Setup

### NuGet Packages

```xml
<!-- Required -->
<PackageReference Include="TrBlazeUI.Primitives" />
<PackageReference Include="TrBlazeUI.Components" />

<!-- Icons (pick one or more) -->
<PackageReference Include="TrBlazeUI.Icons.Lucide" />
<PackageReference Include="TrBlazeUI.Icons.Heroicons" />
<PackageReference Include="TrBlazeUI.Icons.Feather" />
```

### Program.cs Service Registration

```csharp
using TrBlazeUI.Primitives.Extensions;
using TrBlazeUI.Components.Toast;

builder.Services.AddTrBlazeUIPrimitives(); // PortalService, FocusManager, PositioningService, etc.
builder.Services.AddScoped<ToastService>();  // Required for Toast notifications
```

### _Imports.razor

> **Namespace rules — read before you copy this block.**
> - `PortalHost` lives in `TrBlazeUI.Primitives.Services`, **not** in `TrBlazeUI.Primitives`. So do
>   the positioning types `PopoverSide` / `PopoverAlign` / `PositioningStrategy`, each a fixed list of values, that
>   `DropdownMenuContent.Align`, `TooltipContent.Side` and friends take. That sub-namespace is in
>   the block below; without it `<PortalHost />` and `Align="PopoverAlign.Start"` do not compile.
> - `SheetSide` lives in `TrBlazeUI.Primitives.Sheet`. **Do not import that namespace** — it ships a
>   primitive `Sheet`/`SheetContent` that would shadow the styled `TrBlazeUI.Components.Sheet`
>   family (CS0104). Fully qualify the enum instead:
>   `<SheetContent Side="TrBlazeUI.Primitives.Sheet.SheetSide.Right">`.
> - The same trap applies to every `TrBlazeUI.Primitives.*` sub-namespace whose type names are
>   shared with the styled layer: `Checkbox`, `Label`, `Switch`, `Select`, `RadioGroup`,
>   `Collapsible`, `Accordion`, `DropdownMenu`, `Tabs`, `Tooltip`, `Dialog`, `Popover`, `HoverCard`.
>   **Safe set: `TrBlazeUI.Components.*` plus `TrBlazeUI.Primitives` and
>   `TrBlazeUI.Primitives.Services`. Never the other `Primitives.*` sub-namespaces.**
> - Two primitive namespaces are **not** in the shadowing list above and are still left out of the
>   block on purpose, because they are per-file tools rather than page furniture:
>   `TrBlazeUI.Primitives.Floating` (one type, `FloatingPortal` — internal positioning plumbing;
>   use `PortalHost` from `TrBlazeUI.Primitives.Services` instead) and `TrBlazeUI.Primitives.Table`
>   (the raw `Table` / `TableHeader` / `TableRow` / `TableCell` family under `DataTable`). Add
>   `@using TrBlazeUI.Primitives.Table` **at the top of the one file** that hand-builds a plain
>   table; it also brings `SelectionMode`, `SortDirection` and `ColumnDefinition` into scope, which
>   are common names in application code, so it does not belong in every page. For an ordinary data
>   grid use `DataTable` (§6) and import nothing extra.
> - `@using ApexCharts` is required by the chart family (see §8) — the charts are a
>   Blazor-ApexCharts wrapper and the series types come from that package.
> - The block below lists **every one of the 85 `TrBlazeUI.Components.*` component namespaces in
>   the 2.0.9 assembly** — one per component folder. Copy it whole. A partial copy is the single
>   most common cause of a silently broken page — see the RZ10012 note under the block.
> - **On an older version, drop the lines your package does not have.** A missing `@using` gives a
>   silent RZ10012 warning, but a `@using` for a namespace your installed package lacks is CS0246 —
>   a hard compile error. This document ships with the package, so if you are reading it from your
>   own `.trblazeui/` folder it already matches your version and you can copy it whole. Counts by
>   version: **2.0.7 → 79**, **2.0.8 → 81** (adds `TreeView`, `DiffView`), **2.0.9 → 85** (adds
>   `CodeEditor`, `LogView`, `NavList`, `Typing`).

```razor
@using TrBlazeUI.Components
@using TrBlazeUI.Primitives
@using TrBlazeUI.Primitives.Services
@using ApexCharts
@using TrBlazeUI.Components.Button
@using TrBlazeUI.Components.Card
@using TrBlazeUI.Components.Checkbox
@using TrBlazeUI.Components.RadioGroup
@using TrBlazeUI.Components.Select
@using TrBlazeUI.Components.Switch
@using TrBlazeUI.Components.Separator
@using TrBlazeUI.Components.Badge
@using TrBlazeUI.Components.Sidebar
@using TrBlazeUI.Components.Dialog
@using TrBlazeUI.Components.DropdownMenu
@using TrBlazeUI.Components.Toast
@using TrBlazeUI.Components.Tabs
@using TrBlazeUI.Components.Alert
@using TrBlazeUI.Components.Avatar
@using TrBlazeUI.Components.Input
@using TrBlazeUI.Components.Textarea
@using TrBlazeUI.Components.Label
@using TrBlazeUI.Components.Field
@using TrBlazeUI.Components.Combobox
@using TrBlazeUI.Components.Sheet
@using TrBlazeUI.Components.Tooltip
@using TrBlazeUI.Components.Progress
@using TrBlazeUI.Components.Skeleton
@using TrBlazeUI.Components.Spinner
@using TrBlazeUI.Components.DataTable
@using TrBlazeUI.Components.Breadcrumb
@using TrBlazeUI.Components.Collapsible
@using TrBlazeUI.Components.Drawer
@using TrBlazeUI.Components.Slider
@using TrBlazeUI.Components.Carousel
@using TrBlazeUI.Components.Command
@using TrBlazeUI.Components.ContextMenu
@using TrBlazeUI.Components.Popover
@using TrBlazeUI.Components.HoverCard
@using TrBlazeUI.Components.AlertDialog
@using TrBlazeUI.Components.Item
@using TrBlazeUI.Components.Typography
@using TrBlazeUI.Components.NavigationMenu
@using TrBlazeUI.Components.Rating
@using TrBlazeUI.Components.Prose
@using TrBlazeUI.Components.Stat
@using TrBlazeUI.Components.Timeline
@using TrBlazeUI.Components.Stepper
@using TrBlazeUI.Components.CenteredPanel
@using TrBlazeUI.Components.AnchorNav
@using TrBlazeUI.Components.CodeBlock
@using TrBlazeUI.Components.CodeEditor
@using TrBlazeUI.Components.LogView
@using TrBlazeUI.Components.NavList
@using TrBlazeUI.Components.Typing
@using TrBlazeUI.Components.PasswordStrength
@using TrBlazeUI.Components.SortableList
@using TrBlazeUI.Components.Accordion
@using TrBlazeUI.Components.AspectRatio
@using TrBlazeUI.Components.ButtonGroup
@using TrBlazeUI.Components.Calendar
@using TrBlazeUI.Components.Chart
@using TrBlazeUI.Components.ColorPicker
@using TrBlazeUI.Components.CurrencyInput
@using TrBlazeUI.Components.DatePicker
@using TrBlazeUI.Components.DateRangePicker
@using TrBlazeUI.Components.Empty
@using TrBlazeUI.Components.FileUpload
@using TrBlazeUI.Components.Grid
@using TrBlazeUI.Components.InputGroup
@using TrBlazeUI.Components.InputOTP
@using TrBlazeUI.Components.Kbd
@using TrBlazeUI.Components.MarkdownEditor
@using TrBlazeUI.Components.MaskedInput
@using TrBlazeUI.Components.Menubar
@using TrBlazeUI.Components.MultiSelect
@using TrBlazeUI.Components.NativeSelect
@using TrBlazeUI.Components.NumericInput
@using TrBlazeUI.Components.Pagination
@using TrBlazeUI.Components.RangeSlider
@using TrBlazeUI.Components.Resizable
@using TrBlazeUI.Components.ResponsiveNav
@using TrBlazeUI.Components.RichTextEditor
@using TrBlazeUI.Components.ScrollArea
@using TrBlazeUI.Components.TimePicker
@using TrBlazeUI.Components.Toggle
@using TrBlazeUI.Components.Toolbar
@using TrBlazeUI.Components.TreeView
@using TrBlazeUI.Components.DiffView
@using TrBlazeUI.Icons.Lucide.Components
@using TrBlazeUI.Icons.Lucide.Data

@* Deliberately NOT imported — see the namespace rules above:
   TrBlazeUI.Primitives.Sheet         — ships primitive Sheet/SheetContent that shadow the styled
                                        TrBlazeUI.Components.Sheet family (CS0104). Fully qualify
                                        SheetSide instead.
   TrBlazeUI.Primitives.Checkbox / .Label / .Switch / .Select / .RadioGroup / .Collapsible /
   .Accordion / .DropdownMenu / .Tabs / .Tooltip / .Dialog / .Popover / .HoverCard
                                      — same CS0104 shadowing trap.
   TrBlazeUI.Components.Utilities     — the ClassNames/TailwindMerge helper namespace. It contains
                                        no components; import it only in a .cs/.razor file that
                                        calls ClassNames.cn() directly. *@
```

When upgrading, merge newly introduced component namespaces into the consumer's existing
`_Imports.razor`. An unknown component tag can otherwise compile as a literal HTML element and fail
silently at runtime; the compiler does not diagnose the missing namespace.

> **Why a partial copy of this block is dangerous — read this before trimming it.**
> A missing `@using` is **not** a compile error. Razor emits **RZ10012, a warning**, then treats
> the unknown tag as a literal HTML element. `<Empty Title="No results found" />` becomes a literal
> `<empty>` element: the page compiles, renders unstyled inline text (or nothing at all), and no
> build step, test, or runtime check fails. The consumer sees a cosmetically wrong page with a
> clean build log. Do not omit a namespace on the grounds that the page "does not use that
> component" — a later edit that adds the component will fail the same silent way.
>
> **This list must be regenerated from the assembly, not hand-maintained.** A previous hand-edited
> revision of this document listed 49 of the namespaces then shipping; the 30 omissions (including
> `TrBlazeUI.Components.Empty`, whose component this very document documents in §6) reached
> consumers and produced exactly the silent failure above. Regenerate with
> `dotnet run --project tools/splat-audit -- <bin dir>`, which already reflects over the built
> assemblies, and diff the result against this block whenever the library version changes.

### App.razor / MainLayout.razor Setup

```razor
@inherits LayoutComponentBase
@using TrBlazeUI.Components.Sidebar
@using TrBlazeUI.Components.Toast

<SidebarProvider DefaultOpen="true" HeightClass="h-screen">
    <Sidebar Collapsible="true">
        <SidebarHeader>...</SidebarHeader>
        <SidebarContent>...</SidebarContent>
        <SidebarFooter>...</SidebarFooter>
    </Sidebar>

    <SidebarInset>
        <header class="flex h-16 shrink-0 items-center gap-4 border-b bg-background px-4">
            <SidebarTrigger />
        </header>
        <div class="flex-1 overflow-auto p-6 md:p-8">
            @Body
        </div>
    </SidebarInset>
</SidebarProvider>

<ToastProvider Position="ToastPosition.BottomRight" />
<PortalHost />
```

### CSS Imports (in order)

```html
<link rel="stylesheet" href="styles/theme.css" />
<link rel="stylesheet" href="_content/TrBlazeUI.Components/trblazeui.css" />
<link rel="stylesheet" href="styles/base.css" />
```

---

## 2. Theme Setup

### CSS Variables (OKLCH Color Space)

TrBlazeUI uses OKLCH colors in CSS custom properties, compatible with shadcn themes from tweakcn.com.

```css
@layer base {
  :root {
    --background: oklch(1 0 0);
    --foreground: oklch(0.1450 0 0);
    --card: oklch(1 0 0);
    --card-foreground: oklch(0.1450 0 0);
    --popover: oklch(1 0 0);
    --popover-foreground: oklch(0.1450 0 0);
    --primary: oklch(0.2050 0 0);
    --primary-foreground: oklch(0.9850 0 0);
    --secondary: oklch(0.9700 0 0);
    --secondary-foreground: oklch(0.2050 0 0);
    --muted: oklch(0.9700 0 0);
    --muted-foreground: oklch(0.5560 0 0);
    --accent: oklch(0.9700 0 0);
    --accent-foreground: oklch(0.2050 0 0);
    --destructive: oklch(0.5770 0.2450 27.3250);
    --destructive-foreground: oklch(1 0 0);
    --border: oklch(0.9220 0 0);
    --input: oklch(0.9220 0 0);
    --ring: oklch(0.7080 0 0);
    --radius: 0.625rem;

    /* Sidebar-specific */
    --sidebar: oklch(0.9850 0 0);
    --sidebar-foreground: oklch(0.1450 0 0);
    --sidebar-primary: oklch(0.2050 0 0);
    --sidebar-primary-foreground: oklch(0.9850 0 0);
    --sidebar-accent: oklch(0.9700 0 0);
    --sidebar-accent-foreground: oklch(0.2050 0 0);
    --sidebar-border: oklch(0.9220 0 0);

    /* Alert variants */
    --alert-success: oklch(0.55 0.20 142);
    --alert-info: oklch(0.50 0.20 255);
    --alert-warning: oklch(0.68 0.18 55);
    --alert-danger: oklch(0.55 0.22 27);

    /* Chart colors */
    --chart-1 through --chart-5
  }

  .dark {
    /* Dark mode overrides for all above variables */
  }
}
```

### Tailwind v4 Theme Integration

```css
@theme inline {
  --color-background: var(--background);
  --color-foreground: var(--foreground);
  --color-primary: var(--primary);
  --color-primary-foreground: var(--primary-foreground);
  /* ... all semantic colors mapped */
  --radius-sm: calc(var(--radius) - 4px);
  --radius-md: calc(var(--radius) - 2px);
  --radius-lg: var(--radius);
  --radius-xl: calc(var(--radius) + 4px);
}
```

### Dark Mode

Add `class="dark"` to the `<html>` element. All components automatically adapt.

---

## 3. Layout Components

### SidebarProvider

Root wrapper that manages sidebar state, persistence, and responsive behavior.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| DefaultOpen | bool | true | Initial open state |
| Variant | SidebarVariant | Sidebar | Visual variant: Sidebar, Floating, Inset |
| Side | SidebarSide | Left | Which side: Left, Right |
| CookieKey | string? | "sidebar:state" | Cookie key for persistence (null disables) |
| HeightClass | string | "min-h-screen" | Container height CSS class |
| Width | string? | null | Width of the expanded desktop column, any CSS length. Null → the stylesheet's `--sidebar-width` (16rem) |
| MobileWidth | string? | null | Width of the slid-out phone menu, any CSS length. Null → `--sidebar-width-mobile` (18rem) |
| IconWidth | string? | null | Width of the collapsed icon rail, any CSS length. Null → `--sidebar-width-icon` (3rem) |
| ChildContent | RenderFragment | - | Content (Sidebar + SidebarInset) |

**Three widths, three tokens.** The desktop column is `--sidebar-width` (16rem), the collapsed rail
`--sidebar-width-icon` (3rem), and the phone menu — the sheet that slides out below 768px —
`--sidebar-width-mobile` (**18rem**, not 16rem). All three ship as `:root` declarations you can
theme globally; the parameters above set them on this shell only, which is what you want when one
layout differs from the rest of the app.

### Sidebar Component Hierarchy

```
SidebarProvider
  Sidebar (Collapsible="true")
    SidebarHeader
      SidebarHeaderContent
        [logo div]
        SidebarHeaderInfo
          [title span]
          [subtitle span]
    SidebarContent
      SidebarGroup
        SidebarGroupLabel
        SidebarGroupContent
          SidebarMenu
            SidebarMenuItem
              SidebarMenuButton (Tooltip="..." IsActive="true" Href="/path")
                [LucideIcon]
                [span text]
              SidebarMenuBadge  (optional)
              SidebarMenuAction (optional)
              SidebarMenuSub    (submenu)
                SidebarMenuSubItem
                  SidebarMenuSubButton
      SidebarSeparator
    SidebarFooter
      SidebarMenu > SidebarMenuItem > ...
  SidebarInset
    header > SidebarTrigger
    [main content]
  SidebarRail (optional)
```

#### SidebarMenuButton

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Tooltip | string? | null | Tooltip in collapsed state |
| Size | SidebarMenuButtonSize | Default | Size: Default, Small, Large |
| Variant | SidebarMenuButtonVariant | Default | Style: Default, Outline |
| IsActive | bool | false | Active/selected state |
| Href | string? | null | URL (auto-renders as NavLink) |
| Match | NavLinkMatch | Prefix | URL match mode |
| Class | string? | null | Additional CSS |

```razor
<SidebarProvider DefaultOpen="true">
    <Sidebar Collapsible="true">
        <SidebarHeader>
            <SidebarHeaderContent>
                <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                    <LucideIcon Name="command" Size="16" />
                </div>
                <SidebarHeaderInfo>
                    <span class="truncate font-semibold">App Name</span>
                    <span class="truncate text-xs text-muted-foreground">v1.0</span>
                </SidebarHeaderInfo>
            </SidebarHeaderContent>
        </SidebarHeader>
        <SidebarContent>
            <SidebarMenu>
                <SidebarMenuItem>
                    <SidebarMenuButton Href="/" Match="NavLinkMatch.All" Tooltip="Home">
                        <LucideIcon Name="house" Size="16" />
                        <span>Home</span>
                    </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                    <SidebarMenuButton Tooltip="Settings" IsActive="true">
                        <LucideIcon Name="settings" Size="16" />
                        <span>Settings</span>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarContent>
    </Sidebar>
    <SidebarInset>
        <header class="flex h-16 shrink-0 items-center gap-2 border-b px-4">
            <SidebarTrigger />
            <h1 class="text-lg font-semibold">Page Title</h1>
        </header>
        <div class="flex-1 p-6">@Body</div>
    </SidebarInset>
</SidebarProvider>
```

#### Collapsible Submenu Pattern

```razor
<SidebarMenuItem>
    <Collapsible>
        <SidebarMenuButton Tooltip="Models">
            <LucideIcon Name="box" Size="16" />
            <span>Models</span>
            <SidebarMenuChevron>
                <LucideIcon Name="chevron-right" Size="16" />
            </SidebarMenuChevron>
        </SidebarMenuButton>
        <CollapsibleContent>
            <SidebarMenuSub>
                <SidebarMenuSubItem>
                    <SidebarMenuSubButton><span>Sub Item</span></SidebarMenuSubButton>
                </SidebarMenuSubItem>
            </SidebarMenuSub>
        </CollapsibleContent>
    </Collapsible>
</SidebarMenuItem>
```

### Card

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Class | string? | null | Additional CSS classes |
| ChildContent | RenderFragment | - | Card content |

Sub-components: `CardHeader`, `CardTitle`, `CardDescription`, `CardContent`, `CardFooter`, `CardAction`

```razor
<Card>
    <CardHeader>
        <CardTitle>Title</CardTitle>
        <CardDescription>Description text</CardDescription>
    </CardHeader>
    <CardContent>
        <p>Main content here</p>
    </CardContent>
    <CardFooter Class="gap-2">
        <Button Variant="ButtonVariant.Outline">Cancel</Button>
        <Button>Save</Button>
    </CardFooter>
</Card>
```

### Separator

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Orientation | SeparatorOrientation | Horizontal | Horizontal or Vertical |
| Decorative | bool | true | If true, hidden from screen readers |
| Class | string? | null | Additional CSS classes |

```razor
<Separator />
<Separator Orientation="SeparatorOrientation.Vertical" />
```

### AspectRatio

```razor
<AspectRatio Ratio="16.0/9.0">
    <img src="image.jpg" class="rounded-md object-cover" />
</AspectRatio>
```

### ScrollArea

```razor
<ScrollArea Class="h-[200px] w-[350px] rounded-md border p-4">
    <p>Long scrollable content...</p>
</ScrollArea>
```

A height or max-height on `Class` bounds the scrolling viewport; `Height` / `MaxHeight` set it on
the viewport directly.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| StickToEnd | bool | false | Keep the newest content in view as it arrives — see below |
| AtEndChanged | EventCallback<bool> | - | Reports whether the view is at the end right now |
| Height | string? | null | CSS length on the viewport, e.g. `"300px"` |
| MaxHeight | string? | null | CSS length on the viewport |
| Orientation | ScrollAreaOrientation | Vertical | `Vertical`, `Horizontal`, `Both` |
| Type | ScrollAreaType | Hover | When the scrollbar is shown |
| ViewportClass | string? | null | Extra classes on the inner viewport |
| Class | string? | null | Additional CSS classes |

#### Follow a growing log, a build's output, or a chat — `StickToEnd`

**A panel that grows at the bottom and should keep the newest line in view: `StickToEnd="true"`.**
This is the control for the output of a running command, a chat transcript, or any list that is
appended to while the reader watches. The view follows new content
(DOM changes and size changes, including text a child component updates on its own) until the
reader scrolls up; it resumes when they scroll back to the end. `AtEndChanged` reports both, and
`ScrollToEndAsync()` jumps back and resumes:

```razor
<ScrollArea @ref="logArea" StickToEnd="true" AtEndChanged="v => following = v" Class="h-[300px] rounded-md border">
    <CodeBlock Code="@output" ShowCopyButton="false" Class="rounded-none border-0" />
</ScrollArea>
@if (!following)
{
    <Button Size="ButtonSize.Small" OnClick="() => logArea!.ScrollToEndAsync()">Jump to latest</Button>
}

@code {
    private ScrollArea? logArea;
    private bool following = true;
}
```

### Collapsible

```razor
<Collapsible @bind-Open="isOpen">
    <CollapsibleTrigger>Toggle</CollapsibleTrigger>
    <CollapsibleContent>
        <p>Hidden content revealed on toggle</p>
    </CollapsibleContent>
</Collapsible>
```

**A disclosure header that spans its row needs `Class="w-full"`.** `CollapsibleTrigger` renders a
real `<button>` that shrinks to fit its content, like any button. `Class` lands on that button, so
a spacer inside the trigger has something to push against:

```razor
<CollapsibleTrigger Class="w-full text-left">
    <span class="flex w-full items-center gap-2">
        <LucideIcon Name="chevron-down" Size="16" />
        <span class="font-mono">build-phase</span>
        <span class="text-muted-foreground">— 24 runs</span>
        <span class="flex-1"></span>
        <Badge Variant="BadgeVariant.Outline">6 of 24 observed</Badge>
    </span>
</CollapsibleTrigger>
```

`CollapsibleTrigger` also takes `AsChild` and splats unmatched attributes, so a `data-testid` or a
custom component can be the trigger.

### TreeView — a tree of files and folders, or any nested list

**This is the tree control.** A file explorer, a folder tree of unknown depth, an outline, any
hierarchy: rows open and close, one row is selected, and the
keyboard works as in the WAI-ARIA tree (Up/Down move, Right opens or enters, Left closes or goes to
the parent, Home/End, Enter/Space select, a letter jumps to the next row starting with it). Use this,
not nested `Collapsible`s — those have no selection, no arrow keys and no indentation.

```razor
<TreeView @bind-SelectedValue="selectedPath" AriaLabel="Files">
    <TreeItem Label="src" Value="src" DefaultExpanded="true">
        <Icon><LucideIcon Name="folder" Size="16" /></Icon>
        <Trailing><Badge Variant="BadgeVariant.Secondary">3</Badge></Trailing>
        <ChildContent>
            <TreeItem Label="Program.cs" Value="src/Program.cs">
                <Icon><LucideIcon Name="file-code" Size="16" /></Icon>
            </TreeItem>
        </ChildContent>
    </TreeItem>
    @* Children loaded on demand: mark the branch, load in OnExpand, show Loading meanwhile *@
    <TreeItem Label="packages" Value="packages" HasChildren="true" OnExpand="LoadPackagesAsync" Loading="isLoading">
        @foreach (var p in packages)
        {
            <TreeItem Label="@p" Value="@($"packages/{p}")" />
        }
    </TreeItem>
</TreeView>
```

**TreeView**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| SelectedValue | string? | null | The selected row's `Value` |
| SelectedValueChanged | EventCallback<string?> | - | Two-way: `@bind-SelectedValue`. Leave it unbound and the tree keeps the selection itself |
| ToggleOnClick | bool | true | Clicking a branch row also opens and closes it; the chevron always does |
| Indent | string? | null | CSS length per level, default `1rem` |
| AriaLabel | string? | null | Name for the tree, e.g. `"Files"` |
| ChildContent | RenderFragment? | null | The top-level `TreeItem` rows |
| Class | string? | null | Additional CSS classes |

**TreeItem**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Label | string? | null | The row's text |
| LabelContent | RenderFragment? | null | The row's text as markup, instead of `Label` |
| Value | string? | null | The row's identity; this is what `SelectedValue` holds |
| Icon | RenderFragment? | null | Leading slot — put a `<LucideIcon>` in it |
| Trailing | RenderFragment? | null | Trailing slot — a `Badge`, a count, a button |
| ChildContent | RenderFragment? | null | The child rows. Use the explicit `<ChildContent>` tag whenever you also use `<Icon>` or `<Trailing>` |
| Expanded | bool | false | Open state |
| ExpandedChanged | EventCallback<bool> | - | Two-way: `@bind-Expanded` |
| DefaultExpanded | bool | false | Open to begin with, then the row keeps its own state |
| HasChildren | bool? | null | Mark a branch whose children are not rendered yet |
| OnExpand | EventCallback | - | Fired the first time the branch opens — load the children here |
| Loading | bool | false | Show the loading row while `OnExpand` runs |
| LoadingText | string | "Loading…" | Text of that row |
| Disabled | bool | false | The row cannot be selected or opened |
| Class | string? | null | Additional CSS classes |

- The trailing slot is `Trailing`, not `Badge` — a `Badge` component goes *inside* it.
- Unmatched attributes (`data-testid`, `aria-*`) land on the element carrying `role="treeitem"`.
- Children render only while their branch is open, so a closed branch costs nothing.
- Live example: `/components/tree-view` in the demo app. The root renders `role="tree"`.

---

## 4. Navigation Components

### Breadcrumb

`Breadcrumb` owns the `<nav>` **and** the `<ol>`. `BreadcrumbList` is optional — items may be placed
directly inside `Breadcrumb` — and when present it renders no element of its own; its `Class` and
attributes are forwarded onto the parent's `<ol>`.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Class | string? | null | Additional CSS classes on the `<nav>` |
| ListClass | string? | null | Additional CSS classes merged (via `cn`) onto the `<ol>` |
| Wrap | bool | true | **NEW.** `false` keeps the whole trail on one line under width pressure (`flex-nowrap`); the row keeps its height and the container clips the overflow |
| ChildContent | RenderFragment? | null | `BreadcrumbList` or the items directly |
| AdditionalAttributes | - | - | Unmatched attributes are splatted onto the `<nav>` |

`BreadcrumbList` (optional):

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Class | string? | null | **NEW.** Merged (via `cn`) onto the parent's `<ol>` |
| ChildContent | RenderFragment? | null | `BreadcrumbItem` / `BreadcrumbSeparator` children |
| AdditionalAttributes | - | - | Forwarded onto the parent's `<ol>` (e.g. `data-testid`). Ignored if used with no `Breadcrumb` parent |

Sub-components: `BreadcrumbList`, `BreadcrumbItem`, `BreadcrumbLink`, `BreadcrumbPage`,
`BreadcrumbSeparator`.

> **BEHAVIOUR CHANGE — a lowercase `class` on `BreadcrumbList` is now MERGED, not clobbering.**
> It used to be splatted onto the `<ol>` after the built-in class string and replaced it wholesale, so
> the list silently lost `flex`, alignment, gap, text size and colour. It is now merged through
> `ClassNames.cn` (whether supplied as the `Class` parameter or as a raw lowercase `class`), so the
> built-ins survive. Prefer `Class` anyway — it is the documented route.

> **Use `Wrap="false"` inside a fixed-height chrome.** The `<ol>`'s row gap is `gap-y-0`, so a wrapped
> trail adds no vertical padding, but it still adds a second line and grows the bar. `Wrap="false"`
> emits `flex-nowrap` instead of `flex-wrap`. `ListClass="flex-nowrap"` also works now that flex-wrap
> is a merge group.
>
> ```razor
> <Breadcrumb Wrap="false" Class="min-w-0 overflow-hidden">
>     <BreadcrumbList Class="whitespace-nowrap">
>         <BreadcrumbItem><BreadcrumbLink Href="/">Home</BreadcrumbLink></BreadcrumbItem>
>         <BreadcrumbSeparator />
>         <BreadcrumbItem><BreadcrumbPage>Current</BreadcrumbPage></BreadcrumbItem>
>     </BreadcrumbList>
> </Breadcrumb>
> ```

```razor
<Breadcrumb>
    <BreadcrumbList>
        <BreadcrumbItem>
            <BreadcrumbLink Href="/">Home</BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbSeparator />
        <BreadcrumbItem>
            <BreadcrumbLink Href="/components">Components</BreadcrumbLink>
        </BreadcrumbItem>
        <BreadcrumbSeparator />
        <BreadcrumbItem>
            <BreadcrumbPage>Breadcrumb</BreadcrumbPage>
        </BreadcrumbItem>
    </BreadcrumbList>
</Breadcrumb>
```

### Tabs

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| DefaultValue | string? | null | Default active tab (uncontrolled) |
| Value | string? | null | Active tab (controlled) |
| ValueChanged | EventCallback<string?> | - | Tab change callback |
| Orientation | TabsOrientation | Horizontal | Horizontal or Vertical |
| ActivationMode | TabsActivationMode | Automatic | Automatic or Manual |
| Class | string? | null | Additional CSS classes |

```razor
<Tabs DefaultValue="account">
    <TabsList Class="grid w-full grid-cols-2">
        <TabsTrigger Value="account">Account</TabsTrigger>
        <TabsTrigger Value="password">Password</TabsTrigger>
    </TabsList>
    <TabsContent Value="account">
        <p>Account settings content</p>
    </TabsContent>
    <TabsContent Value="password">
        <p>Password settings content</p>
    </TabsContent>
</Tabs>
```

### Pagination

`Pagination` lives in the `TrBlazeUI.Components.Pagination` namespace, which is **not** in the
global `_Imports` — add `@using TrBlazeUI.Components.Pagination`. It is a **composite** (there is
no top-level `CurrentPage` / `TotalPages` / `OnPageChanged` API): compose `PaginationContent` +
`PaginationItem` + `PaginationPrevious` / `PaginationLink` / `PaginationNext` and drive the current
page yourself, or pass a `PaginationState` via the `State` parameter for automatic wiring. (Most
tables get pagination for free through `<DataTable>`, which embeds this internally.)

```razor
@using TrBlazeUI.Components.Pagination

<Pagination>
    <PaginationContent>
        <PaginationItem>
            <PaginationPrevious OnClick="@(() => GoToPage(currentPage - 1))" Disabled="@(currentPage <= 1)" />
        </PaginationItem>
        @for (int i = 1; i <= totalPages; i++)
        {
            var page = i;
            <PaginationItem>
                <PaginationLink IsActive="@(currentPage == page)" OnClick="@(() => GoToPage(page))">@page</PaginationLink>
            </PaginationItem>
        }
        <PaginationItem>
            <PaginationNext OnClick="@(() => GoToPage(currentPage + 1))" Disabled="@(currentPage >= totalPages)" />
        </PaginationItem>
    </PaginationContent>
</Pagination>
```

### NavigationMenu

```razor
<NavigationMenu>
    <NavigationMenuList>
        <NavigationMenuItem>
            <NavigationMenuTrigger>Getting Started</NavigationMenuTrigger>
            <NavigationMenuContent>
                <NavigationMenuLink Href="/docs">Documentation</NavigationMenuLink>
            </NavigationMenuContent>
        </NavigationMenuItem>
    </NavigationMenuList>
</NavigationMenu>
```

### Menubar

```razor
<Menubar>
    <MenubarMenu>
        <MenubarTrigger>File</MenubarTrigger>
        <MenubarContent>
            <MenubarItem>New Tab</MenubarItem>
            <MenubarSeparator />
            <MenubarItem>Exit</MenubarItem>
        </MenubarContent>
    </MenubarMenu>
</Menubar>
```

---

## 5. Form Components

### Button

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Variant | ButtonVariant | Default | Default, Destructive, Outline, Secondary, Ghost, Link |
| Size | ButtonSize | Default | Small, Default, Large, Icon, IconSmall, IconLarge |
| Type | ButtonType | Button | Button, Submit, Reset |
| Disabled | bool | false | Disabled state |
| OnClick | EventCallback<MouseEventArgs> | - | Click handler |
| Icon | RenderFragment? | null | Icon content |
| IconPosition | IconPosition | Start | Start or End |
| Href | string? | null | Renders as anchor when set |
| Target | string? | null | Anchor target |
| AriaLabel | string? | null | Accessible label |
| Class | string? | null | Additional CSS classes |

```razor
<!-- Basic -->
<Button OnClick="HandleClick">Click me</Button>

<!-- Variants -->
<Button Variant="ButtonVariant.Destructive">Delete</Button>
<Button Variant="ButtonVariant.Outline">Cancel</Button>
<Button Variant="ButtonVariant.Secondary">Secondary</Button>
<Button Variant="ButtonVariant.Ghost">Ghost</Button>
<Button Variant="ButtonVariant.Link">Link</Button>

<!-- Sizes -->
<Button Size="ButtonSize.Small">Small</Button>
<Button Size="ButtonSize.Large">Large</Button>

<!-- Icon button -->
<Button Size="ButtonSize.Icon" AriaLabel="Settings">
    <LucideIcon Name="settings" Size="16" />
</Button>

<!-- With icon — THREE supported routes, all valid on 2.1.0. See the note below the block. -->

<!-- 1. Positional: icon inline as the first loose child (button is inline-flex with gap-2). -->
<Button>
    <LucideIcon Name="mail" Size="16" />
    Send Email
</Button>

<!-- 2. <ButtonIcon> wrapper — renders no element of its own, so it mixes freely with loose text. -->
<Button>
    <ButtonIcon><LucideIcon Name="mail" Size="16" /></ButtonIcon>
    Send Email
</Button>

<!-- 3. Icon parameter + IconPosition. Because Icon is a named fragment, the label must be
     wrapped in an explicit <ChildContent> tag — a named fragment cannot sit next to loose
     content. This is legal Razor, not a workaround. -->
<Button IconPosition="IconPosition.End">
    <Icon><LucideIcon Name="mail" Size="16" /></Icon>
    <ChildContent>Send Email</ChildContent>
</Button>

<!-- As link -->
<Button Href="/about" Variant="ButtonVariant.Link">About</Button>

<!-- Submit -->
<Button Type="ButtonType.Submit">Submit Form</Button>
```

Sub-components: `ButtonIcon`

> **Button icons — all three routes work on 2.1.0.** Earlier revisions of this document stated that
> using the `Icon` fragment alongside label text was a Razor compile error (RZ10012). That is
> **false**: `Icon` is a plain `RenderFragment?` parameter (`Button.razor.cs`) rendered inside a
> spacing `<span>` at the start or end of the button according to `IconPosition`
> (`Button.razor`). The RZ10012 error only occurs if you leave the label as *loose* content next to
> the named `Icon` fragment — wrapping it in `<ChildContent>` is the fix, and is ordinary Razor.
>
> - **`<ButtonIcon>`** is the least fussy route. It renders `@ChildContent` with **no wrapper
>   element at all** unless you supply attributes (in which case it wraps in a `<span>` so the
>   attributes have somewhere to land), so it composes freely with loose label text.
> - **The positional route** (icon as the first loose child) remains supported — the button is
>   `inline-flex` with `gap-2`, so spacing is automatic.
> - **`Icon` + `IconPosition`** is the route to use when you need the icon *after* the label
>   (`IconPosition.End`); it is the only one of the three that can place a trailing icon.

### Input

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Type | InputType | Text | Text, Email, Password, Number, Tel, Url, Search, Date, Time, File |
| Value | string? | null | Current value |
| ValueChanged | EventCallback<string?> | - | Value change (two-way: @bind-Value) |
| Placeholder | string? | null | Placeholder text |
| Disabled | bool | false | Disabled state |
| Required | bool | false | Required field |
| Id | string? | null | Element ID (for label association) |
| AriaInvalid | bool? | null | Invalid state |
| AriaDescribedBy | string? | null | ID of describing element |
| AriaLabel | string? | null | Accessible name. **`Placeholder` is a hint, not a name** — it disappears once the field has content. Any field with no visible `<Label For=...>` needs this. |
| DebounceMilliseconds | int | 0 | Delay before `ValueChanged` fires. 0 raises it per keystroke; 150–300 ms cuts the round trips on a Blazor **Server** circuit. The DOM value is never debounced. |
| Class | string? | null | Additional CSS classes |

```razor
<Input @bind-Value="name" Placeholder="Enter your name" />
<Input Type="InputType.Email" @bind-Value="email" Required="true" />
<Input Type="InputType.Password" @bind-Value="password" />
<Input Type="InputType.Number" @bind-Value="age" />
```

### Textarea

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Value | string? | null | Current value |
| ValueChanged | EventCallback<string?> | - | Value change (two-way: @bind-Value) |
| Placeholder | string? | null | Placeholder text |
| MaxLength | int? | null | Character limit |
| Disabled | bool | false | Disabled state |
| Required | bool | false | Required field |
| AriaLabel | string? | null | Accessible name (see the note under `Input`) |
| DebounceMilliseconds | int | 0 | Delay before `ValueChanged` fires |
| Class | string? | null | Additional CSS classes |

```razor
<Textarea @bind-Value="description" Placeholder="Enter description" MaxLength="500" />
```

> **Blazor Server note.** `Input` and `Textarea` keep the DOM value and the server echo separate, so
> fast typing, pasted text and text injected by assistive technology (voice input, switch/AAC
> devices, password managers) is never dropped or reordered by a slow circuit. Delayed parent
> echoes are deferred while the control remains focused, so they cannot overwrite a newer edit.
> `DebounceMilliseconds` only reduces traffic.

### Label

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| For | string? | null | ID of associated form element |
| Class | string? | null | Additional CSS classes |

```razor
<Label For="email">Email Address</Label>
<Input Id="email" Type="InputType.Email" @bind-Value="email" />
```

### Checkbox

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Checked | bool | false | Checked state |
| CheckedChanged | EventCallback<bool> | - | Two-way: @bind-Checked |
| Indeterminate | bool | false | Indeterminate state |
| Disabled | bool | false | Disabled state |
| Id | string? | null | Element ID |
| Class | string? | null | Additional CSS classes |

```razor
<div class="flex items-center space-x-2">
    <Checkbox @bind-Checked="isAccepted" Id="terms" />
    <Label For="terms">Accept terms and conditions</Label>
</div>
```

### Switch — including a small switch inside a table cell

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Checked | bool | false | On/off state |
| CheckedChanged | EventCallback<bool> | - | Two-way: @bind-Checked |
| Size | SwitchSize | Medium | `Small` (20px tall), `Medium` (24px), `Large` (28px) |
| Disabled | bool | false | Disabled state |
| AriaLabel | string? | null | Name for a switch with no visible label — required in a table cell |
| Id | string? | null | Element ID |
| Class | string? | null | Additional CSS classes |

```razor
<div class="flex items-center space-x-2">
    <Switch @bind-Checked="isEnabled" Id="airplane-mode" />
    <Label For="airplane-mode">Airplane Mode</Label>
</div>
```

#### A switch in a table cell — use `Size="SwitchSize.Small"`

`Switch` is not only a form control. `Size="SwitchSize.Small"` is 20px tall; a `DataTable` body
cell at the default `Comfortable` density is about 53px tall, so a small switch sits in a row
without making it taller than its neighbours. Do not hand-build a toggle out of `.toggle` classes
and do not reach for `Checkbox` when the meaning is "on or off", not "chosen".

There is no label beside a switch in a cell — the column header is the label — so give it
`AriaLabel` per row, or the screen reader reads an unnamed control.

```razor
<DataTableColumn TData="Feature" TValue="bool" Property="@(f => f.Enabled)"
                 Header="Enabled" Width="110px" Align="DataTableColumnAlign.Center">
    <CellTemplate Context="feature">
        <Switch Size="SwitchSize.Small"
                Checked="@feature.Enabled"
                AriaLabel="@($"Enable {feature.Name}")"
                CheckedChanged="@(async (bool v) => await SetEnabledAsync(feature, v))" />
    </CellTemplate>
</DataTableColumn>
```

Bind with `Checked` + `CheckedChanged` rather than `@bind-Checked` here, so the page can save the
change. Setting a property on a bound row does not re-render the grid on its own — reassign `Data`
or call `Refresh()` (see "Mutating a row in place" under `DataTable`).

`Small` also suits a dense settings list or a toolbar; `Large` is for a single prominent switch.

### Select (Generic)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TValue | type param | - | Value type (e.g., string) |
| Value | TValue? | null | Selected value |
| ValueChanged | EventCallback<TValue?> | - | Two-way: @bind-Value |
| DefaultValue | TValue? | null | Initial uncontrolled value. A one-way `Value` is used when this is omitted, so no dummy callback is required. |
| Disabled | bool | false | Disabled state |
| DisplayTextSelector | Func<TValue, string>? | null | Display text function |
| Class | string? | null | Additional CSS classes |

Sub-components: `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem`, `SelectGroup`, `SelectLabel`, `SelectSeparator`

```razor
<Select @bind-Value="selectedFruit" TValue="string" Class="w-[280px]">
    <SelectTrigger>
        <SelectValue Placeholder="Select a fruit" />
    </SelectTrigger>
    <SelectContent>
        <SelectItem Value="@("apple")" Text="Apple" TValue="string">Apple</SelectItem>
        <SelectItem Value="@("banana")" Text="Banana" TValue="string">Banana</SelectItem>
        <SelectItem Value="@("cherry")" Text="Cherry" TValue="string">Cherry</SelectItem>
    </SelectContent>
</Select>
```

If `DisplayTextSelector` returns null or empty, `SelectValue` falls back to `Value.ToString()` rather
than the placeholder. Map sentinel values explicitly.

#### Grouped Select

```razor
<Select @bind-Value="selected" TValue="string" Class="w-[280px]">
    <SelectTrigger>
        <SelectValue Placeholder="Select..." />
    </SelectTrigger>
    <SelectContent>
        <SelectGroup>
            <SelectLabel>Fruits</SelectLabel>
            <SelectItem Value="@("apple")" Text="Apple" TValue="string">Apple</SelectItem>
        </SelectGroup>
        <SelectGroup>
            <SelectLabel>Vegetables</SelectLabel>
            <SelectItem Value="@("carrot")" Text="Carrot" TValue="string">Carrot</SelectItem>
        </SelectGroup>
    </SelectContent>
</Select>
```

### RadioGroup (Generic)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TValue | type param | - | Value type |
| Value | TValue | default! | Selected value |
| ValueChanged | EventCallback<TValue> | - | Two-way: @bind-Value |
| Disabled | bool | false | Disabled state |
| AriaLabel | string? | null | Accessible label |
| Class | string? | null | Additional CSS classes |

```razor
<RadioGroup @bind-Value="selectedOption" TValue="string">
    <div class="flex items-center space-x-2">
        <RadioGroupItem Value="@("option1")" Id="r1" TValue="string" />
        <Label For="r1">Option 1</Label>
    </div>
    <div class="flex items-center space-x-2">
        <RadioGroupItem Value="@("option2")" Id="r2" TValue="string" />
        <Label For="r2">Option 2</Label>
    </div>
</RadioGroup>
```

### Combobox (Generic)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TItem | type param | - | Item type |
| Items | IEnumerable<TItem> | required | Data source |
| Value | string? | null | Selected value |
| ValueChanged | EventCallback<string?> | - | Two-way: @bind-Value |
| ValueSelector | Func<TItem, string> | required | Extract value from item |
| DisplaySelector | Func<TItem, string> | required | Extract display text |
| Placeholder | string | "Select an option..." | Button placeholder |
| SearchPlaceholder | string | "Search..." | Search input placeholder |
| EmptyMessage | string | "No results found." | Empty state message |
| Disabled | bool | false | Disabled state |
| PopoverWidth | string | "w-[200px]" | Dropdown width |
| MatchTriggerWidth | bool | false | Match trigger width |

```razor
<Combobox TItem="Framework"
          Items="frameworks"
          @bind-Value="selectedValue"
          ValueSelector="@(f => f.Value)"
          DisplaySelector="@(f => f.Label)"
          Placeholder="Select framework..."
          SearchPlaceholder="Search framework..."
          EmptyMessage="No framework found." />

@code {
    record Framework(string Value, string Label);
    List<Framework> frameworks = new() {
        new("blazor", "Blazor"),
        new("react", "React"),
        new("vue", "Vue")
    };
    string? selectedValue;
}
```

### Field

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Orientation | FieldOrientation | Vertical | Vertical, Horizontal, Responsive |
| IsInvalid | bool | false | Error state |
| Class | string? | null | Additional CSS classes |

Sub-components: `FieldLabel`, `FieldContent`, `FieldDescription`, `FieldError`, `FieldGroup`, `FieldSet`, `FieldLegend`, `FieldTitle`, `FieldSeparator`

```razor
<Field Orientation="FieldOrientation.Vertical">
    <FieldLabel>Email</FieldLabel>
    <FieldContent>
        <Input Type="InputType.Email" @bind-Value="email" />
        <FieldDescription>We will never share your email.</FieldDescription>
    </FieldContent>
</Field>

<Field Orientation="FieldOrientation.Horizontal" IsInvalid="@hasError">
    <FieldLabel>Username</FieldLabel>
    <FieldContent>
        <Input @bind-Value="username" />
        <FieldError>Username is required</FieldError>
    </FieldContent>
</Field>
```

### Calendar

```razor
<Calendar @bind-Value="selectedDate" />
```

### DatePicker

```razor
<DatePicker @bind-Value="selectedDate" Placeholder="Pick a date" />
```

Unmatched attributes such as `data-testid`, `aria-*` and event handlers are forwarded to the
visible trigger button. `TimePicker` follows the same rule.

### DateRangePicker

```razor
<DateRangePicker @bind-StartDate="startDate" @bind-EndDate="endDate" />
```

### TimePicker

```razor
<TimePicker @bind-Value="selectedTime" />
```

### Slider

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Value | double | 0 | Current value |
| ValueChanged | EventCallback<double> | - | Two-way: @bind-Value |
| Min | double | 0 | Minimum |
| Max | double | 100 | Maximum |
| Step | double | 1 | Step increment |
| Disabled | bool | false | Disabled state |
| Class | string? | null | Additional CSS classes |

```razor
<Slider @bind-Value="volume" Min="0" Max="100" Step="1" />
```

### RangeSlider

```razor
<RangeSlider @bind-LowValue="minPrice" @bind-HighValue="maxPrice" Min="0" Max="1000" />
```

### NumericInput

```razor
<NumericInput @bind-Value="quantity" Min="0" Max="100" Step="1" />
```

### CurrencyInput

```razor
<CurrencyInput @bind-Value="amount" CurrencySymbol="$" />
```

### MaskedInput

```razor
<MaskedInput @bind-Value="phone" Mask="(000) 000-0000" />
```

### InputOTP

```razor
<InputOTP @bind-Value="otp" Length="6" />
```

### InputGroup

```razor
<InputGroup>
    <InputGroupAddon>
        <LucideIcon Name="search" Size="16" />
    </InputGroupAddon>
    <InputGroupInput Placeholder="Search..." @bind-Value="searchQuery" />
    <InputGroupButton>
        <Button Size="ButtonSize.Small">Go</Button>
    </InputGroupButton>
</InputGroup>
```

`InputGroupInput` takes the same `DebounceMilliseconds` as `Input` (default 0, one `ValueChanged` per
keystroke). This is how to draw a filter box with a leading icon that re-filters only once typing pauses:

```razor
<InputGroup>
    <InputGroupAddon Align="InputGroupAlign.InlineStart">
        <LucideIcon Name="search" Size="16" />
    </InputGroupAddon>
    <InputGroupInput @bind-Value="filter" DebounceMilliseconds="150" AriaLabel="Filter" />
</InputGroup>
```

### ColorPicker

```razor
<ColorPicker @bind-Value="selectedColor" />
```

### FileUpload

The shipped API is `Files` (`IReadOnlyList<FileUploadItem>?`) + `FilesChanged`
(`EventCallback<IReadOnlyList<FileUploadItem>>`). `OnFilesSelected` is a back-compat **alias** of
`FilesChanged` (both fire together) — prefer `FilesChanged` / `Files` for new code.

```razor
<FileUpload Files="@files" FilesChanged="HandleFiles" Accept=".pdf,.doc" Multiple="true" />

@code {
    private IReadOnlyList<FileUploadItem>? files;
    private void HandleFiles(IReadOnlyList<FileUploadItem> selected) => files = selected;
}
```

### MultiSelect

```razor
<MultiSelect TItem="string"
             Items="options"
             @bind-SelectedValues="selectedItems"
             DisplaySelector="@(x => x)"
             ValueSelector="@(x => x)"
             Placeholder="Select items..." />
```

### NativeSelect

```razor
<NativeSelect @bind-Value="selected">
    <option value="">Choose...</option>
    <option value="1">Option 1</option>
    <option value="2">Option 2</option>
</NativeSelect>
```

The control turns the browser's own arrow off (`appearance-none`) and draws its own chevron as a
background image, so it looks the same in every browser. Nothing is needed from you for that.

### Rating

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Value / ValueChanged | double | 0 | Current rating (two-way: `@bind-Value`) |
| Max | int | 5 | Number of icons |
| AllowHalf / AllowClear | bool | false / true | Half steps; clicking the current value clears it |
| ReadOnly | bool | false | Renders as a **value**, not a control: `role="img"` + `aria-label`, with no radio meaning and no tab stop |
| Disabled | bool | false | `aria-disabled` on the group, options disabled |
| Focusable | bool | true | Set `false` to keep a purely decorative rating out of the tab order |
| Icon / IconTemplate | RatingIcon / RenderFragment | Star | Icon shape or a custom template |
| ActiveColor / InactiveColor | string? | null | Icon colours |
| Size | RatingSize | Default | Small, Default, Large |
| AriaLabel | string? | null | Accessible name of the group (or of the read-only value) |

Each option renders as a real `<button role="radio">` with a roving `tabindex` and a literal
`aria-checked="true"/"false"`, so the control is keyboard operable and screen readers report the
selection. Use `ReadOnly` for display-only ratings — do not wrap an interactive one in
`aria-hidden`.

```razor
@* interactive *@
<Rating @bind-Value="rating" Max="5" AriaLabel="Rate this article" />

@* display only *@
<Rating Value="4" Max="5" ReadOnly="true" Size="RatingSize.Small" />
```

### Toggle

```razor
<Toggle @bind-Pressed="isBold" AriaLabel="Toggle bold">
    <LucideIcon Name="bold" Size="16" />
</Toggle>
```

### ToggleGroup

Several toggles that keep the choice themselves: `Type="ToggleGroupType.Single"` (default, a radio
group) or `Multiple` (`@bind-Values`, a `List<TValue>`). For a view switch in a toolbar —
"Board | Table", "Side by side | Inline" — use `Joined="true"` (one control, shared border) and
`AllowDeselect="false"` (exactly one stays chosen). Not two loose `Toggle`s, and not `Tabs`.

```razor
<ToggleGroup TValue="string" @bind-Value="view" Joined="true" AllowDeselect="false"
             Variant="ToggleVariant.Outline" Size="ToggleSize.Small" AriaLabel="View">
    <ToggleGroupItem TValue="string" Value="@("board")">Board</ToggleGroupItem>
    <ToggleGroupItem TValue="string" Value="@("table")">Table</ToggleGroupItem>
</ToggleGroup>
```

**ToggleGroup<TValue>**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TValue | type param | required | The value type; goes on the group **and** on each item |
| Type | ToggleGroupType | Single | `Single` (one choice) or `Multiple` (several) |
| Value | TValue? | null | The chosen value when `Type="Single"` |
| ValueChanged | EventCallback<TValue?> | - | Two-way: `@bind-Value` |
| Values | List<TValue>? | null | The chosen values when `Type="Multiple"` |
| ValuesChanged | EventCallback<List<TValue>> | - | Two-way: `@bind-Values` |
| DefaultValue | TValue? | null | Initial value when you leave the group unbound |
| Joined | bool | false | One control with a shared border, for a view switch in a toolbar |
| AllowDeselect | bool | true | `false` keeps exactly one item chosen |
| Variant | ToggleVariant | Default | `Default` or `Outline` |
| Size | ToggleSize | Default | `Small`, `Default`, `Large` |
| Disabled | bool | false | Disables the whole group |
| AriaLabel | string? | null | Name for the group |
| Class | string? | null | Additional CSS classes |

**ToggleGroupItem<TValue>** takes `TValue`, `Value`, `Disabled`, `Class` and its content.

- String values need `Value="@("board")"` — the plain `Value="board"` form cannot infer `TValue`.
- One Tab stop (the chosen item); the arrow keys move between items. Single choice renders
  `role="radiogroup"` with `role="radio"` items; multiple renders `aria-pressed` buttons.

---

## 6. Data Display Components

### Avatar

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Size | AvatarSize | Default | Small (h-8), Default (h-10), Large (h-12), ExtraLarge (h-16) |
| Class | string? | null | Additional CSS classes |

Sub-components: `AvatarImage` (Source, Alt), `AvatarFallback`

```razor
<Avatar>
    <AvatarImage Source="https://example.com/avatar.jpg" Alt="John" />
    <AvatarFallback>JD</AvatarFallback>
</Avatar>

<Avatar Size="AvatarSize.Large" Class="rounded-lg">
    <AvatarFallback Class="rounded-lg">AB</AvatarFallback>
</Avatar>
```

### Badge

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Variant | BadgeVariant | Default | Default, Secondary, Destructive, Outline, **Success**, **Info**, **Warning** |
| As | string | `"span"` | The element rendered — `"span"` or `"div"`; anything else falls back to `"span"` |
| Wrap | bool | false | `true` lets a long label wrap inside a pill that grows with it (left-aligned, corner radius) |
| Truncate | bool | false | `true` clips a long label with an ellipsis on one line. Wins over `Wrap` if both are set |
| Class | string? | null | Additional CSS classes |

**NEW: `Success`, `Info` and `Warning` were added to `BadgeVariant`** — seven variants now, not four.
They deliberately mirror the matching `AlertVariant` members so status vocabulary is identical between
a badge and the alert that explains it: each one paints the tinted `--alert-*-bg` surface with
`--alert-*-foreground` text and an `--alert-*/30` border.

| Variant | Emitted classes |
|---------|-----------------|
| Default | `border-transparent bg-primary text-primary-foreground hover:bg-primary/80` |
| Secondary | `border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80` |
| Destructive | `border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80` |
| Outline | `text-foreground` |
| Success | `border-alert-success/30 bg-alert-success-bg text-alert-success-foreground hover:opacity-80` |
| Info | `border-alert-info/30 bg-alert-info-bg text-alert-info-foreground hover:opacity-80` |
| Warning | `border-alert-warning/30 bg-alert-warning-bg text-alert-warning-foreground hover:opacity-80` |

Use `Warning` for "needs attention" status; `Destructive` reads as a failure.

```razor
<Badge>New</Badge>
<Badge Variant="BadgeVariant.Secondary">Draft</Badge>
<Badge Variant="BadgeVariant.Destructive">Error</Badge>
<Badge Variant="BadgeVariant.Outline">Active</Badge>
<Badge Variant="BadgeVariant.Success">Passed</Badge>
<Badge Variant="BadgeVariant.Info">Queued</Badge>
<Badge Variant="BadgeVariant.Warning">Degraded</Badge>
```

**A badge is an inline element.** It renders `<span>`, so a pill can sit inside a sentence or a
`<p>` — a `<div>` there is invalid HTML the parser silently reshapes, and it also means a mockup
that draws its pills as `<span class="badge">` can be matched element for element. Pass `As="div"`
where a selector or a stylesheet depends on the old tag.

**A long label does not wrap unless you ask it to.** By default the label is held on one line and
overflows, which is visible. Say which behaviour you want:

```razor
@* one line, overflows - the default *@
<Badge Variant="BadgeVariant.Outline">transient</Badge>

@* wraps, and the pill grows with it: use this for a label that is a sentence *@
<Badge Variant="BadgeVariant.Outline" Wrap="true">transient · best-effort, absence is not an error</Badge>

@* one line, clipped with an ellipsis: use this for a label that is data *@
<Badge Variant="BadgeVariant.Outline" Truncate="true">@objRow.Label</Badge>
```

`Wrap` also drops the `rounded-full` pill radius to a corner radius, because a 999px radius on a
three-line box turns the end caps into deep arcs that cut into the first and last lines.

### DataTable (Generic) — rows, sorting, paging, and letting the user choose rows

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TData | type param | - | Data item type (class) |
| Data | IEnumerable<TData> | required | Data source |
| SelectionMode | DataTableSelectionMode | None | **Turns row choosing on.** `None`, `Single` (click a row), `Multiple` (a real column of boxes plus a choose-all). See "Letting the user choose rows" below |
| ShowToolbar | bool | false | Opt in to the search / column-visibility toolbar. This decides where the grid's chrome is drawn, **not** whether filtering is available — see `SearchText` |
| SearchText | string? | null | Two-way (`@bind-SearchText`): the grid's global search text. Bind it to host the filter input anywhere — a card header, a page filter bar — and still get the grid's own filtering over its `Filterable` columns. The built-in toolbar box writes back through the same binding |
| ShowColumnChooser | bool | true | `false` drops the toolbar's `Columns` dropdown and keeps the search box. Ignored when `ShowToolbar` is false |
| ShowPagination | bool | true | **Gates the data, not just the pager.** `true` → page window applied and the bar auto-hides when all rows fit one page. `false` → **every** row of the filtered/sorted sequence renders and `InitialPageSize` is ignored |
| ShowHeader | bool | true | `false` renders no `<thead>` at all — for key/value style tables that need no column headings |
| Density | DataTableDensity | Comfortable | `Comfortable` or `Compact`; controls header/body cell padding |
| IsLoading | bool | false | Loading state |
| InitialPageSize | int | 5 | Initial rows per page |
| PageSizes | int[] | [5,10,20,50,100] | Page size options |
| SelectedItems | IReadOnlyCollection<TData> | [] | Two-way: `@bind-SelectedItems` — the rows the user has chosen. `SelectedItems.Count` is the count to put on a button or a banner |
| MinWidth | string? | null | CSS length, e.g. `"720px"`. Set it on wide tables: the grid's wrapper scrolls horizontally, but a `w-full` table with no minimum just shrinks and the right-hand columns are squeezed away with no scrollbar. |
| Class | string? | null | Additional CSS classes |

#### Letting the user choose rows — `SelectionMode` + `@bind-SelectedItems`

**The grid already does this; do not put a plain `Checkbox` in the first column.** Set
`SelectionMode` and bind `SelectedItems`, and the grid renders the choosing column itself:

- a box on every row, each with its own name for a screen reader;
- a choose-all box in the header that also shows the part-chosen (indeterminate) state, and offers
  "Select all on this page" / "Select all N items" / "Clear selection" when the grid is paged;
- the chosen rows handed back through `@bind-SelectedItems`, so `SelectedItems.Count` is the count
  your page puts on a banner or uses to enable its own button.

`DataTableSelectionMode.Multiple` gives the column of boxes. `Single` gives no column — clicking a
row chooses it and clears the previous one. `None` (the default) is why a plain `DataTable` looks
as though it cannot do this at all.

```razor
@if (objChosen.Count > 0)
{
    <Alert Class="mb-4">
        <AlertTitle>@objChosen.Count file(s) chosen</AlertTitle>
    </Alert>
}

<DataTable TData="ChangedFile" Data="@objFiles"
           SelectionMode="DataTableSelectionMode.Multiple"
           @bind-SelectedItems="objChosen">
    <Columns>
        <DataTableColumn TData="ChangedFile" TValue="string" Property="@(f => f.Path)"
                         Header="File" Sortable Filterable
                         CellClass="whitespace-nowrap" HeaderClass="whitespace-nowrap" />
        <DataTableColumn TData="ChangedFile" TValue="string" Property="@(f => f.Change)" Header="Change" />
    </Columns>
</DataTable>

<Button Disabled="@(objChosen.Count == 0)" OnClick="CommitAsync">
    Commit @objChosen.Count file(s)
</Button>

@code {
    private record ChangedFile(string Path, string Change);

    private readonly List<ChangedFile> objFiles =
    [
        new("src/Program.cs", "modified"),
        new("src/Order.cs", "added"),
    ];

    private IReadOnlyCollection<ChangedFile> objChosen = Array.Empty<ChangedFile>();

    private Task CommitAsync() => Task.CompletedTask;
}
```

`SelectedCount` is a read-only property on the grid giving the number of chosen rows across every
page — handy for labelling your own button ("Commit 3 files"). `SelectedItems.Count` gives the
same number whenever you have bound `SelectedItems`. A complete page using this is in §10, "CRUD
Page with DataTable + Dialog". Live example: `/components/datatable`.

When the grid holds more rows than one page, the choose-all cell is a menu offering "select all on
this page" and "select all N" rather than a plain box. Its accessible name carries the state —
"No rows selected, 500 in all — choose rows", "3 of 500 rows selected — choose rows", "All 500
rows selected — choose rows" — and a polite live region announces the count when it changes, so
the state is not sighted-only.

> **BEHAVIOUR CHANGE — `ShowPagination="false"` now renders every row.**
> It was previously a chrome-only switch: the page window was applied unconditionally, so a grid with
> pagination turned off silently rendered just the first `InitialPageSize` rows (5 by default) and
> dropped the rest, with no pager and no row count to give the truncation away. It is now a **data**
> switch — the whole filtered/sorted sequence is materialised and `InitialPageSize` is ignored. If you
> set `ShowPagination="false"` over a large collection and were unknowingly relying on the old
> truncation, you will now get the entire collection; cap it yourself (`Data="@items.Take(50)"`) or
> leave pagination on.

> **`ShowHeader="false"` suppresses only the rendered `<thead>`.** Column metadata comes from the
> cascaded `Columns` fragment, so `DataTableColumn` registration, sorting, filtering and
> column-visibility all continue to work exactly as before. The consequence worth knowing: with no
> header rendered there is **no click target left for sorting**, so `Sortable` columns become
> sortable only through code. Use it for key/value style tables.

> **`Density`** (`DataTableDensity.Comfortable` | `DataTableDensity.Compact`) sets the cell padding:
>
> | Density | Header cell | Body cell |
> |---------|-------------|-----------|
> | `Comfortable` (default) | `h-12 px-4` | `p-4` |
> | `Compact` | `h-9 px-2.5` | `px-2.5 py-2` |
>
> `Compact` reclaims a large share of the width on narrow grids. The density classes are merged
> **first**, so a column's own `CellClass` / `HeaderClass` still overrides the density padding — they
> are merged last and win.
>
> ```razor
> <DataTable TData="Setting" Data="@settings" ShowHeader="false"
>            Density="DataTableDensity.Compact" ShowPagination="false">
>     <Columns>
>         <DataTableColumn TData="Setting" TValue="string"
>                          Property="@(s => s.Key)" Header="Key" CellClass="whitespace-nowrap font-medium" />
>         <DataTableColumn TData="Setting" TValue="string"
>                          Property="@(s => s.Value)" Header="Value" />
>     </Columns>
> </DataTable>
> ```

> **Mutating a row in place.** The grid skips re-rendering while its `Data` reference is unchanged,
> which is what keeps a large table cheap. If you set a property on a bound item rather than
> replacing the collection (`comment.Status = "Approved"`), the cell keeps showing the old value and
> a page-level `StateHasChanged()` cannot fix it. Either reassign `Data`, or capture the grid with
> `@ref` and call `Refresh()`:
>
> ```razor
> <DataTable TData="CommentViewModel" Data="@objComments" @ref="objGrid"> ... </DataTable>
>
> comment.Status = "Approved";
> objGrid?.Refresh();
> ```

#### DataTableColumn

Every one of these is honoured — the grid reads them when it registers and renders the column.
`TData` must match the parent `DataTable`'s `TData`; `TValue` is the type `Property` returns.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TData | type param | required | Data item type (`class`); must match the parent `DataTable` |
| TValue | type param | required | Type returned by `Property` |
| Id | string? | null | Stable column key. When null it is generated from `Header` (lower-cased, spaces → `-`). Set it explicitly if two columns could produce the same slug, or if you persist column-visibility state. |
| Header | string | required | Header text (`EditorRequired`) |
| Property | Func<TData, TValue?> | required | Type-safe value accessor (`EditorRequired`), e.g. `@(p => p.Name)` |
| Format | string? | null | Format string applied to the cell value |
| Sortable | bool | false | Enables click-to-sort on this column's header |
| Filterable | bool | false | Includes this column in the toolbar's search filter |
| Visible | bool | **true** | Initial visibility; the column-visibility toolbar toggles it |
| Width | string? | null | CSS length for the column, e.g. `"200px"`, `"20%"`, `"auto"` |
| MinWidth | string? | null | CSS length, e.g. `"120px"`. Emitted as an inline `min-width` on the column. |
| MaxWidth | string? | null | CSS length, e.g. `"400px"`. Prevents an over-wide column. |
| CellTemplate | RenderFragment<TData>? | null | Custom body-cell rendering; `context` is the row's `TData`. When null the value is rendered with `ToString()`. |
| Align | DataTableColumnAlign? | null | `Start`, `Center` or `End`. Aligns the header cell, the header's own **label box** and every body cell together — the one parameter a figure column needs |
| CellClass | string? | null | Extra CSS classes merged (via `cn`) onto every body cell in this column |
| HeaderClass | string? | null | Extra CSS classes merged (via `cn`) onto this column's header cell |

> **Right-align a figure column with `Align`, not with `text-right`.** The header label is rendered
> inside its own `flex` box, so a `text-right` in `HeaderClass` reaches the `<th>` and then has
> nothing left to move: the cells align and the header stays at the left of the column. `Align`
> puts the matching `justify-*` on that flex box and the matching `text-*` on the cells:
>
> ```razor
> <DataTableColumn TData="Rate" TValue="decimal"
>                  Property="@(r => r.InputPerMillion)" Header="Input"
>                  Align="DataTableColumnAlign.End" CellClass="tabular-nums" />
> ```
>
> `HeaderClass="text-right"` on its own is still honoured for markup written before `Align`
> existed — the grid reads the intent off the class and aligns the label box to match.

> **Identifier columns need `whitespace-nowrap`, or they soft-wrap with nothing to show for it.**
> The grid renders its `<table>` as `w-full` inside its own `relative w-full overflow-x-auto`
> wrapper. Because the table is `w-full` rather than intrinsically sized, CSS auto table-layout
> **compresses a column below the natural width of its own text before the wrapper ever scrolls**.
> A hyphenated or dotted identifier like `fix-issues` or `REQ-UI-007` is a legal break opportunity,
> so it silently soft-wraps onto two lines. Nothing in the DOM, the build, or the console indicates
> anything is wrong — the value is intact, it just reads as two words.
>
> The recipe for any identifier, code, SKU, or slug column:
>
> ```razor
> <DataTableColumn TData="Issue" TValue="string"
>                  Property="@(i => i.Key)" Header="Key" Sortable
>                  CellClass="whitespace-nowrap" HeaderClass="whitespace-nowrap" />
> ```
>
> `whitespace-nowrap` stops the break, which forces the column to its natural width, which finally
> makes the wrapper's `overflow-x-auto` do its job. Pair it with **`DataTable.MinWidth`** (see the
> `DataTable` table above) on grids of five columns or more: `MinWidth` gives the whole table a
> floor so the *remaining* columns are not squeezed away, while `CellClass`/`HeaderClass` protect
> the one column whose content must never break. Column-level `MinWidth` is the narrower tool when
> only one column needs a floor.

```razor
<DataTable TData="Person" Data="@people" SelectionMode="DataTableSelectionMode.Multiple">
    <Columns>
        <DataTableColumn TData="Person" TValue="string"
                         Property="@(p => p.Name)" Header="Name" Sortable Filterable />
        <DataTableColumn TData="Person" TValue="int"
                         Property="@(p => p.Age)" Header="Age" Sortable />
        <DataTableColumn TData="Person" TValue="string"
                         Property="@(p => p.Email)" Header="Email" Filterable
                         CellClass="whitespace-nowrap" HeaderClass="whitespace-nowrap" />
        <DataTableColumn TData="Person" TValue="string"
                         Property="@(p => p.Status)" Header="Status" Width="120px">
            <CellTemplate Context="person">
                <Badge Variant="@(person.Status == "Active" ? BadgeVariant.Default : BadgeVariant.Destructive)">
                    @person.Status
                </Badge>
            </CellTemplate>
        </DataTableColumn>
    </Columns>
</DataTable>

@code {
    record Person(string Name, int Age, string Email, string Status);
    List<Person> people = new() { new("Alice", 30, "alice@test.com", "Active") };
}
```

### Progress

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Value | double | 0 | Current progress value |
| Max | double | 100 | Maximum value |
| Class | string? | null | Container CSS classes |
| IndicatorClass | string? | null | Indicator CSS classes |

```razor
<Progress Value="66" />
<Progress Value="33" Class="h-2" IndicatorClass="bg-destructive" />
```

### Skeleton

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Shape | SkeletonShape | Rectangular | Rectangular or Circular |
| Class | string? | null | Dimensions CSS classes |

```razor
<div class="flex items-center space-x-4">
    <Skeleton Shape="SkeletonShape.Circular" Class="h-12 w-12" />
    <div class="space-y-2">
        <Skeleton Class="h-4 w-[250px]" />
        <Skeleton Class="h-4 w-[200px]" />
    </div>
</div>
```

### Spinner

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Size | SpinnerSize | Default | Small (h-4), Default (h-6), Large (h-10) |
| AriaLabel | string? | "Loading" | Accessible label |
| Class | string? | null | Additional CSS classes |

```razor
<Spinner />
<Spinner Size="SpinnerSize.Large" Class="text-primary" />
```

### Kbd

```razor
<Kbd>Ctrl</Kbd> + <Kbd>C</Kbd>
```

### Typography

Every Typography component takes a `Size` (`TypographySize.Xs` … `Xl6`, default `Default`). Use it
rather than passing a font-size utility through `Class`: `Size` **replaces** the component's own
size classes — including the responsive step `TypographyH1` carries (`text-4xl lg:text-5xl`) —
whereas a class passed through `Class` lands on the same element with the same specificity, so which
one wins is decided by the order Tailwind happens to emit them in.

```razor
<TypographyH1 Size="TypographySize.Xl2">Card heading</TypographyH1>
```

```razor
<TypographyH1>Heading 1</TypographyH1>
<TypographyH2>Heading 2</TypographyH2>
<TypographyH3>Heading 3</TypographyH3>
<TypographyH4>Heading 4</TypographyH4>
<TypographyP>Paragraph text</TypographyP>
<TypographyLead>Lead paragraph</TypographyLead>
<TypographyLarge>Large text</TypographyLarge>
<TypographySmall>Small text</TypographySmall>
<TypographyMuted>Muted text</TypographyMuted>
<TypographyInlineCode>code</TypographyInlineCode>
```

### Empty

`Empty` ships a **flat** API — `Title` / `Description` strings, an `Icon` render fragment, a `Size`
(`EmptySize.Small` / `Default` / `Large`), and `ChildContent` for actions. There is **no**
`EmptyIcon` / `EmptyTitle` / `EmptyDescription` / `EmptyAction` sub-component family. When you
supply both an `Icon` fragment and action content, name **both** fragments explicitly (`Icon` +
`ChildContent`) — mixing an explicit fragment with loose child content is a Razor compile error.

```razor
<Empty Title="No results found"
       Description="Try adjusting your search criteria."
       Size="EmptySize.Default">
    <Icon><LucideIcon Name="inbox" Size="48" /></Icon>
    <ChildContent>
        <Button>Create New</Button>
    </ChildContent>
</Empty>
```

### Item

```razor
<ItemGroup>
    <Item>
        <ItemMedia><Avatar Size="AvatarSize.Small"><AvatarFallback>JD</AvatarFallback></Avatar></ItemMedia>
        <ItemContent>
            <ItemTitle>John Doe</ItemTitle>
            <ItemDescription>Software Engineer</ItemDescription>
        </ItemContent>
        <ItemActions><Button Size="ButtonSize.IconSmall" Variant="ButtonVariant.Ghost"><LucideIcon Name="more-horizontal" Size="16" /></Button></ItemActions>
    </Item>
</ItemGroup>
```

---

## 7. Feedback & Overlay Components

### Alert

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Variant | AlertVariant | Default | Default, Success, Info, Warning, Danger |
| AccentBorder | bool | false | Show thick left border |
| Icon | RenderFragment? | null | Icon content |
| Class | string? | null | Additional CSS classes |

Sub-components: `AlertTitle`, `AlertDescription`, `AlertIcon`

```razor
<Alert Variant="AlertVariant.Default">
    <AlertTitle>Heads up!</AlertTitle>
    <AlertDescription>You can add components to your app.</AlertDescription>
</Alert>

<!-- With an icon — THREE supported routes, all valid on 2.1.0. See the note below the block. -->

<!-- 1. Positional: icon as the first loose child. Alert's CSS positions the first child <svg>. -->
<Alert Variant="AlertVariant.Danger" AccentBorder="true">
    <LucideIcon Name="alert-circle" Size="16" />
    <AlertTitle>Error</AlertTitle>
    <AlertDescription>Something went wrong.</AlertDescription>
</Alert>

<!-- 2. <AlertIcon> wrapper — renders no element of its own, so it mixes freely with AlertTitle. -->
<Alert Variant="AlertVariant.Info">
    <AlertIcon><LucideIcon Name="info" Size="16" /></AlertIcon>
    <AlertTitle>Heads up!</AlertTitle>
    <AlertDescription>Important message.</AlertDescription>
</Alert>

<!-- 3. Icon parameter. Because Icon is a named fragment, the rest of the content must be
     wrapped in an explicit <ChildContent> tag — a named fragment cannot sit next to loose
     content. This is legal Razor, not a workaround. -->
<Alert Variant="AlertVariant.Warning">
    <Icon><LucideIcon Name="triangle-alert" Size="16" /></Icon>
    <ChildContent>
        <AlertTitle>Careful</AlertTitle>
        <AlertDescription>This action affects billing.</AlertDescription>
    </ChildContent>
</Alert>

<Alert Variant="AlertVariant.Success">
    <AlertTitle>Success</AlertTitle>
    <AlertDescription>Changes saved.</AlertDescription>
</Alert>
```

> **Alert icons — all three routes work on 2.1.0.** Earlier revisions of this document instructed
> readers *not* to use an icon fragment alongside `AlertTitle`/`AlertDescription`, calling it an
> RZ10012 compile error. That is **false**, and it contradicted this section's own parameter table,
> which has always listed `Icon | RenderFragment?`. `Icon` is rendered immediately before
> `ChildContent` inside the alert root (`Alert.razor`). The RZ10012 error only occurs if you leave
> the title/description as *loose* content next to the named `Icon` fragment — wrapping them in
> `<ChildContent>` is the fix.
>
> - **`<AlertIcon>`** is the least fussy route. It renders `@ChildContent` with **no wrapper element
>   at all** unless you supply attributes (in which case it wraps in a `<span>` so the attributes
>   have somewhere to land), so it composes freely with loose `AlertTitle`/`AlertDescription`.
> - **The positional route** (icon as the first loose child) remains fully supported and is not
>   deprecated: `Alert`'s class list always includes the icon-positioning rules
>   (`[&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 [&:has(svg)]:pl-11`) specifically so that a
>   loose `<svg>` in any position in the tree is placed correctly. Those rules are no-ops when no
>   SVG is present, so they cost nothing on icon-less alerts.
> - **The `Icon` parameter** gives the icon an explicit, named home — use it when the icon markup is
>   generated in `@code` or shared across alerts.

### AlertDialog

Sub-components: `AlertDialogTrigger`, `AlertDialogContent`, `AlertDialogHeader`, `AlertDialogTitle`,
`AlertDialogDescription`, `AlertDialogFooter`, `AlertDialogCancel`, `AlertDialogAction`

```razor
<AlertDialog>
    <AlertDialogTrigger>
        <Button Variant="ButtonVariant.Destructive">Delete Account</Button>
    </AlertDialogTrigger>
    <AlertDialogContent>
        <AlertDialogHeader>
            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
            <AlertDialogDescription>
                This action cannot be undone.
            </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction>Continue</AlertDialogAction>
        </AlertDialogFooter>
    </AlertDialogContent>
</AlertDialog>
```

#### AlertDialogContent

`AlertDialogContent` renders the visible panel (through `DialogPortal`, `role="alertdialog"`);
`AlertDialog` itself renders no element, so `Class`, `id` and `data-*` belong here.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| ChildContent | RenderFragment? | null | Header / footer / body content |
| CloseOnEscape | bool | **true** | **NEW.** Escape cancels the alert dialog |
| Modal | bool | true | **NEW.** Overlay behaviour only: `true` = clicking the dimmed backdrop does **not** dismiss. Set `false` to allow outside-click dismissal |
| OnEscapeKeyDown | EventCallback<KeyboardEventArgs> | - | **NEW.** Invoked when Escape is pressed, before any dismissal |
| Class | string? | null | Merged via `cn()` over the built-in class list |
| AdditionalAttributes | - | - | Unmatched attributes are splatted onto the panel |

> **TWO BEHAVIOUR CHANGES — read before upgrading.**
> 1. **AlertDialog now closes on Escape by default.** It previously never did, at any setting. If your
>    confirmation must be answered by an explicit action, set `CloseOnEscape="false"` — and then make
>    sure a visible `AlertDialogCancel` is always reachable.
> 2. **AlertDialog no longer closes on an overlay click by default.** This matches shadcn/ui: a
>    confirmation is answered, not clicked away. Opt back in with `Modal="false"`.
>
> The two switches are independent: `Modal` covers overlay clicks only, `CloseOnEscape` covers the key.
> Escape is observed on the document, so it still fires after the body re-renders and focus has left
> the panel (for example once a validation message appeared).
>
> ```razor
> @* Cannot be dismissed by Escape or by clicking away — Cancel/Continue only *@
> <AlertDialogContent CloseOnEscape="false">…</AlertDialogContent>
>
> @* Both light-dismissal routes enabled *@
> <AlertDialogContent Modal="false">…</AlertDialogContent>
> ```
>
> Note this `Modal` is **not** the same parameter as `Dialog.Modal`: `AlertDialog` pins the underlying
> primitive to `Modal="true"` and `AlertDialogContent.Modal` governs the overlay alone, whereas
> `Dialog.Modal` is a master switch over every form of light dismissal (see §Dialog).

### Dialog

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Open | bool? | null | Controlled open state |
| OpenChanged | EventCallback<bool> | - | Two-way: @bind-Open |
| DefaultOpen | bool | false | Default open (uncontrolled) |
| Modal | bool | true | Master switch over light dismissal. **It can only remove dismissal, never add it** — see below |

> **BEHAVIOUR CHANGE — `Dialog.Modal` is now live.** It used to be a dead parameter that nothing
> read, so setting it had no effect at all. It is now published onto the dialog context and both
> `DialogOverlay` and `DialogContent` consult it, as a **master switch that can only take dismissal
> away**: `Modal="false"` disables outside-click **and** Escape outright (the dialog can then only be
> closed by a `DialogClose` or by your own code), while the per-part `CloseOnClick` /
> `CloseOnEscape` remain the local switches. Overlay click closes only when
> `CloseOnClick && Modal`; Escape closes only when `CloseOnEscape && Modal`. So `Modal="false"` with
> `CloseOnEscape="true"` still does **not** close on Escape. If you previously passed `Modal="false"`
> expecting it to be ignored, the dialog will now stop light-dismissing.

Sub-components: `DialogTrigger`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogBody`, `DialogFooter`, `DialogClose`

#### DialogContent

`DialogContent` is the part that renders the visible dialog box; `Dialog` itself renders no element
(see "Guarantees you can rely on"), so `Class`, `id`, `data-*` and event handlers belong here.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| ChildContent | RenderFragment? | null | Dialog body content |
| Class | string? | null | Merged via `cn()` over the built-in class list — see the width note below |
| ShowClose | bool | true | Render the top-right X close button |
| CloseOnEscape | bool | true | Escape closes the dialog |
| TrapFocus | bool | true | Keep keyboard focus inside the dialog while open |
| LockScroll | bool | true | Lock body scroll while open |
| OnEscapeKeyDown | EventCallback<KeyboardEventArgs> | - | Invoked when Escape is pressed |
| MaxHeight | string | `"max-h-[calc(100vh-2rem)]"` | **NEW.** Tailwind max-height utility capping the panel inside the viewport. The class **must already exist in the pre-built `trblazeui.css`** — an arbitrary value that was never emitted fails silently and the panel becomes unbounded. Known to ship: the `max-h-*` scale (`max-h-96`, `max-h-screen`, …) plus `max-h-[70vh]`, `max-h-[300px]`, `max-h-[400px]` and the default |
| AdditionalAttributes | - | - | Unmatched attributes are splatted onto the dialog box |

**Default width is `max-w-lg`**, inside `fixed inset-0 z-50 m-auto flex flex-col h-fit w-full` with
`max-h-[calc(100vh-2rem)] overflow-y-auto` (a tall dialog scrolls internally rather than
overflowing the viewport). The panel is a **column flex box**, which is what lets a `DialogBody`
shrink and scroll while the header and footer stay pinned.

#### DialogBody

**NEW sub-component.** `DialogBody` is the scrolling middle region of the panel: put it between
`DialogHeader` and `DialogFooter` and only that region scrolls, so the header, the footer and the
top-right close button stay pinned while long content moves under them.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| ChildContent | RenderFragment? | null | The scrollable content, between the header and the footer |
| Class | string? | null | Merged via `cn()` over the built-in `min-h-0 overflow-y-auto` |
| AdditionalAttributes | - | - | Unmatched attributes are splatted onto the body `<div>` |

> **Backward compatible — omitting `DialogBody` is still valid and unchanged.** Without one, nothing
> in the panel can shrink, so `DialogContent`'s own `overflow-y-auto` takes over and **the whole panel
> scrolls exactly as it did before** (header and footer scroll away with the content). Add
> `DialogBody` only when you want the header/footer pinned. Pair it with `MaxHeight` to control where
> the scrolling starts.
>
> ```razor
> <DialogContent MaxHeight="max-h-[70vh]">
>     <DialogHeader><DialogTitle>Terms</DialogTitle></DialogHeader>
>     <DialogBody>
>         @* long content — only this scrolls *@
>     </DialogBody>
>     <DialogFooter><Button>Accept</Button></DialogFooter>
> </DialogContent>
> ```

> **To widen a dialog, pass a `max-w-*` through `Class` — it works.**
> `DialogContent` composes its classes with `ClassNames.cn(...)` and `Class` is the **last**
> argument. `TailwindMerge` puts every `max-w-*` utility in the same `max-width` conflict group, so
> the caller's value **evicts** the built-in `max-w-lg` rather than fighting it on specificity.
>
> ```razor
> <DialogContent Class="max-w-3xl">…</DialogContent>
> ```
>
> The full wide scale — `max-w-xl`, `max-w-2xl`, `max-w-3xl`, `max-w-4xl`, `max-w-5xl`,
> `max-w-6xl`, `max-w-7xl` — **ships in `trblazeui.css` as of 2.1.0.** It did **not** ship in
> 2.0.0, which is why consumers on that version concluded the only available widths were the
> `sm`/`md`/`lg`/none steps and gave up. Note this is `Class`, not lowercase `class`: lowercase
> would splat past `cn()` and wipe out the positioning, padding, and animation classes as well
> (see "`Class` vs `class`" near the top of this document).

> **Scoped CSS and `::deep` can NEVER reach a dialog. This is by design and will not change.**
> `DialogContent` renders through `<DialogPortal>` into `div.trblazeui-portal`, appended directly
> under `<body>` by `PortalHost`. The dialog's DOM therefore lives **outside the calling
> component's subtree**, so the calling component's scoped-CSS identifier (`b-xxxxxxxxxx`) is never
> stamped on it and no rule in `MyPage.razor.css` can match it. **`::deep` does not help** — `::deep`
> still requires an ancestor element that carries the component's scope identifier, and the portal
> has none. Style a dialog through one of these instead:
> - the `Class` parameter with Tailwind utilities (the normal route);
> - a global stylesheet (`app.css` / `theme.css`), which has no scoping to defeat;
> - a theme token override, since the dialog's colours come from CSS variables.

```razor
<Dialog>
    <DialogTrigger AsChild>
        <Button>Edit Profile</Button>
    </DialogTrigger>
    <DialogContent>
        <DialogHeader>
            <DialogTitle>Edit profile</DialogTitle>
            <DialogDescription>Make changes to your profile.</DialogDescription>
        </DialogHeader>
        @* DialogBody scrolls on its own; DialogHeader, DialogFooter and the X stay pinned.
           Drop it and the panel scrolls as a whole instead — both forms are supported. *@
        <DialogBody>
            <div class="grid gap-4 py-4">
                <div class="grid grid-cols-4 items-center gap-4">
                    <Label For="name" Class="text-right">Name</Label>
                    <Input Id="name" @bind-Value="name" Class="col-span-3" />
                </div>
            </div>
        </DialogBody>
        <DialogFooter>
            <DialogClose><Button Variant="ButtonVariant.Outline">Cancel</Button></DialogClose>
            <Button>Save changes</Button>
        </DialogFooter>
    </DialogContent>
</Dialog>
```

### Drawer

```razor
<Drawer>
    <DrawerTrigger><Button>Open Drawer</Button></DrawerTrigger>
    <DrawerContent>
        <DrawerHeader>
            <DrawerTitle>Edit settings</DrawerTitle>
            <DrawerDescription>Configure your preferences.</DrawerDescription>
        </DrawerHeader>
        <div class="p-4">Content here</div>
        <DrawerFooter>
            <Button>Save</Button>
            <DrawerClose><Button Variant="ButtonVariant.Outline">Cancel</Button></DrawerClose>
        </DrawerFooter>
    </DrawerContent>
</Drawer>
```

### Sheet

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Open | bool? | null | Controlled open state |
| OpenChanged | EventCallback<bool> | - | Two-way: @bind-Open |
| Side | SheetSide | Right | Top, Right, Bottom, Left |
| Modal | bool | true | Dismiss on outside click/Escape |

```razor
<Sheet>
    <SheetTrigger><Button>Open Sheet</Button></SheetTrigger>
    <SheetContent>
        <SheetHeader>
            <SheetTitle>Sheet Title</SheetTitle>
            <SheetDescription>Sheet description text.</SheetDescription>
        </SheetHeader>
        <div class="py-4">Content here</div>
        <SheetFooter>
            <SheetClose><Button>Close</Button></SheetClose>
        </SheetFooter>
    </SheetContent>
</Sheet>
```

### Toast

**Setup required:** Register `ToastService` in DI and add `<ToastProvider />` in layout.

| ToastProvider Parameter | Type | Default | Description |
|------------------------|------|---------|-------------|
| Position | ToastPosition | BottomRight | TopRight, TopLeft, TopCenter, BottomRight, BottomLeft, BottomCenter |
| MaxToasts | int | 5 | Max visible toasts |

```razor
@inject ToastService ToastService

<Button OnClick="@(() => ToastService.Show("Your file has been saved.", "Success"))">
    Show Toast
</Button>

<Button OnClick="@(() => ToastService.Error("Something went wrong.", "Error"))">
    Error Toast
</Button>

@code {
    // ToastService methods:
    // .Show(description, title?, variant?, duration?)
    // .Success(description, title?)
    // .Error(description, title?)
    // .Dismiss(id)
    // .DismissAll()
}
```

### Toolbar

Action toolbar for desktop/hybrid applications with grouped buttons, separators, and controls.

**Import:** `@using TrBlazeUI.Components.Toolbar`

#### Basic Toolbar

```razor
<Toolbar AriaLabel="File actions">
    <ToolbarGroup>
        <ToolbarButton AriaLabel="New file" OnClick="HandleNew">
            <LucideIcon Name="file-plus" Size="16" />
        </ToolbarButton>
        <ToolbarButton AriaLabel="Open file" OnClick="HandleOpen">
            <LucideIcon Name="folder-open" Size="16" />
        </ToolbarButton>
        <ToolbarButton AriaLabel="Save" OnClick="HandleSave">
            <LucideIcon Name="save" Size="16" />
        </ToolbarButton>
    </ToolbarGroup>
    <ToolbarSeparator />
    <ToolbarGroup>
        <ToolbarButton AriaLabel="Undo" OnClick="HandleUndo">
            <LucideIcon Name="undo-2" Size="16" />
        </ToolbarButton>
        <ToolbarButton AriaLabel="Redo" OnClick="HandleRedo">
            <LucideIcon Name="redo-2" Size="16" />
        </ToolbarButton>
    </ToolbarGroup>
</Toolbar>
```

#### Toolbar Variants

```razor
<Toolbar Variant="ToolbarVariant.Default" AriaLabel="Default toolbar">...</Toolbar>
<Toolbar Variant="ToolbarVariant.Compact" AriaLabel="Compact toolbar">...</Toolbar>
<Toolbar Variant="ToolbarVariant.Dense" AriaLabel="Dense toolbar">...</Toolbar>
```

#### Toggle Buttons (Formatting Toolbar)

```razor
<Toolbar AriaLabel="Text formatting">
    <ToolbarGroup>
        <ToolbarToggleButton AriaLabel="Bold" @bind-IsPressed="objIsBold">
            <LucideIcon Name="bold" Size="16" />
        </ToolbarToggleButton>
        <ToolbarToggleButton AriaLabel="Italic" @bind-IsPressed="objIsItalic">
            <LucideIcon Name="italic" Size="16" />
        </ToolbarToggleButton>
        <ToolbarToggleButton AriaLabel="Underline" @bind-IsPressed="objIsUnderline">
            <LucideIcon Name="underline" Size="16" />
        </ToolbarToggleButton>
    </ToolbarGroup>
</Toolbar>
```

#### With Dropdown Menus

```razor
<Toolbar AriaLabel="IDE toolbar">
    <ToolbarGroup>
        <DropdownMenu>
            <DropdownMenuTrigger>
                <ToolbarButton AriaLabel="Build configuration">
                    Debug
                    <LucideIcon Name="chevron-down" Size="14" />
                </ToolbarButton>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
                <DropdownMenuItem>Debug</DropdownMenuItem>
                <DropdownMenuItem>Release</DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    </ToolbarGroup>
    <ToolbarSeparator />
    <ToolbarGroup>
        <ToolbarButton AriaLabel="Start debugging" Class="text-green-600">
            <LucideIcon Name="play" Size="16" />
        </ToolbarButton>
    </ToolbarGroup>
</Toolbar>
```

#### Vertical Toolbar

```razor
<Toolbar Vertical AriaLabel="Side tools">
    <ToolbarButton AriaLabel="Select">
        <LucideIcon Name="mouse-pointer" Size="16" />
    </ToolbarButton>
    <ToolbarButton AriaLabel="Move">
        <LucideIcon Name="move" Size="16" />
    </ToolbarButton>
    <ToolbarSeparator Vertical="false" />
    <ToolbarButton AriaLabel="Zoom in">
        <LucideIcon Name="zoom-in" Size="16" />
    </ToolbarButton>
</Toolbar>
```

#### Toolbar API

| Component | Key Parameters |
|-----------|---------------|
| `Toolbar` | `Variant` (Default/Compact/Dense), `Vertical` (bool), `AriaLabel`, `Class` |
| `ToolbarGroup` | `AriaLabel`, `Class` |
| `ToolbarButton` | `Variant` (Default/Ghost/Outline), `OnClick`, `Disabled`, `AriaLabel`, `Title`, `Class` |
| `ToolbarToggleButton` | `@bind-IsPressed`, `Disabled`, `AriaLabel`, `Title`, `Class` |
| `ToolbarSeparator` | `Vertical` (bool, default true), `Class` |

### Tooltip

```razor
<Tooltip>
    <TooltipTrigger>
        <Button Variant="ButtonVariant.Outline">Hover me</Button>
    </TooltipTrigger>
    <TooltipContent>
        <p>Tooltip text here</p>
    </TooltipContent>
</Tooltip>
```

TooltipContent parameters: `Side` (PopoverSide: Top, Bottom, Left, Right), `Align` (PopoverAlign: Start, Center, End), `Offset` (int, default 8).

### HoverCard

```razor
<HoverCard>
    <HoverCardTrigger>
        <Button Variant="ButtonVariant.Link">@username</Button>
    </HoverCardTrigger>
    <HoverCardContent>
        <div class="flex space-x-4">
            <Avatar><AvatarFallback>JD</AvatarFallback></Avatar>
            <div><p class="text-sm font-semibold">John Doe</p></div>
        </div>
    </HoverCardContent>
</HoverCard>
```

### Popover

```razor
<Popover>
    <PopoverTrigger>
        <Button Variant="ButtonVariant.Outline">Open Popover</Button>
    </PopoverTrigger>
    <PopoverContent Class="w-80">
        <div class="grid gap-4">
            <h4 class="font-medium leading-none">Dimensions</h4>
            <div class="grid gap-2">
                <Label For="width">Width</Label>
                <Input Id="width" @bind-Value="width" />
            </div>
        </div>
    </PopoverContent>
</Popover>
```

### ContextMenu

```razor
<ContextMenu>
    <ContextMenuTrigger Class="flex h-[150px] w-[300px] items-center justify-center rounded-md border border-dashed">
        Right click here
    </ContextMenuTrigger>
    <ContextMenuContent Class="w-64">
        <ContextMenuItem>Back</ContextMenuItem>
        <ContextMenuItem>Forward</ContextMenuItem>
        <ContextMenuSeparator />
        <ContextMenuItem>Reload</ContextMenuItem>
    </ContextMenuContent>
</ContextMenu>
```

### DropdownMenu

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Open | bool? | null | Controlled open state |
| OpenChanged | EventCallback<bool> | - | Two-way: @bind-Open |
| Modal | bool | true | Dismiss on outside click/Escape |

Sub-components: `DropdownMenuTrigger`, `DropdownMenuContent`, `DropdownMenuItem`, `DropdownMenuCheckboxItem`, `DropdownMenuRadioItem`, `DropdownMenuRadioGroup`, `DropdownMenuLabel`, `DropdownMenuSeparator`, `DropdownMenuGroup`, `DropdownMenuShortcut`

```razor
<DropdownMenu>
    <DropdownMenuTrigger>
        <Button Variant="ButtonVariant.Outline">Open</Button>
    </DropdownMenuTrigger>
    <DropdownMenuContent Class="w-56">
        <DropdownMenuLabel>My Account</DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem>
            <LucideIcon Name="user" Size="16" />
            <span class="ml-2">Profile</span>
        </DropdownMenuItem>
        <DropdownMenuItem>
            <LucideIcon Name="settings" Size="16" />
            <span class="ml-2">Settings</span>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem>
            <LucideIcon Name="log-out" Size="16" />
            <span class="ml-2">Log out</span>
        </DropdownMenuItem>
    </DropdownMenuContent>
</DropdownMenu>
```

### Command (Command Palette)

```razor
<Command>
    <CommandInput Placeholder="Type a command..." />
    <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>
        <CommandGroup Heading="Suggestions">
            <CommandItem>Calendar</CommandItem>
            <CommandItem>Search</CommandItem>
        </CommandGroup>
        <CommandSeparator />
        <CommandGroup Heading="Settings">
            <CommandItem>Profile<CommandShortcut>Ctrl+P</CommandShortcut></CommandItem>
            <CommandItem>Settings<CommandShortcut>Ctrl+S</CommandShortcut></CommandItem>
        </CommandGroup>
    </CommandList>
</Command>
```

---

## 8. Rich Content Components

### Carousel

```razor
<Carousel>
    <CarouselContent>
        <CarouselItem>
            <div class="p-1">
                <Card><CardContent Class="flex aspect-square items-center justify-center p-6">
                    <span class="text-4xl font-semibold">1</span>
                </CardContent></Card>
            </div>
        </CarouselItem>
        <CarouselItem>
            <div class="p-1">
                <Card><CardContent Class="flex aspect-square items-center justify-center p-6">
                    <span class="text-4xl font-semibold">2</span>
                </CardContent></Card>
            </div>
        </CarouselItem>
    </CarouselContent>
    <CarouselPrevious />
    <CarouselNext />
</Carousel>
```

### Chart (Blazor-ApexCharts wrapper)

Chart types: `AreaChart`, `BarChart`, `LineChart`, `PieChart`, `RadarChart`, `RadialChart`.

The chart family wraps **Blazor-ApexCharts**, so `@using ApexCharts` is required (it is in the §1
import block). `ChartContainer` is optional.

**You never have to drop the wrapper to change an option.** `Options` takes a real
`ApexChartOptions<TItem>`, merged over the wrapper's own defaults — so a chart can be made to match
a design (no axis, no gridlines, a compact value above each bar) without giving up `Items`/`XValue`,
the theme colours, or the empty state. See *Steering a chart with `Options`* below.

All six types inherit `ChartBase<TItem> where TItem : class`:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TItem | type param | required | Data item type (`class`) |
| Items | IEnumerable<TItem>? | null | Data source for the built-in single series |
| XValue | Func<TItem, object>? | null | **NEW.** X-axis (category) accessor for the built-in series |
| YValue | Func<TItem, decimal?>? | null | **NEW.** Y-axis accessor for the built-in series |
| SeriesName | string? | null | **NEW.** Legend name of the built-in series; falls back to `Title`, then to `"Series"` |
| EmptyText | string? | null | **NEW.** Message shown in place of an empty chart canvas |
| Options | ApexChartOptions<TItem>? | null | **NEW.** Raw ApexCharts options merged **over** the wrapper's defaults. Anything you set wins; anything you leave null the wrapper fills in |
| OptionsConfigurator | Action<ApexChartOptions<TItem>>? | null | **NEW.** Runs last, after `Options` and every default — for values that must be computed, such as a data-label formatter |
| Config | ChartConfig? | null | Series label / colour map |
| Height | string | `"350px"` | Any CSS height |
| Width | string | `"100%"` | Any CSS width |
| Class | string? | null | Additional CSS classes on the container |
| ShowLegend | bool | true | Show the legend |
| LegendPosition | LegendPosition | Bottom | Top, Bottom, Left, Right, Hidden |
| ShowDataLabels | bool | false | Draw values on the chart elements. Works in both forms. In the shorthand, `Options.DataLabels.Enabled` and `OptionsConfigurator` reach the labels too. The configurator has the last word, and `true` here switches labels on even when `Options` brings its own `DataLabels` object, because `DataLabels.Enabled` is a plain `bool` and cannot say "not set". In the nested form, set `ShowDataLabels` on each `ApexPointSeries` you nest, because ApexCharts takes the label switch from the series |
| ShowTooltip | bool | true | Tooltip on hover |
| Title | string? | null | Chart title |
| EnableAnimations | bool | true | Set false for large datasets |
| ChildContent | RenderFragment? | null | Nested `ApexPointSeries` children |

**CORRECTION — there are now TWO supported forms.** Earlier revisions of this document said there is
no chart-level `XValue`/`YValue` and that series come only from nested `ApexPointSeries` children.
That is out of date:

1. **`Items` + `XValue` + `YValue` shorthand (single series)** — no child markup at all. Before this
   fix `Items` was read by nothing, so a chart given only `Items` painted a silent empty box.
2. **Nested `ApexPointSeries` children (multi-series)** — the existing form, unchanged.

**Children win.** When `ChildContent` is present the chart renders it and ignores `XValue`, `YValue`
and `SeriesName` entirely. The shorthand is used only when there is no child content **and** all three
of `Items`, `XValue`, `YValue` are non-null.

**Neither form → a visible empty state, not a blank box.** With no child content and an incomplete
`Items`/`XValue`/`YValue` triplet, every chart type renders a dashed placeholder
(`data-slot="chart-empty"`, `role="status"`) sized to the chart's own `Height`/`Width`, carrying
`EmptyText` or the default *"No chart series. Supply Items together with XValue and YValue, or nest
ApexPointSeries children."* A misconfigured chart is therefore visible rather than silent.

```razor
@using ApexCharts

@* Form 1 — shorthand, single series, no child markup *@
<BarChart TItem="SalesData" Items="@salesData" Height="280px" ShowLegend="false"
          XValue="@(d => d.Month)"
          YValue="@(d => (decimal?)d.Revenue)"
          SeriesName="Revenue"
          EmptyText="No sales in this period." />

@* Form 2 — nested children, two or more series (children win if both are supplied) *@
<BarChart TItem="SalesData" Items="@salesData" Height="280px">
    <ApexPointSeries TItem="SalesData"
                     Items="@salesData"
                     Name="Revenue"
                     SeriesType="SeriesType.Bar"
                     XValue="@(d => d.Month)"
                     YValue="@(d => (decimal?)d.Revenue)" />
    <ApexPointSeries TItem="SalesData"
                     Items="@salesData"
                     Name="Cost"
                     SeriesType="SeriesType.Bar"
                     XValue="@(d => d.Month)"
                     YValue="@(d => (decimal?)d.Cost)" />
</BarChart>
```

Series colours pick up `--chart-1` … `--chart-5`, which the library now ships defaults for in both
light and dark mode; override them in your own `theme.css` to brand the charts.

#### Steering a chart with `Options`

Give the chart an `ApexChartOptions<TItem>` and every member you set replaces the wrapper's default
for that member; everything you leave null the wrapper still fills in. So changing three options
costs three lines, not the wrapper.

```razor
@using ApexCharts

<ChartContainer Bare="true">
    <BarChart TItem="Total" Items="@objRows" Height="260px" ShowLegend="false" ShowDataLabels="true"
              XValue="@(r => r.Name)" YValue="@(r => (decimal?)r.Value)"
              Options="@objChartOptions"
              OptionsConfigurator="@(o => o.DataLabels.Formatter =
                  "function (v) { return (v / 1e6).toFixed(1) + 'M' }")" />
</ChartContainer>

@code {
    // A comparison chart the usual way: no y axis, no gridlines, the value above each bar.
    private readonly ApexChartOptions<Total> objChartOptions = new()
    {
        Grid = new Grid
        {
            Show = false,
            Xaxis = new GridXAxis { Lines = new Lines { Show = false } },
            Yaxis = new GridYAxis { Lines = new Lines { Show = false } }
        },
        Yaxis = [new YAxis { Show = false }]
    };
}
```

Two details worth knowing:

- **The chart's own parameters still win over `Options`** for what they express — `Variant` drives
  stacking and orientation, because that is the parameter you set to choose them. Use
  `OptionsConfigurator`, which runs after everything, to reach those.
- **The instance is filled in place**, the way `ApexChart` itself treats the options object it is
  given. Hold it in a field the component owns, not in a shared static.

#### ChartContainer

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Bare | bool | false | `true` drops the container's own card chrome — border, card background, shadow and `p-6` — and keeps only the flex column the chart sizes against |
| Class | string? | null | Additional CSS classes |

`ChartContainer` paints a shadcn Card by default. Inside a `Card` — which is where a chart usually
goes — that draws a **card inside a card**. Pass `Bare="true"` for a flush chart that takes its
surface from whatever it is placed in:

```razor
<Card>
    <CardHeader><CardTitle>Total tokens by harness</CardTitle></CardHeader>
    <CardContent>
        <ChartContainer Bare="true">
            <BarChart TItem="Total" Items="@objRows" ... />
        </ChartContainer>
    </CardContent>
</Card>
```

### Prose (rendered HTML you did not author)

Use `Prose` for Markdown output, a CMS body or any opaque HTML blob. It gives tables, `<pre>`
blocks, images and iframes their own overflow context, so a wide table scrolls inside itself
instead of pushing the whole page sideways at 390 px (WCAG 1.4.10 Reflow).

```razor
<Prose>@((MarkupString)renderedHtml)</Prose>
<Prose ConstrainWidth="false">@((MarkupString)previewHtml)</Prose>
```

### CodeBlock

Monochrome code block with an optional language label and a copy button. It deliberately bundles no
syntax highlighter; pass pre-highlighted markup through `Html` if you have one.

```razor
<CodeBlock Language="csharp" Code="@snippet" />
```

### DiffView — compare two texts side by side (before and after)

**This is the difference viewer.** Give it a before text and an after text; do not put two
`CodeBlock`s in a grid and colour the lines yourself. The difference between two texts, side by
side or inline, with line numbers. Added and removed
lines are tinted from the theme (`--success`, `--destructive`) and marked `+`/`-`; unchanged
stretches longer than `ContextLines` fold into a "Show N unchanged lines" button. The comparison
runs in .NET (`TextDiff`, Myers' algorithm) — no script library.

```razor
<DiffView Before="@oldText" After="@newText" Mode="mode"
          BeforeLabel="src/OrderService.cs" AfterLabel="src/OrderService.cs">
    <HunkActions>
        <Button Size="ButtonSize.Small" Variant="ButtonVariant.Ghost" OnClick="() => Accept(context)">Accept</Button>
    </HunkActions>
</DiffView>
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Before | string? | null | The old text |
| After | string? | null | The new text |
| Mode | DiffViewMode | SideBySide | `SideBySide` or `Inline` — pair it with a joined `ToggleGroup TValue="DiffViewMode"` for the switch |
| ShowLineNumbers | bool | true | Line numbers on both sides |
| ContextLines | int | 3 | Unchanged lines kept around each change; a negative value shows everything |
| IgnoreWhitespace | bool | false | Treat lines differing only in spacing as unchanged |
| BeforeLabel | string? | null | Name over the left side, e.g. the file path |
| AfterLabel | string? | null | Name over the right side |
| ShowHeader | bool | true | The names and the `+N -M` counts |
| HunkActions | RenderFragment<DiffHunk>? | null | Buttons beside each changed part; `context` is the `DiffHunk` |
| EmptyText | string | "No differences" | Shown when the two texts match |
| Class | string? | null | Additional CSS classes |

- `context` on `HunkActions` has `Index`, `Header` (`@@ -12,7 +12,9 @@`),
  `OldStart`/`OldCount`/`NewStart`/`NewCount`, `Lines`, `AddedCount`, `RemovedCount`.
- The rendered `DiffView` also exposes `Hunks` through `@ref`, if the page needs the same parts.
- Need the lines in code instead of on the screen? `TextDiff.Compare(before, after)` returns
  `DiffLine`s (`Kind`, `OldNumber`, `NewNumber`, `Text`) and `TextDiff.Hunks(lines, context)`
  groups them. `TextDiff` is a static class in `TrBlazeUI.Components.DiffView`.
- Live example: `/components/diff-view`.

### StatTile / StatGroup

A row of headline statistics — a large value over a small caption — so value/caption sizing does not
drift between pages.

```razor
<StatGroup Columns="4">
    <StatTile Value="20+" Label="Years of experience" />
    <StatTile Value="1,284" Label="Posts" Trend="+12% vs last month" TrendDirection="StatTrend.Up" />
</StatGroup>
```

The value and caption expose stable `data-slot="stat-tile-value"` and
`data-slot="stat-tile-label"` hooks.

### Timeline

```razor
<Timeline>
    <TimelineItem Meta="2022 - now" Title="Senior Engineer" Subtitle="Acme" Current="true" />
    <TimelineItem Meta="2019 - 2022" Title="Engineer" Subtitle="Acme" />
</Timeline>
```

`TimelineItem` also takes **`Status`** (`StepStatus?`, from `TrBlazeUI.Components.Stepper`) when an
entry needs to say what happened to it rather than only where it sits — see the Stepper section
below. `Current="true"` emits `aria-current="step"` and a visually hidden "(current entry)", so it
is not signalled by the marker's fill alone.

### Stepper

Numbered steps for a multi-part flow or a series index. The current step carries
`aria-current="step"`, so it is not signalled by colour alone.

```razor
<Stepper Current="2">
    <StepperItem Title="Draft" />
    <StepperItem Title="Review" Description="Editor sign-off" />
    <StepperItem Title="Publish" />
</Stepper>
```

#### A step that carries its own state

By default a step's state comes from its position relative to `Current`: everything before it is
finished, everything after is not started. That cannot draw a chain that paused in the middle, or
a step that succeeded only on a second try. Give a step **`Status`** and it says what happened to
it instead:

```razor
<Stepper Current="3" Orientation="StepperOrientation.Vertical" AriaLabel="Release pipeline">
    <StepperItem Title="Restore"  Status="StepStatus.Done">
        <Trailing><span class="text-xs text-muted-foreground">4s</span></Trailing>
    </StepperItem>
    <StepperItem Title="Build"    Status="StepStatus.Retried" Description="Succeeded on the second run" />
    <StepperItem Title="Test"     Status="StepStatus.Running" />
    <StepperItem Title="Sign off" Status="StepStatus.Waiting" Description="Needs the owner" />
    <StepperItem Title="Publish"  Status="StepStatus.Pending" />
</Stepper>
```

| Member | Type | Default | Description |
|---|---|---|---|
| `StepperItem.Status` | `StepStatus?` | `null` | `Pending`, `Running`, `Waiting`, `Done`, `Retried`, `Failed`. Leave it null to keep the position-derived behaviour. |
| `StepperItem.Trailing` | `RenderFragment?` | `null` | Content at the step's trailing edge — a duration, a badge, a link. |
| `Stepper.Orientation` | `StepperOrientation` | `Horizontal` | `Vertical` runs the steps top to bottom. |

Each status has its own glyph **and** its own accessible name (`Done` is `✓` named "Done",
`Failed` is `✕` named "Failed", and so on), so the state is never carried by colour alone.
`aria-current="step"` still follows `Stepper.Current`, not `Status`. `StepStatus` lives in
`TrBlazeUI.Components.Stepper`; `Timeline` reuses it, so a page using it on a `TimelineItem` needs
that `@using`.

### AnchorNav (in-page navigation with scrollspy)

AnchorNav intercepts fragment clicks, scrolls with `TopOffset`, and updates history using the
current path. This preserves a nested route even when the document declares `<base href="/">`.

```razor
<AnchorNav Sections="@objSections" @bind-ActiveId="objActiveSection" TopOffset="80" />

@code {
    private readonly List<AnchorNavSection> objSections =
    [
        new("summary", "Summary"),
        new("experience", "Experience"),
    ];
    private string? objActiveSection;
}
```

### SortableList — let the user set the order (move up / move down buttons)

**This is the ordered list.** Each row carries a move-up and a move-down button — **real buttons,
not drag-and-drop**, so it works with a keyboard, a screen reader and a touch screen, and every
move is announced. The first row's up button and the last row's down button are disabled. The list
owns the positions: it writes the reordered collection back through `@bind-Items`, so a fallback
chain, a priority order or a run order needs no reordering code of your own.

There is nothing to drag. If you were looking for dragging, this is still the control you want.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| TItem | type param | required | The item type |
| Items | IList<TItem> | [] | The list, in its current order |
| ItemsChanged | EventCallback<IList<TItem>> | - | Two-way: `@bind-Items`. Fires with the new order after every move |
| ItemTemplate | RenderFragment<TItem>? | null | How one row is drawn; `context` is the item |
| ItemLabel | Func<TItem, string>? | null | The row's name, used for the buttons' accessible names and the move announcement |
| ShowPosition | bool | **true** | The 1-based position shown on each row. See the behaviour note below. |
| AllowRemove | bool | false | Adds a remove button per row, named "Remove {item}" |
| OnRemove | EventCallback<TItem> | - | Raised after the row is taken out and `ItemsChanged` has fired |
| Class | string? | null | Additional CSS classes |

> **BEHAVIOUR CHANGE — `ShowPosition` defaults to `true`.**
> Every existing `SortableList` gains a position number on each row. Pass `ShowPosition="false"`
> to keep the previous look. The default is `true` because a list whose order matters is far
> easier to read when each row says where it is, which is why it was asked for.

Removing a row writes the shortened list back through `@bind-Items` before `OnRemove` fires, and
the removal is announced through the same live region as a move ("Cue sheet removed, 3 items
left"). The position is `aria-hidden` — an `<ol>` already conveys position to a screen reader, and
saying it twice is noise.

```razor
<SortableList TItem="ModelTier" @bind-Items="objChain" ItemLabel="@(t => t.Name)"
              AllowRemove="true" OnRemove="DropTierAsync">
    <ItemTemplate Context="tier">
        <span class="flex w-full items-center gap-2">
            <span class="font-medium">@tier.Name</span>
            <span class="text-muted-foreground">@tier.Provider</span>
        </span>
    </ItemTemplate>
</SortableList>

@code {
    private record ModelTier(string Name, string Provider);

    private IList<ModelTier> objChain =
    [
        new("Primary", "Anthropic"),
        new("Fallback", "OpenAI"),
        new("Last resort", "Local"),
    ];
}
```

Position numbers are yours to draw, as above — the list does not number the rows itself. Live
example: `/components/sortable-list`.

### PasswordStrength

```razor
<Input Type="InputType.Password" @bind-Value="objPassword" />
<PasswordStrength Value="@objPassword" />
```

### CenteredPanel

A vertically centred single-panel page — sign-in, email verification, 404.

```razor
<CenteredPanel Width="CenteredPanelWidth.Medium">
    <Card>...</Card>
</CenteredPanel>
```

### MarkdownEditor

```razor
<MarkdownEditor @bind-Value="markdownContent" />
```

### RichTextEditor

```razor
<RichTextEditor @bind-Value="htmlContent" />
```

---

## 9. Icon Libraries

### LucideIcon (Recommended)

```razor
@using TrBlazeUI.Icons.Lucide.Components
@using TrBlazeUI.Icons.Lucide.Data

<LucideIcon Name="home" Size="24" />
<LucideIcon Name="settings" Size="16" Color="currentColor" StrokeWidth="2" />
<LucideIcon Name="search" Size="20" Class="text-muted-foreground" />
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Name | string | required | Icon name (kebab-case, e.g., "chevron-right") |
| Size | int | 24 | Width/height in pixels |
| Color | string | "currentColor" | Stroke color |
| Fill | string | "none" | Fill color |
| StrokeWidth | string | "2" | Stroke width |
| Class | string? | null | Additional CSS classes |
| AriaLabel | string? | null | Accessible label |

Common icon names: `home`, `house`, `settings`, `user`, `search`, `mail`, `bell`, `calendar`, `folder`, `file`, `file-text`, `trash`, `pencil`, `plus`, `minus`, `x`, `check`, `chevron-right`, `chevron-left`, `chevron-up`, `chevron-down`, `chevrons-up-down`, `arrow-right`, `arrow-left`, `eye`, `eye-off`, `lock`, `unlock`, `star`, `heart`, `download`, `upload`, `share`, `copy`, `filter`, `sort`, `more-horizontal`, `more-vertical`, `menu`, `log-out`, `log-in`, `command`, `layout-dashboard`, `bar-chart-3`, `line-chart`, `inbox`, `box`, `layers`, `zap`, `sparkles`, `life-buoy`, `building`, `briefcase`, `credit-card`, `bold`, `italic`, `underline`, `alert-circle`, `info`, `alert-triangle`, `check-circle`, `x-circle`, `loader`, `panel-left`, `github`

### HeroIcon

```razor
@using TrBlazeUI.Icons.Heroicons.Components

<HeroIcon Name="home" Size="24" Variant="HeroIconVariant.Outline" />
<HeroIcon Name="home" Size="24" Variant="HeroIconVariant.Solid" />
```

### FeatherIcon

```razor
@using TrBlazeUI.Icons.Feather.Components

<FeatherIcon Name="home" Size="24" />
```

---

## 10. Complete Page Examples

> **IMPORTANT:** Every example below includes a complete `@code` block. When generating pages, you MUST always include the `@code` block with all fields, methods, and types referenced in the markup. A page without `@code` will not compile.

### Dashboard Page (Complete)

```razor
@page "/dashboard"
@using TrBlazeUI.Components.Toast
@inject ToastService ToastService

<PageTitle>Dashboard - MyApp</PageTitle>

<div class="space-y-6">
    <div>
        <h1 class="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p class="text-muted-foreground">Overview of your application metrics.</p>
    </div>

    <!-- Stats Cards -->
    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        @foreach (var stat in stats)
        {
            <Card>
                <CardHeader Class="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle Class="text-sm font-medium">@stat.Title</CardTitle>
                    <LucideIcon Name="@stat.Icon" Size="16" Class="text-muted-foreground" />
                </CardHeader>
                <CardContent>
                    <div class="text-2xl font-bold">@stat.Value</div>
                    <p class="text-xs text-muted-foreground">@stat.Change</p>
                </CardContent>
            </Card>
        }
    </div>

    <!-- Main Content Area -->
    <div class="grid gap-4 md:grid-cols-2 lg:grid-cols-7">
        <Card Class="col-span-4">
            <CardHeader>
                <CardTitle>Overview</CardTitle>
            </CardHeader>
            <CardContent>
                <BarChart TItem="MonthlyData" Items="@monthlyData" Height="300px" ShowLegend="false">
                    <ApexPointSeries TItem="MonthlyData" Items="@monthlyData" Name="Value"
                                     SeriesType="SeriesType.Bar"
                                     XValue="@(d => d.Month)" YValue="@(d => (decimal?)d.Value)" />
                </BarChart>
            </CardContent>
        </Card>
        <Card Class="col-span-3">
            <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
                <CardDescription>Latest events in your account.</CardDescription>
            </CardHeader>
            <CardContent>
                <div class="space-y-4">
                    @foreach (var item in recentItems)
                    {
                        <div class="flex items-center gap-4">
                            <Avatar Size="AvatarSize.Small">
                                <AvatarFallback>@item.Initials</AvatarFallback>
                            </Avatar>
                            <div class="flex-1 space-y-1">
                                <p class="text-sm font-medium">@item.Name</p>
                                <p class="text-xs text-muted-foreground">@item.Description</p>
                            </div>
                            <Badge Variant="@item.BadgeVariant">@item.Status</Badge>
                        </div>
                    }
                </div>
            </CardContent>
        </Card>
    </div>
</div>

@code {
    private record StatCard(string Title, string Value, string Change, string Icon);
    private record MonthlyData(string Month, double Value);
    private record ActivityItem(string Name, string Initials, string Description, string Status, BadgeVariant BadgeVariant);

    private List<StatCard> stats = new()
    {
        new("Total Revenue", "$45,231.89", "+20.1% from last month", "dollar-sign"),
        new("Users", "+2,350", "+180 this week", "users"),
        new("Active Sessions", "573", "+12% from yesterday", "activity"),
        new("Conversion Rate", "3.2%", "+0.4% from last month", "trending-up")
    };

    private List<MonthlyData> monthlyData = new()
    {
        new("Jan", 4500), new("Feb", 3800), new("Mar", 5200),
        new("Apr", 4100), new("May", 6300), new("Jun", 5800)
    };

    private List<ActivityItem> recentItems = new()
    {
        new("Alice Johnson", "AJ", "Created new project", "New", BadgeVariant.Default),
        new("Bob Smith", "BS", "Completed deployment", "Done", BadgeVariant.Secondary),
        new("Carol Davis", "CD", "Submitted pull request", "Review", BadgeVariant.Outline)
    };
}
```

### Form Page with Validation (Complete)

```razor
@page "/settings/profile"
@using TrBlazeUI.Components.Toast
@inject ToastService ToastService

<PageTitle>Profile Settings - MyApp</PageTitle>

<div class="space-y-6 max-w-2xl">
    <div>
        <h1 class="text-3xl font-bold tracking-tight">Profile</h1>
        <p class="text-muted-foreground">Manage your account settings.</p>
    </div>

    <Separator />

    <Card>
        <CardHeader>
            <CardTitle>Personal Information</CardTitle>
            <CardDescription>Update your personal details.</CardDescription>
        </CardHeader>
        <CardContent>
            <div class="space-y-4">
                <Field>
                    <FieldLabel>Name</FieldLabel>
                    <FieldContent>
                        <Input @bind-Value="name" Placeholder="Your name" />
                    </FieldContent>
                </Field>

                <Field>
                    <FieldLabel>Email</FieldLabel>
                    <FieldContent>
                        <Input Type="InputType.Email" @bind-Value="email" Placeholder="you@example.com" />
                        <FieldDescription>This is the email used for notifications.</FieldDescription>
                    </FieldContent>
                </Field>

                <Field>
                    <FieldLabel>Bio</FieldLabel>
                    <FieldContent>
                        <Textarea @bind-Value="bio" Placeholder="Tell us about yourself" MaxLength="500" />
                    </FieldContent>
                </Field>

                <Field>
                    <FieldLabel>Role</FieldLabel>
                    <FieldContent>
                        <Select @bind-Value="role" TValue="string" Class="w-full">
                            <SelectTrigger><SelectValue Placeholder="Select a role" /></SelectTrigger>
                            <SelectContent>
                                <SelectItem Value="@("admin")" Text="Admin" TValue="string">Admin</SelectItem>
                                <SelectItem Value="@("user")" Text="User" TValue="string">User</SelectItem>
                                <SelectItem Value="@("viewer")" Text="Viewer" TValue="string">Viewer</SelectItem>
                            </SelectContent>
                        </Select>
                    </FieldContent>
                </Field>

                <Field>
                    <FieldLabel>Preferred Framework</FieldLabel>
                    <FieldContent>
                        <Combobox TItem="FrameworkOption"
                                  Items="frameworks"
                                  @bind-Value="selectedFramework"
                                  ValueSelector="@(f => f.Value)"
                                  DisplaySelector="@(f => f.Label)"
                                  Placeholder="Select framework..."
                                  SearchPlaceholder="Search..."
                                  EmptyMessage="No framework found."
                                  MatchTriggerWidth="true" />
                    </FieldContent>
                </Field>

                <div class="flex items-center space-x-2">
                    <Switch @bind-Checked="notifications" Id="notifications" />
                    <Label For="notifications">Email notifications</Label>
                </div>
            </div>
        </CardContent>
        <CardFooter Class="flex justify-between">
            <Button Variant="ButtonVariant.Outline" OnClick="HandleCancel">Cancel</Button>
            <Button OnClick="HandleSave" Disabled="@isSaving">
                @if (isSaving)
                {
                    <Spinner Size="SpinnerSize.Small" Class="mr-2" />
                    <span>Saving...</span>
                }
                else
                {
                    <span>Save Changes</span>
                }
            </Button>
        </CardFooter>
    </Card>
</div>

@code {
    private record FrameworkOption(string Value, string Label);

    private string name = "";
    private string email = "";
    private string bio = "";
    private string? role;
    private string? selectedFramework;
    private bool notifications = true;
    private bool isSaving = false;

    private List<FrameworkOption> frameworks = new()
    {
        new("blazor", "Blazor"),
        new("react", "React"),
        new("angular", "Angular"),
        new("vue", "Vue.js")
    };

    private async Task HandleSave()
    {
        isSaving = true;
        StateHasChanged();

        try
        {
            await Task.Delay(1000); // Simulate API call
            ToastService.Success("Profile updated successfully.", "Saved");
        }
        catch (Exception ex)
        {
            ToastService.Error($"Failed to save: {ex.Message}", "Error");
        }
        finally
        {
            isSaving = false;
        }
    }

    private void HandleCancel()
    {
        ToastService.Show("Changes discarded.");
    }
}
```

### CRUD Page with DataTable + Dialog (Complete)

```razor
@page "/users"
@using TrBlazeUI.Components.Toast
@inject ToastService ToastService

<PageTitle>User Management - MyApp</PageTitle>

<div class="space-y-6">
    <div class="flex items-center justify-between">
        <div>
            <h1 class="text-3xl font-bold tracking-tight">Users</h1>
            <p class="text-muted-foreground">Manage user accounts.</p>
        </div>
        <Dialog @bind-Open="isAddDialogOpen">
            <DialogTrigger AsChild>
                <Button>
                    <Button.Icon><LucideIcon Name="plus" Size="16" /></Button.Icon>
                    Add User
                </Button>
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Add New User</DialogTitle>
                    <DialogDescription>Create a new user account.</DialogDescription>
                </DialogHeader>
                <div class="space-y-4 py-4">
                    <Field>
                        <FieldLabel>Name</FieldLabel>
                        <FieldContent>
                            <Input @bind-Value="newUserName" Placeholder="Full name" />
                        </FieldContent>
                    </Field>
                    <Field>
                        <FieldLabel>Email</FieldLabel>
                        <FieldContent>
                            <Input Type="InputType.Email" @bind-Value="newUserEmail" Placeholder="user@example.com" />
                        </FieldContent>
                    </Field>
                    <Field>
                        <FieldLabel>Role</FieldLabel>
                        <FieldContent>
                            <Select @bind-Value="newUserRole" TValue="string" Class="w-full">
                                <SelectTrigger><SelectValue Placeholder="Select role" /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem Value="@("Admin")" Text="Admin" TValue="string">Admin</SelectItem>
                                    <SelectItem Value="@("Editor")" Text="Editor" TValue="string">Editor</SelectItem>
                                    <SelectItem Value="@("Viewer")" Text="Viewer" TValue="string">Viewer</SelectItem>
                                </SelectContent>
                            </Select>
                        </FieldContent>
                    </Field>
                </div>
                <DialogFooter>
                    <DialogClose AsChild>
                        <Button Variant="ButtonVariant.Outline">Cancel</Button>
                    </DialogClose>
                    <Button OnClick="HandleAddUser">Create</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    </div>

    @if (selectedUsers.Any())
    {
        <Alert Variant="AlertVariant.Info">
            <AlertTitle>@selectedUsers.Count user(s) selected</AlertTitle>
            <AlertDescription>
                <Button Variant="ButtonVariant.Destructive" Size="ButtonSize.Small"
                        OnClick="HandleDeleteSelected" Class="mt-2">
                    <Button.Icon><LucideIcon Name="trash" Size="14" /></Button.Icon>
                    Delete Selected
                </Button>
            </AlertDescription>
        </Alert>
    }

    <DataTable TData="UserRecord" Data="@users" SelectionMode="DataTableSelectionMode.Multiple"
               @bind-SelectedItems="selectedUsers" InitialPageSize="10">
        <Columns>
            <DataTableColumn TData="UserRecord" TValue="string"
                             Property="@(u => u.Name)" Header="Name" Sortable Filterable />
            <DataTableColumn TData="UserRecord" TValue="string"
                             Property="@(u => u.Email)" Header="Email" Sortable Filterable />
            <DataTableColumn TData="UserRecord" TValue="string"
                             Property="@(u => u.Role)" Header="Role" Sortable />
            <DataTableColumn TData="UserRecord" TValue="string"
                             Property="@(u => u.Status)" Header="Status">
                <CellTemplate Context="user">
                    <Badge Variant="@(user.Status == "Active" ? BadgeVariant.Default : BadgeVariant.Secondary)">
                        @user.Status
                    </Badge>
                </CellTemplate>
            </DataTableColumn>
        </Columns>
    </DataTable>
</div>

@code {
    private record UserRecord(int Id, string Name, string Email, string Role, string Status);

    private bool isAddDialogOpen;
    private string newUserName = "";
    private string newUserEmail = "";
    private string? newUserRole;
    private IReadOnlyCollection<UserRecord> selectedUsers = Array.Empty<UserRecord>();

    private List<UserRecord> users = new()
    {
        new(1, "Alice Johnson", "alice@example.com", "Admin", "Active"),
        new(2, "Bob Smith", "bob@example.com", "Editor", "Active"),
        new(3, "Carol Davis", "carol@example.com", "Viewer", "Inactive")
    };

    private void HandleAddUser()
    {
        if (string.IsNullOrWhiteSpace(newUserName) || string.IsNullOrWhiteSpace(newUserEmail))
        {
            ToastService.Error("Please fill in all required fields.", "Validation Error");
            return;
        }

        var newId = users.Max(u => u.Id) + 1;
        users.Add(new UserRecord(newId, newUserName, newUserEmail, newUserRole ?? "Viewer", "Active"));

        newUserName = "";
        newUserEmail = "";
        newUserRole = null;
        isAddDialogOpen = false;

        ToastService.Success($"User created successfully.", "Created");
    }

    private void HandleDeleteSelected()
    {
        var count = selectedUsers.Count;
        users.RemoveAll(u => selectedUsers.Contains(u));
        selectedUsers = Array.Empty<UserRecord>();
        ToastService.Show($"{count} user(s) deleted.");
    }
}
```

### Settings Page with Tabs (Complete)

```razor
@page "/settings"
@using TrBlazeUI.Components.Toast
@inject ToastService ToastService

<PageTitle>Settings - MyApp</PageTitle>

<div class="space-y-6 max-w-4xl">
    <div>
        <h1 class="text-3xl font-bold tracking-tight">Settings</h1>
        <p class="text-muted-foreground">Manage your application preferences.</p>
    </div>

    <Separator />

    <Tabs DefaultValue="general">
        <TabsList>
            <TabsTrigger Value="general">General</TabsTrigger>
            <TabsTrigger Value="notifications">Notifications</TabsTrigger>
            <TabsTrigger Value="security">Security</TabsTrigger>
        </TabsList>

        <TabsContent Value="general">
            <Card>
                <CardHeader>
                    <CardTitle>General Settings</CardTitle>
                    <CardDescription>Basic application configuration.</CardDescription>
                </CardHeader>
                <CardContent Class="space-y-4">
                    <Field>
                        <FieldLabel>Application Name</FieldLabel>
                        <FieldContent><Input @bind-Value="appName" /></FieldContent>
                    </Field>
                    <Field>
                        <FieldLabel>Language</FieldLabel>
                        <FieldContent>
                            <Select @bind-Value="language" TValue="string" Class="w-full">
                                <SelectTrigger><SelectValue Placeholder="Select language" /></SelectTrigger>
                                <SelectContent>
                                    <SelectItem Value="@("en")" Text="English" TValue="string">English</SelectItem>
                                    <SelectItem Value="@("es")" Text="Spanish" TValue="string">Spanish</SelectItem>
                                    <SelectItem Value="@("fr")" Text="French" TValue="string">French</SelectItem>
                                </SelectContent>
                            </Select>
                        </FieldContent>
                    </Field>
                </CardContent>
                <CardFooter><Button OnClick="@(() => ToastService.Success("General settings saved."))">Save</Button></CardFooter>
            </Card>
        </TabsContent>

        <TabsContent Value="notifications">
            <Card>
                <CardHeader>
                    <CardTitle>Notification Preferences</CardTitle>
                    <CardDescription>Choose what notifications you receive.</CardDescription>
                </CardHeader>
                <CardContent Class="space-y-4">
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium">Email Notifications</p>
                            <p class="text-xs text-muted-foreground">Receive email about activity.</p>
                        </div>
                        <Switch @bind-Checked="emailNotifs" />
                    </div>
                    <Separator />
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium">Push Notifications</p>
                            <p class="text-xs text-muted-foreground">Receive push notifications.</p>
                        </div>
                        <Switch @bind-Checked="pushNotifs" />
                    </div>
                    <Separator />
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium">Marketing Emails</p>
                            <p class="text-xs text-muted-foreground">Receive product updates and offers.</p>
                        </div>
                        <Switch @bind-Checked="marketingNotifs" />
                    </div>
                </CardContent>
            </Card>
        </TabsContent>

        <TabsContent Value="security">
            <Card>
                <CardHeader>
                    <CardTitle>Security</CardTitle>
                    <CardDescription>Manage security settings.</CardDescription>
                </CardHeader>
                <CardContent Class="space-y-4">
                    <Field>
                        <FieldLabel>Current Password</FieldLabel>
                        <FieldContent><Input Type="InputType.Password" @bind-Value="currentPwd" /></FieldContent>
                    </Field>
                    <Field>
                        <FieldLabel>New Password</FieldLabel>
                        <FieldContent><Input Type="InputType.Password" @bind-Value="newPwd" /></FieldContent>
                    </Field>
                    <Field>
                        <FieldLabel>Confirm Password</FieldLabel>
                        <FieldContent><Input Type="InputType.Password" @bind-Value="confirmPwd" /></FieldContent>
                    </Field>
                    <Separator />
                    <div class="flex items-center space-x-2">
                        <Checkbox @bind-Checked="twoFactor" Id="2fa" />
                        <Label For="2fa">Enable two-factor authentication</Label>
                    </div>
                </CardContent>
                <CardFooter><Button OnClick="HandleUpdateSecurity">Update Security</Button></CardFooter>
            </Card>
        </TabsContent>
    </Tabs>
</div>

@code {
    private string appName = "My Application";
    private string? language = "en";
    private bool emailNotifs = true;
    private bool pushNotifs = false;
    private bool marketingNotifs = false;
    private string currentPwd = "";
    private string newPwd = "";
    private string confirmPwd = "";
    private bool twoFactor = false;

    private void HandleUpdateSecurity()
    {
        if (newPwd != confirmPwd)
        {
            ToastService.Error("Passwords do not match.", "Validation Error");
            return;
        }
        ToastService.Success("Security settings updated.", "Saved");
        currentPwd = "";
        newPwd = "";
        confirmPwd = "";
    }
}
```

### Sheet with Form (Complete)

```razor
@* Example: Using Sheet for a side panel form *@

<Sheet @bind-Open="isEditOpen">
    <SheetTrigger AsChild>
        <Button Variant="ButtonVariant.Outline">
            <Button.Icon><LucideIcon Name="pencil" Size="16" /></Button.Icon>
            Edit Details
        </Button>
    </SheetTrigger>
    <SheetContent Class="w-[400px] sm:w-[540px]">
        <SheetHeader>
            <SheetTitle>Edit Profile</SheetTitle>
            <SheetDescription>Make changes to your profile here.</SheetDescription>
        </SheetHeader>
        <div class="space-y-4 py-4">
            <Field>
                <FieldLabel>Name</FieldLabel>
                <FieldContent>
                    <Input @bind-Value="editName" Placeholder="Your name" />
                </FieldContent>
            </Field>
            <Field>
                <FieldLabel>Username</FieldLabel>
                <FieldContent>
                    <Input @bind-Value="editUsername" Placeholder="@username" />
                    <FieldDescription>This is your public display name.</FieldDescription>
                </FieldContent>
            </Field>
        </div>
        <SheetFooter>
            <SheetClose AsChild>
                <Button Variant="ButtonVariant.Outline">Cancel</Button>
            </SheetClose>
            <Button OnClick="HandleSaveEdit">Save Changes</Button>
        </SheetFooter>
    </SheetContent>
</Sheet>

@code {
    private bool isEditOpen;
    private string editName = "Pedro Duarte";
    private string editUsername = "peduarte";

    private void HandleSaveEdit()
    {
        isEditOpen = false;
        // ToastService.Success("Profile updated.");
    }
}
```

### Toast Notification Patterns (Complete)

```razor
@* Toast requires: @inject ToastService ToastService *@
@* Layout requires: <ToastProvider Position="ToastPosition.BottomRight" /> *@
@inject ToastService ToastService

@* Simple notifications *@
<Button OnClick="@(() => ToastService.Show("Your message has been sent."))">
    Default Toast
</Button>
<Button OnClick="@(() => ToastService.Success("Changes saved.", "Success"))">
    Success Toast
</Button>
<Button OnClick="@(() => ToastService.Error("Something went wrong.", "Error"))">
    Error Toast
</Button>

@* Toast with action button *@
<Button OnClick="ShowUndoToast">Toast with Action</Button>

@* Custom duration (ms) — 0 means no auto-dismiss *@
<Button OnClick="@(() => ToastService.Show("Quick!", duration: 2000))">2 Second Toast</Button>
<Button OnClick="ShowPersistentToast">Persistent Toast</Button>

@* Dismiss all *@
<Button Variant="ButtonVariant.Outline" OnClick="@(() => ToastService.DismissAll())">
    Dismiss All
</Button>

@code {
    private void ShowUndoToast()
    {
        ToastService.Show(new ToastData
        {
            Title = "Post deleted",
            Description = "Your post has been removed.",
            ActionText = "Undo",
            OnAction = () => ToastService.Success("Post restored!")
        });
    }

    private void ShowPersistentToast()
    {
        ToastService.Show(new ToastData
        {
            Title = "Important",
            Description = "This won't auto-dismiss. Click X to close.",
            Duration = 0
        });
    }
}
```

---

## Which version added what — read this before reporting a control as missing

**The copy of this document in your own repository is your installed version's reference, not the
latest one.** `.trblazeui/TrBlazeUI-AI-Reference.md` is deployed by the installed NuGet package and
changes only when you upgrade it. A control added after your version is absent from that copy *and*
absent from your `bin` folder, even though it exists and is published. Before concluding that
something does not exist, check your installed version against the table below.

**This has already cost a consumer real work.** A team on 2.0.7 filed a report saying no tree
control and no difference viewer existed. Both had been published in 2.0.8 the day before. The
answer to "the control I need is missing" is usually *upgrade*, sometimes *use the right package
source* — see the next heading — and only rarely *wait for it to be built*.

| Version | Released | Component namespaces | What it added |
|---|---|---|---|
| **2.0.9** | 2026-09-22 | 85 | `CodeEditor` + `EditorTabs`, `LogView`, `NavList`, `Typing`; `Progress.Indeterminate`; `StepStatus` on `StepperItem`/`TimelineItem`; `SortableList.ShowPosition`/`AllowRemove`; `DataTable.SelectedCount` |
| **2.0.8** | 2026-09-20 | 81 | `TreeView` + `TreeItem`, `DiffView` + `TextDiff`, `ScrollArea.StickToEnd`, `ToggleGroup.Joined`/`AllowDeselect`/`AriaLabel` |
| **2.0.7** | 2026-09-15 | 79 | `InputGroupInput.DebounceMilliseconds`; the chart teardown fix |
| **2.0.6** | 2026-09-13 | 79 | newest version currently on nuget.org |

Everything in this document describes **2.0.9**. Each control's own section has its full parameter
table; the version table above only says when it arrived. `CHANGELOG.md` has the detail per release.

### Which package source to use — there are two, and they carry different versions

TrBlazeUI is published to two sources, and **which one you should use depends on who you are**.
They do not carry the same versions, so this is the first thing to check when a control you can
see documented here is missing from your `bin` folder.

| You are | Your source | What it carries |
|---|---|---|
| **An internal application** — one of this organisation's own projects, such as Chatur, TfLens or TrStudio | **GitHub Packages** — `https://nuget.pkg.github.com/techierathore/index.json` | Every release, as soon as it is cut. Currently **2.0.9**. |
| **An external user** — any project outside this organisation | **nuget.org** — the ordinary public source | The versions published for public use. Currently **2.0.6**. |

**If you are an internal application, use GitHub Packages.** It needs authentication even though
the library is Apache 2.0, because that is how GitHub Packages works. Add the source once:

```bash
dotnet nuget add source https://nuget.pkg.github.com/techierathore/index.json \
  --name TrBlazeUI \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT \
  --store-password-in-clear-text
```

The token needs the `read:packages` scope and nothing more. In CI, the workflow adds this source
with the sign-in token the build system supplies — see `nuget.config` at the repository root.

**If you are an external user, use nuget.org** and expect it to lag the internal source. A control
in the version table above that is newer than the newest version on nuget.org is not yet available
to you, and no amount of restoring will find it.

**So when an upgrade cannot find the version you want, check your source before anything else.**
Being on the wrong one looks exactly like a control that does not exist, and it costs more time
than any real defect.

### The controls added in 2.0.9

Full parameter tables. The controls added in 2.0.8 — `TreeView`, `DiffView`,
`ScrollArea.StickToEnd`, `ToggleGroup` — are documented in their own sections (§3, §8, §3, §5).

#### CodeEditor — edit a source file

An editable code area. The Tab key inserts an indent instead of moving focus; press **Escape, then
Tab** to move focus out (the control says so through `aria-describedby`, so it is not a keyboard
trap). Lines do not wrap — the text scrolls sideways — because with soft wrap one logical line
paints as several rows and every gutter number below it would be wrong.

No syntax highlighter is bundled, for the same reason `CodeBlock` bundles none: it would drag a
large JavaScript dependency into every consumer. Pass your own highlighted markup through `Html`.

```razor
<CodeEditor @bind-Value="objSource" Language="csharp" TabSize="4" Rows="20" />
```

| Parameter | Type | Default | What it does |
|---|---|---|---|
| `Value` / `ValueChanged` | `string?` | `null` | The text. Use `@bind-Value`. |
| `Language` | `string?` | `null` | Label in the header; the header is omitted when empty. |
| `Html` | `MarkupString?` | `null` | Your own pre-highlighted markup, painted behind the text. Must track `Value` or the reader sees stale characters. |
| `ShowLineNumbers` | `bool` | `true` | The gutter. |
| `TabSize` | `int` | `4` | Clamped 1–8. |
| `UseSpaces` | `bool` | `true` | Spaces rather than a tab character. |
| `Rows` | `int` | `12` | Visible rows. |
| `ReadOnly` / `Disabled` | `bool` | `false` | Read-only releases the Tab key entirely. |
| `Placeholder` | `string?` | `null` | |
| `Id` / `AriaLabel` / `AriaDescribedBy` | `string?` | `null` | Your `AriaDescribedBy` is prepended to the built-in hint, not replaced. |
| `DebounceMilliseconds` | `int` | `0` | 0 raises `ValueChanged` per keystroke; any other value raises it once typing pauses. |
| `Class` | `string?` | `null` | |
| `LineCount` (read-only) | `int` | — | |

#### EditorTabs — a strip of open files

Not `Tabs`. Each tab has a close button of its own and a mark for unsaved work, and the strip is a
plain list rather than a `role="tablist"`: a tablist's roving `tabindex` would put the close
buttons out of keyboard reach. The active file carries `aria-current="true"`. An unsaved file is
marked twice — a dot whose *presence* carries it, and `", unsaved changes"` in the tab's
accessible name — so it is never colour alone.

`EditorTabs` does not remove a closed tab or pick the next file: you own the list, and an unsaved
file usually needs a prompt first.

```razor
<EditorTabs Items="@objOpenFiles" @bind-ActiveId="objActiveFile" OnClose="CloseFileAsync" />
```

| Parameter | Type | Default | What it does |
|---|---|---|---|
| `Items` | `IReadOnlyList<EditorTabItem>` | `[]` | `record EditorTabItem(string Id, string Label, bool IsDirty = false)` |
| `ActiveId` / `ActiveIdChanged` | `string?` | `null` | Use `@bind-ActiveId`. |
| `OnSelect` / `OnClose` | `EventCallback<EditorTabItem>` | — | |
| `AriaLabel` | `string` | `"Open files"` | |
| `Class` | `string?` | `null` | |

#### LogView — the output of a running command

Lines arrive one at a time, each marked ordinary, success, warning or failure. The panel keeps its
height and follows the newest line until the reader scrolls up, then offers a way back. Appending
a line does **not** re-render the list: lines are cut into blocks of 128 and a full block never
renders again.

The enum is `LogLineKind`, **not** `LogLevel` — `LogLevel` would collide with
`Microsoft.Extensions.Logging.LogLevel` the moment you have both `@using`s.

```razor
<LogView Lines="@objBuildOutput" Height="20rem" MaxLines="2000" ShowTimestamps="true" />
```

| Parameter | Type | Default | What it does |
|---|---|---|---|
| `Lines` | `IReadOnlyList<LogLine>?` | `null` | `record LogLine(string Text, LogLineKind Kind = Ordinary, DateTimeOffset? Timestamp = null)`; `enum LogLineKind { Ordinary, Success, Warning, Failure }` |
| `Height` / `MaxHeight` | `string?` | `null` / `"24rem"` | CSS lengths, as `ScrollArea` expresses them. |
| `Follow` | `bool` | `true` | Drives `ScrollArea.StickToEnd`. |
| `AtEndChanged` | `EventCallback<bool>` | — | Fires false when the reader scrolls up, true when they return. |
| `MaxLines` | `int?` | `null` | Caps the panel; the oldest lines are dropped and a note says how many. **This caps the panel, not your own buffer** — cap that too. |
| `ShowTimestamps` / `TimestampFormat` | `bool` / `string` | `false` / `"HH:mm:ss"` | |
| `LineTemplate` | `RenderFragment<LogLine>?` | `null` | |
| `ShowJumpToNewest` / `JumpToNewestText` | `bool` / `string` | `true` / `"Jump to newest"` | |
| `AriaLabel` / `EmptyText` | `string` | `"Command output"` / `"No output yet"` | |
| `Class` | `string?` | `null` | |

Members: `ScrollToEndAsync()`, `IsAtEnd`, `HeldLineCount`, `DroppedLineCount`.

There is deliberately no `role="log"`: that carries an implicit `aria-live="polite"` and would
announce every line of a build. The panel is a labelled region, and a separate status region
announces one summary per burst of output.

#### Typing — an answer is still being written

Three moving dots, sized in `em` so they match the text they sit in, for work that is under way
with no known end. Use it instead of `Spinner` when text is being composed inline: `Spinner` reads
as "the screen is busy", this reads as "the answer is still coming". Honours
`prefers-reduced-motion`.

```razor
<p>@objAnswer @if (objStillWriting) { <Typing /> }</p>
```

| Parameter | Type | Default |
|---|---|---|
| `Size` | `TypingSize` — `Small`, `Default`, `Large` | `Default` |
| `Label` | `string` | `"Still writing"` — the accessible name, announced once |
| `Class` | `string?` | `null` |

For a bar rather than dots, `Progress` gains **`Indeterminate`** (`bool`, default `false`). When
true it animates without claiming a percentage and **omits `aria-valuenow`**, which is what tells
assistive technology the value is unknown; `Value` and `Max` are ignored.

#### NavList&lt;TItem&gt; — a list that drives a detail pane

A list of multi-line rows — a name, some badges, a couple of values — where one row is chosen and
the rest of the screen follows it. The arrow keys move between rows and the list is one tab stop.

By default it is a `role="listbox"` of `role="option"`, because choosing a row changes what the
*same* screen shows and navigates nowhere. Set `Mode="NavListMode.Navigation"` for a real `<nav>`
of links with `aria-current="page"` when the rows do navigate.

**There is no `ListDetail` component and there will not be one** — the two-pane split is the
page's own layout. Compose it with `Grid` or `ResizablePanelGroup`; `/components/nav-list` shows
both.

```razor
<NavList TItem="Role" Items="@objRoles" ItemId="@(r => r.Id)" @bind-SelectedId="objRoleId" AriaLabel="Roles">
    <ItemTemplate Context="role">
        <div class="font-medium">@role.Name</div>
        <div class="text-xs text-muted-foreground">@role.Summary</div>
    </ItemTemplate>
</NavList>
```

| Parameter | Type | Default | What it does |
|---|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | `[]` | Required. |
| `ItemTemplate` | `RenderFragment<TItem>?` | `null` | Falls back to `ToString()`. |
| `ItemId` | `Func<TItem, string>?` | `null` | Identity. Without it, `EqualityComparer<TItem>.Default`. |
| `SelectedId` / `SelectedIdChanged` | `string?` | `null` | **Preferred.** A re-fetched list holds new instances, so an object-valued selection silently stops matching. |
| `Selected` / `SelectedChanged` | `TItem?` | `default` | Object-valued binding; may be bound at the same time as `SelectedId`, which wins for display. |
| `Mode` | `NavListMode` — `Select`, `Navigation` | `Select` | |
| `ItemHref` | `Func<TItem, string?>?` | `null` | `Navigation` mode only. |
| `Orientation` | `NavListOrientation` — `Vertical`, `Horizontal` | `Vertical` | Picks which arrow keys move. |
| `ItemDisabled` | `Func<TItem, bool>?` | `null` | Disabled rows take no tab stop and the arrows skip them. |
| `EmptyContent` | `RenderFragment?` | `null` | |
| `AriaLabel` / `Class` | `string?` | `null` | |

Members: `CurrentSelectedId`, `CurrentSelected`, `SelectAsync(TItem)`. There is no type-ahead: a
row is several lines with no single label to match on.

---

## Quick Reference: Two-Way Binding Patterns

| Component | Binding Pattern | Type |
|-----------|----------------|------|
| Input | `@bind-Value="str"` | string? |
| Textarea | `@bind-Value="str"` | string? |
| Checkbox | `@bind-Checked="bln"` | bool |
| Switch | `@bind-Checked="bln"` | bool |
| Select | `@bind-Value="val"` | TValue? |
| RadioGroup | `@bind-Value="val"` | TValue |
| Combobox | `@bind-Value="str"` | string? |
| Slider | `@bind-Value="dbl"` | double |
| Rating | `@bind-Value="int"` | int |
| Toggle | `@bind-Pressed="bln"` | bool |
| ToolbarToggleButton | `@bind-IsPressed="bln"` | bool |
| DatePicker | `@bind-Value="dt"` | DateTime? |
| TimePicker | `@bind-Value="ts"` | TimeSpan? |
| Dialog | `@bind-Open="bln"` | bool |
| Sheet | `@bind-Open="bln"` | bool |
| DropdownMenu | `@bind-Open="bln"` | bool |
| Tabs | `@bind-Value="str"` | string? |
| Collapsible | `@bind-Open="bln"` | bool |
| DataTable | `@bind-SelectedItems="col"` | IReadOnlyCollection<TData> |
| MultiSelect | `@bind-SelectedValues="list"` | List<string> |
| TreeView | `@bind-SelectedValue="str"` | string? |
| TreeItem | `@bind-Expanded="bln"` | bool |
| SortableList | `@bind-Items="list"` | IList<TItem> |
| ToggleGroup (Single) | `@bind-Value="val"` | TValue? |
| ToggleGroup (Multiple) | `@bind-Values="list"` | List<TValue> |
| AnchorNav | `@bind-ActiveId="str"` | string |

## Quick Reference: Common CSS Utility Classes

Use these Tailwind classes with the `Class` parameter on any component:

```
Layout:    w-full w-[280px] max-w-md flex grid gap-4
Spacing:   p-4 px-6 py-2 m-4 space-y-4 space-x-2
Text:      text-sm text-lg font-bold font-medium text-muted-foreground
Colors:    bg-primary text-primary-foreground bg-muted bg-destructive
Border:    border rounded-lg border-primary border-dashed
Grid:      grid-cols-2 md:grid-cols-3 lg:grid-cols-4 col-span-3
Flex:      flex items-center justify-between flex-1 shrink-0
Display:   hidden md:block lg:flex
Shadow:    shadow-sm shadow-md shadow-lg
```
