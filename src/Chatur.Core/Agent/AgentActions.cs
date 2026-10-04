using System.Data;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Guards;
using Chatur.Core.Platform;
using Dapper;
using TechieRag.Abstractions;
using TechieRag.Llm;
using TechieRag.Models;

namespace Chatur.Core.Agent;

/// <summary>
/// <see cref="IAgentActions"/> over TechieRag, the <c>Session</c>/<c>SessionEvent</c> tables and the
/// guard pipeline. Not built yet; see the individual method docs for the owning cluster.
/// </summary>
public sealed class AgentActions : IAgentActions
{
    /// <summary>
    /// The tool-calling turns one <see cref="SendAsync"/> call allows before it gives up and answers
    /// in words instead — a runaway loop must end, not run forever.
    /// </summary>
    private const int MaxToolIterations = 8;

    /// <summary>Written between the reply text of two model turns of one send (before and after a tool run), so the pieces do not run together.</summary>
    private const string TurnSeparator = "\n\n";

    private readonly IDbConnectionFactory objDb;
    private readonly AgentSessionRegistry objRegistry;
    private readonly GuardPipeline objGuards;
    private readonly IAgentLlmProviderFactory objLlmProviderFactory;
    private readonly IFileActions objFileActions;
    private readonly IProcessLauncher objProcessLauncher;
    private readonly ICorrectionActions objCorrections;
    private readonly IMeasurementActions objMeasurements;
    private readonly IClock objClock;
    private readonly IRoutingActions objRouting;

