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

/// <summary>The theme and light/dark choice persisted on Settings ▸ Appearance (REQ-UI-037).</summary>
/// <param name="ThemeName">The chosen theme's name.</param>
/// <param name="IsDark">Whether the dark set is showing.</param>
public sealed record ThemeChoice(string ThemeName, bool IsDark);
