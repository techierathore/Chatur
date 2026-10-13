using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// The main window's keyboard shortcuts, via <c>wwwroot/js/shortcuts.js</c>: Ctrl+, (⌘, on a Mac)
/// opens Settings, the shortcut Tools ▸ Settings shows (REQ-UI-073).
/// </summary>
public sealed class ShortcutInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/ChaturUI/js/shortcuts.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>Creates the interop wrapper.</summary>
    /// <param name="aJs">The JS runtime to import <c>shortcuts.js</c> from.</param>
    public ShortcutInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>
    /// Starts listening; each shortcut calls the matching <c>[JSInvokable]</c> method on
    /// <paramref name="aTarget"/> (<c>OnSettingsShortcut</c>).
    /// </summary>
    /// <param name="aTarget">The component the shortcuts call back.</param>
    /// <returns>The listener; dispose it to stop listening.</returns>
    public async Task<IJSObjectReference> ListenAsync<T>(DotNetObjectReference<T> aTarget) where T : class
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<IJSObjectReference>("listen", aTarget).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (objModuleTask.IsValueCreated)
        {
            var vModule = await objModuleTask.Value.ConfigureAwait(false);
            await vModule.DisposeAsync().ConfigureAwait(false);
        }
    }
}
