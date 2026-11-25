using Chatur.Core.Actions;
using Chatur.Core.Data;
using Chatur.Core.Integrations;

namespace Chatur.WebHarness.Services;

/// <summary>
/// The addresses Chatur publishes to outside tools on the web harness (REQ-FN-082). Only "Start a process"
/// is answered today. The bearer token names its account, so the call is answered from that account's own database.
/// An action that is off or not built answers 404, as though it were never there.
/// </summary>
public static class IntegrationEndpoints
{
    /// <summary>The body an outside tool sends to start a process.</summary>
    /// <param name="Project">The project's name; the open project when left out.</param>
    /// <param name="Process">The process to start, e.g. <c>verify-phase</c>.</param>
    /// <param name="Phase">The phase, kept for the tool's own record.</param>
    public sealed record StartBody(string? Project, string? Process, int? Phase);

    /// <summary>Maps the published addresses.</summary>
    /// <param name="aApp">The web application.</param>
    public static void MapIntegrationEndpoints(this WebApplication aApp)
    {
        aApp.MapPost($"{PublishedActionCatalogue.Root}/process/start", async (HttpRequest aRequest, StartBody? aBody, IServiceProvider aServices, CancellationToken aCt) =>
        {
            var vHeader = aRequest.Headers.Authorization.ToString();
            var vToken = vHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? vHeader[7..].Trim() : null;
            if (!IntegrationToken.TryReadAccount(vToken, out var vAccount))
            {
                return Results.Json(new { error = "The token is missing or not one Chatur made." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            using var vPin = AccountScope.Enter(vAccount);
            await using var vScope = aServices.CreateAsyncScope();
            var vActions = vScope.ServiceProvider.GetRequiredService<IIntegrationActions>();
            var vResult = await vActions.StartFromOutsideAsync(vToken, aBody?.Project, aBody?.Process, aCt);
            return vResult.Outcome switch
            {
                OutsideCallOutcome.Started => Results.Json(new { run = vResult.RunId, state = vResult.State, project = vResult.Project, process = vResult.Process }),
                OutsideCallOutcome.Unauthorized => Results.Json(new { error = vResult.Message }, statusCode: StatusCodes.Status401Unauthorized),
                OutsideCallOutcome.NotThere => Results.NotFound(),
                _ => Results.Json(new { error = vResult.Message }, statusCode: StatusCodes.Status400BadRequest),
            };
        }).DisableAntiforgery();
    }
}
