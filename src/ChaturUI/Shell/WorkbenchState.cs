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
    private readonly List<ToolTab> objToolTabs = [];
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
    /// <remarks>The conversation comes back if it was hidden, so the window is never left with neither.</remarks>
    public void CloseFileView()
    {
        FileViewVisible = false;
        ConversationVisible = true;
        RaiseChanged();
    }

    /// <summary>
    /// Whether the conversation panel is showing (View ▸ Chat, and the close button in its header).
    /// Closing it gives the file view the whole window (UI Design "Screen: Workbench", REQ-UI-074).
    /// </summary>
    public bool ConversationVisible { get; private set; } = true;

    /// <summary>Whether the files panel is showing (View ▸ Files).</summary>
    public bool FilesVisible { get; private set; } = true;

    /// <summary>Whether the output strip is showing (View ▸ Output).</summary>
    public bool OutputVisible { get; private set; } = true;

    /// <summary>Hides or shows the conversation. The file view comes back if hiding the conversation would leave the window with neither.</summary>
    /// <param name="aVisible">Whether the conversation should show.</param>
    public void SetConversationVisible(bool aVisible)
    {
        ConversationVisible = aVisible;
        if (!aVisible)
        {
            FileViewVisible = true;
        }

        RaiseChanged();
    }

    /// <summary>Hides or shows the files panel.</summary>
    /// <param name="aVisible">Whether the panel should show.</param>
    public void SetFilesVisible(bool aVisible)
    {
        FilesVisible = aVisible;
        RaiseChanged();
    }

    /// <summary>Hides or shows the output strip.</summary>
    /// <param name="aVisible">Whether the strip should show.</param>
    public void SetOutputVisible(bool aVisible)
    {
        OutputVisible = aVisible;
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

    /// <summary>
    /// Every phase-2 screen open as a tab beside the files — Processes, Board, a Process run, the Agent
    /// workspace — in the order they were opened (UI Design phase 2: "Each of these opens as a tab in the
    /// one window, with the conversation still pinned to its right").
    /// </summary>
    public IReadOnlyList<ToolTab> ToolTabs => objToolTabs;

    /// <summary>The identity of the active tab (a file's relative path or a tool tab's id), or <see langword="null"/> when none is open.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The active tab's own file, or <see langword="null"/> when none is open or the active tab is a screen.</summary>
    public OpenFile? ActiveFile => objOpenFiles.FirstOrDefault(f => f.Id == ActiveId);

    /// <summary>The active tab's screen, or <see langword="null"/> when the active tab is a file or none is open.</summary>
    public ToolTab? ActiveTool => objToolTabs.FirstOrDefault(t => t.Id == ActiveId);

    /// <summary>How many tabs are open, files and screens together.</summary>
    public int TabCount => objOpenFiles.Count + objToolTabs.Count;

    /// <summary>Raised when something asks the conversation to open a session and, optionally, send a first message in it.</summary>
    public event Action<ConversationStart>? ConversationStartRequested;

    /// <summary>
    /// Asks the pinned conversation to open a session and send the first message there, so the reply
    /// streams into the conversation (REQ-UI-053, REQ-FN-059).
    /// </summary>
    /// <param name="aStart">The session to open and what to send first.</param>
    public void RequestConversation(ConversationStart aStart) => ConversationStartRequested?.Invoke(aStart);

    /// <summary>The session a process run handed a step to, while the conversation shows it (REQ-UI-071); <see langword="null"/> otherwise.</summary>
    public HandedSession? Handed { get; private set; }

    /// <summary>Raised when the conversation should start, or stop, showing a handed step's session.</summary>
    public event Action? HandedSessionChanged;

    /// <summary>Asks the conversation to show the agent session a run handed a step to, as it works (REQ-UI-071).</summary>
    /// <param name="aHanded">The session, the agent and the step it works on.</param>
    public void ShowHandedSession(HandedSession aHanded)
    {
        if (Handed == aHanded)
        {
            return;
        }

        Handed = aHanded;
        HandedSessionChanged?.Invoke();
    }

    /// <summary>Tells the conversation the handed step has ended; it keeps what was said and stops following the session.</summary>
    /// <param name="aSessionId">The session that ended.</param>
    public void EndHandedSession(int aSessionId)
    {
        if (Handed is null || Handed.SessionId != aSessionId)
        {
            return;
        }

        Handed = null;
        HandedSessionChanged?.Invoke();
    }

    /// <summary>Opens the Processes screen in a tab, or brings it to the front.</summary>
    /// <param name="aProcessCode">The process to open its steps for; <see langword="null"/> leaves the screen as it is.</param>
    public void OpenProcesses(string? aProcessCode = null)
    {
        if (aProcessCode is not null && objToolTabs.FirstOrDefault(t => t.Kind == ToolKind.Processes) is { } vTab)
        {
            vTab.ProcessCode = aProcessCode;
        }

        OpenTool(new ToolTab("tool:processes", ToolKind.Processes, "Processes") { ProcessCode = aProcessCode });
    }

    /// <summary>Opens the Requirements board in a tab, or brings it to the front.</summary>
    public void OpenBoard() => OpenTool(new ToolTab("tool:board", ToolKind.Board, "Board"));

    /// <summary>Opens one process run in a tab named after the process and the run, or brings it to the front.</summary>
    /// <param name="aRunId">The run's identity.</param>
    /// <param name="aProcessCode">The process the run belongs to, for the tab's label.</param>
    public void OpenProcessRun(int aRunId, string aProcessCode) =>
        OpenTool(new ToolTab($"tool:run:{aRunId}", ToolKind.ProcessRun, $"{aProcessCode} · run {aRunId}") { RunId = aRunId });

    /// <summary>Opens the End-to-end run in a tab, or brings it to the front (phase 3).</summary>
    public void OpenAutoRun() => OpenTool(new ToolTab("tool:auto", ToolKind.AutoRun, "Auto run"));

    /// <summary>Opens the Run queue in a tab, or brings it to the front (phase 3).</summary>
    public void OpenRunQueue() => OpenTool(new ToolTab("tool:auto-queue", ToolKind.RunQueue, "Run queue"));

    /// <summary>Opens the Run report in a tab, or brings it to the front (phase 3).</summary>
    public void OpenRunReport() => OpenTool(new ToolTab("tool:auto-report", ToolKind.RunReport, "Run record"));

    /// <summary>Opens the Agent workspace in a tab, or brings it to the front.</summary>
    /// <param name="aAgentCode">The agent to show; <see langword="null"/> lets the workspace fall back to the first agent.</param>
    public void OpenAgentWorkspace(string? aAgentCode = null)
    {
        OpenTool(new ToolTab("tool:agents", ToolKind.AgentWorkspace, "Agents") { AgentCode = aAgentCode });
        if (aAgentCode is not null)
        {
            SetWorkspaceAgent(aAgentCode);
        }
    }

    /// <summary>Points the Agent workspace tab at another agent; the agent's open sessions are not touched (REQ-FN-060).</summary>
    /// <param name="aAgentCode">The agent's code.</param>
    public void SetWorkspaceAgent(string aAgentCode)
    {
        if (objToolTabs.FirstOrDefault(t => t.Kind == ToolKind.AgentWorkspace) is { } vTab && vTab.AgentCode != aAgentCode)
        {
            vTab.AgentCode = aAgentCode;
            RaiseChanged();
        }
    }

    private void OpenTool(ToolTab aTab)
    {
        if (!objToolTabs.Any(t => t.Id == aTab.Id))
        {
            objToolTabs.Add(aTab);
        }

        ActiveId = aTab.Id;
        FileViewVisible = true;
        RaiseChanged();
    }

    private List<string> TabIds() => objOpenFiles.Select(f => f.Id).Concat(objToolTabs.Select(t => t.Id)).ToList();

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
        var vIndex = TabIds().IndexOf(aId);
        if (vIndex < 0)
        {
            return;
        }

        objOpenFiles.RemoveAll(f => f.Id == aId);
        objToolTabs.RemoveAll(t => t.Id == aId);
        if (ActiveId == aId)
        {
            var vLeft = TabIds();
            ActiveId = vLeft.Count == 0 ? null : vLeft[Math.Min(vIndex, vLeft.Count - 1)];
        }

        RaiseChanged();
    }

    /// <summary>Closes every open tab — called when the selected project changes.</summary>
    public void Reset()
    {
        objOpenFiles.Clear();
        objToolTabs.Clear();
        ActiveId = null;
        FileViewVisible = true;
        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();
}

