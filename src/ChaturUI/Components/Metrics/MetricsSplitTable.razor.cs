using Chatur.Core.Actions;
using Microsoft.AspNetCore.Components;

namespace ChaturUI.Components.Metrics;

/// <summary>One split of the Measurements screen: by model, by agent or by command (REQ-UI-117). Presentation only.</summary>
public partial class MetricsSplitTable
{
    /// <summary>The lines, in the order the action gave them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunSplitLine> Lines { get; set; } = [];

    /// <summary>Which split: <c>model</c>, <c>agent</c> or <c>command</c>; it ends the table's test id.</summary>
    [Parameter, EditorRequired]
    public string Kind { get; set; } = string.Empty;

    /// <summary>The first column's heading: Model, Agent or Command phase.</summary>
    [Parameter, EditorRequired]
    public string KeyHeader { get; set; } = string.Empty;
}
