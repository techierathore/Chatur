using System.Net;
using TechieRag.Models;

namespace Chatur.Core.ProcessRuns;

/// <summary>
/// Recognises a model's usage limit in what a provider raised, and marks the model calls made on behalf of an
/// end-to-end run (REQ-FN-064). TechieRag reports a rate limit (HTTP 429, or 503 with a <c>Retry-After</c>) as
/// <see cref="LlmRateLimitException"/> carrying the provider's <c>RetryAfter</c>; a provider that raises a bare
/// <see cref="HttpRequestException"/> with status 429 has no wait to give.
/// </summary>
public static class ModelUsageLimit
{
    /// <summary>The usage limit a failure is, or <see langword="null"/> when it is anything else.</summary>
    /// <param name="aFailure">What the provider raised; inner exceptions are looked through.</param>
    public static ModelUsageLimitException? Recognise(Exception aFailure)
    {
        for (var vCurrent = aFailure; vCurrent is not null; vCurrent = vCurrent.InnerException)
        {
            if (vCurrent is LlmRateLimitException vRate)
            {
                return new ModelUsageLimitException(vRate.Message, vRate.RetryAfter);
            }

            if (vCurrent is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } vHttp)
            {
                return new ModelUsageLimitException(vHttp.Message);
            }
        }

        return null;
    }
}

/// <summary>
/// Says that the model calls made inside it are made on behalf of an unattended run, so a usage limit is raised as
/// a <see cref="ModelUsageLimitException"/> for the run to wait out instead of being shown to an owner as a note.
/// It follows the call through <see langword="await"/>s and ends when the scope is disposed.
/// </summary>
public static class ModelCallScope
{
    private static readonly AsyncLocal<bool> ForRunFlag = new();

    /// <summary>Whether the code running now is making model calls on behalf of an unattended run.</summary>
    public static bool IsForRun => ForRunFlag.Value;

    /// <summary>Marks the calls made until the result is disposed as made for a run.</summary>
    public static IDisposable ForRun()
    {
        var vWas = ForRunFlag.Value;
        ForRunFlag.Value = true;
        return new Restore(vWas);
    }

    private sealed class Restore : IDisposable
    {
        private readonly bool objWas;

        public Restore(bool aWas) => objWas = aWas;

        public void Dispose() => ForRunFlag.Value = objWas;
    }
}
