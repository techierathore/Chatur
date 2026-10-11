using Chatur.Core.Actions;
using Chatur.Core.ProcessRuns;
using Xunit;

namespace Chatur.Tests.ProcessRuns;

/// <summary>Tests for the readers of test results and the scanner for requirement ids in test names (REQ-UI-048).</summary>
public sealed class ResultParserTests
{
    private const string Trx =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <TestRun id="r1" xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <Results>
            <UnitTestResult executionId="e1" testId="t1" testName="REQ-FN-001 first passes" duration="00:00:00.0123456" outcome="Passed" />
            <UnitTestResult executionId="e2" testId="t2" testName="REQ-FN-002 second fails" duration="00:00:00.0100000" outcome="Failed">
              <Output><ErrorInfo><Message>Assert.Fail failed.
        deliberate</Message><StackTrace>at X</StackTrace></ErrorInfo></Output>
            </UnitTestResult>
            <UnitTestResult executionId="e3" testId="t3" testName="PlainName" duration="00:00:00.0000000" outcome="NotExecuted" />
          </Results>
          <TestDefinitions>
            <UnitTest name="PlainName" id="t3"><TestMethod className="Fixture.Tests" name="PlainName" /></UnitTest>
          </TestDefinitions>
        </TestRun>
        """;

    private const string PlaywrightJson =
        """{"suites":[{"title":"sample.spec.ts","file":"sample.spec.ts","specs":[{"title":"REQ-FN-009 top level passes","tests":[{"projectName":"chromium","results":[{"status":"passed","duration":29}],"status":"expected"}]}],"suites":[{"title":"REQ-UI-001 group","file":"sample.spec.ts","specs":[{"title":"passes here","tests":[{"projectName":"chromium","results":[{"status":"passed","duration":38}],"status":"expected"}]},{"title":"REQ-UI-002/003 fails here","tests":[{"projectName":"chromium","results":[{"status":"failed","duration":12,"error":{"message":"Error: \u001b[2mexpect\u001b[22m toBe\n\nExpected: 2\nReceived: 1"}}],"status":"unexpected"}]},{"title":"skipped one","tests":[{"projectName":"chromium","results":[{"status":"skipped","duration":0}],"status":"skipped"}]}]}]}],"errors":[]}""";

    /// <summary>When a results file is read, then each test has its outcome, its failure message on one line, and the ids its name carries.</summary>
    [Fact(DisplayName = "REQ-UI-048 a trx file yields each test with its outcome and ids")]
    public void TrxYieldsOutcomesAndIds()
    {
        var vTests = TrxResultParser.Parse(Trx);

        Assert.Equal(["REQ-FN-001 first passes", "REQ-FN-002 second fails", "PlainName"], vTests.Select(aTest => aTest.Name));
        Assert.Equal([ProcessTestOutcome.Passed, ProcessTestOutcome.Failed, ProcessTestOutcome.Skipped], vTests.Select(aTest => aTest.Outcome));
        Assert.Equal("Assert.Fail failed. deliberate", vTests[1].Message);
        Assert.Equal(["REQ-FN-002"], vTests[1].Requirements);
        Assert.Empty(vTests[2].Requirements);
        Assert.Equal(12, vTests[0].DurationMs);
    }

    /// <summary>When Playwright's report is read, then group titles count into each name, a failed test has its message, and skipped is not passed.</summary>
    [Fact(DisplayName = "REQ-UI-048 a playwright report yields each test with its outcome and ids")]
    public void PlaywrightYieldsOutcomesAndIds()
    {
        var vTests = PlaywrightResultParser.Parse(PlaywrightJson);

        Assert.Equal(4, vTests.Count);
        Assert.Equal(["REQ-FN-009"], vTests[0].Requirements);
        Assert.Equal("REQ-UI-001 group › passes here [chromium]", vTests[1].Name);
        Assert.Equal(["REQ-UI-001"], vTests[1].Requirements);
        Assert.Equal(["REQ-UI-001", "REQ-UI-002", "REQ-UI-003"], vTests[2].Requirements);
        Assert.Equal(ProcessTestOutcome.Failed, vTests[2].Outcome);
        Assert.Equal("Error: expect toBe Expected: 2 Received: 1", vTests[2].Message);
        Assert.Equal(ProcessTestOutcome.Skipped, vTests[3].Outcome);
        Assert.Null(PlaywrightResultParser.FirstError(PlaywrightJson));
    }

    /// <summary>When a name carries ids, then each distinct id is found, the slash shorthand expands, and a longer number is never read as a shorter one.</summary>
    [Theory(DisplayName = "REQ-UI-048 requirement ids are found in test names")]
    [InlineData("REQ-FN-001 does a thing", "REQ-FN-001")]
    [InlineData("REQ-FN-053/055 stop", "REQ-FN-053,REQ-FN-055")]
    [InlineData("REQ-UI-048 and REQ-FN-051 and REQ-UI-048", "REQ-UI-048,REQ-FN-051")]
    [InlineData("REQ-FN-0510 long", "REQ-FN-0510")]
    [InlineData("no id here", "")]
    public void ScannerFindsIds(string aName, string aExpected) =>
        Assert.Equal(aExpected, string.Join(',', RequirementIdScanner.Scan(aName)));
}
