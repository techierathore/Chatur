using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages;

/// <summary>
/// All projects (mockups/all-projects.html): every project Chatur knows with its phase, how many requirements
/// are open and verified, and when it was last worked on (REQ-FN-076, REQ-UI-067); opening one selects it and
/// goes to Project home (REQ-FN-077); and the sync panel for the developer's roles and rules (REQ-FN-078).
/// </summary>
public partial class AllProjects
{
    private AllProjectsView? objView;
    private RoleSyncStatus? objSync;
    private string objSearch = string.Empty;
    private AllProjectsOrder objOrder = AllProjectsOrder.LastWorked;
    private bool objCards;
    private bool objAddOpen;
    private bool objSyncing;

    private string Lead => objView is null
        ? "Reading every project."
        : $"{objView.TotalProjects} project{(objView.TotalProjects == 1 ? string.Empty : "s")} on this account. Today is {DateTime.Now.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)}.";

    private string SyncedText => objSync?.LastSyncedUtc is { } vWhen ? WhenText(vWhen) : "never";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objView = await AllProjectsActions.LoadAsync(objSearch, objOrder);
        objSync = await RoleSyncActions.StatusAsync();
    }

    private async Task ReloadAsync() => objView = await AllProjectsActions.LoadAsync(objSearch, objOrder);

    private async Task OnSearchChangedAsync(string? aText)
    {
        objSearch = aText ?? string.Empty;
        await ReloadAsync();
    }

    private async Task OnOrderChangedAsync(string? aValue)
    {
        objOrder = Enum.TryParse<AllProjectsOrder>(aValue, out var vOrder) ? vOrder : AllProjectsOrder.LastWorked;
        await ReloadAsync();
    }

    private void OnViewChanged(string? aValue) => objCards = aValue == "Cards";

    private void AddProject() => objAddOpen = true;

    /// <summary>Selects the project and opens Project home on it (REQ-FN-077).</summary>
    private async Task OpenProjectAsync(int aProjectId)
    {
        await AllProjectsActions.OpenAsync(aProjectId);
        Nav.NavigateTo("/");
    }

    private async Task SyncNowAsync()
    {
        objSyncing = true;
        try
        {
            objSync = await RoleSyncActions.SyncNowAsync();
        }
        finally
        {
            objSyncing = false;
        }
    }

    private static string OrderLabel(string? aValue) => aValue switch
    {
        nameof(AllProjectsOrder.Name) => "Name",
        nameof(AllProjectsOrder.OpenRequirements) => "Open requirements",
        _ => "Last worked on",
    };

    private static string Slug(string aName) => Regex.Replace(aName.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    /// <summary>The last two parts of a folder path, so a long one does not push the other columns out of view; the whole path is the cell's tooltip.</summary>
    private static string ShortPath(string aPath)
    {
        var vParts = aPath.TrimEnd('/', '\\').Split('/', '\\');
        return vParts.Length <= 3 ? aPath : "…/" + string.Join('/', vParts[^2..]);
    }

    private static string PhaseText(ProjectStanding aProject) => aProject.Phase is { } vPhase ? $"Phase {vPhase}" : "—";

    private static double VerifiedPercent(ProjectStanding aProject) => aProject.Requirements == 0 ? 0 : 100.0 * aProject.Verified / aProject.Requirements;

    private static BadgeVariant StateVariant(string aState) => aState switch
    {
        "active" => BadgeVariant.Success,
        "paused" => BadgeVariant.Warning,
        _ => BadgeVariant.Secondary,
    };

    /// <summary>"today 14:31", "yesterday", or "19 Sep 2026" — in the machine's own time.</summary>
    private static string WhenText(DateTime? aUtc)
    {
        if (aUtc is not { } vUtc)
        {
            return "never";
        }

        var vLocal = vUtc.ToLocalTime();
        var vDays = (DateTime.Now.Date - vLocal.Date).Days;
        return vDays switch
        {
            0 => $"today {vLocal.ToString("HH:mm", CultureInfo.InvariantCulture)}",
            1 => "yesterday",
            _ => vLocal.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
        };
    }
}
