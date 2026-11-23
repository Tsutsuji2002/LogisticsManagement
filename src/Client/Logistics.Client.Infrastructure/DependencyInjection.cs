using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri(clientOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(clientOptions.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IAuthenticationService, ClientAuthenticationService>();

        return services;
    }
}
