using Chatur.Core.ProcessRuns;
using Chatur.Core.ProcessRuns.Checks;

namespace Chatur.Tests.Support;

/// <summary>A project folder in the temp directory that a test fills with documents and judges with a check.</summary>
public sealed class TempProject : IDisposable
{
    /// <summary>Creates an empty project folder.</summary>
    public TempProject()
    {
        Root = Path.Combine(Path.GetTempPath(), $"chatur-check-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>The folder.</summary>
    public string Root { get; }

    /// <summary>Writes a file at a project-relative path.</summary>
    /// <param name="aRelative">The path.</param>
    /// <param name="aText">The text.</param>
    public TempProject Write(string aRelative, string aText)
    {
        var vFull = Path.Combine(Root, aRelative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(vFull)!);
        File.WriteAllText(vFull, aText);
        return this;
    }

    /// <summary>Sets a file's last-write time.</summary>
    /// <param name="aRelative">The path.</param>
    /// <param name="aUtc">The time.</param>
    public TempProject Touch(string aRelative, DateTime aUtc)
    {
        File.SetLastWriteTimeUtc(Path.Combine(Root, aRelative.Replace('/', Path.DirectorySeparatorChar)), aUtc);
        return this;
    }

    /// <summary>A check context over this folder, the run having begun at <paramref name="aRunStartedUtc"/>.</summary>
    /// <param name="aRunStartedUtc">When the run began.</param>
    /// <param name="aBaseline">The snapshot taken when the run began, or null for none.</param>
    /// <param name="aToday">Today.</param>
    public CheckContext Context(DateTime? aRunStartedUtc = null, IReadOnlyDictionary<string, string>? aBaseline = null, DateTime? aToday = null) => new(
        1,
        1,
        Root,
        new ProjectDocuments(Root),
        aRunStartedUtc ?? DateTime.UtcNow.AddMinutes(-5),
        aBaseline ?? new Dictionary<string, string>(),
        null!,
        null!,
        null!,
        null,
        aToday ?? DateTime.UtcNow);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }
}
