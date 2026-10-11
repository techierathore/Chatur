using System.Text.Json;
using Chatur.Core.Actions;

namespace Chatur.Core.ProcessRuns;

/// <summary>Reads the tests out of the JSON report <c>npx playwright test --reporter=json</c> writes.</summary>
public static class PlaywrightResultParser
{
    private const int MaxMessageChars = 400;

    /// <summary>Every test in the report, with the titles of the groups it sits in counted into its name.</summary>
    /// <param name="aJson">The report's JSON text.</param>
    /// <exception cref="JsonException">The text is not JSON.</exception>
    public static IReadOnlyList<ReportedTest> Parse(string aJson)
    {
        using var vDocument = JsonDocument.Parse(aJson);
        var vTests = new List<ReportedTest>();
        if (vDocument.RootElement.TryGetProperty("suites", out var vSuites) && vSuites.ValueKind == JsonValueKind.Array)
        {
            foreach (var vSuite in vSuites.EnumerateArray())
            {
                ReadSuite(vSuite, [], vTests);
            }
        }

        return vTests;
    }

    /// <summary>The first error the report lists at its top level (a broken config, no test files), or <see langword="null"/>.</summary>
    /// <param name="aJson">The report's JSON text.</param>
    public static string? FirstError(string aJson)
    {
        using var vDocument = JsonDocument.Parse(aJson);
        if (!vDocument.RootElement.TryGetProperty("errors", out var vErrors) || vErrors.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var vError in vErrors.EnumerateArray())
        {
            if (vError.TryGetProperty("message", out var vMessage) && vMessage.GetString() is { Length: > 0 } vText)
            {
                return OneLine(vText);
            }
        }

        return null;
    }

    private static void ReadSuite(JsonElement aSuite, List<string> aTitles, List<ReportedTest> aTests)
    {
        var vTitles = new List<string>(aTitles);
        var vIsFile = aSuite.TryGetProperty("file", out var vFile) && vFile.GetString() == TitleOf(aSuite) && aTitles.Count == 0;
        if (!vIsFile && TitleOf(aSuite) is { Length: > 0 } vTitle)
        {
            vTitles.Add(vTitle);
        }

        if (aSuite.TryGetProperty("specs", out var vSpecs) && vSpecs.ValueKind == JsonValueKind.Array)
        {
            foreach (var vSpec in vSpecs.EnumerateArray())
            {
                ReadSpec(vSpec, vTitles, aTests);
            }
        }

        if (aSuite.TryGetProperty("suites", out var vChildren) && vChildren.ValueKind == JsonValueKind.Array)
        {
            foreach (var vChild in vChildren.EnumerateArray())
            {
                ReadSuite(vChild, vTitles, aTests);
            }
        }
    }

    private static void ReadSpec(JsonElement aSpec, List<string> aTitles, List<ReportedTest> aTests)
    {
        var vName = string.Join(" › ", aTitles.Append(TitleOf(aSpec)));
        if (!aSpec.TryGetProperty("tests", out var vRuns) || vRuns.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var vRun in vRuns.EnumerateArray())
        {
            var vProject = vRun.TryGetProperty("projectName", out var vProjectName) ? vProjectName.GetString() : null;
            var vFullName = string.IsNullOrEmpty(vProject) ? vName : $"{vName} [{vProject}]";
            var vOutcome = OutcomeOf(vRun.TryGetProperty("status", out var vStatus) ? vStatus.GetString() : null);
            aTests.Add(new ReportedTest(
                "playwright",
                vFullName,
                vOutcome,
                RequirementIdScanner.Scan(vFullName),
                vOutcome == ProcessTestOutcome.Failed ? MessageOf(vRun) : null,
                DurationOf(vRun)));
        }
    }

    private static string TitleOf(JsonElement aElement) =>
        aElement.TryGetProperty("title", out var vTitle) ? vTitle.GetString() ?? string.Empty : string.Empty;

    private static ProcessTestOutcome OutcomeOf(string? aStatus) => aStatus switch
    {
        "expected" or "flaky" => ProcessTestOutcome.Passed,
        "unexpected" => ProcessTestOutcome.Failed,
        _ => ProcessTestOutcome.Skipped
    };

    private static string? MessageOf(JsonElement aRun)
    {
        if (!aRun.TryGetProperty("results", out var vResults) || vResults.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var vResult in vResults.EnumerateArray())
        {
            if (vResult.TryGetProperty("error", out var vError) && vError.TryGetProperty("message", out var vMessage) && vMessage.GetString() is { Length: > 0 } vText)
            {
                return OneLine(vText);
            }
        }

        return null;
    }

    private static long DurationOf(JsonElement aRun)
    {
        if (!aRun.TryGetProperty("results", out var vResults) || vResults.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return vResults.EnumerateArray().Sum(aResult => aResult.TryGetProperty("duration", out var vDuration) && vDuration.TryGetInt64(out var vMs) ? vMs : 0);
    }

    private static string OneLine(string aText)
    {
        var vText = string.Join(' ', StripEscapes(aText).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return vText.Length > MaxMessageChars ? vText[..MaxMessageChars] : vText;
    }

    private static string StripEscapes(string aText) =>
        System.Text.RegularExpressions.Regex.Replace(aText, "\u001b\\[[0-9;]*m", string.Empty);
}
