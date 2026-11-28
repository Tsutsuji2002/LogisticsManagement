namespace Logistics.Client.Application.Permissions;

/// <summary>
/// Client-side permission constants matching backend <c>AppPermissions</c>.
/// Used solely for UI menu/action visibility; the backend remains the authoritative enforcement layer.
/// </summary>
public static class ClientPermissions
{
    public static class Roles
    {
        public const string SuperAdmin = "SUPER_ADMIN";
    }

    public static class Configuration
    {
        public const string SettingsRead = "configuration.settings.read";
        public const string SettingsManage = "configuration.settings.manage";
    }

    public static class Identity
    {
        public const string UsersRead = "identity.users.read";
        public const string UsersCreate = "identity.users.create";
        public const string UsersUpdate = "identity.users.update";
        public const string UsersRolesManage = "identity.users.roles.manage";
        public const string UsersBranchesManage = "identity.users.branches.manage";
        public const string UsersBranchAccessRead = "identity.users.branch-access.read";
        public const string UsersBranchAccessManage = "identity.users.branch-access.manage";
        public const string UsersResetPassword = "identity.users.reset-password";
        public const string RolesRead = "identity.roles.read";
    }

    public static class Organization
    {
        public const string CompanyRead = "organization.company.read";
        public const string CompanyUpdate = "organization.company.update";
        public const string BranchesRead = "organization.branches.read";
        public const string BranchesCreate = "organization.branches.create";
        public const string BranchesUpdate = "organization.branches.update";
        public const string BranchesManage = "organization.branches.manage";
        public const string DepartmentsRead = "organization.departments.read";
        public const string DepartmentsCreate = "organization.departments.create";
        public const string DepartmentsUpdate = "organization.departments.update";
        public const string DepartmentsManage = "organization.departments.manage";
        public const string WarehousesRead = "organization.warehouses.read";
        public const string OverviewRead = "organization.overview.read";
    }

    public static class Employees
    {
        public const string Read = "employees.read";
        public const string Create = "employees.create";
        public const string Update = "employees.update";
        public const string StatusManage = "employees.status.manage";
        public const string AccountRead = "employees.account.read";
        public const string AccountCreate = "employees.account.create";
        public const string AccountManage = "employees.account.manage";
    }

    public static class Crm
    {
        public const string CustomersRead = "crm.customers.read";
        public const string CustomersCreate = "crm.customers.create";
        public const string CustomersUpdate = "crm.customers.update";
        public const string CustomersDelete = "crm.customers.delete";
        public const string CustomersStatusManage = "crm.customers.status.manage";
        public const string CustomersOverviewRead = "crm.customers.overview.read";

        public const string ContactsRead = "crm.contacts.read";
        public const string ContactsCreate = "crm.contacts.create";
        public const string ContactsUpdate = "crm.contacts.update";
        public const string ContactsManage = "crm.contacts.manage";

        public const string AddressesRead = "crm.addresses.read";
        public const string AddressesCreate = "crm.addresses.create";
        public const string AddressesUpdate = "crm.addresses.update";
        public const string AddressesManage = "crm.addresses.manage";

        public const string ContractsRead = "crm.contracts.read";
        public const string ContractsCreate = "crm.contracts.create";
        public const string ContractsUpdate = "crm.contracts.update";
        public const string ContractsManage = "crm.contracts.manage";

        public const string PricingRulesRead = "crm.pricing-rules.read";
        public const string PricingRulesCreate = "crm.pricing-rules.create";
        public const string PricingRulesUpdate = "crm.pricing-rules.update";
        public const string PricingRulesManage = "crm.pricing-rules.manage";
    }

    public static class Catalog
    {
        public const string CategoriesRead = "catalog.categories.read";
        public const string CategoriesCreate = "catalog.categories.create";
        public const string CategoriesUpdate = "catalog.categories.update";
        public const string CategoriesManageStatus = "catalog.categories.manage-status";
        public const string CategoriesDelete = "catalog.categories.delete";

        public const string ProductsRead = "catalog.products.read";
        public const string ProductsCreate = "catalog.products.create";
        public const string ProductsUpdate = "catalog.products.update";
        public const string ProductsManageStatus = "catalog.products.manage-status";
        public const string ProductsDelete = "catalog.products.delete";

        public const string UnitsRead = "catalog.units.read";
        public const string UnitsManage = "catalog.units.manage";
        public const string ServicesRead = "catalog.services.read";
    }

    public static class Sales
    {
        public const string OrdersRead = "sales.orders.read";
        public const string OrdersCreate = "sales.orders.create";
        public const string OrdersUpdate = "sales.orders.update";
        public const string OrdersCancel = "sales.orders.cancel";
    }

