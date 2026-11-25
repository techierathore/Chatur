namespace Chatur.Tests.Projects;

/// <summary>Output captured from <c>dotnet new list --type project</c> and <c>dotnet new console --help</c> on the owner's machine (SDK 11.0.100 beside 9.0), cut to the rows the tests use.</summary>
internal static class DotnetNewSamples
{
    /// <summary>The project template table, with the header, the dashes, and a sample of the real rows.</summary>
    public const string ProjectList =
"""
These templates matched your input: --type='project'

Template Name                     Short Name                          Language    Tags                                                                         
--------------------------------  ----------------------------------  ----------  -----------------------------------------------------------------------------
.NET MAUI Blazor Hybrid App       maui-blazor                         [C#]        MAUI/Android/iOS/macOS/Mac Catalyst/Windows/Blazor/Blazor Hybrid/Mobile/Tizen
Android Test Project              androidtest                         [C#]        Android/Mobile/Test                                                          
ASP.NET Core Web API (native ...  webapiaot                           [C#]        Web/Web API/API/Service                                                      
ASP.NET Core Web App (Razor P...  webapp,razor                        [C#]        Web/MVC/Razor Pages                                                          
Blazor Web App                    blazor                              [C#]        Web/Blazor/WebAssembly                                                       
Class Library                     classlib                            [C#],F#,VB  Common/Library                                                               
Console App                       console                             [C#],F#,VB  Common/Console                                                               
iOS Application                   ios                                 [C#],F#,VB  iOS/Mobile                                                                   
Local MCP Server Console App      mcpserver                           [C#]        Common/AI/MCP                                                                
MCP Server App                    mcpserver                           [C#]        Common/AI/MCP                                                                
MSTest Test Project               mstest                              [C#],F#,VB  Test/MSTest/Desktop/Web                                                      
NUnit Test Project                nunit                               [C#],F#,VB  Test/NUnit/Desktop/Web                                                       
TUnit Mixed Test Project          TUnit.FSharp,TUnit.VB,TUnit         [C#],F#,VB  Test/TUnit                                                                   
xUnit Test Project                xunit                               [C#],F#,VB  Test/xUnit/Desktop/Web                                                       
"""
        ;

    /// <summary>The framework option block of <c>dotnet new console --help</c>.</summary>
    public const string ConsoleHelp =
"""
Console App (C#)
Author: Microsoft

Usage:
  dotnet new console [options] [template options]

Options:
  -n, --name <name>       The name for the output being created. If no name is specified, the name of the output directory is used.
  -o, --output <output>   Location to place the generated output.
  --type <project>        Specifies the template type to instantiate.

Template options:
  -f, --framework <net10.0|net11.0|net8.0|net9.0>  The target framework for the project.
                                                   Type: choice
                                                     net11.0  Target net11.0
                                                     net10.0  Target net10.0
                                                     net9.0   Target net9.0
                                                     net8.0   Target net8.0
                                                   Default: net11.0
  --langVersion <langVersion>                      Sets the LangVersion property in the created project file
                                                   Type: text
"""
        ;
}
