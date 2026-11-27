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

    // [REQ-FN-313 mockup parity] The mockups' order and groups (mockups/measurements-all.html "subnav"): This project,
    // Chatur, Your account. The 390 px check found the app's list in another order with no group headings.
    private static IReadOnlyList<SecondaryNavItem> All { get; } =
    [
        new SecondaryNavItem("/prerequisites", "Prerequisites", "nav-prerequisites", "Prerequisites", "wrench"),
        new SecondaryNavItem("/repository", "Repository", "nav-repository", "Repository", "git-branch"),
        new SecondaryNavItem("/worktrees", "Working copies", "nav-worktrees", "Working copies", "copy", Chatur.Core.Licences.ChaturFeatures.Worktrees),
        new SecondaryNavItem("/measurements", "Project measurements", "nav-project-measurements", "Measurements", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/compare-models", "Compare models", "nav-compare-models", "Compare models", "users", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/settings/providers", "Model providers", "nav-providers", "Model providers", "key"),
        new SecondaryNavItem("/settings/routing", "Routing", "nav-routing", "Routing", "route"),
        new SecondaryNavItem("/settings/agents", "Agents", "nav-agents", "Agents", "users"),
        new SecondaryNavItem("/settings/corrections", "Corrections", "nav-corrections", "Corrections", "refresh-cw"),
        new SecondaryNavItem("/settings/measurements", "Measurements", "nav-measurements", "Measurements", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements),
        new SecondaryNavItem("/settings/appearance", "Appearance", "nav-appearance", "Appearance", "palette"),
        new SecondaryNavItem("/settings/account", "Account", "nav-account", "Account", "user"),
        new SecondaryNavItem("/licence", "Licence", "nav-licence", "Licence", "key"),
        new SecondaryNavItem("/feedback", "Feedback", "nav-feedback", "Feedback", "alert-triangle"),
        new SecondaryNavItem("/integrations", "Integrations", "nav-integrations", "Integrations", "route", Chatur.Core.Licences.ChaturFeatures.Integrations),
        new SecondaryNavItem("/all-projects", "All projects", "nav-all-projects", "All projects", "bar-chart-3"),
        new SecondaryNavItem("/measurements/all", "Measurements across projects", "nav-measurements-all", "Measurements across projects", "chart-column", Chatur.Core.Licences.ChaturFeatures.Measurements)
    ];

    /// <summary>The group headings of the list, in order, as the mockups draw them ("This project", "Chatur", "Your account").</summary>
    private static readonly string[] GroupOrder = ["This project", "Chatur", "Your account"];

    /// <summary>The routes under "This project"; every route not here or under "Your account" is under "Chatur".</summary>
    private static readonly HashSet<string> ProjectRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/prerequisites", "/repository", "/worktrees", "/measurements", "/compare-models"
    };

    /// <summary>The routes under "Your account".</summary>
    private static readonly HashSet<string> AccountRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/licence", "/feedback", "/integrations", "/all-projects", "/measurements/all"
    };

    /// <summary>
    /// <see cref="Items"/> under their group headings, in the mockups' order; a group with nothing visible is left out.
    /// </summary>
    public static IReadOnlyList<(string Heading, IReadOnlyList<SecondaryNavItem> Items)> Groups
    {
        get
        {
            var vVisible = Visible();
            return GroupOrder
                .Select(aHeading => (aHeading, (IReadOnlyList<SecondaryNavItem>)vVisible.Where(aItem => GroupOf(aItem.Route) == aHeading).ToList()))
                .Where(aGroup => aGroup.Item2.Count > 0)
                .ToList();
        }
    }

    /// <summary>The heading a route is listed under.</summary>
    /// <param name="aRoute">The item's route.</param>
    public static string GroupOf(string aRoute) =>
        ProjectRoutes.Contains(aRoute) ? GroupOrder[0] : AccountRoutes.Contains(aRoute) ? GroupOrder[2] : GroupOrder[1];

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
