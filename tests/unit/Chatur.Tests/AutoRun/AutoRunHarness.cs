using Chatur.Core.Actions;
using Chatur.Core.AutoRun;
using Chatur.Core.Data;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Processes;
using Chatur.Tests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chatur.Tests.AutoRun;

/// <summary>A clock a test moves by hand.</summary>
public sealed class ManualClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow { get; set; } = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
}

/// <summary>A delay that lets no time pass in the test but moves the manual clock on, and can run a hook while "waiting".</summary>
public sealed class ManualDelay : IAutoRunDelay
{
    private readonly ManualClock objClock;

    /// <summary>Creates the delay.</summary>
    /// <param name="aClock">The clock to move on.</param>
    public ManualDelay(ManualClock aClock)
    {
        objClock = aClock;
    }

    /// <summary>Every span waited, in order.</summary>
    public List<TimeSpan> Waited { get; } = [];

    /// <summary>Run after each wait, e.g. to pause the run while it waits.</summary>
    public Func<Task>? AfterWait { get; set; }

    /// <inheritdoc />
    public async Task DelayAsync(TimeSpan aSpan, CancellationToken aCt)
    {
        Waited.Add(aSpan);
        objClock.UtcNow += aSpan;
        if (AfterWait is { } vHook)
        {
            await vHook().ConfigureAwait(false);
        }
    }
}

/// <summary>A launcher that only remembers it was asked; the test calls <c>AdvanceAsync</c> itself.</summary>
public sealed class RecordingLauncher : IAutoRunLauncher
{
    /// <summary>The runs it was asked to carry on.</summary>
    public List<int> Launched { get; } = [];

    /// <inheritdoc />
    public void Launch(int aAutoRunId) => Launched.Add(aAutoRunId);
}

/// <summary>A verdict writer that grades nothing, so a verifying process does not rewrite the test checklist.</summary>
public sealed class QuietVerdictWriter : IRunVerdictWriter
{
    /// <inheritdoc />
    public Task<string> WriteAsync(ProcessRunView aRun, ProcessDefinition aDefinition, CancellationToken aCt = default) =>
        Task.FromResult("Verdicts written.");
}

/// <summary>Carries out each step as a test scripts it, and remembers every call.</summary>
public sealed class ScriptedExecutor : IProcessStepExecutor
{
    /// <summary>Every call as <c>process:step</c>, in order.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>What a step does; the default passes. The third argument is how many times this exact step was called, from 1.</summary>
    public Func<string, ProcessRunStepView, int, CancellationToken, Task<ProcessStepOutcome>>? Script { get; set; }

    /// <inheritdoc />
    public Task<ProcessStepOutcome> ExecuteAsync(int aRunId, int aProjectId, string aProcessCode, ProcessRunStepView aStep, CancellationToken aCt = default)
    {
        var vKey = $"{aProcessCode}:{aStep.StepCode}";
        Calls.Add(vKey);
        var vTimes = Calls.Count(aCall => aCall == vKey);
        return Script is null
            ? Task.FromResult(new ProcessStepOutcome(true, "Done"))
            : Script(aProcessCode, aStep, vTimes, aCt);
    }
}

/// <summary>Wires the end-to-end run over a test database, a scripted executor and a manual clock.</summary>
public sealed class AutoRunHarness
{
    /// <summary>Wires it.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aOptions">The limits to run under.</param>
    public AutoRunHarness(IDbConnectionFactory aConnections, AutoRunOptions? aOptions = null)
    {
        Executor = new ScriptedExecutor();
        Clock = new ManualClock();
        Delay = new ManualDelay(Clock);
        Launcher = new RecordingLauncher();
        var vEngineClock = new SystemClock();
        var vEngine = new ProcessRunActions(
            aConnections,
            Executor,
            new MeasurementActions(aConnections, NullLogger<MeasurementActions>.Instance),
            new QuietVerdictWriter(),
            new RunEvidenceStore(aConnections, vEngineClock),
            vEngineClock,
            NullLogger<ProcessRunActions>.Instance);
        Processes = new ProcessActions(aConnections, vEngineClock, NullLogger<ProcessActions>.Instance);
        Runs = vEngine;
        var vStore = new AutoRunStore(aConnections, Clock);
        var vViews = new AutoRunViewBuilder(vStore, vEngine, new OpenRequirementReader(new ProjectFolders(aConnections)));
        Actions = new AutoRunActions(
            vStore,
            vViews,
            Processes,
            vEngine,
            vEngine,
            Launcher,
            Delay,
            aOptions ?? new AutoRunOptions { StallTimeout = TimeSpan.FromSeconds(5) },
            Clock,
            NullLogger<AutoRunActions>.Instance);
    }

    /// <summary>The scripted executor.</summary>
    public ScriptedExecutor Executor { get; }

    /// <summary>The manual clock.</summary>
    public ManualClock Clock { get; }

    /// <summary>The delay.</summary>
    public ManualDelay Delay { get; }

    /// <summary>The launcher.</summary>
    public RecordingLauncher Launcher { get; }

    /// <summary>The by-hand process actions.</summary>
    public ProcessActions Processes { get; }

    /// <summary>The process run engine.</summary>
    public ProcessRunActions Runs { get; }

    /// <summary>The end-to-end run under test.</summary>
    public AutoRunActions Actions { get; }
}
