using Logistics.Server.Application.Modules.Notification.Services;
using Logistics.Server.Integrations.Carriers;
using Logistics.Server.Integrations.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Server.Integrations;

/// <summary>
/// Registers carrier adapters, email providers, and external API integration services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddHttpClient();
        services.AddHttpClient("SendGrid");

        if (configuration != null)
        {
            services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        }
        else
        {
            services.AddOptions<EmailSettings>();
        }

        services.AddScoped<ICarrierAdapter, GhnCarrierAdapter>();
        services.AddScoped<ICarrierAdapter, GhtkCarrierAdapter>();
        services.AddScoped<ICarrierAdapter, ViettelPostCarrierAdapter>();
        services.AddScoped<ICarrierAdapterFactory, CarrierAdapterFactory>();

        services.AddScoped<IEmailProvider, ConfigurableEmailProvider>();

        return services;
    }
}