    /// <summary>
    /// Creates the action set.
    /// </summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aRegistry">The cross-request session cancellation a stop request reaches for.</param>
    /// <param name="aGuards">Every tool request passes this before it reaches a tool (REQ-FN-036).</param>
    /// <param name="aLlmProviderFactory">Builds the TechieRag provider for the model a turn uses.</param>
    /// <param name="aFileActions">Reads and writes the files a <c>read-file</c>/<c>edit-file</c> tool call names.</param>
    /// <param name="aProcessLauncher">Runs the project's own build for a <c>run-build</c> tool call.</param>
    /// <param name="aCorrections">Records the wording changes a <c>correct-wording</c> tool call makes (REQ-UI-034).</param>
    /// <param name="aMeasurements">Appends the session and build records to the project's <c>docs/metrics</c> streams (REQ-FN-040).</param>
    /// <param name="aClock">Timestamps a session event as it is recorded.</param>
    /// <param name="aRouting">Reads a tier's fallback chain so a limited or unavailable model steps aside for the next one (REQ-FN-021).</param>
    public AgentActions(
        IDbConnectionFactory aDb,
        AgentSessionRegistry aRegistry,
        GuardPipeline aGuards,
        IAgentLlmProviderFactory aLlmProviderFactory,
        IFileActions aFileActions,
        IProcessLauncher aProcessLauncher,
        ICorrectionActions aCorrections,
        IMeasurementActions aMeasurements,
        IClock aClock,
        IRoutingActions aRouting)
    {
        objDb = aDb;
        objRegistry = aRegistry;
        objGuards = aGuards;
        objLlmProviderFactory = aLlmProviderFactory;
        objFileActions = aFileActions;
        objProcessLauncher = aProcessLauncher;
        objCorrections = aCorrections;
        objMeasurements = aMeasurements;
        objClock = aClock;
        objRouting = aRouting;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Picks the role whose own work best suits the project's state (REQ-FN-024, Architecture §6
    /// "Chatur pre-selects the agent that suits the project's state and says why"): the Analyst when
    /// the project has never had a session, the same role as the busiest open thread when a change is
    /// still waiting for approval (finishing what was started before moving on), and otherwise the
    /// role of the most recent session (continuing the work already under way). Falls back to the
    /// Analyst when the project's seeded roles cannot be read at all, so the picker always has
    /// something selected rather than nothing (REQ-FN-024's acceptance needs a role, not a null).
    /// </remarks>
    public async Task<AgentSuggestion> SuggestAgentAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();

        var vLastSession = await vConnection.QuerySingleOrDefaultAsync<LastSessionRow>(
            new CommandDefinition(
                @"SELECT s.SessionId AS SessionId, s.RoleId AS RoleId, r.Name AS RoleName
                  FROM Session s
                  JOIN Role r ON r.RoleId = s.RoleId
                  WHERE s.ProjectId = @aProjectId
                  ORDER BY s.StartedUtc DESC
                  LIMIT 1;",
                new { aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vLastSession is null)
        {
            var vAnalystId = await vConnection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("SELECT RoleId FROM Role WHERE Code = 'analyst';", cancellationToken: aCt)).ConfigureAwait(false);
            return new AgentSuggestion(vAnalystId, "No session has been started for this project yet; the analyst begins by reading its requirements.");
        }

        var vHasProposedChange = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                @"SELECT 1 FROM Change c
                  JOIN Session s ON s.SessionId = c.SessionId
                  WHERE s.ProjectId = @aProjectId AND c.Status = 'Proposed'
                  LIMIT 1;",
                new { aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vLastRoleId = (int)vLastSession.RoleId;
        return vHasProposedChange is not null
            ? new AgentSuggestion(vLastRoleId, $"{vLastSession.RoleName} left a change waiting for approval; picking up where it stopped.")
            : new AgentSuggestion(vLastRoleId, $"Continuing after {vLastSession.RoleName}'s last session on this project.");
    }

    private sealed record LastSessionRow(long SessionId, long RoleId, string RoleName);

    /// <inheritdoc />
    /// <remarks>
    /// Reads the project's own most recent session and returns it when one is still open
    /// (<c>State != 'Stopped'</c>); otherwise starts a new one with <see cref="SuggestAgentAsync"/>'s
    /// own choice of role, defaulting to "ask me first" (REQ-FN-024) — the safer default for a
    /// session nobody has set a mode on yet — so the Workbench always has a conversation to open on
    /// with no further step (REQ-FN-008).
    /// </remarks>
    public async Task<SessionSummary> EnsureActiveSessionAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();

        var vOpenSession = await vConnection.QuerySingleOrDefaultAsync<SessionRow>(
            new CommandDefinition(
                @"SELECT s.SessionId AS SessionId, s.ProjectId AS ProjectId, r.Name AS RoleName,
                         s.Mode AS Mode, s.StartedUtc AS StartedUtc, s.State AS State
                  FROM Session s
                  JOIN Role r ON r.RoleId = s.RoleId
                  WHERE s.ProjectId = @aProjectId AND s.State != 'Stopped'
                  ORDER BY s.StartedUtc DESC
                  LIMIT 1;",
                new { aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vOpenSession is not null)
        {
            return new SessionSummary((int)vOpenSession.SessionId, (int)vOpenSession.ProjectId, vOpenSession.RoleName, Enum.Parse<SessionMode>(vOpenSession.Mode), ParseStartedUtc(vOpenSession.StartedUtc), vOpenSession.State);
        }

        return await StartSessionAsync(aProjectId, null, aCt).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<SessionSummary> StartSessionAsync(int aProjectId, int? aRoleId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRoleId = aRoleId
            ?? (await SuggestAgentAsync(aProjectId, aCt).ConfigureAwait(false)).RoleId
            ?? await vConnection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("SELECT RoleId FROM Role WHERE Code = 'analyst';", cancellationToken: aCt)).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No role could be found to start a session with.");

        var vStartedUtc = objClock.UtcNow;
        var vNewSessionId = await vConnection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                @"INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@aProjectId, @vRoleId, 'AskFirst', 'Active', @vStartedUtc);
                  SELECT last_insert_rowid();",
                new { aProjectId, vRoleId, vStartedUtc = vStartedUtc.ToString("O") },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vRoleName = await vConnection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition("SELECT Name FROM Role WHERE RoleId = @vRoleId;", new { vRoleId }, cancellationToken: aCt)).ConfigureAwait(false)
            ?? "Agent";

        return new SessionSummary((int)vNewSessionId, aProjectId, vRoleName, SessionMode.AskFirst, vStartedUtc, "Active");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Runs the loop in words: build the conversation so far (REQ-FN-032), stream every turn from the
    /// chosen model through TechieRag's <c>ChatStreamEventsAsync</c> (text delta, tool call,
    /// completed), and hand each text delta back the moment it arrives, so the conversation grows
    /// while the model is still writing (REQ-FN-027). Each tool call the model decides on is run past
    /// <see cref="GuardPipeline"/> (REQ-FN-036) before touching a real tool — a refusal or an
    /// "ask me first" held edit is recorded exactly as it always was — and the loop repeats until a
    /// turn ends in words instead of tools. Every step, refusal and message is appended to
    /// <c>SessionEvent</c> in order as it happens (REQ-FN-032); the final chunk names the model that
    /// answered and the tokens the whole reply used, both read from the completed events
    /// (REQ-FN-026). A model that fails before its first delta steps aside for the next one in its
    /// tier's chain (REQ-FN-021); once any text of the current call has been shown, the call is not
    /// replayed on another model — the failure is reported after the text already shown. A stop
    /// request (REQ-UI-023) cancels mid-stream and keeps the text shown so far on record.
    /// </remarks>
    public async IAsyncEnumerable<AgentReplyChunk> SendAsync(
        int aSessionId,
        string aMessage,
        int? aAgentRoleId,
        int? aModelId,
        SessionMode aMode,
        [EnumeratorCancellation] CancellationToken aCt = default)
    {
        using var vLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(aCt, objRegistry.Token(aSessionId));
        var vCt = vLinkedCts.Token;

        var vSession = await LoadSessionAsync(aSessionId, vCt).ConfigureAwait(false);
        if (vSession is null)
        {
            throw new InvalidOperationException($"Session {aSessionId} was not found.");
        }

        if (aMode != vSession.Mode)
        {
            await SetModeAsync(aSessionId, aMode, vCt).ConfigureAwait(false);
        }

        var vStartedUtc = objClock.UtcNow;
        var vRoleId = aAgentRoleId ?? vSession.RoleId;

        // A role the owner picked in the composer becomes the session's role (REQ-FN-024): the guards
        // read rights from the session (RoleRightsGuard via ToolRequest.SessionId), and the reply,
        // the history list and a reopened session all name it, so the picked role is the one acting.
        if (vRoleId != vSession.RoleId)
        {
            await SetSessionRoleAsync(aSessionId, vRoleId, vCt).ConfigureAwait(false);
        }
        var vSeq = await NextSeqAsync(aSessionId, vCt).ConfigureAwait(false);
        await AppendEventAsync(aSessionId, vSeq++, "user", aMessage, null, null, vCt).ConfigureAwait(false);

        var vHistory = await BuildHistoryAsync(aSessionId, vRoleId, vCt).ConfigureAwait(false);
        vHistory.Add(ChatMessage.User(aMessage));

        var vModel = await ResolveModelAsync(aModelId, vRoleId, vCt).ConfigureAwait(false);
        if (vModel is null)
        {
            const string vText = "No model provider is configured yet. Add one under Settings ▸ Providers.";
            await AppendEventAsync(aSessionId, vSeq, "assistant", vText, null, null, vCt).ConfigureAwait(false);
            await AppendSessionMeasurementAsync(vSession.ProjectId, aSessionId, null, null, null, vStartedUtc, vCt).ConfigureAwait(false);
            yield return new AgentReplyChunk(vText, true, null, null);
            yield break;
        }

        // REQ-FN-021 "a limited model steps aside": walk the chosen model's own tier chain
        // (REQ-FN-020, cluster K's Routing screen), in order, rather than calling only the one model
        // ResolveModelAsync picked — a rate limit, a 5xx, or a plain connection failure from that
        // model, before it has shown any text, steps aside for the next one in the chain.
        var vChain = await BuildFallbackChainAsync(vModel, vRoleId, vCt).ConfigureAwait(false);
        var vChainIndex = 0;

        // SessionId: one stable id per Chatur session, sent in the service's session header when it
        // has one (OpenCode Go's x-opencode-session; TechieRag 1.0.9, REQ-FN-025).
        var vOptions = new LlmCompletionOptions { Tools = BuildToolDefinitions(), SessionId = $"chatur-session-{aSessionId}" };
        var vShown = new StringBuilder(); // every character of reply text the owner has seen this turn
        int? vTotalTokens = null;
        int? vInputTokens = null;
        int? vOutputTokens = null;
        string? vModelName = null;
        var vFailed = false;
        var vFinished = false;
        var vStopped = false;

        for (var vIteration = 0; vIteration < MaxToolIterations && !vFailed && !vFinished && !vStopped; vIteration++)
        {
            var vToolCalls = new List<ToolCall>();
            var vIterationText = new StringBuilder();
            var vSeparatorPending = vShown.Length > 0;
            TokenUsage? vUsage = null;
            string? vCompletedModelName = null;
            ILlmProvider? vLlm = null;

            // Step through the chain until one model completes this call (or the call must be given up).
            while (true)
            {
                vToolCalls.Clear();
                vIterationText.Clear();
                vUsage = null;
                vCompletedModelName = null;
                Exception? vFailure = null;
                var vDeltaShown = false;

                var vOpened = await TryOpenStreamAsync(vChain[vChainIndex], vHistory, vOptions, vCt).ConfigureAwait(false);
                if (vOpened.Failure is not null)
                {
                    vFailure = vOpened.Failure;
                }
                else if (vOpened.Cancelled)
                {
                    vStopped = true;
                    break;
                }
                else
                {
                    vLlm = vOpened.Provider;
                    var vEnumerator = vOpened.Enumerator!;
                    try
                    {
                        while (true)
                        {
                            LlmStreamEvent vEvent;
                            try
                            {
                                if (!await vEnumerator.MoveNextAsync().ConfigureAwait(false))
                                {
                                    break;
                                }

                                vEvent = vEnumerator.Current;
                            }
                            catch (OperationCanceledException)
                            {
                                vStopped = true;
                                break;
                            }
                            catch (Exception vException)
                            {
                                vFailure = vException;
                                break;
                            }

                            if (vEvent.Kind == LlmStreamEventKind.TextDelta)
                            {
                                var vDelta = vEvent.Text ?? string.Empty;
                                if (vDelta.Length == 0)
                                {
                                    continue;
                                }

                                if (!vDeltaShown && vSeparatorPending)
                                {
                                    vShown.Append(TurnSeparator);
                                    yield return new AgentReplyChunk(TurnSeparator, false, null, null);
                                }

                                vDeltaShown = true;
                                vIterationText.Append(vDelta);
                                vShown.Append(vDelta);
                                yield return new AgentReplyChunk(vDelta, false, null, null);
                            }
                            else if (vEvent.Kind == LlmStreamEventKind.ToolCall)
                            {
                                if (vEvent.ToolCall is not null)
                                {
                                    vToolCalls.Add(vEvent.ToolCall);
                                }
                            }
                            else if (vEvent.Kind == LlmStreamEventKind.Completed)
                            {
                                vUsage = vEvent.Usage;
                                vCompletedModelName = vEvent.ModelName;
                            }
                        }
                    }
                    finally
                    {
                        await vEnumerator.DisposeAsync().ConfigureAwait(false);
                    }
                }

                if (vStopped)
                {
                    break;
                }

                if (vFailure is null)
                {
                    break;
                }

                var vSteppingAsideModel = vChain[vChainIndex];
                if (vDeltaShown || !IsModelUnavailable(vFailure) || vChainIndex + 1 >= vChain.Count)
                {
                    // A subscription that needs a fresh sign-in says so plainly (TechieRag 1.0.9's
                    // CodeSignInRequired, TR-RAG-005 closed) rather than as a generic failure.
                    var vNote = vFailure is SubscriptionSignInException { Code: SubscriptionSignInException.CodeSignInRequired }
                        ? $"{vSteppingAsideModel.ProviderName} is not signed in any more. Sign in again under Settings ▸ Providers."
                        : $"{vSteppingAsideModel.ProviderName} did not answer: {vFailure.Message}";
                    if (vShown.Length > 0)
                    {
                        vNote = TurnSeparator + vNote;
                    }

                    vShown.Append(vNote);
                    vFailed = true;
                    yield return new AgentReplyChunk(vNote, false, null, null);
                    break;
                }

                vChainIndex++;
                await AppendEventAsync(
                    aSessionId,
                    vSeq++,
                    "tool",
                    $"{vSteppingAsideModel.ProviderName} was limited or unavailable ({vFailure.Message}); trying {vChain[vChainIndex].ProviderName} instead.",
                    null,
                    null,
                    vCt).ConfigureAwait(false);
            }

            if (vFailed || vStopped)
            {
                break;
            }

            if (vUsage is not null)
            {
                vTotalTokens = (vTotalTokens ?? 0) + vUsage.TotalTokens;
                vInputTokens = (vInputTokens ?? 0) + vUsage.InputTokens;
                vOutputTokens = (vOutputTokens ?? 0) + vUsage.OutputTokens;
            }

            vModelName = string.IsNullOrEmpty(vCompletedModelName) ? vLlm?.ModelName : vCompletedModelName;

            var vTurn = new LlmResponse { Content = vIterationText.ToString(), Usage = new TokenUsage() };
            if (vToolCalls.Count > 0)
            {
                vTurn.ToolCalls = vToolCalls;
            }

            vHistory.Add(vTurn.ToChatMessage());

            if (vToolCalls.Count == 0)
            {
                vFinished = true;
                break;
            }

            foreach (var vToolCall in vToolCalls)
            {
                var vToolRequest = await BuildToolRequestAsync(aSessionId, vSession.ProjectId, vToolCall, vCt).ConfigureAwait(false);
                var vGuardResult = await objGuards.EvaluateAsync(vToolRequest, vCt).ConfigureAwait(false);

                string vToolContent;
                if (!vGuardResult.Allowed)
                {
                    var vReason = vGuardResult.RefusalReason ?? "Refused.";
                    var vIsHeldForApproval =
                        vSession.Mode == SessionMode.AskFirst
                        && string.Equals(vToolRequest.ToolName, AgentToolNames.EditFile, StringComparison.Ordinal)
                        && vReason.Contains("ask me first", StringComparison.OrdinalIgnoreCase);

                    if (vIsHeldForApproval)
                    {
                        await RecordChangeAsync(aSessionId, vToolRequest, "Proposed", vCt).ConfigureAwait(false);
                    }

                    await AppendEventAsync(aSessionId, vSeq++, "refusal", vReason, null, null, vCt).ConfigureAwait(false);
                    vToolContent = vReason;
                }
                else
                {
                    await AppendEventAsync(aSessionId, vSeq++, "tool", vToolRequest.Description, null, null, vCt).ConfigureAwait(false);
                    vToolContent = await ExecuteToolAsync(aSessionId, vSession.ProjectId, vToolRequest, vCt).ConfigureAwait(false);
                }

                vHistory.Add(ChatMessage.Tool(vToolCall.Id, vToolContent));
            }
        }

        if (vStopped)
        {
            // The owner pressed Stop (REQ-UI-023): keep what they had already seen on record, and
            // end without a final chunk, as before.
            // Only the text already shown is written, never a second "stopped" step (StopAsync
            // records that one), and at a fresh sequence number since StopAsync may have taken ours.
            if (vShown.Length > 0)
            {
                var vStoppedSeq = await NextSeqAsync(aSessionId, CancellationToken.None).ConfigureAwait(false);
                await AppendEventAsync(aSessionId, vStoppedSeq, "assistant", vShown.ToString(), vModelName, vTotalTokens, CancellationToken.None).ConfigureAwait(false);
            }

            await AppendSessionMeasurementAsync(vSession.ProjectId, aSessionId, vModelName, vInputTokens, vOutputTokens, vStartedUtc, CancellationToken.None).ConfigureAwait(false);
            yield break;
        }

        if (!vFailed && !vFinished)
        {
            var vNote = "The agent used too many tool calls without finishing; try again, or ask for something smaller.";
            if (vShown.Length > 0)
            {
                vNote = TurnSeparator + vNote;
            }

            vShown.Append(vNote);
            yield return new AgentReplyChunk(vNote, false, null, null);
        }

        var vReportedModelName = vFailed ? null : vModelName;
        await AppendEventAsync(aSessionId, vSeq, "assistant", vShown.ToString(), vReportedModelName, vTotalTokens, vCt).ConfigureAwait(false);
        await AppendSessionMeasurementAsync(vSession.ProjectId, aSessionId, vReportedModelName, vInputTokens, vOutputTokens, vStartedUtc, vCt).ConfigureAwait(false);
        yield return new AgentReplyChunk(string.Empty, true, vReportedModelName, vTotalTokens);
    }

    /// <inheritdoc />
    public async Task StopAsync(int aSessionId, CancellationToken aCt = default)
    {
        // Cancels whatever SendAsync loop (cluster G, once built) is linked to this session's token,
        // and records the stop even when nothing was actually in flight.
        objRegistry.Cancel(aSessionId);

        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Session SET State = 'Stopped' WHERE SessionId = @aSessionId",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        // The activity panel says so (REQ-UI-023): one "stopped" step, after whatever was already recorded.
        var vSeq = await NextSeqAsync(aSessionId, aCt).ConfigureAwait(false);
        await AppendEventAsync(aSessionId, vSeq, StoppedEventKind, StoppedEventText, null, null, aCt).ConfigureAwait(false);
    }

    /// <summary>The <c>SessionEvent</c> kind <see cref="StopAsync"/> records and the activity panel renders as its own step (REQ-UI-023).</summary>
    public const string StoppedEventKind = "stopped";

    private const string StoppedEventText = "Stopped by the owner.";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionSummary>> SessionsAsync(int aProjectId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vRows = await vConnection.QueryAsync<SessionRow>(
            new CommandDefinition(
                @"SELECT s.SessionId AS SessionId, s.ProjectId AS ProjectId, r.Name AS RoleName,
                         s.Mode AS Mode, s.StartedUtc AS StartedUtc, s.State AS State
                  FROM Session s
                  JOIN Role r ON r.RoleId = s.RoleId
                  WHERE s.ProjectId = @aProjectId
                  ORDER BY s.StartedUtc DESC",
                new { aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRows
            .Select(r => new SessionSummary((int)r.SessionId, (int)r.ProjectId, r.RoleName, Enum.Parse<SessionMode>(r.Mode), ParseStartedUtc(r.StartedUtc), r.State))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Reads the session's own summary row and every recorded event in order (REQ-FN-031,
    /// REQ-FN-032) — the same rows <see cref="SendAsync"/> appended as the turn ran, so what the
    /// Workbench shows after a restart is exactly what happened, not a replay built from the
    /// conversation text alone.
    /// </remarks>
    public async Task<SessionDetail> ContinueSessionAsync(int aSessionId, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();

        var vSummaryRow = await vConnection.QuerySingleOrDefaultAsync<SessionRow>(
            new CommandDefinition(
                @"SELECT s.SessionId AS SessionId, s.ProjectId AS ProjectId, r.Name AS RoleName,
                         s.Mode AS Mode, s.StartedUtc AS StartedUtc, s.State AS State
                  FROM Session s
                  JOIN Role r ON r.RoleId = s.RoleId
                  WHERE s.SessionId = @aSessionId;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vSummaryRow is null)
        {
            throw new InvalidOperationException($"Session {aSessionId} was not found.");
        }

        var vEventRows = await vConnection.QueryAsync<FullSessionEventRow>(
            new CommandDefinition(
                "SELECT Seq AS Seq, Kind AS Kind, Payload AS Payload, Model AS Model, Tokens AS Tokens FROM SessionEvent WHERE SessionId = @aSessionId ORDER BY Seq;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        var vSummary = new SessionSummary(
            (int)vSummaryRow.SessionId,
            (int)vSummaryRow.ProjectId,
            vSummaryRow.RoleName,
            Enum.Parse<SessionMode>(vSummaryRow.Mode),
            ParseStartedUtc(vSummaryRow.StartedUtc),
            vSummaryRow.State);

        var vEvents = vEventRows
            .Select(r => new SessionEventRecord((int)r.Seq, r.Kind, r.Payload, r.Model, r.Tokens is null ? null : (int)r.Tokens))
            .ToList();

        return new SessionDetail(vSummary, vEvents);
    }

    private sealed record FullSessionEventRow(long Seq, string Kind, string Payload, string? Model, long? Tokens);

    /// <inheritdoc />
    public async IAsyncEnumerable<ActivityEvent> ActivityAsync(int aSessionId, [EnumeratorCancellation] CancellationToken aCt = default)
    {
        var vLastSeq = 0;

        while (!aCt.IsCancellationRequested)
        {
            List<SessionEventRow> vRows;
            using (var vConnection = objDb.OpenConnection())
            {
                // "user"/"assistant" rows are the conversation itself (REQ-FN-025 through
                // REQ-FN-027's own message thread), not a step the agent took — the activity panel
                // (REQ-UI-022) is for what it DID: a read, an edit, a run, or a refusal.
                vRows = (await vConnection.QueryAsync<SessionEventRow>(
                    new CommandDefinition(
                        "SELECT Seq AS Seq, Kind AS Kind, Payload AS Payload FROM SessionEvent WHERE SessionId = @aSessionId AND Seq > @vLastSeq AND Kind NOT IN ('user', 'assistant') ORDER BY Seq",
                        new { aSessionId, vLastSeq },
                        cancellationToken: aCt)).ConfigureAwait(false)).ToList();
            }

            foreach (var vRow in vRows)
            {
                vLastSeq = (int)vRow.Seq;
                yield return new ActivityEvent(vRow.Kind, vRow.Payload, !string.Equals(vRow.Kind, "refusal", StringComparison.OrdinalIgnoreCase));
            }

            try
            {
                await Task.Delay(500, aCt).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

    /// <inheritdoc />
    public async Task SetModeAsync(int aSessionId, SessionMode aMode, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Session SET Mode = @vMode WHERE SessionId = @aSessionId",
                new { aSessionId, vMode = aMode.ToString() },
                cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>Makes <paramref name="aRoleId"/> the role acting in a session (REQ-FN-024).</summary>
    private async Task SetSessionRoleAsync(int aSessionId, int aRoleId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE Session SET RoleId = @aRoleId WHERE SessionId = @aSessionId",
                new { aSessionId, aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);
    }

    // SQLite's TEXT columns come back through Microsoft.Data.Sqlite as String, so StartedUtc is read
    // as a string and parsed afterwards — the same exact-type-match reason the integer columns above
    // are read as `long`.
    private sealed record SessionRow(long SessionId, long ProjectId, string RoleName, string Mode, string StartedUtc, string State);

    private static DateTime ParseStartedUtc(string aValue) =>
        DateTime.Parse(aValue, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);

    private sealed record SessionEventRow(long Seq, string Kind, string Payload);

    // ---- SendAsync's own helpers -------------------------------------------------------------

    private async Task<SessionCoreRow?> LoadSessionAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<SessionCoreRawRow>(
            new CommandDefinition(
                "SELECT SessionId, ProjectId, RoleId, Mode FROM Session WHERE SessionId = @aSessionId;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        return vRow is null ? null : new SessionCoreRow((int)vRow.SessionId, (int)vRow.ProjectId, (int)vRow.RoleId, Enum.Parse<SessionMode>(vRow.Mode));
    }

    // SQLite's INTEGER columns come back through Microsoft.Data.Sqlite as Int64; Dapper's
    // constructor-based materialization for a record with no parameterless constructor requires an
    // exact type match rather than the numeric-widening a settable-property POCO gets, so every
    // Dapper row record in this file takes `long` for an integer column and narrows to `int`
    // afterwards, on the boundary back to the public, `int`-based Actions DTOs.
    private sealed record SessionCoreRawRow(long SessionId, long ProjectId, long RoleId, string Mode);

    private sealed record SessionCoreRow(int SessionId, int ProjectId, int RoleId, SessionMode Mode);

    private async Task<int> NextSeqAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vMaxSeq = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT MAX(Seq) FROM SessionEvent WHERE SessionId = @aSessionId;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);
        return (vMaxSeq ?? 0) + 1;
    }

    private async Task AppendEventAsync(int aSessionId, int aSeq, string aKind, string aPayload, string? aModel, int? aTokens, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc)
                  VALUES (@aSessionId, @aSeq, @aKind, @aPayload, @aModel, @aTokens, @vCreatedUtc);",
                new { aSessionId, aSeq, aKind, aPayload, aModel, aTokens, vCreatedUtc = objClock.UtcNow.ToString("O") },
                cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the conversation TechieRag sees for this turn: the role's own wording as the system
    /// prompt (REQ-FN-024's chosen role shapes every reply it gives), then every prior user and
    /// assistant message in order (REQ-FN-032).
    /// </summary>
    private async Task<List<ChatMessage>> BuildHistoryAsync(int aSessionId, int aRoleId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();

        var vWording = await vConnection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT Wording FROM Role WHERE RoleId = @aRoleId;", new { aRoleId }, cancellationToken: aCt))
            .ConfigureAwait(false);

        var vHistory = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(vWording))
        {
            vHistory.Add(ChatMessage.System(vWording));
        }

        var vPastEvents = await vConnection.QueryAsync<PastMessageRow>(
            new CommandDefinition(
                "SELECT Kind, Payload FROM SessionEvent WHERE SessionId = @aSessionId AND Kind IN ('user', 'assistant') ORDER BY Seq;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false);

        foreach (var vEvent in vPastEvents)
        {
            vHistory.Add(vEvent.Kind == "user" ? ChatMessage.User(vEvent.Payload) : ChatMessage.Assistant(vEvent.Payload));
        }

        return vHistory;
    }

    private sealed record PastMessageRow(string Kind, string Payload);

    /// <summary>
    /// Resolves the model a turn uses (REQ-FN-025, REQ-FN-026): the model explicitly chosen on
    /// Workbench when there is one, otherwise the first connected model on the acting role's own
    /// tier, and — with only one provider configured — any connected model at all, rather than
    /// refusing to answer just because REQ-FN-020/021's fuller fallback chain (cluster K's Routing
    /// screen) does not exist yet.
    /// </summary>
    private async Task<ConnectedModel?> ResolveModelAsync(int? aModelId, int aRoleId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();

        if (aModelId is not null)
        {
            var vChosen = await vConnection.QuerySingleOrDefaultAsync<ModelRow>(
                new CommandDefinition(ModelSelectSql + "WHERE m.ModelId = @aModelId;", new { aModelId }, cancellationToken: aCt)).ConfigureAwait(false);
            if (vChosen is not null)
            {
                return ToConnectedModel(vChosen);
            }
        }

        var vTier = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition("SELECT Tier FROM Role WHERE RoleId = @aRoleId;", new { aRoleId }, cancellationToken: aCt))
            .ConfigureAwait(false) ?? 2;

        var vTierModel = await vConnection.QuerySingleOrDefaultAsync<ModelRow>(
            new CommandDefinition(
                ModelSelectSql + "WHERE m.Tier = @vTier AND p.State = 'Connected' ORDER BY m.ModelId LIMIT 1;",
                new { vTier },
                cancellationToken: aCt)).ConfigureAwait(false);
        if (vTierModel is not null)
        {
            return ToConnectedModel(vTierModel);
        }

        var vAnyModel = await vConnection.QuerySingleOrDefaultAsync<ModelRow>(
            new CommandDefinition(ModelSelectSql + "WHERE p.State = 'Connected' ORDER BY m.ModelId LIMIT 1;", cancellationToken: aCt))
            .ConfigureAwait(false);

        return vAnyModel is null ? null : ToConnectedModel(vAnyModel);
    }

    /// <summary>
    /// Resolves one specific, still-connected model by id — the chain-walking counterpart of
    /// <see cref="ResolveModelAsync"/>'s explicit-id branch, but never falling back to a tier or "any
    /// model" pick, since a fallback chain (REQ-FN-020) names exact models and a candidate that is no
    /// longer connected is simply skipped (REQ-FN-021), not silently swapped for an unrelated one.
    /// </summary>
    private async Task<ConnectedModel?> ResolveModelByIdAsync(int aModelId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<ModelRow>(
            new CommandDefinition(
                ModelSelectSql + "WHERE m.ModelId = @aModelId AND p.State = 'Connected';",
                new { aModelId },
                cancellationToken: aCt)).ConfigureAwait(false);
        return vRow is null ? null : ToConnectedModel(vRow);
    }

    private const string ModelSelectSql =
        @"SELECT m.ModelId AS ModelId, m.ProviderId AS ProviderId, p.Name AS ProviderName,
                 p.Connector AS Connector, p.BaseUrl AS BaseUrl, p.SecretName AS SecretName, m.Identifier AS Identifier
          FROM Model m
          JOIN Provider p ON p.ProviderId = m.ProviderId ";

    private static ConnectedModel ToConnectedModel(ModelRow aRow) =>
        new((int)aRow.ModelId, (int)aRow.ProviderId, aRow.ProviderName, aRow.Connector, aRow.BaseUrl, aRow.SecretName, aRow.Identifier);

    private sealed record ModelRow(long ModelId, long ProviderId, string ProviderName, string Connector, string BaseUrl, string? SecretName, string Identifier);

    /// <summary>
    /// Builds the ordered list of models one turn tries (REQ-FN-021): the model
    /// <see cref="ResolveModelAsync"/> already picked, first, then the rest of that model's own tier
    /// chain (REQ-FN-020's <see cref="IRoutingActions.TiersAsync"/>) in the order Settings ▸ Routing
    /// stored them, skipping any id that no longer names a connected model and never listing the
    /// starting model twice. A model with no tier, or a tier with no configured chain, yields a
    /// one-model "chain" — precisely today's behaviour, so a project with no Routing setup at all
    /// sees no change (Coding Standards §Testability: this is why the existing single-model tests
    /// still pass unmodified).
    /// </summary>
    private async Task<List<ConnectedModel>> BuildFallbackChainAsync(ConnectedModel aStartingModel, int aRoleId, CancellationToken aCt)
    {
        var vChain = new List<ConnectedModel> { aStartingModel };

        int? vTier;
        using (var vConnection = objDb.OpenConnection())
        {
            vTier = await vConnection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition("SELECT Tier FROM Model WHERE ModelId = @vModelId;", new { vModelId = aStartingModel.ModelId }, cancellationToken: aCt))
                .ConfigureAwait(false);
        }

        if (vTier is null)
        {
            return vChain;
        }

        var vTiers = await objRouting.TiersAsync(aCt).ConfigureAwait(false);
        var vTierChain = vTiers.FirstOrDefault(t => t.Tier == vTier)?.ModelIdsInOrder ?? Array.Empty<int>();

        foreach (var vModelId in vTierChain)
        {
            if (vModelId == aStartingModel.ModelId || vChain.Any(m => m.ModelId == vModelId))
            {
                continue;
            }

            var vCandidate = await ResolveModelByIdAsync(vModelId, aCt).ConfigureAwait(false);
            if (vCandidate is not null)
            {
                vChain.Add(vCandidate);
            }
        }

        return vChain;
    }

    /// <summary>The outcome of building a provider for a model and opening its event stream (REQ-FN-021).</summary>
    private sealed record OpenedStream(ILlmProvider? Provider, IAsyncEnumerator<LlmStreamEvent>? Enumerator, Exception? Failure, bool Cancelled);

    /// <summary>
    /// Builds the provider for one candidate model and opens its <c>ChatStreamEventsAsync</c>,
    /// reporting a failure at either step back to the caller instead of throwing, so
    /// <see cref="SendAsync"/> can decide whether it is worth stepping aside to the next model in the
    /// chain (REQ-FN-021). A genuine cancellation is reported as such rather than treated as a model
    /// failure — the turn is being stopped (REQ-UI-023), not looking for another model to try.
    /// </summary>
    private async Task<OpenedStream> TryOpenStreamAsync(ConnectedModel aModel, List<ChatMessage> aHistory, LlmCompletionOptions aOptions, CancellationToken aCt)
    {
        try
        {
            var vProvider = await objLlmProviderFactory.CreateAsync(aModel, aCt).ConfigureAwait(false);
            var vEnumerator = vProvider.ChatStreamEventsAsync(aHistory, aOptions, aCt).GetAsyncEnumerator(aCt);
            return new OpenedStream(vProvider, vEnumerator, null, false);
        }
        catch (OperationCanceledException)
        {
            return new OpenedStream(null, null, null, true);
        }
        catch (Exception vException)
        {
            return new OpenedStream(null, null, vException, false);
        }
    }

    /// <summary>
    /// Whether a failure is the kind a model "steps aside" for (REQ-FN-021's own words: "limited or
    /// unavailable") rather than a real answer failure worth stopping on: a rate limit
    /// (TechieRag's own <see cref="TechieRag.Models.LlmRateLimitException"/>, a <c>429</c>, is an
    /// <see cref="HttpRequestException"/> under the hood), any <c>5xx</c>, or a connection failure —
    /// an <see cref="HttpRequestException"/> with no status code at all (DNS, refused, reset) is
    /// exactly that.
    /// </summary>
    private static bool IsModelUnavailable(Exception aException)
    {
        if (aException is HttpRequestException vHttpException)
        {
            if (vHttpException.StatusCode is null)
            {
                return true;
            }

            var vStatusCode = (int)vHttpException.StatusCode.Value;
            return vStatusCode == 429 || vStatusCode >= 500;
        }

        return aException is System.Net.Sockets.SocketException;
    }

    /// <summary>The tool-name vocabulary offered to the model this turn (REQ-FN-036, Architecture §3 "Tools: read, search, edit, run, build").</summary>
    private static IReadOnlyList<ToolDefinition> BuildToolDefinitions() =>
    [
        new ToolDefinition
        {
            Name = AgentToolNames.ReadFile,
            Description = "Reads a file's current text from the selected project, given its path relative to the project root.",
            ParametersSchema = """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}"""
        },
        new ToolDefinition
        {
            Name = AgentToolNames.EditFile,
            Description = "Writes a file's new, complete text in the selected project. Held for the owner's approval unless the session is in \"go ahead\".",
            ParametersSchema = """{"type":"object","properties":{"path":{"type":"string"},"content":{"type":"string"}},"required":["path","content"]}""",
            RequiresConfirmation = true
        },
        new ToolDefinition
        {
            Name = AgentToolNames.CorrectWording,
            Description = "Corrects the wording of one of your own agents, rules or process steps in Chatur's database when it proves wrong during work. The old wording is kept, and the correction is logged for the owner to keep or undo. kind is role, rule or step; target is the role's code, the rule's id, or \"process/step\" for a step.",
            ParametersSchema = """{"type":"object","properties":{"kind":{"type":"string","enum":["role","rule","step"]},"target":{"type":"string"},"wording":{"type":"string"},"reason":{"type":"string"}},"required":["kind","target","wording","reason"]}"""
        },
        new ToolDefinition
        {
            Name = AgentToolNames.RunBuild,
            Description = "Builds the selected project and reports whether it succeeded.",
            ParametersSchema = """{"type":"object","properties":{}}"""
        },
        new ToolDefinition
        {
            Name = AgentToolNames.RunSourceControl,
            Description = "Runs a source-control command such as commit, push or reset. Always refused — a model never runs one.",
            ParametersSchema = """{"type":"object","properties":{"command":{"type":"string"}},"required":["command"]}"""
        }
    ];

    /// <summary>
    /// Turns one <see cref="ToolCall"/> into a <see cref="ToolRequest"/>, reading the file's current
    /// text for an <c>edit-file</c> call so <see cref="Guards.AskMeFirstGuard"/>'s refusal can be
    /// turned into a proposed <c>Change</c> row with both sides of the edit, without the guard itself
    /// depending on <see cref="IFileActions"/> (Architecture §7 "Guards" depends only on "Roles").
    /// </summary>
    private async Task<ToolRequest> BuildToolRequestAsync(int aSessionId, int aProjectId, ToolCall aToolCall, CancellationToken aCt)
    {
        var vArgs = ParseArguments(aToolCall.ArgumentsJson);

        switch (aToolCall.Name)
        {
            case AgentToolNames.ReadFile:
            {
                var vPath = GetString(vArgs, "path");
                return new ToolRequest(aSessionId, AgentToolNames.ReadFile, $"Read \"{vPath}\".", vPath);
            }

            case AgentToolNames.EditFile:
            {
                var vPath = GetString(vArgs, "path");
                var vContent = GetString(vArgs, "content");
                string vBefore;
                try
                {
                    vBefore = await objFileActions.ReadAsync(aProjectId, vPath, aCt).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    vBefore = string.Empty; // a file that does not exist yet
                }

                return new ToolRequest(aSessionId, AgentToolNames.EditFile, $"Edit \"{vPath}\".", vPath, vBefore, vContent);
            }

            case AgentToolNames.CorrectWording:
            {
                var vKind = GetString(vArgs, "kind");
                var vTarget = GetString(vArgs, "target");
                var vArguments = new Dictionary<string, string>
                {
                    ["kind"] = vKind,
                    ["target"] = vTarget,
                    ["wording"] = GetString(vArgs, "wording"),
                    ["reason"] = GetString(vArgs, "reason")
                };
                return new ToolRequest(aSessionId, AgentToolNames.CorrectWording, $"Correct the wording of {vKind} \"{vTarget}\".", Arguments: vArguments);
            }

            case AgentToolNames.RunBuild:
                return new ToolRequest(aSessionId, AgentToolNames.RunBuild, "Build the project.");

            case AgentToolNames.RunSourceControl:
            {
                var vCommand = GetString(vArgs, "command");
                return new ToolRequest(aSessionId, AgentToolNames.RunSourceControl, $"Run source control: {vCommand}.");
            }

            default:
                return new ToolRequest(aSessionId, aToolCall.Name, "An unrecognised tool.");
        }
    }

    private static JsonElement ParseArguments(string? aArgumentsJson)
    {
        if (string.IsNullOrWhiteSpace(aArgumentsJson))
        {
            return default;
        }

        try
        {
            return JsonDocument.Parse(aArgumentsJson).RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string GetString(JsonElement aElement, string aName) =>
        aElement.ValueKind == JsonValueKind.Object && aElement.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String
            ? vValue.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>Runs an allowed tool for real and returns the text handed back to the model as the tool's result.</summary>
    private async Task<string> ExecuteToolAsync(int aSessionId, int aProjectId, ToolRequest aRequest, CancellationToken aCt)
    {
        switch (aRequest.ToolName)
        {
            case AgentToolNames.ReadFile:
                try
                {
                    return await objFileActions.ReadAsync(aProjectId, aRequest.FilePath ?? string.Empty, aCt).ConfigureAwait(false);
                }
                catch (Exception vException)
                {
                    return $"Could not read \"{aRequest.FilePath}\": {vException.Message}";
                }

            case AgentToolNames.EditFile:
                // Reached only once the guards have allowed it — "go ahead" mode, since "ask me
                // first" is always refused by AskMeFirstGuard and handled above (REQ-FN-035).
                await objFileActions.SaveAsync(aProjectId, aRequest.FilePath ?? string.Empty, aRequest.After ?? string.Empty, aCt).ConfigureAwait(false);
                await RecordChangeAsync(aSessionId, aRequest, "Approved", aCt).ConfigureAwait(false);
                return $"Wrote \"{aRequest.FilePath}\".";

            case AgentToolNames.CorrectWording:
                return await CorrectWordingAsync(aRequest, aCt).ConfigureAwait(false);

            case AgentToolNames.RunBuild:
                return await RunBuildAsync(aSessionId, aProjectId, aCt).ConfigureAwait(false);

            default:
                return "This tool is not recognised.";
        }
    }

    /// <summary>
    /// Runs a <c>correct-wording</c> call the guards allowed: changes the wording and logs the
    /// correction (REQ-UI-034). A refusal of the new wording (too short, unknown target) is handed
    /// back to the model as the tool's result, not thrown, so the turn carries on.
    /// </summary>
    private async Task<string> CorrectWordingAsync(ToolRequest aRequest, CancellationToken aCt)
    {
        var vArguments = aRequest.Arguments;
        if (vArguments is null)
        {
            return "The correction named no target.";
        }

        try
        {
            var vCorrection = await objCorrections.CorrectWordingAsync(
                vArguments["kind"], vArguments["target"], vArguments["wording"], vArguments["reason"], aCt).ConfigureAwait(false);
            return $"Corrected {vCorrection.Target}. The owner can keep or undo it under Settings > Corrections.";
        }
        catch (Exception vException) when (vException is ArgumentException or KeyNotFoundException)
        {
            return $"The wording was not corrected: {vException.Message}";
        }
    }

    /// <summary>
    /// Appends this turn's <c>sessions</c> record to the project's <c>docs/metrics</c> (REQ-FN-040).
    /// Only what Chatur actually knows is sent — session id, model, turn length and the tokens the
    /// model reported; everything else is left for <see cref="IMeasurementActions.AppendAsync"/> to
    /// write empty (REQ-FN-042). A measurement failure never reaches the turn (REQ-UI-035):
    /// <c>AppendAsync</c> already logs and records a refusal, and anything it still throws is dropped here.
    /// </summary>
    private async Task AppendSessionMeasurementAsync(int aProjectId, int aSessionId, string? aModel, int? aInputTokens, int? aOutputTokens, DateTime aStartedUtc, CancellationToken aCt)
    {
        try
        {
            var vSeconds = Math.Max(0, (objClock.UtcNow - aStartedUtc).TotalSeconds);
            var vFields = new Dictionary<string, string?>
            {
                ["kind"] = "session",
                ["session_id"] = aSessionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["model"] = aModel,
                ["duration_s"] = vSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                ["input_tokens"] = aInputTokens?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["output_tokens"] = aOutputTokens?.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            await objMeasurements.AppendAsync(aProjectId, "sessions.jsonl", vFields, aCt).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Measuring is never allowed to stop the work (REQ-UI-035).
        }
    }

    /// <summary>
    /// Appends a <c>runs</c> record for one build the agent ran (REQ-FN-040): the command, when it
    /// started and ended, its length and whether it passed. Nothing else is known, so nothing else is written.
    /// </summary>
    private async Task AppendBuildMeasurementAsync(int aProjectId, DateTime aStartedUtc, bool aPassed, CancellationToken aCt)
    {
        try
        {
            var vEndedUtc = objClock.UtcNow;
            var vFields = new Dictionary<string, string?>
            {
                ["kind"] = "run",
                ["cmd"] = "dotnet build",
                ["started"] = aStartedUtc.ToString("O"),
                ["ended"] = vEndedUtc.ToString("O"),
                ["duration_s"] = Math.Max(0, (vEndedUtc - aStartedUtc).TotalSeconds).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                ["build_result"] = aPassed ? "pass" : "fail"
            };
            await objMeasurements.AppendAsync(aProjectId, "runs.jsonl", vFields, aCt).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Measuring is never allowed to stop the work (REQ-UI-035).
        }
    }

    /// <summary>Records a change the agent proposed (<c>"ask me first"</c>) or already applied (<c>"go ahead"</c>) so Changes and Repository can both show it (REQ-UI-032, REQ-FN-033, REQ-FN-034).</summary>
    private async Task RecordChangeAsync(int aSessionId, ToolRequest aRequest, string aStatus, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(
            new CommandDefinition(
                "INSERT INTO Change (SessionId, FilePath, Before, After, Status) VALUES (@aSessionId, @FilePath, @Before, @After, @aStatus);",
                new { aSessionId, aRequest.FilePath, aRequest.Before, aRequest.After, aStatus },
                cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>
    /// The one kind of work a <c>run-build</c> tool call is always checking (REQ-FN-019's own known
    /// work kinds): a build only ever follows code the agent wrote.
    /// </summary>
    private const string BuildWorkKind = "write-code";

    private async Task<string> RunBuildAsync(int aSessionId, int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition("SELECT Path FROM Project WHERE ProjectId = @aProjectId;", new { aProjectId }, cancellationToken: aCt))
            .ConfigureAwait(false);
        if (vProjectPath is null)
        {
            return $"Project {aProjectId} was not found.";
        }

        var vProjectRoot = File.Exists(vProjectPath) ? (Path.GetDirectoryName(vProjectPath) ?? vProjectPath) : vProjectPath;
        var vOutput = new List<string>();

        int vExitCode;
        var vBuildStartedUtc = objClock.UtcNow;
        try
        {
            vExitCode = await objProcessLauncher.RunAsync("dotnet", "build", vProjectRoot, vOutput.Add, aCt).ConfigureAwait(false);
        }
        catch (Exception vException)
        {
            return $"Build could not start: {vException.Message}";
        }

        await AppendBuildMeasurementAsync(aProjectId, vBuildStartedUtc, vExitCode == 0, aCt).ConfigureAwait(false);

        // REQ-FN-022 "repeated failed fixes climb a tier": only a build that follows an approved
        // change in this session is "the same fix" — a build run cold, with nothing yet approved,
        // proves nothing about a fix repeatedly failing.
        var vHasApprovedChange = await vConnection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                "SELECT 1 FROM Change WHERE SessionId = @aSessionId AND Status = 'Approved' LIMIT 1;",
                new { aSessionId },
                cancellationToken: aCt)).ConfigureAwait(false) is not null;

        if (vHasApprovedChange)
        {
            if (vExitCode == 0)
            {
                await ResetWorkFailureCountAsync(vConnection, BuildWorkKind, aCt).ConfigureAwait(false);
            }
            else
            {
                await RecordFailedFixAsync(vConnection, aCt).ConfigureAwait(false);
            }
        }

        var vTail = string.Join('\n', vOutput.TakeLast(200));
        return vExitCode == 0 ? $"Build succeeded.\n{vTail}" : $"Build failed (exit {vExitCode}).\n{vTail}";
    }

    /// <summary>
    /// Counts this failure onto <c>WorkFailureCount</c>'s current streak for <see cref="BuildWorkKind"/>
    /// and, once it reaches <see cref="IRoutingActions.EscalationThresholdAsync"/>'s own threshold,
    /// actually climbs the work to the next tier through <see cref="IRoutingActions.RecordEscalationAsync"/>
    /// (REQ-FN-022) — which both records the move and applies it, exactly as that method's own doc
    /// says. The streak resets either way once it is acted on, so a climb is recorded once per streak,
    /// not once per failure past the threshold.
    /// </summary>
    private async Task RecordFailedFixAsync(IDbConnection aConnection, CancellationToken aCt)
    {
        var vFailureCount = await IncrementWorkFailureCountAsync(aConnection, BuildWorkKind, aCt).ConfigureAwait(false);
        var vThreshold = await objRouting.EscalationThresholdAsync(aCt).ConfigureAwait(false);
        if (vFailureCount < vThreshold)
        {
            return;
        }

        var vWorkTiers = await objRouting.WorkTiersAsync(aCt).ConfigureAwait(false);
        var vFromTier = vWorkTiers.TryGetValue(BuildWorkKind, out var vCurrentTier) ? vCurrentTier : 2;
        var vToTier = Math.Min(vFromTier + 1, 3);

        if (vToTier != vFromTier)
        {
            await objRouting.RecordEscalationAsync(
                BuildWorkKind,
                vFromTier,
                vToTier,
                $"The build failed {vFailureCount} times in a row after an approved change.",
                aCt).ConfigureAwait(false);
        }

        await ResetWorkFailureCountAsync(aConnection, BuildWorkKind, aCt).ConfigureAwait(false);
    }

    private static async Task<int> IncrementWorkFailureCountAsync(IDbConnection aConnection, string aWorkKind, CancellationToken aCt)
    {
        var vAffected = await aConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE WorkFailureCount SET FailureCount = FailureCount + 1 WHERE WorkKind = @aWorkKind;",
                new { aWorkKind },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vAffected == 0)
        {
            await aConnection.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO WorkFailureCount (WorkKind, FailureCount) VALUES (@aWorkKind, 1);",
                    new { aWorkKind },
                    cancellationToken: aCt)).ConfigureAwait(false);
            return 1;
        }

        return (int)await aConnection.ExecuteScalarAsync<long>(
            new CommandDefinition("SELECT FailureCount FROM WorkFailureCount WHERE WorkKind = @aWorkKind;", new { aWorkKind }, cancellationToken: aCt))
            .ConfigureAwait(false);
    }

    private static async Task ResetWorkFailureCountAsync(IDbConnection aConnection, string aWorkKind, CancellationToken aCt)
    {
        var vAffected = await aConnection.ExecuteAsync(
            new CommandDefinition(
                "UPDATE WorkFailureCount SET FailureCount = 0 WHERE WorkKind = @aWorkKind;",
                new { aWorkKind },
                cancellationToken: aCt)).ConfigureAwait(false);

        if (vAffected == 0)
        {
            await aConnection.ExecuteAsync(
                new CommandDefinition(
                    "INSERT INTO WorkFailureCount (WorkKind, FailureCount) VALUES (@aWorkKind, 0);",
                    new { aWorkKind },
                    cancellationToken: aCt)).ConfigureAwait(false);
        }
    }
}
