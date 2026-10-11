namespace Chatur.Windows;

/// <summary>
/// The start window's page: opens the BlazorWebView on <c>/start</c> (UI Design "Screen: Start").
/// </summary>
public partial class StartWindowPage : ContentPage
{
    /// <summary>Creates the page, points the web view at Start and gives it the OS zoom keys (§9b).</summary>
    public StartWindowPage()
    {
        InitializeComponent();
        blazorWebView.StartPath = "/start";
        blazorWebView.BlazorWebViewInitialized += (sender, args) => BlazorZoom.Attach(args.WebView);
    }
}
