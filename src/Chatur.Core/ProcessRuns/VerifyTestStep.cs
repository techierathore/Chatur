using System.Text.Json;
using System.Xml;
using Chatur.Core.Actions;
using Chatur.Core.BuildRun;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// The verifying run's test step: runs the project's own tests with machine-readable results
/// (<c>dotnet test</c> with the trx logger, <c>npx playwright test --reporter=json</c>), reads every
/// test's outcome and records it on the run. Tests that fail, or cannot run, do not fail the step: they
/// are evidence the verdict step grades from (a requirement they carry is not verified).
/// </summary>
public sealed class VerifyTestStep
{
    private const int NotRunTailLines = 3;
    private const int NotRunTailChars = 400;

    private readonly IProjectCommandRunner objRunner;
    private readonly RunEvidenceStore objStore;
    private readonly ProcessRunOptions objOptions;
    private readonly string objRunsRoot;

    /// <summary>Creates the step.</summary>
    /// <param name="aRunner">Runs the test commands.</param>
    /// <param name="aStore">Keeps the commands and tests.</param>
    /// <param name="aOptions">The time limit and output cap.</param>
    /// <param name="aRunsRoot">The folder under which each run gets a folder for its result files.</param>
    public VerifyTestStep(IProjectCommandRunner aRunner, RunEvidenceStore aStore, ProcessRunOptions aOptions, string aRunsRoot)
    {
        objRunner = aRunner;
        objStore = aStore;
        objOptions = aOptions;
        objRunsRoot = aRunsRoot;
    }

