// Chatur — the two browser-side helpers the Add a provider dialog's sign-in panel needs
// (BrowserInterop.cs): copy text to the clipboard, and open a page in a new browser window.
export async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch {
        const area = document.createElement('textarea');
        area.value = text;
        document.body.appendChild(area);
        area.select();
        const ok = document.execCommand('copy');
        area.remove();
        return ok;
    }
}

export function openPage(url) {
    return window.open(url, '_blank', 'noopener') !== null;
}
