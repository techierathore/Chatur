using System.Data;
using Chatur.Core.Actions;
using Chatur.Core.Agent.Commands;
using Chatur.Core.Data;
using Chatur.Core.Documents;
using Chatur.Core.Platform;
using Chatur.Core.ProcessRuns;
using ChaturDb;
using Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Chatur.Tests.Agent;

/// <summary>
/// Tests for the agents' commands (BRD-249, BRD-241, REQ-FN-311, REQ-FN-308): every command each TechieFlow agent has is
/// in Chatur's list for the same agent, and the runner carries each kind out in Chatur — answered with no model, started
/// as a process run, or guided until its file is on disk. A question about the commands is answered from the list.
/// The database and the project folder are real and temporary, under the test project's own bin folder.
/// </summary>
public sealed class AgentCommandTests : IDisposable
{
    /// <summary>The commands each TechieFlow agent file lists (TechieFlow .tfcore/agents/*.md, read 22 Nov 2026).</summary>
    private static readonly Dictionary<string, string[]> objTechieFlow = new()
    {
        ["analyst"] = ["help", "day1-brownfield", "day1-greenfield", "mockups", "split-brd", "brainstorm", "amend-docs", "create-project-brief", "doc-out", "research-prompt", "yolo", "exit"],
        ["architect"] = ["help", "doc-out", "research", "yolo", "exit"],
        ["flow-master"] = ["develop-end-to-end", "develop-status", "estimate-phase", "build-phase", "fix-issues", "triage-issues", "log-miss", "triage-and-fix", "amend-docs", "devguide", "productguide", "handoff-phase", "deploy-checklist", "refresh-status", "metrics", "generate-html", "render-workflow-docs", "yolo", "help", "exit"],
        ["verifier"] = ["verify", "report", "help", "exit"]
    };

    private readonly string objDatabasePath = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-commands-{Guid.NewGuid():N}.db");
    private readonly string objRoot = Path.Combine(Chatur.Tests.Support.TestScratch.Root, $"chatur-commands-root-{Guid.NewGuid():N}");
    private readonly FixedPathConnectionFactory objConnections;
    private readonly FakeProcesses objProcesses = new();
    private readonly AgentCommandRunner objSut;
    private readonly int objProjectId;

    /// <summary>Makes a migrated database, an empty project folder named BrainTest, and its project row.</summary>
    public AgentCommandTests()
    {
        var vResult = ChaturDbMigrator.Migrate($"Data Source={objDatabasePath}");
        Assert.True(vResult.Successful, vResult.Error?.Message);
        Directory.CreateDirectory(objRoot);
        objConnections = new FixedPathConnectionFactory(objDatabasePath);
        objSut = new AgentCommandRunner(objConnections, new FixedClock(), new Services(objProcesses, System.Reflection.DispatchProxy.Create<IAutoRunActions, RefusingRuns>()));
        using var vConnection = objConnections.OpenConnection();
        vConnection.Execute("INSERT INTO Project (ProjectFolderId, Name, Path, Kind, LastOpenedUtc) VALUES (NULL, 'Brain Test', @Path, 'Folder', NULL);", new { Path = objRoot });
        objProjectId = vConnection.ExecuteScalar<int>("SELECT ProjectId FROM Project WHERE Path = @Path;", new { Path = objRoot });
    }

    /// <summary>Every command of every TechieFlow agent is in Chatur's list for the same agent.</summary>
    [Fact(DisplayName = "REQ-FN-311 every command of the four TechieFlow agents is in Chatur's list for the same agent")]
    public void EveryTechieFlowCommandIsListed()
    {
        foreach (var (vRole, vCodes) in objTechieFlow)
        {
            var vMissing = vCodes.Where(c => AgentCommandCatalogue.Find(vRole, c) is null).ToList();
            Assert.True(vMissing.Count == 0, $"{vRole} lacks: {string.Join(", ", vMissing)}");
        }

        Assert.All(AgentCommandCatalogue.All.Where(c => c.Kind == AgentCommandKind.Process), c => Assert.NotNull(Chatur.Core.Processes.ProcessCatalogue.Find(c.Target!)));
    }

