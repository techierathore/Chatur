using Chatur.Core.Roles;
using Chatur.Tests.Support;
using Xunit;

namespace Chatur.Tests.Roles;

/// <summary>
/// Tests for how an agent's persona file is read into its greeting (REQ-FN-308): the two shapes the framework uses,
/// and the greeting built for a project with and without the file.
/// </summary>
public sealed class AgentPersonaTests : MigratedDatabaseFixture
{
    private const string TablePersona = """
        # Verifier

        Vidur, the TechieFlow verifier (icon X): the one persona that proves whether each numbered checklist row is implemented. The flow-master builds and fixes; the verifier only grades.

        ## Commands

        | Command | What it does | Task file |
        |---|---|---|
        | `*verify {scope} {App} [--phase N\|all]` | boots the app, runs the checks. Then writes the verdicts | `verify-phase.md` |
        | `*report {scope} {App}` | prints the current Status and Remarks | |
        | `*help` | prints this table | |
        """;

    private const string YamlPersona = """
        # analyst

        ```yaml
        agent:
          name: Chanakya
          id: analyst
          title: Business Analyst
          whenToUse: Use for market research, brainstorming and project briefs
          customization: null
        persona:
          role: Analyst
        commands:
          - help: Show numbered list of the following commands
          - create-project-brief {AppName}: Write the project brief. Runs a task.
          - exit: Say goodbye
        dependencies:
          tasks:
            - create-doc.md
        ```
        """;

    private readonly List<string> objFolders = [];

    /// <summary>When a persona is a markdown table of <c>*command</c> rows, then each row is a command with its one-line description, a bar escaped inside a cell stays in the command, and the header row is not one.</summary>
    [Fact]
    public void TablePersonaGivesItsCommandsAndPurpose()
    {
        var vPersona = AgentPersonaParser.Parse(TablePersona, "Verifier");

        Assert.Equal(["*verify {scope} {App} [--phase N|all]", "*report {scope} {App}", "*help"], vPersona.Commands.Select(aCommand => aCommand.Command).ToArray());
        Assert.Equal("boots the app, runs the checks.", vPersona.Commands[0].Description);
        Assert.Equal("Vidur", vPersona.Name);
        Assert.Equal("Verifier", vPersona.Title);
        Assert.StartsWith("The one persona that proves", vPersona.Purpose);
    }

    /// <summary>When a persona is a YAML <c>commands:</c> list, then each item is a command with a leading star, and the name, title and what it is for come from the <c>agent:</c> block.</summary>
    [Fact]
    public void YamlPersonaGivesItsCommandsAndPurpose()
    {
        var vPersona = AgentPersonaParser.Parse(YamlPersona, "Analyst");

        Assert.Equal(["*help", "*create-project-brief {AppName}", "*exit"], vPersona.Commands.Select(aCommand => aCommand.Command).ToArray());
        Assert.Equal("Write the project brief.", vPersona.Commands[1].Description);
        Assert.Equal("Chanakya", vPersona.Name);
        Assert.Equal("Business Analyst", vPersona.Title);
        Assert.Equal("Use me for market research, brainstorming and project briefs", vPersona.Purpose);
    }

    /// <summary>When a file lists no commands and names no one, then nothing is invented: no commands, and the role's own name stands in.</summary>
    [Fact]
    public void AFileWithNothingToReadGivesNoCommands()
    {
        var vPersona = AgentPersonaParser.Parse("Just some notes.", "Analyst");

        Assert.Empty(vPersona.Commands);
        Assert.Equal("Analyst", vPersona.Name);
        Assert.Equal(string.Empty, vPersona.Purpose);
    }

    /// <summary>When the project has the persona file, then the greeting is built from it, and the system prompt carries the file's text and its commands.</summary>
    [Fact]
    public async Task GreetingAndPromptComeFromThePersonaFile()
    {
        var vFolder = NewProjectFolder();
        WriteAgent(vFolder, "verifier", TablePersona);
        var vProjectId = CreateProject("fixture", vFolder);
        var vSut = new AgentPersonaActions(CreateFactory());

        var vGreeting = await vSut.GreetingAsync(vProjectId, RoleIdFor("verifier"));
        var vPrompt = await vSut.SystemPromptAsync(vProjectId, RoleIdFor("verifier"));

        Assert.NotNull(vGreeting);
        Assert.Equal("Verifier", vGreeting.RoleName);
        Assert.Equal("Vidur", vGreeting.Name);
        // Changed 22 Nov 2026 (BRD-241, REQ-FN-308): the commands are Chatur's own list, never narrowed by the file.
        Assert.Equal(Chatur.Core.Agent.Commands.AgentCommandCatalogue.ForRole("verifier").Count, vGreeting.Commands.Count);
        Assert.Contains(vGreeting.Commands, aCommand => aCommand.Command == "*verify {scope} {App} [--phase N|all]");
        Assert.NotNull(vPrompt);
        Assert.Contains("*report {scope} {App}", vPrompt);
        Assert.Contains("Vidur, the TechieFlow verifier", vPrompt);
        Assert.Contains("only grades", vPrompt);
    }

    /// <summary>When the project has no persona file, then the greeting is the role's own wording and the commands stored for the role, so a role the owner added greets too.</summary>
    [Fact]
    public async Task WithoutAFileTheGreetingComesFromTheRole()
    {
        var vProjectId = CreateProject("plain", NewProjectFolder());
        var vSut = new AgentPersonaActions(CreateFactory());

        var vGreeting = await vSut.GreetingAsync(vProjectId, RoleIdFor("analyst"));
        var vPrompt = await vSut.SystemPromptAsync(vProjectId, RoleIdFor("analyst"));

        Assert.NotNull(vGreeting);
        Assert.Equal("Analyst", vGreeting.Name);
        Assert.Contains("never changes code", vGreeting.Purpose);
        Assert.Contains(vGreeting.Commands, aCommand => aCommand.Command == "*create-project-brief {App}");
        Assert.Contains(vGreeting.Commands, aCommand => aCommand.Command == "*brainstorm {topic}");
        Assert.Contains("never say you lack a command that is on it", vPrompt);
        Assert.NotNull(vPrompt);
        Assert.Contains("*split-brd", vPrompt);
    }

    /// <summary>When there is no such role, then there is no greeting and no prompt.</summary>
    [Fact]
    public async Task AnUnknownRoleHasNoGreeting()
    {
        var vSut = new AgentPersonaActions(CreateFactory());

        Assert.Null(await vSut.GreetingAsync(1, 9999));
        Assert.Null(await vSut.SystemPromptAsync(1, 9999));
    }

    private string NewProjectFolder()
    {
        var vFolder = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-persona-{Guid.NewGuid():N}");
        Directory.CreateDirectory(vFolder);
        objFolders.Add(vFolder);
        return vFolder;
    }

    private static void WriteAgent(string aFolder, string aCode, string aText)
    {
        var vDirectory = Path.Combine(aFolder, ".tfcore", "agents");
        Directory.CreateDirectory(vDirectory);
        File.WriteAllText(Path.Combine(vDirectory, aCode + ".md"), aText);
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var vFolder in objFolders.Where(Directory.Exists))
        {
            Directory.Delete(vFolder, true);
        }

        base.Dispose();
    }
}
