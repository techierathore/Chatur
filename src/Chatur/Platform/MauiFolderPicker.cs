using Chatur.Core.Platform;
#if WINDOWS
using WinRT.Interop;
using Windows.Storage.Pickers;
#elif MACCATALYST
using UIKit;
using UniformTypeIdentifiers;
#endif

namespace Chatur.Platform;

/// <summary>
/// The machine's own folder-choosing dialog (REQ-UI-005): on Windows,
/// <c>Windows.Storage.Pickers.FolderPicker</c> initialised via
/// <c>WinRT.Interop.InitializeWithWindow</c> against the app's current top-level window; on Mac
/// Catalyst, <c>UIDocumentPickerViewController</c> presented from the current
/// <c>UIViewController</c>. Returns <see langword="null"/> when the owner cancels, or when there is
/// no window/view controller to present from yet — the caller (the Project folders dialog) falls
/// back to letting the owner type the path directly, exactly as it does on
/// <see cref="Chatur.WebHarness"/>, which has no native dialog at all.
/// </summary>
public sealed class MauiFolderPicker : IFolderPicker
{
    /// <inheritdoc />
    public async Task<string?> PickFolderAsync(string aTitle, CancellationToken aCt = default)
    {
#if WINDOWS
        var vNativeWindow = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (vNativeWindow is null)
        {
            return null;
        }

        var vPicker = new FolderPicker();
        InitializeWithWindow.Initialize(vPicker, WindowNative.GetWindowHandle(vNativeWindow));
        vPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        vPicker.FileTypeFilter.Add("*");

        var vFolder = await vPicker.PickSingleFolderAsync();
        return vFolder?.Path;
#elif MACCATALYST
        var vController = Microsoft.Maui.ApplicationModel.Platform.GetCurrentUIViewController();
        if (vController is null)
        {
            return null;
        }

        var vCompletion = new TaskCompletionSource<string?>();
        var vDocumentPicker = new UIDocumentPickerViewController(new[] { UTTypes.Folder });
        vDocumentPicker.DidPickDocumentAtUrls += (_, aArgs) => vCompletion.TrySetResult(aArgs.Urls?.FirstOrDefault()?.Path);
        vDocumentPicker.WasCancelled += (_, _) => vCompletion.TrySetResult(null);

        vController.PresentViewController(vDocumentPicker, animated: true, completionHandler: null);
        return await vCompletion.Task;
#else
        throw new PlatformNotSupportedException("Chatur ships for Windows and Mac Catalyst only.");
#endif
    }

    /// <inheritdoc />
    /// <remarks>
    /// MAUI's own <see cref="FilePicker"/>, which is the operating system's file dialog on Windows and
    /// on Mac Catalyst (BRD-215). It must be shown from the main thread.
    /// </remarks>
    public async Task<string?> PickFileAsync(string aTitle, CancellationToken aCt = default)
    {
        var vResult = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(new PickOptions { PickerTitle = aTitle }));
        return vResult?.FullPath;
    }

    /// <inheritdoc />
    /// <remarks>
    /// MAUI's <see cref="FilePicker"/> with a file type made of the extensions: on Windows the dialog's
    /// filter shows only those files (BRD-221: Open a solution shows .sln and .slnx); on Mac Catalyst any
    /// file can be chosen, since a solution file has no system type there, and the path is checked after.
    /// </remarks>
    public async Task<string?> PickFileAsync(string aTitle, IReadOnlyList<string> aExtensions, CancellationToken aCt = default)
    {
        var vFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.WinUI] = aExtensions,
            [DevicePlatform.MacCatalyst] = new[] { "public.item" }
        });
        var vResult = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(new PickOptions { PickerTitle = aTitle, FileTypes = vFileType }));
        return vResult?.FullPath;
    }
}
