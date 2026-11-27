using Chatur.Core;
using Chatur.Core.Actions;
using Chatur.Core.Platform;
using Microsoft.AspNetCore.Components;
using TrBlazeUI.Components.Toast;

namespace ChaturUI.Shell;

/// <summary>
/// Code-behind for <c>NewProjectDialog.razor</c>, the one New project dialog of every state (BRD-244, BRD-247,
/// BRD-276, REQ-UI-112, REQ-FN-310). It always lists the machine's <c>dotnet new</c> project templates, and always
/// shows the two full paths Create will make before it makes them: the solution file and the project folder. The
/// folder that is open (a folder, or an open solution's own folder) is where things go, shown and not typed, with
/// "Change location" for the rare case; with nothing open there is one Location field. The solution file is made in
/// that folder itself and the project one folder under it (owner's test of 23 Nov 2026: never a folder named after
/// the solution inside the folder that was open). After Create the run targets are read again, so Run has the new
/// project (BRD-248, REQ-FN-309).
/// </summary>
public partial class NewProjectDialog
{
    private static readonly NewProjectPlan NoPlan = new(string.Empty, null, false, null, null, [], null);

    private IReadOnlyList<ProjectTemplate> objTemplates = [];
    private ProjectTemplate? objSelected;
    private TemplateFrameworks objFrameworks = new([], null);
    private string? objFramework;
    private string objName = string.Empty;
    private string objSolutionName = string.Empty;
    private string objLocation = string.Empty;
    private string objSearch = string.Empty;
    private bool objChangeLocation;
    private string objSolutionExtension = ".slnx";
    private NewProjectPlan objPlan = NoPlan;
    private string? objErrorMessage;
    private bool objLoading;
    private bool objFrameworkLoading;
    private bool objIsBusy;
    private bool objWasOpen;

    [Inject]
    private IProjectTemplateActions Templates { get; set; } = default!;

    [Inject]
    private IBuildRunActions BuildRun { get; set; } = default!;

    [Inject]
    private IFolderPicker Picker { get; set; } = default!;

    [Inject]
    private AppState AppStateService { get; set; } = default!;

