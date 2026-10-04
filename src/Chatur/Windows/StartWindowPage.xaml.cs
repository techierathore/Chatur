namespace Chatur.Windows;

/// <summary>
/// The start window's page: opens the BlazorWebView on <c>/start</c> (UI Design "Screen: Start").
/// </summary>
public partial class StartWindowPage : ContentPage
{
    /// <summary>Creates the page and points the web view at Start.</summary>
    public StartWindowPage()
    {
        InitializeComponent();
        blazorWebView.StartPath = "/start";
    }
}
