namespace ChaturUI.Layouts;

/// <summary>One entry of the secondary window's left navigation list (UI Design "Design system").</summary>
/// <param name="Route">The route this item opens.</param>
/// <param name="Label">The label shown in the list.</param>
/// <param name="TestId">The stable test id for this row.</param>
/// <param name="Title">The title shown in the secondary window's title bar when this item is open.</param>
/// <param name="Icon">The Lucide icon name shown beside the label.</param>
/// <param name="Feature">The licence feature this entry offers, or <see langword="null"/> when no licence switches it; the entry is not drawn while the feature is off (REQ-FN-075).</param>
public sealed record SecondaryNavItem(string Route, string Label, string TestId, string Title, string Icon, string? Feature = null);
