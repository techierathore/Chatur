using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Documents;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Chatur.Core.Agent.Commands;

/// <summary>Who typed a message, to which agent, in which project.</summary>
/// <param name="SessionId">The conversation.</param>
/// <param name="ProjectId">The project it belongs to.</param>
/// <param name="RoleId">The agent the message went to.</param>
/// <param name="Mode">The conversation's mode now.</param>
public sealed record AgentCommandContext(int SessionId, int ProjectId, int RoleId, SessionMode Mode);

/// <summary>What Chatur did with a message that was a command, or a question about the commands.</summary>
/// <param name="Reply">The answer Chatur gives itself, with no model; <see langword="null"/> for a guided command.</param>
/// <param name="Command">The command, when the message named one.</param>
/// <param name="Mode">The conversation's new mode, when the command changed it (<c>*yolo</c>).</param>
/// <param name="EndsConversation">Whether the conversation is closed (<c>*exit</c>).</param>
public sealed record AgentCommandOutcome(string? Reply, AgentCommand? Command, SessionMode? Mode = null, bool EndsConversation = false);

/// <summary>
/// Carries out an agent's commands in Chatur (BRD-249, BRD-241, REQ-FN-311, REQ-FN-308). A typed <c>*command</c> is
/// looked up in <see cref="AgentCommandCatalogue"/>: Chatur answers it itself, starts its process run, or hands the
/// agent the command's steps (<see cref="AgentCommandGuides"/>) for the turns that follow, until its file is written.
/// A question about the commands is answered from the same list, never by a model's guess.
/// </summary>
public interface IAgentCommandRunner
{
    /// <summary>Carries out the message when it is a command or a question about the commands; <see langword="null"/> when it is neither.</summary>
    /// <param name="aContext">The conversation.</param>
    /// <param name="aMessage">What the person typed.</param>
    /// <param name="aCt">A token that cancels the work.</param>
    Task<AgentCommandOutcome?> TryRunAsync(AgentCommandContext aContext, string aMessage, CancellationToken aCt = default);

    /// <summary>The steps of the guided command this conversation is in, for the agent's instructions; <see langword="null"/> when none.</summary>
    /// <param name="aSessionId">The conversation.</param>
    /// <param name="aCt">A token that cancels the read.</param>
    Task<string?> ActiveGuideAsync(int aSessionId, CancellationToken aCt = default);

    /// <summary>
    /// After an agent's reply in a guided command: when the command's file is now on disk, the command is over and a
    /// line saying so is returned; otherwise <see langword="null"/>.
    /// </summary>
    /// <param name="aSessionId">The conversation.</param>
    /// <param name="aCt">A token that cancels the check.</param>
    Task<string?> AfterReplyAsync(int aSessionId, CancellationToken aCt = default);
}

/// <inheritdoc />
public sealed partial class AgentCommandRunner : IAgentCommandRunner
{
    private const string GuidedKeyPrefix = "AgentSession.Guided.";

    private readonly IDbConnectionFactory objDb;
    private readonly IClock objClock;
    private readonly IServiceProvider objServices;

    /// <summary>Creates the runner.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    /// <param name="aClock">Stamps what the commands write.</param>
    /// <param name="aServices">Resolves the process, end-to-end and estimate actions when a command needs them (they depend on the agent actions, which depend on this).</param>
    public AgentCommandRunner(IDbConnectionFactory aDb, IClock aClock, IServiceProvider aServices)
    {
        objDb = aDb;
        objClock = aClock;
        objServices = aServices;
    }

