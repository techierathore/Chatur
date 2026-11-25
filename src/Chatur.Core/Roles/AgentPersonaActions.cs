using System.Text;
using System.Text.RegularExpressions;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Dapper;

namespace Chatur.Core.Roles;

/// <summary>
/// <see cref="IAgentPersonaActions"/> over the <c>Role</c> and <c>RoleCommand</c> tables and the persona file
/// <c>.tfcore/agents/&lt;role code&gt;.md</c> in the project's own folder (REQ-FN-308). A project without that file
/// (not a TechieFlow project, or a role the owner added) gets its agent from the role's stored wording and commands.
/// </summary>
public sealed partial class AgentPersonaActions : IAgentPersonaActions
{
    private readonly IDbConnectionFactory objConnections;

    /// <summary>Creates the actions.</summary>
    /// <param name="aConnections">Opens connections to Chatur's database.</param>
    public AgentPersonaActions(IDbConnectionFactory aConnections)
    {
        objConnections = aConnections;
    }

    /// <inheritdoc />
    public async Task<AgentGreeting?> GreetingAsync(int aProjectId, int aRoleId, CancellationToken aCt = default)
    {
        var vLoaded = await LoadAsync(aProjectId, aRoleId, aCt).ConfigureAwait(false);
        return vLoaded?.Greeting;
    }

    /// <inheritdoc />
    public async Task<string?> SystemPromptAsync(int aProjectId, int aRoleId, CancellationToken aCt = default)
    {
        var vLoaded = await LoadAsync(aProjectId, aRoleId, aCt).ConfigureAwait(false);
        if (vLoaded is null)
        {
            return null;
        }

        var vPrompt = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(vLoaded.Wording))
        {
            vPrompt.AppendLine(vLoaded.Wording.Trim()).AppendLine();
        }

        if (vLoaded.Greeting.Commands.Count > 0 || vLoaded.FileText is not null)
        {
            vPrompt.Append("You are acting as \"").Append(vLoaded.Greeting.RoleName).AppendLine("\" in Chatur. Stay in this agent's character and follow its definition.");
            vPrompt.AppendLine("When the person asks who you are, what you are for or what commands you have, answer from this definition and list your real commands below. Never answer with a generic description.");
            vPrompt.AppendLine("The list below is complete and every command on it works in Chatur: never say you lack a command that is on it. When the person asks for one in words, tell them to type it with its star (for example *brainstorm a to-do app), which starts it.");
            vPrompt.AppendLine();
        }

        if (vLoaded.Greeting.Commands.Count > 0)
        {
            vPrompt.AppendLine("Your commands:");
            foreach (var vCommand in vLoaded.Greeting.Commands)
            {
                vPrompt.Append("- ").Append(vCommand.Command);
                if (vCommand.Description.Length > 0)
                {
                    vPrompt.Append(": ").Append(vCommand.Description);
                }

                vPrompt.AppendLine();
            }

            vPrompt.AppendLine();
        }

        if (vLoaded.FileText is not null)
        {
            vPrompt.Append("Your agent definition (").Append(vLoaded.FileName).AppendLine("):").AppendLine().Append(vLoaded.FileText.Trim());
        }

        var vText = vPrompt.ToString().Trim();
        return vText.Length == 0 ? null : vText;
    }

    private async Task<LoadedAgent?> LoadAsync(int aProjectId, int aRoleId, CancellationToken aCt)
    {
        string? vProjectPath;
        RoleRow? vRole;
        List<PersonaCommand> vStored;
        using (var vConnection = objConnections.OpenConnection())
        {
            vRole = await vConnection.QuerySingleOrDefaultAsync<RoleRow>(new CommandDefinition(
                "SELECT Code, Name, Wording FROM Role WHERE RoleId = @aRoleId;",
                new { aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false);
            if (vRole is null)
            {
                return null;
            }

            vProjectPath = await vConnection.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(
                "SELECT Path FROM Project WHERE ProjectId = @aProjectId;",
                new { aProjectId },
                cancellationToken: aCt)).ConfigureAwait(false);
            vStored = (await vConnection.QueryAsync<StoredCommandRow>(new CommandDefinition(
                "SELECT Command, Description FROM RoleCommand WHERE RoleId = @aRoleId ORDER BY RoleCommandId;",
                new { aRoleId },
                cancellationToken: aCt)).ConfigureAwait(false))
                .Select(aRow => new PersonaCommand(aRow.Command, aRow.Description ?? string.Empty))
                .ToList();
        }

        // BRD-241, BRD-249 (22 Nov 2026): the four agents' commands are Chatur's own list, the same as the TechieFlow
        // agent's and the one *help, the runner and the agent's instructions use; a persona file's list never narrows it.
        var vOwnList = Agent.Commands.AgentCommandCatalogue.Covers(vRole.Code);
        if (vOwnList)
        {
            vStored = Agent.Commands.AgentCommandCatalogue.ForRole(vRole.Code).Select(c => new PersonaCommand(c.Usage, c.Description)).ToList();
        }

        var vFileName = $"{vRole.Code}.md";
        var vFileText = await ReadPersonaAsync(vProjectPath, vRole.Code, aCt).ConfigureAwait(false);
        if (vFileText is null)
        {
            var vOwn = new AgentGreeting(vRole.Name, vRole.Name, vRole.Name, vRole.Wording, vStored);
            return new LoadedAgent(vOwn, vRole.Wording, null, vFileName);
        }

        var vParsed = AgentPersonaParser.Parse(vFileText, vRole.Name);
        var vPurpose = vParsed.Purpose.Length > 0 ? vParsed.Purpose : vRole.Wording;
        var vCommands = vOwnList || vParsed.Commands.Count == 0 ? vStored : vParsed.Commands;
        return new LoadedAgent(new AgentGreeting(vRole.Name, vParsed.Name, vParsed.Title, vPurpose, vCommands), vRole.Wording, vFileText, vFileName);
    }

    /// <summary>The text of the project's persona file for a role code, or <see langword="null"/> when the project has none.</summary>
    private static async Task<string?> ReadPersonaAsync(string? aProjectPath, string aRoleCode, CancellationToken aCt)
    {
        if (string.IsNullOrWhiteSpace(aProjectPath) || !SafeCode().IsMatch(aRoleCode))
        {
            return null;
        }

        var vRoot = File.Exists(aProjectPath) ? Path.GetDirectoryName(aProjectPath) ?? aProjectPath : aProjectPath;
        var vFile = Path.Combine(vRoot, ".tfcore", "agents", aRoleCode + ".md");
        try
        {
            return File.Exists(vFile) ? await File.ReadAllTextAsync(vFile, aCt).ConfigureAwait(false) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9-]*$")]
    private static partial Regex SafeCode();

    private sealed record LoadedAgent(AgentGreeting Greeting, string Wording, string? FileText, string FileName);

    private sealed class RoleRow
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Wording { get; set; } = string.Empty;
    }

    private sealed class StoredCommandRow
    {
        public string Command { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
