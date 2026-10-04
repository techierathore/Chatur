namespace Chatur.Windows;

/// <summary>
/// The main window's page: opens the BlazorWebView on the Workbench (UI Design "Screen: Workbench").
/// </summary>
public partial class MainWindowPage : ContentPage
{
    /// <summary>Creates the page and points the web view at the Workbench.</summary>
    public MainWindowPage()
    {
        InitializeComponent();
        blazorWebView.StartPath = "/";
    }
}
