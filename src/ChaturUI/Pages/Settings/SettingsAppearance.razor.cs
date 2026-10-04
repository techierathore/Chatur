using System.Text.Json;
using Chatur.Core.Actions;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Appearance (mockups/settings-appearance.html). Lists every theme (REQ-UI-037),
/// chooses one and the light/dark mode, persists both through <see cref="IThemeActions"/> — which
/// updates <see cref="Chatur.Core.AppState"/> so every open window repaints at once — and adds a
/// theme from a file (REQ-UI-038).
/// </summary>
public partial class SettingsAppearance
{
    private IReadOnlyList<ThemeSummary>? objThemes;
    private bool? objAddDialogOpen;
    private string objNewThemePath = string.Empty;
    private string? objAddError;
    private bool objIsAdding;

    private static readonly (string Token, string Label)[] SwatchTokens =
    [("bg", "background"), ("card", "card"), ("accent", "accent"), ("fg", "text")];

    private static readonly Dictionary<string, string> BuiltInDescriptions = new()
    {
        ["amber"] = "Warm greys and a soft orange. The one Chatur opens with.",
        ["indigo"] = "Cool greys under a blue-violet accent.",
        ["teal"] = "Quiet blue-green. Easy on a long session.",
        ["slate"] = "Almost no colour at all. The plainest of the four.",
    };

    private ThemeSummary? CurrentTheme => objThemes?.FirstOrDefault(t => t.Name == State.ThemeName);

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    /// <summary>The line under a theme's name: a built-in theme's own wording, or where an added one came from.</summary>
    private static string DescriptionOf(ThemeSummary aTheme) =>
        aTheme.Source == "file"
            ? "Added from a file."
            : BuiltInDescriptions.GetValueOrDefault(aTheme.Name, "Ships with Chatur.");

    /// <summary>The four colours a card shows — background, card, accent and text — read from the theme's own stored values for the mode showing.</summary>
    private IEnumerable<(string Label, string Colour)> SwatchColours(ThemeSummary aTheme)
    {
        var vValues = State.IsDark ? aTheme.DarkValues : aTheme.LightValues;
        return SwatchTokens
            .Select(t => (t.Label, Colour: vValues.GetValueOrDefault(t.Token, string.Empty)))
            .Where(t => t.Colour.Length > 0);
    }

    /// <summary>Saves the chosen theme as a file that can be edited or passed on, in the shape "Add a theme from a file" reads (REQ-UI-038).</summary>
    private async Task ExportAsync()
    {
        if (CurrentTheme is not { } vTheme)
        {
            return;
        }

        var vJson = JsonSerializer.Serialize(
            new { light = vTheme.LightValues, dark = vTheme.DarkValues },
            new JsonSerializerOptions { WriteIndented = true });
        await FileDownload.DownloadTextAsync($"{vTheme.Name}.json", vJson);
        Toast.Success($"'{vTheme.Name}' was written out as {vTheme.Name}.json.");
    }

    private async Task LoadAsync()
    {
        objThemes = await ThemeActions.ListAsync();
    }

    /// <summary>Chooses a theme, keeping the current light/dark mode (REQ-UI-037).</summary>
    private async Task ChooseAsync(string aThemeName)
    {
        await ThemeActions.ChooseAsync(aThemeName, State.IsDark);
        Toast.Success($"{aThemeName} is now the chosen theme.");
    }

    /// <summary>Switches light/dark without changing the theme (REQ-UI-037).</summary>
    private async Task OnModeChangedAsync(string aMode) =>
        await ThemeActions.ChooseAsync(State.ThemeName, aMode == "dark");

    /// <summary>Back to Amber, dark — the shipped default.</summary>
    private async Task ResetAsync() => await ThemeActions.ChooseAsync("amber", true);

    /// <summary>Reads the typed file, validates its shape, and adds it beside the built-in themes (REQ-UI-038).</summary>
    private async Task AddThemeAsync()
    {
        objIsAdding = true;
        objAddError = null;
        try
        {
            var vAdded = await ThemeActions.AddFromFileAsync(objNewThemePath);
            objThemes = await ThemeActions.ListAsync();
            objNewThemePath = string.Empty;
            objAddDialogOpen = false;
            Toast.Success($"'{vAdded.Name}' joined the list — no new build needed.");
        }
        catch (Exception aEx) when (aEx is ArgumentException or InvalidOperationException or FileNotFoundException)
        {
            objAddError = aEx.Message;
        }
        finally
        {
            objIsAdding = false;
        }
    }
}
