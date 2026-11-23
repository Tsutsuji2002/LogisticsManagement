using Logistics.Server.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

// Organization
using Logistics.Server.Domain.Entities.Organization;
// Identity
using Logistics.Server.Domain.Entities.Identity;
// Configuration
using Logistics.Server.Domain.Entities.Configuration;
// CRM
using Logistics.Server.Domain.Entities.Crm;
// Catalog
using Logistics.Server.Domain.Entities.Catalog;
// Sales
using Logistics.Server.Domain.Entities.Sales;
// Shipping
using Logistics.Server.Domain.Entities.Shipping;
// Carrier
using Logistics.Server.Domain.Entities.Carrier;
// Warehouse
using Logistics.Server.Domain.Entities.Warehouse;
// Inventory
using Logistics.Server.Domain.Entities.Inventory;
// Finance
using Logistics.Server.Domain.Entities.Finance;
// Notification
using Logistics.Server.Domain.Entities.Notification;
// Integration
using Logistics.Server.Domain.Entities.Integration;
// Audit
using Logistics.Server.Domain.Entities.Audit;

namespace Logistics.Server.Infrastructure.Persistence;

/// <summary>
/// Application database context — 90 DbSets across 14 schemas.
/// </summary>
public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // ── Organization (5) ──
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Domain.Entities.Organization.Warehouse> Warehouses => Set<Domain.Entities.Organization.Warehouse>();

    // ── Identity (9) ──
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserBranchAccess> UserBranchAccesses => Set<UserBranchAccess>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();

    // ── Configuration (8) ──
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();

    // ── CRM (6) ──
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<CustomerContract> CustomerContracts => Set<CustomerContract>();
    public DbSet<CustomerPricingRule> CustomerPricingRules => Set<CustomerPricingRule>();

    // ── Catalog (2) ──
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();

    // ── Sales (4) ──
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderAddress> OrderAddresses => Set<OrderAddress>();
    public DbSet<OrderPackage> OrderPackages => Set<OrderPackage>();

    // ── Shipping (10) ──
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentPackage> ShipmentPackages => Set<ShipmentPackage>();
    public DbSet<ShipmentAddress> ShipmentAddresses => Set<ShipmentAddress>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistories => Set<ShipmentStatusHistory>();
    public DbSet<ShipmentAssignment> ShipmentAssignments => Set<ShipmentAssignment>();
    public DbSet<PickupRequest> PickupRequests => Set<PickupRequest>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<ProofOfDelivery> ProofOfDeliveries => Set<ProofOfDelivery>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ShipmentRoute> ShipmentRoutes => Set<ShipmentRoute>();

    // ── Carrier (8) ──
    public DbSet<Domain.Entities.Carrier.Carrier> Carriers => Set<Domain.Entities.Carrier.Carrier>();
    public DbSet<CarrierService> CarrierServices => Set<CarrierService>();
    public DbSet<CarrierConfiguration> CarrierConfigurations => Set<CarrierConfiguration>();
    public DbSet<CarrierTrackingMapping> CarrierTrackingMappings => Set<CarrierTrackingMapping>();
    public DbSet<CarrierTrackingEvent> CarrierTrackingEvents => Set<CarrierTrackingEvent>();
    public DbSet<CarrierWebhookLog> CarrierWebhookLogs => Set<CarrierWebhookLog>();
    public DbSet<CarrierApiLog> CarrierApiLogs => Set<CarrierApiLog>();
    public DbSet<CarrierSyncJob> CarrierSyncJobs => Set<CarrierSyncJob>();

    // ── Warehouse (8) ──
    public DbSet<WarehouseZone> WarehouseZones => Set<WarehouseZone>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<WarehouseReceipt> WarehouseReceipts => Set<WarehouseReceipt>();
    public DbSet<WarehouseReceiptItem> WarehouseReceiptItems => Set<WarehouseReceiptItem>();
    public DbSet<WarehouseDispatch> WarehouseDispatches => Set<WarehouseDispatch>();
    public DbSet<WarehouseDispatchItem> WarehouseDispatchItems => Set<WarehouseDispatchItem>();
    public DbSet<WarehouseDispatchPackage> WarehouseDispatchPackages => Set<WarehouseDispatchPackage>();
    public DbSet<ParcelHandlingEvent> ParcelHandlingEvents => Set<ParcelHandlingEvent>();

    // ── Inventory (9) ──
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryLot> InventoryLots => Set<InventoryLot>();
    public DbSet<InventorySerialNumber> InventorySerialNumbers => Set<InventorySerialNumber>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryTransactionItem> InventoryTransactionItems => Set<InventoryTransactionItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();

    // ── Finance (12) ──
    public DbSet<ShippingRateCard> ShippingRateCards => Set<ShippingRateCard>();
    public DbSet<ShippingRateRule> ShippingRateRules => Set<ShippingRateRule>();
    public DbSet<ShipmentCharge> ShipmentCharges => Set<ShipmentCharge>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<CODCollection> CODCollections => Set<CODCollection>();
    public DbSet<CODRemittance> CODRemittances => Set<CODRemittance>();
    public DbSet<CODRemittanceItem> CODRemittanceItems => Set<CODRemittanceItem>();
    public DbSet<CustomerBalance> CustomerBalances => Set<CustomerBalance>();
    public DbSet<FinancialLedgerEntry> FinancialLedgerEntries => Set<FinancialLedgerEntry>();

    // ── Notification (5) ──
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Domain.Entities.Notification.Notification> Notifications => Set<Domain.Entities.Notification.Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ScheduledReport> ScheduledReports => Set<ScheduledReport>();

    // ── Integration (2) ──
    public DbSet<BackgroundJobLog> BackgroundJobLogs => Set<BackgroundJobLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // ── Audit (2) ──
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Create all 14 schemas
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = _currentUserService?.Username ?? "System";

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.PublicId == Guid.Empty)
                        entry.Entity.PublicId = Guid.NewGuid();
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy ??= currentUser;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseHistoricalEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