    /// <inheritdoc />
    public async Task<AgentCommandOutcome?> TryRunAsync(AgentCommandContext aContext, string aMessage, CancellationToken aCt = default)
    {
        var vText = (aMessage ?? string.Empty).Trim();
        var vAgent = await AgentAsync(aContext.RoleId, aCt).ConfigureAwait(false);
        if (vAgent is null)
        {
            return null;
        }

        var vCommands = await CommandsOfAsync(vAgent, aCt).ConfigureAwait(false);
        if (vText.StartsWith('*'))
        {
            var vSpace = vText.IndexOfAny([' ', '\t', '\n']);
            var vCode = (vSpace < 0 ? vText[1..] : vText[1..vSpace]).Trim();
            var vArguments = vSpace < 0 ? string.Empty : vText[(vSpace + 1)..].Trim();
            var vCommand = AgentCommandCatalogue.Find(vAgent.Code, vCode)
                ?? vCommands.FirstOrDefault(c => AgentCommandCatalogue.Normalise(c.Code) == AgentCommandCatalogue.Normalise(vCode));
            await ClearGuidedAsync(aContext.SessionId, aCt).ConfigureAwait(false);
            if (vCommand is null)
            {
                return new AgentCommandOutcome(
                    $"The {vAgent.Name} has no *{vCode} command. Its commands are:\n\n{ListOf(vCommands)}", null);
            }

            return await RunAsync(aContext, vAgent, vCommands, vCommand, vArguments, aCt).ConfigureAwait(false);
        }

        if (IsCommandQuestion(vText))
        {
            return new AgentCommandOutcome(AnswerQuestion(vAgent, vCommands, vText), null);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<string?> ActiveGuideAsync(int aSessionId, CancellationToken aCt = default)
    {
        var vGuided = await GuidedAsync(aSessionId, aCt).ConfigureAwait(false);
        return vGuided?.Guide;
    }

    /// <inheritdoc />
    public async Task<string?> AfterReplyAsync(int aSessionId, CancellationToken aCt = default)
    {
        var vGuided = await GuidedAsync(aSessionId, aCt).ConfigureAwait(false);
        if (vGuided?.File is not { Length: > 0 } vFile)
        {
            return null;
        }

        var vRoot = await SessionRootAsync(aSessionId, aCt).ConfigureAwait(false);
        if (vRoot is null)
        {
            return null;
        }

        var vPath = Path.Combine(vRoot, vFile.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(vPath) || File.GetLastWriteTimeUtc(vPath) < vGuided.StartedUtc.AddSeconds(-2))
        {
            return null;
        }

        await ClearGuidedAsync(aSessionId, aCt).ConfigureAwait(false);
        return $"Chatur: *{vGuided.Code} is done — {vFile} is written ({new FileInfo(vPath).Length:N0} bytes).";
    }

    private async Task<AgentCommandOutcome> RunAsync(
        AgentCommandContext aContext, AgentRow aAgent, IReadOnlyList<AgentCommand> aCommands, AgentCommand aCommand, string aArguments, CancellationToken aCt)
    {
        var vProject = await ProjectAsync(aContext.ProjectId, aCt).ConfigureAwait(false);
        switch (aCommand.Kind)
        {
            case AgentCommandKind.Guided:
                {
                    var vFile = aCommand.Target?.Replace("{App}", vProject.App, StringComparison.Ordinal);
                    var vGuide = AgentCommandGuides.For(aCommand, vProject.App, aArguments, vFile, objClock.UtcNow.ToLocalTime());
                    await SetGuidedAsync(aContext.SessionId, new GuidedState(aCommand.Code, vFile, vGuide, objClock.UtcNow), aCt).ConfigureAwait(false);
                    return new AgentCommandOutcome(null, aCommand);
                }

            case AgentCommandKind.Process:
                return new AgentCommandOutcome(await StartProcessAsync(aContext, vProject, aCommand, aArguments, aCt).ConfigureAwait(false), aCommand);
        }

        return aCommand.Code switch
        {
            "help" => new AgentCommandOutcome($"{aAgent.Name} — commands. Type one with its star, for example `{aCommands.FirstOrDefault(c => c.Code != "help")?.Usage ?? "*help"}`.\n\n{ListOf(aCommands)}", aCommand),
            "yolo" => Yolo(aContext, aCommand),
            "exit" => new AgentCommandOutcome($"Goodbye from the {aAgent.Name}. This conversation is closed; the next one starts fresh.", aCommand, EndsConversation: true),
            "develop-end-to-end" => new AgentCommandOutcome(await DevelopSafelyAsync(aContext, vProject, aArguments, aCt).ConfigureAwait(false), aCommand),
            "develop-status" => new AgentCommandOutcome(await DevelopStatusAsync(aContext.ProjectId, aCt).ConfigureAwait(false), aCommand),
            "estimate-phase" => new AgentCommandOutcome(await EstimateAsync(aContext.ProjectId, aCt).ConfigureAwait(false), aCommand),
            "log-miss" => new AgentCommandOutcome(LogMiss(vProject, aArguments), aCommand),
            "generate-html" => new AgentCommandOutcome(GenerateHtml(vProject, aArguments), aCommand),
            "render-workflow-docs" => new AgentCommandOutcome(RenderWorkflowDocs(vProject), aCommand),
            "report" => new AgentCommandOutcome(Report(vProject, aArguments), aCommand),
            _ => new AgentCommandOutcome($"*{aCommand.Code}: {aCommand.Description}", aCommand)
        };
    }

    /// <summary>The commands as a numbered markdown list.</summary>
    private static string ListOf(IReadOnlyList<AgentCommand> aCommands)
    {
        var vText = new StringBuilder();
        for (var vIndex = 0; vIndex < aCommands.Count; vIndex++)
        {
            vText.Append(CultureInfo.InvariantCulture, $"{vIndex + 1}. `{aCommands[vIndex].Usage}` — {aCommands[vIndex].Description}\n");
        }

        return vText.ToString().TrimEnd();
    }

    /// <summary>Whether a message asks what commands the agent has, or whether it has one.</summary>
    /// <param name="aText">The message.</param>
    public static bool IsCommandQuestion(string aText)
    {
        if (aText.Length == 0 || aText.Length > 300 || !CommandWord().IsMatch(aText))
        {
            return false;
        }

        return aText.TrimEnd().EndsWith('?') || QuestionStart().IsMatch(aText);
    }

    /// <summary>The answer to a question about the commands, from the list: the ones it names, or every one.</summary>
    private static string AnswerQuestion(AgentRow aAgent, IReadOnlyList<AgentCommand> aCommands, string aText)
    {
        var vFlat = AgentCommandCatalogue.Normalise(aText);
        var vNamed = aCommands
            .Where(c => c.Code is not ("help" or "exit" or "yolo"))
            .Where(c => AgentCommandCatalogue.Normalise(c.Code) is { Length: >= 6 } vCode ? vFlat.Contains(vCode, StringComparison.Ordinal) : Regex.IsMatch(aText, $@"\b{Regex.Escape(c.Code)}\b", RegexOptions.IgnoreCase))
            .ToList();
        if (vNamed.Count > 0)
        {
            var vText = new StringBuilder($"Yes. The {aAgent.Name} has ");
            vText.Append(vNamed.Count == 1 ? "this command" : "these commands").Append(":\n\n");
            foreach (var vCommand in vNamed)
            {
                vText.Append(CultureInfo.InvariantCulture, $"- `{vCommand.Usage}` — {vCommand.Description}\n");
            }

            vText.Append("\nType it with its star to start it. `*help` lists all ").Append(aCommands.Count).Append('.');
            return vText.ToString();
        }

        return $"The {aAgent.Name} has {aCommands.Count} commands:\n\n{ListOf(aCommands)}\n\nType one with its star to start it.";
    }

    private AgentCommandOutcome Yolo(AgentCommandContext aContext, AgentCommand aCommand)
    {
        var vOn = aContext.Mode != SessionMode.GoAhead;
        return new AgentCommandOutcome(
            vOn
                ? "YOLO is on: this conversation is in **Go ahead**, so the agent's changes are written without waiting for your approval. Type `*yolo` again to turn it off."
                : "YOLO is off: this conversation is back in **Ask first**, so every change waits for your approval.",
            aCommand,
            vOn ? SessionMode.GoAhead : SessionMode.AskFirst);
    }

    /// <summary>Starts the command's process run; what the person typed after it is kept in the project for the run's steps to read.</summary>
    private async Task<string> StartProcessAsync(AgentCommandContext aContext, ProjectRow aProject, AgentCommand aCommand, string aArguments, CancellationToken aCt)
    {
        var vProcesses = objServices.GetRequiredService<IProcessActions>();
        var vGiven = WithoutAppName(aArguments, aProject.App);
        string? vKept = null;
        if (vGiven.Length > 0 && aProject.Root is { } vRoot)
        {
            // Reports go where triage-issues reads them (docs/issues/); anything else is the run's request.
            var vFolder = aCommand.Target is "triage-issues" or "fix-issues" ? "issues" : "requests";
            var vName = $"{objClock.UtcNow:yyyyMMdd-HHmmss}-{aCommand.Code}.md";
            vKept = $"docs/{vFolder}/{vName}";
            var vPath = Path.Combine(vRoot, "docs", vFolder, vName);
            Directory.CreateDirectory(Path.GetDirectoryName(vPath)!);
            await File.WriteAllTextAsync(vPath, $"# *{aCommand.Code}\n\nGiven by the person on {objClock.UtcNow:yyyy-MM-dd HH:mm} UTC:\n\n{vGiven}\n", aCt).ConfigureAwait(false);
        }

        var vStarted = await vProcesses.StartAsync(aContext.ProjectId, aCommand.Target!, aCt).ConfigureAwait(false);
        var vText = new StringBuilder();
        vText.Append(CultureInfo.InvariantCulture, $"{(vStarted.AlreadyRunning ? "Already running" : "Started")}: **{vStarted.ProcessCode}**, process run {vStarted.RunId}, on step {vStarted.CurrentStep}");
        if (vStarted.FirstStepWording.Length > 0 && vStarted.CurrentStep == 1)
        {
            vText.Append(" — ").Append(vStarted.FirstStepWording);
        }

        vText.Append(CultureInfo.InvariantCulture, $".\n\nFollow it, answer its stops and see its checks in [Processes ▸ run {vStarted.RunId}](/processes/{vStarted.RunId}/run).");
        if (vKept is not null)
        {
            vText.Append(CultureInfo.InvariantCulture, $" What you gave with the command is kept in `{vKept}` for its steps to read.");
        }

        if (aCommand.Code == "triage-and-fix")
        {
            using var vConnection = objDb.OpenConnection();
            await vConnection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Setting (Key, Value) VALUES (@Key, 'fix-issues') ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value;",
                new { Key = ProcessRunActions.FollowUpSettingKey(vStarted.RunId) },
                cancellationToken: aCt)).ConfigureAwait(false);
            vText.Append("\n\nWhen the triage run finishes, Chatur starts the **fix-issues** run on the same project.");
        }

        return vText.ToString();
    }