    /// <summary>A name typed with a hyphen, without it, or with Chatur's older name finds the command.</summary>
    [Fact(DisplayName = "REQ-FN-311 brain-storm, brainstorm and verify-phase find their commands")]
    public void SpellingsFindTheCommand()
    {
        Assert.Equal("brainstorm", AgentCommandCatalogue.Find("analyst", "brain-storm")?.Code);
        Assert.Equal("brainstorm", AgentCommandCatalogue.Find("analyst", "*brainstorm")?.Code);
        Assert.Equal("verify", AgentCommandCatalogue.Find("verifier", "verify-phase")?.Code);
        Assert.Null(AgentCommandCatalogue.Find("analyst", "build-phase"));
    }

    /// <summary>*help lists every command of the agent, numbered, with no model.</summary>
    [Fact(DisplayName = "REQ-FN-311 *help lists every command of the analyst, numbered, with no model")]
    public async Task HelpListsEveryCommand()
    {
        var vOutcome = await RunAsync("analyst", "*help");

        Assert.NotNull(vOutcome?.Reply);
        foreach (var vCode in objTechieFlow["analyst"])
        {
            Assert.Contains($"`*{vCode}", vOutcome!.Reply!, StringComparison.Ordinal);
        }

        Assert.Contains("12. ", vOutcome!.Reply!, StringComparison.Ordinal);
    }

    /// <summary>The owner's question of 22 Nov is answered yes from the list, never by a model's guess.</summary>
    [Fact(DisplayName = "REQ-FN-308 asked whether it has a brainstorm command, the analyst answers yes from its list")]
    public async Task BrainstormQuestionIsAnsweredYes()
    {
        var vOutcome = await RunAsync("analyst", "Don't you have brain-storm command ?");

        Assert.NotNull(vOutcome?.Reply);
        Assert.StartsWith("Yes.", vOutcome!.Reply!, StringComparison.Ordinal);
        Assert.Contains("*brainstorm {topic}", vOutcome.Reply!, StringComparison.Ordinal);
        Assert.Null(await RunAsync("analyst", "Write a short note about the login screen."));
    }

    /// <summary>"What commands do you have?" gets the whole list.</summary>
    [Fact(DisplayName = "REQ-FN-308 what commands do you have is answered with the agent's whole list")]
    public async Task CommandsQuestionGetsTheList()
    {
        var vOutcome = await RunAsync("flow-master", "What commands do you have?");

        Assert.Contains("has 20 commands", vOutcome!.Reply!, StringComparison.Ordinal);
        Assert.Contains("`*render-workflow-docs {App}`", vOutcome.Reply!, StringComparison.Ordinal);
    }

    /// <summary>An unknown command is answered from the list, with no model.</summary>
    [Fact(DisplayName = "REQ-FN-311 an unknown command is answered from the list")]
    public async Task UnknownCommandIsAnswered()
    {
        var vOutcome = await RunAsync("verifier", "*brainstorm");

        Assert.StartsWith("The Verifier has no *brainstorm command.", vOutcome!.Reply!, StringComparison.Ordinal);
        Assert.Contains("`*report {scope} {App}`", vOutcome.Reply!, StringComparison.Ordinal);
    }

    /// <summary>*yolo turns Go ahead on, and off again; *exit closes the conversation.</summary>
    [Fact(DisplayName = "REQ-FN-311 *yolo switches Go ahead and *exit closes the conversation")]
    public async Task YoloAndExit()
    {
        Assert.Equal(SessionMode.GoAhead, (await RunAsync("architect", "*yolo"))!.Mode);
        Assert.Equal(SessionMode.AskFirst, (await RunAsync("architect", "*yolo", SessionMode.GoAhead))!.Mode);
        Assert.True((await RunAsync("verifier", "*exit"))!.EndsConversation);
    }

