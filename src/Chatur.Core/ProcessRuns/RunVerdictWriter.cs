using System.Globalization;
using System.Text;
using Chatur.Core.Actions;
using Chatur.Core.Board;
using Chatur.Core.Data;
using Chatur.Core.Platform;
using Chatur.Core.Processes;
using Dapper;

namespace Chatur.Core.ProcessRuns;

/// <summary>Writes a verifying run's verdicts into the project's checklist at the process's write step (REQ-UI-048).</summary>
public interface IRunVerdictWriter
{
    /// <summary>
    /// Grades the requirements the run can grade and writes each verdict into the project's own
    /// checklist, through <see cref="IProcessActions.WriteDocumentAsync"/>, recording each on the run.
    /// </summary>
    /// <param name="aRun">The run, with every step before the write step finished.</param>
    /// <param name="aDefinition">The process being run.</param>
    /// <param name="aCt">A token that cancels the write.</param>
    /// <returns>What to show against the write step.</returns>
    Task<string> WriteAsync(ProcessRunView aRun, ProcessDefinition aDefinition, CancellationToken aCt = default);
}

/// <summary>
/// <see cref="IRunVerdictWriter"/> over the newest-phase checklist in the project's docs folder.
/// Nothing is written unless the owner approved at the owner stop before the write step. Each
/// requirement waiting for a verdict (status <c>Implemented</c> or <c>Needs re-verify</c>, a recorded
/// defect included) is graded by <see cref="VerdictPlanner"/> from the tests the run really ran:
/// <c>Verified</c> only when a test carrying its id ran and every such test passed, otherwise
/// <c>Needs re-verify</c> with the reason. Every other row is left as it is.
/// </summary>
public sealed class RunVerdictWriter : IRunVerdictWriter
{
    private readonly IDbConnectionFactory objConnections;
    private readonly IProcessActions objProcesses;
    private readonly IClock objClock;
    private readonly ProjectFolders objFolders;
    private readonly RunEvidenceStore objEvidence;

    /// <summary>Creates the writer.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    /// <param name="aProcesses">Writes the checklist, the only way a process writes a project document.</param>
    /// <param name="aClock">The time source, for the remark's date.</param>
    /// <param name="aFolders">Finds the project's folder and newest checklist.</param>
    /// <param name="aEvidence">Reads what the run's tests showed.</param>
    public RunVerdictWriter(
        IDbConnectionFactory aConnections,
        IProcessActions aProcesses,
        IClock aClock,
        ProjectFolders aFolders,
        RunEvidenceStore aEvidence)
    {
        objConnections = aConnections;
        objProcesses = aProcesses;
        objClock = aClock;
        objFolders = aFolders;
        objEvidence = aEvidence;
    }

    /// <inheritdoc />
    public async Task<string> WriteAsync(ProcessRunView aRun, ProcessDefinition aDefinition, CancellationToken aCt = default)
    {
        var vApproval = aRun.Steps
            .Take(aDefinition.VerdictStep - 1)
            .LastOrDefault(aStep => aStep.Kind == ProcessStepKind.OwnerStop);
        if (vApproval is not null && !string.Equals(vApproval.Answer, nameof(ProcessAnswer.Yes), StringComparison.Ordinal))
        {
            await ClearAsync(aRun.RunId, aCt).ConfigureAwait(false);
            return "You did not approve the verdicts, so none was written. The requirements stay open.";
        }

        var vFolder = await objFolders.FolderAsync(aRun.ProjectId, aCt).ConfigureAwait(false);
        var vFile = vFolder is null ? null : ProjectFolders.NewestChecklist(vFolder);
        if (vFolder is null || vFile is null)
        {
            await ClearAsync(aRun.RunId, aCt).ConfigureAwait(false);
            return "The project has no checklist in its docs folder, so no verdict was written.";
        }

        return await GradeAsync(aRun, vFolder, vFile, aCt).ConfigureAwait(false);
    }

    private async Task<string> GradeAsync(ProcessRunView aRun, string aFolder, string aFile, CancellationToken aCt)
    {
        var vFullPath = Path.Combine(aFolder, aFile);
        var vHasBom = HasBom(vFullPath);
        var vText = await File.ReadAllTextAsync(vFullPath, aCt).ConfigureAwait(false);
        var vEvidence = await objEvidence.TestEvidenceAsync(aRun.RunId, aCt).ConfigureAwait(false);
        var vPlan = VerdictPlanner.Plan(ChecklistDocument.Parse(vText, aFile, ChecklistDocument.PhaseOf(aFile)), vEvidence, aRun.RunId, aRun.ProcessCode, objClock.UtcNow);
        foreach (var vVerdict in vPlan)
        {
            vText = ChecklistDocument.WithVerdict(vText, vVerdict.RequirementId, vVerdict.Status, vVerdict.Percent, vVerdict.Remark);
        }

        if (vPlan.Count > 0)
        {
            await objProcesses.WriteDocumentAsync(aRun.ProjectId, aFile, (vHasBom ? "\uFEFF" : string.Empty) + vText, aCt).ConfigureAwait(false);
        }

        await RecordAsync(aRun.RunId, aFile, vPlan, aCt).ConfigureAwait(false);
        var vVerified = vPlan.Count(aVerdict => aVerdict.Status == VerdictPlanner.VerifiedStatus);
        return vPlan.Count == 0
            ? $"No requirement in {aFile} was waiting for a verdict, so none was written."
            : $"Wrote {vPlan.Count} verdict{(vPlan.Count == 1 ? string.Empty : "s")} into {aFile}: {vVerified} Verified by passing tests, {vPlan.Count - vVerified} Needs re-verify.";
    }

    private static bool HasBom(string aPath)
    {
        using var vStream = File.OpenRead(aPath);
        var vHead = new byte[3];
        return vStream.Read(vHead, 0, 3) == 3 && vHead.AsSpan().SequenceEqual(Encoding.UTF8.GetPreamble());
    }

    private async Task ClearAsync(int aRunId, CancellationToken aCt) =>
        await RecordAsync(aRunId, string.Empty, [], aCt).ConfigureAwait(false);

    private async Task RecordAsync(int aRunId, string aFile, IReadOnlyList<PlannedVerdict> aVerdicts, CancellationToken aCt)
    {
        using var vConnection = objConnections.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM ProcessRunVerdict WHERE RunId = @aRunId",
            new { aRunId },
            cancellationToken: aCt)).ConfigureAwait(false);
        foreach (var vVerdict in aVerdicts)
        {
            await vConnection.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO ProcessRunVerdict (RunId, RequirementId, ChecklistFile, Status, Percent, Remark, WrittenUtc)
                VALUES (@aRunId, @RequirementId, @ChecklistFile, @Status, @Percent, @Remark, @vNow)
                """,
                new
                {
                    aRunId,
                    vVerdict.RequirementId,
                    ChecklistFile = aFile,
                    vVerdict.Status,
                    vVerdict.Percent,
                    vVerdict.Remark,
                    vNow = objClock.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                },
                cancellationToken: aCt)).ConfigureAwait(false);
        }
    }
}
