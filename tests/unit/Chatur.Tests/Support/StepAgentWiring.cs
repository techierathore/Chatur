using System.Runtime.CompilerServices;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.Corrections;
using Chatur.Core.Data;
using Chatur.Core.Guards;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Chatur.Core.Routing;
using Chatur.Core.StepAgents;
using Microsoft.Extensions.Logging.Abstractions;
using TechieRag.Abstractions;
using TechieRag.Models;

namespace Chatur.Tests.Support;

/// <summary>Builds the real agent path (agent actions, guard pipeline, routing) over a test database with a scripted model, and the step hand-over on top of it.</summary>
public static class StepAgentWiring
{
    /// <summary>The hand-over over the real agent path.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aProvider">The scripted model every session talks to.</param>
    /// <param name="aProjectFolder">Where the agent's files go.</param>
    public static StepAgentActions StepAgents(IDbConnectionFactory aConnections, ILlmProvider aProvider, string aProjectFolder) =>
        new(aConnections, Agent(aConnections, aProvider, aProjectFolder), new SystemClock());

    /// <summary>The real agent path with real guards.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aProvider">The scripted model.</param>
    /// <param name="aProjectFolder">The folder file edits are written under.</param>
    public static AgentActions Agent(IDbConnectionFactory aConnections, ILlmProvider aProvider, string aProjectFolder) =>
        new(
            aConnections,
            new AgentSessionRegistry(),
            new GuardPipeline([new RoleRightsGuard(aConnections), new AskMeFirstGuard(aConnections), new SourceControlRefusalGuard()]),
            new SingleProviderFactory(aProvider),
            new DiskFileActions(aConnections, aProjectFolder),
            new SucceedingLauncher(),
            new CorrectionActions(aConnections),
            new MeasurementActions(aConnections, NullLogger<MeasurementActions>.Instance),
            new SystemClock(),
            new RoutingActions(aConnections));

    /// <summary>The real executor with the hand-over and the baseline.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aAppData">Where Chatur's own files go.</param>
    /// <param name="aSteps">The hand-over; the same instance the engine stops through, as one registry serves every scope in the app.</param>
    public static ProjectStepExecutor Executor(IDbConnectionFactory aConnections, string aAppData, IStepAgentActions aSteps)
    {
        var vClock = new SystemClock();
        return new ProjectStepExecutor(
            new ProjectFolders(aConnections),
            new RunEvidenceStore(aConnections, vClock),
            new ProjectCommandRunner(NullLogger<ProjectCommandRunner>.Instance),
            new ProcessRunOptions(),
            new FixedPaths(aAppData),
            vClock,
            aSteps,
            new ProcessRunBaseline(aConnections));
    }

    /// <summary>The run engine over the real executor and hand-over.</summary>
    /// <param name="aConnections">The test database.</param>
    /// <param name="aAppData">Where Chatur's own files go.</param>
    /// <param name="aProvider">The scripted model.</param>
    /// <param name="aProjectFolder">The folder file edits are written under.</param>
    public static ProcessRunActions Engine(IDbConnectionFactory aConnections, string aAppData, ILlmProvider aProvider, string aProjectFolder)
    {
        var vClock = new SystemClock();
        var vEvidence = new RunEvidenceStore(aConnections, vClock);
        var vSteps = StepAgents(aConnections, aProvider, aProjectFolder);
        return new ProcessRunActions(
            aConnections,
            Executor(aConnections, aAppData, vSteps),
            new MeasurementActions(aConnections, NullLogger<MeasurementActions>.Instance),
            new RunVerdictWriter(
                aConnections,
                new Chatur.Core.Processes.ProcessActions(aConnections, vClock, NullLogger<Chatur.Core.Processes.ProcessActions>.Instance),
                vClock,
                new ProjectFolders(aConnections),
                vEvidence),
            vEvidence,
            vClock,
            NullLogger<ProcessRunActions>.Instance,
            vSteps);
    }

    private sealed class FixedPaths : IAppPaths
    {
        public FixedPaths(string aFolder)
        {
            AppDataFolder = aFolder;
        }

        public string AppDataFolder { get; }

        public string DatabasePath => Path.Combine(AppDataFolder, "chatur.db");

        public string LogFolder => Path.Combine(AppDataFolder, "logs");
    }

    private sealed class SingleProviderFactory : IAgentLlmProviderFactory
    {
        private readonly ILlmProvider objProvider;

        public SingleProviderFactory(ILlmProvider aProvider) => objProvider = aProvider;

        public Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default) => Task.FromResult(objProvider);
    }

    private sealed class SucceedingLauncher : IProcessLauncher
    {
        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            aOnOutputLine("Build succeeded.");
            return Task.FromResult(0);
        }
    }
}

