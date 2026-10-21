namespace Chatur.Core.Actions;

/// <summary>One theme Chatur can show, as Settings ▸ Appearance lists it (REQ-UI-037, REQ-UI-038).</summary>
/// <param name="Name">The theme's name. One of the four built-in names matches a file under
/// <c>wwwroot/themes</c>; a theme added from a file is applied at runtime from
/// <paramref name="LightValues"/>/<paramref name="DarkValues"/> instead, so it needs no new build.</param>
/// <param name="Source">Whether the theme came with the build (<c>"build"</c>) or was added from a file (<c>"file"</c>).</param>
/// <param name="LightValues">The theme's light set of named colour values (REQ-UI-038).</param>
/// <param name="DarkValues">The theme's dark set of named colour values (REQ-UI-038).</param>
public sealed record ThemeSummary(
    string Name,
    string Source,
    IReadOnlyDictionary<string, string> LightValues,
    IReadOnlyDictionary<string, string> DarkValues);

/// <summary>How the app decides whether to show the light or dark set of the chosen theme.</summary>
public enum ThemeMode
{
    /// <summary>Always use the light set.</summary>
    Light,

    /// <summary>Always use the dark set.</summary>
    Dark,

    /// <summary>Use whichever set matches the operating system's current appearance.</summary>
    FollowSystem
}

/// <summary>The theme and light/dark choice persisted on Settings ▸ Appearance (REQ-UI-037).</summary>
/// <param name="ThemeName">The chosen theme's name.</param>
/// <param name="Mode">How the light/dark choice is made.</param>
public sealed record ThemeChoice(string ThemeName, ThemeMode Mode);
