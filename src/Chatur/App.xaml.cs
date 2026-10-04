using Chatur.Services;

namespace Chatur;

/// <summary>
/// The MAUI application object. Opens the start window first — nothing is open yet, so there is no
/// menu bar, no files and no conversation (Architecture §6, 2026-09-22 "the start window opens
/// first").
/// </summary>
public partial class App : Application
{
    private readonly IWindowService objWindows;

    /// <summary>
    /// Creates the application.
    /// </summary>
    /// <param name="aWindows">The window service, resolved from the container <see cref="MauiProgram"/> built.</param>
    public App(IWindowService aWindows)
    {
        objWindows = aWindows;
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override Window CreateWindow(IActivationState? aActivationState) =>
        objWindows.CreateStartWindow();
}
