using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// The chat box's keys, via <c>wwwroot/js/composer.js</c>: Enter sends and Shift+Enter starts a new line
/// (REQ-FN-025), and <c>@</c> opens the project's files to attach one (REQ-UI-079).
/// </summary>
public sealed class ComposerInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/ChaturUI/js/composer.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>Creates the interop wrapper.</summary>
    /// <param name="aJs">The JS runtime to import <c>composer.js</c> from.</param>
    public ComposerInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>
    /// Starts listening on the text box inside <paramref name="aHost"/>; the keys call the
    /// <c>[JSInvokable]</c> methods <c>OnEnterToSend</c>, <c>OnMentionQuery</c> and <c>OnMentionKey</c>
    /// on <paramref name="aTarget"/>.
    /// </summary>
    /// <param name="aHost">The element that holds the chat's text box.</param>
    /// <param name="aTarget">The component the keys call back.</param>
    /// <returns>The listener: <c>setMentionOpen</c>, <c>insertMention</c>, <c>clear</c>, and <c>dispose</c> to stop.</returns>
    public async Task<IJSObjectReference> AttachAsync<T>(ElementReference aHost, DotNetObjectReference<T> aTarget) where T : class
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        return await vModule.InvokeAsync<IJSObjectReference>("attach", aHost, aTarget).ConfigureAwait(false);
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
