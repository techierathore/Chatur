// Chatur — the Edit menu's commands for the file editor (EditorInterop.cs). The editor is a textarea;
// a menu click has moved focus away from it, so each command puts focus (and the selection it kept)
// back first, then runs the browser's own editing command, which also keeps the editor's undo history.
let objSavedSelection = null;

function editor() {
    return document.querySelector('[data-testid="editor"] textarea');
}

function saveSelection() {
    const area = editor();
    if (!area) {
        return;
    }
    const start = area.selectionStart ?? 0;
    const end = area.selectionEnd ?? start;
    objSavedSelection = { start, end, text: area.value.slice(start, end) };
}

function focusEditor() {
    const area = editor();
    if (area) {
        area.focus({ preventScroll: true });
        if (objSavedSelection) {
            area.setSelectionRange(objSavedSelection.start, objSavedSelection.end);
        }
    }
    return area;
}

// Keep the selection around even after a menu click moves focus away.
document.addEventListener('selectionchange', saveSelection);

export async function run(command) {
    const area = focusEditor();
    if (!area) {
        return false;
    }

    // The browser's execCommand does not always write to the system clipboard in a headless test,
    // so copy and cut also write the selected text explicitly (REQ-UI-073).
    if (command === 'copy' || command === 'cut') {
        const vText = objSavedSelection?.text ?? area.value.slice(area.selectionStart ?? 0, area.selectionEnd ?? 0);
        if (vText) {
            try {
                await navigator.clipboard.writeText(vText);
            } catch {
                // best effort: execCommand below is the real UI behaviour
            }
        }
    }

    return document.execCommand(command);
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
    const area = editor();
    if (!area) {
        return { undo: false, redo: false };
    }
    return { undo: document.queryCommandEnabled('undo'), redo: document.queryCommandEnabled('redo') };
}
