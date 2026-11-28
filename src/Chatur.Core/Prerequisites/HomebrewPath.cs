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

    /// <summary>Where the .NET installer for macOS puts <c>dotnet</c>, which a Finder-started app does not see.</summary>
    public const string DotNetInstallDirectory = "/usr/local/share/dotnet";

    /// <summary>
    /// The <c>PATH</c> a Mac app started from Finder, the Dock or <c>open</c> should run with: its own (launchd gives
    /// only <c>/usr/bin:/bin:/usr/sbin:/sbin</c>), then what a login shell adds from <c>/etc/paths</c> and every file in
    /// <c>/etc/paths.d</c> (macOS's own <c>path_helper</c>, where the .NET installer registers
    /// <c>/usr/local/share/dotnet</c> and <c>~/.dotnet/tools</c>), then Homebrew and the .NET install folder. Without it
    /// the installed app cannot start <c>dotnet</c>, <c>git</c> or <c>node</c> (27 Nov 2026, the first Mac install).
    /// </summary>
    /// <param name="aExistingPath">The process's own <c>PATH</c>.</param>
    /// <param name="aPathFileLines">The lines of <c>/etc/paths</c> followed by those of each <c>/etc/paths.d</c> file, in order.</param>
    /// <param name="aHomeFolder">The user's home folder, for a line that starts with <c>~</c>.</param>
    /// <returns>The full <c>PATH</c>; each folder once, the existing ones first.</returns>
    public static string ForMacApp(string? aExistingPath, IEnumerable<string> aPathFileLines, string aHomeFolder)
    {
        var vEntries = (aExistingPath ?? string.Empty).Split(':', StringSplitOptions.RemoveEmptyEntries).ToList();
        var vCandidates = aPathFileLines
            .Select(aLine => aLine.Trim())
            .Where(aLine => aLine.Length > 0 && !aLine.StartsWith('#'))
            .Select(aLine => aLine.StartsWith('~') ? aHomeFolder.TrimEnd('/') + aLine[1..] : aLine)
            .Concat(HomebrewDirectories)
            .Append(DotNetInstallDirectory);
        foreach (var vDirectory in vCandidates)
        {
            if (!vEntries.Contains(vDirectory))
            {
                vEntries.Add(vDirectory);
            }
        }

        return string.Join(':', vEntries);
    }

    /// <summary>
    /// Reads <c>/etc/paths</c> and the <c>/etc/paths.d</c> files (sorted by name, as <c>path_helper</c> reads them) and
    /// returns <see cref="ForMacApp"/> for this process. A file that cannot be read is left out.
    /// </summary>
    /// <param name="aExistingPath">The process's own <c>PATH</c>.</param>
    /// <returns>The full <c>PATH</c> for a Mac app.</returns>
    public static string ForThisMacApp(string? aExistingPath)
    {
        var vFiles = new List<string> { "/etc/paths" };
        try
        {
            if (Directory.Exists("/etc/paths.d"))
            {
                vFiles.AddRange(Directory.GetFiles("/etc/paths.d").OrderBy(aName => aName, StringComparer.Ordinal));
            }
        }
        catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
        {
            // Only /etc/paths, then Homebrew and the .NET folder.
        }

        var vLines = new List<string>();
        foreach (var vFile in vFiles)
        {
            try
            {
                vLines.AddRange(File.ReadAllLines(vFile));
            }
            catch (Exception vException) when (vException is IOException or UnauthorizedAccessException)
            {
                // A file that cannot be read adds nothing.
            }
        }

        return ForMacApp(aExistingPath, vLines, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }
}
