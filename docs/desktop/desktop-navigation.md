# Desktop Shell & Navigation Catalog

## 1. Main Shell Layout ([MainForm.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.WinForms/MainForm.cs))

- **Collapsible Left Sidebar (`250px` / `58px`)**: Renders permission-filtered expandable navigation groups from [`NavigationCatalog`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Application/Navigation/NavigationCatalog.cs).
- **Top Header Bar**:
  - Group breadcrumb + active screen title.
  - **Accessible Branch Context Dropdown**: Populated from `IBranchContextService`. `SuperAdmin` and multi-branch users can switch between `"All accessible branches"` and specific branches; single-branch users see a locked single branch. Switching immediately triggers `IModuleView.OnBranchContextChangedAsync()`.
  - **Language Selector**: Switches between `vi-VN` and `en-US`.
  - **Notification Bell**: Displays unread badge count (refreshed automatically every 60 seconds via `System.Windows.Forms.Timer` calling `GET /api/v1/notifications/unread-count`).
  - **User Menu**: Profile & Security shortcut, live `/auth/me` permission sync, Logout, and Logout All Devices.
- **Bottom Status Strip**: Shows live `/api/health` connection state, active Environment (`Development` / `Production`), username + roles, and current branch scope.

## 2. Navigation Hierarchy

| Group Key | Navigation Key | Section Title (VI) | Required Any Permissions |
|---|---|---|---|
| `Dashboard` | `Dashboard` | Tổng quan điều hành | `dashboard.read`, `dashboard.company.read`, `dashboard.branch.read` |
| `Organization` | `Org.Company`, `Org.Branches`, `Org.Departments`, `Org.Employees`, `Org.Users` | Tổ chức, Chi nhánh, Phòng ban, Nhân viên, Tài khoản | `organization.*.read`, `employees.read`, `identity.users.read`, `identity.roles.read` |
| `CRM` | `Crm.Customers` | Khách hàng & Hồ sơ | `crm.customers.read` |
| `Catalog` | `Catalog.Categories`, `Catalog.Products`, `Catalog.Units`, `Catalog.Services` | Danh mục sản phẩm, SKU, ĐVT, Dịch vụ | `catalog.*.read` |
| `Sales` | `Sales.Orders` | Quản lý đơn hàng | `sales.orders.read` |
| `Shipping` | `Shipping.Shipments`, `Shipping.Pickups`, `Shipping.Returns` | Vận đơn, Lấy hàng, Hoàn hàng | `shipping.shipments.read`, `shipping.pickups.read`, `shipping.returns.read` |
| `Carrier` | `Carrier.Carriers`, `Carrier.Operations` | Hãng 3PL, Cấu hình & Nhật ký | `carrier.carriers.read` |
| `Warehouse` | `Warehouse.Facilities`, `Warehouse.ParcelHub`, `Warehouse.Dispatches` | Kho, Khu vực/Vị trí, Hub bưu kiện, Phiếu xuất | `warehouse.operations.read` |
| `Inventory` | `Inventory.Balances`, `Inventory.Receipts`, `Inventory.Movements`, `Inventory.Tracking` | Tồn kho, Nhập kho, Điều chỉnh/Chuyển/Giữ hàng, Lot/Serial | `inventory.stock.read` |
| `Finance` | `Finance.Rates`, `Finance.Invoices`, `Finance.Balances` | Bảng giá/Phí, Hóa đơn/Thanh toán, Công nợ/Sổ cái | `finance.rates.read`, `finance.charges.read`, `finance.invoices.read`, `finance.payments.read`, `finance.balances.read`, `finance.ledger.read` |
| `COD` | `Cod.Collections`, `Cod.Remittances`, `Cod.Reconciliation` | Thu hộ COD, Hoàn trả COD, Đối soát COD | `cod.collections.read`, `cod.remittances.read`, `cod.reconciliation.read` |
| `Notifications` | `Notifications.Center`, `Notifications.Templates` | Thông báo của tôi, Mẫu thông báo | `notification.notifications.read-own`, `notification.templates.read` |
| `Reports` | `Reports.Explorer`, `Reports.Scheduled` | Báo cáo & Xuất CSV, Lịch báo cáo & Jobs | `reports.*.read`, `reports.schedule.manage`, `notification.reports.read`, `system.background-jobs.read` |
| `Settings` | `Settings.Workspace` | Hồ sơ, Phiên & Cấu hình API | *(Always accessible while signed in)* |
