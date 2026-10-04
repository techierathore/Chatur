using Chatur.Core.Guards;
using Xunit;

namespace Chatur.Tests.Guards;

/// <summary>
/// Tests for <see cref="SourceControlRefusalGuard"/> (REQ-UI-033, REQ-NFR-005: one test per guard
/// for what it allows and what it refuses). No database is needed — the rule is a pure name match.
/// </summary>
public sealed class SourceControlRefusalGuardTests
{
    /// <summary>
    /// When a model asks for the seeded "run-source-control" action, then the guard refuses it, with
    /// no exception (Coding Standards "A model never runs a source-control command").
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-005 source-control guard refuses a source-control action")]
    public async Task EvaluateRefusesTheSeededSourceControlAction()
    {
        var vGuard = new SourceControlRefusalGuard();

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(1, "run-source-control", "git commit -m done"));

        Assert.False(vResult.Allowed);
        Assert.NotNull(vResult.RefusalReason);
    }

    /// <summary>
    /// When a model asks for a git porcelain command by name, then the guard refuses it too, even
    /// though "run-source-control" is the seeded action name — the rule has no exception.
    /// </summary>
    [Theory]
    [InlineData("commit")]
    [InlineData("push")]
    [InlineData("git-reset")]
    [InlineData("source-control-checkout")]
    public async Task EvaluateRefusesEveryKnownSourceControlName(string aToolName)
    {
        var vGuard = new SourceControlRefusalGuard();

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(1, aToolName, "a source-control command"));

        Assert.False(vResult.Allowed);
    }

    /// <summary>
    /// When a model asks for an ordinary tool such as reading or editing a file, then the guard
    /// allows it — the refusal is specific to source control, not a blanket refusal.
    /// </summary>
    [Theory(DisplayName = "REQ-NFR-005 source-control guard allows an ordinary tool")]
    [InlineData("read-file")]
    [InlineData("edit-file")]
    [InlineData("run-build")]
    public async Task EvaluateAllowsAnOrdinaryTool(string aToolName)
    {
        var vGuard = new SourceControlRefusalGuard();

        var vResult = await vGuard.EvaluateAsync(new ToolRequest(1, aToolName, "an ordinary tool"));

        Assert.True(vResult.Allowed);
        Assert.Null(vResult.RefusalReason);
    }
}
