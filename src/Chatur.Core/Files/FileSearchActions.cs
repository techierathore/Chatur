using Chatur.Core.Actions;

namespace Chatur.Core.Files;

/// <summary>
/// <see cref="IFileSearchActions"/> over <see cref="IFileActions"/>: the tree gives the names, and
/// each text file is read once for a content search (REQ-UI-073). Binary files, very large ones and
/// anything past <see cref="MaxFilesRead"/> files are skipped, so a search over a big project ends.
/// </summary>
public sealed class FileSearchActions : IFileSearchActions
{
    /// <summary>The most files one content search reads.</summary>
    public const int MaxFilesRead = 3000;

    private const int MaxFileLength = 1_000_000;

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".webp", ".dll", ".exe", ".pdb", ".zip", ".gz", ".nupkg",
        ".woff", ".woff2", ".ttf", ".otf", ".db", ".sqlite", ".pdf", ".so", ".dylib", ".mp4", ".mp3"
    };

    private readonly IFileActions objFiles;

    /// <summary>Creates the actions.</summary>
    /// <param name="aFiles">Lists and reads the project's files.</param>
    public FileSearchActions(IFileActions aFiles)
    {
        objFiles = aFiles;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSearchHit>> FindFilesAsync(int aProjectId, string aName, int aMax = 50, CancellationToken aCt = default)
    {
        var vName = (aName ?? string.Empty).Trim();
        var vAll = Flatten(await objFiles.TreeAsync(aProjectId, aCt).ConfigureAwait(false)).ToList();

        // A name that starts the file's name first, then one inside it, then one only in its folder path.
        var vMatches = vName.Length == 0
            ? vAll
            : vAll
                .Select(aFile => (File: aFile, Rank: Rank(aFile, vName)))
                .Where(aPair => aPair.Rank < 3)
                .OrderBy(aPair => aPair.Rank)
                .ThenBy(aPair => aPair.File.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Select(aPair => aPair.File)
                .ToList();

        return vMatches.Take(aMax).Select(aFile => new FileSearchHit(aFile.RelativePath, 0, string.Empty)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileSearchHit>> FindInFilesAsync(int aProjectId, string aText, int aMax = 200, CancellationToken aCt = default)
    {
        var vHits = new List<FileSearchHit>();
        if (string.IsNullOrEmpty(aText))
        {
            return vHits;
        }

        var vFiles = Flatten(await objFiles.TreeAsync(aProjectId, aCt).ConfigureAwait(false))
            .Where(aFile => !BinaryExtensions.Contains(Path.GetExtension(aFile.Name)))
            .Take(MaxFilesRead);

        foreach (var vFile in vFiles)
        {
            aCt.ThrowIfCancellationRequested();

            string vContent;
            try
            {
                vContent = await objFiles.ReadAsync(aProjectId, vFile.RelativePath, aCt).ConfigureAwait(false);
            }
            catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (vContent.Length > MaxFileLength || vContent.Contains('\0'))
            {
                continue;
            }

            var vLineNumber = 0;
            foreach (var vLine in vContent.Split('\n'))
            {
                vLineNumber++;
                if (vLine.Contains(aText, StringComparison.OrdinalIgnoreCase))
                {
                    vHits.Add(new FileSearchHit(vFile.RelativePath, vLineNumber, vLine.Trim()));
                    if (vHits.Count >= aMax)
                    {
                        return vHits;
                    }
                }
            }
        }

        return vHits;
    }

    private static int Rank(FileTreeNode aFile, string aName)
    {
        if (aFile.Name.StartsWith(aName, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (aFile.Name.Contains(aName, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return aFile.RelativePath.Contains(aName, StringComparison.OrdinalIgnoreCase) ? 2 : 3;
    }

    private static IEnumerable<FileTreeNode> Flatten(IEnumerable<FileTreeNode> aNodes)
    {
        foreach (var vNode in aNodes)
        {
            if (vNode.IsFolder)
            {
                foreach (var vChild in Flatten(vNode.Children))
                {
                    yield return vChild;
                }
            }
            else
            {
                yield return vNode;
            }
        }
    }
}
