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

    /// <summary>
    /// Ends this window's Blazor app as the window closes: its components are disposed and let go of the app-wide
    /// events they listen to (REQ-NFR-011). Without it a closed window's components stayed subscribed, drew into a
    /// web view that was gone at every later project switch, and each draw left two "Unobserved task exception"
    /// lines in the log (owner's log, 21 Nov 2026, 20:36:46). MAUI 10 disposes the app without blocking the window.
    /// </summary>
    public void ReleaseWebView()
    {
        // First the page is marked gone, so its components send nothing more to a script that is going: a call into
        // a closed web view made the web view's own error report fail, and each failure was another unobserved task
        // exception. On the window's own thread this runs at once, before the app is disposed below.
        _ = blazorWebView.TryDispatchAsync(aServices => ChaturUI.Services.JsCleanUp.MarkGone(aServices.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()));
        blazorWebView.Handler?.DisconnectHandler();
    }

    /// <summary>
    /// Takes the page down while its web view is still there (REQ-NFR-011): the page draws nothing, every component's
    /// clean-up reaches a live page, and only then may the window close.
    /// </summary>
    public async Task PrepareToCloseAsync()
    {
        try
        {
            await blazorWebView.TryDispatchAsync(aServices => aServices.GetRequiredService<ChaturUI.Services.WindowLifetime>().Close());
            // The empty page's render and the components' clean-up messages are sent within a few frames.
            await Task.Delay(400);
        }
        catch (Exception aException) when (aException is InvalidOperationException or ObjectDisposedException)
        {
            // the web view is already gone; nothing is left to take down
        }
    }
}
