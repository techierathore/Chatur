using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.CodeEditor;

namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's open-file tabs and editor: opening a file shows its text in a new tab named after
/// it (REQ-UI-040), and a tab with unsaved changes is marked and asks before it is closed
/// (REQ-UI-041).
/// </summary>
/// <remarks>Owning cluster: H (REQ-UI-040, REQ-UI-041); G (REQ-FN-044 through REQ-FN-046).</remarks>
public partial class EditorArea : IDisposable
{
    private string? objPendingCloseId;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private IFileActions Files { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Workbench.Changed += OnWorkbenchChanged;
    }

    private IReadOnlyList<EditorTabItem> TabItems =>
        Workbench.OpenFiles.Select(f => new EditorTabItem(f.Id, f.Name, f.IsDirty))
            .Concat(Workbench.ToolTabs.Select(t => new EditorTabItem(t.Id, t.Title)))
            .ToList();

    /// <summary>
    /// Gives each tab the id the mockups draw: <c>tab-processes</c>, <c>tab-board</c>, <c>tab-process-run</c>,
    /// <c>tab-agent-workspace</c>, and for a file <c>tab-</c> plus its name without the extension, in lower case
    /// (<c>Query.cs</c> is <c>tab-query</c>).
    /// </summary>
    private IReadOnlyDictionary<string, object>? TabAttributesOf(EditorTabItem aItem)
    {
        var vTool = Workbench.ToolTabs.FirstOrDefault(t => t.Id == aItem.Id);
        var vSuffix = vTool is null
            ? System.IO.Path.GetFileNameWithoutExtension(aItem.Label).ToLowerInvariant()
            : vTool.Kind switch
            {
                ToolKind.Processes => "processes",
                ToolKind.Board => "board",
                ToolKind.ProcessRun => "process-run",
                ToolKind.AutoRun => "auto-run",
                ToolKind.RunQueue => "run-queue",
                ToolKind.RunReport => "run-report",
                _ => "agent-workspace"
            };
        return new Dictionary<string, object> { ["data-testid"] = $"tab-{vSuffix}" };
    }

    private void OnActiveIdChanged(string? aId)
    {
        if (aId is not null)
        {
            Workbench.Activate(aId);
        }
    }

    private void OnContentChanged(string aValue)
    {
        if (Workbench.ActiveFile is { } vFile)
        {
            Workbench.UpdateContent(vFile.Id, aValue);
        }
    }

    /// <summary>
    /// Saves the active tab's current text to disk (REQ-FN-044), then clears its unsaved mark.
    /// </summary>
    private async Task SaveActiveAsync()
    {
        if (Workbench.ActiveFile is not { } vFile || AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        await Files.SaveAsync(vProject.ProjectId, vFile.RelativePath, vFile.CurrentContent);
        Workbench.MarkSaved(vFile.Id);
    }

    /// <summary>
    /// A tab's close button was pressed (REQ-UI-041): a clean tab closes at once, an unsaved one
    /// waits for the confirm dialog.
    /// </summary>
    private void RequestClose(EditorTabItem aItem)
    {
        var vFile = Workbench.OpenFiles.FirstOrDefault(f => f.Id == aItem.Id);
        if (vFile is null)
        {
            // A screen's tab has nothing unsaved; a run carries on without its tab.
            Workbench.Close(aItem.Id);
            return;
        }

        if (vFile.IsDirty)
        {
            // EditorTabs' own close button lives inside a child component; setting this field alone
            // is not guaranteed to repaint EditorArea, so the dialog is asked for explicitly rather
            // than relying on an automatic re-render that a plain field write does not raise.
            objPendingCloseId = vFile.Id;
            StateHasChanged();
        }
        else
        {
            Workbench.Close(vFile.Id);
        }
    }

    private void ConfirmDiscardAndClose()
    {
        if (objPendingCloseId is { } vId)
        {
            Workbench.Close(vId);
        }

        objPendingCloseId = null;
        StateHasChanged();
    }

    private void CancelClose()
    {
        objPendingCloseId = null;
        StateHasChanged();
    }

    /// <summary>
    /// Binding <c>OpenChanged</c> as well as <c>Open</c> is what makes AlertDialog treat this as fully
    /// controlled rather than falling back to its own uncontrolled state after the first render — an
    /// <c>Open</c> with no matching <c>OpenChanged</c> was never actually reopening the dialog. Also
    /// catches Escape and an outside click, both of which close the dialog without going through
    /// <see cref="CancelClose"/> otherwise.
    /// </summary>
    private void OnCloseDialogOpenChanged(bool aOpen)
    {
        if (!aOpen)
        {
            CancelClose();
        }
    }

    /// <summary>Opens the active tab's file in the owner's chosen editor (REQ-FN-045).</summary>
    private async Task OpenInEditorAsync()
    {
        if (Workbench.ActiveFile is not { } vFile || AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        await Files.OpenInEditorAsync(vProject.ProjectId, vFile.RelativePath);
    }

    /// <summary>Opens the active tab's file with the machine's own default application (REQ-FN-046).</summary>
    private async Task OpenWithDefaultAppAsync()
    {
        if (Workbench.ActiveFile is not { } vFile || AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        await Files.OpenWithDefaultAppAsync(vProject.ProjectId, vFile.RelativePath);
    }

    private void OnWorkbenchChanged() => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose() => Workbench.Changed -= OnWorkbenchChanged;
}
