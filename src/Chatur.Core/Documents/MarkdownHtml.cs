using System.Net;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Chatur.Core.Documents;

/// <summary>
/// Markdown to HTML, for a reply in the conversation (BRD-250, REQ-UI-114) and for the documents
/// <c>*generate-html</c> and <c>*render-workflow-docs</c> write (BRD-249, REQ-FN-311). Raw HTML in the markdown is
/// shown as text, never run, and a link keeps its address only when it is http, https, mailto, or a path inside the
/// project; anything else (a <c>javascript:</c> address) becomes <c>#</c>.
/// </summary>
public static class MarkdownHtml
{
    private static readonly MarkdownPipeline objReplyPipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .UseTaskLists()
        .UseSoftlineBreakAsHardlineBreak()
        .DisableHtml()
        .Build();

    private static readonly MarkdownPipeline objDocumentPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <summary>A reply's markdown as safe HTML: a single line break stays a line break, as the model meant it.</summary>
    /// <param name="aMarkdown">The reply's text.</param>
    public static string Reply(string? aMarkdown) => Render(aMarkdown ?? string.Empty, objReplyPipeline);

    /// <summary>A document's markdown as safe HTML, without the page around it.</summary>
    /// <param name="aMarkdown">The document's text.</param>
    public static string Body(string? aMarkdown) => Render(aMarkdown ?? string.Empty, objDocumentPipeline);

    /// <summary>A whole HTML page for a markdown document, readable on its own in any browser, light or dark.</summary>
    /// <param name="aTitle">The page's title.</param>
    /// <param name="aMarkdown">The document's text.</param>
    public static string Page(string aTitle, string? aMarkdown)
    {
        var vTitle = WebUtility.HtmlEncode(aTitle);
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{vTitle}}</title>
            <style>
            :root { color-scheme: light dark; }
            body { font-family: system-ui, -apple-system, "Segoe UI", sans-serif; line-height: 1.55; max-width: 960px; margin: 2rem auto; padding: 0 1rem; }
            table { border-collapse: collapse; display: block; overflow-x: auto; }
            th, td { border: 1px solid #8884; padding: .35rem .6rem; text-align: left; vertical-align: top; }
            code, pre { font-family: ui-monospace, Consolas, monospace; font-size: .9em; }
            pre { overflow-x: auto; padding: .75rem; border: 1px solid #8884; border-radius: 6px; }
            </style>
            </head>
            <body>
            {{Body(aMarkdown)}}
            </body>
            </html>
            """;
    }

    private static string Render(string aMarkdown, MarkdownPipeline aPipeline)
    {
        var vDocument = Markdown.Parse(aMarkdown, aPipeline);
        foreach (var vLink in vDocument.Descendants<LinkInline>())
        {
            if (!IsSafeAddress(vLink.Url))
            {
                vLink.Url = "#";
            }
        }

        foreach (var vLink in vDocument.Descendants<AutolinkInline>())
        {
            if (!IsSafeAddress(vLink.Url))
            {
                vLink.Url = "#";
            }
        }

        using var vWriter = new StringWriter();
        var vRenderer = new HtmlRenderer(vWriter);
        aPipeline.Setup(vRenderer);
        vRenderer.Render(vDocument);
        vWriter.Flush();
        return vWriter.ToString();
    }

    /// <summary>Whether a link's address may be kept: http, https, mailto, an anchor, or a relative path.</summary>
    /// <param name="aUrl">The address as written.</param>
    public static bool IsSafeAddress(string? aUrl)
    {
        if (string.IsNullOrWhiteSpace(aUrl))
        {
            return true;
        }

        var vUrl = aUrl.Trim();
        var vColon = vUrl.IndexOf(':', StringComparison.Ordinal);
        var vSlash = vUrl.IndexOfAny(['/', '?', '#']);
        if (vColon < 0 || (vSlash >= 0 && vSlash < vColon))
        {
            return true;
        }

        var vScheme = vUrl[..vColon].ToLowerInvariant();
        return vScheme is "http" or "https" or "mailto";
    }
}
