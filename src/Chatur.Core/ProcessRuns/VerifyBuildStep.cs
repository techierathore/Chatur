using System.Globalization;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.BuildRun;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// The verifying run's build step: runs the project's own build (<c>dotnet build</c> for a .NET
/// project, <c>npm run build</c> for a node project with a build script) and records each command's
/// exit code, warnings and log tail on the run. A build that did not succeed fails the step, which stops
/// the run (REQ-FN-053, REQ-FN-055).
/// </summary>
public sealed partial class VerifyBuildStep
{
    private const int FailureTailLines = 8;
    private const int FailureTailChars = 1200;

    private readonly IProjectCommandRunner objRunner;
    private readonly RunEvidenceStore objStore;
    private readonly ProcessRunOptions objOptions;

    /// <summary>Creates the step.</summary>
    /// <param name="aRunner">Runs the build commands.</param>
    /// <param name="aStore">Keeps what the commands came to.</param>
    /// <param name="aOptions">The time limit and output cap.</param>
    public VerifyBuildStep(IProjectCommandRunner aRunner, RunEvidenceStore aStore, ProcessRunOptions aOptions)
    {
        objRunner = aRunner;
        objStore = aStore;
        objOptions = aOptions;
    }

    [GeneratedRegex(@"(\d+)\s+Warning\(s\)")]
    private static partial Regex WarningCount();

    /// <summary>Builds the project and records what came of it.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aPosition">The step's position.</param>
    /// <param name="aFolder">The project's folder.</param>
    /// <param name="aCt">A token that cancels the build and stops its process tree.</param>
    /// <returns>A passed outcome when every build command succeeded or the project has none; otherwise a failed one naming the command, exit code and the end of its output.</returns>
    public async Task<ProcessStepOutcome> RunAsync(int aRunId, int aPosition, string aFolder, CancellationToken aCt)
    {
        await objStore.ClearAsync(aRunId, RunCommandKind.Build, aCt).ConfigureAwait(false);
        var vCommands = CommandsFor(aFolder);
        if (vCommands.Count == 0)
        {
            return new ProcessStepOutcome(true, "Nothing to build: no .NET solution or project and no package.json build script was found.");
        }

        var vSummaries = new List<string>();
        foreach (var vCommand in vCommands)
        {
            var vResult = await objRunner.RunAsync(vCommand, aCt).ConfigureAwait(false);
            var vRecord = ToRecord(vCommand, vResult);
            await objStore.SaveCommandAsync(aRunId, aPosition, vRecord, aCt).ConfigureAwait(false);
            if (vRecord.Outcome != RunCommandOutcome.Succeeded)
            {
                return new ProcessStepOutcome(false, FailureText(vRecord));
            }

            vSummaries.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{vRecord.Command} exit code 0 in {RunText.Span(vRecord.DurationMs)}{(vRecord.Warnings is { } vWarnings ? $", {vWarnings} warning{(vWarnings == 1 ? string.Empty : "s")}" : string.Empty)}, limit {RunText.Span(vRecord.TimeoutMs)}"));
        }

        return new ProcessStepOutcome(true, "Build passed: " + string.Join("; ", vSummaries) + ".");
    }

    private List<ProjectCommand> CommandsFor(string aFolder)
    {
        var vCommands = new List<ProjectCommand>();
        if (ProjectToolchain.DotNetBuildTarget(aFolder) is { } vTarget)
        {
            // --no-incremental: an up-to-date project skips the compiler and reports 0 warnings even when its code has
            // some (an agent's own build of the same sources leaves it up to date), so the "no warning" check would pass on one.
            vCommands.Add(new ProjectCommand(
                ProjectToolchain.DotNetExecutable(),
                $"build \"{vTarget}\" --nologo --no-incremental -tl:off -nodeReuse:false -p:UseSharedCompilation=false",
                aFolder,
                objOptions.BuildTimeout,
                objOptions.MaxOutputChars));
        }

        if (ProjectToolchain.HasNodeBuildScript(aFolder))
        {
            vCommands.Add(new ProjectCommand(ProjectToolchain.NpmExecutable(), "run build", aFolder, objOptions.BuildTimeout, objOptions.MaxOutputChars));
        }

        return vCommands;
    }

    private static RunCommandRecord ToRecord(ProjectCommand aCommand, ProjectCommandResult aResult)
    {
        var vOutcome = !aResult.Started ? RunCommandOutcome.NotRun
            : aResult.TimedOut ? RunCommandOutcome.TimedOut
            : aResult.ExitCode == 0 ? RunCommandOutcome.Succeeded
            : RunCommandOutcome.Failed;
        var vMatches = WarningCount().Matches(aResult.Tail);
        int? vWarnings = vMatches.Count > 0 ? int.Parse(vMatches[^1].Groups[1].Value, CultureInfo.InvariantCulture) : null;
        var vReason = vOutcome switch
        {
            RunCommandOutcome.NotRun => aResult.Error,
            RunCommandOutcome.TimedOut => $"{aCommand.Display} ran past its limit of {RunText.Span((long)aCommand.Timeout.TotalMilliseconds)} and was stopped.",
            RunCommandOutcome.Failed => $"{aCommand.Display} exited with code {aResult.ExitCode}.",
            _ => null
        };
        return new RunCommandRecord(
            RunCommandKind.Build,
            aCommand.Display,
            vOutcome,
            aResult.Started && !aResult.TimedOut ? aResult.ExitCode : null,
            vWarnings,
            (long)aResult.Elapsed.TotalMilliseconds,
            (long)aCommand.Timeout.TotalMilliseconds,
            aResult.Tail,
            vReason);
    }

    private static string FailureText(RunCommandRecord aRecord)
    {
        var vTail = RunText.Tail(aRecord.LogTail, FailureTailLines, FailureTailChars);
        return $"The build failed. {aRecord.Reason}" + (vTail.Length == 0 ? string.Empty : "\nEnd of its output:\n" + vTail);
    }
}