    public static class Shipping
    {
        public const string ShipmentsRead = "shipping.shipments.read";
        public const string ShipmentsCreate = "shipping.shipments.create";
        public const string ShipmentsUpdate = "shipping.shipments.update";
        public const string ShipmentsAssign = "shipping.shipments.assign";
        public const string ShipmentsDeliver = "shipping.shipments.deliver";
        public const string ShipmentsCancel = "shipping.shipments.cancel";
        public const string ShipmentsManageStatus = "shipping.shipments.manage-status";
        public const string TrackingRead = "shipping.tracking.read";
        public const string PickupsRead = "shipping.pickups.read";
        public const string PickupsCreate = "shipping.pickups.create";
        public const string PickupsManage = "shipping.pickups.manage";
        public const string ReturnsRead = "shipping.returns.read";
        public const string ReturnsManage = "shipping.returns.manage";
        public const string RoutesRead = "shipping.routes.read";
        public const string RoutesManage = "shipping.routes.manage";
    }

    public static class Carrier
    {
        public const string CarriersRead = "carrier.carriers.read";
        public const string CarriersManage = "carrier.carriers.manage";
    }

    public static class Warehouse
    {
        public const string OperationsRead = "warehouse.operations.read";
        public const string ReceiptsManage = "warehouse.receipts.manage";
        public const string DispatchesManage = "warehouse.dispatches.manage";
        public const string SortingManage = "warehouse.sorting.manage";
        public const string WarehousesRead = "warehouse.warehouses.read";
        public const string WarehousesManage = "warehouse.warehouses.manage";
        public const string ZonesManage = "warehouse.zones.manage";
        public const string LocationsManage = "warehouse.locations.manage";
    }

    public static class Inventory
    {
        public const string InventoryRead = "inventory.stock.read";
        public const string InventoryManage = "inventory.stock.manage";
        public const string AdjustmentsManage = "inventory.adjustments.manage";
        public const string TransfersManage = "inventory.transfers.manage";
        public const string ReservationsManage = "inventory.reservations.manage";
        public const string LotsManage = "inventory.lots.manage";
        public const string SerialNumbersManage = "inventory.serial-numbers.manage";
    }

    public static class Finance
    {
        public const string RatesRead = "finance.rates.read";
        public const string RatesManage = "finance.rates.manage";
        public const string ChargesRead = "finance.charges.read";
        public const string ChargesCalculate = "finance.charges.calculate";
        public const string ChargesAdjust = "finance.charges.adjust";
        public const string InvoicesRead = "finance.invoices.read";
        public const string InvoicesCreate = "finance.invoices.create";
        public const string InvoicesIssue = "finance.invoices.issue";
        public const string InvoicesCancel = "finance.invoices.cancel";
        public const string PaymentsRead = "finance.payments.read";
        public const string PaymentsManage = "finance.payments.manage";
        public const string PaymentsAllocate = "finance.payments.allocate";
        public const string PaymentsReverse = "finance.payments.reverse";
        public const string BalancesRead = "finance.balances.read";
        public const string LedgerRead = "finance.ledger.read";
        public const string BalancesReconcile = "finance.balances.reconcile";
    }

    public static class Cod
    {
        public const string CollectionsRead = "cod.collections.read";
        public const string CollectionsCreate = "cod.collections.create";
        public const string CollectionsCollect = "cod.collections.collect";
        public const string CollectionsVerify = "cod.collections.verify";
        public const string CollectionsManage = "cod.collections.manage";
        public const string RemittancesRead = "cod.remittances.read";
        public const string RemittancesCreate = "cod.remittances.create";
        public const string RemittancesConfirm = "cod.remittances.confirm";
        public const string RemittancesManage = "cod.remittances.manage";
        public const string ReconciliationRead = "cod.reconciliation.read";
        public const string OperationsReadOwn = "cod.operations.read-own";
        public const string OperationsReadBranch = "cod.operations.read-branch";
    }

    public static class Notification
    {
        public const string NotificationsReadOwn = "notification.notifications.read-own";
        public const string NotificationsManage = "notification.notifications.manage";
        public const string TemplatesRead = "notification.templates.read";
        public const string TemplatesManage = "notification.templates.manage";
        public const string ReportsRead = "notification.reports.read";
        public const string ReportsCreate = "notification.reports.create";
        public const string ReportsUpdate = "notification.reports.update";
        public const string ReportsManage = "notification.reports.manage";
    }

    public static class BackgroundProcessing
    {
        public const string JobsRead = "system.background-jobs.read";
        public const string JobsRetry = "system.background-jobs.retry";
        public const string JobsManage = "system.background-jobs.manage";
        public const string OperationsRead = "system.background-processing.read";
        public const string OperationsManage = "system.background-processing.manage";
    }

    public static class Reporting
    {
        public const string DashboardRead = "dashboard.read";
        public const string DashboardCompanyRead = "dashboard.company.read";
        public const string DashboardBranchRead = "dashboard.branch.read";
        public const string OrdersRead = "reports.orders.read";
        public const string ShipmentsRead = "reports.shipments.read";
        public const string DeliveryRead = "reports.delivery.read";
        public const string WarehouseRead = "reports.warehouse.read";
        public const string InventoryRead = "reports.inventory.read";
        public const string CarrierRead = "reports.carrier.read";
        public const string FinanceRead = "reports.finance.read";
        public const string CodRead = "reports.cod.read";
        public const string Export = "reports.export";
        public const string ScheduleManage = "reports.schedule.manage";
    }
}
