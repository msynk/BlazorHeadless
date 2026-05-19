using Microsoft.Extensions.DependencyInjection;

namespace BlazorHeadless;

/// <summary>
/// Service-collection extensions for registering BlazorHeadless's runtime services.
/// </summary>
public static class BhServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required by BlazorHeadless components that need
    /// JS interop (currently Dialog; later Popover, Combobox positioning, etc.).
    /// Components that don't need JS work without this call.
    ///
    /// <para>
    /// Register once during application startup:
    /// <code>builder.Services.AddBlazorHeadless();</code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddBlazorHeadless(this IServiceCollection services)
    {
        services.AddScoped<BhInterop>();
        return services;
    }
}
