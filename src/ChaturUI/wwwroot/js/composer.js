// Chatur — the chat box's keys (ComposerInterop.cs), as in Cursor or Claude Code: Enter sends, Shift+Enter
// starts a new line, and @ opens the project's files to attach one. While that list is open, the arrows,
// Enter, Tab and Escape drive the list instead of the box.
const mentionPattern = /(^|\s)@([^\s@]*)$/;

export function attach(host, dotNetRef) {
    const box = host.querySelector('textarea');
    if (!box) {
        return { dispose: () => {}, setMentionOpen: () => {}, insertMention: () => {}, clear: () => {} };
    }

    let mentionOpen = false;

    const query = () => {
        const match = mentionPattern.exec(box.value.slice(0, box.selectionStart));
        return match ? match[2] : null;
    };

    const onKey = (e) => {
        if (e.isComposing) {
            return;
        }
        if (mentionOpen && ['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape'].includes(e.key)) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnMentionKey', e.key);
            return;
        }
        if (e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.altKey && !e.metaKey) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnEnterToSend', box.value);
        }
    };
    const onInput = () => dotNetRef.invokeMethodAsync('OnMentionQuery', query());

    box.addEventListener('keydown', onKey);
    box.addEventListener('input', onInput);
    box.addEventListener('click', onInput);

    return {
        dispose: () => {
            box.removeEventListener('keydown', onKey);
            box.removeEventListener('input', onInput);
            box.removeEventListener('click', onInput);
        },
        setMentionOpen: (open) => { mentionOpen = open; },
        // Replaces the @word before the caret with the file's path, and tells Blazor the box changed.
        insertMention: (path) => {
            const before = box.value.slice(0, box.selectionStart);
            const match = mentionPattern.exec(before);
            const start = match ? before.length - match[2].length - 1 : before.length;
            const after = box.value.slice(box.selectionStart);
            const inserted = `@${path} `;
            box.value = box.value.slice(0, start) + inserted + after;
            const caret = start + inserted.length;
            box.setSelectionRange(caret, caret);
            box.dispatchEvent(new Event('input', { bubbles: true }));
            box.dispatchEvent(new Event('change', { bubbles: true }));
            box.focus();
        },
        clear: () => {
            box.value = '';
            box.dispatchEvent(new Event('input', { bubbles: true }));
            box.dispatchEvent(new Event('change', { bubbles: true }));
        },
    };
}
