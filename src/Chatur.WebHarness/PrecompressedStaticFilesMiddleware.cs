using Microsoft.AspNetCore.StaticFiles;

namespace Chatur.WebHarness;

/// <summary>
/// Serves the pre-compressed <c>.br</c>/<c>.gz</c> files the SDK publish produces when the
/// uncompressed file is missing. Without this, Blazor Server's <c>_framework/blazor.web.js</c>
/// and TrBlazeUI's static assets return 404 after a publish that keeps only compressed copies.
/// </summary>
public static class PrecompressedStaticFilesExtensions
{
    private static readonly FileExtensionContentTypeProvider objContentTypes = new();

    /// <summary>
    /// Adds middleware that serves <c>.br</c> or <c>.gz</c> variants of <c>_framework</c> and
    /// <c>_content</c> requests when the uncompressed file does not exist on disk.
    /// </summary>
    public static IApplicationBuilder UsePrecompressedStaticFiles(this IApplicationBuilder aApp)
    {
        return aApp.Use(async (aContext, aNext) =>
        {
            var vPath = aContext.Request.Path.Value ?? string.Empty;
            if (!vPath.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase) &&
                !vPath.StartsWith("/_content/", StringComparison.OrdinalIgnoreCase))
            {
                await aNext();
                return;
            }

            var vEnv = aContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var vRelative = vPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var vFilePath = Path.Combine(vEnv.WebRootPath, vRelative);

            if (File.Exists(vFilePath))
            {
                await aNext();
                return;
            }

            var vAccept = aContext.Request.Headers.AcceptEncoding.ToString();
            string? vCompressedPath = null;
            string? vEncoding = null;

            if (vAccept.Contains("br", StringComparison.OrdinalIgnoreCase))
            {
                vCompressedPath = $"{vFilePath}.br";
                vEncoding = "br";
            }
            else if (vAccept.Contains("gzip", StringComparison.OrdinalIgnoreCase))
            {
                vCompressedPath = $"{vFilePath}.gz";
                vEncoding = "gzip";
            }

            if (vCompressedPath is not null && File.Exists(vCompressedPath))
            {
                if (!objContentTypes.TryGetContentType(vFilePath, out var vContentType))
                {
                    vContentType = "application/octet-stream";
                }

                aContext.Response.ContentType = vContentType;
                aContext.Response.Headers.ContentEncoding = vEncoding;
                await aContext.Response.SendFileAsync(vCompressedPath);
                return;
            }

            await aNext();
        });
    }
}
