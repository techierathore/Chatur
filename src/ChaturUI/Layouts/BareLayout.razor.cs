using Chatur.Core;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Layouts;

/// <summary>
/// The window shell for Sign in and Register: a wordmark bar and centred content, no menu bar, no
/// files and no conversation — there is nothing to work on until the owner is signed in
/// (Architecture §1 "Three windows"; UI Design "Design system"). Also carries the window's own
/// light/dark toggle (mockups/sign-in.html, mockups/register.html "window-tools") — the same
/// <see cref="AppState"/> + <see cref="ThemeInterop"/> pair Settings ▸ Appearance (cluster L, REQ-UI-037)
/// will drive from its own screen; this is only the in-window switch every window mockup shows,
/// never a second place the choice is decided. It does not touch <c>IThemeActions</c>, so it does
/// not persist across a restart — cluster L's row.
/// </summary>
public partial class BareLayout : IDisposable
{
    private static readonly string[] Palette = ["amber", "indigo", "teal", "slate"];

    [Inject]
    private AppState AppState { get; set; } = default!;

    [Inject]
    private ThemeInterop ThemeInterop { get; set; } = default!;

    private string ThemeDisplayName =>
        AppState.ThemeName.Length > 0
            ? char.ToUpperInvariant(AppState.ThemeName[0]) + AppState.ThemeName[1..]
            : AppState.ThemeName;

    /// <inheritdoc />
    protected override void OnInitialized() => AppState.Changed += OnAppStateChanged;

    private async Task ToggleDarkModeAsync()
    {
        var vIsDark = !AppState.IsDark;
        AppState.SetTheme(AppState.ThemeName, vIsDark);
        await ThemeInterop.SetThemeAsync(AppState.ThemeName, vIsDark);
    }

    private async Task CyclePaletteAsync()
    {
        var vIndex = Array.IndexOf(Palette, AppState.ThemeName);
        var vNext = Palette[(vIndex + 1) % Palette.Length];
        AppState.SetTheme(vNext, AppState.IsDark);
        await ThemeInterop.SetThemeAsync(vNext, AppState.IsDark);
    }

    private void OnAppStateChanged() => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => AppState.Changed -= OnAppStateChanged;
}
