using Chatur.Core.Models;
using Xunit;

namespace Chatur.Tests.Models;

/// <summary>Tests for <see cref="OpenAICompatibleConnection"/> and the OpenAI-compatible route.</summary>
public sealed class OpenAICompatibleConnectionTests
{
    /// <summary>When a service lists its models, then their identifiers are read in order.</summary>
    [Fact]
    public void ParseModelIdsReadsTheOpenAIShape()
    {
        var vIds = OpenAICompatibleConnection.ParseModelIds("""{"object":"list","data":[{"id":"kimi-k2.7-code"},{"id":"glm-5.3"}]}""");

        Assert.Equal(new[] { "kimi-k2.7-code", "glm-5.3" }, vIds);
    }

    /// <summary>When a model list is not in the OpenAI shape, then no models are read.</summary>
    [Fact]
    public void ParseModelIdsReadsNothingFromAnotherShape()
    {
        Assert.Empty(OpenAICompatibleConnection.ParseModelIds("""{"models":["x"]}"""));
    }
}
