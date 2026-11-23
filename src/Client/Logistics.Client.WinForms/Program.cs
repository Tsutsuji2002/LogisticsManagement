using Logistics.Client.Application;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.WinForms.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Client.WinForms;

/// <summary>
/// Entry point for the Windows Forms Desktop Client.
/// </summary>
internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var mainForm = ServiceProvider.GetRequiredService<MainForm>();
            System.Windows.Forms.Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An unhandled exception occurred during application startup:\n\n{ex}",
                "Logistics Client Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // 1. Client Infrastructure layer (IApiClient, HttpClient, auth, etc.)
        var apiOptions = new ApiClientOptions
        {
            BaseUrl = "http://localhost:5000",
            TimeoutSeconds = 30
        };
        services.AddClientInfrastructure(apiOptions);

        // 2. Client Application layer (presenters, use cases)
        services.AddClientApplication();

        // 3. Client UI navigation service
        services.AddSingleton<INavigationService, NavigationService>();

        // 4. Windows Forms views
        services.AddTransient<MainForm>();
    }
}
