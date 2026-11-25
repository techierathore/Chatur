using Chatur.Core.Actions;
using Chatur.Core.Integrations;

namespace ChaturUI.Pages;

/// <summary>
/// Integrations (mockups/integrations.html): the actions Chatur offers to outside tools, what the verifier
/// drove on real windows and the signed downloads of a release (REQ-FN-081 to REQ-FN-083, REQ-UI-069).
/// </summary>
public partial class Integrations
{
    private IReadOnlyList<PublishedAction>? objActions;
    private IReadOnlyList<OutsideRun>? objOutsideRuns;
    private IReadOnlyList<WindowCheck>? objChecks;
    private IReadOnlyList<SignedDownload>? objDownloads;
    private string? objNewToken;

    private int OnCount => objActions?.Count(aAction => aAction.Enabled) ?? 0;

    private string Lead =>
        $"{objActions?.Count ?? 0} actions over {Navigation.BaseUri.TrimEnd('/')}. {(objActions?.Count ?? 0) - OnCount} are off, so they are not there at all.";

    private string ExampleCall =>
        $"POST {Navigation.BaseUri.TrimEnd('/')}{PublishedActionCatalogue.Root}/process/start\n"
        + "Authorization: Bearer chatur_pat_…\n"
        + "Content-Type: application/json\n\n"
        + "{ \"project\": \"MyProject\", \"process\": \"verify-phase\" }\n\n"
        + "200 OK\n"
        + "{ \"run\": 483, \"state\": \"Running\", \"project\": \"MyProject\", \"process\": \"verify-phase\" }";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        objActions = await IntegrationActions.ListActionsAsync();
        objOutsideRuns = await IntegrationActions.ListOutsideRunsAsync();
        objChecks = await IntegrationActions.ListWindowChecksAsync();
        objDownloads = await IntegrationActions.ListDownloadsAsync();
    }

    private async Task NewTokenAsync()
    {
        var vToken = await IntegrationActions.CreateTokenAsync();
        objNewToken = vToken.Value;
    }

    /// <summary>Puts the example call and its answer on the clipboard (mockup copy-example).</summary>
    private async Task CopyExampleAsync()
    {
        if (await Browser.CopyTextAsync(ExampleCall))
        {
            Toast.Success("Copied the call and its answer");
        }
    }

    private async Task CopyTokenAsync()
    {
        if (objNewToken is not null && await Browser.CopyTextAsync(objNewToken))
        {
            Toast.Success("Token copied");
        }
    }

    private void HideToken() => objNewToken = null;

    private async Task SetAsync(PublishedAction aAction, bool aOn)
    {
        try
        {
            await IntegrationActions.SetActionEnabledAsync(aAction.Code, aOn);
        }
        catch (ArgumentException vException)
        {
            Toast.Error(vException.Message);
        }

        objActions = await IntegrationActions.ListActionsAsync();
    }

    private static string StateText(PublishedAction aAction) => aAction.Enabled ? "on" : aAction.Built ? "off" : "planned";

    private static string NotarisationText(NotarisationState aState) => aState switch
    {
        NotarisationState.Notarised => "Notarised",
        NotarisationState.NotNotarised => "Not notarised",
        _ => "Notarising does not apply",
    };

    private static string When(DateTime aUtc) => aUtc.ToLocalTime().ToString("d MMM yyyy HH:mm");
}
