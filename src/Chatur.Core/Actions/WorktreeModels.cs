namespace Chatur.Core.Actions;

/// <summary>One working copy of the project's repository, as the Working copies screen lists it (REQ-FN-079).</summary>
/// <param name="Path">The copy's folder.</param>
/// <param name="Branch">The branch the copy sits on; empty when its head is detached.</param>
/// <param name="IsMain">Whether this is the folder the owner works in, which is never removed here.</param>
/// <param name="RunId">The newest run that worked in this copy, or <see langword="null"/> when none did.</param>
/// <param name="RunProcess">The process of that run, e.g. <c>refresh-status</c>.</param>
/// <param name="State"><c>in use</c> (the owner's own folder, or a run is going in it), <c>finished</c> (a run worked in it and has ended) or <c>free</c>.</param>
/// <param name="MadeUtc">When the copy's folder was made.</param>
public sealed record WorkingCopy(string Path, string Branch, bool IsMain, int? RunId, string? RunProcess, string State, DateTime MadeUtc);

/// <summary>The branches the Add dialog offers and the folder under which it proposes a copy's own folder.</summary>
/// <param name="Branches">Local branches that no working copy has checked out yet.</param>
/// <param name="FolderRoot">A folder outside the project under which copies are kept by default.</param>
public sealed record WorkingCopyChoices(IReadOnlyList<string> Branches, string FolderRoot)
{
    /// <summary>The folder proposed for a copy of a branch: the root plus the branch name with its slashes made safe.</summary>
    /// <param name="aBranch">The branch.</param>
    public string FolderFor(string aBranch)
    {
        var vSlug = new string(aBranch.Trim().Select(aChar => char.IsLetterOrDigit(aChar) || aChar is '-' or '_' or '.' ? aChar : '-').ToArray()).Trim('-');
        return Path.Combine(FolderRoot, vSlug.Length == 0 ? "copy" : vSlug);
    }
}
