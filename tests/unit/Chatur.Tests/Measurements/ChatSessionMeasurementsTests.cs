using Chatur.Core.Actions;
using Chatur.Core.Measurements;
using Xunit;

namespace Chatur.Tests.Measurements;

/// <summary>
/// The owner's test of 25 Nov 2026: tokens were used in a chat and Measurements showed nothing. A chat session's own
/// record, written per turn with what the session has used in all, now counts once as a line with its tokens.
/// </summary>
public sealed class ChatSessionMeasurementsTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    private const string Sessions =
        "{\"v\":1,\"ts\":\"2026-11-25T10:00:00Z\",\"kind\":\"session\",\"app\":\"BrainTest\",\"harness\":\"chatur\",\"session_id\":\"chatur-a-1\",\"model\":\"claude-opus-5-5\",\"duration_s\":30,\"input_tokens\":100,\"output_tokens\":40,\"cost_usd\":null}\n"
        + "{\"v\":1,\"ts\":\"2026-11-25T10:05:00Z\",\"kind\":\"session\",\"app\":\"BrainTest\",\"harness\":\"chatur\",\"session_id\":\"chatur-a-1\",\"model\":\"claude-opus-5-5\",\"duration_s\":300,\"input_tokens\":900,\"output_tokens\":400,\"cost_usd\":null}\n"
        + "{\"v\":1,\"ts\":\"2026-11-25T11:00:00Z\",\"kind\":\"session\",\"app\":\"BrainTest\",\"harness\":\"chatur\",\"session_id\":\"chatur-b-2\",\"model\":\"claude-sonnet-5-5\",\"duration_s\":60,\"input_tokens\":50,\"output_tokens\":20,\"cost_usd\":null}\n";

    /// <summary>When a session was written on every turn, then it counts once, by the record with the most output tokens, and its tokens reach the totals and the split by model (REQ-UI-115, REQ-UI-117, REQ-FN-040).</summary>
    [Fact(DisplayName = "REQ-UI-115 a chat session counts once, by its latest record, with its tokens")]
    public void ChatSessionCountsOnceWithItsTokens()
    {
        var vResult = MetricsCalculator.Compute(null, null, null, MetricsPeriod.AllHistory, NowUtc, TimeZoneInfo.Utc, Sessions);

        Assert.Equal(2, vResult.Totals.Runs);
        Assert.Equal(950, vResult.Totals.TokensIn.Value);
        Assert.Equal(420, vResult.Totals.TokensOut.Value);
        Assert.Equal(360, vResult.Totals.WallClockS.Value);
        Assert.Equal(["claude-opus-5-5", "claude-sonnet-5-5"], vResult.ByModel.Select(l => l.Key).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        Assert.All(vResult.Runs, r => Assert.Equal("chat", r.Command));
    }

    /// <summary>When there is no sessions file, then the figures are what the runs alone give (REQ-NFR-014).</summary>
    [Fact(DisplayName = "REQ-NFR-014 without a sessions file the figures are the runs alone")]
    public void NoSessionsFileChangesNothing()
    {
        var vResult = MetricsCalculator.Compute(null, null, null, MetricsPeriod.AllHistory, NowUtc, TimeZoneInfo.Utc, null);

        Assert.Equal(0, vResult.Totals.Runs);
        Assert.Null(vResult.Totals.TokensOut.Value);
    }
}
