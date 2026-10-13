using System.Collections.Concurrent;
using Chatur.Core.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chatur.Core.AutoRun;

/// <summary>
/// The real <see cref="IAutoRunLauncher"/>: one background task per run, each in its own service scope, so
/// a run goes on after the screen that started it is closed. Registered as a singleton.
/// </summary>
public sealed class BackgroundAutoRunLauncher : IAutoRunLauncher
{
    private readonly ConcurrentDictionary<int, bool> objActive = new();
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
    public bool Launch(int aAutoRunId)
    {
        if (!objActive.TryAdd(aAutoRunId, true))
        {
            return false;
        }

        _ = Task.Run(() => CarryOnAsync(aAutoRunId));
        return true;
    }

    private async Task CarryOnAsync(int aAutoRunId)
    {
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
            objActive.TryRemove(aAutoRunId, out _);
        }
    }
}
