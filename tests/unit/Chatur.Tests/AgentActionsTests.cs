using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using Chatur.Core.Actions;
using Chatur.Core.Agent;
using Chatur.Core.Corrections;
using Chatur.Core.Data;
using Chatur.Core.Guards;
using Chatur.Core.Measurements;
using Chatur.Core.Platform;
using Chatur.Core.Routing;
using ChaturDb;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using TechieRag.Abstractions;
using TechieRag.Models;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests <see cref="AgentActions"/> against a real, temporary SQLite database (migrated and seeded
/// exactly as a real installation is) and a fake chat client instead of a real TechieRag provider and
/// network call (Coding Standards §Testability; "unit-test the loop with a fake chat client").
/// Exercises REQ-FN-024 through REQ-FN-027, REQ-FN-031, REQ-FN-032 and REQ-FN-036 — the guard
/// pipeline in these tests is the real <see cref="RoleRightsGuard"/>/<see cref="AskMeFirstGuard"/>/
/// <see cref="SourceControlRefusalGuard"/>, not a fake, so a test failure here would be a real guard
/// regression, not a fake one agreeing with itself.
/// </summary>
public sealed class AgentActionsTests : IDisposable
{
    private readonly string objDatabasePath = Path.Combine(Path.GetTempPath(), $"chatur-agent-tests-{Guid.NewGuid():N}.db");
    private readonly TestDbConnectionFactory objDb;
    private readonly GuardPipeline objGuards;
    private readonly FakeFileActions objFiles = new();
    private readonly FakeProcessLauncher objProcessLauncher = new();
    private readonly ICorrectionActions objCorrections;
    private readonly IMeasurementActions objMeasurements;

    public AgentActionsTests()
    {
        var vConnectionString = $"Data Source={objDatabasePath}";
        var vResult = ChaturDbMigrator.Migrate(vConnectionString);
        Assert.True(vResult.Successful, vResult.Error?.Message);

        objDb = new TestDbConnectionFactory(vConnectionString);
        objCorrections = new CorrectionActions(objDb);
        objMeasurements = new MeasurementActions(objDb, NullLogger<MeasurementActions>.Instance);
        objGuards = new GuardPipeline(new IToolGuard[]
        {
            new RoleRightsGuard(objDb),
            new AskMeFirstGuard(objDb),
            new SourceControlRefusalGuard(),
        });
    }

    /// <summary>
    /// When the model answers with no tool call, then the reply's text is handed back piece by piece
    /// (REQ-FN-027), the final piece names the model that produced it (REQ-FN-026) and carries the
    /// real token count, and the reply is on record for the session (REQ-FN-025, REQ-FN-032).
    /// </summary>
    [Fact]
    public async Task SendAsyncAnswersWithTheModelsReplyWhenNoToolIsUsed()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-a");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse { Content = "Hello there.", Usage = new TokenUsage { InputTokens = 10, OutputTokens = 5 } });
        var vSut = CreateSut(vProvider);

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Hi", null, null, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
        }

        Assert.Equal("Hello there.", string.Concat(vChunks.Select(c => c.TextDelta)));
        Assert.True(vChunks[^1].IsFinal);
        Assert.Equal("fake-model", vChunks[^1].ModelName);
        Assert.Equal(15, vChunks[^1].TokensUsed);
    }

    /// <summary>
    /// When the session is in "go ahead" and the acting role may edit files, then an <c>edit-file</c>
    /// tool call passes the guards, writes the file for real and is recorded as an approved change
    /// (REQ-FN-025, REQ-FN-036).
    /// </summary>
    [Fact]
    public async Task SendAsyncWritesTheFileWhenGoAheadAllowsTheEdit()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-b");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.EditFile, ArgumentsJson = """{"path":"a.txt","content":"new text"}""" }],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "Done.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        await foreach (var _ in vSut.SendAsync(vSessionId, "Please edit a.txt", null, null, SessionMode.GoAhead))
        {
        }

        Assert.Contains(objFiles.Saved, s => s.ProjectId == vProjectId && s.Path == "a.txt" && s.Content == "new text");
        Assert.Equal("Approved", ReadChangeStatus(vSessionId));
    }

    /// <summary>
    /// When the owner picks a different role in the composer than the one the session started with,
    /// then that role is the one acting: its rights decide the guard (the Analyst may not edit, the
    /// flow master may), and the session records the new role (REQ-FN-024, fix 2026-10-01).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-024 the role picked in the composer is the role acting")]
    public async Task SendAsyncActsAsThePickedRole()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-picked-role");
        var vAnalyst = GetRoleId("analyst");
        var vFlowMaster = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vAnalyst, SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.EditFile, ArgumentsJson = """{"path":"a.txt","content":"picked"}""" }],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "Done.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        await foreach (var _ in vSut.SendAsync(vSessionId, "Please edit a.txt", vFlowMaster, null, SessionMode.GoAhead))
        {
        }

        Assert.Contains(objFiles.Saved, s => s.ProjectId == vProjectId && s.Path == "a.txt" && s.Content == "picked");
        var vSession = Assert.Single(await vSut.SessionsAsync(vProjectId));
        Assert.Equal("Flow master", vSession.RoleName, ignoreCase: true);
    }

    /// <summary>
    /// When the session is in "ask me first", then the same <c>edit-file</c> tool call is refused by
    /// <see cref="AskMeFirstGuard"/>, nothing is written to disk, and the agent loop turns the
    /// refusal into a proposed <c>Change</c> row instead of only a refusal (REQ-FN-035).
    /// </summary>
    [Fact]
    public async Task SendAsyncHoldsTheEditForApprovalWhenAskFirst()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-c");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.AskFirst);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.EditFile, ArgumentsJson = """{"path":"b.txt","content":"proposed"}""" }],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "I've proposed the change.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        await foreach (var _ in vSut.SendAsync(vSessionId, "Edit b.txt", null, null, SessionMode.AskFirst))
        {
        }

        Assert.DoesNotContain(objFiles.Saved, s => s.Path == "b.txt");
        Assert.Equal("Proposed", ReadChangeStatus(vSessionId));
        Assert.True(CountSessionEvents(vSessionId, "refusal") >= 1);
    }

    /// <summary>
    /// When a model asks to run a source-control command, then <see cref="SourceControlRefusalGuard"/>
    /// refuses it with no exception, and the refusal — not a source-control call — is what is
    /// recorded (REQ-UI-033, REQ-FN-036).
    /// </summary>
    [Fact]
    public async Task SendAsyncRefusesASourceControlToolCall()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-d");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.RunSourceControl, ArgumentsJson = """{"command":"commit"}""" }],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "I cannot run that.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        await foreach (var _ in vSut.SendAsync(vSessionId, "commit please", null, null, SessionMode.GoAhead))
        {
        }

        using var vConnection = objDb.OpenConnection();
        var vReason = vConnection.QuerySingleOrDefault<string>(
            "SELECT Payload FROM SessionEvent WHERE Kind = 'refusal' AND SessionId = @vSessionId;", new { vSessionId });
        Assert.NotNull(vReason);
        Assert.Contains("source-control", vReason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// REQ-UI-030 "a role may only do what its rights allow", proven end to end with no live model:
    /// when the acting role's rights refuse a tool (the Analyst, seeded with <c>edit-file</c> off in
    /// <c>0002-SeedRoles.sql</c>, asks to edit a file), then <see cref="RoleRightsGuard"/> — the real
    /// guard, not a fake — refuses it through the same <see cref="GuardPipeline"/> every tool call
    /// passes (REQ-FN-036), the file is untouched, no <c>Change</c> row is created at all (unlike the
    /// "ask me first" hold, this is a hard refusal, not something waiting for approval), and the
    /// refusal is recorded as a <c>SessionEvent</c> of <c>Kind = "refusal"</c> — the exact row
    /// <c>AgentActions.ActivityAsync</c> reads and the Workbench activity panel renders (REQ-UI-022).
    /// </summary>
    [Fact]
    public async Task SendAsyncRefusesAnEditWhenTheRoleHasNoRightToIt()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-e");
        var vRoleId = GetRoleId("analyst");
        InsertConnectedModel(3);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.EditFile, ArgumentsJson = """{"path":"c.txt","content":"x"}""" }],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "I cannot edit files.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        await foreach (var _ in vSut.SendAsync(vSessionId, "edit c.txt", null, null, SessionMode.GoAhead))
        {
        }

        Assert.DoesNotContain(objFiles.Saved, s => s.Path == "c.txt");

        using var vConnection = objDb.OpenConnection();
        var vChangeCount = vConnection.ExecuteScalar<long>("SELECT COUNT(*) FROM Change WHERE SessionId = @vSessionId;", new { vSessionId });
        Assert.Equal(0, vChangeCount);

        Assert.Equal(1, CountSessionEvents(vSessionId, "refusal"));
        var vRefusalPayload = vConnection.QuerySingleOrDefault<string>(
            "SELECT Payload FROM SessionEvent WHERE SessionId = @vSessionId AND Kind = 'refusal';", new { vSessionId });
        Assert.NotNull(vRefusalPayload);

        // The activity panel is exactly ActivityAsync's own stream of non-conversation SessionEvent
        // rows (REQ-UI-022) — proving the refusal reaches that stream, not only the SessionEvent
        // table, is what makes REQ-UI-030's "the refusal is shown" honest rather than assumed.
        var vActivity = new List<ActivityEvent>();
        using var vActivityCts = new CancellationTokenSource();
        await foreach (var vEvent in vSut.ActivityAsync(vSessionId, vActivityCts.Token))
        {
            vActivity.Add(vEvent);
            if (vActivity.Count >= 1)
            {
                vActivityCts.Cancel();
            }
        }

        var vRefusalActivity = Assert.Single(vActivity, e => e.Kind == "refusal");
        Assert.False(vRefusalActivity.Succeeded);
    }

    /// <summary>
    /// When the chosen model (first in its tier's own fallback chain, REQ-FN-020) is rate-limited,
    /// then <see cref="AgentActions.SendAsync"/> steps aside to the next model in that chain and the
    /// reply names the model that actually answered — not the one that was limited (REQ-FN-021,
    /// REQ-FN-026). Also asserts an activity event records which model stepped aside and why, since
    /// REQ-FN-021 says Chatur "says which model answered", and the activity panel (REQ-UI-022) is
    /// where that would be seen.
    /// </summary>
    [Fact]
    public async Task SendAsyncStepsAsideToTheNextModelInTheChainWhenTheFirstIsRateLimited()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-j");
        var vRoleId = GetRoleId("flow-master");
        var vLimitedModelId = InsertConnectedModel(2, "model-limited", "Limited Provider");
        var vFallbackModelId = InsertConnectedModel(2, "model-fallback", "Fallback Provider");

        var vRouting = new RoutingActions(objDb);
        await vRouting.ReorderChainAsync(2, [vLimitedModelId, vFallbackModelId]);

        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vLimitedProvider = new FakeLlmProvider(new HttpRequestException("Rate limited by upstream.", null, HttpStatusCode.TooManyRequests))
        {
            ModelNameOverride = "model-limited"
        };
        var vFallbackProvider = new FakeLlmProvider(new LlmResponse { Content = "Answered by the fallback.", Usage = new TokenUsage { InputTokens = 2, OutputTokens = 2 } })
        {
            ModelNameOverride = "model-fallback"
        };

        var vFactory = new RoutingFakeLlmProviderFactory(new Dictionary<int, ILlmProvider>
        {
            [vLimitedModelId] = vLimitedProvider,
            [vFallbackModelId] = vFallbackProvider,
        });

        var vSut = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, vFactory, objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Hi", null, vLimitedModelId, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
        }

        Assert.Equal("Answered by the fallback.", string.Concat(vChunks.Select(c => c.TextDelta)));
        Assert.Equal("model-fallback", vChunks[^1].ModelName);

        using var vConnection = objDb.OpenConnection();
        var vSteppingAsideNote = vConnection.QuerySingleOrDefault<string>(
            "SELECT Payload FROM SessionEvent WHERE SessionId = @vSessionId AND Kind = 'tool' AND Payload LIKE '%limited or unavailable%';",
            new { vSessionId });
        Assert.NotNull(vSteppingAsideNote);
    }

    /// <summary>
    /// When the project's build fails, for the same session, the set number of times in a row right
    /// after an approved change — the honest reading of "the same fix failing" this fix chose (see
    /// this row's checklist Remark) — then the second failure reaches the threshold and
    /// <see cref="AgentActions"/> actually climbs <c>"write-code"</c> to the next tier and records why
    /// (REQ-FN-022), rather than the first failure alone (below the threshold) doing anything.
    /// </summary>
    [Fact(DisplayName = "REQ-FN-022 repeated failed builds after an approved change climb the work kind a tier and record why")]
    public async Task RunBuildEscalatesTheWorkKindAfterRepeatedFailedBuildsFollowingAnApprovedChange()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-k");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);
        InsertApprovedChange(vSessionId);

        var vRouting = new RoutingActions(objDb);
        await vRouting.SetEscalationThresholdAsync(2);

        objProcessLauncher.ExitCode = 1; // every build in this test fails

        ToolCall NewBuildCall() => new() { Id = "call-build", Name = AgentToolNames.RunBuild, ArgumentsJson = "{}" };
        LlmResponse NewToolResponse() => new() { ToolCalls = [NewBuildCall()], Usage = new TokenUsage() };
        LlmResponse NewFinalResponse() => new() { Content = "Still failing.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } };

        // First failed build: one failure, below the threshold of 2 — no escalation yet.
        var vSut1 = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(new FakeLlmProvider(NewToolResponse(), NewFinalResponse())), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);
        await foreach (var _ in vSut1.SendAsync(vSessionId, "Please rebuild.", null, null, SessionMode.GoAhead))
        {
        }

        Assert.Empty(ReadRoutingEscalations());

        // Second failed build reaches the threshold and climbs the tier.
        var vSut2 = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(new FakeLlmProvider(NewToolResponse(), NewFinalResponse())), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);
        await foreach (var _ in vSut2.SendAsync(vSessionId, "Please rebuild again.", null, null, SessionMode.GoAhead))
        {
        }

        var vEscalation = Assert.Single(ReadRoutingEscalations());
        Assert.Equal("write-code", vEscalation.WorkKind);
        Assert.Equal(2, vEscalation.FromTier);
        Assert.Equal(3, vEscalation.ToTier);

        var vTiers = await vRouting.WorkTiersAsync();
        Assert.Equal(3, vTiers["write-code"]);
    }

    /// <summary>
    /// When the same repeated build failure is fixed (a later build succeeds), then the failure streak
    /// resets — a build that finally works must not silently count towards a future escalation
    /// (REQ-FN-022).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-022 a build that succeeds resets the failure streak")]
    public async Task RunBuildResetsTheFailureStreakOnceTheBuildSucceeds()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-l");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);
        InsertApprovedChange(vSessionId);

        var vRouting = new RoutingActions(objDb);
        await vRouting.SetEscalationThresholdAsync(2);

        ToolCall NewBuildCall() => new() { Id = "call-build", Name = AgentToolNames.RunBuild, ArgumentsJson = "{}" };

        objProcessLauncher.ExitCode = 1;
        var vSutFail = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(new FakeLlmProvider(
            new LlmResponse { ToolCalls = [NewBuildCall()], Usage = new TokenUsage() },
            new LlmResponse { Content = "Failed.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } })), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);
        await foreach (var _ in vSutFail.SendAsync(vSessionId, "Rebuild.", null, null, SessionMode.GoAhead))
        {
        }

        objProcessLauncher.ExitCode = 0; // the fix lands
        var vSutSucceed = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(new FakeLlmProvider(
            new LlmResponse { ToolCalls = [NewBuildCall()], Usage = new TokenUsage() },
            new LlmResponse { Content = "Fixed.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } })), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);
        await foreach (var _ in vSutSucceed.SendAsync(vSessionId, "Rebuild again.", null, null, SessionMode.GoAhead))
        {
        }

        // A third failure alone must not escalate — the streak reset when the build succeeded.
        objProcessLauncher.ExitCode = 1;
        var vSutFailAgain = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(new FakeLlmProvider(
            new LlmResponse { ToolCalls = [NewBuildCall()], Usage = new TokenUsage() },
            new LlmResponse { Content = "Failed again.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } })), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);
        await foreach (var _ in vSutFailAgain.SendAsync(vSessionId, "Rebuild once more.", null, null, SessionMode.GoAhead))
        {
        }

        Assert.Empty(ReadRoutingEscalations());
    }

    private void InsertApprovedChange(int aSessionId)
    {
        using var vConnection = objDb.OpenConnection();
        vConnection.Execute(
            "INSERT INTO Change (SessionId, FilePath, Before, After, Status) VALUES (@aSessionId, 'a.cs', 'old', 'new', 'Approved');",
            new { aSessionId });
    }

    // Dapper's constructor-based materialization needs an exact type match against SQLite's own
    // INTEGER width (Int64) — the same reason every Dapper row record in AgentActions.cs itself takes
    // `long` and narrows to `int` afterwards.
    private sealed record RoutingEscalationRawRow(string WorkKind, long FromTier, long ToTier, string Reason);

    private sealed record RoutingEscalationRow(string WorkKind, int FromTier, int ToTier, string Reason);

    private IReadOnlyList<RoutingEscalationRow> ReadRoutingEscalations()
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.Query<RoutingEscalationRawRow>("SELECT WorkKind, FromTier, ToTier, Reason FROM RoutingEscalation;")
            .Select(r => new RoutingEscalationRow(r.WorkKind, (int)r.FromTier, (int)r.ToTier, r.Reason))
            .ToList();
    }

    /// <summary>
    /// When the model is still writing, then the first text delta reaches the caller before the model
    /// has finished — proven by holding the fake model open after its first delta until the caller has
    /// read that delta — and the whole reply and its token count still arrive intact (REQ-FN-027,
    /// REQ-FN-026).
    /// </summary>
    [Fact]
    public async Task SendAsyncYieldsEachDeltaAsItArrives()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-m");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);

        var vGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vProvider = new FakeLlmProvider(
            new LlmResponse { Content = "abcdef", Usage = new TokenUsage { InputTokens = 3, OutputTokens = 4 } })
        {
            GateAfterFirstDelta = vGate
        };
        var vSut = CreateSut(vProvider);

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Hi", null, null, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
            if (vChunks.Count == 1)
            {
                Assert.Equal("abc", vChunk.TextDelta);
                Assert.False(vChunk.IsFinal);
                vGate.SetResult(); // only now may the model finish
            }
        }

        Assert.Equal("abcdef", string.Concat(vChunks.Select(c => c.TextDelta)));
        Assert.True(vChunks[^1].IsFinal);
        Assert.Equal(7, vChunks[^1].TokensUsed);
    }

    /// <summary>
    /// When a turn uses a tool and then answers, then the tool call still passes the guards and runs,
    /// the text of both model turns is shown in order, and the token count and model name come from the
    /// completed events of the whole reply (REQ-FN-027, REQ-FN-026, REQ-FN-036).
    /// </summary>
    [Fact]
    public async Task SendAsyncStreamsTheTextOfEveryTurnAroundAToolCall()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-n");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                Content = "Editing now.",
                ToolCalls = [new ToolCall { Id = "call-1", Name = AgentToolNames.EditFile, ArgumentsJson = """{"path":"n.txt","content":"n"}""" }],
                Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 }
            },
            new LlmResponse { Content = "Done.", Usage = new TokenUsage { InputTokens = 2, OutputTokens = 2 } });
        var vSut = CreateSut(vProvider);

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Edit n.txt", null, null, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
        }

        Assert.Equal("Editing now.\n\nDone.", string.Concat(vChunks.Select(c => c.TextDelta)));
        Assert.Equal(6, vChunks[^1].TokensUsed);
        Assert.Equal("fake-model", vChunks[^1].ModelName);
        Assert.Contains(objFiles.Saved, s => s.Path == "n.txt");
    }

    /// <summary>
    /// When a model fails after it has already shown text, then the call is not replayed on the next
    /// model in the chain — the failure is reported after the text already shown, and the reply names
    /// no model (REQ-FN-021, REQ-FN-027).
    /// </summary>
    [Fact]
    public async Task SendAsyncDoesNotReplayOnAnotherModelOnceTextHasBeenShown()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-o");
        var vFirstModelId = InsertConnectedModel(2, "model-first", "First Provider");
        var vSecondModelId = InsertConnectedModel(2, "model-second", "Second Provider");
        var vRouting = new RoutingActions(objDb);
        await vRouting.ReorderChainAsync(2, [vFirstModelId, vSecondModelId]);
        var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);

        var vFirst = new FakeLlmProvider(new LlmResponse { Content = "abcdef", Usage = new TokenUsage() })
        {
            FailAfterFirstDelta = new HttpRequestException("Overloaded.", null, HttpStatusCode.ServiceUnavailable)
        };
        var vSecond = new FakeLlmProvider(new LlmResponse { Content = "never shown", Usage = new TokenUsage() });
        var vFactory = new RoutingFakeLlmProviderFactory(new Dictionary<int, ILlmProvider>
        {
            [vFirstModelId] = vFirst,
            [vSecondModelId] = vSecond,
        });
        var vSut = new AgentActions(objDb, new AgentSessionRegistry(), objGuards, vFactory, objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), vRouting);

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Hi", null, vFirstModelId, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
        }

        var vText = string.Concat(vChunks.Select(c => c.TextDelta));
        Assert.StartsWith("abc", vText);
        Assert.Contains("First Provider did not answer: Overloaded.", vText);
        Assert.DoesNotContain("never shown", vText);
        Assert.Equal(0, vSecond.StreamCount);
        Assert.Null(vChunks[^1].ModelName);
    }

    /// <summary>
    /// When the owner stops a session while the model is mid-stream, then the send ends without a final
    /// chunk, the model's wait is cancelled, and the text shown so far stays on record (REQ-UI-023,
    /// REQ-FN-027).
    /// </summary>
    [Fact]
    public async Task SendAsyncStopsMidStreamWhenTheSessionIsStopped()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-p");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);

        var vProvider = new FakeLlmProvider(new LlmResponse { Content = "abcdef", Usage = new TokenUsage() })
        {
            GateAfterFirstDelta = new TaskCompletionSource() // never released: only a stop ends the wait
        };
        var vRegistry = new AgentSessionRegistry();
        var vSut = new AgentActions(objDb, vRegistry, objGuards, new FakeLlmProviderFactory(vProvider), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), new RoutingActions(objDb));

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vChunk in vSut.SendAsync(vSessionId, "Hi", null, null, SessionMode.GoAhead))
        {
            vChunks.Add(vChunk);
            vRegistry.Cancel(vSessionId);
        }

        var vOnly = Assert.Single(vChunks);
        Assert.Equal("abc", vOnly.TextDelta);
        Assert.DoesNotContain(vChunks, c => c.IsFinal);

        using var vConnection = objDb.OpenConnection();
        var vRecorded = vConnection.QuerySingleOrDefault<string>(
            "SELECT Payload FROM SessionEvent WHERE SessionId = @vSessionId AND Kind = 'assistant';", new { vSessionId });
        Assert.Equal("abc", vRecorded);
    }

    /// <summary>
    /// When the owner stops a session, then the session is marked Stopped and exactly one "stopped"
    /// step is recorded after the earlier events for the activity panel to show (REQ-UI-023).
    /// </summary>
    [Fact]
    public async Task StopAsyncRecordsAStoppedActivityStep()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-stop");
        var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);
        var vSut = CreateSut(new FakeLlmProvider());

        await vSut.StopAsync(vSessionId);

        using var vConnection = objDb.OpenConnection();
        var vState = vConnection.QuerySingle<string>("SELECT State FROM Session WHERE SessionId = @vSessionId;", new { vSessionId });
        Assert.Equal("Stopped", vState);

        var vStopped = vConnection.Query<string>(
            "SELECT Payload FROM SessionEvent WHERE SessionId = @vSessionId AND Kind = 'stopped';", new { vSessionId }).ToList();
        Assert.Equal("Stopped by the owner.", Assert.Single(vStopped));
    }

    /// <summary>When no provider is connected, then the reply says so in one piece rather than failing outright (graceful degradation; "needs a provider key (owner)").</summary>
    [Fact]
    public async Task SendAsyncSaysNoProviderIsConfiguredWhenNoneAreConnected()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-f");
        var vRoleId = GetRoleId("architect");
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vSut = CreateSut(new FakeLlmProvider());

        var vChunks = new List<AgentReplyChunk>();
        await foreach (var vReplyChunk in vSut.SendAsync(vSessionId, "Hello", null, null, SessionMode.GoAhead))
        {
            vChunks.Add(vReplyChunk);
        }

        var vOnlyChunk = Assert.Single(vChunks);
        Assert.Contains("No model provider is configured", vOnlyChunk.TextDelta);
        Assert.True(vOnlyChunk.IsFinal);
    }

    /// <summary>When a project has never had a session, then the Analyst is suggested, with a reason (REQ-FN-024).</summary>
    [Fact]
    public async Task SuggestAgentAsyncSuggestsTheAnalystWhenNoSessionExists()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-g");
        var vSut = CreateSut(new FakeLlmProvider());

        var vSuggestion = await vSut.SuggestAgentAsync(vProjectId);

        Assert.Equal(GetRoleId("analyst"), vSuggestion.RoleId);
        Assert.Contains("analyst", vSuggestion.Why, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>When the last session left nothing waiting, then that same role is suggested again, with a reason naming it (REQ-FN-024).</summary>
    [Fact]
    public async Task SuggestAgentAsyncContinuesTheLastRoleWhenNothingIsWaiting()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-h");
        var vRoleId = GetRoleId("architect");
        InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vSut = CreateSut(new FakeLlmProvider());
        var vSuggestion = await vSut.SuggestAgentAsync(vProjectId);

        Assert.Equal(vRoleId, vSuggestion.RoleId);
        Assert.Contains("Architect", vSuggestion.Why);
    }

    /// <summary>
    /// When the owner starts a new conversation, then it is the newest session with the chosen role,
    /// it holds none of the earlier conversation, and the earlier one is still in the history
    /// (REQ-UI-031, mockups/main.html <c>new-chat</c>).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-031 a new conversation keeps the earlier one in the history")]
    public async Task StartSessionAsyncKeepsTheEarlierOneInTheHistory()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-new-chat");
        var vAnalyst = GetRoleId("analyst");
        var vFlowMaster = GetRoleId("flow-master");
        var vSut = CreateSut(new FakeLlmProvider(new LlmResponse { Content = "Hi.", Usage = new TokenUsage() }));
        var vFirst = await vSut.EnsureActiveSessionAsync(vProjectId);

        var vNew = await vSut.StartSessionAsync(vProjectId, vFlowMaster);

        Assert.NotEqual(vFirst.SessionId, vNew.SessionId);
        Assert.Equal("Flow master", vNew.RoleName, ignoreCase: true);
        Assert.Empty((await vSut.ContinueSessionAsync(vNew.SessionId)).Events);
        var vSessions = await vSut.SessionsAsync(vProjectId);
        Assert.Equal(2, vSessions.Count);
        Assert.Contains(vSessions, s => s.SessionId == vFirst.SessionId);
        Assert.NotEqual(vAnalyst, vFlowMaster);
    }

    /// <summary>
    /// When a session is continued, then its own summary and every event it recorded come back in
    /// order — the same record <see cref="AgentActions.SendAsync"/> wrote as the turn ran, so a
    /// restart shows exactly what happened (REQ-FN-031, REQ-FN-032).
    /// </summary>
    [Fact]
    public async Task ContinueSessionAsyncReturnsEveryRecordedEventInOrder()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-i");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);

        var vSut = CreateSut(new FakeLlmProvider(new LlmResponse { Content = "Hi.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } }));
        await foreach (var _ in vSut.SendAsync(vSessionId, "Hello", null, null, SessionMode.GoAhead))
        {
        }

        var vDetail = await vSut.ContinueSessionAsync(vSessionId);

        Assert.Equal(vSessionId, vDetail.Summary.SessionId);
        Assert.Equal("Flow master", vDetail.Summary.RoleName);
        Assert.True(vDetail.Events.Count >= 2);
        Assert.Equal("user", vDetail.Events[0].Kind);
        Assert.Equal("Hello", vDetail.Events[0].Payload);
        Assert.Contains(vDetail.Events, e => e.Kind == "assistant" && e.Payload == "Hi.");
    }

    /// <summary>Deletes the temporary database file this test created.</summary>
    /// <summary>
    /// When the model calls <c>correct-wording</c> for a role, then the call passes the guards, the
    /// role's wording changes as a versioned save, <c>CorrectionActions.ListAsync</c> returns a row
    /// with the before and after wording, the reason and when (REQ-UI-034), and undoing that row puts
    /// the old wording back (REQ-FN-038).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-034 an agent correction changes the role wording and logs a correction")]
    public async Task SendAsyncCorrectsARoleWordingAndLogsTheCorrection()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-correct");
        var vRoleId = GetRoleId("verifier");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.AskFirst);
        var vOldWording = ReadRoleWording("analyst");
        const string vNewWording = "Reads the project and the BRD, asks the owner when a requirement is unclear, and never changes code.";

        var vArguments = System.Text.Json.JsonSerializer.Serialize(new { kind = "role", target = "analyst", wording = vNewWording, reason = "It never said to ask when unsure." });
        var vProvider = new FakeLlmProvider(
            new LlmResponse { ToolCalls = [new ToolCall { Id = "call-c", Name = AgentToolNames.CorrectWording, ArgumentsJson = vArguments }], Usage = new TokenUsage() },
            new LlmResponse { Content = "Corrected.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });
        var vSut = CreateSut(vProvider);

        var vBefore = DateTime.UtcNow.AddSeconds(-1);
        await foreach (var _ in vSut.SendAsync(vSessionId, "Fix the analyst wording.", null, null, SessionMode.AskFirst))
        {
        }

        Assert.Equal(vNewWording, ReadRoleWording("analyst"));
        var vCorrection = Assert.Single(await objCorrections.ListAsync());
        Assert.Equal("analyst", vCorrection.Target);
        Assert.Equal(vOldWording, vCorrection.Before);
        Assert.Equal(vNewWording, vCorrection.After);
        Assert.Equal("It never said to ask when unsure.", vCorrection.Why);
        Assert.Equal("Proposed", vCorrection.Status);
        Assert.True(vCorrection.CreatedUtc >= vBefore);
        Assert.Equal(0, CountSessionEvents(vSessionId, "refusal"));

        await objCorrections.UndoAsync(vCorrection.CorrectionId);

        Assert.Equal(vOldWording, ReadRoleWording("analyst"));
        Assert.Equal("Undone", Assert.Single(await objCorrections.ListAsync()).Status);
    }

    /// <summary>
    /// When the model calls <c>correct-wording</c> for a rule and for a process step, then each
    /// text changes, each gains a correction row, and undoing them puts the old text back (REQ-UI-034).
    /// </summary>
    [Fact(DisplayName = "REQ-UI-034 a rule and a step correction are logged and undo restores them")]
    public async Task SendAsyncCorrectsARuleAndAStepAndUndoRestoresThem()
    {
        var vProjectId = InsertProject("/tmp/chatur-tests/proj-correct-rule");
        var vRoleId = GetRoleId("flow-master");
        InsertConnectedModel(1);
        var vSessionId = InsertSession(vProjectId, vRoleId, SessionMode.GoAhead);
        int vRuleId;
        using (var vConnection = objDb.OpenConnection())
        {
            vRuleId = vConnection.ExecuteScalar<int>("SELECT MIN(RuleId) FROM Rule;");
            vConnection.Execute("INSERT INTO ProcessStepText (ProcessCode, StepCode, Text, Version) VALUES ('build-phase', 'smoke', 'Smoke it once.', 1);");
        }

        var vOldRule = ReadRuleText(vRuleId);
        var vRuleArguments = System.Text.Json.JsonSerializer.Serialize(new { kind = "rule", target = vRuleId.ToString(), wording = "A reworded rule.", reason = "It was ambiguous." });
        var vStepArguments = System.Text.Json.JsonSerializer.Serialize(new { kind = "step", target = "build-phase/smoke", wording = "Smoke it at two widths.", reason = "One width missed overlap." });
        var vProvider = new FakeLlmProvider(
            new LlmResponse
            {
                ToolCalls =
                [
                    new ToolCall { Id = "call-r", Name = AgentToolNames.CorrectWording, ArgumentsJson = vRuleArguments },
                    new ToolCall { Id = "call-s", Name = AgentToolNames.CorrectWording, ArgumentsJson = vStepArguments }
                ],
                Usage = new TokenUsage()
            },
            new LlmResponse { Content = "Done.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });

        await foreach (var _ in CreateSut(vProvider).SendAsync(vSessionId, "Fix the wording.", null, null, SessionMode.GoAhead))
        {
        }

        Assert.Equal("A reworded rule.", ReadRuleText(vRuleId));
        Assert.Equal("Smoke it at two widths.", ReadStepText("build-phase", "smoke"));
        var vCorrections = await objCorrections.ListAsync();
        Assert.Equal(2, vCorrections.Count);

        foreach (var vCorrection in vCorrections)
        {
            await objCorrections.UndoAsync(vCorrection.CorrectionId);
        }

        Assert.Equal(vOldRule, ReadRuleText(vRuleId));
        Assert.Equal("Smoke it once.", ReadStepText("build-phase", "smoke"));
    }

    /// <summary>
    /// When a session turn runs in a project, then the project's <c>docs/metrics</c> holds all five
    /// files and <c>sessions.jsonl</c> carries one line signed <c>"tool":"chatur"</c> with the session
    /// id, model and tokens, and everything unmeasured empty (REQ-FN-040, REQ-FN-041, REQ-FN-042).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-040 a session turn writes the five metrics files")]
    public async Task SendAsyncWritesASessionRecordIntoTheProjectMetrics()
    {
        var vFolder = Path.Combine(Path.GetTempPath(), $"chatur-metrics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vFolder);
        try
        {
            var vProjectId = InsertProject(vFolder);
            InsertConnectedModel(1);
            var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);
            var vProvider = new FakeLlmProvider(
                new LlmResponse { Content = "Hello.", Usage = new TokenUsage { InputTokens = 10, OutputTokens = 5 } });

            await foreach (var _ in CreateSut(vProvider).SendAsync(vSessionId, "Hi", null, null, SessionMode.GoAhead))
            {
            }

            var vMetrics = Path.Combine(vFolder, "docs", "metrics");
            foreach (var vName in new[] { "runs", "gates", "sessions", "commits", "misses" })
            {
                Assert.True(File.Exists(Path.Combine(vMetrics, vName + ".jsonl")), vName);
            }

            var vLine = Assert.Single(File.ReadAllLines(Path.Combine(vMetrics, "sessions.jsonl")));
            Assert.Contains("\"tool\":\"chatur\"", vLine);
            Assert.Contains("\"kind\":\"session\"", vLine);
            Assert.Contains($"\"session_id\":\"{vSessionId}\"", vLine);
            Assert.Contains("\"model\":\"fake-model\"", vLine);
            Assert.Contains("\"input_tokens\":\"10\"", vLine);
            Assert.Contains("\"output_tokens\":\"5\"", vLine);
            Assert.Contains("\"cost_usd\":\"\"", vLine);
        }
        finally
        {
            Directory.Delete(vFolder, true);
        }
    }

    /// <summary>
    /// When no model provider is configured, then the turn still counts as a session that ran: the five
    /// files exist and the sessions line has an empty model (REQ-FN-040, REQ-FN-042).
    /// </summary>
    [Fact(DisplayName = "REQ-FN-041 REQ-FN-042 a no-provider turn writes a chatur-signed record with unmeasured fields empty")]
    public async Task SendAsyncWritesASessionRecordWhenNoProviderIsConfigured()
    {
        var vFolder = Path.Combine(Path.GetTempPath(), $"chatur-metrics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vFolder);
        try
        {
            var vProjectId = InsertProject(vFolder);
            var vSessionId = InsertSession(vProjectId, GetRoleId("analyst"), SessionMode.AskFirst);

            await foreach (var _ in CreateSut(new FakeLlmProvider()).SendAsync(vSessionId, "Hi", null, null, SessionMode.AskFirst))
            {
            }

            var vLine = Assert.Single(File.ReadAllLines(Path.Combine(vFolder, "docs", "metrics", "sessions.jsonl")));
            Assert.Contains("\"tool\":\"chatur\"", vLine);
            Assert.Contains("\"model\":\"\"", vLine);
            Assert.True(File.Exists(Path.Combine(vFolder, "docs", "metrics", "runs.jsonl")));
        }
        finally
        {
            Directory.Delete(vFolder, true);
        }
    }

    /// <summary>
    /// When the agent runs a build, then a <c>runs</c> record with the command and its result is written
    /// (REQ-FN-040), and a project whose metrics folder cannot be written never breaks the turn (REQ-UI-035).
    /// </summary>
    [Fact]
    public async Task SendAsyncWritesARunRecordForABuildAndSurvivesAnUnwritableMetricsFolder()
    {
        var vFolder = Path.Combine(Path.GetTempPath(), $"chatur-metrics-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vFolder);
        try
        {
            var vProjectId = InsertProject(vFolder);
            InsertConnectedModel(1);
            var vSessionId = InsertSession(vProjectId, GetRoleId("flow-master"), SessionMode.GoAhead);
            objProcessLauncher.ExitCode = 0;
            var vProvider = new FakeLlmProvider(
                new LlmResponse { ToolCalls = [new ToolCall { Id = "call-b", Name = AgentToolNames.RunBuild, ArgumentsJson = "{}" }], Usage = new TokenUsage() },
                new LlmResponse { Content = "Built.", Usage = new TokenUsage { InputTokens = 1, OutputTokens = 1 } });

            await foreach (var _ in CreateSut(vProvider).SendAsync(vSessionId, "Build it.", null, null, SessionMode.GoAhead))
            {
            }

            var vRun = Assert.Single(File.ReadAllLines(Path.Combine(vFolder, "docs", "metrics", "runs.jsonl")));
            Assert.Contains("\"cmd\":\"dotnet build\"", vRun);
            Assert.Contains("\"build_result\":\"pass\"", vRun);
            Assert.Contains("\"mode\":\"\"", vRun);
        }
        finally
        {
            Directory.Delete(vFolder, true);
        }

        // A project folder that is a file (so docs/metrics cannot exist) must not break the turn.
        var vBlocker = Path.Combine(Path.GetTempPath(), $"chatur-blocker-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(vBlocker, "x");
        try
        {
            var vProjectId = InsertProject(vBlocker);
            var vSessionId = InsertSession(vProjectId, GetRoleId("analyst"), SessionMode.AskFirst);
            var vChunks = new List<AgentReplyChunk>();
            await foreach (var vChunk in CreateSut(new FakeLlmProvider()).SendAsync(vSessionId, "Hi", null, null, SessionMode.AskFirst))
            {
                vChunks.Add(vChunk);
            }

            Assert.True(vChunks[^1].IsFinal);
        }
        finally
        {
            File.Delete(vBlocker);
        }
    }

    private string ReadRoleWording(string aCode)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.QuerySingle<string>("SELECT Wording FROM Role WHERE Code = @aCode;", new { aCode });
    }

    private string ReadRuleText(int aRuleId)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.QuerySingle<string>("SELECT Text FROM Rule WHERE RuleId = @aRuleId;", new { aRuleId });
    }

    private string ReadStepText(string aProcessCode, string aStepCode)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.QuerySingle<string>("SELECT Text FROM ProcessStepText WHERE ProcessCode = @aProcessCode AND StepCode = @aStepCode;", new { aProcessCode, aStepCode });
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }
    }

    private AgentActions CreateSut(FakeLlmProvider aProvider) =>
        new(objDb, new AgentSessionRegistry(), objGuards, new FakeLlmProviderFactory(aProvider), objFiles, objProcessLauncher, objCorrections, objMeasurements, new SystemClock(), new RoutingActions(objDb));

    private int InsertProject(string aPath)
    {
        using var vConnection = objDb.OpenConnection();
        var vId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Project (Name, Path, Kind) VALUES (@Name, @aPath, 'Folder'); SELECT last_insert_rowid();",
            new { Name = Path.GetFileName(aPath), aPath });
        return (int)vId;
    }

    private int InsertSession(int aProjectId, int aRoleId, SessionMode aMode)
    {
        using var vConnection = objDb.OpenConnection();
        var vId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@aProjectId, @aRoleId, @vMode, 'Active', @vStartedUtc); SELECT last_insert_rowid();",
            new { aProjectId, aRoleId, vMode = aMode.ToString(), vStartedUtc = DateTime.UtcNow.ToString("O") });
        return (int)vId;
    }

    private int InsertConnectedModel(int aTier, string aIdentifier = "fake-identifier", string aProviderName = "Test Provider")
    {
        using var vConnection = objDb.OpenConnection();
        var vProviderId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Provider (Name, Connector, SignInMethod, BaseUrl, SecretName, State) VALUES (@aProviderName, 'Fake', 'Key', 'http://localhost', NULL, 'Connected'); SELECT last_insert_rowid();",
            new { aProviderName });
        var vModelId = vConnection.ExecuteScalar<long>(
            "INSERT INTO Model (ProviderId, Tier, Identifier) VALUES (@vProviderId, @aTier, @aIdentifier); SELECT last_insert_rowid();",
            new { vProviderId, aTier, aIdentifier });
        return (int)vModelId;
    }

    private int GetRoleId(string aCode)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.QuerySingleOrDefault<int?>("SELECT RoleId FROM Role WHERE Code = @aCode;", new { aCode })
            ?? throw new InvalidOperationException($"Role \"{aCode}\" was not seeded.");
    }

    private string? ReadChangeStatus(int aSessionId)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.QuerySingleOrDefault<string>("SELECT Status FROM Change WHERE SessionId = @aSessionId;", new { aSessionId });
    }

    private long CountSessionEvents(int aSessionId, string aKind)
    {
        using var vConnection = objDb.OpenConnection();
        return vConnection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM SessionEvent WHERE SessionId = @aSessionId AND Kind = @aKind;", new { aSessionId, aKind });
    }

    private sealed class TestDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string objConnectionString;

        public TestDbConnectionFactory(string aConnectionString) => objConnectionString = aConnectionString;

        public IDbConnection OpenConnection()
        {
            var vConnection = new SqliteConnection(objConnectionString);
            vConnection.Open();
            using var vPragma = vConnection.CreateCommand();
            vPragma.CommandText = "PRAGMA foreign_keys = ON;";
            vPragma.ExecuteNonQuery();
            return vConnection;
        }
    }

    private sealed class FakeFileActions : IFileActions
    {
        private readonly Dictionary<(int ProjectId, string Path), string> objFiles = new();

        public List<(int ProjectId, string Path, string Content)> Saved { get; } = [];

        public Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default) =>
            throw new NotSupportedException("Not used by AgentActions.");

        public Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) =>
            objFiles.TryGetValue((aProjectId, aRelativePath), out var vContent)
                ? Task.FromResult(vContent)
                : Task.FromException<string>(new FileNotFoundException(aRelativePath));

        public Task SaveAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default)
        {
            objFiles[(aProjectId, aRelativePath)] = aContent;
            Saved.Add((aProjectId, aRelativePath, aContent));
            return Task.CompletedTask;
        }

        public Task OpenInEditorAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) =>
            throw new NotSupportedException("Not used by AgentActions.");

        public Task OpenWithDefaultAppAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) =>
            throw new NotSupportedException("Not used by AgentActions.");
    }

    private sealed class FakeProcessLauncher : IProcessLauncher
    {
        /// <summary>The exit code the next <see cref="RunAsync"/> reports — 0 (success) unless a test needs a failing build (REQ-FN-022).</summary>
        public int ExitCode { get; set; }

        public Task OpenInEditorAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task OpenWithDefaultAppAsync(string aFilePath, CancellationToken aCt = default) => Task.CompletedTask;

        public Task<int> RunAsync(string aCommand, string aArguments, string aWorkingDirectory, Action<string> aOnOutputLine, CancellationToken aCt = default)
        {
            aOnOutputLine("build output");
            return Task.FromResult(ExitCode);
        }
    }

    private sealed class FakeLlmProviderFactory : IAgentLlmProviderFactory
    {
        private readonly ILlmProvider objProvider;

        public FakeLlmProviderFactory(ILlmProvider aProvider) => objProvider = aProvider;

        public Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default) => Task.FromResult(objProvider);
    }

    /// <summary>
    /// Hands back a different fake provider per <see cref="ConnectedModel.ModelId"/> — the fallback
    /// chain (REQ-FN-021) walks several real model ids, so a single-provider fake cannot tell them
    /// apart.
    /// </summary>
    private sealed class RoutingFakeLlmProviderFactory : IAgentLlmProviderFactory
    {
        private readonly IReadOnlyDictionary<int, ILlmProvider> objProvidersByModelId;

        public RoutingFakeLlmProviderFactory(IReadOnlyDictionary<int, ILlmProvider> aProvidersByModelId) => objProvidersByModelId = aProvidersByModelId;

        public Task<ILlmProvider> CreateAsync(ConnectedModel aModel, CancellationToken aCt = default) =>
            objProvidersByModelId.TryGetValue(aModel.ModelId, out var vProvider)
                ? Task.FromResult(vProvider)
                : throw new InvalidOperationException($"The test did not set up a fake provider for model {aModel.ModelId}.");
    }

    /// <summary>
    /// A scripted streaming chat client standing in for a real TechieRag provider (Coding Standards
    /// §Testability). Each queued <see cref="LlmResponse"/> is one model call, streamed as two text
    /// deltas (its content split in half), then its tool calls, then the completed event.
    /// </summary>
    private sealed class FakeLlmProvider : ILlmProvider
    {
        private readonly Queue<LlmResponse> objResponses;
        private readonly Exception? objFailFirstWith;
        private bool objHasFailedOnce;

        public FakeLlmProvider(params LlmResponse[] aResponses)
        {
            objResponses = new Queue<LlmResponse>(aResponses);
        }

        /// <summary>Throws <paramref name="aFailure"/> when the first stream is read, before any delta, standing in for a real model that is rate-limited or unavailable (REQ-FN-021).</summary>
        public FakeLlmProvider(Exception aFailure)
        {
            objResponses = new Queue<LlmResponse>();
            objFailFirstWith = aFailure;
        }

        public string Name => "Fake";

        /// <summary>Overrides <see cref="ModelName"/> for a test that must tell several fake models apart (REQ-FN-021, REQ-FN-026).</summary>
        public string? ModelNameOverride { get; init; }

        /// <summary>When set, the stream throws this after its first delta has been yielded (a model that fails part-way).</summary>
        public Exception? FailAfterFirstDelta { get; init; }

        /// <summary>When set, the stream waits on this after its first delta, so a test can observe that delta before the model "finishes".</summary>
        public TaskCompletionSource? GateAfterFirstDelta { get; init; }

        /// <summary>How many times a stream was opened on this provider.</summary>
        public int StreamCount { get; private set; }

        public string ModelName => ModelNameOverride ?? "fake-model";

        public bool SupportsToolCalling => true;

        public bool SupportsStreaming => true;

#pragma warning disable CS0067 // never raised by this fake; the interface requires it
        public event EventHandler<LlmCompletionEventArgs>? OnCompletionCompleted;
#pragma warning restore CS0067

        public Task<LlmResponse> CompleteAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AgentActions does not call CompleteAsync.");

        public IAsyncEnumerable<string> CompleteStreamAsync(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AgentActions does not call CompleteStreamAsync.");

        public Task<LlmResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AgentActions streams every turn through ChatStreamEventsAsync (REQ-FN-027).");

        public IAsyncEnumerable<string> ChatStreamAsync(IReadOnlyList<ChatMessage> messages, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AgentActions streams every turn through ChatStreamEventsAsync (REQ-FN-027).");

        public async IAsyncEnumerable<LlmStreamEvent> ChatStreamEventsAsync(
            IReadOnlyList<ChatMessage> messages,
            LlmCompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            StreamCount++;

            if (objFailFirstWith is not null && !objHasFailedOnce)
            {
                objHasFailedOnce = true;
                throw objFailFirstWith;
            }

            if (objResponses.Count == 0)
            {
                throw new InvalidOperationException("The test did not queue enough scripted responses.");
            }

            var vResponse = objResponses.Dequeue();
            var vContent = vResponse.Content ?? string.Empty;
            var vHalf = vContent.Length / 2;
            var vPieces = vContent.Length == 0 ? Array.Empty<string>() : new[] { vContent[..vHalf], vContent[vHalf..] }.Where(p => p.Length > 0).ToArray();

            for (var vIndex = 0; vIndex < vPieces.Length; vIndex++)
            {
                yield return LlmStreamEvent.FromText(vPieces[vIndex]);

                if (vIndex == 0 && GateAfterFirstDelta is not null)
                {
                    await GateAfterFirstDelta.Task.WaitAsync(cancellationToken);
                }

                if (vIndex == 0 && FailAfterFirstDelta is not null)
                {
                    throw FailAfterFirstDelta;
                }
            }

            foreach (var vToolCall in vResponse.ToolCalls ?? [])
            {
                yield return LlmStreamEvent.FromToolCall(vToolCall);
            }

            yield return LlmStreamEvent.FromCompleted(vResponse.Usage, vResponse.HasToolCalls ? "tool_calls" : "stop", ModelName);
        }

        public Task<T> CompleteAsync<T>(string prompt, LlmCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : class =>
            throw new NotSupportedException("AgentActions does not call the typed CompleteAsync.");

        public int EstimateTokenCount(string text) => text.Length / 4;
    }
}
