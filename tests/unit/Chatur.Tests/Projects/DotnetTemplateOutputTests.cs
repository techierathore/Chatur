using Chatur.Core.Actions;
using Chatur.Core.Projects;
using Xunit;

namespace Chatur.Tests.Projects;

/// <summary>
/// Tests for <see cref="DotnetTemplateOutput"/> (BRD-244, REQ-UI-112): reading <c>dotnet new list</c> and a
/// template's help, and the command lines that make a project and add it to a solution. The sample text is
/// real output from the owner's machine.
/// </summary>
public sealed class DotnetTemplateOutputTests
{
    /// <summary>When the real table is read, then each project template comes out with its name, short name, languages and tags.</summary>
    [Fact]
    public void RealTableIsRead()
    {
        var vTemplates = DotnetTemplateOutput.ParseTemplates(DotnetNewSamples.ProjectList);

        var vConsole = Assert.Single(vTemplates, t => t.ShortName == "console");
        Assert.Equal("Console App", vConsole.Name);
        Assert.Equal("[C#],F#,VB", vConsole.Languages);
        Assert.Equal("Common/Console", vConsole.Tags);
        var vMaui = Assert.Single(vTemplates, t => t.ShortName == "maui-blazor");
        Assert.Equal(".NET MAUI Blazor Hybrid App", vMaui.Name);
        Assert.Equal("MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Blazor/Blazor Hybrid/Mobile/Tizen", vMaui.Tags);
        Assert.Contains(vTemplates, t => t.ShortName == "xunit");
        Assert.Contains(vTemplates, t => t.ShortName == "nunit");
        Assert.Contains(vTemplates, t => t.ShortName == "mstest");
        Assert.Contains(vTemplates, t => t.ShortName == "blazor");
        Assert.Contains(vTemplates, t => t.ShortName == "classlib");
    }

    /// <summary>When a template lists several short names, then the first is the one kept.</summary>
    [Fact]
    public void FirstShortNameIsKept()
    {
        var vTemplates = DotnetTemplateOutput.ParseTemplates(DotnetNewSamples.ProjectList);

        Assert.Contains(vTemplates, t => t.ShortName == "webapp" && t.Name.StartsWith("ASP.NET Core Web App", StringComparison.Ordinal));
        Assert.DoesNotContain(vTemplates, t => t.ShortName.Contains(','));
    }

    /// <summary>When a short name is listed twice, then it counts once.</summary>
    [Fact]
    public void RepeatedShortNameCountsOnce()
    {
        var vTemplates = DotnetTemplateOutput.ParseTemplates(DotnetNewSamples.ProjectList);

        Assert.Single(vTemplates, t => t.ShortName == "mcpserver");
    }

    /// <summary>When a template offers no C#, then it is left out; one whose default language is not C# but offers it stays.</summary>
    [Fact]
    public void TemplatesWithoutCSharpAreLeftOut()
    {
        const string vOutput =
            "Template Name  Short Name  Language  Tags\n" +
            "-------------  ----------  --------  ----------\n" +
            "Only F Sharp   fsonly      F#        Common\n" +
            "Mixed          mixed       F#,C#     Common\n" +
            "Plain C Sharp  csonly      [C#]      Common\n";

        var vTemplates = DotnetTemplateOutput.ParseTemplates(vOutput);

        Assert.Equal(new[] { "mixed", "csonly" }, vTemplates.Select(t => t.ShortName).ToArray());
    }

    /// <summary>When the output is not a table, or is empty, then no templates come out and nothing throws.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("No templates found matching: 'zzz'.")]
    public void NoTableGivesNoTemplates(string aOutput)
    {
        Assert.Empty(DotnetTemplateOutput.ParseTemplates(aOutput));
    }

    /// <summary>When the output has Windows line endings, then it reads the same.</summary>
    [Fact]
    public void WindowsLineEndingsRead()
    {
        var vWindows = DotnetNewSamples.ProjectList.Replace("\n", "\r\n", StringComparison.Ordinal);

        Assert.Equal(
            DotnetTemplateOutput.ParseTemplates(DotnetNewSamples.ProjectList).Select(t => t.ShortName),
            DotnetTemplateOutput.ParseTemplates(vWindows).Select(t => t.ShortName));
    }

