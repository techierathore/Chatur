using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.CompareModels;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using ChaturDb;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using TechieRag.Abstractions;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests.CompareModels;

/// <summary>
/// Tests <see cref="CompareModelsActions"/> against a real, migrated SQLite database, a real project folder under this test
/// project's bin folder, the real <see cref="MeasurementActions"/> writing the project's <c>docs/metrics</c>, and a scripted
/// model per compared model instead of a real TechieRag call (Coding Standards §Testability). BRD-291 to BRD-296.
/// </summary>
public sealed class CompareModelsActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(TestScratch.Root, $"chatur-compare-tests-{Guid.NewGuid():N}.db");
    private readonly string objProject = Path.Combine(TestScratch.Root, $"chatur-compare-project-{Guid.NewGuid():N}");
    private readonly TestDbConnectionFactory objDb;
    private readonly Dictionary<int, ScriptedModel> objModels = [];

    public CompareModelsActionsTests()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(vConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);
        objDb = new TestDbConnectionFactory(vConnectionString);

        Write("readme.md", "# Demo\nLine one\nLine two\n");
        Write("src/app.cs", "class App { }\n");
        Write("bin/old.dll", "not copied");
        Write(".git/HEAD", "ref: refs/heads/main\n");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objProject))
        {
            Directory.Delete(objProject, true);
        }
    }

    /// <summary>
    /// When two to four models are picked, then a pick is allowed; when one or five are picked, then the pick is refused
    /// with a reason in plain words, and Run refuses it too without starting any model (REQ-FN-327).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-327 one or five models are refused with the reason")]
    public async Task PickOfOneOrFiveIsRefused()
    {
        var vSut = CreateSut();
        Assert.Null(vSut.PickRefusal(2));
        Assert.Null(vSut.PickRefusal(4));
        Assert.Contains("at least two", vSut.PickRefusal(1));
        Assert.Contains("four at most", vSut.PickRefusal(5));

        var vProjectId = InsertProject();
        var vIds = Enumerable.Range(1, 5).Select(aIndex => AddModel($"model-{aIndex}", Answer($"answer {aIndex}"))).ToList();

        var vOne = await Assert.ThrowsAsync<ArgumentException>(() => vSut.RunAsync(new CompareRequest(vProjectId, [vIds[0]], CompareKind.Question, "Which files?")));
        Assert.Contains("at least two", vOne.Message);
        var vFive = await Assert.ThrowsAsync<ArgumentException>(() => vSut.RunAsync(new CompareRequest(vProjectId, vIds, CompareKind.Question, "Which files?")));
        Assert.Contains("four at most", vFive.Message);
        Assert.All(objModels.Values, aModel => Assert.Equal(0, aModel.Calls));
        Assert.Empty(await vSut.HistoryAsync(vProjectId));
    }

    /// <summary>
    /// When the person ticks two models of connected providers and types a task, then Run starts each model on that task,
    /// and each answer, in the order picked, is the comparison's result (REQ-FN-327). Every model of every connected
    /// provider is offered, labelled with its provider.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-327 Run starts each picked model on the task")]
    public async Task RunStartsEachPickedModel()
    {
        var vProjectId = InsertProject();
        var vFirst = AddModel("claude-sonnet-5-5", Answer("Sonnet's answer."));
        var vSecond = AddModel("claude-haiku-5-5", Answer("Haiku's answer."));
        var vSut = CreateSut();

        var vOffered = await vSut.ModelsAsync();
        Assert.Equal(new[] { "claude-haiku-5-5", "claude-sonnet-5-5" }, vOffered.Select(aModel => aModel.Identifier).Order());
        Assert.All(vOffered, aModel => Assert.Equal("Test provider", aModel.ProviderName));

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vFirst, vSecond], CompareKind.Question, "Which files?"));

        Assert.Equal("done", vComparison.State);
        Assert.Equal(new[] { "claude-sonnet-5-5", "claude-haiku-5-5" }, vComparison.Results.Select(aResult => aResult.Model));
        Assert.Equal(new[] { "Sonnet's answer.", "Haiku's answer." }, vComparison.Results.Select(aResult => aResult.Answer));
        Assert.All(objModels.Values, aModel => Assert.Contains("Which files?", aModel.FirstTask));
    }

    /// <summary>
    /// When the kind is "Question, changes nothing", then each model reads the open project through the read tool and is
    /// offered no tool that writes; a write it asks for anyway is refused; every file of the project is the same
    /// afterwards, no copy is made, and the comparison says nothing in the project changed (REQ-FN-328).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-328 a question reads the open project and changes nothing")]
    public async Task QuestionChangesNothing()
    {
        var vProjectId = InsertProject();
        var vBefore = Hashes(objProject);
        var vReader = AddModel("reader", Call("read-file", """{"path":"readme.md"}"""), Answer("It says Demo."));
        var vWriter = AddModel("writer", Call("write-file", """{"path":"readme.md","content":"overwritten"}"""), Answer("Tried."));
        var vSut = CreateSut();

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vReader, vWriter], CompareKind.Question, "What does the readme say?"));

        Assert.Equal(vBefore, Hashes(objProject));
        Assert.True(vComparison.ProjectUnchanged);
        Assert.False(Directory.Exists(Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder)));
        Assert.All(objModels.Values, aModel => Assert.Equal(new[] { "read-file" }, aModel.ToolsOffered));
        Assert.Contains("# Demo", objModels[vReader].ToolResults.Single());
        Assert.Contains("nothing may be written", objModels[vWriter].ToolResults.Single());
    }

    /// <summary>
    /// When the kind is "Task that changes files", then each model works in its own copy under the project's
    /// <c>bin/chatur-compare/&lt;comparison&gt;</c> folder (bin, obj and .git left out of it), its changed files are listed
    /// against the project with lines added and removed, the real project is unchanged, and every copy is removed when
    /// the run ends (REQ-FN-329).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-329 a task works in its own copy under bin, removed afterwards")]
    public async Task TaskWorksInItsOwnCopy()
    {
        var vProjectId = InsertProject();
        var vBefore = Hashes(objProject);
        var vCopiesSeen = new List<string>();
        var vFirst = AddModel(
            "editor/one",
            Call("write-file", """{"path":"notes.md","content":"new\nnotes\n"}"""),
            Call("write-file", """{"path":"readme.md","content":"# Demo\nLine one\nLine 2\nLine three\n"}"""),
            Answer("Added notes and changed the readme."));
        var vSecond = AddModel("editor-two", Call("write-file", """{"path":"../../../escape.md","content":"x"}"""), Answer("Tried to leave."));
        objModels[vFirst].OnCall = () => vCopiesSeen.AddRange(CopiesNow());
        var vSut = CreateSut();

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vFirst, vSecond], CompareKind.Change, "Add notes."));

        Assert.Equal(vBefore, Hashes(objProject));
        var vCopyRoot = Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder, vComparison.ComparisonId.ToString());
        Assert.Contains(vCopiesSeen, aFile => aFile.StartsWith(vCopyRoot, StringComparison.Ordinal) && aFile.EndsWith("notes.md", StringComparison.Ordinal));
        Assert.DoesNotContain(vCopiesSeen, aFile => aFile.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || aFile.EndsWith("old.dll", StringComparison.Ordinal));
        Assert.False(Directory.Exists(Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder)));
        Assert.True(vComparison.CopiesRemoved);
        Assert.False(File.Exists(Path.Combine(objProject, "bin", "escape.md")));

        var vFiles = vComparison.Results[0].Files;
        Assert.Equal(2, vFiles.Count);
        Assert.Equal(new CompareChangedFile("notes.md", "A", 2, 0), vFiles[0]);
        Assert.Equal(new CompareChangedFile("readme.md", "M", 2, 1), vFiles[1]);
        Assert.Empty(vComparison.Results[1].Files);
        Assert.Contains("outside the project", objModels[vSecond].ToolResults.Single());
    }

    /// <summary>
    /// When the comparison appends a model's run record to the project's docs/metrics while another model still works in
    /// its copy, then that copy's changed files list only what the model changed, never docs/metrics (REQ-FN-329; found by
    /// the browser check of 27 Nov 2026, which saw "M docs/metrics/runs.jsonl" on a card).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-329 docs/metrics written during the run is not a changed file")]
    public void MetricsWrittenDuringTheRunIsNotAChange()
    {
        var vCopy = Path.Combine(TestScratch.Root, $"chatur-compare-copy-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(Path.Combine(objProject, "docs", "metrics"));
            File.WriteAllText(Path.Combine(objProject, "docs", "metrics", "runs.jsonl"), "{\"kind\":\"run\"}\n");
            CompareWorkspace.Copy(objProject, vCopy);
            // While the models work, the comparison appends a model's run record to the project's own docs/metrics.
            File.AppendAllText(Path.Combine(objProject, "docs", "metrics", "runs.jsonl"), "{\"kind\":\"run\"}\n");
            File.WriteAllText(Path.Combine(vCopy, "notes.md"), "one line\n");

            var vChanges = CompareWorkspace.Changes(objProject, vCopy);

            Assert.Equal([new CompareChangedFile("notes.md", "A", 1, 0)], vChanges);
        }
        finally
        {
            if (Directory.Exists(vCopy))
            {
                Directory.Delete(vCopy, recursive: true);
            }
        }
    }

    /// <summary>
    /// When one model fails part-way and the run is a task that changes files, then that model's error is in its own
    /// result, the other model still finishes, and every copy is still removed (REQ-FN-329; UI Design, error state).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-329 copies are removed when a model fails")]
    public async Task CopiesAreRemovedWhenAModelFails()
    {
        var vProjectId = InsertProject();
        var vGood = AddModel("good", Call("write-file", """{"path":"a.md","content":"a"}"""), Answer("Done."));
        var vBad = AddModel("bad", Call("write-file", """{"path":"b.md","content":"b"}"""));
        objModels[vBad].FailOnCall = 2;
        var vSut = CreateSut();

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vGood, vBad], CompareKind.Change, "Write a file."));

        Assert.Equal("done", vComparison.State);
        Assert.Equal("done", vComparison.Results[0].State);
        Assert.Equal("failed", vComparison.Results[1].State);
        Assert.Contains("the model broke", vComparison.Results[1].Error);
        Assert.False(Directory.Exists(Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder)));
        Assert.False(File.Exists(Path.Combine(objProject, "a.md")));
    }

    /// <summary>
    /// When the run is cancelled while the models are working in their copies, then each model reads "Stopped before it
    /// finished." and every copy is removed (REQ-FN-329).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-329 copies are removed when the run is cancelled")]
    public async Task CopiesAreRemovedWhenCancelled()
    {
        var vProjectId = InsertProject();
        using var vCts = new CancellationTokenSource();
        var vFirst = AddModel("slow-one", Answer("never"));
        var vSecond = AddModel("slow-two", Answer("never"));
        foreach (var vModel in objModels.Values)
        {
            vModel.WaitBeforeAnswer = true;
        }

        var vSut = CreateSut();
        var vRun = vSut.RunAsync(new CompareRequest(vProjectId, [vFirst, vSecond], CompareKind.Change, "Slow task."), null, vCts.Token);
        await WaitUntilAsync(() => objModels.Values.All(aModel => aModel.Calls > 0));
        Assert.True(Directory.Exists(Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder)));
        await vCts.CancelAsync();
        var vComparison = await vRun;

        Assert.All(vComparison.Results, aResult => Assert.Equal("Stopped before it finished.", aResult.Error));
        Assert.Equal("failed", vComparison.State);
        Assert.False(Directory.Exists(Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder)));
    }

    /// <summary>
    /// When a comparison ends, then each model's result has its own time, tokens in and out as TechieRag reported them
    /// over every call, and TechieRag's own price, or no price when TechieRag gave none (REQ-UI-132).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-132 each model has its time, tokens and cost or no price")]
    public async Task EachModelHasTimeTokensAndCost()
    {
        var vProjectId = InsertProject();
        var vPriced = AddModel(
            "priced",
            Call("read-file", """{"path":"."}""", 100, 10, 0.004m),
            Answer("Two files.", 200, 20, 0.006m));
        var vFree = AddModel("free", Answer("Two.", 50, 5, 0m));
        var vSut = CreateSut();

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vPriced, vFree], CompareKind.Question, "How many files?"));

        var vFirst = vComparison.Results[0];
        Assert.Equal(300, vFirst.TokensIn);
        Assert.Equal(30, vFirst.TokensOut);
        Assert.Equal(0.01m, vFirst.CostUsd);
        Assert.NotNull(vFirst.DurationMs);
        Assert.Contains("readme.md", objModels[vPriced].ToolResults.Single());
        var vSecond = vComparison.Results[1];
        Assert.Equal(50, vSecond.TokensIn);
        Assert.Equal(5, vSecond.TokensOut);
        Assert.Null(vSecond.CostUsd);
        Assert.NotNull(vSecond.DurationMs);
    }

    /// <summary>
    /// When the person marks a model's result right and then wrong, then the latest mark is kept, shows again when that
    /// comparison is opened again, and Past comparisons counts it (REQ-UI-133).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-133 a mark is kept and shows again when the comparison is opened")]
    public async Task MarkIsKeptAndShowsAgain()
    {
        var vProjectId = InsertProject();
        var vFirst = AddModel("first", Answer("A."));
        var vSecond = AddModel("second", Answer("B."));
        var vSut = CreateSut();
        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vFirst, vSecond], CompareKind.Question, "Q?"));

        await vSut.MarkAsync(vComparison.Results[0].ResultId, true);
        await vSut.MarkAsync(vComparison.Results[0].ResultId, false);
        await vSut.MarkAsync(vComparison.Results[1].ResultId, true);

        var vAgain = await CreateSut().OpenAsync(vComparison.ComparisonId);
        Assert.NotNull(vAgain);
        Assert.Equal(new[] { "wrong", "right" }, vAgain.Results.Select(aResult => aResult.Mark));
        var vHistory = Assert.Single(await vSut.HistoryAsync(vProjectId));
        Assert.Equal((1, 1, 0), (vHistory.Right, vHistory.Wrong, vHistory.NotMarked));
        Assert.Equal(new[] { "first", "second" }, vHistory.Models);
    }

    /// <summary>
    /// When a comparison ends and marks change, then runs.jsonl holds one compare-models record per model in TfLens's
    /// shape (numbers as numbers, started to the millisecond and unique, cost null when there is no price, files_written
    /// for a task), and gates.jsonl holds one record per mark whose run_id is that model's started, Verified for right and
    /// FAIL with gate owner-mark for wrong; a changed mark is a new record (REQ-FN-330). The comparison opens again.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-330 runs and marks are kept in docs/metrics in TfLens's shape")]
    public async Task RunsAndMarksAreKeptInMetrics()
    {
        var vProjectId = InsertProject();
        var vFirst = AddModel("priced-model", Call("write-file", """{"path":"x.md","content":"x"}""", 10, 1, 0.5m), Answer("Wrote x.", 20, 2, 0.25m));
        var vSecond = AddModel("free-model", Answer("Nothing to do.", 7, 3, 0m));
        var vSut = CreateSut();

        var vComparison = await vSut.RunAsync(new CompareRequest(vProjectId, [vFirst, vSecond], CompareKind.Change, "Write x."));
        await vSut.MarkAsync(vComparison.Results[0].ResultId, true);
        await vSut.MarkAsync(vComparison.Results[0].ResultId, false);

        var vRuns = ReadStream("runs.jsonl");
        Assert.Equal(2, vRuns.Count);
        Assert.All(vRuns, aRun =>
        {
            Assert.Equal(1, aRun.GetProperty("v").GetInt32());
            Assert.Equal("run", aRun.GetProperty("kind").GetString());
            Assert.Equal("compare-models", aRun.GetProperty("cmd").GetString());
            Assert.Equal("change", aRun.GetProperty("mode").GetString());
            Assert.Equal("chatur", aRun.GetProperty("harness").GetString());
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$", aRun.GetProperty("started").GetString());
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$", aRun.GetProperty("ended").GetString());
            Assert.Equal(JsonValueKind.Number, aRun.GetProperty("duration_s").ValueKind);
            Assert.Equal(JsonValueKind.Number, aRun.GetProperty("tokens_in").ValueKind);
        });
        Assert.Equal(2, vRuns.Select(aRun => aRun.GetProperty("started").GetString()).Distinct().Count());

        var vPriced = vRuns.Single(aRun => aRun.GetProperty("model").GetString() == "priced-model");
        Assert.Equal(30, vPriced.GetProperty("tokens_in").GetInt32());
        Assert.Equal(3, vPriced.GetProperty("tokens_out").GetInt32());
        Assert.Equal(0.75, vPriced.GetProperty("cost_usd").GetDouble(), 6);
        Assert.Equal(1, vPriced.GetProperty("files_written").GetInt32());
        var vFree = vRuns.Single(aRun => aRun.GetProperty("model").GetString() == "free-model");
        Assert.Equal(JsonValueKind.Null, vFree.GetProperty("cost_usd").ValueKind);
        Assert.Equal(0, vFree.GetProperty("files_written").GetInt32());

        var vGates = ReadStream("gates.jsonl");
        Assert.Equal(2, vGates.Count);
        Assert.All(vGates, aGate =>
        {
            Assert.Equal("gate", aGate.GetProperty("kind").GetString());
            Assert.Equal(vPriced.GetProperty("started").GetString(), aGate.GetProperty("run_id").GetString());
            Assert.Equal(new[] { "owner-mark" }, aGate.GetProperty("gates_run").EnumerateArray().Select(aItem => aItem.GetString()));
        });
        Assert.Equal("Verified", vGates[0].GetProperty("verdict").GetString());
        Assert.Equal(JsonValueKind.Null, vGates[0].GetProperty("gate").ValueKind);
        Assert.Equal("FAIL", vGates[1].GetProperty("verdict").GetString());
        Assert.Equal("owner-mark", vGates[1].GetProperty("gate").GetString());

        var vAgain = await CreateSut().OpenAsync(vComparison.ComparisonId);
        Assert.Equal("wrong", vAgain!.Results[0].Mark);
        Assert.Equal(vComparison.Results[0].Files, vAgain.Results[0].Files);
    }

    private CompareModelsActions CreateSut() => new(
        objDb,
        new ScriptedFactory(objModels),
        new MeasurementActions(objDb, NullLogger<MeasurementActions>.Instance),
        new SystemClock(),
        NullLogger<CompareModelsActions>.Instance);

    private void Write(string aRelative, string aText)
    {
        var vFull = Path.Combine(objProject, aRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(vFull)!);
        File.WriteAllText(vFull, aText);
    }

    private int InsertProject()
    {
        using var vConnection = objDb.OpenConnection();
        return (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO Project (Name, Path, Kind) VALUES ('CompareDemo', @objProject, 'Folder'); SELECT last_insert_rowid();",
            new { objProject });
    }

    private int AddModel(string aIdentifier, params Step[] aSteps)
    {
        using var vConnection = objDb.OpenConnection();
        var vProviderId = vConnection.ExecuteScalar<long?>("SELECT ProviderId FROM Provider WHERE Name = 'Test provider';")
            ?? vConnection.ExecuteScalar<long>(
                "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES ('Test provider', 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();");
        var vModelId = (int)vConnection.ExecuteScalar<long>(
            "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, 2, @aIdentifier); SELECT last_insert_rowid();",
            new { vProviderId, aIdentifier });
        objModels[vModelId] = new ScriptedModel(aSteps);
        return vModelId;
    }

    private IEnumerable<string> CopiesNow()
    {
        var vCopies = Path.Combine(objProject, "bin", CompareModelsActions.CopiesFolder);
        return Directory.Exists(vCopies) ? Directory.GetFiles(vCopies, "*", SearchOption.AllDirectories) : [];
    }

    private List<JsonElement> ReadStream(string aName) =>
        File.ReadAllLines(Path.Combine(objProject, "docs", "metrics", aName))
            .Where(aLine => aLine.Trim().Length > 0)
            .Select(aLine => JsonDocument.Parse(aLine).RootElement.Clone())
            .ToList();

    /// <summary>Every file of a folder (docs/metrics and the bin folder left out) with a hash of its bytes.</summary>
    private static SortedDictionary<string, string> Hashes(string aRoot)
    {
        var vResult = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var vFile in Directory.GetFiles(aRoot, "*", SearchOption.AllDirectories))
        {
            var vRelative = Path.GetRelativePath(aRoot, vFile).Replace('\\', '/');
            if (vRelative.StartsWith("bin/", StringComparison.Ordinal) || vRelative.StartsWith("docs/metrics/", StringComparison.Ordinal))
            {
                continue;
            }

            vResult[vRelative] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(vFile)));
        }

        return vResult;
    }

    private static async Task WaitUntilAsync(Func<bool> aCondition)
    {
        for (var vTry = 0; vTry < 200 && !aCondition(); vTry++)
        {
            await Task.Delay(25);
        }

        Assert.True(aCondition());
    }

    private static Step Answer(string aText, int aIn = 5, int aOut = 2, decimal aCost = 0m) => new(aText, null, null, aIn, aOut, aCost);

    private static Step Call(string aTool, string aArguments, int aIn = 5, int aOut = 2, decimal aCost = 0m) => new(null, aTool, aArguments, aIn, aOut, aCost);

    /// <summary>One model call: text, or one tool call, and what it used.</summary>
    private sealed record Step(string? Text, string? Tool, string? Arguments, int In, int Out, decimal Cost);

    private sealed class ScriptedFactory(Dictionary<int, ScriptedModel> aModels) : IAgentLlmProviderFactory
    {
        public Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default) => Task.FromResult<ILlmProvider>(aModels[aModel.ModelId]);
    }

    /// <summary>A scripted model: each stream it is opened with plays the next <see cref="Step"/>.</summary>
    private sealed class ScriptedModel(Step[] aSteps) : ILlmProvider
    {
        private readonly Queue<Step> objSteps = new(aSteps);

        public int Calls { get; private set; }

        public int FailOnCall { get; set; }

        public bool WaitBeforeAnswer { get; set; }

        public Action? OnCall { get; set; }

        public string FirstTask { get; private set; } = string.Empty;

        public List<string> ToolsOffered { get; private set; } = [];

        public List<string> ToolResults { get; } = [];

        public string Name => "Scripted";

        public string ModelName => "scripted";

        public bool SupportsToolCalling => true;

        public bool SupportsStreaming => true;

#pragma warning disable CS0067 // never raised by this fake; the interface requires it
        public event EventHandler<LlmCompletionEventArgs>? OnCompletionCompleted;
#pragma warning restore CS0067

        public Task<LlmResponse> CompleteAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<string> CompleteStreamAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<string> ChatStreamAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<T> CompleteAsync<T>(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();

        public int EstimateTokenCount(string text) => text.Length / 4;

        public async IAsyncEnumerable<LlmStreamEvent> ChatStreamEventsAsync(
            IReadOnlyList<ChatMessage> messages,
            LlmCompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Calls++;
            OnCall?.Invoke();
            if (Calls == 1)
            {
                FirstTask = string.Join("\n", messages.Select(aMessage => aMessage.Content ?? string.Empty));
                ToolsOffered = options?.Tools?.Select(aTool => aTool.Name).ToList() ?? [];
            }
            else if (messages[^1].Content is { } vToolResult)
            {
                ToolResults.Add(vToolResult);
            }

            if (FailOnCall == Calls)
            {
                throw new InvalidOperationException("the model broke");
            }

            if (WaitBeforeAnswer)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            var vStep = objSteps.Dequeue();
            if (vStep.Text is not null)
            {
                yield return LlmStreamEvent.FromText(vStep.Text);
            }

            if (vStep.Tool is not null)
            {
                yield return LlmStreamEvent.FromToolCall(new ToolCall { Id = $"call-{Calls}", Name = vStep.Tool, ArgumentsJson = vStep.Arguments! });
            }

            yield return LlmStreamEvent.FromCompleted(
                new TokenUsage { InputTokens = vStep.In, OutputTokens = vStep.Out, EstimatedCostUsd = vStep.Cost },
                vStep.Tool is null ? "stop" : "tool_calls",
                ModelName);
        }
    }
}
