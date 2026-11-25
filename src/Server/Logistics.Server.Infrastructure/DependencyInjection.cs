using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Catalog.Services;
using Logistics.Server.Application.Modules.Crm.Services;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Application.Modules.Organization.Services;
using Logistics.Server.Application.Modules.Sales.Services;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Server.Infrastructure.Persistence.Repositories;
using Logistics.Server.Infrastructure.Services;
using Logistics.Server.Infrastructure.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Logistics.Server.Infrastructure;

/// <summary>
/// Registers infrastructure layer services, database persistence, security providers, and external adapters.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentBranchContext, CurrentBranchContext>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ICacheService, InMemoryCacheService>();

        // Security & Identity Infrastructure
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserSessionService, UserSessionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IIdentityDataSeeder, IdentityDataSeeder>();
        services.AddScoped<IAuditService, AuditService>();

        // Domain Module Services
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IWarehouseService, WarehouseService>();

        // CRM Module Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerContactService, CustomerContactService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<ICustomerContractService, CustomerContractService>();
        services.AddScoped<ICustomerPricingRuleService, CustomerPricingRuleService>();

        // Catalog Module Services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
        services.AddScoped<ILogisticsServiceCatalogService, LogisticsServiceCatalogService>();
        services.AddScoped<ICatalogReadService, CatalogReadService>();

        // Sales Module Services
        services.AddScoped<INumberSequenceService, NumberSequenceService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderItemService, OrderItemService>();
        services.AddScoped<IOrderPackageService, OrderPackageService>();
        services.AddScoped<IOrderAddressService, OrderAddressService>();

        // Shipping Module Services
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IShipmentPackageService, ShipmentPackageService>();
        services.AddScoped<IShipmentAddressService, ShipmentAddressService>();
        services.AddScoped<IPickupRequestService, PickupRequestService>();
        services.AddScoped<IShipmentAssignmentService, ShipmentAssignmentService>();
        services.AddScoped<IDeliveryAttemptService, DeliveryAttemptService>();
        services.AddScoped<IProofOfDeliveryService, ProofOfDeliveryService>();
        services.AddScoped<IReturnRequestService, ReturnRequestService>();
        services.AddScoped<IShipmentRouteService, ShipmentRouteService>();

        // Carrier Module Services
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierManagementService, CarrierManagementService>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierServiceManagementService, CarrierServiceManagementService>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierConfigurationService, CarrierConfigurationServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierTrackingMappingService, CarrierTrackingMappingServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierIntegrationService, CarrierIntegrationServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierWebhookService, CarrierWebhookServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierApiLogService, CarrierApiLogServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierSyncJobService, CarrierSyncJobServiceImpl>();
        services.AddScoped<Application.Modules.Carrier.Services.ICarrierTrackingSyncOrchestrator, CarrierTrackingSyncOrchestrator>();

        // Warehouse & Inventory Module Services
        services.AddScoped<Application.Modules.Warehouse.Services.IWarehouseManagementService, WarehouseManagementService>();
        services.AddScoped<Application.Modules.Warehouse.Services.IInventoryService, InventoryService>();
        services.AddScoped<Application.Modules.Warehouse.Services.IParcelHandlingService, ParcelHandlingService>();

        // Finance & Billing Module Services
        services.AddScoped<Application.Modules.Finance.Services.IFinancialLedgerService, FinancialLedgerService>();
        services.AddScoped<Application.Modules.Finance.Services.IRateManagementService, RateManagementService>();
        services.AddScoped<Application.Modules.Finance.Services.IShippingFeeCalculationService, ShippingFeeCalculationService>();
        services.AddScoped<Application.Modules.Finance.Services.IShipmentChargeService, ShipmentChargeService>();
        services.AddScoped<Application.Modules.Finance.Services.IInvoiceService, InvoiceService>();
        services.AddScoped<Application.Modules.Finance.Services.IPaymentService, PaymentService>();
        services.AddScoped<Application.Modules.Finance.Services.ICustomerBalanceService, CustomerBalanceService>();

        // COD Module Services
        services.AddScoped<Application.Modules.Cod.Services.ICodCollectionService, CodCollectionService>();
        services.AddScoped<Application.Modules.Cod.Services.ICodEmployeeService, CodEmployeeService>();
        services.AddScoped<Application.Modules.Cod.Services.ICodRemittanceService, CodRemittanceService>();
        services.AddScoped<Application.Modules.Cod.Services.ICodReconciliationService, CodReconciliationService>();

        // Notification & Background Processing Services
        services.AddSingleton<Application.Modules.Notification.Services.INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<Application.Modules.Notification.Services.INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<Application.Modules.Notification.Services.INotificationService, NotificationService>();
        services.AddScoped<Application.Modules.Notification.Services.INotificationDeliveryService, NotificationDeliveryService>();

        services.AddScoped<IOutboxService, OutboxService>();
        services.AddScoped<Application.Modules.BackgroundProcessing.Services.IOutboxProcessorService, OutboxProcessorService>();
        services.AddScoped<Application.Modules.BackgroundProcessing.Services.IBackgroundJobService, BackgroundJobService>();
        services.AddScoped<Application.Modules.BackgroundProcessing.Services.IReportExecutionEngine, ReportingExecutionEngine>();
        services.AddScoped<Application.Modules.BackgroundProcessing.Services.IScheduledReportService, ScheduledReportService>();
        services.AddScoped<Application.Modules.BackgroundProcessing.Services.IScheduledMaintenanceService, ScheduledMaintenanceService>();

        // Reporting & Dashboard Services
        services.AddScoped<Application.Modules.Reporting.Services.IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<Application.Modules.Reporting.Services.IOperationalReportService, OperationalReportService>();
        services.AddScoped<Application.Modules.Reporting.Services.IFinancialReportService, FinancialReportService>();
        services.AddScoped<Application.Modules.Reporting.Services.IReportExportService, ReportExportService>();

        // Outbox Integration Event Handlers
        services.AddScoped<IOutboxEventHandler, OrderConfirmedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, OrderCancelledOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, ShipmentCreatedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, ShipmentStatusChangedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, DeliveryCompletedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, DeliveryFailedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, ReturnInitiatedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, CarrierBookingOutcomeOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, CarrierSyncFailedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, InventoryReceiptCompletedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, InvoiceIssuedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, PaymentConfirmedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, CodCollectedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, CodRemittedOutboxHandler>();
        services.AddScoped<IOutboxEventHandler, CodDiscrepancyDetectedOutboxHandler>();

        return services;
    }
}
