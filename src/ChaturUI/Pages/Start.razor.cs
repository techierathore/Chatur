using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Start (mockups/start.html). Reopens whatever project survived the restart (REQ-FN-006, cluster
/// F); otherwise loads the named folders and the projects Chatur finds in them (REQ-UI-005,
/// REQ-UI-006, REQ-UI-008, REQ-UI-009) and opens the one the owner chooses into the Workbench
/// (REQ-UI-007).
/// </summary>
public partial class Start
{
    private IReadOnlyList<ProjectFolder> objFolders = Array.Empty<ProjectFolder>();
    private IReadOnlyList<ProjectSummary> objProjects = Array.Empty<ProjectSummary>();
    private bool objIsLoading = true;
    private bool objDialogOpen;
    private bool objSearchOpen;
    private string? objSearchText;
    private bool objForgetMode;

    /// <summary>
    /// Set by the menu bar's "All projects…" (<c>/start?all=1</c>): the owner deliberately came to
    /// the project list, so Start must show it even though a project is selected (REQ-UI-010).
    /// </summary>
    [SupplyParameterFromQuery(Name = "all")]
    public string? All { get; set; }

    /// <summary>
    /// The projects the list shows: every one, or only those whose name or any part of whose path
    /// holds what the owner typed in the search box (UI Design "Screen: Start", <c>recent-search</c>).
    /// </summary>
    private IReadOnlyList<ProjectSummary> VisibleProjects
    {
        get
        {
            var vText = objSearchText?.Trim();
            if (!objSearchOpen || string.IsNullOrEmpty(vText))
            {
                return objProjects;
            }

            return objProjects
                .Where(aProject => aProject.Name.Contains(vText, StringComparison.OrdinalIgnoreCase)
                    || aProject.Path.Contains(vText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    private bool ShowAll => !string.IsNullOrEmpty(All);

    /// <summary>
    /// Reopens whatever project survived the restart (REQ-FN-006): when
    /// <see cref="IProjectActions.SelectedProjectAsync"/> still names one, Start sends the owner
    /// straight into the Workbench for it — the same place choosing it from the recent list would
    /// (REQ-UI-007) — rather than making him pick it again. With nothing selected yet, Start loads
    /// the named folders and scans them for the recent list (REQ-UI-006, REQ-UI-008).
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var vSelectedProject = await Projects.SelectedProjectAsync();
        if (vSelectedProject is not null && !ShowAll)
        {
            Nav.NavigateTo("/");
            return;
        }

        await ReloadAsync();
    }

    /// <summary>
    /// Re-reads the named folders and re-scans them for the projects they hold. Called on load, and
    /// again after the Project folders dialog adds or removes a folder (REQ-UI-005, REQ-UI-009).
    /// </summary>
    private async Task ReloadAsync()
    {
        objIsLoading = true;
        StateHasChanged();
        try
        {
            objFolders = await Projects.ListFoldersAsync();
            objProjects = await Projects.ScanAsync();
        }
        finally
        {
            objIsLoading = false;
        }
    }

    private void OpenFoldersDialog() => objDialogOpen = true;

    /// <summary>Shows or hides the search box; hiding it clears the filter so the whole list returns.</summary>
    private void ToggleSearch()
    {
        objSearchOpen = !objSearchOpen;
        if (!objSearchOpen)
        {
            objSearchText = null;
        }
    }

    /// <summary>Turns the per-row "remove from the list" buttons on or off.</summary>
    private void ToggleForgetMode() => objForgetMode = !objForgetMode;

    /// <summary>
    /// Drops one project from the list; its folder on disk is untouched (UI Design "Screen: Start",
    /// <c>forget</c>). The list is re-read so it matches what the database now holds.
    /// </summary>
    private async Task ForgetAsync(int aProjectId)
    {
        await Projects.ForgetProjectAsync(aProjectId);
        await ReloadAsync();
        if (objProjects.Count == 0)
        {
            objForgetMode = false;
        }
    }

    /// <summary>Selects the project and opens the Workbench on it (REQ-UI-007).</summary>
    private async Task OpenProjectAsync(int aProjectId)
    {
        await Projects.SelectProjectAsync(aProjectId);
        Nav.NavigateTo("/");
    }

    /// <summary>A short, friendly rendering of when a project was last opened, for the recent list (REQ-UI-006).</summary>
    private static string FormatWhen(DateTime? aWhen)
    {
        if (aWhen is null)
        {
            return "Never opened";
        }

        var vAge = DateTime.UtcNow - aWhen.Value;
        return vAge switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalHours: < 1 } => $"{(int)vAge.TotalMinutes} minute(s) ago",
            { TotalDays: < 1 } => $"{(int)vAge.TotalHours} hour(s) ago",
            { TotalDays: < 30 } => $"{(int)vAge.TotalDays} day(s) ago",
            _ => aWhen.Value.ToLocalTime().ToString("d MMMM yyyy")
        };
    }
}