    /// <summary>Runs the project's tests and records every command and every test.</summary>
    /// <param name="aRunId">The run.</param>
    /// <param name="aPosition">The step's position.</param>
    /// <param name="aFolder">The project's folder.</param>
    /// <param name="aCt">A token that cancels the tests and stops their process tree.</param>
    /// <returns>Always a passed outcome whose result says how many tests ran and how they came out, or why they could not run.</returns>
    public async Task<ProcessStepOutcome> RunAsync(int aRunId, int aPosition, string aFolder, CancellationToken aCt)
    {
        await objStore.ClearAsync(aRunId, RunCommandKind.Test, aCt).ConfigureAwait(false);
        var vRunFolder = Path.Combine(objRunsRoot, aRunId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (Directory.Exists(vRunFolder))
        {
            Directory.Delete(vRunFolder, true);
        }

        Directory.CreateDirectory(vRunFolder);
        var vPlans = PlansFor(aFolder, vRunFolder);
        if (vPlans.Count == 0)
        {
            return new ProcessStepOutcome(true, "No tests found: the project has no .NET test project and no Playwright config. Every requirement will need a test.");
        }

        foreach (var vPlan in vPlans)
        {
            await RunPlanAsync(aRunId, aPosition, vPlan, aCt).ConfigureAwait(false);
        }

        return new ProcessStepOutcome(true, Summary(await objStore.TestEvidenceAsync(aRunId, aCt).ConfigureAwait(false)));
    }

    private List<TestPlan> PlansFor(string aFolder, string aRunFolder)
    {
        var vPlans = new List<TestPlan>();
        var vIndex = 0;
        foreach (var vProject in ProjectToolchain.DotNetTestProjects(aFolder))
        {
            vIndex++;
            var vResults = Path.Combine(aRunFolder, $"dotnet-{vIndex}");
            vPlans.Add(new TestPlan(
                new ProjectCommand(
                    ProjectToolchain.DotNetExecutable(),
                    $"test \"{vProject}\" --nologo -tl:off -nodeReuse:false -p:UseSharedCompilation=false --logger \"trx;LogFileName=results.trx\" --results-directory \"{vResults}\"",
                    aFolder,
                    objOptions.TestTimeout,
                    objOptions.MaxOutputChars),
                Path.Combine(vResults, "results.trx"),
                TestReportKind.Trx));
        }

        if (ProjectToolchain.HasPlaywrightConfig(aFolder))
        {
            var vReport = Path.Combine(aRunFolder, "playwright.json");
            vPlans.Add(new TestPlan(
                new ProjectCommand(
                    ProjectToolchain.NpxExecutable(),
                    $"--no-install playwright test --reporter=json --output \"{Path.Combine(aRunFolder, "playwright-output")}\"",
                    aFolder,
                    objOptions.TestTimeout,
                    objOptions.MaxOutputChars,
                    new Dictionary<string, string> { ["PLAYWRIGHT_JSON_OUTPUT_NAME"] = vReport }),
                vReport,
                TestReportKind.PlaywrightJson));
        }

        return vPlans;
    }

    private async Task RunPlanAsync(int aRunId, int aPosition, TestPlan aPlan, CancellationToken aCt)
    {
        var vResult = await objRunner.RunAsync(aPlan.Command, aCt).ConfigureAwait(false);
        var (vOutcome, vTests, vReason) = Interpret(aPlan, vResult);
        await objStore.SaveCommandAsync(aRunId, aPosition, new RunCommandRecord(
            RunCommandKind.Test,
            aPlan.Command.Display,
            vOutcome,
            vResult.Started && !vResult.TimedOut ? vResult.ExitCode : null,
            null,
            (long)vResult.Elapsed.TotalMilliseconds,
            (long)aPlan.Command.Timeout.TotalMilliseconds,
            vResult.Tail,
            vReason), aCt).ConfigureAwait(false);
        await objStore.SaveTestsAsync(aRunId, vTests, aCt).ConfigureAwait(false);
    }

    private static (RunCommandOutcome Outcome, IReadOnlyList<ReportedTest> Tests, string? Reason) Interpret(TestPlan aPlan, ProjectCommandResult aResult)
    {
        var vName = Path.GetFileName(aPlan.Command.FileName);
        if (!aResult.Started)
        {
            return (RunCommandOutcome.NotRun, [], aResult.Error);
        }

        if (aResult.TimedOut)
        {
            return (RunCommandOutcome.TimedOut, [], $"{vName} tests ran past their limit of {RunText.Span((long)aPlan.Command.Timeout.TotalMilliseconds)} and were stopped");
        }

        if (!File.Exists(aPlan.ResultFile))
        {
            // dotnet test exits 0 and writes no file for a project with no tests in it; anything else is a run that never reported.
            return aPlan.Kind == TestReportKind.Trx && aResult.ExitCode == 0
                ? (RunCommandOutcome.Succeeded, [], null)
                : (RunCommandOutcome.NotRun, [], $"{aPlan.Command.Display} exited with code {aResult.ExitCode} and wrote no results: {OneLine(aResult.Tail)}");
        }

        return Read(aPlan, aResult);
    }

    private static (RunCommandOutcome Outcome, IReadOnlyList<ReportedTest> Tests, string? Reason) Read(TestPlan aPlan, ProjectCommandResult aResult)
    {
        try
        {
            var vText = File.ReadAllText(aPlan.ResultFile);
            var vTests = aPlan.Kind == TestReportKind.Trx ? TrxResultParser.Parse(vText) : PlaywrightResultParser.Parse(vText);
            if (vTests.Count == 0 && aPlan.Kind == TestReportKind.PlaywrightJson && PlaywrightResultParser.FirstError(vText) is { } vError)
            {
                return (RunCommandOutcome.NotRun, [], $"Playwright reported no tests: {vError}");
            }

            return (RunCommandOutcome.Succeeded, vTests, null);
        }
        catch (Exception aEx) when (aEx is XmlException or JsonException or IOException)
        {
            return (RunCommandOutcome.NotRun, [], $"The results of {aPlan.Command.Display} could not be read: {aEx.Message} (exit code {aResult.ExitCode})");
        }
    }

    private static string OneLine(string aOutput)
    {
        var vTail = RunText.Tail(aOutput, NotRunTailLines, NotRunTailChars);
        return vTail.Length == 0 ? "no output" : vTail.Replace('\n', ' ');
    }

    private static string Summary(TestEvidence aEvidence)
    {
        var vPassed = aEvidence.Tests.Count(aTest => aTest.Outcome == ProcessTestOutcome.Passed);
        var vFailed = aEvidence.Tests.Count(aTest => aTest.Outcome == ProcessTestOutcome.Failed);
        var vCarrying = aEvidence.Tests.Count(aTest => aTest.Requirements.Count > 0);
        var vText = $"{aEvidence.Tests.Count} tests ran: {vPassed} passed, {vFailed} failed, {aEvidence.Tests.Count - vPassed - vFailed} skipped; {vCarrying} carry a requirement id.";
        return aEvidence.NotRunReasons.Count == 0 ? vText : vText + " Could not run: " + string.Join("; ", aEvidence.NotRunReasons) + ".";
    }
}
