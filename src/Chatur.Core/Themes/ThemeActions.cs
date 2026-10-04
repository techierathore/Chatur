using System.Data;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Themes;

/// <summary>
/// <see cref="IThemeActions"/> over the <c>Theme</c> and <c>Setting</c> tables. A theme is a named
/// set of OKLCH-shaped colour values, held as data rather than compiled into a component (Coding
/// Standards "A theme is a file of OKLCH values"; REQ-UI-037, REQ-UI-038 — cluster L).
/// </summary>
public sealed class ThemeActions : IThemeActions
{
    /// <summary>
    /// The named colour tokens every theme's light and dark set must carry — the shape the four
    /// built-in themes already use in <c>0002-SeedRoles.sql</c>, and the shape a file added later
    /// is validated against.
    /// </summary>
    private static readonly string[] RequiredTokens =
    [
        "bg", "card", "fg", "dim", "faint", "line", "line2", "soft", "hover", "accent", "accentFg", "accentSoft"
    ];

    private readonly IDbConnectionFactory objConnections;
    private readonly AppState objAppState;

    /// <summary>
    /// Creates the action implementation.
    /// </summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aAppState">The shared state every window reads, updated so every window repaints (REQ-UI-037).</param>
    public ThemeActions(IDbConnectionFactory aConnections, AppState aAppState)
    {
        objConnections = aConnections;
        objAppState = aAppState;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ThemeSummary>> ListAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vRows = await vConnection.QueryAsync<(string Name, string Values, string Source)>(
            new CommandDefinition(
                "SELECT Name, \"Values\", Source FROM Theme ORDER BY ThemeId",
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRows.Select(r => ToSummary(r.Name, r.Values, r.Source)).ToList();
    }

    /// <inheritdoc />
    public async Task<ThemeChoice> CurrentAsync(CancellationToken aCt = default)
    {
        using var vConnection = objConnections.OpenConnection();
        var vName = await ReadSettingAsync(vConnection, "Appearance.ThemeName", aCt).ConfigureAwait(false);
        var vDark = await ReadSettingAsync(vConnection, "Appearance.IsDark", aCt).ConfigureAwait(false);

        return new ThemeChoice(vName ?? "amber", vDark is null || vDark == "1");
    }

    /// <inheritdoc />
    public async Task ChooseAsync(string aThemeName, bool aDark, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aThemeName))
        {
            throw new ArgumentException("A theme name is required.", nameof(aThemeName));
        }

        using var vConnection = objConnections.OpenConnection();

        var vExists = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT ThemeId FROM Theme WHERE Name = @Name",
                new { Name = aThemeName },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vExists is null)
        {
            throw new KeyNotFoundException($"No theme named '{aThemeName}'.");
        }

        await UpsertSettingAsync(vConnection, "Appearance.ThemeName", aThemeName, aCt).ConfigureAwait(false);
        await UpsertSettingAsync(vConnection, "Appearance.IsDark", aDark ? "1" : "0", aCt).ConfigureAwait(false);

        // Every open window reads AppState, so this one write repaints all of them (REQ-UI-037).
        objAppState.SetTheme(aThemeName, aDark);
    }

    /// <inheritdoc />
    public async Task<ThemeSummary> AddFromFileAsync(string aFilePath, CancellationToken aCt = default)
    {
        if (string.IsNullOrWhiteSpace(aFilePath) || !File.Exists(aFilePath))
        {
            throw new FileNotFoundException("The theme file was not found.", aFilePath);
        }

        var vName = ToThemeName(Path.GetFileNameWithoutExtension(aFilePath));
        var vJson = await File.ReadAllTextAsync(aFilePath, aCt).ConfigureAwait(false);

        ValidateShapeOrThrow(vJson, vName);

        using var vConnection = objConnections.OpenConnection();

        var vExists = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT ThemeId FROM Theme WHERE Name = @Name",
                new { Name = vName },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vExists is not null)
        {
            throw new InvalidOperationException($"A theme named '{vName}' already exists.");
        }

        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO Theme (Name, \"Values\", Source) VALUES (@Name, @Values, 'file')",
                new { Name = vName, Values = vJson },
                cancellationToken: aCt)).ConfigureAwait(false);

        return ToSummary(vName, vJson, "file");
    }

    private static string ToThemeName(string aFileStem)
    {
        var vName = aFileStem.Trim().ToLowerInvariant().Replace(' ', '-');
        if (vName.Length == 0 || !vName.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
        {
            throw new ArgumentException(
                $"'{aFileStem}' is not a usable theme name — use letters, digits and hyphens only.");
        }

        return vName;
    }

    private static void ValidateShapeOrThrow(string aJson, string aThemeName)
    {
        using JsonDocument vDoc = ParseOrThrow(aJson, aThemeName);
        var vRoot = vDoc.RootElement;
        ValidateSetOrThrow(vRoot, "light", aThemeName);
        ValidateSetOrThrow(vRoot, "dark", aThemeName);
    }

    private static JsonDocument ParseOrThrow(string aJson, string aThemeName)
    {
        try
        {
            return JsonDocument.Parse(aJson);
        }
        catch (JsonException aEx)
        {
            throw new ArgumentException($"'{aThemeName}' is not a valid theme file: {aEx.Message}", aEx);
        }
    }

    private static void ValidateSetOrThrow(JsonElement aRoot, string aSetName, string aThemeName)
    {
        if (!aRoot.TryGetProperty(aSetName, out var vSet) || vSet.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException(
                $"'{aThemeName}' is missing its \"{aSetName}\" set of colour values.");
        }

        var vMissing = RequiredTokens
            .Where(t => !vSet.TryGetProperty(t, out var vValue)
                || vValue.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(vValue.GetString()))
            .ToList();

        if (vMissing.Count > 0)
        {
            throw new ArgumentException(
                $"'{aThemeName}' is missing its {aSetName} value{(vMissing.Count > 1 ? "s" : "")} for: {string.Join(", ", vMissing)}.");
        }
    }

    private static ThemeSummary ToSummary(string aName, string aValuesJson, string aSource)
    {
        using var vDoc = JsonDocument.Parse(aValuesJson);
        var vLight = ReadSet(vDoc.RootElement, "light");
        var vDark = ReadSet(vDoc.RootElement, "dark");
        return new ThemeSummary(aName, aSource, vLight, vDark);
    }

    private static IReadOnlyDictionary<string, string> ReadSet(JsonElement aRoot, string aSetName)
    {
        if (!aRoot.TryGetProperty(aSetName, out var vSet) || vSet.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, string>();
        }

        return vSet.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty);
    }

    private static async Task<string?> ReadSettingAsync(IDbConnection aConnection, string aKey, CancellationToken aCt) =>
        await aConnection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                "SELECT Value FROM Setting WHERE Key = @Key",
                new { Key = aKey },
                cancellationToken: aCt)).ConfigureAwait(false);

    private static async Task UpsertSettingAsync(IDbConnection aConnection, string aKey, string aValue, CancellationToken aCt)
    {
        var vChanged = await aConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Setting SET Value = @Value WHERE Key = @Key",
                new { Key = aKey, Value = aValue },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vChanged == 0)
        {
            await aConnection.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO Setting (Key, Value) VALUES (@Key, @Value)",
                    new { Key = aKey, Value = aValue },
                    cancellationToken: aCt)).ConfigureAwait(false);
        }
    }
}
