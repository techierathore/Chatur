using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// Hands the browser a text file to save, via a <c>Blob</c> and a synthetic anchor click
/// (<c>wwwroot/js/download.js</c>) — the missing half of Settings ▸ Agents' Export button
/// (REQ-FN-028's own doc comment says "written to a file", but nothing downloaded one until this
/// existed), and what makes REQ-FN-029's export-then-import round trip real rather than only
/// exercising <c>IRoleActions.ImportAsync</c> against a string built by the test itself.
/// </summary>
public sealed class FileDownloadInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/ChaturUI/js/download.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>Creates the interop wrapper.</summary>
    /// <param name="aJs">The JS runtime to import <c>download.js</c> from.</param>
    public FileDownloadInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>Downloads <paramref name="aContent"/> as a file named <paramref name="aFileName"/>.</summary>
    /// <param name="aFileName">The file name the browser offers to save as.</param>
    /// <param name="aContent">The file's whole text content.</param>
    /// <param name="aContentType">The file's MIME type.</param>
    public async Task DownloadTextAsync(string aFileName, string aContent, string aContentType = "application/json")
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        await vModule.InvokeVoidAsync("downloadText", aFileName, aContent, aContentType).ConfigureAwait(false);
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
