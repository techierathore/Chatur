using Chatur.Core.Actions;

namespace Chatur.Core;

/// <summary>
/// The shared, in-memory state every screen reads: the selected project, the signed-in user and the
/// chosen theme (foundation brief §"src/ChaturUI" — "a shared app-state service ... lives behind the
/// action layer"). A view never sets these fields itself; an action method that changes one of them
/// updates <see cref="AppState"/> so every open window sees the same value, and raises
/// <see cref="Changed"/> so a component can re-render.
/// </summary>
public sealed class AppState
{
    /// <summary>Raised after any property on this instance changes.</summary>
    public event Action? Changed;

    /// <summary>The project every screen is currently working in, or <see langword="null"/> when none is selected.</summary>
    public ProjectSummary? SelectedProject { get; private set; }

    /// <summary>The signed-in owner, or <see langword="null"/> when nobody is signed in.</summary>
    public CurrentUser? CurrentUser { get; private set; }

    /// <summary>The chosen theme's name — <c>"amber"</c> until Settings ▸ Appearance changes it.</summary>
    public string ThemeName { get; private set; } = "amber";

    /// <summary>Whether the chosen theme is showing its dark set.</summary>
    public bool IsDark { get; private set; } = true;

    /// <summary>
    /// Records the project every screen should now show.
    /// </summary>
    /// <param name="aProject">The newly selected project, or <see langword="null"/> when it was closed.</param>
    public void SetSelectedProject(ProjectSummary? aProject)
    {
        SelectedProject = aProject;
        RaiseChanged();
    }

    /// <summary>
    /// Records who is signed in.
    /// </summary>
    /// <param name="aUser">The signed-in owner, or <see langword="null"/> after sign-out.</param>
    public void SetCurrentUser(CurrentUser? aUser)
    {
        CurrentUser = aUser;
        RaiseChanged();
    }

    /// <summary>
    /// Records the chosen theme.
    /// </summary>
    /// <param name="aThemeName">The theme's name, matching a file under <c>wwwroot/themes</c>.</param>
    /// <param name="aIsDark">Whether the dark set of that theme is showing.</param>
    public void SetTheme(string aThemeName, bool aIsDark)
    {
        ThemeName = aThemeName;
        IsDark = aIsDark;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();
}
