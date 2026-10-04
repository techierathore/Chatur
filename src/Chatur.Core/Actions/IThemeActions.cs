namespace Chatur.Core.Actions;

/// <summary>
/// Listing, choosing and adding themes — a theme is a named set of OKLCH values held as data, never
/// colour written into a component (Architecture §1 Q8; page Settings ▸ Appearance).
/// </summary>
public interface IThemeActions
{
    /// <summary>Every theme Chatur can show, built-in and added (REQ-UI-037, REQ-UI-038).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<IReadOnlyList<ThemeSummary>> ListAsync(CancellationToken aCt = default);

    /// <summary>The theme and light/dark choice persisted on this machine (REQ-UI-037).</summary>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<ThemeChoice> CurrentAsync(CancellationToken aCt = default);

    /// <summary>
    /// Chooses a theme and whether it shows its light or dark set (REQ-UI-037).
    /// </summary>
    /// <param name="aThemeName">The theme to choose.</param>
    /// <param name="aDark">Whether to show the dark set.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    Task ChooseAsync(string aThemeName, bool aDark, CancellationToken aCt = default);

    /// <summary>
    /// Adds a theme from a file of OKLCH values, with no new build of Chatur (REQ-UI-038).
    /// </summary>
    /// <param name="aFilePath">The file to read the theme from.</param>
    /// <param name="aCt">A token that cancels the add.</param>
    Task<ThemeSummary> AddFromFileAsync(string aFilePath, CancellationToken aCt = default);
}