/// <summary>The phase-2 screens that open as a tab in the main window.</summary>
public enum ToolKind
{
    /// <summary>The twelve processes and their steps.</summary>
    Processes,

    /// <summary>The requirements board.</summary>
    Board,

    /// <summary>One run of one process.</summary>
    ProcessRun,

    /// <summary>One agent's workspace.</summary>
    AgentWorkspace,

    /// <summary>The end-to-end run: one brief, the whole chain (phase 3).</summary>
    AutoRun,

    /// <summary>The queue of runs, one at a time (phase 3).</summary>
    RunQueue,

    /// <summary>The record of a finished run (phase 3).</summary>
    RunReport
}

/// <summary>One screen open as a tab beside the files.</summary>
public sealed class ToolTab
{
    /// <summary>Creates the tab.</summary>
    /// <param name="aId">The tab's identity; opening the same screen again brings this tab forward.</param>
    /// <param name="aKind">Which screen the tab shows.</param>
    /// <param name="aTitle">The tab's label.</param>
    public ToolTab(string aId, ToolKind aKind, string aTitle)
    {
        Id = aId;
        Kind = aKind;
        Title = aTitle;
    }

    /// <summary>The tab's identity.</summary>
    public string Id { get; }

    /// <summary>Which screen the tab shows.</summary>
    public ToolKind Kind { get; }

    /// <summary>The tab's label.</summary>
    public string Title { get; }

    /// <summary>For a process run: the run it shows.</summary>
    public int RunId { get; init; }

    /// <summary>For Processes: the process whose steps the screen opens (a deep link), or <see langword="null"/> for the default.</summary>
    public string? ProcessCode { get; set; }

    /// <summary>For the Agent workspace: the agent it shows now.</summary>
    public string? AgentCode { get; set; }
}

/// <summary>A request that the conversation open a session and, when <see cref="Message"/> is set, send it first.</summary>
/// <param name="SessionId">The session to open.</param>
/// <param name="RoleId">The agent the session runs as, or <see langword="null"/> to keep the session's own.</param>
/// <param name="Message">What to send first, or <see langword="null"/> to only open the session.</param>
public sealed record ConversationStart(int SessionId, int? RoleId, string? Message);

/// <summary>An agent session a process run handed a step to (REQ-UI-071).</summary>
/// <param name="SessionId">The agent's session.</param>
/// <param name="AgentName">The agent, shown as the speaker's name.</param>
/// <param name="ModelName">The model the session uses, when known.</param>
/// <param name="StepLabel">Which step it works on, e.g. <c>build-phase run 43, step 4</c>.</param>
public sealed record HandedSession(int SessionId, string AgentName, string? ModelName, string StepLabel);

/// <summary>One open tab: a file's path,the text on disk when it was opened, and the editor's current text.</summary>
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
