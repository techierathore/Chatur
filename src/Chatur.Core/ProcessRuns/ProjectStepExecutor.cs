using Chatur.Core.Actions;
using Chatur.Core.Board;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns.Checks;
using Chatur.Core.Processes;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// The real <see cref="IProcessStepExecutor"/>. <c>verify-phase</c> does its work on the user's
/// project: reads the checklist, runs the project's own build and tests, checks the build left no
/// warning, and shows what the tests proved before the owner is asked to approve (REQ-UI-048). In
/// every other process a work step is handed to the agent that owns the process and a check judges what
/// the agent left on disk (REQ-FN-084..088, REQ-FN-092..102). Without the hand-over seam (a head that
/// has none) those steps say "not carried out" and no check passes on that.
/// </summary>
public sealed class ProjectStepExecutor : IProcessStepExecutor
{
    private const string VerifyPhase = "verify-phase";

    private readonly ProjectFolders objFolders;
    private readonly RunEvidenceStore objStore;
    private readonly VerifyBuildStep objBuild;
    private readonly VerifyTestStep objTests;
    private readonly IClock objClock;
    private readonly IStepAgentActions? objAgents;
    private readonly ProcessRunBaseline? objBaseline;

    /// <summary>Creates the executor.</summary>
    /// <param name="aFolders">Finds the project's folder and checklist.</param>
    /// <param name="aStore">Keeps and reads the run's evidence.</param>
    /// <param name="aRunner">Runs the project's own commands.</param>
    /// <param name="aOptions">Time limits and the output cap.</param>
    /// <param name="aPaths">Where Chatur keeps its own files; each run's result files go under it.</param>
    /// <param name="aClock">The time source.</param>
    /// <param name="aAgents">Hands a work step to the process's agent; <see langword="null"/> leaves work steps not carried out.</param>
    /// <param name="aBaseline">Keeps what the project's files looked like when the run began; <see langword="null"/> leaves checks that compare with it failing.</param>
    public ProjectStepExecutor(
        ProjectFolders aFolders,
        RunEvidenceStore aStore,
        IProjectCommandRunner aRunner,
        ProcessRunOptions aOptions,
        IAppPaths aPaths,
        IClock aClock,
        IStepAgentActions? aAgents = null,
        ProcessRunBaseline? aBaseline = null)
    {
        objAgents = aAgents;
        objBaseline = aBaseline;
        objFolders = aFolders;
        objStore = aStore;
        objBuild = new VerifyBuildStep(aRunner, aStore, aOptions);
        objTests = new VerifyTestStep(aRunner, aStore, aOptions, Path.Combine(aPaths.AppDataFolder, "runs"));
        objClock = aClock;
    }