    /// <summary>A process command starts its process run and keeps what was typed with it where the run's steps read it.</summary>
    [Fact(DisplayName = "REQ-FN-311 *amend-docs starts its process run and keeps the change for its steps")]
    public async Task ProcessCommandStartsItsRun()
    {
        var vOutcome = await RunAsync("analyst", "*amend-docs BrainTest add a dark mode");

        Assert.Equal("amend-docs", objProcesses.Started);
        Assert.Contains("process run 7", vOutcome!.Reply!, StringComparison.Ordinal);
        Assert.Contains("/processes/7/run", vOutcome.Reply!, StringComparison.Ordinal);
        var vKept = Directory.GetFiles(Path.Combine(objRoot, "docs", "requests"));
        Assert.Single(vKept);
        Assert.Contains("add a dark mode", File.ReadAllText(vKept[0]), StringComparison.Ordinal);
    }

    /// <summary>*triage-and-fix starts the triage run and names the fix run that follows it.</summary>
    [Fact(DisplayName = "REQ-FN-311 *triage-and-fix starts triage and queues the fix run after it")]
    public async Task TriageAndFixQueuesTheFix()
    {
        await RunAsync("flow-master", "*triage-and-fix the Run button stays grey");

        Assert.Equal("triage-issues", objProcesses.Started);
        Assert.Single(Directory.GetFiles(Path.Combine(objRoot, "docs", "issues")));
        using var vConnection = objConnections.OpenConnection();
        Assert.Equal("fix-issues", vConnection.ExecuteScalar<string>("SELECT Value FROM Setting WHERE Key = @Key;", new { Key = ProcessRunActions.FollowUpSettingKey(7) }));
    }

    /// <summary>A guided command gives the agent its steps until its file is on disk, then says the file is written.</summary>
    [Fact(DisplayName = "REQ-FN-312 *create-project-brief guides the agent until docs/BrainTest-Brief.md is written")]
    public async Task GuidedCommandEndsWhenItsFileIsWritten()
    {
        var vSession = NewSession("analyst");

        var vOutcome = await objSut.TryRunAsync(new AgentCommandContext(vSession, objProjectId, RoleId("analyst"), SessionMode.GoAhead), "*create-project-brief BrainTest");

        Assert.Null(vOutcome!.Reply);
        var vGuide = await objSut.ActiveGuideAsync(vSession);
        Assert.Contains("docs/BrainTest-Brief.md", vGuide, StringComparison.Ordinal);
        Assert.Contains("## Must do", vGuide, StringComparison.Ordinal);
        Assert.Null(await objSut.AfterReplyAsync(vSession));

        Directory.CreateDirectory(Path.Combine(objRoot, "docs"));
        File.WriteAllText(Path.Combine(objRoot, "docs", "BrainTest-Brief.md"), "# BrainTest — Brief\n");

        Assert.Contains("docs/BrainTest-Brief.md is written", await objSut.AfterReplyAsync(vSession), StringComparison.Ordinal);
        Assert.Null(await objSut.ActiveGuideAsync(vSession));
    }

    /// <summary>The brainstorm's steps are TechieFlow's facilitation, saved to the brainstorm results file.</summary>
    [Fact(DisplayName = "REQ-FN-312 *brainstorm guides a facilitated session saved to docs/brainstorming-session-results.md")]
    public async Task BrainstormIsGuided()
    {
        var vSession = NewSession("analyst");

        await objSut.TryRunAsync(new AgentCommandContext(vSession, objProjectId, RoleId("analyst"), SessionMode.AskFirst), "*brainstorm a small to-do app");

        var vGuide = await objSut.ActiveGuideAsync(vSession);
        Assert.Contains("You are the facilitator", vGuide, StringComparison.Ordinal);
        Assert.Contains("docs/brainstorming-session-results.md", vGuide, StringComparison.Ordinal);
        Assert.Contains("a small to-do app", vGuide, StringComparison.Ordinal);
    }