    /// <summary>
    /// *develop-end-to-end, with a refusal from the run engine (a brief that is too short, a run that cannot be stopped)
    /// answered in the chat as the reply, never thrown at the conversation (REQ-UI-122).
    /// </summary>
    private async Task<string> DevelopSafelyAsync(AgentCommandContext aContext, ProjectRow aProject, string aArguments, CancellationToken aCt)
    {
        try
        {
            return await DevelopAsync(aContext, aProject, aArguments, aCt).ConfigureAwait(false);
        }
        catch (Exception vError) when (vError is ArgumentException or InvalidOperationException)
        {
            var vReason = vError is ArgumentException vArgument && vArgument.ParamName is { Length: > 0 } vName
                ? vArgument.Message.Replace($" (Parameter '{vName}')", string.Empty, StringComparison.Ordinal)
                : vError.Message;
            return $"The end-to-end run did not start: {vReason} Type `*develop-end-to-end {{App}} {{brief}}` with a brief of at least 40 characters, saying what should be true when the run finishes.";
        }
        catch (System.Data.Common.DbException vError)
        {
            // A database refusal (a leftover row, a locked file) is said in the chat, never left to end the conversation.
            return $"The end-to-end run did not start: Chatur could not record it ({vError.GetType().Name}). Close and reopen the project, then type the command again.";
        }
    }

