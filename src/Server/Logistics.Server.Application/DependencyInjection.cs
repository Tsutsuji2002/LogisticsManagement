using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Server.Application;

/// <summary>
/// Registers application layer services and FluentValidation validators.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
