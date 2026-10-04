using Chatur.Core.Actions;
using TrBlazeUI.Components.Badge;
using TrBlazeUI.Components.DiffView;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Corrections (mockups/settings-corrections.html). Lists every correction
/// (REQ-UI-034), and calls into <see cref="ICorrectionActions"/>'s keep/undo/export-seed for
/// REQ-FN-037 through REQ-FN-039 once cluster K has wired them up.
/// </summary>
public partial class SettingsCorrections
{
    private IReadOnlyList<Correction>? objCorrections;
    private IReadOnlyCollection<Correction> objSelected = Array.Empty<Correction>();

    private Correction? SelectedCorrection => objSelected.FirstOrDefault() ?? objCorrections?.FirstOrDefault();

    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        objCorrections = await CorrectionActions.ListAsync();
    }

    private async Task KeepAsync(Correction aCorrection)
    {
        try
        {
            await CorrectionActions.KeepAsync(aCorrection.CorrectionId);
            Toast.Success($"\"{aCorrection.Target}\" kept.");
            await LoadAsync();
        }
        catch (NotImplementedException)
        {
            Toast.Error("Keeping a correction is not wired up yet.");
        }
    }

    private async Task UndoAsync(Correction aCorrection)
    {
        try
        {
            await CorrectionActions.UndoAsync(aCorrection.CorrectionId);
            Toast.Success($"\"{aCorrection.Target}\" undone.");
            await LoadAsync();
        }
        catch (NotImplementedException)
        {
            Toast.Error("Undoing a correction is not wired up yet.");
        }
    }

    private async Task ExportSeedAsync()
    {
        try
        {
            var vJson = await CorrectionActions.ExportSeedAsync();
            await FileDownload.DownloadTextAsync($"chatur-corrections-seed-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json", vJson);
            Toast.Success("Every kept correction was written to the seed file.");
        }
        catch (NotImplementedException)
        {
            Toast.Error("Exporting the seed data is not wired up yet.");
        }
    }

    /// <summary>Copies the chosen correction's change as plain text, one line per changed line with a leading minus or plus.</summary>
    private async Task CopyDiffAsync()
    {
        if (SelectedCorrection is not { } vCorrection)
        {
            return;
        }

        var vLines = TextDiff.Compare(vCorrection.Before, vCorrection.After)
            .Where(l => l.Kind != DiffLineKind.Unchanged)
            .Select(l => (l.Kind == DiffLineKind.Added ? "+ " : "- ") + l.Text);
        if (await Browser.CopyTextAsync(string.Join('\n', vLines)))
        {
            Toast.Success("Copied the change.");
        }
        else
        {
            Toast.Error("Could not copy to the clipboard.");
        }
    }

    /// <summary>What kind of wording a correction changed: a rule, a process step, or (a bare role code) an agent.</summary>
    private static string KindOf(Correction aCorrection) =>
        aCorrection.Target.StartsWith("rule:", StringComparison.Ordinal) ? "rule"
        : aCorrection.Target.StartsWith("step:", StringComparison.Ordinal) ? "step"
        : "agent";

    private static BadgeVariant StatusVariant(string aStatus) =>
        aStatus switch
        {
            "Kept" => BadgeVariant.Success,
            "Undone" => BadgeVariant.Secondary,
            _ => BadgeVariant.Warning
        };
}
