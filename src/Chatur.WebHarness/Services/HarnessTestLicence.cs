using System.Text.Json;
using Chatur.Core.Licences;

namespace Chatur.WebHarness.Services;

/// <summary>
/// Reads the <c>testLicence</c> block of <c>tests/verify/test-settings.json</c> (the file beside the tests,
/// never an environment variable). When the block is there and <c>enabled</c> is true, the web harness holds the
/// licence in memory (<see cref="InMemoryLicenceSource"/>) instead of asking the shared App Manager, so a test
/// can forget a machine or switch a feature safely. The real app has no such block and always uses App Manager.
/// </summary>
public static class HarnessTestLicence
{
    private const string SettingsRelativePath = "tests/verify/test-settings.json";

    /// <summary>Reads the block, or returns <see langword="null"/> when the file, the block or its switch is absent.</summary>
    /// <param name="aConfiguration">The harness configuration; <c>Chatur:TestSettingsPath</c> may name the file.</param>
    /// <param name="aStartFolders">Folders to search upward from for the settings file.</param>
    public static TestLicenceOptions? Load(IConfiguration aConfiguration, IEnumerable<string> aStartFolders)
    {
        var vPath = aConfiguration[HarnessTestProvider.SettingsPathKey] is { Length: > 0 } vNamed ? vNamed : Find(aStartFolders);
        if (vPath is null || !File.Exists(vPath))
        {
            return null;
        }

        using var vDocument = JsonDocument.Parse(File.ReadAllText(vPath), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        if (!vDocument.RootElement.TryGetProperty("testLicence", out var vBlock)
            || vBlock.ValueKind != JsonValueKind.Object
            || !(vBlock.TryGetProperty("enabled", out var vEnabled) && vEnabled.ValueKind == JsonValueKind.True))
        {
            return null;
        }

        var vDevices = new List<TestLicenceDevice>();
        if (vBlock.TryGetProperty("devices", out var vList) && vList.ValueKind == JsonValueKind.Array)
        {
            foreach (var vDevice in vList.EnumerateArray())
            {
                vDevices.Add(new TestLicenceDevice(
                    Text(vDevice, "id", "device"),
                    Text(vDevice, "name", "Machine"),
                    Text(vDevice, "platform", string.Empty),
                    Text(vDevice, "appVersion", string.Empty),
                    Number(vDevice, "daysSinceSeen", 0),
                    Number(vDevice, "signInCount", 1),
                    Flag(vDevice, "thisDevice"),
                    Flag(vDevice, "blocked")));
            }
        }

        var vOff = new List<string>();
        if (vBlock.TryGetProperty("featuresOff", out var vOffList) && vOffList.ValueKind == JsonValueKind.Array)
        {
            vOff.AddRange(vOffList.EnumerateArray().Where(aItem => aItem.ValueKind == JsonValueKind.String).Select(aItem => aItem.GetString()!));
        }

        return new TestLicenceOptions(
            Text(vBlock, "name", "Professional"),
            Text(vBlock, "state", "Active"),
            Text(vBlock, "key", "TEST-LICENCE-KEY"),
            Number(vBlock, "daysRemaining", 287),
            Number(vBlock, "daysTotal", 365),
            Number(vBlock, "deviceCap", 1),
            Text(vBlock, "overCapWarning", "This licence allows fewer machines than are signed in."),
            vDevices,
            vOff);
    }

    private static string? Find(IEnumerable<string> aStartFolders)
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

    private static string Text(JsonElement aBlock, string aName, string aFallback) =>
        aBlock.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String ? vValue.GetString()! : aFallback;

    private static int Number(JsonElement aBlock, string aName, int aFallback) =>
        aBlock.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.Number ? vValue.GetInt32() : aFallback;

    private static bool Flag(JsonElement aBlock, string aName) =>
        aBlock.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.True;
}
