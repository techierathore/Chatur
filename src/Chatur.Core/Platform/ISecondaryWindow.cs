namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over the secondary window (Architecture §6, 2026-09-22 "one secondary window
/// carrying everything that is not the work"): Prerequisites, Repository and every Settings page.
/// The MAUI head opens a real second window, or switches the open one to the page; the web harness
/// has one browser tab, so it navigates that tab (REQ-FN-106).
/// </summary>
public interface ISecondaryWindow
{
    /// <summary>
    /// Opens the secondary window on a page, or brings the open one forward on that page.
    /// </summary>
    /// <param name="aRoute">The page's route, e.g. <c>/settings/providers</c> or <c>/prerequisites</c>.</param>
    void Open(string aRoute);

    /// <summary>
    /// Closes the secondary window, leaving the main window as it was. The web harness, which shows
    /// the page in its one tab, goes back to the Workbench instead.
    /// </summary>
    void Close();
}
