namespace ChaturUI.Layouts;

/// <summary>
/// The fixed order and labels of the secondary window's navigation list — Prerequisites, Repository,
/// then the seven Settings tabs (UI Design "Screen: Settings"). One list here keeps the navigation,
/// the title bar and every page's own route from drifting apart.
/// </summary>
public static class SecondaryNavigation
{
    /// <summary>Every phase-1 destination in the secondary window, in the order the nav shows them.</summary>
    public static IReadOnlyList<SecondaryNavItem> Items { get; } =
    [
        new SecondaryNavItem("/prerequisites", "Prerequisites", "nav-prerequisites", "Prerequisites", "wrench"),
        new SecondaryNavItem("/repository", "Repository", "nav-repository", "Repository", "git-branch"),
        new SecondaryNavItem("/settings/providers", "Model providers", "nav-providers", "Model providers", "key"),
        new SecondaryNavItem("/settings/routing", "Routing", "nav-routing", "Routing", "route"),
        new SecondaryNavItem("/settings/agents", "Agents", "nav-agents", "Agents", "users"),
        new SecondaryNavItem("/settings/corrections", "Corrections", "nav-corrections", "Corrections", "refresh-cw"),
        new SecondaryNavItem("/settings/measurements", "Measurements", "nav-measurements", "Measurements", "chart-column"),
        new SecondaryNavItem("/settings/appearance", "Appearance", "nav-appearance", "Appearance", "palette"),
        new SecondaryNavItem("/settings/account", "Account", "nav-account", "Account", "user")
    ];

    /// <summary>
    /// Finds the navigation item for a route.
    /// </summary>
    /// <param name="aRelativePath">The current route, relative to the app base.</param>
    /// <returns>The matching item, or <see langword="null"/> when the route matches none of them.</returns>
    public static SecondaryNavItem? Match(string aRelativePath)
    {
        var vPath = "/" + aRelativePath.TrimStart('/');
        return Items.FirstOrDefault(aItem => string.Equals(aItem.Route, vPath, StringComparison.OrdinalIgnoreCase));
    }
}
