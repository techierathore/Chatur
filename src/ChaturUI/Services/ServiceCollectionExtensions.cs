using ChaturUI.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace ChaturUI.Services;

/// <summary>
/// Registers <c>ChaturUI</c>'s own services: <see cref="ThemeInterop"/>, <see cref="FileDownloadInterop"/>
/// and <see cref="WorkbenchState"/>. Every head calls this alongside
/// <c>Chatur.Core.ServiceCollectionExtensions.AddChaturCore</c>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds every <c>ChaturUI</c> service.
    /// </summary>
    /// <param name="aServices">The service collection to add to.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddChaturUI(this IServiceCollection aServices)
    {
        aServices.AddScoped<ThemeInterop>();
        aServices.AddScoped<FileDownloadInterop>();
        aServices.AddScoped<BrowserInterop>();
        aServices.AddScoped<WorkbenchState>();
        return aServices;
    }
}
