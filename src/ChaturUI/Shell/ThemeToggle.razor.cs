using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// The title bar's light/dark switch (REQ-UI-037, UI Design "Screen: Start" <c>startbar</c>): flips
/// the dark set on and off without changing which theme is chosen, through
/// <see cref="IThemeActions"/>, and repaints its own icon whenever <see cref="AppState"/> changes.
/// </summary>
public partial class ThemeToggle : IDisposable
{
    [Inject]
    private IThemeActions ThemeActionsService { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() => AppStateService.Changed += OnAppStateChanged;

    /// <summary>Switches light/dark, keeping the chosen theme (REQ-UI-037).</summary>
    private async Task ToggleModeAsync() =>
        await ThemeActionsService.ChooseAsync(AppStateService.ThemeName, !AppStateService.IsDark);

    private void OnAppStateChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => AppStateService.Changed -= OnAppStateChanged;
}
