using Logistics.Client.Application;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.WinForms.Controls.Carrier;
using Logistics.Client.WinForms.Controls.Catalog;
using Logistics.Client.WinForms.Controls.COD;
using Logistics.Client.WinForms.Controls.CRM;
using Logistics.Client.WinForms.Controls.Dashboard;
using Logistics.Client.WinForms.Controls.Finance;
using Logistics.Client.WinForms.Controls.Inventory;
using Logistics.Client.WinForms.Controls.Notifications;
using Logistics.Client.WinForms.Controls.Organization;
using Logistics.Client.WinForms.Controls.Reports;
using Logistics.Client.WinForms.Controls.Sales;
using Logistics.Client.WinForms.Controls.Settings;
using Logistics.Client.WinForms.Controls.Shipping;
using Logistics.Client.WinForms.Controls.Warehouse;
using Logistics.Client.WinForms.Forms.Authentication;
using Logistics.Client.WinForms.Security;
using Logistics.Client.WinForms.Services;
using Logistics.Client.WinForms.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Logistics.Client.WinForms;

internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogisticsManagement",
            "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDir, "desktop-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        try
        {
            ApplicationConfiguration.Initialize();
            AppTheme.InitializeAntdTheme();

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            // Force settings resolution from appsettings*.json & saved user overrides before HttpClient use
            _ = ServiceProvider.GetRequiredService<IClientSettingsService>();

            var authService = ServiceProvider.GetRequiredService<IAuthenticationService>();
            authService.TryRestoreSessionAsync().GetAwaiter().GetResult();

            while (true)
            {
                if (!authService.IsAuthenticated)
                {
                    using var loginForm = ServiceProvider.GetRequiredService<LoginForm>();
                    if (loginForm.ShowDialog() != DialogResult.OK || !authService.IsAuthenticated)
                        break;
                }

                using var uiScope = ServiceProvider.CreateScope();
                using var mainForm = uiScope.ServiceProvider.GetRequiredService<MainForm>();
                System.Windows.Forms.Application.Run(mainForm);

                if (!mainForm.ReloginRequested)
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled desktop client exception.");
            MessageBox.Show(
                $"Đã xảy ra lỗi nghiêm trọng khi chạy ứng dụng:\n\n{ex.Message}",
                "Logistics Management Desktop",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: false);
        });

        // Windows DPAPI CurrentUser token store must be registered BEFORE AddClientInfrastructure
        services.AddSingleton<ISecureTokenStore, DpapiSecureTokenStore>();

        services.AddClientInfrastructure(new ApiClientOptions
        {
            BaseUrl = "http://localhost:5289",
            TimeoutSeconds = 30
        });

        services.AddClientApplication();
        services.AddSingleton<INavigationService, NavigationService>();

        // Forms & Module Workspaces (Scoped per authenticated MainForm session so state resets cleanly on relogin)
        services.AddTransient<LoginForm>();
        services.AddScoped<MainForm>();
        services.AddScoped<ExecutiveDashboardControl>();
        services.AddScoped<OrganizationWorkspaceControl>();
        services.AddScoped<CrmWorkspaceControl>();
        services.AddScoped<CatalogWorkspaceControl>();
        services.AddScoped<SalesOrdersWorkspaceControl>();
        services.AddScoped<ShippingWorkspaceControl>();
        services.AddScoped<CarrierWorkspaceControl>();
        services.AddScoped<WarehouseWorkspaceControl>();
        services.AddScoped<InventoryWorkspaceControl>();
        services.AddScoped<FinanceWorkspaceControl>();
        services.AddScoped<CodWorkspaceControl>();
        services.AddScoped<NotificationsWorkspaceControl>();
        services.AddScoped<ReportsWorkspaceControl>();
        services.AddScoped<SettingsWorkspaceControl>();
    }
}
