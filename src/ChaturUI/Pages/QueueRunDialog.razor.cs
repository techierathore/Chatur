using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages;

/// <summary>
/// The "Queue a run" form on Run queue (mockups/run-queue.html). Files the run through
/// <see cref="IRunQueueActions.QueueAsync"/> and shows a refusal (no project, a brief that is too
/// short or too long) inside the dialog rather than throwing.
/// </summary>
public partial class QueueRunDialog
{
    private IReadOnlyList<ProjectSummary> objProjects = Array.Empty<ProjectSummary>();
    private string objProjectKey = string.Empty;
    private string objBrief = string.Empty;
    private string? objErrorMessage;
    private bool objIsBusy;
    private bool objWasOpen;

    /// <summary>Whether the dialog is showing.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the dialog opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised after a run was queued.</summary>
    [Parameter]
    public EventCallback<RunQueueAdded> OnQueued { get; set; }

    private string ProjectKey
    {
        get => objProjectKey;
        set => objProjectKey = value ?? string.Empty;
    }

    private string Brief
    {
        get => objBrief;
        set => objBrief = value ?? string.Empty;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Open && !objWasOpen)
        {
            objErrorMessage = null;
            objBrief = string.Empty;
            try
            {
                objProjects = await ProjectActions.ScanAsync();
                var vSelected = await ProjectActions.SelectedProjectAsync();
                objProjectKey = (vSelected?.ProjectId ?? objProjects.FirstOrDefault()?.ProjectId)?.ToString() ?? string.Empty;
            }
            catch (Exception aEx)
            {
                objErrorMessage = aEx.Message;
            }
        }

        objWasOpen = Open;
    }

    private async Task HandleOpenChangedAsync(bool aOpen)
    {
        await OpenChanged.InvokeAsync(aOpen);
    }

    private Task CloseAsync() => OpenChanged.InvokeAsync(false);

    private async Task QueueAsync()
    {
        objErrorMessage = null;
        if (!int.TryParse(objProjectKey, out var vProjectId))
        {
            objErrorMessage = "Pick the project the run is on.";
            return;
        }

        objIsBusy = true;
        try
        {
            var vAdded = await RunQueueActions.QueueAsync(vProjectId, objBrief);
            await OnQueued.InvokeAsync(vAdded);
        }
        catch (ArgumentException aEx)
        {
            objErrorMessage = aEx.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }
}
