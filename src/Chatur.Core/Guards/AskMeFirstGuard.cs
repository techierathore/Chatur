using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Guards;

/// <summary>
/// Holds a write behind the owner's approval when the session's mode is "ask me first" (REQ-FN-035).
/// A "go ahead" session's writes pass this guard without stopping.
/// </summary>
/// <remarks>
/// Implemented by cluster O ahead of cluster G (REQ-NFR-005 needs a real rule to test what it
/// allows and what it refuses). This guard only ever refuses a request whose
/// <see cref="ToolRequest.ToolName"/> names one of the write tools in <see cref="WriteToolNames"/>,
/// and only when the session's stored <c>Mode</c> parses to <see cref="SessionMode.AskFirst"/>; every
/// other request — a read, a build, a run, or any write under <see cref="SessionMode.GoAhead"/> —
/// is allowed. <see cref="GuardResult"/> carries only allow/refuse, so turning a held write into a
/// proposed <c>Change</c> row (rather than simply stopping it) is the agent loop's job (cluster G)
/// once it sees this specific refusal reason; this guard's rule ends at "no tool write happens
/// unapproved".
/// </remarks>
public sealed class AskMeFirstGuard : IToolGuard
{
    /// <summary>
    /// The tool names this guard treats as a write to disk. Cluster G owns the agent loop's tool
    /// vocabulary; this list is the working set REQ-FN-044/045/046 and the <c>ToolRequest</c> doc
    /// comment's own example (<c>"edit-file"</c>) name today, and is additive-only to extend.
    /// </summary>
    private static readonly HashSet<string> WriteToolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "edit-file",
        "write-file",
        "create-file",
        "delete-file",
    };

    private readonly IDbConnectionFactory objDb;

    /// <summary>Creates the guard.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    public AskMeFirstGuard(IDbConnectionFactory aDb)
    {
        objDb = aDb;
    }

    /// <inheritdoc />
    public async Task<GuardResult> EvaluateAsync(ToolRequest aRequest, CancellationToken aCt = default)
    {
        if (!WriteToolNames.Contains(aRequest.ToolName))
        {
            return GuardResult.Allow();
        }

        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT Mode FROM Session WHERE SessionId = @SessionId;",
            new { aRequest.SessionId },
            cancellationToken: aCt);
        var vMode = await vConnection.QuerySingleOrDefaultAsync<string?>(vCommand).ConfigureAwait(false);

        if (vMode is not null
            && Enum.TryParse<SessionMode>(vMode, out var vParsedMode)
            && vParsedMode == SessionMode.AskFirst)
        {
            return GuardResult.Refuse(
                $"The session is in \"ask me first\"; \"{aRequest.ToolName}\" waits for the owner's approval as a proposed change.");
        }

        return GuardResult.Allow();
    }
}
