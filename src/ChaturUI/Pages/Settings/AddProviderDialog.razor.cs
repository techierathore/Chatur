using Chatur.Core.Actions;
using Chatur.Core.Platform;
using ChaturUI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TechieRag.Llm;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// The "Add a provider" dialog on Settings ▸ Model providers (REQ-FN-014, REQ-FN-015, REQ-UI-019;
/// mockups/settings-providers.html). Test calls the address once without saving anything; Add
/// files the provider (a pasted key goes to the secret store, a local model needs none) and
/// reports any failure inside the dialog rather than throwing.
/// </summary>
public partial class AddProviderDialog
{
    private string objName = string.Empty;
    private string objConnector = "OpenAICompatible";
    private string objSignIn = "key";
    private string objAddress = string.Empty;
    private string objKey = string.Empty;
    private ProviderTestResult? objTestResult;
    private string? objErrorMessage;
    private bool objIsBusy;
    private bool objIsTesting;
    private bool objWasOpen;
    private SignInCodePrompt? objPrompt;
    private SubscriptionTermsInfo? objTerms;
    private bool objIsStartingSignIn;
    private bool objCopied;
    private CancellationTokenSource? objSignInCts;

    /// <summary>Whether the dialog is showing.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Raised when the dialog opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised after a provider was added, so the table can reload.</summary>
    [Parameter]
    public EventCallback OnAdded { get; set; }

    private string ProviderName
    {
        get => objName;
        set => objName = value ?? string.Empty;
    }

    private string Address
    {
        get => objAddress;
        set => objAddress = value ?? string.Empty;
    }

    private string PastedKey
    {
        get => objKey;
        set => objKey = value ?? string.Empty;
    }

    /// <summary>The connector value sent to Core.</summary>
    private string Connector
    {
        get => objConnector;
        set
        {
            if (string.IsNullOrEmpty(value) || value == objConnector)
            {
                return;
            }

            objConnector = value;
            objAddress = DefaultAddress(value);
            if (value == "Ollama")
            {
                objSignIn = "none";
            }
            else if (objSignIn == "none" || (objSignIn == "browser" && value != "OpenAI"))
            {
                objSignIn = "key";
            }

            RefreshTerms();

            objTestResult = null;
            objErrorMessage = null;
        }
    }

