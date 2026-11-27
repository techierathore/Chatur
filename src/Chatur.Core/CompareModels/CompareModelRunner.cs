using System.Text;
using System.Text.Json;
using Chatur.Core.Actions;
using TechieRag.Abstractions;
using TechieRag.Models;

namespace Chatur.Core.CompareModels;

/// <summary>What one model did on a comparison's task: its answer and what TechieRag reported it used.</summary>
/// <param name="Answer">The model's text, every turn of it, as Markdown.</param>
/// <param name="TokensIn">Tokens in over every call, or <see langword="null"/> when no call reported any.</param>
/// <param name="TokensOut">Tokens out over every call, or <see langword="null"/> when no call reported any.</param>
/// <param name="CostUsd">TechieRag's own price over every call, or <see langword="null"/> when it priced none.</param>
public sealed record CompareModelOutcome(string Answer, long? TokensIn, long? TokensOut, decimal? CostUsd);

/// <summary>
/// Runs one model on one comparison's task through TechieRag, the way the chat does (streamed events, tool calls fed back
/// as tool results), with the tools of a <see cref="CompareWorkspace"/> only (REQ-FN-328, REQ-FN-329). A question offers
/// <c>read-file</c> alone, so there is no tool that writes; a task that changes files adds <c>write-file</c>, and its
/// workspace is the model's own copy. No source-control tool and no build tool is offered at all.
/// </summary>
public static class CompareModelRunner
{
    /// <summary>The read tool's name, the same word the chat's agent uses.</summary>
    public const string ReadTool = "read-file";

    /// <summary>The write tool's name; offered only for a task that changes files.</summary>
    public const string WriteTool = "write-file";

    /// <summary>The model turns one run allows before it is stopped with what it has said so far.</summary>
    private const int MaxTurns = 25;

    private const string QuestionPrompt =
        "You are answering a question about a software project. Read its files with the read-file tool: give it a path " +
        "relative to the project's root to read a file, or a folder (\".\" for the whole project) to list what is in it. " +
        "You have no tool that changes anything, and nothing you do changes the project. Read what you need, then answer " +
        "in Markdown, briefly.";

    private const string ChangePrompt =
        "You are doing a task on a software project, in your own copy of it. read-file reads a file, or lists a folder " +
        "(\".\" for the whole project); write-file writes a file's complete new text. Paths are relative to the project's " +
        "root. Do the task, then say in a few lines of Markdown what you changed.";

    /// <summary>The tools a kind offers (REQ-FN-328: a question has no tool that writes).</summary>
    /// <param name="aKind">Question or a task that changes files.</param>
    public static IReadOnlyList<ToolDefinition> ToolsFor(CompareKind aKind)
    {
        var vTools = new List<ToolDefinition>
        {
            new()
            {
                Name = ReadTool,
                Description = "Reads a file's text from the project, given its path relative to the project's root. Given a folder (or \".\" for the whole project) it lists what is in it instead.",
                ParametersSchema = """{"type":"object","properties":{"path":{"type":"string"}},"required":["path"]}"""
            }
        };

        if (aKind == CompareKind.Change)
        {
            vTools.Add(new ToolDefinition
            {
                Name = WriteTool,
                Description = "Writes a file's new, complete text in your copy of the project, given its path relative to the project's root.",
                ParametersSchema = """{"type":"object","properties":{"path":{"type":"string"},"content":{"type":"string"}},"required":["path","content"]}"""
            });
        }

        return vTools;
    }

