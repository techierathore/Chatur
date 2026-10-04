using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Guards;

/// <summary>
/// Refuses a tool request the acting role's rights do not allow (REQ-UI-030). Reads
/// <c>RoleRight</c> rows and nothing else (Architecture §5 "Guards").
/// </summary>
/// <remarks>
/// Implemented by cluster O ahead of cluster K/L (REQ-NFR-005 needs a real rule to test against
/// what it allows and what it refuses, not a stub). The rule: look up the session's role, then the
/// <c>RoleRight</c> row for that role and <see cref="ToolRequest.ToolName"/> as the <c>Action</c>.
/// No matching row is treated as a refusal — a right must be granted explicitly, never assumed —
/// so a role gains a tool only once K/L seed or edit its <c>RoleRight</c> rows accordingly.
/// </remarks>
public sealed class RoleRightsGuard : IToolGuard
{
    private readonly IDbConnectionFactory objDb;

    /// <summary>Creates the guard.</summary>
    /// <param name="aDb">Opens connections to Chatur's own database.</param>
    public RoleRightsGuard(IDbConnectionFactory aDb)
    {
        objDb = aDb;
    }

    /// <inheritdoc />
    public async Task<GuardResult> EvaluateAsync(ToolRequest aRequest, CancellationToken aCt = default)
    {
        using var vConnection = objDb.OpenConnection();
        var vCommand = new CommandDefinition(
            "SELECT RoleId FROM Session WHERE SessionId = @SessionId;",
            new { aRequest.SessionId },
            cancellationToken: aCt);
        var vRoleId = await vConnection.QuerySingleOrDefaultAsync<int?>(vCommand).ConfigureAwait(false);
        if (vRoleId is null)
        {
            return GuardResult.Refuse($"Session {aRequest.SessionId} has no role to check rights against.");
        }

        var vRightCommand = new CommandDefinition(
            "SELECT Allowed FROM RoleRight WHERE RoleId = @RoleId AND Action = @Action;",
            new { RoleId = vRoleId.Value, Action = aRequest.ToolName },
            cancellationToken: aCt);
        var vAllowed = await vConnection.QuerySingleOrDefaultAsync<long?>(vRightCommand).ConfigureAwait(false);

        if (vAllowed is null)
        {
            return GuardResult.Refuse($"The acting role has no right recorded for \"{aRequest.ToolName}\".");
        }

        return vAllowed.Value != 0
            ? GuardResult.Allow()
            : GuardResult.Refuse($"The acting role may not use \"{aRequest.ToolName}\".");
    }
}