    private string SignIn
    {
        get => objSignIn;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                objSignIn = value;
                RefreshTerms();
                objTestResult = null;
                objErrorMessage = null;
            }
        }
    }

    [Inject]
    private BrowserInterop Browser { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    private bool CanAdd => objSignIn != "browser" || (objConnector == "OpenAI" && objTerms is { Permitted: true });

    private string PromptHost => objPrompt is null ? string.Empty : new Uri(objPrompt.VerificationUrl).Host;

    private int MinutesLeft => objPrompt is null
        ? 0
        : Math.Max(1, (int)Math.Ceiling((objPrompt.ExpiresAt - new DateTimeOffset(Clock.UtcNow, TimeSpan.Zero)).TotalMinutes));

    private static string SecretStoreName => OperatingSystem.IsWindows() ? "Credential Manager" : "the Keychain";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Open && !objWasOpen)
        {
            Reset();
        }

        objWasOpen = Open;
    }

    private void Reset()
    {
        objName = string.Empty;
        objConnector = "OpenAICompatible";
        objSignIn = "key";
        objAddress = DefaultAddress(objConnector);
        objKey = string.Empty;
        objTestResult = null;
        objErrorMessage = null;
        objIsBusy = false;
        objIsTesting = false;
        objPrompt = null;
        objTerms = null;
        objIsStartingSignIn = false;
        objCopied = false;
    }

    private void RefreshTerms() =>
        objTerms = objSignIn == "browser" && objConnector == "OpenAI"
            ? ProviderActions.SubscriptionTerms(ChatGptConnector)
            : null;

    private static string DefaultAddress(string aConnector) =>
        aConnector switch
        {
            "Anthropic" => "https://api.anthropic.com",
            "OpenAI" => "https://api.openai.com/v1",
            "Google" => "https://generativelanguage.googleapis.com",
            "Ollama" => "http://localhost:11434",
            _ => string.Empty
        };

    private static string ConnectorLabel(string aConnector) =>
        aConnector switch
        {
            "OpenAICompatible" => "OpenAI-compatible",
            _ => aConnector
        };

    private static string SignInLabel(string aSignIn) =>
        aSignIn switch
        {
            "browser" => "Sign in with the browser",
            "none" => "No sign-in",
            _ => "Pasted key"
        };

    private const string ChatGptConnector = Chatur.Core.Models.ProviderActions.ChatGptSubscriptionConnector;

    private string? KeyOrNull() => SignIn == "key" && !string.IsNullOrEmpty(objKey) ? objKey : null;

    private async Task TestAsync()
    {
        objErrorMessage = null;
        objTestResult = null;
        objIsBusy = true;
        objIsTesting = true;
        StateHasChanged();
        try
        {
            objTestResult = await ProviderActions.TestUnsavedAsync(objConnector, objAddress.Trim(), KeyOrNull());
        }
        catch (Exception vException) when (vException is not OperationCanceledException)
        {
            objTestResult = new ProviderTestResult(false, vException.Message, Array.Empty<string>());
        }
        finally
        {
            objIsBusy = false;
            objIsTesting = false;
        }
    }

    private async Task AddAsync()
    {
        objErrorMessage = null;
        objIsBusy = true;
        StateHasChanged();
        try
        {
            var vName = objName.Trim();
            var vAddress = objAddress.Trim();
            var vProvider = SignIn == "browser"
                ? await AddSubscriptionAsync(vName)
                : SignIn == "key"
                ? await ProviderActions.AddWithKeyAsync(vName, objConnector, vAddress, objKey)
                : await ProviderActions.AddLocalAsync(vName, vAddress);
            Toast.Success($"{vProvider.Name} added.");
            await CloseAsync();
            await OnAdded.InvokeAsync();
        }
        catch (OperationCanceledException)
        {
            // Cancel pressed while waiting for the sign-in: nothing was saved, the dialog is closing.
        }
        catch (SubscriptionSignInException vException)
        {
            objErrorMessage = SignInMessage(vException);
            objPrompt = null;
        }
        catch (Exception vException)
        {
            objErrorMessage = vException.Message;
        }
        finally
        {
            objIsBusy = false;
            objIsStartingSignIn = false;
        }
    }

    /// <summary>Starts the sign-in and stays open until the owner has authorised the code (REQ-FN-016).</summary>
    private async Task<ModelProvider> AddSubscriptionAsync(string aName)
    {
        objSignInCts?.Cancel();
        objSignInCts = new CancellationTokenSource();
        objIsStartingSignIn = true;
        objPrompt = null;
        objCopied = false;
        await InvokeAsync(StateHasChanged);
        return await ProviderActions.AddSubscriptionAsync(aName, ChatGptConnector, ShowCodeAsync, objSignInCts.Token);
    }

    private async Task ShowCodeAsync(SignInCodePrompt aPrompt, CancellationToken aCt)
    {
        objPrompt = aPrompt;
        objIsStartingSignIn = false;
        await InvokeAsync(StateHasChanged);
        await OpenBrowserAsync();
    }

    private async Task OpenBrowserAsync()
    {
        if (objPrompt is not null)
        {
            try
            {
                await Browser.OpenPageAsync(objPrompt.VerificationUrl);
            }
            catch (JSException)
            {
                // The button stays: the owner can open the page from it, or type the address shown.
            }
        }
    }

    private async Task CopyCodeAsync()
    {
        if (objPrompt is not null)
        {
            objCopied = await Browser.CopyTextAsync(objPrompt.UserCode);
        }
    }

    private static string SignInMessage(SubscriptionSignInException aException) =>
        aException.Code switch
        {
            SubscriptionSignInException.CodeExpired => "The sign-in code ran out before you finished. Press Add to get a new one.",
            SubscriptionSignInException.CodeRejected => "OpenAI turned the sign-in down. Check that you signed in to the right ChatGPT account and try again.",
            SubscriptionSignInException.CodeSessionRejected => "OpenAI no longer accepts the saved sign-in. Press Add to sign in again.",
            SubscriptionSignInException.CodeNotPermitted => "OpenAI does not permit a ChatGPT subscription to be used this way, so it cannot be added.",
            _ => aException.Message
        };

    private async Task CancelAsync() => await CloseAsync();

    private async Task HandleOpenChangedAsync(bool aOpen)
    {
        if (!aOpen)
        {
            objSignInCts?.Cancel();
            objKey = string.Empty;
        }

        await OpenChanged.InvokeAsync(aOpen);
    }

    private async Task CloseAsync()
    {
        await HandleOpenChangedAsync(false);
    }
}
