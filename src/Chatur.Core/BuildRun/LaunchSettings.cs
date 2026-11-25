using System.Text.Json;
using System.Text.RegularExpressions;

namespace Chatur.Core.BuildRun;

/// <summary>
/// What a project's own <c>Properties/launchSettings.json</c> says about running it, read the way Visual Studio
/// reads it: the first profile whose <c>commandName</c> is <c>Project</c> is the one that runs, with its
/// environment and its addresses (REQ-FN-306, BRD-236). Before 21 Nov 2026 Chatur ran every project with
/// <c>--no-launch-profile</c>, so a web project came up in Production on http://localhost:5000 instead of the
/// address its launch settings name, and no browser was ever opened (owner's evening test).
/// </summary>
public static partial class LaunchSettings
{
    /// <summary>One launch profile, as far as running the project needs it.</summary>
    /// <param name="Name">The profile's name, passed to <c>dotnet run --launch-profile</c>.</param>
    /// <param name="ApplicationUrls">The addresses the profile has the app listen on, in the file's order.</param>
    /// <param name="LaunchUrl">The page the profile opens, relative to the first address, or an absolute address.</param>
    /// <param name="LaunchBrowser">Whether the profile asks for a browser once the app is up.</param>
    public sealed record Profile(string Name, IReadOnlyList<string> ApplicationUrls, string? LaunchUrl, bool LaunchBrowser);

    private static readonly JsonDocumentOptions ReadOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// The profile <c>dotnet run</c> would use for this project file: the first with <c>commandName</c> <c>Project</c>,
    /// or none when the project has no launch settings, no such profile, or a file that cannot be read.
    /// </summary>
    /// <param name="aProjectFile">The full path of the project's <c>.csproj</c>.</param>
    public static Profile? ProjectProfile(string aProjectFile)
    {
        var vFolder = Path.GetDirectoryName(aProjectFile);
        if (string.IsNullOrEmpty(vFolder))
        {
            return null;
        }

        var vFile = Path.Combine(vFolder, "Properties", "launchSettings.json");
        if (!File.Exists(vFile))
        {
            return null;
        }

        try
        {
            using var vDocument = JsonDocument.Parse(File.ReadAllText(vFile), ReadOptions);
            if (!vDocument.RootElement.TryGetProperty("profiles", out var vProfiles) || vProfiles.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var vProfile in vProfiles.EnumerateObject())
            {
                if (vProfile.Value.ValueKind != JsonValueKind.Object
                    || !string.Equals(StringOf(vProfile.Value, "commandName"), "Project", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var vUrls = (StringOf(vProfile.Value, "applicationUrl") ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var vLaunchBrowser = vProfile.Value.TryGetProperty("launchBrowser", out var vBrowser)
                    && vBrowser.ValueKind == JsonValueKind.True;
                return new Profile(vProfile.Name, vUrls, StringOf(vProfile.Value, "launchUrl"), vLaunchBrowser);
            }
        }
        catch (Exception aException) when (aException is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    /// <summary>
    /// Whether a project is a web project, from its SDK: a browser is opened for one of these once it is up.
    /// </summary>
    /// <param name="aProjectText">The text of the project's <c>.csproj</c>.</param>
    public static bool IsWebProject(string aProjectText)
    {
        var vSdk = SdkAttribute().Match(aProjectText).Groups[1].Value;
        return vSdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)
            || vSdk.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The address a "Now listening on: …" line names, with a wildcard host written as <c>localhost</c>, or none
    /// when the line is not one.
    /// </summary>
    /// <param name="aLine">One line of the program's output.</param>
    public static string? ListeningAddress(string aLine)
    {
        var vMatch = ListeningLine().Match(aLine);
        return vMatch.Success ? Browsable(vMatch.Groups["url"].Value) : null;
    }

    /// <summary>
    /// The address to open in the browser: the profile's first address (with its launch page) when it has one,
    /// else the address the program said it listens on.
    /// </summary>
    /// <param name="aProfile">The project's launch profile, if any.</param>
    /// <param name="aListening">The address the program printed in its "Now listening on" line.</param>
    public static string BrowserAddress(Profile? aProfile, string aListening)
    {
        var vBase = aProfile is { ApplicationUrls.Count: > 0 } ? Browsable(aProfile.ApplicationUrls[0]) : aListening;
        var vLaunch = aProfile?.LaunchUrl;
        if (string.IsNullOrWhiteSpace(vLaunch))
        {
            return vBase;
        }

        if (Uri.TryCreate(vLaunch, UriKind.Absolute, out var vAbsolute) && (vAbsolute.Scheme == Uri.UriSchemeHttp || vAbsolute.Scheme == Uri.UriSchemeHttps))
        {
            return vLaunch;
        }

        return vBase.TrimEnd('/') + "/" + vLaunch.TrimStart('/');
    }

    private static string Browsable(string aUrl) =>
        WildcardHost().Replace(aUrl.Trim(), "${scheme}localhost");

    private static string? StringOf(JsonElement aElement, string aName) =>
        aElement.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String ? vValue.GetString() : null;

    [GeneratedRegex(@"Now listening on:\s*(?<url>https?://\S+)", RegexOptions.IgnoreCase)]
    private static partial Regex ListeningLine();

    [GeneratedRegex(@"^(?<scheme>https?://)(\*|\+|0\.0\.0\.0|\[::\])(?=[:/]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex WildcardHost();

    [GeneratedRegex(@"<Project\s+Sdk=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex SdkAttribute();
}
