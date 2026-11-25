using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Chatur.Core.BuildRun;

/// <summary>
/// Reads which projects a solution lists and which of them can be run (BRD-227), from the solution file
/// itself (<c>.sln</c> or the XML <c>.slnx</c>) and each project's own <c>.csproj</c> — never by asking
/// the owner, and never by building anything.
/// </summary>
public static partial class SolutionProjects
{
    /// <summary>One runnable project of a solution.</summary>
    /// <param name="Path">The project file's full path.</param>
    /// <param name="TargetFrameworks">The frameworks it targets, in the order its file names them.</param>
    public sealed record RunnableProject(string Path, IReadOnlyList<string> TargetFrameworks);

    /// <summary>Whether a path names a solution file, <c>.sln</c> or <c>.slnx</c>.</summary>
    /// <param name="aPath">The path.</param>
    public static bool IsSolutionFile(string aPath) =>
        aPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) || aPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase);

    /// <summary>The full paths of the C# projects a solution lists, in the solution's own order.</summary>
    /// <param name="aSolutionPath">The solution file.</param>
    public static IReadOnlyList<string> Listed(string aSolutionPath)
    {
        var vRoot = System.IO.Path.GetDirectoryName(aSolutionPath)!;
        string vText;
        try
        {
            vText = File.ReadAllText(aSolutionPath);
        }
        catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        IEnumerable<string> vRelative;
        if (aSolutionPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                vRelative = XDocument.Parse(vText).Descendants()
                    .Where(e => e.Name.LocalName == "Project")
                    .Select(e => (string?)e.Attribute("Path") ?? string.Empty);
            }
            catch (System.Xml.XmlException)
            {
                return [];
            }
        }
        else
        {
            vRelative = SlnProjectLine().Matches(vText).Select(m => m.Groups["path"].Value);
        }

        return vRelative
            .Where(r => r.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Select(r => System.IO.Path.GetFullPath(System.IO.Path.Combine(vRoot, r.Replace('\\', System.IO.Path.DirectorySeparatorChar).Replace('/', System.IO.Path.DirectorySeparatorChar))))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The projects of a solution that can be run: a web, worker or Blazor WebAssembly project, a MAUI app,
    /// or one whose <c>OutputType</c> is <c>Exe</c> or <c>WinExe</c>; never a test project or a library.
    /// </summary>
    /// <param name="aSolutionPath">The solution file.</param>
    public static IReadOnlyList<RunnableProject> Runnable(string aSolutionPath)
    {
        var vResult = new List<RunnableProject>();
        foreach (var vProject in Listed(aSolutionPath))
        {
            string vText;
            try
            {
                vText = File.ReadAllText(vProject);
            }
            catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (IsRunnable(vText))
            {
                vResult.Add(new RunnableProject(vProject, TargetFrameworksOf(vText)));
            }
        }

        return vResult;
    }

    /// <summary>
    /// The test projects a solution lists (Microsoft.NET.Test.Sdk or <c>IsTestProject</c>), by full path: their run
    /// target runs their tests (REQ-UI-107).
    /// </summary>
    /// <param name="aSolutionPath">The full path of the .sln or .slnx file.</param>
    public static IReadOnlyList<string> TestProjects(string aSolutionPath)
    {
        var vResult = new List<string>();
        foreach (var vProject in Listed(aSolutionPath))
        {
            try
            {
                var vText = File.ReadAllText(vProject);
                if (vText.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) || TestProjectProperty().IsMatch(vText))
                {
                    vResult.Add(vProject);
                }
            }
            catch (Exception aException) when (aException is IOException or UnauthorizedAccessException)
            {
                // a project that cannot be read is not offered
            }
        }

        return vResult;
    }

    /// <summary>Whether a project file's text describes something that can be run.</summary>
    /// <param name="aProjectText">The <c>.csproj</c> text.</param>
    public static bool IsRunnable(string aProjectText)
    {
        if (aProjectText.Contains("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)
            || TestProjectProperty().IsMatch(aProjectText))
        {
            return false;
        }

        var vOutputType = OutputTypeProperty().Match(aProjectText);
        if (vOutputType.Success)
        {
            var vType = vOutputType.Groups[1].Value.Trim();
            return vType.Equals("Exe", StringComparison.OrdinalIgnoreCase) || vType.Equals("WinExe", StringComparison.OrdinalIgnoreCase);
        }

        var vSdk = SdkAttribute().Match(aProjectText).Groups[1].Value;
        return vSdk.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase)
            || vSdk.Contains("Microsoft.NET.Sdk.Worker", StringComparison.OrdinalIgnoreCase)
            || vSdk.Contains("Microsoft.NET.Sdk.BlazorWebAssembly", StringComparison.OrdinalIgnoreCase)
            || UseMauiProperty().IsMatch(aProjectText);
    }

    private static IReadOnlyList<string> TargetFrameworksOf(string aProjectText)
    {
        var vMany = TargetFrameworksProperty().Match(aProjectText);
        if (vMany.Success)
        {
            // A MAUI project names its platforms with MSBuild conditions; only the plain names are kept.
            return vMany.Groups[1].Value
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !t.Contains('$'))
                .ToList();
        }

        var vOne = TargetFrameworkProperty().Match(aProjectText);
        return vOne.Success ? [vOne.Groups[1].Value.Trim()] : [];
    }

    [GeneratedRegex(@"^Project\(""\{[^}]+\}""\)\s*=\s*""[^""]*""\s*,\s*""(?<path>[^""]+)""", RegexOptions.Multiline)]
    private static partial Regex SlnProjectLine();

    [GeneratedRegex(@"<OutputType>\s*([^<]+?)\s*</OutputType>", RegexOptions.IgnoreCase)]
    private static partial Regex OutputTypeProperty();

    [GeneratedRegex(@"<IsTestProject>\s*true\s*</IsTestProject>", RegexOptions.IgnoreCase)]
    private static partial Regex TestProjectProperty();

    [GeneratedRegex(@"<UseMaui>\s*true\s*</UseMaui>", RegexOptions.IgnoreCase)]
    private static partial Regex UseMauiProperty();

    [GeneratedRegex(@"<Project\s+Sdk=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex SdkAttribute();

    [GeneratedRegex(@"<TargetFrameworks>([^<]+)</TargetFrameworks>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworksProperty();

    [GeneratedRegex(@"<TargetFramework>([^<]+)</TargetFramework>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworkProperty();
}
