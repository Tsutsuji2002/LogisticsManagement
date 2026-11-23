using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Identity.Services;
using Logistics.Server.Domain.Entities.Identity;
using Logistics.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Idempotent database seeder for reference permissions, system roles, default role-permission matrix,
/// and optional administrator account bootstrapping via configuration/environment variables.
/// </summary>
public class IdentityDataSeeder : IIdentityDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityDataSeeder> _logger;

    public IdentityDataSeeder(
        ApplicationDbContext context,
        IPasswordHasherService passwordHasher,
        IConfiguration configuration,
        ILogger<IdentityDataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedRolePermissionsAsync(cancellationToken);
        await SeedBootstrapAdminAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await _context.Permissions
            .Select(p => p.Code)
            .ToHashSetAsync(cancellationToken);

        var allDefinitions = AppPermissions.GetAllPermissions();
        var toAdd = new List<Permission>();

        foreach (var (module, code, name, description) in allDefinitions)
        {
            if (!existingCodes.Contains(code))
            {
                toAdd.Add(new Permission
                {
                    Module = module,
                    Code = code,
                    Name = name,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await _context.Permissions.AddRangeAsync(toAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} new permission records.", toAdd.Count);
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var existingRoles = await _context.Roles
            .Select(r => r.Code)
            .ToHashSetAsync(cancellationToken);

        var allRoles = AppRoles.GetAllRoles();
        var toAdd = new List<Role>();

        foreach (var (code, name, description, isSystem) in allRoles)
        {
            if (!existingRoles.Contains(code))
            {
                toAdd.Add(new Role
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    IsSystem = isSystem,
                    IsActive = true,
                    CreatedBy = "SystemSeed"
                });
            }
        }

        if (toAdd.Count > 0)
        {
            await _context.Roles.AddRangeAsync(toAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} new system roles.", toAdd.Count);
        }
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .Include(r => r.RolePermissions)
            .ToListAsync(cancellationToken);

        var permissions = await _context.Permissions
            .ToDictionaryAsync(p => p.Code, cancellationToken);

        var roleMap = roles.ToDictionary(r => r.Code);

        // Define default role assignments
        var matrix = new Dictionary<string, IEnumerable<string>>
        {
            [AppRoles.SuperAdmin] = permissions.Keys,

            [AppRoles.BranchManager] = new[]
            {
                AppPermissions.Organization.CompanyRead,
                AppPermissions.Organization.CompaniesRead,
                AppPermissions.Organization.BranchesRead,
                AppPermissions.Organization.BranchesUpdate,
                AppPermissions.Organization.DepartmentsRead,
                AppPermissions.Organization.DepartmentsCreate,
                AppPermissions.Organization.DepartmentsUpdate,
                AppPermissions.Organization.DepartmentsManage,
                AppPermissions.Organization.EmployeesRead,
                AppPermissions.Organization.EmployeesManage,
                AppPermissions.Organization.WarehousesRead,
                AppPermissions.Organization.OverviewRead,
                AppPermissions.Employees.Read,
                AppPermissions.Employees.Create,
                AppPermissions.Employees.Update,
                AppPermissions.Employees.StatusManage,
                AppPermissions.Employees.AccountRead,
                AppPermissions.Crm.CustomersRead,
                AppPermissions.Crm.CustomersCreate,
                AppPermissions.Crm.CustomersUpdate,
                AppPermissions.Crm.CustomersStatusManage,
                AppPermissions.Crm.CustomersOverviewRead,
                AppPermissions.Crm.ContactsRead,
                AppPermissions.Crm.ContactsCreate,
                AppPermissions.Crm.ContactsUpdate,
                AppPermissions.Crm.ContactsManage,
                AppPermissions.Crm.AddressesRead,
                AppPermissions.Crm.AddressesCreate,
                AppPermissions.Crm.AddressesUpdate,
                AppPermissions.Crm.AddressesManage,
                AppPermissions.Crm.ContractsRead,
                AppPermissions.Crm.ContractsCreate,
                AppPermissions.Crm.ContractsUpdate,
                AppPermissions.Crm.ContractsManage,
                AppPermissions.Crm.PricingRulesRead,
                AppPermissions.Crm.PricingRulesCreate,
                AppPermissions.Crm.PricingRulesUpdate,
                AppPermissions.Crm.PricingRulesManage,
                AppPermissions.Catalog.CategoriesRead,
                AppPermissions.Catalog.CategoriesCreate,
                AppPermissions.Catalog.CategoriesUpdate,
                AppPermissions.Catalog.CategoriesManageStatus,
                AppPermissions.Catalog.ProductsRead,
                AppPermissions.Catalog.ProductsCreate,
                AppPermissions.Catalog.ProductsUpdate,
                AppPermissions.Catalog.ProductsManageStatus,
                AppPermissions.Catalog.UnitsRead,
                AppPermissions.Catalog.ServicesRead,
                AppPermissions.Sales.OrdersRead,
                AppPermissions.Sales.OrdersCreate,
                AppPermissions.Sales.OrdersUpdate,
                AppPermissions.Sales.OrdersCancel,
                AppPermissions.Shipping.ShipmentsRead,
                AppPermissions.Shipping.ShipmentsCreate,
                AppPermissions.Shipping.ShipmentsUpdate,
                AppPermissions.Shipping.ShipmentsAssign,
                AppPermissions.Shipping.ShipmentsCancel,
                AppPermissions.Shipping.ShipmentsManageStatus,
                AppPermissions.Shipping.TrackingRead,
                AppPermissions.Shipping.PickupsRead,
                AppPermissions.Shipping.PickupsCreate,
                AppPermissions.Shipping.PickupsManage,
                AppPermissions.Shipping.ReturnsRead,
                AppPermissions.Shipping.ReturnsManage,
                AppPermissions.Shipping.RoutesRead,
                AppPermissions.Shipping.RoutesManage,
                AppPermissions.Carrier.CarriersRead,
                AppPermissions.Carrier.CarriersManage,
                AppPermissions.Warehouse.OperationsRead,
                AppPermissions.Warehouse.ReceiptsManage,
                AppPermissions.Warehouse.DispatchesManage,
                AppPermissions.Warehouse.SortingManage,
                AppPermissions.Warehouse.WarehousesRead,
                AppPermissions.Warehouse.WarehousesManage,
                AppPermissions.Warehouse.ZonesManage,
                AppPermissions.Warehouse.LocationsManage,
                AppPermissions.Warehouse.ParcelHandlingRead,
                AppPermissions.Warehouse.ParcelHandlingManage,
                AppPermissions.Inventory.InventoryRead,
                AppPermissions.Inventory.InventoryManage,
                AppPermissions.Inventory.AdjustmentsManage,
                AppPermissions.Inventory.TransfersManage,
                AppPermissions.Inventory.ReceiptsManage,
                AppPermissions.Inventory.IssuesManage,
                AppPermissions.Inventory.ReservationsManage,
                AppPermissions.Inventory.LotsManage,
                AppPermissions.Inventory.SerialNumbersManage,
                AppPermissions.Notification.NotificationsRead
            },

            [AppRoles.OperationsManager] = new[]
            {
                AppPermissions.Organization.BranchesRead,
                AppPermissions.Organization.WarehousesRead,
                AppPermissions.Organization.OverviewRead,
                AppPermissions.Employees.Read,
                AppPermissions.Shipping.ShipmentsRead,
                AppPermissions.Shipping.ShipmentsCreate,
                AppPermissions.Shipping.ShipmentsUpdate,
                AppPermissions.Shipping.ShipmentsAssign,
                AppPermissions.Shipping.ShipmentsDeliver,
                AppPermissions.Shipping.ShipmentsCancel,
                AppPermissions.Shipping.ShipmentsManageStatus,
                AppPermissions.Shipping.TrackingRead,
                AppPermissions.Shipping.PickupsRead,
                AppPermissions.Shipping.PickupsCreate,
                AppPermissions.Shipping.PickupsManage,
                AppPermissions.Shipping.ReturnsRead,
                AppPermissions.Shipping.ReturnsManage,
                AppPermissions.Shipping.RoutesRead,
                AppPermissions.Shipping.RoutesManage,
                AppPermissions.Carrier.CarriersRead,
                AppPermissions.Carrier.CarriersManage,
                AppPermissions.Warehouse.OperationsRead,
                AppPermissions.Warehouse.ReceiptsManage,
                AppPermissions.Warehouse.DispatchesManage,
                AppPermissions.Warehouse.SortingManage,
                AppPermissions.Warehouse.WarehousesRead,
                AppPermissions.Warehouse.WarehousesManage,
                AppPermissions.Warehouse.ZonesManage,
                AppPermissions.Warehouse.LocationsManage,
                AppPermissions.Warehouse.ParcelHandlingRead,
                AppPermissions.Warehouse.ParcelHandlingManage,
                AppPermissions.Inventory.InventoryRead,
                AppPermissions.Notification.NotificationsRead
            },

            [AppRoles.WarehouseStaff] = new[]
            {
                AppPermissions.Organization.WarehousesRead,
                AppPermissions.Warehouse.OperationsRead,
                AppPermissions.Warehouse.ReceiptsManage,
                AppPermissions.Warehouse.DispatchesManage,
                AppPermissions.Warehouse.SortingManage,
                AppPermissions.Warehouse.WarehousesRead,
                AppPermissions.Warehouse.ZonesManage,
                AppPermissions.Warehouse.LocationsManage,
                AppPermissions.Warehouse.ParcelHandlingRead,
                AppPermissions.Warehouse.ParcelHandlingManage,
                AppPermissions.Inventory.InventoryRead,
                AppPermissions.Inventory.InventoryManage,
                AppPermissions.Inventory.AdjustmentsManage,
                AppPermissions.Inventory.TransfersManage,
                AppPermissions.Inventory.ReceiptsManage,
                AppPermissions.Inventory.IssuesManage,
                AppPermissions.Inventory.ReservationsManage,
                AppPermissions.Inventory.LotsManage,
                AppPermissions.Inventory.SerialNumbersManage,
                AppPermissions.Catalog.CategoriesRead,
                AppPermissions.Catalog.ProductsRead,
                AppPermissions.Catalog.UnitsRead,
                AppPermissions.Shipping.TrackingRead
            },

            [AppRoles.CustomerService] = new[]
            {
                AppPermissions.Organization.BranchesRead,
                AppPermissions.Employees.Read,
                AppPermissions.Crm.CustomersRead,
                AppPermissions.Crm.CustomersCreate,
                AppPermissions.Crm.CustomersUpdate,
                AppPermissions.Crm.CustomersOverviewRead,
                AppPermissions.Crm.ContactsRead,
                AppPermissions.Crm.ContactsCreate,
                AppPermissions.Crm.ContactsUpdate,
                AppPermissions.Crm.ContactsManage,
                AppPermissions.Crm.AddressesRead,
                AppPermissions.Crm.AddressesCreate,
                AppPermissions.Crm.AddressesUpdate,
                AppPermissions.Crm.AddressesManage,
                AppPermissions.Crm.ContractsRead,
                AppPermissions.Crm.PricingRulesRead,
                AppPermissions.Sales.OrdersRead,
                AppPermissions.Sales.OrdersCreate,
                AppPermissions.Sales.OrdersUpdate,
                AppPermissions.Sales.OrdersCancel,
                AppPermissions.Shipping.ShipmentsRead,
                AppPermissions.Shipping.TrackingRead,
                AppPermissions.Shipping.PickupsRead,
                AppPermissions.Shipping.PickupsCreate,
                AppPermissions.Shipping.ReturnsRead,
                AppPermissions.Catalog.CategoriesRead,
                AppPermissions.Catalog.ProductsRead,
                AppPermissions.Catalog.UnitsRead,
                AppPermissions.Catalog.ServicesRead,
                AppPermissions.Notification.NotificationsRead
            },

            [AppRoles.FinanceStaff] = new[]
            {
                AppPermissions.Organization.BranchesRead,
                AppPermissions.Employees.Read,
                AppPermissions.Finance.RatesRead,
                AppPermissions.Finance.RatesManage,
                AppPermissions.Finance.ChargesRead,
                AppPermissions.Finance.ChargesCalculate,
                AppPermissions.Finance.ChargesAdjust,
                AppPermissions.Finance.InvoicesRead,
                AppPermissions.Finance.InvoicesCreate,
                AppPermissions.Finance.InvoicesIssue,
                AppPermissions.Finance.InvoicesCancel,
                AppPermissions.Finance.PaymentsRead,
                AppPermissions.Finance.PaymentsManage,
                AppPermissions.Finance.PaymentsAllocate,
                AppPermissions.Finance.PaymentsReverse,
                AppPermissions.Finance.BalancesRead,
                AppPermissions.Finance.LedgerRead,
                AppPermissions.Finance.BalancesReconcile,
                AppPermissions.Finance.CodManage,
                AppPermissions.Crm.CustomersRead,
                AppPermissions.Crm.CustomersOverviewRead,
                AppPermissions.Crm.ContractsRead,
                AppPermissions.Crm.ContractsCreate,
                AppPermissions.Crm.ContractsUpdate,
                AppPermissions.Crm.ContractsManage,
                AppPermissions.Crm.PricingRulesRead,
                AppPermissions.Crm.PricingRulesCreate,
                AppPermissions.Crm.PricingRulesUpdate,
                AppPermissions.Crm.PricingRulesManage,
                AppPermissions.Catalog.CategoriesRead,
                AppPermissions.Catalog.ProductsRead,
                AppPermissions.Catalog.UnitsRead,
                AppPermissions.Catalog.ServicesRead,
                AppPermissions.Sales.OrdersRead
            },

            [AppRoles.ReadOnly] = new[]
            {
                AppPermissions.Organization.CompanyRead,
                AppPermissions.Organization.CompaniesRead,
                AppPermissions.Organization.BranchesRead,
                AppPermissions.Organization.DepartmentsRead,
                AppPermissions.Organization.EmployeesRead,
                AppPermissions.Organization.WarehousesRead,
                AppPermissions.Organization.OverviewRead,
                AppPermissions.Employees.Read,
                AppPermissions.Crm.CustomersRead,
                AppPermissions.Crm.CustomersOverviewRead,
                AppPermissions.Crm.ContactsRead,
                AppPermissions.Crm.AddressesRead,
                AppPermissions.Crm.ContractsRead,
                AppPermissions.Crm.PricingRulesRead,
                AppPermissions.Catalog.CategoriesRead,
                AppPermissions.Catalog.ProductsRead,
                AppPermissions.Catalog.UnitsRead,
                AppPermissions.Catalog.ServicesRead,
                AppPermissions.Sales.OrdersRead,
                AppPermissions.Shipping.ShipmentsRead,
                AppPermissions.Shipping.TrackingRead,
                AppPermissions.Shipping.PickupsRead,
                AppPermissions.Shipping.ReturnsRead,
                AppPermissions.Warehouse.OperationsRead,
                AppPermissions.Inventory.InventoryRead,
                AppPermissions.Finance.RatesRead,
                AppPermissions.Finance.InvoicesRead
            }
        };

        var addedCount = 0;

        foreach (var (roleCode, targetPermissions) in matrix)
        {
            if (!roleMap.TryGetValue(roleCode, out var role)) continue;

            var existingPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            foreach (var permCode in targetPermissions)
            {
                if (permissions.TryGetValue(permCode, out var perm) && !existingPermissionIds.Contains(perm.Id))
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = perm.Id,
                        GrantedBy = "SystemSeed",
                        GrantedAt = DateTime.UtcNow
                    });
                    addedCount++;
                }
            }
        }

        if (addedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Assigned {Count} new default role-permission mappings.", addedCount);
        }
    }

    private async Task SeedBootstrapAdminAsync(CancellationToken cancellationToken)
    {
        var adminUsername = _configuration["AdminBootstrap:Username"];
        var adminPassword = _configuration["AdminBootstrap:Password"];
        var adminEmail = _configuration["AdminBootstrap:Email"] ?? "admin@logistics.internal";

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogInformation("No AdminBootstrap configuration found. Skipping automated admin account provisioning.");
            return;
        }

        var normalizedUsername = adminUsername.Trim();
        var exists = await _context.UserAccounts.AnyAsync(u => u.Username == normalizedUsername, cancellationToken);
        if (exists)
        {
            return;
        }

        var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == AppRoles.SuperAdmin, cancellationToken);
        if (superAdminRole == null)
        {
            _logger.LogWarning("SuperAdmin role not found during admin bootstrap.");
            return;
        }

        var adminUser = new UserAccount
        {
            Username = normalizedUsername,
            Email = adminEmail.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.HashPassword(adminPassword),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsActive = true,
            CreatedBy = "AdminBootstrap",
            CreatedAt = DateTime.UtcNow
        };

        adminUser.UserRoles.Add(new UserRole
        {
            RoleId = superAdminRole.Id,
            AssignedBy = "AdminBootstrap",
            AssignedAt = DateTime.UtcNow
        });

        await _context.UserAccounts.AddAsync(adminUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully bootstrapped initial administrator account '{Username}'.", normalizedUsername);
    }
}
