namespace Chatur.Windows;

/// <summary>
/// The main window's page: opens the BlazorWebView on the Workbench (UI Design "Screen: Workbench").
/// </summary>
public partial class MainWindowPage : ContentPage
{
    /// <summary>Creates the page, points the web view at the Workbench and gives it the OS zoom keys (§9b).</summary>
    public MainWindowPage()
    {
        InitializeComponent();
        blazorWebView.StartPath = "/";
        blazorWebView.BlazorWebViewInitialized += (sender, args) => BlazorZoom.Attach(args.WebView);
    }
}
