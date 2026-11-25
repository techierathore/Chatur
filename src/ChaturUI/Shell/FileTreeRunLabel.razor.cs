using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>The label of a runnable project's row in Files, with its "Set as the project to run" right-click menu (BRD-228).</summary>
public partial class FileTreeRunLabel
{
    /// <summary>The tree row: the project's folder or its <c>.csproj</c>.</summary>
    [Parameter, EditorRequired]
    public FileTreeNode Node { get; set; } = default!;

    /// <summary>The run target the row stands for.</summary>
    [Parameter, EditorRequired]
    public RunTarget Target { get; set; } = default!;

    /// <summary>The files panel that sets the target and knows which one is chosen.</summary>
    [Parameter]
    public FilesPanel? Panel { get; set; }

    private bool IsChosen => Panel?.IsRunTarget(Target) ?? false;

    private async Task SetAsync()
    {
        if (Panel is not null)
        {
            await Panel.SetRunTargetAsync(Target);
        }
    }
}