    [Inject]
    private WorkbenchState Workbench { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    /// <summary>Whether the dialog is open. Two-way bindable from the caller with <c>@bind-Open</c>.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when <see cref="Open"/> changes, including when the dialog closes itself.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Whether a solution is open on the Workbench, so the new project is added to it.</summary>
    [Parameter]
    public bool InSolution { get; set; }

    /// <summary>The folder open on the Workbench, whose emptiness makes it the suggested location (BRD-247); <see langword="null"/> when none.</summary>
    [Parameter]
    public string? OpenFolder { get; set; }

    [Inject]
    private ToastService Toast { get; set; } = default!;

    private string Title => InSolution ? $"New project in {SolutionName}" : "New project";

    private string Description => InSolution
        ? "Pick a template and name the project. It is made with dotnet new and added to the solution."
        : objPlan.SolutionExists
            ? "Pick a template and name the project. It is made with dotnet new and added to the solution this folder has."
            : "Pick a template and name the solution. Chatur makes the solution and its first project with dotnet new, and opens it.";

    private string NamePlaceholder => objPlan.SolutionExists
        ? "A name for the new project"
        : string.IsNullOrWhiteSpace(objSolutionName) ? "The solution's name, unless you type another" : objSolutionName.Trim();

    private string SolutionName => AppStateService.SelectedProject?.Name ?? "the solution";

    /// <summary>The folder that is open: the open solution's own folder, or the open folder; <see langword="null"/> with nothing open.</summary>
    private string? BaseFolder => InSolution
        ? Path.GetDirectoryName(AppStateService.SelectedProject?.Path ?? string.Empty)
        : string.IsNullOrWhiteSpace(OpenFolder) ? null : OpenFolder;

    /// <summary>Whether the folder is shown as "Will be created in", not as a field to type.</summary>
    private bool LocationIsFixed => BaseFolder is not null && !objChangeLocation;

    /// <summary>The solution's folder as things stand: the open folder, or what the person typed or picked.</summary>
    private string LocationValue => LocationIsFixed ? BaseFolder! : objLocation;

    /// <summary>Whether the solution's name is asked for: only when the folder has no solution yet.</summary>
    private bool AsksSolutionName => !InSolution && !objPlan.SolutionExists;

    /// <summary>The plan's problem, once the person has typed enough for it to be about what they typed.</summary>
    private string? PlanProblem => objPlan.Problem is { } vProblem && (objPlan.Folder.Length > 0 || objLocation.Length > 0 || objName.Length > 0 || objSolutionName.Length > 0)
        ? vProblem
        : null;

    private bool CanCreate => !objIsBusy && !objLoading && objSelected is not null
        && objPlan is { Problem: null, SolutionFile: not null, ProjectFolder: not null };

    private NewProjectRequest Request => new(
        LocationValue,
        InSolution ? AppStateService.SelectedProject?.Path : null,
        objSolutionName,
        objName,
        objSelected?.ShortName ?? string.Empty,
        objFramework,
        LocationIsFixed && !InSolution);

    /// <summary>The project types the search box leaves in the list: every word typed is in the name, short name or tags (REQ-UI-127).</summary>
    private IReadOnlyList<ProjectTemplate> ShownTemplates
    {
        get
        {
            var vWords = objSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return vWords.Length == 0
                ? objTemplates
                : objTemplates.Where(t => vWords.All(w =>
                    t.Name.Contains(w, StringComparison.OrdinalIgnoreCase)
                    || t.ShortName.Contains(w, StringComparison.OrdinalIgnoreCase)
                    || t.Tags.Contains(w, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }

    private void OnSearchChanged(string? aValue) => objSearch = aValue ?? string.Empty;

    /// <summary>Reads again what Create would make, from the names and the folder as they are now.</summary>
    private void Replan()
    {
        try
        {
            objPlan = Templates.Plan(Request, objSolutionExtension);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            objPlan = NoPlan with { Problem = aException.Message };
        }
    }

    private bool IsSelected(ProjectTemplate aTemplate) => objSelected is not null && objSelected.ShortName == aTemplate.ShortName;

    private int? ProjectIdForReads => AppStateService.SelectedProject?.ProjectId;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var vJustOpened = Open && !objWasOpen;
        objWasOpen = Open;
        if (vJustOpened)
        {
            await LoadAsync();
        }
    }

    /// <summary>Reads the machine's templates (a few seconds the first time, at once after), and starts on the console template.</summary>
    private async Task LoadAsync()
    {
        objName = string.Empty;
        objSolutionName = string.Empty;
        objLocation = string.Empty;
        objSearch = string.Empty;
        objChangeLocation = false;
        objErrorMessage = null;
        objSelected = null;
        objFramework = null;
        objFrameworks = new TemplateFrameworks([], null);
        objLoading = true;
        Replan();
        try
        {
            objTemplates = await Templates.TemplatesAsync(ProjectIdForReads);
            objSolutionExtension = await Templates.SolutionExtensionAsync(ProjectIdForReads);
            Replan();
            var vFirst = objTemplates.FirstOrDefault(t => t.ShortName == "console") ?? objTemplates.FirstOrDefault();
            objLoading = false;
            if (vFirst is not null)
            {
                await SelectTemplateAsync(vFirst);
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            objTemplates = [];
            objErrorMessage = aException.Message;
        }
        finally
        {
            objLoading = false;
        }
    }

    /// <summary>Chooses a template, and reads the frameworks it can target.</summary>
    private async Task SelectTemplateAsync(ProjectTemplate aTemplate)
    {
        objSelected = aTemplate;
        objFrameworks = new TemplateFrameworks([], null);
        objFramework = null;
        objFrameworkLoading = true;
        try
        {
            var vFrameworks = await Templates.FrameworksAsync(ProjectIdForReads, aTemplate.ShortName);
            if (objSelected?.ShortName == aTemplate.ShortName)
            {
                objFrameworks = vFrameworks;
                objFramework = vFrameworks.Preferred;
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            // The template's own framework is used when its choices cannot be read.
            objErrorMessage = aException.Message;
        }
        finally
        {
            objFrameworkLoading = false;
        }
    }

    private void OnFrameworkChanged(string? aValue) => objFramework = string.IsNullOrEmpty(aValue) ? objFramework : aValue;

    private void OnSolutionNameChanged(string? aValue)
    {
        objSolutionName = aValue ?? string.Empty;
        Replan();
    }

    private void OnNameChanged(string? aValue)
    {
        objName = aValue ?? string.Empty;
        Replan();
    }

    private void OnLocationChanged(string? aValue)
    {
        objLocation = aValue ?? string.Empty;
        Replan();
    }

    /// <summary>"Change location": the folder becomes a field, starting on the folder that was shown.</summary>
    private void ChangeLocation()
    {
        objLocation = BaseFolder ?? string.Empty;
        objChangeLocation = true;
        Replan();
    }

    /// <summary>Opens the machine's own folder picker for the solution's folder.</summary>
    private async Task BrowseAsync()
    {
        if (await Picker.PickFolderAsync("The folder the solution goes in") is { } vPicked)
        {
            objLocation = vPicked;
            Replan();
        }
    }

    /// <summary>
    /// Makes what the dialog showed: the project, in the solution the folder has or in a new one made in the folder
    /// itself. A solution that was not open is opened on the Workbench. Then the run targets and the files are read
    /// again, so Run and FILES have the new project.
    /// </summary>
    private async Task CreateAsync()
    {
        if (objSelected is null)
        {
            return;
        }

        objIsBusy = true;
        objErrorMessage = null;
        try
        {
            var vMade = await Templates.CreateAsync(Request);

            // The project just added is the one that runs, when it can run (REQ-FN-309, owner's test of 25 Nov 2026).
            await BuildRun.SetRunTargetForProjectFileAsync(vMade.ProjectId, vMade.ProjectFile);
            Workbench.SetRunTargets(await BuildRun.TargetsAsync(vMade.ProjectId));
            Workbench.SetStartupTargets(await BuildRun.StartupAsync(vMade.ProjectId));
            Workbench.NotifyFilesViewChanged();
            if (vMade.ProjectsAdded.Count > 0)
            {
                Toast.Success($"{Path.GetFileName(vMade.SolutionFile)} was made in {Path.GetDirectoryName(vMade.SolutionFile)} and now holds the {(vMade.ProjectsAdded.Count == 1 ? "project" : $"{vMade.ProjectsAdded.Count} projects")} that had no solution.", "New project");
            }

            await OpenChanged.InvokeAsync(false);
            if (vMade.OpenedNow)
            {
                Nav.NavigateTo("/");
            }
        }
        catch (Exception aException) when (aException is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            objErrorMessage = aException.Message;
            Replan();
        }
        finally
        {
            objIsBusy = false;
        }
    }
}
