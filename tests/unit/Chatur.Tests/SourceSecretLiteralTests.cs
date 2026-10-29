using System.Text.RegularExpressions;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Guards REQ-NFR-001 at the source level: a secret is never written into a source file. A real
/// provider key once sat as a string literal in <c>src/Chatur.WebHarness/Program.cs</c>, the file
/// was in a public repository and the key had to be revoked. This test reads every text file under
/// <c>src/</c> and fails on any token that starts <c>oc_sk_</c> or <c>sk-</c>, the shapes of the
/// provider keys Chatur connects to. The real key comes from configuration (user secrets) only.
/// </summary>
public sealed class SourceSecretLiteralTests
{
    private static readonly string[] TextExtensions =
    {
        ".cs", ".razor", ".json", ".js", ".css", ".xaml", ".csproj", ".props", ".targets",
        ".html", ".md", ".txt", ".xml", ".yaml", ".yml", ".config", ".plist", ".manifest",
    };

    // A token that starts oc_sk_ or sk- and is not the tail of a longer word: "ask-input" and
    // "ask-me-first" are not keys; "sk-live-…" and "oc_sk_…" are.
    private static readonly Regex KeyShape = new(
        @"(?<![A-Za-z0-9_-])(?:oc_sk_|sk-)[A-Za-z0-9_-]*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// When every text file under <c>src/</c> (build output and deployed library dot-folders excluded) is read, then no token in any
    /// of them starts <c>oc_sk_</c> or <c>sk-</c>. The failure message names the file and line and
    /// shows only the first eight characters of the match, so the test output never carries a key.
    /// </summary>
    [Fact(DisplayName = "REQ-NFR-001 no provider key literal is written into a source file under src/")]
    public void NoProviderKeyLiteralUnderSrc()
    {
        var vSourceRoot = Path.Combine(FindRepositoryRoot(), "src");
        Assert.True(Directory.Exists(vSourceRoot), $"src/ folder not found at {vSourceRoot}");

        var vHits = new List<string>();
        foreach (var vFile in EnumerateSourceFiles(vSourceRoot))
        {
            var vLines = File.ReadAllLines(vFile);
            for (var vIndex = 0; vIndex < vLines.Length; vIndex++)
            {
                foreach (Match vMatch in KeyShape.Matches(vLines[vIndex]))
                {
                    var vShown = vMatch.Value.Length > 8 ? vMatch.Value[..8] + "…" : vMatch.Value;
                    vHits.Add($"{Path.GetRelativePath(vSourceRoot, vFile)}:{vIndex + 1} — {vShown}");
                }
            }
        }

        Assert.True(
            vHits.Count == 0,
            "A provider-key-shaped literal is in a source file under src/. Read it from configuration instead:"
            + Environment.NewLine + string.Join(Environment.NewLine, vHits));
    }

    /// <summary>Every text file under the folder, skipping build output and deployed library dot-folders.</summary>
    private static IEnumerable<string> EnumerateSourceFiles(string aRoot)
    {
        var vPending = new Stack<string>();
        vPending.Push(aRoot);
        while (vPending.Count > 0)
        {
            var vFolder = vPending.Pop();
            foreach (var vChild in Directory.EnumerateDirectories(vFolder))
            {
                var vName = Path.GetFileName(vChild);

                // A dot-folder under src/ (.techierag, .opencode, .trblazeui) is a library's
                // NuGet-deployed reference copy with placeholder keys in its examples,
                // never Chatur source.
                if (vName.StartsWith('.')
                    || vName.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || vName.Equals("obj", StringComparison.OrdinalIgnoreCase)
                    || vName.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                vPending.Push(vChild);
            }

            foreach (var vFile in Directory.EnumerateFiles(vFolder))
            {
                if (TextExtensions.Contains(Path.GetExtension(vFile), StringComparer.OrdinalIgnoreCase))
                {
                    yield return vFile;
                }
            }
        }
    }

    /// <summary>Walks up from the test assembly's folder to the folder that holds <c>Chatur.sln</c>.</summary>
    private static string FindRepositoryRoot()
    {
        var vFolder = new DirectoryInfo(AppContext.BaseDirectory);
        while (vFolder is not null)
        {
            if (File.Exists(Path.Combine(vFolder.FullName, "Chatur.sln")))
            {
                return vFolder.FullName;
            }

            vFolder = vFolder.Parent;
        }

        throw new InvalidOperationException("Chatur.sln was not found above " + AppContext.BaseDirectory);
    }
}
