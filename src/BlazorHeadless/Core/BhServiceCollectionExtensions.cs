using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlazorHeadless;

/// <summary>
/// Service-collection extensions for registering BlazorHeadless's runtime services.
/// </summary>
public static class BhServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services used by BlazorHeadless components: the JS runtime
    /// wrapper (<see cref="BhInterop"/>), the live-region announcer
    /// (<see cref="IBhAnnouncer"/>) and the toast queue (<see cref="BhToaster"/>).
    /// Safe to call more than once.
    ///
    /// <para>
    /// Register once during application startup (in both the server and the
    /// client project of a Blazor Web App that uses WebAssembly rendering):
    /// <code>builder.Services.AddBlazorHeadless();</code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddBlazorHeadless(this IServiceCollection services)
    {
        services.TryAddScoped<BhInterop>();
        services.TryAddScoped<IBhAnnouncer, BhAnnouncer>();
        services.TryAddScoped<BhToaster>();
        return services;
    }
}
