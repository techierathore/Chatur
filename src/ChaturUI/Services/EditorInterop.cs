using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// Runs the Edit menu's commands (undo, redo, cut, copy, paste) on the open file's editor, via
/// <c>wwwroot/js/editor.js</c> (REQ-UI-073). The editor is the library's own text area, which keeps its
/// own undo history; these commands act on that, so the menu does exactly what the keys would.
/// </summary>
public sealed class EditorInterop
{
    private const string ModulePath = "./_content/ChaturUI/js/editor.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>Creates the interop wrapper.</summary>
    /// <param name="aJs">The JS runtime to import <c>editor.js</c> from.</param>
    public EditorInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>Runs <c>undo</c>, <c>redo</c>, <c>cut</c> or <c>copy</c> on the editor.</summary>
    /// <param name="aCommand">The browser editing command's name.</param>
    /// <returns><see langword="true"/> when the editor took the command.</returns>
    public async Task<bool> RunAsync(string aCommand)
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<bool>("run", aCommand).ConfigureAwait(false);
    }

    /// <summary>Pastes the clipboard's text at the editor's cursor.</summary>
    /// <returns><see langword="true"/> when something was pasted.</returns>
    public async Task<bool> PasteAsync()
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<bool>("paste").ConfigureAwait(false);
    }

    /// <summary>Whether the editor has something to undo and something to redo.</summary>
    public async Task<(bool CanUndo, bool CanRedo)> StateAsync()
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        var vState = await vModule.InvokeAsync<EditState>("state").ConfigureAwait(false);
        return (vState.Undo, vState.Redo);
    }

    private sealed record EditState(bool Undo, bool Redo);
}
