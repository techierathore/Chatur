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
    private PhaseEstimate? objEstimate;
    private int? objEstimateProjectId;
    private string? objEstimateError;

    /// <summary>Whether the dialog is showing.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the dialog opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised after a run was queued.</summary>
    [Parameter]
    public EventCallback<RunQueueAdded> OnQueued { get; set; }

    private bool IsBriefFilled => objBrief.Trim().Length >= AutoRunBrief.MinLength;

    private string EstimateTitle =>
        $"Before {objProjects.FirstOrDefault(aProject => aProject.ProjectId.ToString() == objProjectKey)?.Name ?? "the run"} joins the queue — what it should cost";

    private async Task OnProjectChangedAsync(string aValue)
    {
        objProjectKey = aValue ?? string.Empty;
        objEstimate = null;
        objEstimateProjectId = null;
        await LoadEstimateAsync();
    }

    private async Task OnBriefChangedAsync(string? aValue)
    {
        objBrief = aValue ?? string.Empty;
        await LoadEstimateAsync();
    }

    /// <summary>Works out the estimate once the run is filled in (a project and a brief); it depends on the project, so typing on does not ask again.</summary>
    private async Task LoadEstimateAsync()
    {
        if (!IsBriefFilled || !int.TryParse(objProjectKey, out var vProjectId) || (objEstimateProjectId == vProjectId && objEstimate is not null))
        {
            return;
        }

        try
        {
            objEstimate = await EstimateActions.EstimateAsync(vProjectId);
            objEstimateProjectId = vProjectId;
            objEstimateError = null;
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            objEstimate = null;
            objEstimateProjectId = null;
            objEstimateError = aEx.Message;
        }
    }

    /// <summary>Keeps the estimate the owner was shown with the queued run. A failure is shown and never undoes the queueing.</summary>
    private async Task KeepEstimateAsync(int aProjectId, RunQueueAdded aAdded)
    {
        try
        {
            var vEstimate = objEstimateProjectId == aProjectId && objEstimate is not null
                ? objEstimate
                : await EstimateActions.EstimateAsync(aProjectId);
            if (vEstimate is not null)
            {
                await EstimateActions.ConfirmAsync(aProjectId, vEstimate, EstimateRunKinds.Queue, aAdded.Item.QueueItemId);
            }
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            ToastService.Error(aEx.Message, "The run is queued but its estimate was not kept");
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (Open && !objWasOpen)
        {
            objErrorMessage = null;
            objBrief = string.Empty;
            objEstimate = null;
            objEstimateProjectId = null;
            objEstimateError = null;
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
            await KeepEstimateAsync(vProjectId, vAdded);
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
