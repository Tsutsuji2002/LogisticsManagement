using Logistics.Server.Integrations.Carriers;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Server.Integrations;

/// <summary>
/// Registers carrier adapters and external API integration services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddScoped<ICarrierAdapter, GhnCarrierAdapter>();
        services.AddScoped<ICarrierAdapter, GhtkCarrierAdapter>();
        services.AddScoped<ICarrierAdapter, ViettelPostCarrierAdapter>();
        services.AddScoped<ICarrierAdapterFactory, CarrierAdapterFactory>();

        return services;
    }
}
