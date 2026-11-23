namespace Logistics.Server.Application.Common.Security;

/// <summary>
/// Strongly typed permission codes matching domain permission reference data.
/// Format: {module}.{resource}.{action}
/// </summary>
public static class AppPermissions
{
    public static class Identity
    {
        public const string UsersRead = "identity.users.read";
        public const string UsersCreate = "identity.users.create";
        public const string UsersUpdate = "identity.users.update";
        public const string UsersDelete = "identity.users.delete";
        public const string UsersRolesManage = "identity.users.roles.manage";
        public const string UsersBranchesManage = "identity.users.branches.manage";
        public const string UsersBranchAccessRead = "identity.users.branch-access.read";
        public const string UsersBranchAccessManage = "identity.users.branch-access.manage";
        public const string UsersResetPassword = "identity.users.reset-password";
        public const string RolesRead = "identity.roles.read";
        public const string RolesManage = "identity.roles.manage";
        public const string PermissionsRead = "identity.permissions.read";
        public const string SessionsRead = "identity.sessions.read";
        public const string SessionsManage = "identity.sessions.manage";
    }

    public static class Organization
    {
        public const string CompaniesRead = "organization.companies.read";
        public const string CompaniesManage = "organization.companies.manage";
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
        public const string EmployeesRead = "organization.employees.read";
        public const string EmployeesManage = "organization.employees.manage";
        public const string WarehousesRead = "organization.warehouses.read";
        public const string WarehousesManage = "organization.warehouses.manage";
        public const string OverviewRead = "organization.overview.read";
        public const string OverviewGlobal = "organization.overview.global";
    }

    public static class Employees
    {
        public const string Read = "employees.read";
        public const string Create = "employees.create";
        public const string Update = "employees.update";
        public const string Manage = "employees.manage";
        public const string StatusManage = "employees.status.manage";
        public const string AccountRead = "employees.account.read";
        public const string AccountCreate = "employees.account.create";
        public const string AccountManage = "employees.account.manage";
    }

    public static class Configuration
    {
        public const string SettingsRead = "configuration.settings.read";
        public const string SettingsManage = "configuration.settings.manage";
        public const string ReferenceDataRead = "configuration.reference.read";
        public const string ReferenceDataManage = "configuration.reference.manage";
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
        public const string ProductsManage = "catalog.products.manage";

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
        public const string ParcelHandlingRead = "warehouse.parcel-handling.read";
        public const string ParcelHandlingManage = "warehouse.parcel-handling.manage";
    }

    public static class Inventory
    {
        public const string InventoryRead = "inventory.stock.read";
        public const string InventoryManage = "inventory.stock.manage";
        public const string AdjustmentsManage = "inventory.adjustments.manage";
        public const string TransfersManage = "inventory.transfers.manage";
        public const string ReceiptsManage = "inventory.receipts.manage";
        public const string IssuesManage = "inventory.issues.manage";
        public const string ReservationsManage = "inventory.reservations.manage";
        public const string LotsManage = "inventory.lots.manage";
        public const string SerialNumbersManage = "inventory.serial-numbers.manage";
    }

    public static class Finance
    {
        public const string RatesRead = "finance.rates.read";
        public const string RatesManage = "finance.rates.manage";
        public const string InvoicesRead = "finance.invoices.read";
        public const string InvoicesCreate = "finance.invoices.create";
        public const string PaymentsManage = "finance.payments.manage";
        public const string CodManage = "finance.cod.manage";
    }

    public static class Notification
    {
        public const string NotificationsRead = "notification.notifications.read";
        public const string TemplatesManage = "notification.templates.manage";
        public const string ReportsManage = "notification.reports.manage";
    }

    public static class Audit
    {
        public const string LogsRead = "audit.logs.read";
    }

