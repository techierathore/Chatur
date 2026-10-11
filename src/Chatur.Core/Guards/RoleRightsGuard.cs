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
    private const string WriteTool = "edit-file";
    private const string DocsAction = "write-docs";

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

        if (vAllowed is not null && vAllowed.Value != 0)
        {
            return GuardResult.Allow();
        }

        // REQ-FN-103: a file write is also allowed to a role that holds "write-docs" when the path, fully
        // resolved, is inside <project>/docs/. Any other tool, and any other path, needs its own right.
        if (string.Equals(aRequest.ToolName, WriteTool, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(aRequest.FilePath))
        {
            var vDocsRight = await vConnection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
                "SELECT Allowed FROM RoleRight WHERE RoleId = @RoleId AND Action = @Action;",
                new { RoleId = vRoleId.Value, Action = DocsAction },
                cancellationToken: aCt)).ConfigureAwait(false);
            if (vDocsRight is not null && vDocsRight.Value != 0)
            {
                var vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
                    "SELECT p.Path FROM Session s JOIN Project p ON p.ProjectId = s.ProjectId WHERE s.SessionId = @SessionId;",
                    new { aRequest.SessionId },
                    cancellationToken: aCt)).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(vProjectPath) && IsInsideDocs(vProjectPath, aRequest.FilePath))
                {
                    return GuardResult.Allow();
                }

                return GuardResult.Refuse(
                    $"The acting role holds \"writes documents\" only, and \"{aRequest.FilePath}\" does not resolve to a path inside docs/; writing there needs the \"{WriteTool}\" right.");
            }
        }

        if (vAllowed is null)
        {
            return GuardResult.Refuse($"The acting role has no right recorded for \"{aRequest.ToolName}\".");
        }

        return GuardResult.Refuse($"The acting role may not use \"{aRequest.ToolName}\".");
    }

    /// <summary>
    /// True when <paramref name="aRelativePath"/>, joined to the project root and fully resolved
    /// (<c>..</c>, absolute paths and links followed), lies inside the project's <c>docs</c> folder.
    /// </summary>
    internal static bool IsInsideDocs(string aProjectPath, string aRelativePath)
    {
        try
        {
            var vRoot = Path.GetFullPath(aProjectPath);
            var vDocs = ResolveLinks(Path.Combine(vRoot, "docs"));
            var vTarget = ResolveLinks(Path.GetFullPath(Path.Combine(vRoot, aRelativePath)));
            var vPrefix = vDocs.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var vComparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return vTarget.StartsWith(vPrefix, vComparison);
        }
        catch (Exception vException) when (vException is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Walks a full path one segment at a time, replacing every existing link by its final target.</summary>
    private static string ResolveLinks(string aFullPath)
    {
        var vRoot = Path.GetPathRoot(aFullPath) ?? string.Empty;
        var vCurrent = vRoot;
        var vPending = new Stack<string>(aFullPath[vRoot.Length..]
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).Reverse());
        var vHops = 0;
        while (vPending.Count > 0)
        {
            var vSegment = vPending.Pop();
            if (vSegment == ".")
            {
                continue;
            }

            if (vSegment == "..")
            {
                vCurrent = Path.GetDirectoryName(vCurrent.TrimEnd(Path.DirectorySeparatorChar)) ?? vRoot;
                continue;
            }

            var vNext = Path.Combine(vCurrent, vSegment);
            FileSystemInfo vInfo = Directory.Exists(vNext) ? new DirectoryInfo(vNext) : new FileInfo(vNext);
            if (vInfo.LinkTarget is not null && ++vHops < 40)
            {
                var vTarget = Path.GetFullPath(Path.Combine(vCurrent, vInfo.LinkTarget));
                var vTargetRoot = Path.GetPathRoot(vTarget) ?? vRoot;
                vCurrent = vTargetRoot;
                foreach (var vPart in vTarget[vTargetRoot.Length..]
                    .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).Reverse())
                {
                    vPending.Push(vPart);
                }

                continue;
            }

            vCurrent = vNext;
        }

        return vCurrent;
    }
}
