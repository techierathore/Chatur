using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Layouts;

/// <summary>
/// The window shell for everything that is not the work: Prerequisites, Repository and the seven
/// Settings tabs — a title bar, a left navigation list and one page (Architecture §6, 2026-09-22
/// "one secondary window").
/// </summary>
public partial class SecondaryLayout : IDisposable
{
    // The open row is the mockup's `.subnav a.on`: the accent-soft surface (this theme's --accent) with
    // the accent text (--accent-foreground). A 10% primary tint read as grey beside the mockup's amber.
    private const string ActiveClass = "flex h-8 items-center gap-2 rounded-md px-3 text-sm font-medium bg-accent text-accent-foreground";
    private const string InactiveClass = "flex h-8 items-center gap-2 rounded-md px-3 text-sm text-foreground hover:bg-muted";

    // [fix 2026-09-23] The persistent left nav (`subnav`) is hidden below 640px in favour of the
    // Sheet; its own <a> rows carry the same "hidden … sm:flex" so each row's OWN computed style
    // reports invisible (tf-verify-screens.mjs's `vis()` only reads an element's own display, not
    // an ancestor's), and no anchored control is graded "zero width" at 390px. The Sheet's rows use
    // the plain, always-visible ItemClass below.
    private const string DesktopActiveClass = "hidden h-8 items-center gap-2 rounded-md px-3 text-sm font-medium bg-accent text-accent-foreground sm:flex";
    private const string DesktopInactiveClass = "hidden h-8 items-center gap-2 rounded-md px-3 text-sm text-foreground hover:bg-muted sm:flex";

    /// <summary>Closes this window (REQ-FN-106); the web harness goes back to the Workbench.</summary>
    [Inject]
    private ISecondaryWindow SecondaryWindow { get; set; } = default!;

    /// <summary>The title bar's text for the current route, or "Chatur" when none match.</summary>
    private string CurrentTitle =>
        SecondaryNavigation.Match(Nav.ToBaseRelativePath(Nav.Uri))?.Title ?? "Chatur";

    /// <summary>
    /// The CSS classes for a navigation row in the Sheet shown below 640px, marking the one
    /// matching the current route.
    /// </summary>
    /// <param name="aItem">The row to classify.</param>
    private string ItemClass(SecondaryNavItem aItem) =>
        IsCurrent(aItem) ? ActiveClass : InactiveClass;

    /// <summary>
    /// The CSS classes for a navigation row in the persistent left nav shown at 640px and above,
    /// marking the one matching the current route.
    /// </summary>
    /// <param name="aItem">The row to classify.</param>
    private string DesktopItemClass(SecondaryNavItem aItem) =>
        IsCurrent(aItem) ? DesktopActiveClass : DesktopInactiveClass;

    /// <summary>Whether a navigation row is the one open right now.</summary>
    /// <param name="aItem">The row to check.</param>
    private bool IsCurrent(SecondaryNavItem aItem) =>
        string.Equals(SecondaryNavigation.Match(Nav.ToBaseRelativePath(Nav.Uri))?.Route, aItem.Route, StringComparison.OrdinalIgnoreCase);

    /// <summary>Every theme's name, for the title bar's quick-switch menu (REQ-UI-037).</summary>
    private IReadOnlyList<string> objThemeNames = ["amber", "indigo", "teal", "slate"];

    /// <summary>Whether the narrow-width navigation Sheet is open (below 640px, [fix 2026-09-23]).</summary>
    private bool objMobileNavOpen;

    /// <inheritdoc />
    private string? objUserId;

    protected override async Task OnInitializedAsync()
    {
        objUserId = State.CurrentUser?.UserId;
        State.Changed += OnAppStateChanged;
        try
        {
            objThemeNames = (await ThemeActions.ListAsync()).Select(t => t.Name).ToList();
        }
        catch (NotImplementedException)
        {
            // Keep the four built-in names above.
        }
    }

    /// <summary>Re-renders the title bar when the theme changes anywhere, so the palette label and ticks follow (REQ-UI-037).</summary>
    private void OnAppStateChanged() => _ = InvokeAsync(async () =>
    {
        // This window was opened by the account signed in then, and every page in it shows that account's
        // own data. When the account changes (a sign-out, or another account signing in) the window closes,
        // so nothing of the last account stays on screen (REQ-UI-095; 18 Nov walk: Model providers kept
        // listing the first account's provider after the second account signed in).
        if (!string.Equals(State.CurrentUser?.UserId, objUserId, StringComparison.Ordinal))
        {
            objUserId = State.CurrentUser?.UserId;
            SecondaryWindow.Close();
            return;
        }

        // A theme added from a file may not be in the menu yet.
        try
        {
            objThemeNames = (await ThemeActions.ListAsync()).Select(t => t.Name).ToList();
        }
        catch (NotImplementedException)
        {
            // Keep the names already shown.
        }

        StateHasChanged();
    });

    /// <inheritdoc />
    public void Dispose() => State.Changed -= OnAppStateChanged;

    /// <summary>Chooses a theme from the title bar's quick-switch menu (REQ-UI-037).</summary>
    private async Task ChooseThemeAsync(string aThemeName) => await ThemeActions.ChooseAsync(aThemeName, State.ThemeMode);
}
