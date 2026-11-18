using System.Collections.Concurrent;
using Chatur.Core.Actions;
using Chatur.Core.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.AutoRun;

/// <summary>
/// The real <see cref="IAutoRunLauncher"/>: one background task per run, each in its own service scope, so
/// a run goes on after the screen that started it is closed. Registered as a singleton.
/// </summary>
public sealed class BackgroundAutoRunLauncher : IAutoRunLauncher
{
    private readonly ConcurrentDictionary<string, bool> objActive = new();
    private readonly IServiceScopeFactory objScopes;
    private readonly ILogger<BackgroundAutoRunLauncher> objLogger;

    /// <summary>Creates the launcher.</summary>
    /// <param name="aScopes">Makes the scope each run works in.</param>
    /// <param name="aLogger">Where a failure inside a run is logged.</param>
    public BackgroundAutoRunLauncher(IServiceScopeFactory aScopes, ILogger<BackgroundAutoRunLauncher> aLogger)
    {
        objScopes = aScopes;
        objLogger = aLogger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// The run belongs to the account that launched it, and stays pinned to that account's database while
    /// it goes on in the background, whoever signs in meanwhile (BRD-213, BRD-216). Two accounts number
    /// their runs from 1 each, so "already going" is asked per account.
    /// </remarks>
    public bool Launch(int aAutoRunId)
    {
        string? vUserId;
        using (var vScope = objScopes.CreateScope())
        {
            vUserId = vScope.ServiceProvider.GetService<IDbConnectionFactory>()?.CurrentUserId;
        }

        var vKey = $"{vUserId ?? "install"}|{aAutoRunId}";
        if (!objActive.TryAdd(vKey, true))
        {
            return false;
        }

        _ = Task.Run(() => CarryOnAsync(aAutoRunId, vUserId, vKey));
        return true;
    }

    private async Task CarryOnAsync(int aAutoRunId, string? aUserId, string aKey)
    {
        using var vPin = AccountScope.Enter(aUserId);
        try
        {
            using var vScope = objScopes.CreateScope();
            var vActions = vScope.ServiceProvider.GetRequiredService<IAutoRunActions>();
            await vActions.AdvanceAsync(aAutoRunId).ConfigureAwait(false);
        }
        catch (Exception aEx)
        {
            objLogger.LogError(aEx, "End-to-end run {AutoRunId} failed inside Chatur.", aAutoRunId);
        }
        finally
        {
            objActive.TryRemove(aKey, out _);
        }
    }
}
