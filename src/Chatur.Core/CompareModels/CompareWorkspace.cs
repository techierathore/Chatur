using System.Security.Cryptography;
using System.Text;
using Chatur.Core.Actions;

namespace Chatur.Core.CompareModels;

/// <summary>
/// The files one compared model may touch, bound to one root folder (REQ-FN-328, REQ-FN-329): the open project for a
/// question, the model's own copy for a task that changes files. Every path a model gives is resolved under that root
/// and refused when it leaves it, so a model can never reach the real project from its copy, nor anything outside the
/// project from a question. Also copies a project, takes a snapshot of it, and lists what a copy changed.
/// </summary>
public sealed class CompareWorkspace
{
    /// <summary>The folders never copied, listed or compared: build output, packages and source control.</summary>
    public static readonly IReadOnlySet<string> SkippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules", ".vs"
    };

    /// <summary>The largest file text handed back to a model, in characters; the rest is cut with a note.</summary>
    private const int MaxReadChars = 200_000;

    /// <summary>The most lines a folder listing hands back.</summary>
    private const int MaxListLines = 300;

    private readonly string objRoot;

    /// <summary>Binds the tools to one folder.</summary>
    /// <param name="aRoot">The folder every path is resolved under.</param>
    public CompareWorkspace(string aRoot)
    {
        objRoot = Path.GetFullPath(aRoot);
    }

    /// <summary>The folder every path is resolved under.</summary>
    public string Root => objRoot;

    /// <summary>
    /// The folder a project's files live in: the project's path itself, or the folder of a solution file when the project
    /// was opened by one (the same rule the agent's build uses).
    /// </summary>
    /// <param name="aProjectPath">The <c>Project.Path</c> value.</param>
    public static string ProjectRoot(string aProjectPath) =>
        File.Exists(aProjectPath) ? (Path.GetDirectoryName(aProjectPath) ?? aProjectPath) : aProjectPath;

    /// <summary>A model name made safe for a folder name and a test id: lower case, letters and digits, dashes between.</summary>
    /// <param name="aName">The model's name, e.g. <c>openai/gpt-4.1</c>.</param>
    public static string SafeName(string aName)
    {
        var vBuilder = new StringBuilder(aName.Length);
        foreach (var vChar in aName.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(vChar))
            {
                vBuilder.Append(vChar);
            }
            else if (vBuilder.Length > 0 && vBuilder[^1] != '-')
            {
                vBuilder.Append('-');
            }
        }

        var vSafe = vBuilder.ToString().Trim('-');
        return vSafe.Length == 0 ? "model" : vSafe;
    }

    /// <summary>
    /// Resolves a path a model gave under the root, or <see langword="null"/> when it is absolute or leaves the root.
    /// </summary>
    /// <param name="aRelativePath">The path, relative to the root; <c>.</c> or empty is the root itself.</param>
    public string? Resolve(string aRelativePath)
    {
        var vPath = (aRelativePath ?? string.Empty).Trim().Replace('\\', '/');
        if (vPath.Length == 0 || vPath is "." or "./" or "/")
        {
            return objRoot;
        }

        if (Path.IsPathRooted(vPath) || vPath.StartsWith('/') || vPath.Contains(':', StringComparison.Ordinal))
        {
            return null;
        }

        var vFull = Path.GetFullPath(Path.Combine(objRoot, vPath.Replace('/', Path.DirectorySeparatorChar)));
        var vRootWithSeparator = objRoot.EndsWith(Path.DirectorySeparatorChar) ? objRoot : objRoot + Path.DirectorySeparatorChar;
        var vComparison = Chatur.Core.Platform.HostPlatform.HasCaseInsensitiveNames ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(vFull, objRoot, vComparison) || vFull.StartsWith(vRootWithSeparator, vComparison) ? vFull : null;
    }

    /// <summary>
    /// The read tool: a file's text, or for a folder what is in it (three levels deep, build output left out). Never
    /// throws; a path it cannot read comes back as a sentence the model reads.
    /// </summary>
    /// <param name="aRelativePath">The path, relative to the root.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    public async Task<string> ReadAsync(string aRelativePath, CancellationToken aCt)
    {
        var vFull = Resolve(aRelativePath);
        if (vFull is null)
        {
            return $"\"{aRelativePath}\" is outside the project; only paths inside it can be read.";
        }

        try
        {
            if (Directory.Exists(vFull))
            {
                return List(vFull);
            }

            if (!File.Exists(vFull))
            {
                return $"\"{aRelativePath}\" is not a file or folder of the project.";
            }

            var vText = await File.ReadAllTextAsync(vFull, aCt).ConfigureAwait(false);
            return vText.Length > MaxReadChars
                ? vText[..MaxReadChars] + $"\n… (cut at {MaxReadChars:N0} characters of {vText.Length:N0})"
                : vText;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception vException)
        {
            return $"Could not read \"{aRelativePath}\": {vException.Message}";
        }
    }

    /// <summary>
    /// The write tool, offered only for a task that changes files and only on a model's own copy: writes a file's complete
    /// new text under the root. Never throws; a refusal comes back as a sentence the model reads.
    /// </summary>
    /// <param name="aRelativePath">The path, relative to the root.</param>
    /// <param name="aContent">The file's complete new text.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    public async Task<string> WriteAsync(string aRelativePath, string aContent, CancellationToken aCt)
    {
        var vFull = Resolve(aRelativePath);
        if (vFull is null)
        {
            return $"\"{aRelativePath}\" is outside the project; nothing was written.";
        }

        if (string.Equals(vFull, objRoot, StringComparison.Ordinal) || Directory.Exists(vFull))
        {
            return $"\"{aRelativePath}\" is a folder, not a file; nothing was written.";
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(vFull)!);
            await File.WriteAllTextAsync(vFull, aContent, aCt).ConfigureAwait(false);
            return $"Wrote \"{aRelativePath}\".";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception vException)
        {
            return $"Could not write \"{aRelativePath}\": {vException.Message}";
        }
    }

    /// <summary>Copies a project folder, leaving out <see cref="SkippedFolders"/> at every level.</summary>
    /// <param name="aSource">The project folder.</param>
    /// <param name="aDestination">The copy's folder; made when it is not there.</param>
    public static void Copy(string aSource, string aDestination)
    {
        Directory.CreateDirectory(aDestination);
        foreach (var vFile in Directory.EnumerateFiles(aSource))
        {
            File.Copy(vFile, Path.Combine(aDestination, Path.GetFileName(vFile)), true);
        }

        foreach (var vFolder in Directory.EnumerateDirectories(aSource))
        {
            var vName = Path.GetFileName(vFolder);
            if (SkippedFolders.Contains(vName))
            {
                continue;
            }

            Copy(vFolder, Path.Combine(aDestination, vName));
        }
    }

    /// <summary>
    /// Every file under a folder with a hash of its bytes, keyed by its path relative to the folder with forward slashes,
    /// <see cref="SkippedFolders"/> left out; <paramref name="aSkipMetrics"/> also leaves out <c>docs/metrics</c>, which
    /// Chatur itself appends to while models run.
    /// </summary>
    /// <param name="aRoot">The folder.</param>
    /// <param name="aSkipMetrics">Whether to leave out <c>docs/metrics</c>.</param>
    public static IReadOnlyDictionary<string, string> Snapshot(string aRoot, bool aSkipMetrics)
    {
        var vResult = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var vRelative in Files(aRoot))
        {
            if (aSkipMetrics && vRelative.StartsWith("docs/metrics/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using var vStream = File.OpenRead(Path.Combine(aRoot, vRelative));
            vResult[vRelative] = Convert.ToHexString(SHA256.HashData(vStream));
        }

        return vResult;
    }

    /// <summary>Whether two snapshots hold the same files with the same bytes.</summary>
    /// <param name="aBefore">The first snapshot.</param>
    /// <param name="aAfter">The second snapshot.</param>
    public static bool Same(IReadOnlyDictionary<string, string> aBefore, IReadOnlyDictionary<string, string> aAfter) =>
        aBefore.Count == aAfter.Count
        && aBefore.All(aPair => aAfter.TryGetValue(aPair.Key, out var vHash) && vHash == aPair.Value);

    /// <summary>
    /// What a copy changed against the project (REQ-FN-329): each file added, modified or deleted, with the lines added and
    /// removed, in path order. Lines are compared as a multiset, so a moved line is neither added nor removed.
    /// </summary>
    /// <param name="aProjectRoot">The project folder.</param>
    /// <param name="aCopyRoot">The model's copy.</param>
    public static IReadOnlyList<CompareChangedFile> Changes(string aProjectRoot, string aCopyRoot)
    {
        // docs/metrics is Chatur's own: the comparison appends each model's run record to the project's files while the
        // other models are still working, so it differs from every copy without any model having touched it.
        static bool Kept(string aPath) => !aPath.StartsWith("docs/metrics/", StringComparison.OrdinalIgnoreCase);
        var vProjectFiles = Files(aProjectRoot).Where(Kept).ToHashSet(StringComparer.Ordinal);
        var vCopyFiles = Files(aCopyRoot).Where(Kept).ToHashSet(StringComparer.Ordinal);
        var vResult = new List<CompareChangedFile>();

        foreach (var vPath in vProjectFiles.Union(vCopyFiles).OrderBy(aPath => aPath, StringComparer.Ordinal))
        {
            var vInProject = vProjectFiles.Contains(vPath);
            var vInCopy = vCopyFiles.Contains(vPath);
            var vBefore = vInProject ? File.ReadAllBytes(Path.Combine(aProjectRoot, vPath)) : [];
            var vAfter = vInCopy ? File.ReadAllBytes(Path.Combine(aCopyRoot, vPath)) : [];
            if (vInProject && vInCopy && vBefore.AsSpan().SequenceEqual(vAfter))
            {
                continue;
            }

            var (vAdded, vRemoved) = CountLines(Lines(vBefore), Lines(vAfter));
            var vChange = !vInProject ? "A" : !vInCopy ? "D" : "M";
            vResult.Add(new CompareChangedFile(vPath, vChange, vAdded, vRemoved));
        }

        return vResult;
    }

    /// <summary>Every file under a folder, relative with forward slashes, <see cref="SkippedFolders"/> left out at every level.</summary>
    private static IEnumerable<string> Files(string aRoot)
    {
        var vPending = new Stack<string>();
        vPending.Push(aRoot);
        while (vPending.Count > 0)
        {
            var vFolder = vPending.Pop();
            foreach (var vFile in Directory.EnumerateFiles(vFolder))
            {
                yield return Path.GetRelativePath(aRoot, vFile).Replace('\\', '/');
            }

            foreach (var vChild in Directory.EnumerateDirectories(vFolder))
            {
                if (!SkippedFolders.Contains(Path.GetFileName(vChild)))
                {
                    vPending.Push(vChild);
                }
            }
        }
    }

    private static List<string> Lines(byte[] aBytes) =>
        aBytes.Length == 0
            ? []
            : Encoding.UTF8.GetString(aBytes).Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n').Split('\n').ToList();

    private static (int Added, int Removed) CountLines(List<string> aBefore, List<string> aAfter)
    {
        var vLeft = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var vLine in aBefore)
        {
            vLeft[vLine] = vLeft.GetValueOrDefault(vLine) + 1;
        }

        var vAdded = 0;
        foreach (var vLine in aAfter)
        {
            if (vLeft.TryGetValue(vLine, out var vCount) && vCount > 0)
            {
                vLeft[vLine] = vCount - 1;
            }
            else
            {
                vAdded++;
            }
        }

        return (vAdded, vLeft.Values.Sum());
    }

    private string List(string aFolder)
    {
        var vLines = new List<string>();
        AddLevel(aFolder, 0, vLines);
        var vName = Path.GetRelativePath(objRoot, aFolder).Replace('\\', '/');
        return vLines.Count == 0
            ? $"Nothing is in \"{vName}\"."
            : $"Contents of \"{vName}\" (folders end with /):\n" + string.Join('\n', vLines);
    }

    private void AddLevel(string aFolder, int aDepth, List<string> aLines)
    {
        foreach (var vEntry in Directory.EnumerateFileSystemEntries(aFolder).OrderBy(aEntry => aEntry, StringComparer.OrdinalIgnoreCase))
        {
            if (aLines.Count >= MaxListLines)
            {
                return;
            }

            var vIsFolder = Directory.Exists(vEntry);
            if (vIsFolder && SkippedFolders.Contains(Path.GetFileName(vEntry)))
            {
                continue;
            }

            aLines.Add(new string(' ', aDepth * 2) + Path.GetRelativePath(objRoot, vEntry).Replace('\\', '/') + (vIsFolder ? "/" : string.Empty));
            if (vIsFolder && aDepth < 2)
            {
                AddLevel(vEntry, aDepth + 1, aLines);
            }
        }
    }
}
