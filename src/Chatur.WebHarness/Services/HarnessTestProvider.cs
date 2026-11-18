using System.Text.Json;

namespace Chatur.WebHarness.Services;

/// <summary>
/// The model provider the web harness seeds for the verify tests, read from the <c>testProvider</c>
/// block of <c>tests/verify/test-settings.json</c> (the same file the browser tests read through
/// <c>tests/verify/support/settings.ts</c>). Nothing about any one provider is written in code: the
/// name, connector, address and model all come from that file, and the key too unless the file leaves
/// it empty, in which case configuration's <c>Chatur:TestProvider:Key</c> (the project's user secrets in
/// Development) is the fallback.
/// </summary>
/// <param name="Name">The provider's name as Settings ▸ Providers shows it.</param>
/// <param name="Connector">The TechieRag connector, e.g. the value the Add provider dialog stores.</param>
/// <param name="BaseUrl">The provider's web address; empty for a connector with a fixed endpoint.</param>
/// <param name="Model">The one model identifier filed under the provider.</param>
/// <param name="Key">The key, or <see langword="null"/> when neither the file nor configuration holds one.</param>
public sealed record HarnessTestProvider(string Name, string Connector, string BaseUrl, string Model, string? Key)
{
    /// <summary>The configuration key that holds the fallback key, and the name the key is filed under in the secret store.</summary>
    public const string SecretName = "Chatur:TestProvider:Key";

    /// <summary>The configuration key that may name the settings file directly, overriding the search.</summary>
    public const string SettingsPathKey = "Chatur:TestSettingsPath";

    private const string SettingsRelativePath = "tests/verify/test-settings.json";

    /// <summary>
    /// Reads the test provider, or returns <see langword="null"/> with a reason when the file or its
    /// <c>testProvider</c> block is missing or incomplete. The reason never contains the key.
    /// </summary>
    /// <param name="aConfiguration">The harness configuration (user secrets in Development).</param>
    /// <param name="aStartFolders">Folders to search upward from for <c>tests/verify/test-settings.json</c>.</param>
    /// <param name="aReason">Why nothing was read, when the result is <see langword="null"/>.</param>
    public static HarnessTestProvider? Load(IConfiguration aConfiguration, IEnumerable<string> aStartFolders, out string aReason)
    {
        var vPath = aConfiguration[SettingsPathKey] is { Length: > 0 } vNamed ? vNamed : FindSettingsFile(aStartFolders);
        if (vPath is null || !File.Exists(vPath))
        {
            aReason = $"{SettingsRelativePath} was not found";
            return null;
        }

        using var vDocument = JsonDocument.Parse(File.ReadAllText(vPath), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (!vDocument.RootElement.TryGetProperty("testProvider", out var vBlock) || vBlock.ValueKind != JsonValueKind.Object)
        {
            aReason = $"{vPath} has no testProvider block";
            return null;
        }

        var vName = Text(vBlock, "name");
        var vConnector = Text(vBlock, "connector");
        var vModel = Text(vBlock, "model");
        if (vName.Length == 0 || vConnector.Length == 0 || vModel.Length == 0)
        {
            aReason = $"the testProvider block in {vPath} needs name, connector and model";
            return null;
        }

        var vKey = Text(vBlock, "key");
        if (vKey.Length == 0)
        {
            vKey = aConfiguration[SecretName] ?? string.Empty;
        }

        aReason = string.Empty;
        return new HarnessTestProvider(vName, vConnector, Text(vBlock, "baseUrl"), vModel, vKey.Length == 0 ? null : vKey);
    }

    /// <summary>
    /// The owner test user's email from <c>testUsers.Owner.email</c> in the same settings file, or
    /// <see langword="null"/> when the file or the entry is missing. Only that account is given the
    /// fixture project and the test provider at its first sign-in; every other account starts empty,
    /// as in the app (BRD-210).
    /// </summary>
    /// <param name="aConfiguration">The harness configuration.</param>
    /// <param name="aStartFolders">Folders to search upward from for <c>tests/verify/test-settings.json</c>.</param>
    public static string? OwnerEmail(IConfiguration aConfiguration, IEnumerable<string> aStartFolders)
    {
        var vPath = aConfiguration[SettingsPathKey] is { Length: > 0 } vNamed ? vNamed : FindSettingsFile(aStartFolders);
        if (vPath is null || !File.Exists(vPath))
        {
            return null;
        }

        using var vDocument = JsonDocument.Parse(File.ReadAllText(vPath), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (vDocument.RootElement.TryGetProperty("testUsers", out var vUsers)
            && vUsers.ValueKind == JsonValueKind.Object
            && vUsers.TryGetProperty("Owner", out var vOwner)
            && vOwner.ValueKind == JsonValueKind.Object)
        {
            var vEmail = Text(vOwner, "email");
            return vEmail.Length == 0 ? null : vEmail;
        }

        return null;
    }

    private static string? FindSettingsFile(IEnumerable<string> aStartFolders)
    {
        foreach (var vStart in aStartFolders)
        {
            for (var vFolder = new DirectoryInfo(vStart); vFolder is not null; vFolder = vFolder.Parent)
            {
                var vCandidate = Path.Combine(vFolder.FullName, SettingsRelativePath);
                if (File.Exists(vCandidate))
                {
                    return vCandidate;
                }
            }
        }

        return null;
    }

    private static string Text(JsonElement aBlock, string aName) =>
        aBlock.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String
            ? vValue.GetString()!.Trim()
            : string.Empty;
}