/// <summary>Reads and writes real files under a project's folder, the way the file layer does for a head.</summary>
public sealed class DiskFileActions : IFileActions
{
    private readonly string objFolder;

    /// <summary>Creates the file layer.</summary>
    /// <param name="aConnections">Unused; the folder is given.</param>
    /// <param name="aFolder">The project's folder.</param>
    public DiskFileActions(IDbConnectionFactory aConnections, string aFolder)
    {
        _ = aConnections;
        objFolder = aFolder;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) =>
        File.Exists(Path.Combine(objFolder, aRelativePath)) ? File.ReadAllTextAsync(Path.Combine(objFolder, aRelativePath), aCt) : Task.FromException<string>(new FileNotFoundException(aRelativePath));

    /// <inheritdoc />
    public async Task SaveAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default)
    {
        var vFull = Path.Combine(objFolder, aRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(vFull)!);
        await File.WriteAllTextAsync(vFull, aContent, aCt);
    }

    /// <inheritdoc />
    public Task OpenInEditorAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task OpenWithDefaultAppAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) => Task.CompletedTask;
}

/// <summary>A scripted streaming model: each queued response is one model call. A gate holds the model after its first piece of text, so a test can act while the agent is still working.</summary>
public sealed class ScriptedModel : ILlmProvider
{
    private readonly Queue<LlmResponse> objResponses;

    /// <summary>Creates the model with the replies it will give, one per call.</summary>
    /// <param name="aResponses">The replies.</param>
    public ScriptedModel(params LlmResponse[] aResponses)
    {
        objResponses = new Queue<LlmResponse>(aResponses);
    }

    /// <summary>When set, the stream waits on this after its first piece of text.</summary>
    public TaskCompletionSource? Gate { get; init; }

    /// <summary>The model name the replies report.</summary>
    public string ModelNameOverride { get; init; } = "scripted-model";

    /// <summary>When set, the first call fails with this before any text.</summary>
    public Exception? FailWith { get; init; }

    /// <summary>How many calls were made.</summary>
    public int Calls { get; private set; }

    /// <summary>A reply that only says something.</summary>
    /// <param name="aText">What the model says.</param>
    public static LlmResponse Say(string aText) => new() { Content = aText, Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } };

    /// <summary>A reply that asks for one tool.</summary>
    /// <param name="aName">The tool.</param>
    /// <param name="aJson">Its arguments.</param>
    public static LlmResponse Call(string aName, string aJson) => new() { ToolCalls = [new ToolCall { Id = Guid.NewGuid().ToString("N"), Name = aName, ArgumentsJson = aJson }], Usage = new TokenUsage() };

    /// <inheritdoc />
    public string Name => "Scripted";

    /// <inheritdoc />
    public string ModelName => ModelNameOverride;

    /// <inheritdoc />
    public bool SupportsToolCalling => true;

    /// <inheritdoc />
    public bool SupportsStreaming => true;

#pragma warning disable CS0067
    /// <inheritdoc />
    public event EventHandler<LlmCompletionEventArgs>? OnCompletionCompleted;
#pragma warning restore CS0067

    /// <inheritdoc />
    public Task<LlmResponse> CompleteAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<string> CompleteStreamAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public IAsyncEnumerable<string> ChatStreamAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public async IAsyncEnumerable<LlmStreamEvent> ChatStreamEventsAsync(
        IReadOnlyList<ChatMessage> messages,
        LlmCompletionOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls++;
        if (FailWith is not null && Calls == 1)
        {
            throw FailWith;
        }

        var vResponse = objResponses.Count > 0 ? objResponses.Dequeue() : Say("Nothing more to do.");
        var vContent = vResponse.Content ?? string.Empty;
        if (vContent.Length > 0)
        {
            var vHalf = Math.Max(1, vContent.Length / 2);
            yield return LlmStreamEvent.FromText(vContent[..vHalf]);
            if (Gate is not null)
            {
                await Gate.Task.WaitAsync(cancellationToken);
            }

            if (vHalf < vContent.Length)
            {
                yield return LlmStreamEvent.FromText(vContent[vHalf..]);
            }
        }
        else if (Gate is not null && (vResponse.ToolCalls?.Count ?? 0) == 0)
        {
            await Gate.Task.WaitAsync(cancellationToken);
        }

        foreach (var vToolCall in vResponse.ToolCalls ?? [])
        {
            yield return LlmStreamEvent.FromToolCall(vToolCall);
        }

        yield return LlmStreamEvent.FromCompleted(vResponse.Usage, vResponse.HasToolCalls ? "tool_calls" : "stop", ModelNameOverride);
    }

    /// <inheritdoc />
    public Task<T> CompleteAsync<T>(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();

    /// <inheritdoc />
    public int EstimateTokenCount(string text) => text.Length / 4;
}
