using Chatur.Core.Actions;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Agents (mockups/settings-agents.html). Lists the seeded roles (REQ-UI-025), shows a
/// role's rights, commands, rules and history (REQ-UI-026, REQ-UI-029), saves a change
/// (REQ-UI-027) and sets a role's model tier (REQ-UI-028). REQ-UI-030 ("a role may only do what its
/// rights allow") is demonstrated by the guard pipeline at the point a role's request is refused,
/// not by this page — the Rights panel here only shows what each switch already governs.
/// </summary>
public partial class SettingsAgents
{
    /// <summary>Every right the seed data grants or withholds (0002-SeedRoles.sql), with a
    /// screen-friendly label and, where the mockup draws that right, the name its anchors use
    /// (<c>right-reads-code</c>, <c>switch-reads-code</c> …). The Rights panel shows on/off for each;
    /// editing a right is not part of <see cref="IRoleActions.SaveAsync"/>, so the switches only show.</summary>
    private static readonly (string Action, string Label, string? Mock)[] KnownActions =
    [
        ("read-file", "Reads code", "reads-code"),
        ("edit-file", "Changes code", "changes-code"),
        ("run-build", "Runs commands", "runs-commands"),
        ("run-source-control", "Runs source control", null),
        ("mark-verified", "Marks work verified", "marks-verified"),
        ("correct-wording", "Corrects its own wording", null)
    ];

    /// <summary>The views the "Showing" picker offers, in the mockup's order.</summary>
    private static readonly string[] Subtabs = ["wording", "rights", "commands", "rules", "history"];

