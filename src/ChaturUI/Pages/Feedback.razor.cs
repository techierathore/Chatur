using Chatur.Core;
using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using TrBlazeUI.Components.Badge;
using TrBlazeUI.Components.Toast;

namespace ChaturUI.Pages;

/// <summary>
/// Feedback and issues (mockups/feedback.html). Sends an entry through <see cref="IFeedbackActions"/>
/// (REQ-UI-062) carrying the name the owner picked (REQ-FN-073), lists every entry with its state and
/// answer (REQ-UI-063), and asks again every few seconds while an entry is waiting so it goes as soon as
/// the network is back, without another press (REQ-FN-074).
/// </summary>
public partial class Feedback : IDisposable
{
    private static readonly TimeSpan cRetryEvery = TimeSpan.FromSeconds(6);

    private FeedbackView? objView;
    private string objAbout = FeedbackAbout.Chatur;
    private string objSeverity = nameof(FeedbackSeverity.Major);
    private string objTitle = string.Empty;
    private string objDetail = string.Empty;
    private bool objAttach = true;
    private string? objError;
    private bool objIsBusy;
    private bool objIsReading;
    private Timer? objTimer;

    [Inject]
    private IFeedbackActions FeedbackActions { get; set; } = default!;

    [Inject]
    private AppState AppState { get; set; } = default!;

    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    [Inject]
    private ToastService Toast { get; set; } = default!;

    private bool CanSend =>
        !objIsBusy
        && objTitle.Trim().Length >= FeedbackLimits.TitleMin
        && objDetail.Trim().Length >= FeedbackLimits.DetailMin;

    private string CountText => objView is null ? "···" : objView.Entries.Count == 1 ? "1 entry" : $"{objView.Entries.Count} entries";

    private string DestinationName => AppState.CurrentUser?.Email ?? "you";

    private string AppManagerAddress => Configuration["AppManager:BaseUrl"] ?? "App Manager";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        objTimer = new Timer(_ => _ = InvokeAsync(RetryAsync), null, cRetryEvery, cRetryEvery);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        objTimer?.Dispose();
        objTimer = null;
        GC.SuppressFinalize(this);
    }

    private void OnAboutChanged(string? aValue) => objAbout = aValue ?? FeedbackAbout.Chatur;

    private void OnSeverityChanged(string? aValue) => objSeverity = aValue ?? nameof(FeedbackSeverity.Major);

    private void Clear()
    {
        objTitle = string.Empty;
        objDetail = string.Empty;
        objError = null;
    }

    private async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        objIsBusy = true;
        objError = null;
        try
        {
            var vEntry = await FeedbackActions.SendAsync(new FeedbackDraft(objAbout, Enum.Parse<FeedbackSeverity>(objSeverity), objTitle, objDetail, objAttach));
            Toast.Success(vEntry.State == FeedbackState.Waiting
                ? "Kept on this machine — it goes when the network is back."
                : "Raised through App Manager.");
            objTitle = string.Empty;
            objDetail = string.Empty;
            await LoadAsync();
        }
        catch (ArgumentException aEx)
        {
            objError = aEx.Message;
        }
        finally
        {
            objIsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        objIsBusy = true;
        try
        {
            await LoadAsync();
        }
        finally
        {
            objIsBusy = false;
        }
    }

    /// <summary>Only asks while something is waiting, so a settled list makes no calls on its own.</summary>
    private async Task RetryAsync()
    {
        if (objView is { WaitingCount: > 0 } && !objIsBusy)
        {
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        if (objIsReading)
        {
            return;
        }

        objIsReading = true;
        try
        {
            objView = await FeedbackActions.ListAsync();
        }
        catch (Exception aEx) when (aEx is not OperationCanceledException)
        {
            objError = "The list could not be read: " + aEx.Message;
        }
        finally
        {
            objIsReading = false;
            StateHasChanged();
        }
    }

    private static BadgeVariant StateVariant(FeedbackState aState) => aState switch
    {
        FeedbackState.Waiting => BadgeVariant.Warning,
        FeedbackState.Fixed => BadgeVariant.Success,
        _ => BadgeVariant.Secondary
    };

    private static string StateWord(FeedbackState aState) => aState switch
    {
        FeedbackState.Waiting => "waiting to send",
        FeedbackState.Raised => "raised",
        FeedbackState.Answered => "answered",
        _ => "fixed"
    };
}
