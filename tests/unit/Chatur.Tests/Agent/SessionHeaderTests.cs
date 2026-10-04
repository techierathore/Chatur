using Chatur.Core.Agent;
using Xunit;

namespace Chatur.Tests.Agent;

/// <summary>Tests for how Chatur picks a service's per-conversation session header (TechieRag 1.0.9).</summary>
public sealed class SessionHeaderTests
{
    /// <summary>
    /// When a provider's web address is OpenCode Go's, then the conversation's id is sent in
    /// <c>x-opencode-session</c>, which OpenCode Go refuses to answer without (REQ-FN-025, TR-RAG-003).
    /// </summary>
    [Theory(DisplayName = "REQ-FN-025 OpenCode Go gets its session header")]
    [InlineData("https://opencode.ai/zen/go/v1")]
    [InlineData("https://opencode.ai/zen/go/v1/")]
    public void OpenCodeGoGetsItsSessionHeader(string aBaseUrl)
    {
        Assert.Equal("x-opencode-session", TechieRagLlmProviderFactory.SessionHeaderFor(aBaseUrl));
    }

    /// <summary>When a service reads no session header, then none is sent.</summary>
    [Fact]
    public void OtherServicesGetNoSessionHeader()
    {
        Assert.Null(TechieRagLlmProviderFactory.SessionHeaderFor("https://api.openai.com/v1"));
    }
}
