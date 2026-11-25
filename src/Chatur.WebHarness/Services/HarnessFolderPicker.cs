using Chatur.Core.Platform;
using Microsoft.Extensions.Logging;

namespace Chatur.WebHarness.Services;

/// <summary>
/// A browser has no folder-picker dialog to open, so the harness logs the request and returns
/// nothing chosen — a screen driving this is expected to fall back to a typed path field instead
/// (Foundation brief "folder picker = a path read from a text field").
/// </summary>
public sealed class HarnessFolderPicker : IFolderPicker
{
    private readonly ILogger<HarnessFolderPicker> objLogger;

    /// <summary>
    /// Creates the picker.
    /// </summary>
    /// <param name="aLogger">Where the request is logged.</param>
    public HarnessFolderPicker(ILogger<HarnessFolderPicker> aLogger)
    {
        objLogger = aLogger;
    }

    /// <inheritdoc />
    public Task<string?> PickFolderAsync(string aTitle, CancellationToken aCt = default)
    {
        objLogger.LogInformation("Folder picker requested ({Title}); the harness has no dialog to open — use a typed path field instead.", aTitle);
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The harness has no file dialog either. It logs what the dialog would have shown, so a test can see that
    /// Open a solution asks for a file picker showing .sln and .slnx files, not a folder picker (BRD-221).
    /// </remarks>
    public Task<string?> PickFileAsync(string aTitle, IReadOnlyList<string> aExtensions, CancellationToken aCt = default)
    {
        objLogger.LogInformation("File picker requested ({Title}) for {Extensions}; the harness has no dialog to open — use a typed path field instead.", aTitle, string.Join(", ", aExtensions));
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<string?> PickFileAsync(string aTitle, CancellationToken aCt = default)
    {
        objLogger.LogInformation("File picker requested ({Title}) for any file; the harness has no dialog to open — use a typed path field instead.", aTitle);
        return Task.FromResult<string?>(null);
    }

}
