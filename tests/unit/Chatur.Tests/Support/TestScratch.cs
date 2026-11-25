namespace Chatur.Tests.Support;

/// <summary>
/// Scratch space for a unit test: a folder under this test project's own bin folder, never the system's temp folder,
/// so a test run makes nothing outside the repository (owner, 21 Nov 2026). Each test still deletes what it made.
/// </summary>
public static class TestScratch
{
    /// <summary>The scratch folder, ending with a separator, as <see cref="Path.GetTempPath"/> did.</summary>
    public static string Root { get; } = CreateRoot();

    /// <summary>An empty file of its own in the scratch folder, as <see cref="Path.GetTempFileName"/> made one.</summary>
    public static string NewFile()
    {
        var vPath = Path.Combine(Root, $"tmp-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(vPath, string.Empty);
        return vPath;
    }

    private static string CreateRoot()
    {
        var vRoot = Path.Combine(AppContext.BaseDirectory, "scratch");
        Directory.CreateDirectory(vRoot);
        return vRoot + Path.DirectorySeparatorChar;
    }
}
