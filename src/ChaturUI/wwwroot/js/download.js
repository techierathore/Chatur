// Chatur — hands the browser a text file to save (FileDownloadInterop.cs). A Blob plus a synthetic
// anchor click is the standard way a Blazor WebAssembly/Server page triggers a "Save As" without a
// server round trip; the object URL is revoked right after the click so it does not leak.
export function downloadText(fileName, content, contentType) {
    const blob = new Blob([content], { type: contentType || 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
}
