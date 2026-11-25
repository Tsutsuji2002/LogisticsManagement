using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;

namespace Logistics.Client.Application.Navigation;

public sealed record NavigationItemDescriptor(
    string Key,
    string ParentGroup,
    string TitleKey,
    string DefaultTitle,
    string[] RequiredAnyPermissions);

public sealed record NavigationGroupDescriptor(
    string GroupKey,
    string IconCode,
    string TitleKey,
    string DefaultTitle,
    IReadOnlyList<NavigationItemDescriptor> Items);

/// <summary>
/// Authoritative desktop navigation hierarchy and permission gating table shared between shell UI and unit tests.
/// </summary>
public static class NavigationCatalog
{
    public static readonly IReadOnlyList<NavigationGroupDescriptor> Groups =
    [
        new("Dashboard", "[DB]", "Nav.Dashboard", "Bảng điều khiển", [
            new("Dashboard", "Dashboard", "Nav.Dashboard", "Tổng quan điều hành",
                [ClientPermissions.Reporting.DashboardRead, ClientPermissions.Reporting.DashboardCompanyRead, ClientPermissions.Reporting.DashboardBranchRead])
        ]),
        new("Organization", "[TC]", "Nav.Organization", "Tổ chức & Nhân sự", [
            new("Org.Company", "Organization", "Nav.OrgCompany", "Thông tin công ty", [ClientPermissions.Organization.CompanyRead]),
            new("Org.Branches", "Organization", "Nav.OrgBranches", "Chi nhánh", [ClientPermissions.Organization.BranchesRead]),
            new("Org.Departments", "Organization", "Nav.OrgDepartments", "Phòng ban", [ClientPermissions.Organization.DepartmentsRead]),
            new("Org.Employees", "Organization", "Nav.OrgEmployees", "Nhân viên", [ClientPermissions.Employees.Read]),
            new("Org.Users", "Organization", "Nav.OrgUsers", "Tài khoản & Phân quyền", [ClientPermissions.Identity.UsersRead, ClientPermissions.Identity.RolesRead])
        ]),
        new("CRM", "[KH]", "Nav.CRM", "Khách hàng (CRM)", [
            new("Crm.Customers", "CRM", "Nav.CrmCustomers", "Khách hàng & Hồ sơ", [ClientPermissions.Crm.CustomersRead])
        ]),
        new("Catalog", "[HH]", "Nav.Catalog", "Danh mục hàng hóa", [
            new("Catalog.Categories", "Catalog", "Nav.CatalogCategories", "Nhóm sản phẩm", [ClientPermissions.Catalog.CategoriesRead]),
            new("Catalog.Products", "Catalog", "Nav.CatalogProducts", "Sản phẩm (SKU)", [ClientPermissions.Catalog.ProductsRead]),
            new("Catalog.Units", "Catalog", "Nav.CatalogUnits", "Đơn vị tính", [ClientPermissions.Catalog.UnitsRead]),
            new("Catalog.Services", "Catalog", "Nav.CatalogServices", "Dịch vụ vận chuyển", [ClientPermissions.Catalog.ServicesRead])
        ]),
        new("Sales", "[ĐH]", "Nav.Sales", "Đơn hàng (Sales)", [
            new("Sales.Orders", "Sales", "Nav.SalesOrders", "Quản lý đơn hàng", [ClientPermissions.Sales.OrdersRead])
        ]),
        new("Shipping", "[VĐ]", "Nav.Shipping", "Vận đơn & Giao nhận", [
            new("Shipping.Shipments", "Shipping", "Nav.Shipments", "Vận đơn", [ClientPermissions.Shipping.ShipmentsRead]),
            new("Shipping.Pickups", "Shipping", "Nav.Pickups", "Yêu cầu lấy hàng", [ClientPermissions.Shipping.PickupsRead]),
            new("Shipping.Returns", "Shipping", "Nav.Returns", "Yêu cầu hoàn hàng", [ClientPermissions.Shipping.ReturnsRead])
        ]),
        new("Carrier", "[3PL]", "Nav.Carrier", "Đối tác vận chuyển", [
            new("Carrier.Carriers", "Carrier", "Nav.CarrierList", "Hãng vận chuyển & Cấu hình", [ClientPermissions.Carrier.CarriersRead]),
            new("Carrier.Operations", "Carrier", "Nav.CarrierOps", "Tích hợp & Nhật ký đồng bộ", [ClientPermissions.Carrier.CarriersRead])
        ]),
        new("Warehouse", "[KB]", "Nav.Warehouse", "Kho bãi & Bưu kiện", [
            new("Warehouse.Facilities", "Warehouse", "Nav.WarehouseList", "Kho, Khu vực & Vị trí", [ClientPermissions.Warehouse.OperationsRead]),
            new("Warehouse.ParcelHub", "Warehouse", "Nav.ParcelHub", "Quét & Xử lý bưu kiện", [ClientPermissions.Warehouse.OperationsRead]),
            new("Warehouse.Dispatches", "Warehouse", "Nav.Dispatches", "Phiếu xuất kho / Hub", [ClientPermissions.Warehouse.OperationsRead])
        ]),
        new("Inventory", "[TK]", "Nav.Inventory", "Tồn kho vật tư / SKU", [
            new("Inventory.Balances", "Inventory", "Nav.InvBalances", "Tồn kho & Định mức SKU", [ClientPermissions.Inventory.InventoryRead]),
            new("Inventory.Receipts", "Inventory", "Nav.InvReceipts", "Nhập kho & Giao dịch", [ClientPermissions.Inventory.InventoryRead]),
            new("Inventory.Movements", "Inventory", "Nav.InvMovements", "Điều chỉnh, Chuyển kho & Giữ hàng", [ClientPermissions.Inventory.InventoryRead]),
            new("Inventory.Tracking", "Inventory", "Nav.InvTracking", "Lô hàng (Lot) & Số Serial", [ClientPermissions.Inventory.InventoryRead])
        ]),
        new("Finance", "[TCN]", "Nav.Finance", "Tài chính & Công nợ", [
            new("Finance.Rates", "Finance", "Nav.FinRates", "Bảng giá & Tính cước", [ClientPermissions.Finance.RatesRead, ClientPermissions.Finance.ChargesRead]),
            new("Finance.Invoices", "Finance", "Nav.FinInvoices", "Hóa đơn & Thanh toán", [ClientPermissions.Finance.InvoicesRead, ClientPermissions.Finance.PaymentsRead]),
            new("Finance.Balances", "Finance", "Nav.FinBalances", "Công nợ & Sổ cái tài chính", [ClientPermissions.Finance.BalancesRead, ClientPermissions.Finance.LedgerRead])
        ]),
        new("COD", "[COD]", "Nav.COD", "Quản lý thu hộ (COD)", [
            new("Cod.Collections", "COD", "Nav.CodCollections", "Thu hộ COD tài xế", [ClientPermissions.Cod.CollectionsRead]),
            new("Cod.Remittances", "COD", "Nav.CodRemittances", "Bảng kê hoàn trả COD", [ClientPermissions.Cod.RemittancesRead]),
            new("Cod.Reconciliation", "COD", "Nav.CodRecon", "Đối soát COD", [ClientPermissions.Cod.ReconciliationRead])
        ]),
        new("Notifications", "[TB]", "Nav.Notifications", "Thông báo", [
            new("Notifications.Center", "Notifications", "Nav.MyNotifications", "Thông báo của tôi", [ClientPermissions.Notification.NotificationsReadOwn]),
            new("Notifications.Templates", "Notifications", "Nav.NotifTemplates", "Mẫu thông báo", [ClientPermissions.Notification.TemplatesRead])
        ]),
        new("Reports", "[BC]", "Nav.Reports", "Báo cáo & Tác vụ nền", [
            new("Reports.Explorer", "Reports", "Nav.ReportsExplorer", "Báo cáo chi tiết & Xuất CSV", [
                ClientPermissions.Reporting.OrdersRead, ClientPermissions.Reporting.ShipmentsRead, ClientPermissions.Reporting.DeliveryRead,
                ClientPermissions.Reporting.CarrierRead, ClientPermissions.Reporting.WarehouseRead, ClientPermissions.Reporting.InventoryRead,
                ClientPermissions.Reporting.FinanceRead, ClientPermissions.Reporting.CodRead
            ]),
            new("Reports.Scheduled", "Reports", "Nav.ReportsScheduled", "Lịch báo cáo & Tác vụ nền", [
                ClientPermissions.Reporting.ScheduleManage, ClientPermissions.Notification.ReportsRead, ClientPermissions.BackgroundProcessing.JobsRead
            ])
        ]),
        new("Settings", "[CD]", "Nav.Settings", "Cài đặt & Bảo mật", [
            new("Settings.Workspace", "Settings", "Nav.Settings", "Hồ sơ, Phiên & Cấu hình API", [])
        ])
    ];

    public static IReadOnlyList<NavigationGroupDescriptor> GetVisibleGroups(IAuthorizationService auth)
    {
        var result = new List<NavigationGroupDescriptor>();
        foreach (var group in Groups)
        {
            var visibleItems = group.Items
                .Where(item => item.RequiredAnyPermissions.Length == 0 || auth.HasAnyPermission(item.RequiredAnyPermissions))
                .ToList();

            if (visibleItems.Count > 0)
            {
                result.Add(group with { Items = visibleItems });
            }
        }
        return result;
    }
}
