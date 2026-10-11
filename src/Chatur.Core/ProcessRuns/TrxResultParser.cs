using System.Globalization;
using System.Xml.Linq;
using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>Reads the tests out of a Visual Studio test results file (<c>.trx</c>) that <c>dotnet test --logger trx</c> writes.</summary>
public static class TrxResultParser
{
    private const int MaxMessageChars = 400;

    /// <summary>Every test result in the file, in file order.</summary>
    /// <param name="aTrx">The file's XML text.</param>
    /// <exception cref="System.Xml.XmlException">The text is not XML.</exception>
    public static IReadOnlyList<ReportedTest> Parse(string aTrx)
    {
        var vRoot = XDocument.Parse(aTrx).Root;
        if (vRoot is null)
        {
            return [];
        }

        var vMethods = vRoot.Descendants()
            .Where(aElement => aElement.Name.LocalName == "UnitTest")
            .ToDictionary(
                aElement => (string?)aElement.Attribute("id") ?? string.Empty,
                aElement => MethodOf(aElement),
                StringComparer.OrdinalIgnoreCase);

        return vRoot.Descendants()
            .Where(aElement => aElement.Name.LocalName == "UnitTestResult")
            .Select(aElement => ToTest(aElement, vMethods))
            .ToList();
    }

    private static string MethodOf(XElement aUnitTest)
    {
        var vMethod = aUnitTest.Elements().FirstOrDefault(aElement => aElement.Name.LocalName == "TestMethod");
        return vMethod is null ? string.Empty : $"{(string?)vMethod.Attribute("className")}.{(string?)vMethod.Attribute("name")}";
    }

    private static ReportedTest ToTest(XElement aResult, Dictionary<string, string> aMethods)
    {
        var vName = (string?)aResult.Attribute("testName") ?? string.Empty;
        var vMethod = aMethods.GetValueOrDefault((string?)aResult.Attribute("testId") ?? string.Empty) ?? string.Empty;
        var vOutcome = OutcomeOf((string?)aResult.Attribute("outcome"));
        var vMessage = vOutcome == ProcessTestOutcome.Failed ? MessageOf(aResult) : null;
        return new ReportedTest(
            "dotnet",
            vName,
            vOutcome,
            RequirementIdScanner.Scan(vName + " " + vMethod),
            vMessage,
            DurationOf((string?)aResult.Attribute("duration")));
    }

    private static ProcessTestOutcome OutcomeOf(string? aOutcome) => aOutcome switch
    {
        "Passed" => ProcessTestOutcome.Passed,
        "Failed" or "Error" or "Timeout" or "Aborted" => ProcessTestOutcome.Failed,
        _ => ProcessTestOutcome.Skipped
    };

    private static string? MessageOf(XElement aResult)
    {
        var vMessage = aResult.Descendants().FirstOrDefault(aElement => aElement.Name.LocalName == "Message")?.Value.Trim();
        if (string.IsNullOrEmpty(vMessage))
        {
            return null;
        }

        var vOneLine = string.Join(' ', vMessage.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return vOneLine.Length > MaxMessageChars ? vOneLine[..MaxMessageChars] : vOneLine;
    }

    private static long DurationOf(string? aDuration) =>
        TimeSpan.TryParse(aDuration, CultureInfo.InvariantCulture, out var vSpan) ? (long)vSpan.TotalMilliseconds : 0;
}
