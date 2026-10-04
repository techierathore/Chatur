using Microsoft.UI.Xaml;

namespace Chatur.WinUI;

/// <summary>
/// The WinUI application object the Windows platform head actually starts — the entry point MAUI's
/// SDK generates a <c>Main</c> for. Delegates straight to <see cref="MauiProgram.CreateMauiApp"/>,
/// the same as every other platform.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>Initializes the singleton application object.</summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override MauiApp CreateMauiApp() => Chatur.MauiProgram.CreateMauiApp();
}
