using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>Renders one level of a file tree, recursing into itself for each folder's children.</summary>
public partial class FileTreeBranch
{
    /// <summary>The rows to render at this level.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<FileTreeNode> Nodes { get; set; } = [];

    /// <summary>The files panel that reads each folder as it opens and knows the run targets (BRD-217, BRD-228).</summary>
    [CascadingParameter]
    public FilesPanel? Panel { get; set; }

    // The folders open at this level, so a folder shows the open-folder icon while open (mockups/main.html `i-folder-open`).
    private readonly HashSet<string> objOpen = [];

    /// <summary>A runnable project's name, which opens "Set as the project to run" on a right-click (BRD-228).</summary>
    private RenderFragment RunLabel(FileTreeNode aNode, RunTarget aTarget) => aBuilder =>
    {
        aBuilder.OpenComponent<FileTreeRunLabel>(0);
        aBuilder.AddComponentParameter(1, nameof(FileTreeRunLabel.Node), aNode);
        aBuilder.AddComponentParameter(2, nameof(FileTreeRunLabel.Target), aTarget);
        aBuilder.AddComponentParameter(3, nameof(FileTreeRunLabel.Panel), Panel);
        aBuilder.CloseComponent();
    };

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
