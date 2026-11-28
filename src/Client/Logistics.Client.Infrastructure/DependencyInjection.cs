using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Api;
using Logistics.Client.Infrastructure.Configuration;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.Infrastructure.Security;
using Logistics.Client.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Logistics.Client.Infrastructure;

/// <summary>
/// Registers client infrastructure services into the DI container.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddClientInfrastructure(this IServiceCollection services, ApiClientOptions? options = null)
    {
        var clientOptions = options ?? new ApiClientOptions();

        services.AddSingleton(clientOptions);
        services.AddSingleton<IClientSettingsService>(sp => new ClientSettingsService(clientOptions));

        // Token storage defaults to InMemory for unit tests; WinForms overrides with DPAPI CurrentUser store
        services.TryAddSingleton<ISecureTokenStore, InMemorySecureTokenStore>();
        services.AddSingleton<IBranchContextService, BranchContextService>();
        services.AddSingleton<IAuthenticationService, ClientAuthenticationService>();
        services.AddSingleton<IAuthorizationService, ClientAuthorizationService>();

        services.AddTransient<AuthenticationDelegatingHandler>();

        // Separate named bare HttpClient for auth login/refresh calls to avoid handler re-entrancy
        services.AddHttpClient("LogisticsAuth", (sp, client) =>
        {
            var opts = sp.GetRequiredService<ApiClientOptions>();
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 5, 300));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddHttpClient<IApiClient, ApiClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<ApiClientOptions>();
            if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
                client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 5, 300));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddHttpMessageHandler<AuthenticationDelegatingHandler>();

        // Typed Domain API Clients
        services.AddTransient<IHealthApiClient, HealthApiClient>();
        services.AddTransient<IDevTestDataApiClient, DevTestDataApiClient>();
        services.AddTransient<IAuthSessionApiClient, AuthSessionApiClient>();
        services.AddTransient<IOrganizationApiClient, OrganizationApiClient>();
        services.AddTransient<IIdentityAdminApiClient, IdentityAdminApiClient>();
        services.AddTransient<ICrmApiClient, CrmApiClient>();
        services.AddTransient<ICatalogApiClient, CatalogApiClient>();
        services.AddTransient<ISalesApiClient, SalesApiClient>();
        services.AddTransient<IShippingApiClient, ShippingApiClient>();
        services.AddTransient<ICarrierApiClient, CarrierApiClient>();
        services.AddTransient<IWarehouseApiClient, WarehouseApiClient>();
        services.AddTransient<IInventoryApiClient, InventoryApiClient>();
        services.AddTransient<IFinanceApiClient, FinanceApiClient>();
        services.AddTransient<ICodApiClient, CodApiClient>();
        services.AddTransient<INotificationApiClient, NotificationApiClient>();
        services.AddTransient<IReportingApiClient, ReportingApiClient>();

        return services;
    }
}