    /// <summary>*log-miss writes one miss record and one readable line, in the person's words.</summary>
    [Fact(DisplayName = "REQ-FN-311 *log-miss writes the miss record and the readable line")]
    public async Task LogMissWritesTheRecord()
    {
        var vOutcome = await RunAsync("flow-master", "*log-miss BrainTest \"Run stayed grey after New project\" REQ-FN-309");

        Assert.Contains("MISS-BrainTest-20261122-01", vOutcome!.Reply!, StringComparison.Ordinal);
        var vStream = File.ReadAllText(Path.Combine(objRoot, "docs", "metrics", "misses.jsonl"));
        Assert.Contains("\"what\":\"Run stayed grey after New project\"", vStream, StringComparison.Ordinal);
        Assert.Contains("\"req_id\":\"REQ-FN-309\"", vStream, StringComparison.Ordinal);
        Assert.Contains("| MISS-BrainTest-20261122-01 |", File.ReadAllText(Path.Combine(objRoot, "docs", "BrainTest-Misses.md")), StringComparison.Ordinal);
    }

    /// <summary>*generate-html and *render-workflow-docs write HTML pages beside the markdown, in Chatur.</summary>
    [Fact(DisplayName = "REQ-FN-311 *generate-html and *render-workflow-docs write the HTML pages")]
    public async Task HtmlIsWritten()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, "docs"));
        File.WriteAllText(Path.Combine(objRoot, "docs", "BrainTest-BRD.md"), "# BrainTest — BRD\n\n**BRD-1** — one thing.\n");
        File.WriteAllText(Path.Combine(objRoot, "PROJECT-STATUS.md"), "# Status\n");

        await RunAsync("flow-master", "*generate-html docs/BrainTest-BRD.md");
        var vRendered = await RunAsync("flow-master", "*render-workflow-docs");

        Assert.Contains("<strong>BRD-1</strong>", File.ReadAllText(Path.Combine(objRoot, "docs", "BrainTest-BRD.html")), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(objRoot, "PROJECT-STATUS.html")));
        Assert.Contains("PROJECT-STATUS.html", vRendered!.Reply!, StringComparison.Ordinal);
    }

    /// <summary>*report shows the rows in scope from the checklist, running nothing.</summary>
    [Fact(DisplayName = "REQ-FN-311 *report shows the status and remarks of the rows in scope")]
    public async Task ReportShowsTheRows()
    {
        Directory.CreateDirectory(Path.Combine(objRoot, "docs"));
        File.WriteAllText(Path.Combine(objRoot, "docs", "BrainTest-Checklist.md"),
            "| ID | Requirement | Status | % | Remarks | Details |\n|---|---|---|---|---|---|\n| REQ-UI-001 | A list | Verified | 100% | passed | x |\n| REQ-FN-001 | A save | Not Started | 0% | — | x |\n");

        var vOutcome = await RunAsync("verifier", "*report ui BrainTest");

        Assert.Contains("| REQ-UI-001 | Verified | passed |", vOutcome!.Reply!, StringComparison.Ordinal);
        Assert.DoesNotContain("REQ-FN-001", vOutcome.Reply!, StringComparison.Ordinal);
    }

    /// <summary>REQ-UI-114: a reply's bold, list, code and table are drawn as HTML, raw HTML is shown as text, and a script link goes nowhere.</summary>
    [Fact(DisplayName = "REQ-UI-114 a reply's markdown is drawn as formatted text and never runs HTML")]
    public void ReplyMarkdownIsFormatted()
    {
        var vHtml = MarkdownHtml.Reply("- **\\*create-project-brief**: writes the brief\n\n`code`\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n<script>x()</script> [x](javascript:alert(1))");

        Assert.Contains("<strong>*create-project-brief</strong>", vHtml, StringComparison.Ordinal);
        Assert.Contains("<li>", vHtml, StringComparison.Ordinal);
        Assert.Contains("<code>code</code>", vHtml, StringComparison.Ordinal);
        Assert.Contains("<table>", vHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("**", vHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", vHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("javascript:", vHtml, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(objDatabasePath))
        {
            File.Delete(objDatabasePath);
        }

        if (Directory.Exists(objRoot))
        {
            Directory.Delete(objRoot, true);
        }
    }

    /// <summary>
    /// REQ-UI-122: <c>*develop-end-to-end</c> with a brief the run engine refuses (under 40 characters) is answered in the
    /// chat with the reason and the way to type it, and nothing is thrown at the conversation.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-122 a brief the run engine refuses is answered in the reply, not thrown")]
    public async Task DevelopWithShortBriefAnswersInReply()
    {
        var vOutcome = await RunAsync("flow-master", "*develop-end-to-end BrainTest a small app");
        Assert.NotNull(vOutcome?.Reply);
        Assert.Contains("did not start", vOutcome!.Reply);
        Assert.Contains("at least 40 characters", vOutcome.Reply);
        Assert.DoesNotContain("(Parameter", vOutcome.Reply);
    }

    private Task<AgentCommandOutcome?> RunAsync(string aRole, string aMessage, SessionMode aMode = SessionMode.AskFirst) =>
        objSut.TryRunAsync(new AgentCommandContext(NewSession(aRole), objProjectId, RoleId(aRole), aMode), aMessage);

    private int RoleId(string aCode)
    {
        using var vConnection = objConnections.OpenConnection();
        return vConnection.ExecuteScalar<int>("SELECT RoleId FROM Role WHERE Code = @aCode;", new { aCode });
    }

    private int NewSession(string aRole)
    {
        using var vConnection = objConnections.OpenConnection();
        return vConnection.ExecuteScalar<int>(
            "INSERT INTO Session (ProjectId, RoleId, Mode, State, StartedUtc) VALUES (@objProjectId, @RoleId, 'AskFirst', 'Open', '2026-11-22T10:00:00Z'); SELECT last_insert_rowid();",
            new { objProjectId, RoleId = RoleId(aRole) });
    }

    private sealed class FakeProcesses : IProcessActions
    {
        public string? Started { get; private set; }

        public Task<ProcessRunStarted> StartAsync(int aProjectId, string aProcessCode, CancellationToken aCt = default)
        {
            Started = aProcessCode;
            return Task.FromResult(new ProcessRunStarted(7, aProcessCode, aProjectId, "running", 1, "Read the late change", DateTime.UtcNow, false));
        }

        public Task<IReadOnlyList<ProcessSummary>> ListAsync(int? aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProcessStepInfo>> StepsAsync(string aProcessCode, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task SaveStepWordingAsync(string aProcessCode, string aStepCode, string aText, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task<ProcessDocuments?> DocumentsAsync(int aProjectId, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task WriteDocumentAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default) => throw new NotSupportedException();
    }

    private sealed class Services(IProcessActions aProcesses, IAutoRunActions aRuns) : IServiceProvider
    {
        public object? GetService(Type aServiceType) =>
            aServiceType == typeof(IProcessActions) ? aProcesses : aServiceType == typeof(IAutoRunActions) ? aRuns : null;
    }

    /// <summary>End-to-end runs with none going, which refuse a brief shorter than 40 characters as the real engine does.</summary>
    public class RefusingRuns : System.Reflection.DispatchProxy
    {
        /// <inheritdoc />
        protected override object? Invoke(System.Reflection.MethodInfo? aMethod, object?[]? aArgs)
        {
            if (aMethod!.Name == "StartAsync")
            {
                throw new ArgumentException("Say what should be true when the run finishes: a brief of at least 40 characters.", "aBrief");
            }

            var vResult = aMethod.ReturnType.GetGenericArguments()[0];
            var vValue = vResult.IsGenericType && vResult.GetGenericTypeDefinition() == typeof(IReadOnlyList<>)
                ? Array.CreateInstance(vResult.GetGenericArguments()[0], 0)
                : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(vResult).Invoke(null, [vValue]);
        }
    }

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FixedPathConnectionFactory(string aDatabasePath) : IDbConnectionFactory
    {
        public IDbConnection OpenConnection()
        {
            var vConnection = new SqliteConnection($"Data Source={aDatabasePath}");
            vConnection.Open();
            return vConnection;
        }
    }
}
