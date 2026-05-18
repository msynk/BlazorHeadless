using Microsoft.Extensions.DependencyInjection;

namespace HeadlessUI.Blazor;

/// <summary>
/// Service-collection extensions for registering HeadlessUI.Blazor's runtime services.
/// </summary>
public static class HeadlessUIServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required by HeadlessUI.Blazor components that need
    /// JS interop (currently Dialog; later Popover, Combobox positioning, etc.).
    /// Components that don't need JS work without this call.
    ///
    /// <para>
    /// Register once during application startup:
    /// <code>builder.Services.AddHeadlessUI();</code>
    /// </para>
    /// </summary>
    public static IServiceCollection AddHeadlessUI(this IServiceCollection services)
    {
        services.AddScoped<HeadlessUIInterop>();
        return services;
    }
}
