using Chatur.Core.Actions;
using Chatur.Core.Files;
using Xunit;

namespace Chatur.Tests;

/// <summary>
/// Tests for <see cref="FileSearchActions"/>, the search behind Edit ▸ Go to file and Find in files
/// (REQ-UI-073), over a fixed in-memory project.
/// </summary>
public sealed class FileSearchActionsTests
{
    private readonly FileSearchActions objSut = new(new FakeFiles());

    /// <summary>A name that starts a file's name comes before one that is only in a folder's name; equal matches are in path order.</summary>
    [Fact]
    public async Task FilesStartingWithTheNameComeFirst()
    {
        var vHits = await objSut.FindFilesAsync(1, "notes");

        Assert.Equal(["docs/notes-old.md", "Notes/notes.txt", "src/Notes/other.cs"], vHits.Select(h => h.RelativePath).ToArray());
    }

    /// <summary>A name that matches nothing lists nothing.</summary>
    [Fact]
    public async Task UnknownNameFindsNothing()
    {
        Assert.Empty(await objSut.FindFilesAsync(1, "zzz"));
    }

    /// <summary>An empty name lists files, so Go to file opens with something to pick.</summary>
    [Fact]
    public async Task EmptyNameListsFiles()
    {
        Assert.NotEmpty(await objSut.FindFilesAsync(1, string.Empty));
    }

    /// <summary>Finding text gives each line it is on, with the line number, in any case.</summary>
    [Fact]
    public async Task TextIsFoundOnEachLineInAnyCase()
    {
        var vHits = await objSut.FindInFilesAsync(1, "TICK");

        Assert.Equal(2, vHits.Count);
        Assert.Equal(("docs/notes-old.md", 1, "tick tock"), (vHits[0].RelativePath, vHits[0].Line, vHits[0].Text));
        Assert.Equal(("Program.cs", 2, "Console.WriteLine(\"tick\");"), (vHits[1].RelativePath, vHits[1].Line, vHits[1].Text));
    }

    /// <summary>A file with a NUL in it is binary and is not searched, even when its extension is not on the list.</summary>
    [Fact]
    public async Task BinaryContentIsNotSearched()
    {
        Assert.DoesNotContain(await objSut.FindInFilesAsync(1, "secret"), h => h.RelativePath == "data.bin2");
    }

    /// <summary>The search stops at the number of hits asked for.</summary>
    [Fact]
    public async Task SearchStopsAtTheMaximum()
    {
        Assert.Single(await objSut.FindInFilesAsync(1, "tick", 1));
    }

    private sealed class FakeFiles : IFileActions
    {
        private static readonly Dictionary<string, string> Contents = new()
        {
            ["Program.cs"] = "using System;\nConsole.WriteLine(\"tick\");\n",
            ["Notes/notes.txt"] = "alpha\n",
            ["docs/notes-old.md"] = "tick tock\n",
            ["src/Notes/other.cs"] = "class Other { }\n",
            ["data.bin2"] = "secret\0binary",
        };

        public Task<IReadOnlyList<FileTreeNode>> TreeAsync(int aProjectId, CancellationToken aCt = default)
        {
            FileTreeNode File(string aPath) => new(Path.GetFileName(aPath), aPath, false, []);
            IReadOnlyList<FileTreeNode> vTree =
            [
                new FileTreeNode("Notes", "Notes", true, [File("Notes/notes.txt")]),
                new FileTreeNode("docs", "docs", true, [File("docs/notes-old.md")]),
                new FileTreeNode("src", "src", true, [new FileTreeNode("Notes", "src/Notes", true, [File("src/Notes/other.cs")])]),
                File("Program.cs"),
                File("data.bin2"),
            ];
            return Task.FromResult(vTree);
        }

        public Task<string> ReadAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) =>
            Task.FromResult(Contents[aRelativePath]);

        public Task SaveAsync(int aProjectId, string aRelativePath, string aContent, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task OpenInEditorAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) => throw new NotSupportedException();

        public Task OpenWithDefaultAppAsync(int aProjectId, string aRelativePath, CancellationToken aCt = default) => throw new NotSupportedException();
    }
}
