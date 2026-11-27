using Chatur.Core.BuildRun;
using Xunit;

namespace Chatur.Tests.BuildRun;

/// <summary>
/// Output marks a running program's line by its own level, never because it came on standard error (REQ-UI-107,
/// BRD-235): the .NET console logger's <c>warn:</c> header and the indented message under it are a warning, a
/// <c>fail:</c> or <c>crit:</c> line and its message are errors, and a line that is not a logger line keeps the earlier rule.
/// </summary>
public sealed class ProgramLineLevelsTests
{
    /// <summary>
    /// When an ASP.NET app writes the https-redirection warning as a <c>warn:</c> header and the indented
    /// "Failed to determine the https port for redirect." line, then neither line is an error; both are warnings.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-107 the https port warning is a warning, not an error")]
    public void HttpsPortWarningIsNotError()
    {
        var vLevels = new ProgramLineLevels();

        var vHeader = vLevels.Read("warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]");
        var vMessage = vLevels.Read("      Failed to determine the https port for redirect.");

        Assert.Equal(ProgramLineLevel.Warning, vHeader);
        Assert.Equal(ProgramLineLevel.Warning, vMessage);
    }

    /// <summary>
    /// When a program logs at fail and then at crit, then each header and the indented lines under it are errors,
    /// and an info line after them is ordinary again.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-107 a fail or crit line and its message are errors")]
    public void FailLineIsError()
    {
        var vLevels = new ProgramLineLevels();

        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("fail: Microsoft.AspNetCore.Server.Kestrel[13]"));
        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("      Connection id \"0HN\": An unhandled exception was thrown by the application."));
        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("crit: Microsoft.Hosting.Lifetime[0]"));
        Assert.Equal(ProgramLineLevel.Ordinary, vLevels.Read("info: Microsoft.Hosting.Lifetime[14]"));
        Assert.Equal(ProgramLineLevel.Ordinary, vLevels.Read("      Now listening on: http://localhost:5000"));
    }

    /// <summary>
    /// When lines are not logger lines, then the earlier rule holds: "Failed …", an unhandled exception and a
    /// compiler-style error are errors, other text is ordinary, and a logger header's level ends at the first line
    /// that is not indented.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-107 a line that is not a logger line keeps the earlier rule")]
    public void PlainLinesKeepEarlierRule()
    {
        var vLevels = new ProgramLineLevels();

        Assert.Equal(ProgramLineLevel.Warning, vLevels.Read("warn: Some.Category[1]"));
        Assert.Equal(ProgramLineLevel.Ordinary, vLevels.Read("hello"));
        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("  Failed Lib.Tests.SumTest [12 ms]"));
        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("Unhandled exception. System.InvalidOperationException: boom"));
        Assert.Equal(ProgramLineLevel.Error, vLevels.Read("Program.cs(3,1): error CS1002: ; expected"));
        Assert.Equal(ProgramLineLevel.Ordinary, vLevels.Read("Done."));
    }
}
