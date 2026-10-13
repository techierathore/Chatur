using Microsoft.AspNetCore.Components;

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
        blazorWebView.BlazorWebViewInitialized += (sender, args) => BlazorZoom.Attach(args.WebView);
    }

    /// <summary>
    /// Switches this already-open window to another route.
    /// </summary>
    /// <param name="aRoute">The route to show.</param>
    /// <remarks>
    /// Setting <see cref="Microsoft.AspNetCore.Components.WebView.Maui.BlazorWebView.StartPath"/>
    /// after the page has loaded does not re-navigate a running Blazor app, so the route goes to this
    /// window's own <see cref="NavigationManager"/> through the web view's dispatcher (REQ-FN-106).
    /// Before the web view is up, the start path is still the one it will open on.
    /// </remarks>
    public void NavigateTo(string aRoute)
    {
        blazorWebView.StartPath = aRoute;
        _ = blazorWebView.TryDispatchAsync(aServices => aServices.GetRequiredService<NavigationManager>().NavigateTo(aRoute));
    }
}
