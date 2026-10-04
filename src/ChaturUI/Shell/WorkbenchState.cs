namespace ChaturUI.Shell;

/// <summary>
/// The Workbench's own open-tab state — which files are open, which one is active, and which have
/// unsaved changes (REQ-UI-040, REQ-UI-041). This is presentation state, not action-layer data, so it
/// lives here in <c>ChaturUI</c> rather than in <c>Chatur.Core.AppState</c>: <see cref="FilesPanel"/>
/// opens a file into it when a tree row is chosen, and <see cref="EditorArea"/> renders whatever it
/// holds — the two regions coordinate through this rather than through parameters neither owns.
/// </summary>
public sealed class WorkbenchState
{
    private readonly List<OpenFile> objOpenFiles = [];
    private readonly HashSet<string> objPendingChangePaths = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Raised after the open files, the active file, or a file's own text changes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised the moment a build or run is asked for, before <c>IBuildRunActions</c> is even called
    /// (REQ-UI-012, REQ-UI-013). <c>BuildRunActions</c>' own output buffer is one growing list per
    /// project shared across every build and run in turn — a build's lines are still sitting in it
    /// when the next run starts — so <see cref="OutputStrip"/> needs to know exactly when a fresh one
    /// begins to clear what it is showing, rather than growing its own display forever alongside it.
    /// </summary>
    public event Action? BuildOrRunStarted;

    /// <summary>Announces that a build or run was just asked for, for <see cref="BuildOrRunStarted"/>'s listener.</summary>
    public void NotifyBuildOrRunStarted() => BuildOrRunStarted?.Invoke();

    /// <summary>
    /// Raised after the branch checked out for the selected project may have changed — the toolbar's
    /// branch picker switched it — so the files panel re-reads the branch it names.
    /// </summary>
    public event Action? BranchChanged;

    /// <summary>Announces that the project's checked-out branch may have changed, for <see cref="BranchChanged"/>'s listener.</summary>
    public void NotifyBranchChanged() => BranchChanged?.Invoke();

    /// <summary>
    /// Whether the file view (the tabs and the editor) is showing beside the conversation. Closing it
    /// gives the conversation the whole window; opening a file, or a proposed change, brings it back
    /// (UI Design "Screen: Workbench", <c>close-file-view</c>).
    /// </summary>
    public bool FileViewVisible { get; private set; } = true;

    /// <summary>Hides the file view so the conversation takes the window. The open tabs are kept.</summary>
    public void CloseFileView()
    {
        FileViewVisible = false;
        RaiseChanged();
    }