    private IReadOnlyList<RoleSummary>? objRoles;
    private readonly Dictionary<int, RoleDetail> objDetails = [];
    private IReadOnlyList<RoutingTier> objTiers = [];
    private IReadOnlyList<ModelSummary> objModels = [];
    private int? objSelectedRoleId;
    private RoleDetail? objDetail;
    private string objWordingDraft = string.Empty;
    private string objCommandsDraft = string.Empty;
    private string objRulesDraft = string.Empty;
    private string objTierDraft = "2";
    private string objShow = "wording";
    private bool objSubtabOpen;
    private bool objTierOpen;
    private bool objAddOpen;
    private bool objIsSaving;
    private bool objShowImportPicker;
    private IReadOnlyList<TrBlazeUI.Components.FileUpload.FileUploadItem>? objImportFiles;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        objModels = await ProviderActions.ListModelsAsync();
        objTiers = await RoutingActions.TiersAsync();
        await ReloadAsync();
        if (objRoles is { Count: > 0 })
        {
            await SelectAsync(objRoles[0].RoleId);
        }
    }

    /// <summary>Reads every role and its detail again, so the list shows each one's version and rights.</summary>
    private async Task ReloadAsync()
    {
        objRoles = await RoleActions.RolesAsync();
        objDetails.Clear();
        foreach (var vRole in objRoles)
        {
            objDetails[vRole.RoleId] = await RoleActions.RoleAsync(vRole.RoleId);
        }
    }

    private async Task SelectAsync(int aRoleId)
    {
        objSelectedRoleId = aRoleId;
        objDetail = await RoleActions.RoleAsync(aRoleId);
        LoadDraftFromDetail();
    }

    private void LoadDraftFromDetail()
    {
        if (objDetail is null)
        {
            return;
        }

        objWordingDraft = objDetail.Wording;
        objCommandsDraft = string.Join(Environment.NewLine, objDetail.Commands);
        objRulesDraft = string.Join(Environment.NewLine, objDetail.Rules);
        objTierDraft = objDetail.Summary.Tier.ToString();
    }

    /// <summary>Throws away the draft and shows the saved version again.</summary>
    private void DiscardAsync() => LoadDraftFromDetail();

    /// <summary>Saves the wording, commands and rules as a new version, then the tier if it changed (REQ-UI-027, REQ-UI-028, REQ-UI-029).</summary>
    private async Task SaveAsync()
    {
        if (objSelectedRoleId is not { } vRoleId || objDetail is null)
        {
            return;
        }

        objIsSaving = true;
        try
        {
            var vCommands = SplitLines(objCommandsDraft);
            var vRules = SplitLines(objRulesDraft);

            objDetail = await RoleActions.SaveAsync(vRoleId, objWordingDraft, vCommands, vRules);
            if (int.TryParse(objTierDraft, out var vTier) && vTier != objDetail.Summary.Tier)
            {
                objDetail = await RoleActions.SetTierAsync(vRoleId, vTier);
            }

            await ReloadAsync();
            LoadDraftFromDetail();
            Toast.Success($"{objDetail.Summary.Name} saved as v{VersionOf(vRoleId)}.");
        }
        catch (NotImplementedException)
        {
            Toast.Error("Saving an agent is not wired up yet.");
        }
        catch (ArgumentException aEx)
        {
            Toast.Error(aEx.Message);
        }
        finally
        {
            objIsSaving = false;
        }
    }

    /// <summary>Brings an earlier version's wording back and saves it as the next version, keeping the commands and rules as they are.</summary>
    private async Task RestoreAsync(RoleVersion aVersion)
    {
        if (objSelectedRoleId is not { } vRoleId || objDetail is null)
        {
            return;
        }

        objIsSaving = true;
        try
        {
            objDetail = await RoleActions.SaveAsync(vRoleId, aVersion.Wording, objDetail.Commands, objDetail.Rules);
            await ReloadAsync();
            LoadDraftFromDetail();
            Toast.Success($"{objDetail.Summary.Name} saved as v{VersionOf(vRoleId)}, with the wording of v{aVersion.Version}.");
        }
        catch (ArgumentException aEx)
        {
            Toast.Error(aEx.Message);
        }
        finally
        {
            objIsSaving = false;
        }
    }

    private void OpenAddAgent() => objAddOpen = true;

    /// <summary>Shows a freshly added agent: the list is read again and the agent is chosen.</summary>
    private async Task AgentAddedAsync(int aRoleId)
    {
        await ReloadAsync();
        await SelectAsync(aRoleId);
        Toast.Success($"{objDetail?.Summary.Name} added as v1.");
    }

    private async Task ExportAsync()
    {
        try
        {
            var vJson = await RoleActions.ExportAsync();
            await FileDownload.DownloadTextAsync($"chatur-roles-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json", vJson);
            Toast.Success("Every agent, right, command and rule was written to a file.");
        }
        catch (NotImplementedException)
        {
            Toast.Error("Exporting agents is not wired up yet.");
        }
    }

    /// <summary>Shows or hides the file picker the Import button reveals (REQ-FN-029).</summary>
    private void ToggleImportPicker() => objShowImportPicker = !objShowImportPicker;

    /// <summary>
    /// Reads the chosen export file's text and brings its roles, rights, commands and rules in
    /// (REQ-FN-029) — the file input/dropzone this row's own checklist Remark said was missing.
    /// </summary>
    private async Task HandleImportFilesAsync(IReadOnlyList<TrBlazeUI.Components.FileUpload.FileUploadItem> aFiles)
    {
        objImportFiles = aFiles;
        var vPicked = aFiles.Count > 0 ? aFiles[0] : null;
        if (vPicked is null)
        {
            return;
        }

        try
        {
            const long vMaxFileSize = 5 * 1024 * 1024;
            await using var vStream = vPicked.File.OpenReadStream(vMaxFileSize);
            using var vReader = new StreamReader(vStream);
            var vJson = await vReader.ReadToEndAsync();

            await RoleActions.ImportAsync(vJson);

            await ReloadAsync();
            if (objSelectedRoleId is { } vRoleId)
            {
                objDetail = await RoleActions.RoleAsync(vRoleId);
                LoadDraftFromDetail();
            }

            objShowImportPicker = false;
            objImportFiles = null;
            Toast.Success("Roles, rights, commands and rules were imported.");
        }
        catch (FormatException aEx)
        {
            Toast.Error(aEx.Message);
        }
        catch (NotImplementedException)
        {
            Toast.Error("Importing agents is not wired up yet.");
        }
    }

    private void ChooseSubtab(string aSubtab)
    {
        objShow = aSubtab;
        objSubtabOpen = false;
    }

    private void ChooseTier(int aTier)
    {
        objTierDraft = aTier.ToString();
        objTierOpen = false;
    }

    /// <summary>The version a role is at: the newest row of its history (the seed writes version 1).</summary>
    private int VersionOf(int aRoleId) =>
        objDetails.TryGetValue(aRoleId, out var vDetail) && vDetail.History.Count > 0 ? vDetail.History.Max(h => h.Version) : 1;

    /// <summary>The pills under a role's name in the list — what it may do, in the mockup's words.</summary>
    private IEnumerable<string> PillsOf(int aRoleId)
    {
        if (!objDetails.TryGetValue(aRoleId, out var vDetail))
        {
            yield break;
        }

        if (vDetail.Rights.Contains("read-file"))
        {
            yield return "reads code";
        }

        if (vDetail.Rights.Contains("edit-file"))
        {
            yield return "changes code";
        }

        if (vDetail.Rights.Contains("run-build"))
        {
            yield return "runs commands";
        }

        if (vDetail.Rights.Contains("mark-verified"))
        {
            yield return "marks verified";
        }
    }

    /// <summary>The anchor the mockup gives a role's list entry: the code with its hyphens left out (<c>flowmaster</c>).</summary>
    private static string MockupId(string aCode) => aCode.Replace("-", string.Empty, StringComparison.Ordinal);

    /// <summary>Where the mockup's own id for a list entry differs from the role's code, a box around the button carries it; otherwise none.</summary>
    private static string? WrapperTestId(string aCode) => MockupId(aCode) == aCode ? null : $"agent-{MockupId(aCode)}";

    /// <summary>Only a role's "reads code" pill has an anchor in the mockup (<c>analyst-right-read</c>).</summary>
    private static string? PillTestId(string aMockupId, string aPill) => aPill == "reads code" ? $"{aMockupId}-right-read" : null;

    private string SavedLine()
    {
        if (objDetail is null)
        {
            return string.Empty;
        }

        var vNewest = objDetail.History.OrderByDescending(h => h.Version).FirstOrDefault();
        var vSaved = vNewest is null ? string.Empty : $" Saved {vNewest.ValidFromUtc.ToLocalTime():d MMMM yyyy}.";
        return $"{objDetail.Summary.Name} is at version {VersionOf(objDetail.Summary.RoleId)}.{vSaved} A change is saved as the next version and the earlier ones stay in the history.";
    }

    private string RightTitle(string aAction) =>
        $"{objDetail?.Summary.Name} " + aAction switch
        {
            "read-file" => "may read every file in the project.",
            "edit-file" => "may edit files only if this is on.",
            "run-build" => "may run builds and tests only if this is on.",
            "run-source-control" => "never runs source control on its own.",
            "mark-verified" => "may write a verdict only if this is on — only the Verifier may.",
            _ => "may correct its own wording only if this is on."
        };

    private string ChangesCodeNote(bool aAllowed) =>
        aAllowed
            ? $"{objDetail?.Summary.Name} may change code; an edit is held for your approval unless the session is on go ahead."
            : $"{objDetail?.Summary.Name} reads code to understand it, but cannot change it. A change it would make is carried out by an agent that may.";

    private static string RestoreTitle(RoleVersion aVersion) => $"Brings back the wording of v{aVersion.Version} and saves it as the next version.";

    private static string SubtabLabel(string aSubtab) =>
        aSubtab switch
        {
            "wording" => "Wording",
            "rights" => "Rights",
            "commands" => "Commands",
            "rules" => "Rules",
            _ => "History"
        };

    /// <summary>The first model of the chosen tier's fallback chain, as Settings ▸ Routing orders it.</summary>
    private string FirstInChain()
    {
        var vChain = objTiers.FirstOrDefault(t => t.Tier.ToString() == objTierDraft)?.ModelIdsInOrder;
        var vFirst = vChain is { Count: > 0 } ? objModels.FirstOrDefault(m => m.ModelId == vChain[0]) : null;
        return vFirst is null ? "No model is in that chain yet" : $"First in that chain: {vFirst.Identifier}";
    }

    private static IReadOnlyList<string> SplitLines(string aText) =>
        aText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string TierLabel(int aTier) =>
        aTier switch
        {
            1 => "Tier 1 — the strongest",
            2 => "Tier 2 — everyday",
            _ => "Tier 3 — on this machine"
        };
}
