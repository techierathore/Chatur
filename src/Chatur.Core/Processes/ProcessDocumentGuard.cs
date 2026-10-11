namespace Chatur.Core.Processes;

/// <summary>
/// Decides whether a process may write a path in a project: only the project's own BRD, checklist and
/// status documents, and nothing else in the folder (REQ-FN-051). A class of its own so it is tested
/// without a database.
/// </summary>
public static class ProcessDocumentGuard
{
    /// <summary>The status document's project-relative path.</summary>
    public const string StatusPath = "PROJECT-STATUS.md";

    /// <summary>
    /// Resolves a project-relative path to the full path it may be written at.
    /// </summary>
    /// <param name="aProjectRoot">The project's folder.</param>
    /// <param name="aRelativePath">The path a process asks to write.</param>
    /// <param name="aFullPath">The full path to write when allowed.</param>
    /// <returns><see langword="true"/> when the path is the status document, or a <c>.md</c> BRD or checklist directly under <c>docs/</c>.</returns>
    public static bool TryResolve(string aProjectRoot, string aRelativePath, out string aFullPath)
    {
        aFullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(aRelativePath) || Path.IsPathRooted(aRelativePath))
        {
            return false;
        }

        var vNormalised = aRelativePath.Replace('\\', '/');
        if (vNormalised.Split('/').Any(aPart => aPart is "." or ".." or ""))
        {
            return false;
        }

        if (!IsDocumentName(vNormalised))
        {
            return false;
        }

        var vRoot = Path.GetFullPath(aProjectRoot).TrimEnd(Path.DirectorySeparatorChar);
        var vFull = Path.GetFullPath(Path.Combine(vRoot, vNormalised));
        if (!vFull.StartsWith(vRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        aFullPath = vFull;
        return true;
    }

    /// <summary>Whether a project-relative, forward-slash path names a BRD, checklist or status document.</summary>
    /// <param name="aNormalisedPath">The path with forward slashes.</param>
    public static bool IsDocumentName(string aNormalisedPath)
    {
        if (aNormalisedPath == StatusPath)
        {
            return true;
        }

        var vParts = aNormalisedPath.Split('/');
        return vParts.Length == 2
            && vParts[0] == "docs"
            && (vParts[1].EndsWith("-BRD.md", StringComparison.Ordinal)
                || vParts[1].EndsWith("-Checklist.md", StringComparison.Ordinal));
    }
}
