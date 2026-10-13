// Chatur — the Edit menu's commands for the file editor (EditorInterop.cs). The editor is a textarea;
// a menu click has moved focus away from it, so each command puts focus (and the selection it kept)
// back first, then runs the browser's own editing command, which also keeps the editor's undo history.
function editor() {
    return document.querySelector('[data-testid="editor"] textarea');
}

function focusEditor() {
    const area = editor();
    if (area) {
        area.focus({ preventScroll: true });
    }
    return area;
}

export function run(command) {
    const area = focusEditor();
    return area ? document.execCommand(command) : false;
}

export async function paste() {
    const area = focusEditor();
    if (!area) {
        return false;
    }
    try {
        const text = await navigator.clipboard.readText();
        return document.execCommand('insertText', false, text);
    } catch {
        return false;
    }
}

export function state() {
    const area = focusEditor();
    if (!area) {
        return { undo: false, redo: false };
    }
    return { undo: document.queryCommandEnabled('undo'), redo: document.queryCommandEnabled('redo') };
}