    private async Task<string> DevelopAsync(AgentCommandContext aContext, ProjectRow aProject, string aArguments, CancellationToken aCt)
    {
        var vRuns = objServices.GetRequiredService<IAutoRunActions>();
        var vArgs = WithoutAppName(aArguments, aProject.App);
        var vCurrent = await vRuns.CurrentAsync(aContext.ProjectId, aCt).ConfigureAwait(false);
        if (vArgs.Contains("--follow", StringComparison.OrdinalIgnoreCase))
        {
            return vCurrent is null ? "No end-to-end run is going on this project." : DescribeRun(vCurrent) + "\n\nWatch it live in [Project ▸ End-to-end run](/auto).";
        }

        if (vArgs.Contains("--stop", StringComparison.OrdinalIgnoreCase))
        {
            if (vCurrent is null)
            {
                return "No end-to-end run is going on this project, so there is nothing to stop.";
            }

            var vStopped = await vRuns.StopAsync(vCurrent.AutoRunId, aCt).ConfigureAwait(false);
            return $"Stopped end-to-end run {vCurrent.AutoRunId}. {(vStopped?.StateText ?? string.Empty)} `*develop-end-to-end --resume` carries it on from the step it stopped in.";
        }

        if (vArgs.Contains("--resume", StringComparison.OrdinalIgnoreCase))
        {
            var vTarget = vCurrent?.AutoRunId ?? (await vRuns.ListAsync(aContext.ProjectId, aCt).ConfigureAwait(false)).FirstOrDefault()?.AutoRunId;
            if (vTarget is not { } vRunId)
            {
                return "This project has no end-to-end run to resume. `*develop-end-to-end {brief}` starts one.";
            }

            var vResumed = await vRuns.ResumeAsync(vRunId, aCt).ConfigureAwait(false);
            return vResumed is null ? $"End-to-end run {vRunId} could not be resumed." : "Resumed. " + DescribeRun(vResumed);
        }

        if (vCurrent is not null)
        {
            return "An end-to-end run is already going on this project. " + DescribeRun(vCurrent);
        }

        var vBrief = Regex.Replace(vArgs, @"--no-git|--model\s+\S+", string.Empty, RegexOptions.IgnoreCase).Trim();
        var vStarted = await vRuns.StartAsync(aContext.ProjectId, vBrief, aCt).ConfigureAwait(false);
        var vNotes = vArgs.Contains("--model", StringComparison.OrdinalIgnoreCase)
            ? " Each step runs on the model routing picks for its agent; choose a model in Settings ▸ Routing to change that."
            : string.Empty;
        return $"Started end-to-end run {vStarted.AutoRunId}: {vStarted.Chain.Count} processes, from **{vStarted.Chain.FirstOrDefault()?.ProcessCode}** to **{vStarted.Chain.LastOrDefault()?.ProcessCode}**. Chatur never commits; the owner does.{vNotes}\n\nFollow it in [Project ▸ End-to-end run](/auto), or type `*develop-status`.";
    }