    /// <summary>
    /// Returns all registered permission definitions with their metadata.
    /// </summary>
    public static IReadOnlyList<(string Module, string Code, string Name, string Description)> GetAllPermissions()
    {
        return new List<(string, string, string, string)>
        {
            // Identity
            ("Identity", Identity.UsersRead, "Read Users", "Allows viewing user accounts and profiles"),
            ("Identity", Identity.UsersCreate, "Create Users", "Allows creating new user accounts"),
            ("Identity", Identity.UsersUpdate, "Update Users", "Allows updating user account information"),
            ("Identity", Identity.UsersDelete, "Delete Users", "Allows deleting or deactivating user accounts"),
            ("Identity", Identity.UsersRolesManage, "Manage User Roles", "Allows assigning and revoking user roles"),
            ("Identity", Identity.UsersBranchesManage, "Manage User Branches", "Allows granting and revoking branch access"),
            ("Identity", Identity.UsersBranchAccessRead, "Read User Branch Access", "Allows viewing user branch assignments"),
            ("Identity", Identity.UsersBranchAccessManage, "Manage User Branch Access", "Allows modifying user branch assignments"),
            ("Identity", Identity.UsersResetPassword, "Reset User Passwords", "Allows administrative password resets"),
            ("Identity", Identity.RolesRead, "Read Roles", "Allows viewing role definitions and permissions"),
            ("Identity", Identity.RolesManage, "Manage Roles", "Allows modifying role definitions and permissions"),
            ("Identity", Identity.PermissionsRead, "Read Permissions", "Allows viewing permission catalog"),
            ("Identity", Identity.SessionsRead, "Read User Sessions", "Allows viewing active user login sessions"),
            ("Identity", Identity.SessionsManage, "Manage User Sessions", "Allows terminating user login sessions"),

            // Organization
            ("Organization", Organization.CompaniesRead, "Read Companies", "Allows viewing companies"),
            ("Organization", Organization.CompaniesManage, "Manage Companies", "Allows creating and editing companies"),
            ("Organization", Organization.CompanyRead, "Read Company Profile", "Allows viewing company details"),
            ("Organization", Organization.CompanyUpdate, "Update Company Profile", "Allows modifying company details"),
            ("Organization", Organization.BranchesRead, "Read Branches", "Allows viewing branch information"),
            ("Organization", Organization.BranchesCreate, "Create Branches", "Allows creating new branches"),
            ("Organization", Organization.BranchesUpdate, "Update Branches", "Allows updating branch details"),
            ("Organization", Organization.BranchesManage, "Manage Branches", "Allows activating, deactivating and managing branches"),
            ("Organization", Organization.DepartmentsRead, "Read Departments", "Allows viewing departments"),
            ("Organization", Organization.DepartmentsCreate, "Create Departments", "Allows creating departments"),
            ("Organization", Organization.DepartmentsUpdate, "Update Departments", "Allows modifying departments"),
            ("Organization", Organization.DepartmentsManage, "Manage Departments", "Allows managing department status and members"),
            ("Organization", Organization.EmployeesRead, "Read Employees", "Allows viewing employee records"),
            ("Organization", Organization.EmployeesManage, "Manage Employees", "Allows creating and editing employee profiles"),
            ("Organization", Organization.WarehousesRead, "Read Warehouses", "Allows viewing warehouse facilities"),
            ("Organization", Organization.WarehousesManage, "Manage Warehouses", "Allows creating and managing warehouses"),
            ("Organization", Organization.OverviewRead, "Read Organization Overview", "Allows viewing organization overview metrics"),
            ("Organization", Organization.OverviewGlobal, "Read Global Organization Overview", "Allows viewing enterprise-wide overview metrics"),

            // Employees
            ("Employees", Employees.Read, "Read Employees", "Allows viewing employee information and directories"),
            ("Employees", Employees.Create, "Create Employees", "Allows registering new employees"),
            ("Employees", Employees.Update, "Update Employees", "Allows editing employee information"),
            ("Employees", Employees.Manage, "Manage Employees", "Allows managing employee assignments"),
            ("Employees", Employees.StatusManage, "Manage Employee Status", "Allows modifying employment status and terminations"),
            ("Employees", Employees.AccountRead, "Read Employee Account", "Allows viewing linked user account information"),
            ("Employees", Employees.AccountCreate, "Create Employee Account", "Allows provisioning user accounts for employees"),
            ("Employees", Employees.AccountManage, "Manage Employee Account", "Allows managing linked account statuses"),

            // Configuration
            ("Configuration", Configuration.SettingsRead, "Read Settings", "Allows viewing system settings"),
            ("Configuration", Configuration.SettingsManage, "Manage Settings", "Allows updating system settings"),
            ("Configuration", Configuration.ReferenceDataRead, "Read Reference Data", "Allows viewing reference data (provinces, units, etc.)"),
            ("Configuration", Configuration.ReferenceDataManage, "Manage Reference Data", "Allows editing reference data"),

            // CRM
            ("CRM", Crm.CustomersRead, "Read Customers", "Allows viewing customer profiles and accounts"),
            ("CRM", Crm.CustomersCreate, "Create Customers", "Allows creating new customers"),
            ("CRM", Crm.CustomersUpdate, "Update Customers", "Allows updating customer information"),
            ("CRM", Crm.CustomersDelete, "Delete Customers", "Allows deleting or deactivating customers"),
            ("CRM", Crm.CustomersStatusManage, "Manage Customer Status", "Allows activating and deactivating customer accounts"),
            ("CRM", Crm.CustomersOverviewRead, "Read Customer Overview", "Allows viewing CRM customer overview metrics"),
            ("CRM", Crm.ContactsRead, "Read Customer Contacts", "Allows viewing contacts associated with customers"),
            ("CRM", Crm.ContactsCreate, "Create Customer Contacts", "Allows adding contacts to customers"),
            ("CRM", Crm.ContactsUpdate, "Update Customer Contacts", "Allows editing customer contacts"),
            ("CRM", Crm.ContactsManage, "Manage Customer Contacts", "Allows setting primary contacts and removing contacts"),
            ("CRM", Crm.AddressesRead, "Read Customer Addresses", "Allows viewing addresses associated with customers"),
            ("CRM", Crm.AddressesCreate, "Create Customer Addresses", "Allows adding addresses to customers"),
            ("CRM", Crm.AddressesUpdate, "Update Customer Addresses", "Allows modifying customer addresses"),
            ("CRM", Crm.AddressesManage, "Manage Customer Addresses", "Allows managing default addresses and address statuses"),
            ("CRM", Crm.ContractsRead, "Read Customer Contracts", "Allows viewing customer contracts"),
            ("CRM", Crm.ContractsCreate, "Create Customer Contracts", "Allows authoring new customer contracts"),
            ("CRM", Crm.ContractsUpdate, "Update Customer Contracts", "Allows modifying customer contract terms"),
            ("CRM", Crm.ContractsManage, "Manage Customer Contracts", "Allows activating, suspending, and managing contract status"),
            ("CRM", Crm.PricingRulesRead, "Read Customer Pricing Rules", "Allows viewing customer custom pricing rules"),
            ("CRM", Crm.PricingRulesCreate, "Create Customer Pricing Rules", "Allows creating custom pricing rules for customers"),
            ("CRM", Crm.PricingRulesUpdate, "Update Customer Pricing Rules", "Allows editing customer pricing rules"),
            ("CRM", Crm.PricingRulesManage, "Manage Customer Pricing Rules", "Allows activating, deactivating, and deleting pricing rules"),

            // Catalog
            ("Catalog", Catalog.CategoriesRead, "Read Product Categories", "Allows viewing product categories and hierarchy"),
            ("Catalog", Catalog.CategoriesCreate, "Create Product Categories", "Allows creating new product categories"),
            ("Catalog", Catalog.CategoriesUpdate, "Update Product Categories", "Allows modifying product categories"),
            ("Catalog", Catalog.CategoriesManageStatus, "Manage Category Status", "Allows activating and deactivating product categories"),
            ("Catalog", Catalog.CategoriesDelete, "Delete Product Categories", "Allows removing unused product categories"),
            ("Catalog", Catalog.ProductsRead, "Read Products", "Allows viewing product catalog"),
            ("Catalog", Catalog.ProductsCreate, "Create Products", "Allows adding new products to the catalog"),
            ("Catalog", Catalog.ProductsUpdate, "Update Products", "Allows modifying product catalog items"),
            ("Catalog", Catalog.ProductsManageStatus, "Manage Product Status", "Allows activating and deactivating products"),
            ("Catalog", Catalog.ProductsDelete, "Delete Products", "Allows deleting unreferenced products"),
            ("Catalog", Catalog.ProductsManage, "Manage Products", "Allows creating and editing product catalog items"),
            ("Catalog", Catalog.UnitsRead, "Read Units of Measure", "Allows viewing units of measure and conversions"),
            ("Catalog", Catalog.UnitsManage, "Manage Units of Measure", "Allows creating, editing, and managing units of measure"),
            ("Catalog", Catalog.ServicesRead, "Read Services", "Allows viewing logistics service offerings"),

            // Sales
            ("Sales", Sales.OrdersRead, "Read Orders", "Allows viewing sales orders"),
            ("Sales", Sales.OrdersCreate, "Create Orders", "Allows creating new sales orders"),
            ("Sales", Sales.OrdersUpdate, "Update Orders", "Allows editing sales orders"),
            ("Sales", Sales.OrdersCancel, "Cancel Orders", "Allows cancelling sales orders"),

            // Shipping
            ("Shipping", Shipping.ShipmentsRead, "Read Shipments", "Allows viewing shipments and tracking status"),
            ("Shipping", Shipping.ShipmentsCreate, "Create Shipments", "Allows creating new shipments"),
            ("Shipping", Shipping.ShipmentsUpdate, "Update Shipments", "Allows editing shipment details"),
            ("Shipping", Shipping.ShipmentsAssign, "Assign Shipments", "Allows assigning shipments to couriers or routes"),
            ("Shipping", Shipping.ShipmentsDeliver, "Record Deliveries", "Allows completing deliveries and recording POD"),
            ("Shipping", Shipping.ShipmentsCancel, "Cancel Shipments", "Allows cancelling shipments"),
            ("Shipping", Shipping.ShipmentsManageStatus, "Manage Shipment Status", "Allows transitioning shipment status through operational milestones"),
            ("Shipping", Shipping.TrackingRead, "Read Tracking", "Allows viewing tracking events and history"),
            ("Shipping", Shipping.PickupsRead, "Read Pickup Requests", "Allows viewing pickup requests"),
            ("Shipping", Shipping.PickupsCreate, "Create Pickup Requests", "Allows creating pickup requests"),
            ("Shipping", Shipping.PickupsManage, "Manage Pickup Requests", "Allows assigning, confirming, completing and cancelling pickup requests"),
            ("Shipping", Shipping.ReturnsRead, "Read Return Requests", "Allows viewing return requests"),
            ("Shipping", Shipping.ReturnsManage, "Manage Return Requests", "Allows creating, approving, rejecting and completing return requests"),
            ("Shipping", Shipping.RoutesRead, "Read Shipment Routes", "Allows viewing shipment route segments"),
            ("Shipping", Shipping.RoutesManage, "Manage Shipment Routes", "Allows adding and updating shipment route segments"),

            // Carrier
            ("Carrier", Carrier.CarriersRead, "Read Carriers", "Allows viewing external carrier integrations"),
            ("Carrier", Carrier.CarriersManage, "Manage Carriers", "Allows configuring external carrier credentials and services"),

            // Warehouse
            ("Warehouse", Warehouse.OperationsRead, "Read Warehouse Operations", "Allows viewing receipts and dispatches"),
            ("Warehouse", Warehouse.ReceiptsManage, "Manage Receipts", "Allows receiving inventory into warehouse"),
            ("Warehouse", Warehouse.DispatchesManage, "Manage Dispatches", "Allows dispatching goods from warehouse"),
            ("Warehouse", Warehouse.SortingManage, "Manage Sorting Hubs", "Allows sorting and scanning parcels at hubs"),
            ("Warehouse", Warehouse.WarehousesRead, "Read Warehouses", "Allows viewing warehouse facilities and metadata"),
            ("Warehouse", Warehouse.WarehousesManage, "Manage Warehouses", "Allows creating and managing warehouses"),
            ("Warehouse", Warehouse.ZonesManage, "Manage Warehouse Zones", "Allows configuring warehouse storage and sorting zones"),
            ("Warehouse", Warehouse.LocationsManage, "Manage Warehouse Locations", "Allows configuring bin and rack locations"),
            ("Warehouse", Warehouse.ParcelHandlingRead, "Read Parcel Handling", "Allows viewing parcel handling events"),
            ("Warehouse", Warehouse.ParcelHandlingManage, "Manage Parcel Handling", "Allows scanning, sorting, and dispatching customer parcels"),

            // Inventory
            ("Inventory", Inventory.InventoryRead, "Read Inventory", "Allows viewing stock balances and lots"),
            ("Inventory", Inventory.InventoryManage, "Manage Inventory Items", "Allows configuring inventory item reorder rules"),
            ("Inventory", Inventory.AdjustmentsManage, "Manage Adjustments", "Allows making inventory stock adjustments"),
            ("Inventory", Inventory.TransfersManage, "Manage Transfers", "Allows executing stock transfers between locations"),
            ("Inventory", Inventory.ReceiptsManage, "Manage Receipts", "Allows posting inventory receipts"),
            ("Inventory", Inventory.IssuesManage, "Manage Issues", "Allows issuing inventory stock"),
            ("Inventory", Inventory.ReservationsManage, "Manage Reservations", "Allows creating and consuming inventory reservations"),
            ("Inventory", Inventory.LotsManage, "Manage Lots", "Allows creating and tracking inventory lots"),
            ("Inventory", Inventory.SerialNumbersManage, "Manage Serial Numbers", "Allows registering and tracking serial numbers"),

            // Finance
            ("Finance", Finance.RatesRead, "Read Rates", "Allows viewing shipping rate cards and rules"),
            ("Finance", Finance.RatesManage, "Manage Rates", "Allows configuring shipping rate cards and rules"),
            ("Finance", Finance.InvoicesRead, "Read Invoices", "Allows viewing customer invoices and balances"),
            ("Finance", Finance.InvoicesCreate, "Create Invoices", "Allows generating customer invoices"),
            ("Finance", Finance.PaymentsManage, "Manage Payments", "Allows recording payments and allocations"),
            ("Finance", Finance.CodManage, "Manage COD", "Allows managing COD collection and remittances"),

            // Notification
            ("Notification", Notification.NotificationsRead, "Read Notifications", "Allows viewing user notifications"),
            ("Notification", Notification.TemplatesManage, "Manage Templates", "Allows managing notification templates"),
            ("Notification", Notification.ReportsManage, "Manage Reports", "Allows managing report schedules"),

            // Audit
            ("Audit", Audit.LogsRead, "Read Audit Logs", "Allows viewing system audit logs and activity history")
        };
    }
}
