using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>Renders one level of a file tree, recursing into itself for each folder's children.</summary>
public partial class FileTreeBranch
{
    /// <summary>The rows to render at this level.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FileTreeNode> Nodes { get; set; } = [];

    // The folders open at this level, so a folder shows the open-folder icon while open (mockups/main.html `i-folder-open`).
    private readonly HashSet<string> objOpen = [];

    private void SetOpen(string aPath, bool aOpen)
    {
        if (aOpen)
        {
            objOpen.Add(aPath);
        }
        else
        {
            objOpen.Remove(aPath);
        }
    }
}
