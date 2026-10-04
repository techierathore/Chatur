namespace Chatur.Services;

/// <summary>
/// Opens and activates Chatur's three windows — start, main and secondary (Architecture §6,
/// 2026-09-22 "Three windows, not one"). No cluster opens a <see cref="Window"/> directly; every
/// screen that needs another window goes through this.
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Creates the start window. Called exactly once, by <see cref="App.CreateWindow"/>.
    /// </summary>
    /// <returns>The new window, showing <c>/start</c>.</returns>
    Window CreateStartWindow();

    /// <summary>
    /// Opens the main window, showing the Workbench. Activates it instead of opening a second one
    /// if it is already open.
    /// </summary>
    void ShowMainWindow();

    /// <summary>
    /// Opens the secondary window on a given route, or activates it and switches its route if it is
    /// already open (UI Design "Screen: Settings", "Screen: Prerequisites", "Screen: Repository").
    /// </summary>
    /// <param name="aRoute">The route to show, e.g. <c>/prerequisites</c> or <c>/settings/providers</c>.</param>
    void ShowSecondaryWindow(string aRoute);
}
