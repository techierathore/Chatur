using System.Text;

namespace Chatur.Tests.Support;

/// <summary>
/// Writes a tiny, real .NET test project into a temporary folder: tests named with requirement ids,
/// one passing and one failing, restored from the machine's own package cache so it builds without a
/// network. The verdict tests run Chatur's real build and test steps against it.
/// </summary>
public static class FixtureDotNetProject
{
    /// <summary>A test carrying <c>REQ-FN-001</c> that passes.</summary>
    public const string PassingId = "REQ-FN-001";

    /// <summary>A test carrying <c>REQ-FN-002</c> that fails.</summary>
    public const string FailingId = "REQ-FN-002";

    /// <summary>The failing test's display name.</summary>
    public const string FailingName = "REQ-FN-002 second requirement fails";

    private const string Tests =
        """
        using System.Threading;
        using Xunit;

        namespace Fixture;

        public class RequirementTests
        {
            [Fact(DisplayName = "REQ-FN-001 first requirement passes")]
            public void FirstPasses() => Assert.Equal(4, 2 + 2);

            [Fact(DisplayName = "REQ-FN-002 second requirement fails")]
            public void SecondFails()
            {
                Thread.Sleep(1500);
                Assert.Fail("deliberate failure for the verdict test");
            }

            [Fact(DisplayName = "REQ-FN-006/007 shorthand pair passes")]
            public void PairPasses() => Assert.Equal(6, 3 + 3);
        }
        """;

    /// <summary>Writes the project (and an empty <c>docs</c> folder) into the folder.</summary>
    /// <param name="aFolder">The folder to write into; created when missing.</param>
    /// <param name="aBrokenBuild">Whether the test source does not compile.</param>
    public static void Write(string aFolder, bool aBrokenBuild = false)
    {
        Directory.CreateDirectory(Path.Combine(aFolder, "docs"));
        File.WriteAllText(Path.Combine(aFolder, "Directory.Build.props"), "<Project />\n");
        File.WriteAllText(Path.Combine(aFolder, "nuget.config"), NuGetConfig());
        File.WriteAllText(
            Path.Combine(aFolder, "Fixture.Tests.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <IsPackable>false</IsPackable>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
                <PackageReference Include="xunit" Version="2.*" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(aFolder, "RequirementTests.cs"), aBrokenBuild ? Tests.Replace("4, 2 + 2", "4, 2 +", StringComparison.Ordinal) : Tests);
    }

    private static string NuGetConfig()
    {
        var vCache = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrWhiteSpace(vCache))
        {
            vCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }

        var vText = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>\n  <packageSources>\n    <clear />\n");
        vText.Append(Directory.Exists(vCache)
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"    <add key=\"cache\" value=\"{vCache}\" />\n")
            : "    <add key=\"nuget\" value=\"https://api.nuget.org/v3/index.json\" />\n");
        vText.Append("  </packageSources>\n</configuration>\n");
        return vText.ToString();
    }
}