    private async Task<string> DevelopStatusAsync(int aProjectId, CancellationToken aCt)
    {
        var vRuns = objServices.GetRequiredService<IAutoRunActions>();
        var vCurrent = await vRuns.CurrentAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vCurrent is not null)
        {
            return "Going: " + DescribeRun(vCurrent);
        }

        var vLast = (await vRuns.ListAsync(aProjectId, aCt).ConfigureAwait(false)).FirstOrDefault();
        return vLast is null
            ? "No end-to-end run has been started on this project."
            : $"No end-to-end run is going. The last one, run {vLast.AutoRunId}, is {vLast.State.ToString().ToLowerInvariant()} (started {vLast.StartedUtc.ToLocalTime():d MMM HH:mm}).";
    }

    private static string DescribeRun(AutoRunView aRun)
    {
        var vNow = aRun.Now is { } vStep
            ? $" Now: {vStep.Agent} on **{vStep.ProcessCode}** (process {vStep.ProcessPosition} of {vStep.ProcessCount}), step {vStep.StepPosition} — {vStep.StepWording}."
            : string.Empty;
        return $"Run {aRun.AutoRunId}: {aRun.StateText}, {aRun.ProgressPercent}% done.{vNow} Open requirements: {aRun.Open.Total} ({aRun.Open.NotStarted} not started, {aRun.Open.InHand} in hand, {aRun.Open.SentBack} sent back). Tokens so far: {aRun.TokensOutSoFar:N0}.";
    }

    private async Task<string> EstimateAsync(int aProjectId, CancellationToken aCt)
    {
        var vEstimates = objServices.GetRequiredService<IEstimateActions>();
        var vEstimate = await vEstimates.EstimateAsync(aProjectId, aCt).ConfigureAwait(false);
        if (vEstimate is null || vEstimate.NoChecklist)
        {
            return "There is no checklist yet, so there is nothing to estimate. Day 1 (`*day1-greenfield`) writes it.";
        }

        if (vEstimate.NoEstimateYet)
        {
            return $"No estimate yet for phase {vEstimate.Phase}: no run on record does the same kind of work. The first build gives the figures.";
        }

        var vText = new StringBuilder($"Estimate for phase {vEstimate.Phase} of {vEstimate.App}, {vEstimate.Rows.Count} open rows (low / likely / high):\n\n| Work | Tokens out | Time | Based on |\n|---|---|---|---|\n");
        foreach (var vTotal in vEstimate.Totals)
        {
            var vTokens = vTotal.TokensOut is { } vT ? $"{vT.Low:N0} / {vT.Mid:N0} / {vT.High:N0}" : "not known";
            var vTime = vTotal.DurationS is { } vD ? $"{Minutes(vD.Low)} / {Minutes(vD.Mid)} / {Minutes(vD.High)}" : "not known";
            vText.Append(CultureInfo.InvariantCulture, $"| {vTotal.Step} | {vTokens} | {vTime} | {vTotal.Basis}, {vTotal.BasisRuns} run(s) |\n");
        }

        return vText.ToString().TrimEnd();
    }

    private static string Minutes(long aSeconds) => aSeconds < 90 ? $"{aSeconds} s" : $"{Math.Round(aSeconds / 60.0):0} min";

    /// <summary>
    /// *log-miss: one miss record in the project's <c>docs/metrics/misses.jsonl</c> (the stream the run report reads) and one
    /// line in <c>docs/{App}-Misses.md</c>, in the person's own words. Never argues and never fixes.
    /// </summary>
    private string LogMiss(ProjectRow aProject, string aArguments)
    {
        if (aProject.Root is not { } vRoot)
        {
            return "The project's folder is not there, so the miss could not be written.";
        }

        var vArgs = WithoutAppName(aArguments, aProject.App);
        var vFixed = vArgs.Contains("--fixed", StringComparison.OrdinalIgnoreCase);
        var vReq = RequirementId().Match(vArgs) is { Success: true } vMatch ? vMatch.Value : null;
        var vQuoted = Regex.Match(vArgs, "[\"“]([^\"”]+)[\"”]");
        var vWhat = vQuoted.Success
            ? vQuoted.Groups[1].Value.Trim()
            : Regex.Replace(Regex.Replace(vArgs, "--fixed", string.Empty, RegexOptions.IgnoreCase), @"\bREQ-(UI|FN|NFR|RAG)-\d{3,4}\b", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (vWhat.Length == 0)
        {
            return "Say what was missed, in one sentence: `*log-miss \"the Run button stayed grey after New project\"`.";
        }

        var vNow = objClock.UtcNow;
        var vMetrics = Path.Combine(vRoot, "docs", "metrics");
        Directory.CreateDirectory(vMetrics);
        var vStream = Path.Combine(vMetrics, "misses.jsonl");
        var vToday = vNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var vCount = File.Exists(vStream)
            ? File.ReadLines(vStream).Count(l => l.Contains($"\"MISS-{aProject.App}-{vToday}-", StringComparison.Ordinal) && l.Contains("\"kind\":\"miss\"", StringComparison.Ordinal))
            : 0;
        var vId = $"MISS-{aProject.App}-{vToday}-{vCount + 1:00}";
        var vRecord = new Dictionary<string, object?>
        {
            ["kind"] = "miss",
            ["v"] = 1,
            ["ts"] = vNow.ToString("O", CultureInfo.InvariantCulture),
            ["app"] = aProject.App,
            ["miss_id"] = vId,
            ["req_id"] = vReq,
            ["what"] = vWhat,
            ["found_by"] = "owner",
            ["found_phase"] = "log-miss",
            ["severity"] = "major",
            ["harness"] = "chatur"
        };
        var vLines = new StringBuilder(JsonSerializer.Serialize(vRecord)).Append('\n');
        if (vFixed)
        {
            vLines.Append(JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["kind"] = "miss-fix",
                ["v"] = 1,
                ["ts"] = vNow.ToString("O", CultureInfo.InvariantCulture),
                ["app"] = aProject.App,
                ["miss_id"] = vId,
                ["verdict"] = "Verified",
                ["fix_cost"] = "none",
                ["harness"] = "chatur"
            })).Append('\n');
        }

        File.AppendAllText(vStream, vLines.ToString());
        var vReadable = Path.Combine(vRoot, "docs", $"{aProject.App}-Misses.md");
        if (!File.Exists(vReadable))
        {
            File.WriteAllText(vReadable, $"# {aProject.App} — Misses\n\nOne line per miss, in the words it was reported in. The records are in docs/metrics/misses.jsonl.\n\n| Id | Date | Requirement | What was missed | State |\n|---|---|---|---|---|\n");
        }

        File.AppendAllText(vReadable, $"| {vId} | {vNow:yyyy-MM-dd} | {vReq ?? "none named"} | {vWhat.Replace("|", "/", StringComparison.Ordinal)} | {(vFixed ? "fixed" : "open")} |\n");
        return $"Logged **{vId}**{(vReq is null ? string.Empty : $" against {vReq}")}: \"{vWhat}\" — {(vFixed ? "recorded as already fixed" : "open")}. Written to `docs/metrics/misses.jsonl` and `docs/{aProject.App}-Misses.md`.{(vFixed ? string.Empty : " `*fix-issues` fixes it.")}";
    }

    private static string GenerateHtml(ProjectRow aProject, string aArguments)
    {
        if (aProject.Root is not { } vRoot)
        {
            return "The project's folder is not there.";
        }

        var vPath = aArguments.Trim().Trim('"');
        if (vPath.Length == 0)
        {
            return "Name the markdown file: `*generate-html docs/MyApp-BRD.md`.";
        }

        var vFull = Path.GetFullPath(Path.Combine(vRoot, vPath));
        if (!vFull.StartsWith(Path.GetFullPath(vRoot), StringComparison.OrdinalIgnoreCase) || !File.Exists(vFull))
        {
            return $"`{vPath}` is not a file in this project.";
        }

        var vOut = WriteHtml(vFull);
        return $"Written: `{Path.GetRelativePath(vRoot, vOut).Replace('\\', '/')}`.";
    }

    private static string RenderWorkflowDocs(ProjectRow aProject)
    {
        if (aProject.Root is not { } vRoot)
        {
            return "The project's folder is not there.";
        }

        var vDocs = Path.Combine(vRoot, "docs");
        var vFiles = new List<string>();
        if (Directory.Exists(vDocs))
        {
            vFiles.AddRange(Directory.GetFiles(vDocs, "*-BRD.md"));
            vFiles.AddRange(Directory.GetFiles(vDocs, "*-Architecture.md"));
            vFiles.AddRange(Directory.GetFiles(vDocs, "*-Brief.md"));
        }

        var vStatus = Path.Combine(vRoot, "PROJECT-STATUS.md");
        if (File.Exists(vStatus))
        {
            vFiles.Add(vStatus);
        }

        if (vFiles.Count == 0)
        {
            return "There is no BRD, Architecture, brief or PROJECT-STATUS.md in this project yet, so nothing was rendered.";
        }

        var vWritten = vFiles.Order(StringComparer.OrdinalIgnoreCase).Select(f => "- `" + Path.GetRelativePath(vRoot, WriteHtml(f)).Replace('\\', '/') + "`");
        return "Rendered to HTML beside each document:\n\n" + string.Join('\n', vWritten);
    }

    private static string WriteHtml(string aMarkdownFile)
    {
        var vOut = Path.ChangeExtension(aMarkdownFile, ".html");
        var vMarkdown = File.ReadAllText(aMarkdownFile);
        var vTitle = vMarkdown.Split('\n').FirstOrDefault(l => l.StartsWith("# ", StringComparison.Ordinal))?[2..].Trim()
            ?? Path.GetFileNameWithoutExtension(aMarkdownFile);
        File.WriteAllText(vOut, MarkdownHtml.Page(vTitle, vMarkdown));
        return vOut;
    }

    /// <summary>*report {scope}: the Status and Remarks of the rows in scope, read from every checklist, running nothing.</summary>
    private static string Report(ProjectRow aProject, string aArguments)
    {
        var vDocs = aProject.Root is { } vRoot ? Path.Combine(vRoot, "docs") : null;
        if (vDocs is null || !Directory.Exists(vDocs) || Directory.GetFiles(vDocs, "*-Checklist.md").Length == 0)
        {
            return "This project has no checklist yet, so there is nothing to report.";
        }

        var vScope = WithoutAppName(aArguments, aProject.App).Trim();
        var vIds = RequirementId().Matches(vScope).Select(m => m.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var vRows = new List<string[]>();
        foreach (var vFile in Directory.GetFiles(vDocs, "*-Checklist.md").Order(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var vLine in File.ReadLines(vFile))
            {
                var vCells = vLine.Split('|').Select(c => c.Trim()).ToArray();
                if (vCells.Length < 7 || !RequirementId().IsMatch(vCells[1]) || RequirementId().Match(vCells[1]).Value != vCells[1])
                {
                    continue;
                }

                var vId = vCells[1];
                var vIn = vIds.Count > 0 ? vIds.Contains(vId)
                    : vScope.StartsWith("ui", StringComparison.OrdinalIgnoreCase) ? vId.StartsWith("REQ-UI-", StringComparison.Ordinal)
                    : vScope.StartsWith("func", StringComparison.OrdinalIgnoreCase) ? vId.StartsWith("REQ-FN-", StringComparison.Ordinal) || vId.StartsWith("REQ-RAG-", StringComparison.Ordinal)
                    : true;
                if (vIn)
                {
                    vRows.Add([vId, vCells[3], vCells[5]]);
                }
            }
        }

        if (vRows.Count == 0)
        {
            return $"No checklist row is in scope \"{vScope}\".";
        }

        var vText = new StringBuilder($"{vRows.Count} row(s) in scope{(vScope.Length == 0 ? string.Empty : $" \"{vScope}\"")}: ");
        vText.Append(string.Join(", ", vRows.GroupBy(r => r[1]).Select(g => $"{g.Count()} {g.Key}"))).Append(".\n\n| Row | Status | Remarks |\n|---|---|---|\n");
        foreach (var vRow in vRows.Take(60))
        {
            var vRemark = vRow[2].Length > 160 ? vRow[2][..157] + "…" : vRow[2];
            vText.Append(CultureInfo.InvariantCulture, $"| {vRow[0]} | {vRow[1]} | {vRemark} |\n");
        }

        if (vRows.Count > 60)
        {
            vText.Append(CultureInfo.InvariantCulture, $"\n{vRows.Count - 60} more; name ids to see them.");
        }

        return vText.ToString().TrimEnd();
    }

    /// <summary>What the person typed after a command, without the project's name when they gave it first (TechieFlow's <c>{App}</c>).</summary>
    private static string WithoutAppName(string aArguments, string aApp)
    {
        var vText = aArguments.Trim();
        return vText.StartsWith(aApp, StringComparison.OrdinalIgnoreCase) && (vText.Length == aApp.Length || char.IsWhiteSpace(vText[aApp.Length]))
            ? vText[aApp.Length..].Trim()
            : vText;
    }

    private async Task<AgentRow?> AgentAsync(int aRoleId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        return await vConnection.QuerySingleOrDefaultAsync<AgentRow>(new CommandDefinition(
            "SELECT Code, Name FROM Role WHERE RoleId = @aRoleId;", new { aRoleId }, cancellationToken: aCt)).ConfigureAwait(false);
    }

    /// <summary>The agent's commands: Chatur's own list for the four agents, the stored ones for an agent the owner added.</summary>
    private async Task<IReadOnlyList<AgentCommand>> CommandsOfAsync(AgentRow aAgent, CancellationToken aCt)
    {
        if (AgentCommandCatalogue.Covers(aAgent.Code))
        {
            return AgentCommandCatalogue.ForRole(aAgent.Code);
        }

        using var vConnection = objDb.OpenConnection();
        var vStored = await vConnection.QueryAsync<(string Command, string? Description)>(new CommandDefinition(
            "SELECT c.Command, c.Description FROM RoleCommand c JOIN Role r ON r.RoleId = c.RoleId WHERE r.Code = @Code ORDER BY c.RoleCommandId;",
            new { aAgent.Code }, cancellationToken: aCt)).ConfigureAwait(false);
        return vStored.Select(s => new AgentCommand(aAgent.Code, s.Command.TrimStart('*'), string.Empty, s.Description ?? string.Empty, AgentCommandKind.Guided)).ToList();
    }

    private async Task<ProjectRow> ProjectAsync(int aProjectId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vRow = await vConnection.QuerySingleOrDefaultAsync<(string Name, string Path)>(new CommandDefinition(
            "SELECT Name, Path FROM Project WHERE ProjectId = @aProjectId;", new { aProjectId }, cancellationToken: aCt)).ConfigureAwait(false);
        return new ProjectRow(AppName(vRow.Name ?? "Project"), RootOf(vRow.Path));
    }

    private async Task<string?> SessionRootAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vPath = await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT p.Path FROM Session s JOIN Project p ON p.ProjectId = s.ProjectId WHERE s.SessionId = @aSessionId;",
            new { aSessionId }, cancellationToken: aCt)).ConfigureAwait(false);
        return RootOf(vPath);
    }

    /// <summary>The project's folder: a solution's own folder, or the folder itself; <see langword="null"/> when it is gone.</summary>
    private static string? RootOf(string? aPath)
    {
        if (string.IsNullOrEmpty(aPath))
        {
            return null;
        }

        if (File.Exists(aPath))
        {
            return Path.GetDirectoryName(aPath);
        }

        return Directory.Exists(aPath) ? aPath : null;
    }

    /// <summary>The project's name as TechieFlow's <c>{App}</c>: letters and digits only.</summary>
    /// <param name="aName">The project's name.</param>
    public static string AppName(string aName)
    {
        var vName = new string(aName.Where(char.IsLetterOrDigit).ToArray());
        return vName.Length == 0 ? "Project" : vName;
    }

    private async Task<GuidedState?> GuidedAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        var vJson = await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
            "SELECT Value FROM Setting WHERE Key = @Key;", new { Key = GuidedKeyPrefix + aSessionId }, cancellationToken: aCt)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(vJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<GuidedState>(vJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task SetGuidedAsync(int aSessionId, GuidedState aState, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Setting (Key, Value) VALUES (@Key, @Value) ON CONFLICT (Key) DO UPDATE SET Value = excluded.Value;",
            new { Key = GuidedKeyPrefix + aSessionId, Value = JsonSerializer.Serialize(aState) },
            cancellationToken: aCt)).ConfigureAwait(false);
    }

    private async Task ClearGuidedAsync(int aSessionId, CancellationToken aCt)
    {
        using var vConnection = objDb.OpenConnection();
        await vConnection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM Setting WHERE Key = @Key;", new { Key = GuidedKeyPrefix + aSessionId }, cancellationToken: aCt)).ConfigureAwait(false);
    }

    [GeneratedRegex(@"\bcommands?\b", RegexOptions.IgnoreCase)]
    private static partial Regex CommandWord();

    [GeneratedRegex(@"^\s*(what|which|do|don'?t|does|have|has|list|show|tell|can|any|is there|are there)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuestionStart();

    [GeneratedRegex(@"REQ-(UI|FN|NFR|RAG)-\d{3,4}", RegexOptions.IgnoreCase)]
    private static partial Regex RequirementId();

    /// <summary>A guided command in progress in one conversation.</summary>
    /// <param name="Code">The command.</param>
    /// <param name="File">The file it writes, relative to the project, or <see langword="null"/>.</param>
    /// <param name="Guide">The steps given to the agent.</param>
    /// <param name="StartedUtc">When it was typed.</param>
    public sealed record GuidedState(string Code, string? File, string Guide, DateTime StartedUtc);

    private sealed class AgentRow
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private sealed record ProjectRow(string App, string? Root);
}
