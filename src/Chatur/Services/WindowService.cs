using Chatur.Windows;

namespace Chatur.Services;

/// <summary>
/// <see cref="IWindowService"/> over <see cref="Microsoft.Maui.Controls.Application"/>'s own
/// multi-window API. Keeps at most one instance of each window open, tracked by field rather than
/// by searching <c>Application.Current.Windows</c>, so a window this process opened is always the
/// one it activates.
/// </summary>
public sealed class WindowService : IWindowService
{
    private Window? objMainWindow;
    private Window? objSecondaryWindow;
    private SecondaryWindowPage? objSecondaryPage;

#if WINDOWS
    /// <summary>Whether the secondary window's page has been taken down and the window may now close. Only the Windows
    /// close button asks it; on a Mac the window's own close button ends the scene directly.</summary>
    private bool objSecondaryReadyToClose;
#endif

    /// <inheritdoc />
    public Window CreateStartWindow()
    {
        var vPage = new StartWindowPage();
        var vWindow = new Window(vPage) { Title = "Chatur" };
        vWindow.Destroying += (_, _) => vPage.ReleaseWebView();
        return vWindow;
    }

    /// <inheritdoc />
    public void ShowMainWindow()
    {
        if (objMainWindow is not null)
        {
            return;
        }

        var vPage = new MainWindowPage();
        objMainWindow = new Window(vPage) { Title = "Chatur" };
        objMainWindow.Destroying += (_, _) =>
        {
            vPage.ReleaseWebView();
            objMainWindow = null;
        };
        Application.Current?.OpenWindow(objMainWindow);
    }

    /// <inheritdoc />
    public void ShowSecondaryWindow(string aRoute)
    {
        if (objSecondaryWindow is null || objSecondaryPage is null)
        {
            var vPage = new SecondaryWindowPage(aRoute);
#if WINDOWS
            objSecondaryReadyToClose = false;
#endif
            objSecondaryPage = vPage;
            objSecondaryWindow = new Window(vPage) { Title = "Chatur" };
            var vWindow = objSecondaryWindow;
#if WINDOWS
            // The window's own close button: the page is taken down first, then the window closes (REQ-NFR-011).
            vWindow.HandlerChanged += (_, _) =>
            {
                if (vWindow.Handler?.PlatformView is Microsoft.UI.Xaml.Window vNative)
                {
                    vNative.AppWindow.Closing += async (_, aArgs) =>
                    {
                        if (objSecondaryReadyToClose)
                        {
                            return;
                        }

                        aArgs.Cancel = true;
                        await CloseSecondaryAsync(vWindow, vPage);
                    };
                }
            };
#endif
            objSecondaryWindow.Destroying += (_, _) =>
            {
                vPage.ReleaseWebView();
                objSecondaryWindow = null;
                objSecondaryPage = null;
            };
            Application.Current?.OpenWindow(objSecondaryWindow);
            return;
        }

        objSecondaryPage.NavigateTo(aRoute);
        Application.Current?.ActivateWindow(objSecondaryWindow);
    }

    /// <inheritdoc />
    public void CloseSecondaryWindow()
    {
        if (objSecondaryWindow is { } vWindow && objSecondaryPage is { } vPage)
        {
            _ = CloseSecondaryAsync(vWindow, vPage);
        }
    }

    /// <summary>
    /// Closes the secondary window once its page has been taken down while its web view is still there, so nothing
    /// is sent to a web view that has gone (REQ-NFR-011).
    /// </summary>
    private async Task CloseSecondaryAsync(Window aWindow, SecondaryWindowPage aPage)
    {
        await aPage.PrepareToCloseAsync();
#if WINDOWS
        objSecondaryReadyToClose = true;
#endif
        Application.Current?.CloseWindow(aWindow);
    }
}
