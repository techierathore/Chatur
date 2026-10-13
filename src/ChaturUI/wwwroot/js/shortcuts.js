// Chatur — the main window's keyboard shortcuts (ShortcutInterop.cs). Ctrl+, (⌘, on a Mac) opens
// Settings, as Tools ▸ Settings shows. Listened for in the capture phase, so the editor does not eat it.
export function listen(dotNetRef) {
    const onKey = (e) => {
        if (e.key !== ',' || e.altKey || e.shiftKey || !(e.ctrlKey || e.metaKey)) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        dotNetRef.invokeMethodAsync('OnSettingsShortcut');
    };
    window.addEventListener('keydown', onKey, true);
    return { dispose: () => window.removeEventListener('keydown', onKey, true) };
}
