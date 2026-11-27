using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Shell;

/// <summary>
/// Code-behind for <c>StartupProjectsDialog.razor</c> (REQ-FN-324): which projects of the open solution Run starts
/// together. It lists each project that can run once, ticks the ones saved before, and saves the choice for this
/// solution through <see cref="IBuildRunActions.SetStartupAsync"/>.
/// </summary>
public partial class StartupProjectsDialog
{
    private IReadOnlyList<RunTarget> objProjects = [];
    private readonly HashSet<string> objTicked = new(StringComparer.OrdinalIgnoreCase);
    private bool objIsBusy;
    private bool objWasOpen;

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    /// <summary>Whether the dialog is open. Two-way bindable from the caller with <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when <see cref="Open"/> changes, including when the dialog closes itself.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    private string Hint => objTicked.Count switch
    {
        0 => "Nothing ticked: Run starts the one project chosen in the target picker.",
        1 => "One ticked: Run starts that project alone.",
        _ => $"{objTicked.Count} ticked: Run builds and starts them together, and Stop ends them all."
    };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var vJustOpened = Open && !objWasOpen;
        objWasOpen = Open;
        if (!vJustOpened)
        {
            return;
        }

        // One row per project: a project that targets several platforms is listed once, on the one this machine can build.
        objProjects = Workbench.RunTargets
            .Where(t => !t.IsTest && t.ProjectFile.Length > 0 && t.IsAvailableOnThisMachine)
            .GroupBy(t => t.ProjectFile, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        objTicked.Clear();
        var vChosen = Workbench.HasMultipleStartup ? Workbench.StartupTargets : [];
        foreach (var vTarget in vChosen)
        {
            objTicked.Add(vTarget.ProjectFile);
        }
    }

    private static string NameOf(RunTarget aTarget) => System.IO.Path.GetFileNameWithoutExtension(aTarget.ProjectFile);

    private static string Slug(string aFile) =>
        new(aFile.Select(c => char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-').ToArray());

    private void Tick(string aFile, bool aOn)
    {
        if (aOn)
        {
            objTicked.Add(aFile);
        }
        else
        {
            objTicked.Remove(aFile);
        }
    }

    /// <summary>Keeps the choice and tells the toolbar and Files, so both show what Run will start.</summary>
    private async Task SaveAsync()
    {
        if (AppStateService.SelectedProject is not { } vProject)
        {
            return;
        }

        objIsBusy = true;
        try
        {
            var vFiles = objProjects.Select(p => p.ProjectFile).Where(objTicked.Contains).ToList();
            await BuildRun.SetStartupAsync(vProject.ProjectId, vFiles);
            if (vFiles.Count == 1 && objProjects.FirstOrDefault(p => p.ProjectFile == vFiles[0]) is { } vOnly)
            {
                Workbench.SelectRunTarget(vOnly);
                await BuildRun.SetRunTargetAsync(vProject.ProjectId, vOnly.RunTargetId);
            }

            Workbench.SetStartupTargets(await BuildRun.StartupAsync(vProject.ProjectId));
            await OpenChanged.InvokeAsync(false);
        }
        finally
        {
            objIsBusy = false;
        }
    }
}
