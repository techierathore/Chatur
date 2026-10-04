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

    /// <inheritdoc />
    public Window CreateStartWindow() =>
        new(new StartWindowPage()) { Title = "Chatur" };

    /// <inheritdoc />
    public void ShowMainWindow()
    {
        if (objMainWindow is not null)
        {
            return;
        }

        objMainWindow = new Window(new MainWindowPage()) { Title = "Chatur" };
        objMainWindow.Destroying += (_, _) => objMainWindow = null;
        Application.Current?.OpenWindow(objMainWindow);
    }

    /// <inheritdoc />
    public void ShowSecondaryWindow(string aRoute)
    {
        if (objSecondaryWindow is null || objSecondaryPage is null)
        {
            objSecondaryPage = new SecondaryWindowPage(aRoute);
            objSecondaryWindow = new Window(objSecondaryPage) { Title = "Chatur" };
            objSecondaryWindow.Destroying += (_, _) =>
            {
                objSecondaryWindow = null;
                objSecondaryPage = null;
            };
            Application.Current?.OpenWindow(objSecondaryWindow);
            return;
        }

        objSecondaryPage.NavigateTo(aRoute);
    }
}
