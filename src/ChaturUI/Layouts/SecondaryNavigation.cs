using Chatur.Core;
namespace ChaturUI.Layouts;

/// <summary>
/// The fixed order and labels of the secondary window's navigation list — Prerequisites, Repository,
/// then the seven Settings tabs (UI Design "Screen: Settings"). One list here keeps the navigation,
/// the title bar and every page's own route from drifting apart.
/// </summary>
public static class SecondaryNavigation
{
    /// <summary>Every phase-1 destination in the secondary window, in the order the nav shows them.</summary>
    public static IReadOnlyList<SecondaryNavItem> Items => Visible();

    private static AppState? objState;

    /// <summary>
    /// Points the navigation at the shared state, so an entry whose licence feature is off is left out of
    /// <see cref="Items"/> (REQ-FN-075). Called by the window's route guard once the owner is signed in.
    /// </summary>
    /// <param name="aState">The shared app state.</param>
    public static void Bind(AppState aState) => objState = aState;

    private static List<SecondaryNavItem> Visible() =>
        objState is null
            ? [.. All]
            : All.Where(aItem => aItem.Feature is null || objState.IsFeatureOn(aItem.Feature)).ToList();

    private static IReadOnlyList<SecondaryNavItem> All { get; } =
    [
        new SecondaryNavItem("/prerequisites", "Prerequisites", "nav-prerequisites", "Prerequisites", "wrench"),
        new SecondaryNavItem("/repository", "Repository", "nav-repository", "Repository", "git-branch"),
        new SecondaryNavItem("/settings/providers", "Model providers", "nav-providers", "Model providers", "key"),
        new SecondaryNavItem("/settings/routing", "Routing", "nav-routing", "Routing", "route"),
        new SecondaryNavItem("/settings/agents", "Agents", "nav-agents", "Agents", "users"),
        new SecondaryNavItem("/settings/corrections", "Corrections", "nav-corrections", "Corrections", "refresh-cw"),
        new SecondaryNavItem("/measurements", "Project measurements", "nav-project-measurements", "Measurements", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/settings/measurements", "Measurements", "nav-measurements", "Measurements", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/settings/appearance", "Appearance", "nav-appearance", "Appearance", "palette"),
        new SecondaryNavItem("/settings/account", "Account", "nav-account", "Account", "user"),
        new SecondaryNavItem("/licence", "Licence", "nav-licence", "Licence", "key"),
        new SecondaryNavItem("/feedback", "Feedback", "nav-feedback", "Feedback", "alert-triangle"),
        new SecondaryNavItem("/worktrees", "Working copies", "nav-worktrees", "Working copies", "copy", Chatur.Core.Licences.ChaturFeatures.Worktrees),
        new SecondaryNavItem("/all-projects", "All projects", "nav-all-projects", "All projects", "bar-chart-3"),
        new SecondaryNavItem("/measurements/all", "Measurements across projects", "nav-measurements-all", "Measurements across projects", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/integrations", "Integrations", "nav-integrations", "Integrations", "route", Chatur.Core.Licences.ChaturFeatures.Integrations)
    ];

    /// <summary>
    /// Finds the navigation item for a route.
    /// </summary>
    /// <param name="aRelativePath">The current route, relative to the app base.</param>
    /// <returns>The matching item, or <see langword="null"/> when the route matches none of them.</returns>
    public static SecondaryNavItem? Match(string aRelativePath)
    {
        var vPath = "/" + aRelativePath.TrimStart('/');
        return All.FirstOrDefault(aItem => string.Equals(aItem.Route, vPath, StringComparison.OrdinalIgnoreCase));
    }
}
