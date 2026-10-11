namespace Chatur.Core.ProcessRuns;

/// <summary>How a test command reports its results.</summary>
public enum TestReportKind
{
    /// <summary>A Visual Studio results file, <c>.trx</c>.</summary>
    Trx,

    /// <summary>Playwright's JSON report.</summary>
    PlaywrightJson
}

/// <summary>One test command to run, and the file its results come back in.</summary>
/// <param name="Command">What to run.</param>
/// <param name="ResultFile">The file the command writes its results to.</param>
/// <param name="Kind">The file's format.</param>
public sealed record TestPlan(ProjectCommand Command, string ResultFile, TestReportKind Kind);