    /// <summary>
    /// Runs the model to the end of its answer, executing every tool call it makes against <paramref name="aWorkspace"/>.
    /// </summary>
    /// <param name="aProvider">The model, built by TechieRag.</param>
    /// <param name="aModelIdentifier">The model's identifier, which TechieRag's price table is read by.</param>
    /// <param name="aKind">Question or a task that changes files; decides the tools.</param>
    /// <param name="aWorkspace">The folder its tools reach: the project for a question, its own copy for a task.</param>
    /// <param name="aTask">The task.</param>
    /// <param name="aSessionId">An id sent to a service that routes by conversation.</param>
    /// <param name="aCt">A token that stops the model.</param>
    /// <returns>Its answer and what it used.</returns>
    public static async Task<CompareModelOutcome> RunAsync(
        ILlmProvider aProvider,
        string aModelIdentifier,
        CompareKind aKind,
        CompareWorkspace aWorkspace,
        string aTask,
        string aSessionId,
        CancellationToken aCt)
    {
        var vHistory = new List<ChatMessage>
        {
            ChatMessage.System(aKind == CompareKind.Question ? QuestionPrompt : ChangePrompt),
            ChatMessage.User(aTask)
        };
        var vOptions = new LlmCompletionOptions { Tools = ToolsFor(aKind), SessionId = aSessionId };
        var vAnswer = new StringBuilder();
        long? vTokensIn = null;
        long? vTokensOut = null;
        // A call's own usage usually carries no price; then TechieRag's price table gives it, as for the key's count (KeyUsageRecorder).
        // One call without a price makes the whole cost "no price", never a part of it.
        decimal? vCost = 0m;

        for (var vTurn = 0; vTurn < MaxTurns; vTurn++)
        {
            var vText = new StringBuilder();
            var vCalls = new List<ToolCall>();
            await foreach (var vEvent in aProvider.ChatStreamEventsAsync(vHistory, vOptions, aCt).WithCancellation(aCt).ConfigureAwait(false))
            {
                switch (vEvent.Kind)
                {
                    case LlmStreamEventKind.TextDelta:
                        vText.Append(vEvent.Text);
                        break;
                    case LlmStreamEventKind.ToolCall when vEvent.ToolCall is not null:
                        vCalls.Add(vEvent.ToolCall);
                        break;
                    case LlmStreamEventKind.Completed when vEvent.Usage is not null:
                        vTokensIn = (vTokensIn ?? 0) + vEvent.Usage.InputTokens;
                        vTokensOut = (vTokensOut ?? 0) + vEvent.Usage.OutputTokens;
                        var vPrice = vEvent.Usage.EstimatedCostUsd > 0m
                            ? vEvent.Usage.EstimatedCostUsd
                            : Measurements.KeyUsageRecorder.PriceOf(aModelIdentifier, vEvent.Usage.InputTokens, vEvent.Usage.OutputTokens, vEvent.Usage.IsEstimated);
                        vCost = vCost is null || vPrice is null ? null : vCost + vPrice;
                        break;
                }
            }

            if (vText.Length > 0)
            {
                if (vAnswer.Length > 0)
                {
                    vAnswer.Append("\n\n");
                }

                vAnswer.Append(vText);
            }

            var vResponse = new LlmResponse { Content = vText.ToString(), Usage = new TokenUsage() };
            if (vCalls.Count > 0)
            {
                vResponse.ToolCalls = vCalls;
            }

            if (vCalls.Count > 0 || vText.Length > 0)
            {
                vHistory.Add(vResponse.ToChatMessage());
            }

            if (vCalls.Count == 0)
            {
                return new CompareModelOutcome(vAnswer.ToString().Trim(), vTokensIn, vTokensOut, vCost > 0m ? vCost : null);
            }

            foreach (var vCall in vCalls)
            {
                vHistory.Add(ChatMessage.Tool(vCall.Id, await RunToolAsync(aKind, aWorkspace, vCall, aCt).ConfigureAwait(false)));
            }
        }

        vAnswer.Append(vAnswer.Length > 0 ? "\n\n" : string.Empty)
            .Append($"(Stopped after {MaxTurns} turns without a final answer.)");
        return new CompareModelOutcome(vAnswer.ToString().Trim(), vTokensIn, vTokensOut, vCost > 0m ? vCost : null);
    }

    /// <summary>Runs one tool call; a write on a question is refused here as well as never offered.</summary>
    private static async Task<string> RunToolAsync(CompareKind aKind, CompareWorkspace aWorkspace, ToolCall aCall, CancellationToken aCt)
    {
        var vArgs = Parse(aCall.ArgumentsJson);
        return aCall.Name switch
        {
            ReadTool => await aWorkspace.ReadAsync(Text(vArgs, "path"), aCt).ConfigureAwait(false),
            WriteTool when aKind == CompareKind.Change => await aWorkspace.WriteAsync(Text(vArgs, "path"), Text(vArgs, "content"), aCt).ConfigureAwait(false),
            WriteTool => "This is a question: nothing may be written, and nothing was.",
            _ => $"There is no tool called \"{aCall.Name}\" here; only {string.Join(" and ", ToolsFor(aKind).Select(aTool => aTool.Name))}."
        };
    }

    private static JsonElement Parse(string? aJson)
    {
        if (string.IsNullOrWhiteSpace(aJson))
        {
            return default;
        }

        try
        {
            return JsonDocument.Parse(aJson).RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static string Text(JsonElement aElement, string aName) =>
        aElement.ValueKind == JsonValueKind.Object && aElement.TryGetProperty(aName, out var vValue) && vValue.ValueKind == JsonValueKind.String
            ? vValue.GetString() ?? string.Empty
            : string.Empty;
}
