namespace Chatur.Core.Guards;

/// <summary>Whether a guard allows a tool request to reach the tool (REQ-FN-036).</summary>
/// <param name="Allowed">Whether the request may proceed.</param>
/// <param name="RefusalReason">Why it was refused, shown in the activity panel; <see langword="null"/> when allowed.</param>
public sealed record GuardResult(bool Allowed, string? RefusalReason)
{
    /// <summary>A result that lets the request through.</summary>
    public static GuardResult Allow() => new(true, null);

    /// <summary>
    /// A result that refuses the request.
    /// </summary>
    /// <param name="aReason">Why the request was refused.</param>
    public static GuardResult Refuse(string aReason) => new(false, aReason);
}
