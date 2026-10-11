using Chatur.Core.Actions;

namespace Chatur.Core.Board;

/// <summary>
/// <see cref="IRequirementBoardActions"/> over the selected project's <c>docs/*Checklist*.md</c>
/// files. The files are the only store: nothing is cached, and a write changes only the one row.
/// </summary>
public sealed class RequirementBoardActions : IRequirementBoardActions
{
    private static readonly System.Text.UTF8Encoding Utf8WithBom = new(true);
    private static readonly System.Text.UTF8Encoding Utf8WithoutBom = new(false);

    private readonly IProjectActions objProjects;

    /// <summary>Creates the actions.</summary>
    /// <param name="aProjects">Says which project is selected.</param>
    public RequirementBoardActions(IProjectActions aProjects)
    {
        objProjects = aProjects;
    }

    /// <inheritdoc />
    public IReadOnlyList<BoardAgent> Agents => RequirementAgents.All;

    /// <inheritdoc />
    public async Task<RequirementBoard> LoadAsync(RequirementFilter aFilter, CancellationToken aCt = default)
    {
        var vProject = await objProjects.SelectedProjectAsync(aCt).ConfigureAwait(false);
        var vFolder = vProject is null ? null : FolderOf(vProject.Path);
        if (vProject is null || vFolder is null)
        {
            return new RequirementBoard(false, vProject?.Name ?? string.Empty, [], [], [], new BoardCounts(0, 0, 0, 0, 0));
        }

        var vFiles = ChecklistFiles(vFolder);
        var vAll = new List<RequirementItem>();
        foreach (var vFile in vFiles)
        {
            var vText = await File.ReadAllTextAsync(Path.Combine(vFolder, vFile), aCt).ConfigureAwait(false);
            vAll.AddRange(ChecklistDocument.Parse(vText, vFile, ChecklistDocument.PhaseOf(vFile)));
        }

        return new RequirementBoard(
            true,
            vProject.Name,
            vFiles,
            vAll.Select(aItem => aItem.Phase).Distinct().Order().ToList(),
            aFilter.Apply(vAll),
            Count(vAll));
    }

    /// <inheritdoc />
    public async Task SetStatusAsync(string aChecklistFile, string aRequirementId, string aBoardStatus, CancellationToken aCt = default)
    {
        _ = ChecklistDocument.ToFileStatus(aBoardStatus);
        var vProject = await objProjects.SelectedProjectAsync(aCt).ConfigureAwait(false);
        var vFolder = vProject is null ? null : FolderOf(vProject.Path);
        if (vFolder is null)
        {
            throw new InvalidOperationException("No project is selected.");
        }

        var vFile = ChecklistFiles(vFolder).FirstOrDefault(aName => string.Equals(aName, aChecklistFile, StringComparison.Ordinal))
            ?? throw new ArgumentException($"'{aChecklistFile}' is not a checklist file of the selected project.", nameof(aChecklistFile));
        var vPath = Path.Combine(vFolder, vFile);
        var vHasBom = HasBom(vPath);
        var vText = await File.ReadAllTextAsync(vPath, aCt).ConfigureAwait(false);
        var vChanged = ChecklistDocument.WithStatus(vText, aRequirementId, aBoardStatus);
        await File.WriteAllTextAsync(vPath, vChanged, vHasBom ? Utf8WithBom : Utf8WithoutBom, aCt).ConfigureAwait(false);
    }

    private static string? FolderOf(string aProjectPath)
    {
        var vFolder = Directory.Exists(aProjectPath) ? aProjectPath : Path.GetDirectoryName(aProjectPath);
        return vFolder is not null && Directory.Exists(vFolder) ? vFolder : null;
    }

    private static List<string> ChecklistFiles(string aFolder)
    {
        var vDocs = Path.Combine(aFolder, "docs");
        if (!Directory.Exists(vDocs))
        {
            return [];
        }

        return Directory.EnumerateFiles(vDocs, "*Checklist*.md")
            .Select(aPath => "docs/" + Path.GetFileName(aPath))
            .OrderBy(aName => ChecklistDocument.PhaseOf(aName))
            .ThenBy(aName => aName, StringComparer.Ordinal)
            .ToList();
    }

    private static BoardCounts Count(List<RequirementItem> aAll) => new(
        aAll.Count,
        aAll.Count(aItem => aItem.BoardStatus == BoardStatuses.Planned),
        aAll.Count(aItem => aItem.BoardStatus == BoardStatuses.InProgress),
        aAll.Count(aItem => aItem.BoardStatus == BoardStatuses.Verified),
        aAll.Count(aItem => aItem.BoardStatus == BoardStatuses.Blocked));

    private static bool HasBom(string aPath)
    {
        using var vStream = File.OpenRead(aPath);
        var vHead = new byte[3];
        return vStream.Read(vHead, 0, 3) == 3 && vHead[0] == 0xEF && vHead[1] == 0xBB && vHead[2] == 0xBF;
    }
}
