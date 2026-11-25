using Chatur.Core.Agent;
using Xunit;

namespace Chatur.Tests.Agent;

/// <summary>
/// Proves the stalled-turn check behind REQ-FN-318 (*devguide, owner's brief of 22 Nov 2026): in the verify of that
/// evening the agent answered *devguide with nothing, then "I'll list the project first." with no tool call, and the
/// reply ended there three times running, so the guide was never written. A turn like that is now told to go on.
/// </summary>
public sealed class AgentStallTests
{
    /// <summary>An empty turn and a turn that only announces its next step are stalls.</summary>
    [Theory(DisplayName = "REQ-FN-318 a turn that says nothing or only announces its next step is a stall")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("I'll start by listing the project to find the routed pages.")]
    [InlineData("I'll list the project first, so I can find the routed pages.")]
    [InlineData("Let me read the pages:")]
    [InlineData("First, I will look at Program.cs.")]
    [InlineData("Okay, I’m going to read the csproj now.")]
    public void AnnouncementIsAStall(string aText) => Assert.True(AgentActions.IsStall(aText));

    /// <summary>A turn that reports what was done, or asks the person something, is not a stall.</summary>
    [Theory(DisplayName = "REQ-FN-318 a turn that reports work done or asks a question is not a stall")]
    [InlineData("The guide is written to docs/App-DevGuide.md, with 4 screens mapped.")]
    [InlineData("Which screens should the guide cover first?")]
    [InlineData("Done. I wrote the file.")]
    public void ReportIsNotAStall(string aText) => Assert.False(AgentActions.IsStall(aText));

    /// <summary>A turn that ended at the output limit is recognised under both providers' words; a normal end is not.</summary>
    [Theory(DisplayName = "REQ-FN-318 a turn cut off at the output limit is recognised")]
    [InlineData("max_tokens", true)]
    [InlineData("length", true)]
    [InlineData("end_turn", false)]
    [InlineData("tool_use", false)]
    [InlineData(null, false)]
    public void CutOffIsRecognised(string? aReason, bool aExpected) => Assert.Equal(aExpected, AgentActions.IsCutOff(aReason));
}
