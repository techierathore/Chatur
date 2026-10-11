using Chatur.Core.Actions;
using ChaturUI.Shell;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// Deep link to Processes, a Process run, the Requirements board or the Agent workspace: opens that
/// screen as a tab in the main window and goes on to it. It is a way in by address, not how the screens
/// are reached — the Project menu and the buttons on the screens open the same tabs.
/// </summary>
public partial class ToolTabLink
{
    /// <summary>The run's id, for <c>/processes/{id}/run</c>.</summary>
    [Parameter]
    public int Id { get; set; }

    /// <summary>The agent's code, for <c>/workspace/{agent}</c>.</summary>
    [Parameter]
    public string? Agent { get; set; }

    /// <summary>The process to open, for <c>/processes?process=verify-phase</c>.</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "process")]
    public string? Process { get; set; }

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    [Inject]
    private WorkbenchState WorkbenchTabs { get; set; } = default!;

    [Inject]
    private IProjectActions Projects { get; set; } = default!;

    [Inject]
    private IProcessRunActions Runs { get; set; } = default!;

    /// <summary>
    /// Runs once the window is interactive, not while the page is first rendered on the server: a tab
    /// opened during that first render would belong to a state the live window never sees.
    /// </summary>
    /// <param name="aFirstRender">Whether this is the component's first render.</param>
    protected override async Task OnAfterRenderAsync(bool aFirstRender)
    {
        if (!aFirstRender)
        {
            return;
        }

        // The selected project is read first, so the main window finds it already chosen and does not
        // close the tab being opened as though the project had changed.
        await Projects.SelectedProjectAsync();

        var vPath = "/" + Nav.ToBaseRelativePath(Nav.Uri).Split('?', '#')[0].Trim('/');
        if (vPath == "/board")
        {
            WorkbenchTabs.OpenBoard();
        }
        else if (vPath == "/auto")
        {
            WorkbenchTabs.OpenAutoRun();
        }
        else if (vPath == "/auto/queue")
        {
            WorkbenchTabs.OpenRunQueue();
        }
        else if (vPath == "/auto/report")
        {
            WorkbenchTabs.OpenRunReport();
        }
        else if (vPath.StartsWith("/workspace/", StringComparison.Ordinal))
        {
            WorkbenchTabs.OpenAgentWorkspace(Agent);
        }
        else if (vPath.StartsWith("/processes/", StringComparison.Ordinal))
        {
            var vRun = await Runs.GetAsync(Id);
            if (vRun is null)
            {
                WorkbenchTabs.OpenProcesses();
            }
            else
            {
                WorkbenchTabs.OpenProcessRun(vRun.RunId, vRun.ProcessCode);
            }
        }
        else
        {
            WorkbenchTabs.OpenProcesses(Process);
        }

        Nav.NavigateTo("/", replace: true);
    }
}
