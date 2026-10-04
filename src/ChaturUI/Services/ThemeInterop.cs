using Microsoft.JSInterop;

namespace ChaturUI.Services;

/// <summary>
/// Applies a theme to the current document by setting <c>data-theme</c> and toggling the
/// <c>dark</c> class on <c>&lt;html&gt;</c> — the same attributes <c>wwwroot/themes/*.css</c> read
/// (Architecture §1 Q8; the "ThemeService hook" the foundation brief asks for). Settings ▸
/// Appearance (cluster L) calls this after <c>IThemeActions.ChooseAsync</c> saves the choice.
/// </summary>
public sealed class ThemeInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/ChaturUI/js/theme.js";

    private readonly Lazy<Task<IJSObjectReference>> objModuleTask;

    /// <summary>
    /// Creates the interop wrapper.
    /// </summary>
    /// <param name="aJs">The JS runtime to import <c>theme.js</c> from.</param>
    public ThemeInterop(IJSRuntime aJs)
    {
        objModuleTask = new Lazy<Task<IJSObjectReference>>(
            () => aJs.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>
    /// Sets the document's theme and light/dark mode, for one of the four built-in themes whose
    /// full rule set is already loaded as a static stylesheet.
    /// </summary>
    /// <param name="aThemeName">The theme's name, matching a file under <c>wwwroot/themes</c>.</param>
    /// <param name="aIsDark">Whether to show the dark set.</param>
    public async Task SetThemeAsync(string aThemeName, bool aIsDark)
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        await vModule.InvokeVoidAsync("setTheme", aThemeName, aIsDark).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies a theme added from a file (REQ-UI-038) by mapping its named colour values onto the
    /// real TrBlazeUI variables at runtime — there is no static stylesheet for it, so "no new build
    /// of Chatur" still holds.
    /// </summary>
    /// <param name="aThemeName">The theme's name.</param>
    /// <param name="aLightValues">The theme's light set of named colour values.</param>
    /// <param name="aDarkValues">The theme's dark set of named colour values.</param>
    /// <param name="aIsDark">Whether to show the dark set.</param>
    public async Task ApplyCustomThemeAsync(
        string aThemeName,
        IReadOnlyDictionary<string, string> aLightValues,
        IReadOnlyDictionary<string, string> aDarkValues,
        bool aIsDark)
    {
        var vModule = await objModuleTask.Value.ConfigureAwait(false);
        await vModule.InvokeVoidAsync("applyCustomTheme", aThemeName, aLightValues, aDarkValues, aIsDark).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!objModuleTask.IsValueCreated)
        {
            return;
        }

        try
        {
            var vModule = await objModuleTask.Value.ConfigureAwait(false);
            await vModule.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone — nothing left to dispose on the JS side.
        }
        catch (InvalidOperationException)
        {
            // Disposed while the page was still being statically prerendered, before any JS interop
            // call is allowed — there is nothing on the JS side yet either.
        }
    }
}
