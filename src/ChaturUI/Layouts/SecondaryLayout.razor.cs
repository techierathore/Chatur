namespace ChaturUI.Layouts;

/// <summary>
/// The window shell for everything that is not the work: Prerequisites, Repository and the seven
/// Settings tabs — a title bar, a left navigation list and one page (Architecture §6, 2026-09-22
/// "one secondary window").
/// </summary>
public partial class SecondaryLayout : IDisposable
{
    private const string ActiveClass = "flex h-8 items-center gap-2 rounded-md px-3 text-sm font-medium bg-primary/10 text-primary";
    private const string InactiveClass = "flex h-8 items-center gap-2 rounded-md px-3 text-sm text-foreground hover:bg-accent";

    // [fix 2026-09-23] The persistent left nav (`subnav`) is hidden below 640px in favour of the
    // Sheet; its own <a> rows carry the same "hidden … sm:flex" so each row's OWN computed style
    // reports invisible (tf-verify-screens.mjs's `vis()` only reads an element's own display, not
    // an ancestor's), and no anchored control is graded "zero width" at 390px. The Sheet's rows use
    // the plain, always-visible ItemClass below.
    private const string DesktopActiveClass = "hidden h-8 items-center gap-2 rounded-md px-3 text-sm font-medium bg-primary/10 text-primary sm:flex";
    private const string DesktopInactiveClass = "hidden h-8 items-center gap-2 rounded-md px-3 text-sm text-foreground hover:bg-accent sm:flex";

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
    protected override async Task OnInitializedAsync()
    {
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
    private async Task ChooseThemeAsync(string aThemeName) => await ThemeActions.ChooseAsync(aThemeName, State.IsDark);
}
