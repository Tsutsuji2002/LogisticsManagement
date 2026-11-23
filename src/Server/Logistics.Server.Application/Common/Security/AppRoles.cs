namespace Logistics.Server.Application.Common.Security;

/// <summary>
/// Standard system role codes and human-readable names.
/// </summary>
public static class AppRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string BranchManager = "BRANCH_MANAGER";
    public const string OperationsManager = "OPERATIONS_MANAGER";
    public const string WarehouseStaff = "WAREHOUSE_STAFF";
    public const string CustomerService = "CUSTOMER_SERVICE";
    public const string FinanceStaff = "FINANCE_STAFF";
    public const string ReadOnly = "READ_ONLY";

    public static IReadOnlyList<(string Code, string Name, string Description, bool IsSystem)> GetAllRoles()
    {
        return new List<(string, string, string, bool)>
        {
            (SuperAdmin, "System Administrator", "Full access to all system functions and administrative controls", true),
            (BranchManager, "Branch Manager", "Full operational management of assigned branch, employees, and inventory", true),
            (OperationsManager, "Operations Manager", "Manages transport logistics, shipments, dispatch, and carrier operations", true),
            (WarehouseStaff, "Warehouse Staff", "Manages warehouse receipts, sorting, packaging, and stock inventory", true),
            (CustomerService, "Customer Service Staff", "Handles customer inquiries, CRM records, orders, and shipment tracking", true),
            (FinanceStaff, "Finance Staff", "Handles shipping rates, invoicing, payments, and COD collections", true),
            (ReadOnly, "Read-Only Staff", "View-only access across core operational modules", true)
        };
    }
}
