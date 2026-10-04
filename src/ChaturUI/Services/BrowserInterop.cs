using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// Copies text to the clipboard and opens a web page, via <c>wwwroot/js/browser.js</c>, for the
/// Add a provider dialog's "Sign in with the browser" panel (REQ-FN-016). In the MAUI head the
/// BlazorWebView hands an external address to the operating system's browser; in the web harness
/// it is <c>window.open</c>. Chatur.Core has no seam for opening an address, and Core is not a
/// view's to change, so this lives beside the other interop wrappers.
/// </summary>
public sealed class BrowserInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/ChaturUI/js/browser.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>Creates the interop wrapper.</summary>
    /// <param name="aJs">The JS runtime to import <c>browser.js</c> from.</param>
    public BrowserInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>Puts <paramref name="aText"/> on the clipboard.</summary>
    /// <param name="aText">The text to copy.</param>
    /// <returns><see langword="true"/> when the copy went through.</returns>
    public async Task<bool> CopyTextAsync(string aText)
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<bool>("copyText", aText).ConfigureAwait(false);
    }

    /// <summary>Opens <paramref name="aUrl"/> in the browser.</summary>
    /// <param name="aUrl">The absolute web address to open.</param>
    /// <returns><see langword="false"/> when the browser refused to open a window on its own.</returns>
    public async Task<bool> OpenPageAsync(string aUrl)
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<bool>("openPage", aUrl).ConfigureAwait(false);
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
