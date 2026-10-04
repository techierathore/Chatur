using Chatur.Core;
using Chatur.Core.Actions;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// Loads the persisted theme once per window and repaints the document whenever
/// <see cref="AppState"/>'s theme changes, so every open window shows the same choice at once
/// (REQ-UI-037). A theme added from a file (REQ-UI-038) is applied through
/// <see cref="ThemeInterop.ApplyCustomThemeAsync"/> instead of the static stylesheet the four
/// built-in themes use.
/// </summary>
public partial class ThemeHost : ComponentBase, IDisposable
{
    [Inject]
    private IThemeActions ThemeActionsService { get; set; } = default!;

    [Inject]
    private AppState AppState { get; set; } = default!;

    [Inject]
    private ThemeInterop ThemeInterop { get; set; } = default!;

    private IReadOnlyList<ThemeSummary> objThemes = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AppState.Changed += OnAppStateChanged;

        try
        {
            objThemes = await ThemeActionsService.ListAsync();
            var vCurrent = await ThemeActionsService.CurrentAsync();
            AppState.SetTheme(vCurrent.ThemeName, vCurrent.IsDark);
        }
        catch (NotImplementedException)
        {
            // The theme table has not been wired up yet on this build; AppState already carries
            // its own default (amber, dark) and ApplyAsync below still paints that.
        }

        // ApplyAsync is JS interop and used to run here, but every route in the app is rendered with
        // an initial static-prerender pass before the SignalR circuit exists (Blazor Server), and JS
        // interop during that pass throws "JavaScript interop calls cannot be issued at this time" —
        // observed on every route (/, /sign-in, /start, ...) while smoke-testing REQ-UI-005 through
        // REQ-UI-009, unrelated to those rows. Deferred to OnAfterRenderAsync(firstRender) below,
        // which only ever runs once the circuit is live, exactly as the exception's own message says.
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool aFirstRender)
    {
        if (aFirstRender)
        {
            await ApplyAsync().ConfigureAwait(false);
        }
    }

    private void OnAppStateChanged() => _ = InvokeAsync(ApplyAsync);

    private async Task ApplyAsync()
    {
        // Re-read the list on every repaint so a theme added from a file since startup (or re-added
        // with new colours) has its tokens known the moment it is chosen (REQ-UI-038).
        try
        {
            objThemes = await ThemeActionsService.ListAsync();
        }
        catch (NotImplementedException)
        {
            // Theme table not wired; fall through to the built-in path.
        }

        var vTheme = objThemes.FirstOrDefault(t => t.Name == AppState.ThemeName);

        if (vTheme is { Source: "file" })
        {
            await ThemeInterop.ApplyCustomThemeAsync(vTheme.Name, vTheme.LightValues, vTheme.DarkValues, AppState.IsDark)
                .ConfigureAwait(false);
        }
        else
        {
            await ThemeInterop.SetThemeAsync(AppState.ThemeName, AppState.IsDark).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose() => AppState.Changed -= OnAppStateChanged;
}
