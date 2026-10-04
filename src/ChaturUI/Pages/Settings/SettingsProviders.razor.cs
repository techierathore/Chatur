using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Model providers (mockups/settings-providers.html). Lists the connected providers,
/// tests one (REQ-UI-019) and removes one (REQ-UI-020). <see cref="IProviderActions"/> itself is
/// cluster K's; until it is wired up this page falls back to the honest "no providers yet" empty
/// state rather than crashing on <see cref="NotImplementedException"/>.
/// </summary>
public partial class SettingsProviders
{
    private IReadOnlyList<ModelProvider>? objProviders;
    private IReadOnlyList<ModelSummary> objModels = [];
    private readonly Dictionary<int, ProviderTestResult?> objTestResults = new();
    private readonly HashSet<int> testingIds = new();
    private bool objAddOpen;

    private void OpenAddDialog() => objAddOpen = true;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            objProviders = await ProviderActions.ListAsync();
            objModels = await ProviderActions.ListModelsAsync();
        }
        catch (NotImplementedException)
        {
            objProviders = null;
            objModels = [];
        }
    }

    /// <summary>The models a provider serves, for its row's Models cell (UI Design "Screen: Settings", <c>providers-table</c>).</summary>
    /// <param name="aProvider">The provider whose models to list.</param>
    private IReadOnlyList<ModelSummary> ModelsOf(ModelProvider aProvider) =>
        objModels.Where(m => m.ProviderId == aProvider.ProviderId).ToList();

    /// <summary>Calls the provider once and reports what came back (REQ-UI-019).</summary>
    private async Task TestAsync(ModelProvider aProvider)
    {
        testingIds.Add(aProvider.ProviderId);
        StateHasChanged();
        try
        {
            var vResult = await ProviderActions.TestAsync(aProvider.ProviderId);
            objTestResults[aProvider.ProviderId] = vResult;
            if (vResult.Succeeded)
            {
                Toast.Success($"{aProvider.Name} answered.");
            }
            else
            {
                Toast.Error(vResult.Message ?? $"{aProvider.Name} did not answer.");
            }
        }
        catch (NotImplementedException)
        {
            Toast.Error("Testing a provider is not wired up yet.");
        }
        finally
        {
            testingIds.Remove(aProvider.ProviderId);
            await LoadAsync();
        }
    }

    /// <summary>Removes a provider; its secret is deleted from the store along with it (REQ-UI-020).</summary>
    private async Task RemoveAsync(ModelProvider aProvider)
    {
        try
        {
            await ProviderActions.RemoveAsync(aProvider.ProviderId);
            Toast.Success($"{aProvider.Name} removed.");
            await LoadAsync();
        }
        catch (NotImplementedException)
        {
            Toast.Error("Removing a provider is not wired up yet.");
        }
    }

    /// <summary>The names under a provider's model count: the first few, then how many more there are, so a provider with dozens of models keeps its row to a few lines.</summary>
    /// <param name="aModels">The provider's models.</param>
    private static string NamesOf(IReadOnlyList<ModelSummary> aModels)
    {
        if (aModels.Count == 0)
        {
            return "None until it has been tested";
        }

        var vShown = string.Join(" · ", aModels.Take(MaxModelNames).Select(m => m.Identifier));
        return aModels.Count > MaxModelNames ? $"{vShown} · +{aModels.Count - MaxModelNames} more" : vShown;
    }

    private const int MaxModelNames = 3;

    private static string Key(ModelProvider aProvider) => aProvider.Name.ToLowerInvariant().Replace(' ', '-');

    private static BadgeVariant StateVariant(ProviderState aState) =>
        aState switch
        {
            ProviderState.Connected => BadgeVariant.Success,
            ProviderState.Failed => BadgeVariant.Destructive,
            _ => BadgeVariant.Warning
        };

    private static string StateLabel(ProviderState aState) =>
        aState switch
        {
            ProviderState.Connected => "connected",
            ProviderState.Failed => "failed",
            _ => "not tested yet"
        };
}
