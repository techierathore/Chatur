namespace Chatur.Core.Platform;

/// <summary>
/// Platform port over the machine's own folder-choosing dialog (Architecture §2, "the file
/// dialogs"). The web harness fakes this with a text field; the MAUI head opens the real
/// operating-system dialog.
/// </summary>
public interface IFolderPicker
{
    /// <summary>
    /// Opens the machine's folder picker and waits for the owner to choose or cancel.
    /// </summary>
    /// <param name="aTitle">The dialog's title.</param>
    /// <param name="aCt">A token that cancels the wait.</param>
    /// <returns>The chosen folder's absolute path, or <see langword="null"/> when the owner cancelled.</returns>
    Task<string?> PickFolderAsync(string aTitle, CancellationToken aCt = default);
}
