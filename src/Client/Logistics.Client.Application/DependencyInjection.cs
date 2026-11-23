using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Presenters;
using Logistics.Client.Application.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Client.Application;

/// <summary>
/// Dependency injection extensions for the client application layer.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers client application presenters, workflows, and use cases.
    /// </summary>
    public static IServiceCollection AddClientApplication(this IServiceCollection services)
    {
        // Factory for creating presenters bound to a specific view instance (MVP pattern)
        services.AddTransient<Func<IDashboardView, DashboardPresenter>>(sp =>
            view => new DashboardPresenter(view, sp.GetRequiredService<IApiClient>()));

        return services;
    }
}