    /// <inheritdoc />
    public async Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default)
    {
        if (!string.Equals(aProcessCode, VerifyPhase, StringComparison.OrdinalIgnoreCase))
        {
            return await ExecuteHandedAsync(aRunId, aProjectId, aProcessCode, aStep, aCt).ConfigureAwait(false);
        }

        var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false);
        return aStep.StepCode switch
        {
            "s1" => await ReadChecklistAsync(vFolder, aCt).ConfigureAwait(false),
            "s2" => vFolder is null ? NoFolder() : await objBuild.RunAsync(aRunId, aStep.Position, vFolder, aCt).ConfigureAwait(false),
            "s3" => await CheckBuildAsync(aRunId, aCt).ConfigureAwait(false),
            "s4" => vFolder is null ? NoFolder() : await objTests.RunAsync(aRunId, aStep.Position, vFolder, aCt).ConfigureAwait(false),
            "s5" => await PreviewVerdictsAsync(aRunId, vFolder, aCt).ConfigureAwait(false),
            _ => new ProcessStepOutcome(true, string.Empty)
        };
    }

    /// <summary>A step of any process but verify-phase: the work step goes to the agent, then the check judges the disk.</summary>
    private async Task<ProcessStepOutcome> ExecuteHandedAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt)
    {
        var vDefinition = ProcessCatalogue.Find(aProcessCode);
        if (objAgents is null || vDefinition is null)
        {
            return new ProcessStepOutcome(aStep.Kind != ProcessStepKind.Check, "Not carried out: no agent can be handed this step here.");
        }

        var vFolder = await objFolders.FolderAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vFolder is null)
        {
            return NoFolder();
        }

        if (objBaseline is not null)
        {
            await objBaseline.EnsureAsync(aRunId, vFolder, aCt).ConfigureAwait(false);
        }

        var vWorkText = string.Empty;
        if (aStep.Kind == ProcessStepKind.Step && !ProcessChecks.ChaturRunsItself(aProcessCode, aStep.Position))
        {
            var vWork = await objAgents.HandAsync(aRunId, aStep.Position, aCt).ConfigureAwait(false);
            if (vWork.StoppedByOwner)
            {
                return new ProcessStepOutcome(false, "Stopped by the owner.", true);
            }

            if (vWork.ModelName is null)
            {
                return new ProcessStepOutcome(false, $"The {vWork.AgentName} could not work this step: {Shorten(vWork.FinalMessage ?? "no model answered.")}");
            }

            vWorkText = Describe(vWork);
        }

        var vIsCheck = aStep.Kind == ProcessStepKind.Check || vDefinition.HasCheckAt(aStep.Position);
        if (!vIsCheck)
        {
            return new ProcessStepOutcome(true, vWorkText);
        }

        var vCheck = ProcessChecks.Find(aProcessCode, aStep.Position);
        if (vCheck is null)
        {
            return new ProcessStepOutcome(false, $"No check is defined for step {aStep.Position} of {aProcessCode}, so it cannot pass.");
        }

        var vStarted = objBaseline is null ? null : await objBaseline.StartedUtcAsync(aRunId, aCt).ConfigureAwait(false);
        var vContext = new CheckContext(
            aRunId,
            aStep.Position,
            vFolder,
            new ProjectDocuments(vFolder),
            vStarted ?? objClock.UtcNow,
            objBaseline is null ? new Dictionary<string, string>() : await objBaseline.LoadAsync(aRunId, aCt).ConfigureAwait(false),
            objBuild,
            objTests,
            objStore,
            objAgents,
            objClock.UtcNow);
        var vOutcome = await vCheck(vContext, aCt).ConfigureAwait(false);
        return vOutcome.Passed && vWorkText.Length > 0
            ? vOutcome with { Result = vWorkText + " " + vOutcome.Result }
            : vOutcome;
    }

    private static string Describe(ProcessStepWork aWork)
    {
        var vText = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"The {aWork.AgentName} ({aWork.ModelName}) wrote {Count(aWork.FilesWritten.Count, "file")}, ran {Count(aWork.CommandsRun.Count, "command")}, was refused {Count(aWork.Refusals.Count, "time")}");
        if (aWork.FilesProposed.Count > 0)
        {
            vText += $", and left {Count(aWork.FilesProposed.Count, "change")} waiting for approval";
        }

        return vText + (string.IsNullOrWhiteSpace(aWork.FinalMessage) ? "." : ". " + Shorten(aWork.FinalMessage));
    }

    private static string Count(int aCount, string aNoun) => $"{aCount} {aNoun}{(aCount == 1 ? string.Empty : "s")}";

    private static string Shorten(string aText)
    {
        var vOneLine = aText.Replace('\n', ' ').Trim();
        return vOneLine.Length <= 300 ? vOneLine : vOneLine[..297] + "...";
    }

    private static ProcessStepOutcome NoFolder() =>
        new(false, "The project's folder was not found on disk, so nothing could be run.");

    private async Task<ProcessStepOutcome> ReadChecklistAsync(string? aFolder, CancellationToken aCt)
    {
        var vItems = await ReadItemsAsync(aFolder, aCt).ConfigureAwait(false);
        if (vItems is null)
        {
            return new ProcessStepOutcome(true, "The project has no checklist in its docs folder; there is nothing to grade.");
        }

        var vWaiting = vItems.Items.Count(aItem => VerdictPlanner.IsGraded(aItem.FileStatus));
        return new ProcessStepOutcome(true, $"Read {vItems.File}: {vItems.Items.Count} requirements, {vWaiting} waiting for a verdict.");
    }

    private async Task<ProcessStepOutcome> CheckBuildAsync(int aRunId, CancellationToken aCt)
    {
        return await BuildChecks.NoWarningAsync(objStore, aRunId, aCt).ConfigureAwait(false);
    }

    private async Task<ProcessStepOutcome> PreviewVerdictsAsync(int aRunId, string? aFolder, CancellationToken aCt)
    {
        var vItems = await ReadItemsAsync(aFolder, aCt).ConfigureAwait(false);
        if (vItems is null)
        {
            return new ProcessStepOutcome(true, "No checklist, so no requirement will be graded.");
        }

        var vEvidence = await objStore.TestEvidenceAsync(aRunId, aCt).ConfigureAwait(false);
        var vPlan = VerdictPlanner.Plan(vItems.Items, vEvidence, aRunId, "verify-phase", objClock.UtcNow);
        var vProven = vPlan.Count(aVerdict => aVerdict.Status == VerdictPlanner.VerifiedStatus);
        return new ProcessStepOutcome(true, $"{vPlan.Count} requirements will be graded: {vProven} proven by passing tests, {vPlan.Count - vProven} need re-verify.");
    }

    private async Task<ChecklistRead?> ReadItemsAsync(string? aFolder, CancellationToken aCt)
    {
        var vFile = aFolder is null ? null : ProjectFolders.NewestChecklist(aFolder);
        if (aFolder is null || vFile is null)
        {
            return null;
        }

        var vText = await File.ReadAllTextAsync(Path.Combine(aFolder, vFile), aCt).ConfigureAwait(false);
        return new ChecklistRead(vFile, ChecklistDocument.Parse(vText, vFile, ChecklistDocument.PhaseOf(vFile)));
    }

    private sealed record ChecklistRead(string File, IReadOnlyList<RequirementItem> Items);
}
