using Chatur.Core.AgentWorkspace;
using Chatur.Core.Platform;
using Chatur.Tests.Support;
using Dapper;
using Xunit;

namespace Chatur.Tests.AgentWorkspace;

/// <summary>Tests for <see cref="AgentWorkspaceActions"/> against a real, migrated, temporary database.</summary>
public sealed class AgentWorkspaceActionsTests : MigratedDatabaseFixture
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc);
    }

    private AgentWorkspaceActions Create() => new(CreateFactory(), new FixedClock());

    /// <summary>
    /// REQ-FN-058: when an agent lacks a right, then the command needing it is not offered in its
    /// workspace and the right is listed once as withheld.
    /// </summary>
    [Fact]
    public async Task CommandNeedingAWithheldRightIsNotOffered()
    {
        var vProject = CreateProject("TfLens", "/tmp/tflens-p2h-a");

        var vVerifier = await Create().WorkspaceAsync("verifier", vProject);
        var vAnalyst = await Create().WorkspaceAsync("analyst", vProject);

        Assert.Contains(vVerifier.Commands, c => c.Command == "verify-phase");
        Assert.Contains(vVerifier.Denied, d => d.Action == "edit-file");
        Assert.DoesNotContain(vVerifier.Commands, c => c.Command == "build-phase");
        Assert.DoesNotContain(vAnalyst.Allowed, r => r.Action == "edit-file");
        Assert.Equal(1, vVerifier.Denied.Count(d => d.Action == "edit-file"));
    }

    /// <summary>
    /// REQ-FN-058: when a right is taken away in the database, then the command that needs it
    /// disappears on the next read.
    /// </summary>
    [Fact]
    public async Task WithdrawingARightRemovesItsCommand()
    {
        var vProject = CreateProject("TfLens", "/tmp/tflens-p2h-b");
        Assert.Contains((await Create().WorkspaceAsync("verifier", vProject)).Commands, c => c.Command == "verify-phase");

        using (var vConnection = CreateFactory().OpenConnection())
        {
            vConnection.Execute("UPDATE RoleRight SET Allowed = 0 WHERE Action = 'mark-verified' AND RoleId = @Id", new { Id = RoleIdFor("verifier") });
        }

        var vAfter = await Create().WorkspaceAsync("verifier", vProject);

        Assert.DoesNotContain(vAfter.Commands, c => c.Command == "verify-phase");
    }

    /// <summary>
    /// REQ-FN-059: when the owner starts a command from a workspace, then the session runs as that
    /// agent on the selected project; a command the agent is not offered is refused.
    /// </summary>
    [Fact]
    public async Task StartCommandOpensASessionAsThatAgentOnThatProject()
    {
        var vProject = CreateProject("TfLens", "/tmp/tflens-p2h-c");
        var vOther = CreateProject("Other", "/tmp/tflens-p2h-d");

        var vStart = await Create().StartCommandAsync("verifier", vProject, "verify-phase");

        Assert.Equal("*verify-phase", vStart.Message);
        Assert.Equal(RoleIdFor("verifier"), vStart.RoleId);
        using var vConnection = CreateFactory().OpenConnection();
        var vRow = vConnection.QuerySingle<(long RoleId, long ProjectId, string State)>("SELECT RoleId, ProjectId, State FROM Session WHERE SessionId = @Id", new { Id = vStart.SessionId });
        Assert.Equal(RoleIdFor("verifier"), (int)vRow.RoleId);
        Assert.Equal(vProject, (int)vRow.ProjectId);
        Assert.NotEqual(vOther, (int)vRow.ProjectId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create().StartCommandAsync("verifier", vProject, "build-phase"));
    }

    /// <summary>
    /// REQ-FN-060: when the owner moves to another agent's workspace, then the session he left is
    /// still open, with its messages, and is there when he comes back.
    /// </summary>
    [Fact]
    public async Task MovingToAnotherAgentLeavesTheSessionOpen()
    {
        var vProject = CreateProject("TfLens", "/tmp/tflens-p2h-e");
        var vStart = await Create().StartCommandAsync("verifier", vProject, "verify-phase");
        using (var vConnection = CreateFactory().OpenConnection())
        {
            vConnection.Execute(
                "INSERT INTO SessionEvent (SessionId, Seq, Kind, Payload, Model, Tokens, CreatedUtc) VALUES (@Id, 1, 'user', 'Check REQ-UI-019', NULL, NULL, '2026-10-06T09:00:00Z'), (@Id, 2, 'assistant', 'Reading it now', 'm', 120, '2026-10-06T09:00:01Z')",
                new { Id = vStart.SessionId });
        }

        var vAway = await Create().WorkspaceAsync("flow-master", vProject);
        var vBack = await Create().WorkspaceAsync("verifier", vProject);

        Assert.Empty(vAway.Sessions);
        Assert.Equal(1, vAway.Agents.Single(a => a.Code == "verifier").OpenSessions);
        var vSession = Assert.Single(vBack.Sessions);
        Assert.Equal(vStart.SessionId, vSession.SessionId);
        Assert.Equal("Check REQ-UI-019", vSession.Title);
        Assert.Equal(2, vSession.MessageCount);
        Assert.Equal(120, vSession.TokensUsed);
        Assert.NotEqual("Stopped", vSession.State);
    }

    /// <summary>An unknown agent in the route falls back to the first agent.</summary>
    [Fact]
    public async Task UnknownAgentFallsBackToTheFirst()
    {
        var vProject = CreateProject("TfLens", "/tmp/tflens-p2h-f");

        var vView = await Create().WorkspaceAsync("nobody", vProject);

        Assert.Equal("analyst", vView.Agent.Code);
    }
}