    /// <summary>
    /// Records which files have a proposed change waiting for the owner, so the editor can keep each
    /// read-only until it is settled (UI Design "Screen: Workbench", <c>editor</c>).
    /// </summary>
    /// <param name="aRelativePaths">The files' paths relative to the project root.</param>
    public void SetPendingChanges(IEnumerable<string> aRelativePaths)
    {
        var vNext = aRelativePaths.Select(NormalizePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (vNext.SetEquals(objPendingChangePaths))
        {
            return;
        }

        objPendingChangePaths.Clear();
        objPendingChangePaths.UnionWith(vNext);
        RaiseChanged();
    }

    /// <summary>Whether a proposed change is waiting on this file, which makes it read-only until the owner settles it.</summary>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    public bool HasPendingChange(string aRelativePath) => objPendingChangePaths.Contains(NormalizePath(aRelativePath));

    private static string NormalizePath(string aPath) => aPath.Replace('\\', '/').TrimStart('/');

    /// <summary>Every file open in a tab, in the order they were opened.</summary>
    public IReadOnlyList<OpenFile> OpenFiles => objOpenFiles;

    /// <summary>The relative path identifying the active tab, or <see langword="null"/> when none is open.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The active tab's own file, or <see langword="null"/> when none is open.</summary>
    public OpenFile? ActiveFile => objOpenFiles.FirstOrDefault(f => f.Id == ActiveId);

    /// <summary>
    /// Opens a file into a tab, or brings its existing tab to the front (REQ-UI-040).
    /// </summary>
    /// <param name="aRelativePath">The file's path relative to the project root; also the tab's identity.</param>
    /// <param name="aContent">The file's text, read fresh from disk.</param>
    public void Open(string aRelativePath, string aContent)
    {
        var vExisting = objOpenFiles.FirstOrDefault(f => f.Id == aRelativePath);
        if (vExisting is null)
        {
            objOpenFiles.Add(new OpenFile(aRelativePath, aContent));
        }

        ActiveId = aRelativePath;
        FileViewVisible = true;
        RaiseChanged();
    }

    /// <summary>
    /// Replaces an open tab's text with what is now on disk — after an approved change wrote the file
    /// — and clears any unsaved mark. A file that is not open is left alone.
    /// </summary>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aContent">The file's text, read fresh from disk.</param>
    public void Reload(string aRelativePath, string aContent)
    {
        var vFile = objOpenFiles.FirstOrDefault(f => f.Id == aRelativePath);
        if (vFile is null)
        {
            return;
        }

        vFile.OriginalContent = aContent;
        vFile.CurrentContent = aContent;
        RaiseChanged();
    }

    /// <summary>Makes an already-open tab the active one.</summary>
    /// <param name="aId">The tab's identity (its relative path).</param>
    public void Activate(string aId)
    {
        ActiveId = aId;
        RaiseChanged();
    }

    /// <summary>
    /// Records the editor's current text for a tab, without saving it — this is what puts the unsaved
    /// mark on the tab (REQ-UI-041).
    /// </summary>
    /// <param name="aId">The tab to update.</param>
    /// <param name="aContent">The editor's current text.</param>
    public void UpdateContent(string aId, string aContent)
    {
        var vFile = objOpenFiles.FirstOrDefault(f => f.Id == aId);
        if (vFile is null)
        {
            return;
        }

        vFile.CurrentContent = aContent;
        RaiseChanged();
    }

    /// <summary>Marks a tab's current text as the text now on disk, clearing its unsaved mark.</summary>
    /// <param name="aId">The tab that was just saved.</param>
    public void MarkSaved(string aId)
    {
        var vFile = objOpenFiles.FirstOrDefault(f => f.Id == aId);
        if (vFile is null)
        {
            return;
        }

        vFile.OriginalContent = vFile.CurrentContent;
        RaiseChanged();
    }

    /// <summary>
    /// Closes a tab outright — the caller is responsible for asking first when
    /// <see cref="OpenFile.IsDirty"/> is true (REQ-UI-041).
    /// </summary>
    /// <param name="aId">The tab to close.</param>
    public void Close(string aId)
    {
        var vIndex = objOpenFiles.FindIndex(f => f.Id == aId);
        if (vIndex < 0)
        {
            return;
        }

        objOpenFiles.RemoveAt(vIndex);
        if (ActiveId == aId)
        {
            ActiveId = objOpenFiles.Count == 0 ? null : objOpenFiles[Math.Min(vIndex, objOpenFiles.Count - 1)].Id;
        }

        RaiseChanged();
    }

    /// <summary>Closes every open tab — called when the selected project changes.</summary>
    public void Reset()
    {
        objOpenFiles.Clear();
        ActiveId = null;
        FileViewVisible = true;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();
}

/// <summary>One open tab: a file's path, the text on disk when it was opened, and the editor's current text.</summary>
public sealed class OpenFile
{
    /// <summary>
    /// Creates the tab.
    /// </summary>
    /// <param name="aRelativePath">The file's path relative to the project root.</param>
    /// <param name="aContent">The text read from disk when the tab opened.</param>
    public OpenFile(string aRelativePath, string aContent)
    {
        Id = aRelativePath;
        RelativePath = aRelativePath;
        OriginalContent = aContent;
        CurrentContent = aContent;
    }

    /// <summary>The tab's identity — the file's relative path.</summary>
    public string Id { get; }

    /// <summary>The file's path relative to the project root.</summary>
    public string RelativePath { get; }

    /// <summary>The file's own name, for the tab label.</summary>
    public string Name => System.IO.Path.GetFileName(RelativePath);

    /// <summary>The text on disk as of the last read or save.</summary>
    public string OriginalContent { get; set; }

    /// <summary>The editor's current text, which may differ from <see cref="OriginalContent"/>.</summary>
    public string CurrentContent { get; set; }

    /// <summary>Whether the editor's text differs from what is on disk (REQ-UI-041).</summary>
    public bool IsDirty => !string.Equals(OriginalContent, CurrentContent, StringComparison.Ordinal);
}
