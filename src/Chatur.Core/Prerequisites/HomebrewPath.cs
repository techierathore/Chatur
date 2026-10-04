namespace Chatur.Core.Prerequisites;

/// <summary>
/// Adds Homebrew's own directories to a probe's <c>PATH</c>. An app launched from Finder on macOS
/// does not inherit the login shell's <c>PATH</c>, so a tool installed with Homebrew is otherwise
/// invisible to a child process Chatur starts (REQ-FN-012).
/// </summary>
public static class HomebrewPath
{
    /// <summary>Where Homebrew puts its symlinks: Apple Silicon, then Intel.</summary>
    public static readonly IReadOnlyList<string> HomebrewDirectories = new[] { "/opt/homebrew/bin", "/usr/local/bin" };

    /// <summary>
    /// Appends every Homebrew directory not already present to <paramref name="aExistingPath"/>, when
    /// probing on macOS. Any other platform gets its <c>PATH</c> back unchanged.
    /// </summary>
    /// <param name="aExistingPath">The process's own <c>PATH</c> value before augmentation.</param>
    /// <param name="aIsMacOs">
    /// Whether the probe is running on macOS. Taken as a parameter, rather than read inline from
    /// <see cref="OperatingSystem.IsMacOS"/>, so the Mac branch can be exercised in a unit test on
    /// any machine.
    /// </param>
    /// <returns>The augmented <c>PATH</c>, always using macOS's own <c>:</c> separator.</returns>
    public static string Augment(string? aExistingPath, bool aIsMacOs)
    {
        var vExisting = aExistingPath ?? string.Empty;
        if (!aIsMacOs)
        {
            return vExisting;
        }

        var vEntries = vExisting.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var vDirectory in HomebrewDirectories)
        {
            if (!vEntries.Contains(vDirectory))
            {
                vEntries.Add(vDirectory);
            }
        }

        return string.Join(':', vEntries);
    }
}
