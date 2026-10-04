using Chatur.Core.Actions;

namespace ChaturUI.Pages.Settings;

/// <summary>
/// Settings ▸ Measurements (mockups/settings-measurements.html). Reads
/// <see cref="IMeasurementActions.StreamsAsync"/> for the selected project and shows each of the
/// five streams' record count, newest record and whether its last write failed (REQ-UI-035,
/// REQ-UI-036).
/// </summary>
public partial class SettingsMeasurements
{
    private IReadOnlyList<MeasurementStream>? objStreams;
    private MeasurementFolder? objFolder;
    private bool objIsLoading;


    /// <inheritdoc />
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        if (State.SelectedProject is null)
        {
            objStreams = null;
            objFolder = null;
            return;
        }

        objIsLoading = true;
        try
        {
            var vProjectId = State.SelectedProject.ProjectId;
            objStreams = await MeasurementActions.StreamsAsync(vProjectId);
            objFolder = await MeasurementActions.FolderAsync(vProjectId);
        }
        finally
        {
            objIsLoading = false;
        }
    }

    /// <summary>The count's colour: the Misses tile is always drawn in the warning colour, as the mockup does.</summary>
    private static string TileCountClass(MeasurementStream aStream) =>
        aStream.Name == "misses.jsonl" ? "text-lg font-semibold text-alert-warning" : "text-lg font-semibold";

    /// <summary>A <c>data-testid</c> for each stream's row, so a check can find the row by stream.</summary>
    private static IReadOnlyDictionary<string, object>? RowHooks(MeasurementStream aStream) =>
        new Dictionary<string, object> { ["data-testid"] = $"stream-{StreamKey(aStream.Name)}", ["class"] = "border-t" };

    private string FailedStreamsText()
    {
        var vNames = (objStreams ?? []).Where(aStream => aStream.LastWriteFailed).Select(aStream => aStream.Name).ToList();
        return vNames.Count == 1 ? $"to {vNames[0]}" : $"to {string.Join(", ", vNames)}";
    }

    private async Task CopyFolderAsync()
    {
        if (objFolder is not null)
        {
            await CopyAsync(objFolder.FolderPath, "Copied the folder.");
        }
    }

    private async Task CopyRecordAsync()
    {
        if (objFolder?.NewestRunRecord is { } vRecord)
        {
            await CopyAsync(vRecord, "Copied the record.");
        }
    }

    private async Task CopyAsync(string aText, string aMessage)
    {
        if (await Browser.CopyTextAsync(aText))
        {
            Toast.Success(aMessage);
        }
        else
        {
            Toast.Error("Could not copy to the clipboard.");
        }
    }

    /// <summary>A display name for a stream, from its file name (REQ-UI-036).</summary>
    private static string Label(string aStreamName) =>
        aStreamName switch
        {
            "runs.jsonl" => "Runs",
            "gates.jsonl" => "Gates",
            "misses.jsonl" => "Misses",
            "sessions.jsonl" => "Sessions",
            "commits.jsonl" => "Commits",
            _ => aStreamName
        };

    /// <summary>A short key for <c>data-testid</c> anchors, without the dot the mockup's ids avoid.</summary>
    private static string StreamKey(string aStreamName) => Label(aStreamName).ToLowerInvariant();
}
