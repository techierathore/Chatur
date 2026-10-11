using TechieRag.Abstractions;
using TechieRag.Llm;

namespace Chatur.Core.Routing;

/// <summary><see cref="IModelChooserFactory"/> over TechieRag's own <see cref="ModelChooser"/> (REQ-FN-071).</summary>
public sealed class TechieRagModelChooserFactory : IModelChooserFactory
{
    /// <inheritdoc />
    public IModelChooser Create(ILlmProvider aSmallModel) =>
        new ModelChooser(aSmallModel, new ModelChooserOptions
        {
            Guidance = "Choose the cheapest model that can do the work well. A short question or a greeting needs the smallest model; writing or reviewing code, or a long plan, needs the strongest."
        });
}
