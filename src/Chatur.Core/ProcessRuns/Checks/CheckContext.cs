using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns.Checks;

/// <summary>Everything a check reads: the run, the project's folder and documents, the snapshot taken when the run began, and the steps Chatur runs itself.</summary>
/// <param name="RunId">The process run.</param>
/// <param name="Position">The 1-based row the check belongs to.</param>
/// <param name="Folder">The project's folder.</param>
/// <param name="Documents">Reads the project's documents.</param>
/// <param name="RunStartedUtc">When the run began; a document "written in the run" was last written at or after this.</param>
/// <param name="Baseline">The project's files when the run began handing steps out; empty when none was taken.</param>
/// <param name="Build">Builds the project, for the checks that judge Chatur's own build.</param>
/// <param name="Tests">Runs the project's tests, for the checks that judge Chatur's own test run.</param>
/// <param name="Store">Keeps and reads the commands and tests the run recorded.</param>
/// <param name="Agents">Tells what an earlier handed step of the run did, or <see langword="null"/> when steps are not handed.</param>
/// <param name="TodayUtc">Today's date, for a hand-off dated that day.</param>
public sealed record CheckContext(
    int RunId,
    int Position,
    string Folder,
    ProjectDocuments Documents,
    DateTime RunStartedUtc,
    IReadOnlyDictionary<string, string> Baseline,
    VerifyBuildStep Build,
    VerifyTestStep Tests,
    RunEvidenceStore Store,
    IStepAgentActions? Agents,
    DateTime TodayUtc);

/// <summary>Builds the outcome of a check from what it found wrong.</summary>
public static class CheckOutcome
{
    private const int MaxListed = 8;

    /// <summary>A passed outcome saying what held.</summary>
    /// <param name="aWhat">What was checked and found right.</param>
    public static ProcessStepOutcome Pass(string aWhat) => new(true, aWhat);

    /// <summary>The check passes when nothing is wrong; otherwise it fails, listing the first problems.</summary>
    /// <param name="aWhat">What the check judges, e.g. <c>the brief names an audience</c>.</param>
    /// <param name="aProblems">What was found wrong.</param>
    /// <param name="aHeld">What to say when nothing is wrong.</param>
    public static ProcessStepOutcome From(string aWhat, IReadOnlyList<string> aProblems, string aHeld)
    {
        if (aProblems.Count == 0)
        {
            return new ProcessStepOutcome(true, aHeld);
        }

        var vListed = string.Join("; ", aProblems.Take(MaxListed));
        var vMore = aProblems.Count > MaxListed ? $" (and {aProblems.Count - MaxListed} more)" : string.Empty;
        return new ProcessStepOutcome(false, $"The check \"{aWhat}\" failed: {vListed}{vMore}.");
    }

    /// <summary>A failed outcome with one reason.</summary>
    /// <param name="aWhat">What the check judges.</param>
    /// <param name="aReason">Why it failed.</param>
    public static ProcessStepOutcome Fail(string aWhat, string aReason) => new(false, $"The check \"{aWhat}\" failed: {aReason}");
}