    /// <summary>When the real console help is read, then the framework choices come out newest first with the template's own default.</summary>
    [Fact]
    public void FrameworksAreReadNewestFirst()
    {
        var vFrameworks = DotnetTemplateOutput.ParseFrameworks(DotnetNewSamples.ConsoleHelp);

        Assert.Equal(new[] { "net11.0", "net10.0", "net9.0", "net8.0" }, vFrameworks.Choices.ToArray());
        Assert.Equal("net11.0", vFrameworks.Default);
    }

    /// <summary>When net10.0 is offered, then it is the framework a new project starts with, not the template's newer default.</summary>
    /// <summary>
    /// When the SDK prints "&lt;choice&gt;" in the option's header (the 10.0 SDK on Linux), then the frameworks are read
    /// from the lines under it, and the word "choice" is never offered as a framework.
    /// </summary>
    [Fact(DisplayName = "REQ-UI-112 frameworks listed under a choice header are read")]
    public void ChoiceHeaderFrameworksAreRead()
    {
        const string vHelp = "Template options:\n  -f, --framework <choice>     The target framework for the project.\n                               Type: choice\n                                 net10.0  Target net10.0\n                                 net9.0   Target net9.0\n                               Default: net10.0\n  --langVersion <langVersion>  Sets the LangVersion property\n";

        var vFrameworks = DotnetTemplateOutput.ParseFrameworks(vHelp);

        Assert.Equal(new[] { "net10.0", "net9.0" }, vFrameworks.Choices.ToArray());
        Assert.Equal("net10.0", vFrameworks.Default);
    }

    [Fact]
    public void Net10IsPreferredWhenOffered()
    {
        var vFrameworks = DotnetTemplateOutput.ParseFrameworks(DotnetNewSamples.ConsoleHelp);

        Assert.Equal("net10.0", vFrameworks.Preferred);
        Assert.Equal("net9.0", new TemplateFrameworks(["net9.0", "net8.0"], null).Preferred);
        Assert.Null(new TemplateFrameworks([], null).Preferred);
    }

    /// <summary>When the help has no framework option, then the choices are empty.</summary>
    [Fact]
    public void HelpWithoutFrameworkHasNoChoices()
    {
        var vFrameworks = DotnetTemplateOutput.ParseFrameworks("Class Library (C#)\n\nTemplate options:\n  --langVersion <langVersion>  Sets it\n");

        Assert.Empty(vFrameworks.Choices);
        Assert.Null(vFrameworks.Default);
    }

    /// <summary>When a project is made, then the command is dotnet new with the name, the location and the framework.</summary>
    [Fact]
    public void NewCommandLineCarriesNameLocationAndFramework()
    {
        var vArguments = DotnetTemplateOutput.NewArguments("console", "Tools", "/work/Sln/Tools", "net10.0");

        Assert.Equal("new console -n \"Tools\" -o \"/work/Sln/Tools\" -f net10.0", vArguments);
    }

    /// <summary>When the template has no framework choice, then no -f is passed.</summary>
    [Fact]
    public void NewCommandLineOmitsFrameworkWhenNone()
    {
        Assert.Equal("new maui-blazor -n \"App\" -o \"/w/App\"", DotnetTemplateOutput.NewArguments("maui-blazor", "App", "/w/App", null));
    }

    /// <summary>When the project is added to the solution, then the command is dotnet sln add with both paths.</summary>
    [Fact]
    public void AddCommandLineNamesSolutionAndProject()
    {
        Assert.Equal(
            "sln \"/w/Sln/Sln.sln\" add \"/w/Sln/Tools/Tools.csproj\"",
            DotnetTemplateOutput.AddToSolutionArguments("/w/Sln/Sln.sln", "/w/Sln/Tools/Tools.csproj"));
    }

    /// <summary>When a short name or framework carries shell characters, then no command line is built.</summary>
    [Theory]
    [InlineData("console & calc", "net10.0")]
    [InlineData("console", "net10.0 --force")]
    public void UnsafeTokensAreRefused(string aShortName, string aFramework)
    {
        Assert.Throws<InvalidOperationException>(() => DotnetTemplateOutput.NewArguments(aShortName, "X", "/w/X", aFramework));
    }

    /// <summary>The help command names the template, and the list command asks for project templates only.</summary>
    [Fact]
    public void ListAndHelpCommandLines()
    {
        Assert.Equal("new list --type project", DotnetTemplateOutput.ListArguments);
        Assert.Equal("new xunit --help", DotnetTemplateOutput.HelpArguments("xunit"));
    }
}
