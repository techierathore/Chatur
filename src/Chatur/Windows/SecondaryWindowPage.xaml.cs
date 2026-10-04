namespace Chatur.Windows;

/// <summary>
/// The secondary window's page: opens the BlazorWebView on whichever route it was asked to show
/// (Prerequisites, Repository, or one of the Settings tabs).
/// </summary>
public partial class SecondaryWindowPage : ContentPage
{
    /// <summary>
    /// Creates the page and points the web view at the given route.
    /// </summary>
    /// <param name="aRoute">The route to open, e.g. <c>/prerequisites</c> or <c>/settings/providers</c>.</param>
    public SecondaryWindowPage(string aRoute)
    {
        InitializeComponent();
        blazorWebView.StartPath = aRoute;
    }

    /// <summary>
    /// Switches this already-open window to another route.
    /// </summary>
    /// <param name="aRoute">The route to show.</param>
    /// <remarks>
    /// Not built yet: setting <see cref="Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView.StartPath"/>
    /// after the page has loaded does not re-navigate a running Blazor circuit. The real
    /// implementation resolves <c>NavigationManager</c> from <c>blazorWebView.Services</c> once the
    /// web view has initialised and calls <c>NavigateTo</c> on it (cluster L/M, whichever needs a
    /// second route in the same already-open window first).
    /// </remarks>
    public void NavigateTo(string aRoute)
    {
        blazorWebView.StartPath = aRoute;
    }
}
