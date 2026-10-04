using Microsoft.AspNetCore.Components;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// The "Add an agent" dialog on Settings ▸ Agents (mockups/settings-agents.html). Files a new agent
/// through <see cref="IRoleActions.AddAsync"/> and reports a refusal (an empty or taken name, wording
/// that is too short) inside the dialog rather than throwing.
/// </summary>
public partial class AddAgentDialog
{
    private string objName = string.Empty;
    private string objWording = string.Empty;
    private bool objChangesCode;
    private bool objRunsCommands;
    private string? objErrorMessage;
    private bool objIsBusy;

    /// <summary>Whether the dialog is showing.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the dialog opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised after an agent was added, with its role id, so the page can show it.</summary>
    [Parameter]
    public EventCallback<int> OnAdded { get; set; }

    private string AgentName
    {
        get => objName;
        set => objName = value ?? string.Empty;
    }

    private string Wording
    {
        get => objWording;
        set => objWording = value ?? string.Empty;
    }

    private async Task AddAsync()
    {
        objIsBusy = true;
        objErrorMessage = null;
        try
        {
            var vActions = new List<string> { "read-file" };
            if (objChangesCode)
            {
                vActions.Add("edit-file");
            }

            if (objRunsCommands)
            {
                vActions.Add("run-build");
            }

            var vDetail = await RoleActions.AddAsync(objName, objWording, 2, vActions);
            objName = string.Empty;
            objWording = string.Empty;
            objChangesCode = false;
            objRunsCommands = false;
            await OnAdded.InvokeAsync(vDetail.Summary.RoleId);
            await HandleOpenChangedAsync(false);
        }
        catch (ArgumentException aEx)
        {
            objErrorMessage = aEx.Message.Split(" (Parameter", 2)[0];
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private async Task HandleOpenChangedAsync(bool aOpen)
    {
        if (!aOpen)
        {
            objErrorMessage = null;
        }

        await OpenChanged.InvokeAsync(aOpen);
    }

    private async Task CloseAsync() => await HandleOpenChangedAsync(false);
}
