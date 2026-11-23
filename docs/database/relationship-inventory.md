# Foreign Key and Relationship Inventory

**Total Tables:** 90  
**Enforced Delete Behavior:** `DeleteBehavior.Restrict` / `NO ACTION` across all business operational, master, and financial relationships to avoid accidental cascading loss of historical or financial data.

| # | Dependent Table | Foreign Key Column | Principal Table | Delete Behavior | Description |
|:---|:---|:---|:---|:---|:---|
| 1 | `organization.Branches` | `CompanyId` | `organization.Companies` | Restrict | Parent enterprise company. |
| 2 | `organization.Branches` | `AddressId` | `crm.Addresses` | SetNull | Physical branch location. |
| 3 | `organization.Departments` | `CompanyId` | `organization.Companies` | Restrict | Owning company. |
| 4 | `organization.Departments` | `BranchId` | `organization.Branches` | Restrict | Host branch. |
| 5 | `organization.Departments` | `ManagerEmployeeId` | `organization.Employees` | SetNull | Department supervisor. |
| 6 | `organization.Employees` | `BranchId` | `organization.Branches` | Restrict | Primary operating branch. |
| 7 | `organization.Employees` | `DepartmentId` | `organization.Departments` | Restrict | Functional department. |
| 8 | `organization.Employees` | `AddressId` | `crm.Addresses` | SetNull | Residential address. |
| 9 | `organization.Warehouses` | `BranchId` | `organization.Branches` | Restrict | Controlling branch. |
| 10 | `organization.Warehouses` | `AddressId` | `crm.Addresses` | SetNull | Facility geographic address. |
| 11 | `organization.Warehouses` | `ManagerEmployeeId` | `organization.Employees` | SetNull | Facility director. |
| 12 | `identity.UserAccounts` | `EmployeeId` | `organization.Employees` | SetNull | Employee identity profile. |
| 13 | `identity.UserRoles` | `UserId` | `identity.UserAccounts` | Cascade | Bound user account. |
| 14 | `identity.UserRoles` | `RoleId` | `identity.Roles` | Restrict | Assigned role. |
| 15 | `identity.RolePermissions` | `RoleId` | `identity.Roles` | Cascade | Parent role. |
| 16 | `identity.RolePermissions` | `PermissionId` | `identity.Permissions` | Restrict | Entitled permission. |
| 17 | `identity.UserBranchAccess` | `UserId` | `identity.UserAccounts` | Cascade | Authorized user. |
| 18 | `identity.UserBranchAccess` | `BranchId` | `organization.Branches` | Restrict | Permitted branch. |
| 19 | `identity.RefreshTokens` | `UserId` | `identity.UserAccounts` | Cascade | Token owner. |
| 20 | `identity.UserSessions` | `UserId` | `identity.UserAccounts` | Cascade | Active session owner. |
| 21 | `identity.LoginHistories` | `UserId` | `identity.UserAccounts` | SetNull | Attempted user account. |
| 22 | `configuration.Provinces` | `CountryId` | `configuration.Countries` | Restrict | Parent country. |
| 23 | `configuration.Districts` | `ProvinceId` | `configuration.Provinces` | Restrict | Parent province. |
| 24 | `configuration.Wards` | `DistrictId` | `configuration.Districts` | Restrict | Parent district. |
| 25 | `crm.Addresses` | `CountryId` | `configuration.Countries` | Restrict | Nation. |
| 26 | `crm.Addresses` | `ProvinceId` | `configuration.Provinces` | SetNull | Province/City. |
| 27 | `crm.Addresses` | `DistrictId` | `configuration.Districts` | SetNull | District. |
| 28 | `crm.Addresses` | `WardId` | `configuration.Wards` | SetNull | Ward. |
| 29 | `crm.Customers` | `BranchId` | `organization.Branches` | Restrict | Registering branch. |
| 30 | `crm.Customers` | `AccountManagerEmployeeId` | `organization.Employees` | SetNull | Sales representative. |
| 31 | `crm.CustomerContacts` | `CustomerId` | `crm.Customers` | Cascade | Parent customer. |
| 32 | `crm.CustomerAddresses` | `CustomerId` | `crm.Customers` | Cascade | Parent customer. |
| 33 | `crm.CustomerAddresses` | `AddressId` | `crm.Addresses` | Restrict | Referenced address. |
| 34 | `crm.CustomerContracts` | `CustomerId` | `crm.Customers` | Restrict | Contracted client. |
| 35 | `crm.CustomerContracts` | `FileAttachmentId` | `audit.FileAttachments` | SetNull | Scanned contract file. |
| 36 | `crm.CustomerPricingRules` | `CustomerId` | `crm.Customers` | Cascade | Beneficiary client. |
| 37 | `crm.CustomerPricingRules` | `CustomerContractId` | `crm.CustomerContracts` | SetNull | Associated contract. |
| 38 | `crm.CustomerPricingRules` | `ShippingRateCardId` | `finance.ShippingRateCards` | SetNull | Custom rate card. |
| 39 | `catalog.ProductCategories`| `ParentCategoryId` | `catalog.ProductCategories`| Restrict | Parent category. |
| 40 | `catalog.Products` | `CategoryId` | `catalog.ProductCategories`| Restrict | Product category. |
| 41 | `catalog.Products` | `UnitOfMeasureId` | `configuration.UnitsOfMeasure` | Restrict | Standard UOM. |
| 42 | `catalog.Products` | `CurrencyId` | `configuration.Currencies` | Restrict | Declared value currency. |
| 43 | `sales.Orders` | `CustomerId` | `crm.Customers` | Restrict | Ordering customer. |
| 44 | `sales.Orders` | `BranchId` | `organization.Branches` | Restrict | Booking branch. |
| 45 | `sales.Orders` | `CurrencyId` | `configuration.Currencies` | Restrict | Billing currency. |
| 46 | `sales.OrderItems` | `OrderId` | `sales.Orders` | Cascade | Parent order. |
| 47 | `sales.OrderItems` | `ProductId` | `catalog.Products` | SetNull | Catalog product. |
| 48 | `sales.OrderItems` | `UnitOfMeasureId` | `configuration.UnitsOfMeasure` | Restrict | Order unit. |
| 49 | `sales.OrderAddresses` | `OrderId` | `sales.Orders` | Cascade | Parent order snapshot. |
| 50 | `sales.OrderAddresses` | `CountryId` | `configuration.Countries` | Restrict | Address country. |
| 51 | `sales.OrderAddresses` | `ProvinceId` | `configuration.Provinces` | SetNull | Address province. |
| 52 | `sales.OrderAddresses` | `DistrictId` | `configuration.Districts` | SetNull | Address district. |
| 53 | `sales.OrderAddresses` | `WardId` | `configuration.Wards` | SetNull | Address ward. |
| 54 | `sales.OrderPackages` | `OrderId` | `sales.Orders` | Cascade | Parent order. |
| 55 | `shipping.Shipments` | `OrderId` | `sales.Orders` | SetNull | Originating order. |
| 56 | `shipping.Shipments` | `CustomerId` | `crm.Customers` | Restrict | Shipper customer. |
| 57 | `shipping.Shipments` | `OriginBranchId` | `organization.Branches` | Restrict | Origin branch. |
| 58 | `shipping.Shipments` | `DestinationBranchId` | `organization.Branches` | Restrict | Destination branch. |
| 59 | `shipping.Shipments` | `AssignedCarrierId` | `carrier.Carriers` | SetNull | External carrier. |
| 60 | `shipping.Shipments` | `CarrierServiceId` | `carrier.CarrierServices` | SetNull | Carrier service tier. |
| 61 | `shipping.Shipments` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 62 | `shipping.ShipmentPackages`| `ShipmentId` | `shipping.Shipments` | Cascade | Master shipment. |
| 63 | `shipping.ShipmentPackages`| `OrderPackageId` | `sales.OrderPackages` | SetNull | Linked order package. |
| 64 | `shipping.ShipmentAddresses`| `ShipmentId` | `shipping.Shipments` | Cascade | Master shipment. |
| 65 | `shipping.ShipmentAddresses`| `CountryId` | `configuration.Countries` | Restrict | Country. |
| 66 | `shipping.ShipmentAddresses`| `ProvinceId` | `configuration.Provinces` | SetNull | Province. |
| 67 | `shipping.ShipmentAddresses`| `DistrictId` | `configuration.Districts` | SetNull | District. |
| 68 | `shipping.ShipmentAddresses`| `WardId` | `configuration.Wards` | SetNull | Ward. |
| 69 | `shipping.ShipmentStatusHistories` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 70 | `shipping.ShipmentStatusHistories` | `LocationWarehouseId` | `organization.Warehouses` | SetNull | Facility milestone. |
| 71 | `shipping.ShipmentStatusHistories` | `LocationBranchId` | `organization.Branches` | SetNull | Branch milestone. |
| 72 | `shipping.ShipmentStatusHistories` | `ChangedByUserId` | `identity.UserAccounts` | SetNull | Operator. |
| 73 | `shipping.ShipmentStatusHistories` | `ChangedByEmployeeId`| `organization.Employees`| SetNull | Worker / Driver. |
| 74 | `shipping.ShipmentAssignments` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 75 | `shipping.ShipmentAssignments` | `AssignedToEmployeeId`| `organization.Employees`| Restrict | Assigned courier. |
| 76 | `shipping.ShipmentAssignments` | `AssignedBranchId` | `organization.Branches` | Restrict | Supervising branch. |
| 77 | `shipping.ShipmentAssignments` | `AssignedByUserId` | `identity.UserAccounts` | SetNull | Dispatcher user. |
| 78 | `shipping.PickupRequests` | `CustomerId` | `crm.Customers` | Restrict | Requesting client. |
| 79 | `shipping.PickupRequests` | `RequestedAddressId`| `crm.Addresses` | Restrict | Collection location. |
| 80 | `shipping.PickupRequests` | `AssignedEmployeeId`| `organization.Employees`| SetNull | Pickup courier. |
| 81 | `shipping.DeliveryAttempts`| `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 82 | `shipping.DeliveryAttempts`| `AttemptedByEmployeeId`| `organization.Employees`| Restrict | Delivery courier. |
| 83 | `shipping.ProofOfDeliveries`| `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 84 | `shipping.ProofOfDeliveries`| `DeliveryAttemptId` | `shipping.DeliveryAttempts` | Restrict | Successful attempt. |
| 85 | `shipping.ProofOfDeliveries`| `SignatureAttachmentId`| `audit.FileAttachments`| SetNull | Signature image. |
| 86 | `shipping.ProofOfDeliveries`| `PhotoAttachmentId` | `audit.FileAttachments` | SetNull | Photo image. |
| 87 | `shipping.ProofOfDeliveries`| `VerifiedByUserId` | `identity.UserAccounts` | SetNull | Auditor user. |
| 88 | `shipping.ReturnRequests` | `OriginalShipmentId` | `shipping.Shipments` | Restrict | Outbound shipment. |
| 89 | `shipping.ReturnRequests` | `NewReturnShipmentId` | `shipping.Shipments` | SetNull | Inbound return shipment. |
| 90 | `shipping.ReturnRequests` | `ApprovedByUserId` | `identity.UserAccounts` | SetNull | Authorizing user. |
| 91 | `shipping.ShipmentRoutes` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 92 | `shipping.ShipmentRoutes` | `FromWarehouseId` | `organization.Warehouses` | Restrict | Departure hub. |
| 93 | `shipping.ShipmentRoutes` | `ToWarehouseId` | `organization.Warehouses` | Restrict | Arrival hub. |
| 94 | `shipping.ShipmentRoutes` | `CarrierId` | `carrier.Carriers` | SetNull | Linehaul carrier. |
| 95 | `carrier.CarrierServices` | `CarrierId` | `carrier.Carriers` | Restrict | Parent carrier. |
| 96 | `carrier.CarrierConfigurations` | `CarrierId` | `carrier.Carriers` | Restrict | Target carrier. |
| 97 | `carrier.CarrierTrackingMappings`| `CarrierId` | `carrier.Carriers` | Restrict | Origin carrier. |
| 98 | `carrier.CarrierTrackingEvents` | `CarrierId` | `carrier.Carriers` | Restrict | Emitting carrier. |
| 99 | `carrier.CarrierTrackingEvents` | `ProcessedShipmentId` | `shipping.Shipments` | SetNull | Resolved shipment. |
| 100 | `carrier.CarrierWebhookLogs` | `CarrierId` | `carrier.Carriers` | Restrict | Target carrier. |
| 101 | `carrier.CarrierApiLogs` | `CarrierId` | `carrier.Carriers` | Restrict | Target carrier. |
| 102 | `carrier.CarrierSyncJobs` | `CarrierId` | `carrier.Carriers` | Restrict | Polled carrier. |
| 103 | `warehouse.WarehouseZones` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 104 | `warehouse.WarehouseLocations` | `WarehouseZoneId` | `warehouse.WarehouseZones` | Restrict | Zone. |
| 105 | `warehouse.WarehouseReceipts` | `WarehouseId` | `organization.Warehouses` | Restrict | Receiving facility. |
| 106 | `warehouse.WarehouseReceipts` | `SourceBranchId` | `organization.Branches` | SetNull | Sending branch. |
| 107 | `warehouse.WarehouseReceipts` | `ReceivedByEmployeeId`| `organization.Employees`| Restrict | Receiving worker. |
| 108 | `warehouse.WarehouseReceiptItems`| `WarehouseReceiptId` | `warehouse.WarehouseReceipts` | Cascade | Parent receipt. |
| 109 | `warehouse.WarehouseReceiptItems`| `ProductId` | `catalog.Products` | Restrict | Product. |
| 110 | `warehouse.WarehouseReceiptItems`| `UnitOfMeasureId` | `configuration.UnitsOfMeasure` | Restrict | Receipt UOM. |
| 111 | `warehouse.WarehouseReceiptItems`| `TargetLocationId` | `warehouse.WarehouseLocations` | SetNull | Target bin. |
| 112 | `warehouse.WarehouseDispatches` | `WarehouseId` | `organization.Warehouses` | Restrict | Departure facility. |
| 113 | `warehouse.WarehouseDispatches` | `DestinationWarehouseId` | `organization.Warehouses` | SetNull | Target facility. |
| 114 | `warehouse.WarehouseDispatches` | `DestinationBranchId` | `organization.Branches` | SetNull | Target branch. |
| 115 | `warehouse.WarehouseDispatches` | `AssignedCarrierId` | `carrier.Carriers` | SetNull | Linehaul carrier. |
| 116 | `warehouse.WarehouseDispatches` | `DispatchedByEmployeeId`| `organization.Employees`| Restrict | Dispatching worker. |
| 117 | `warehouse.WarehouseDispatchItems` | `WarehouseDispatchId` | `warehouse.WarehouseDispatches` | Cascade | Parent dispatch. |
| 118 | `warehouse.WarehouseDispatchItems` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 119 | `warehouse.WarehouseDispatchItems` | `UnitOfMeasureId` | `configuration.UnitsOfMeasure` | Restrict | Dispatch UOM. |
| 120 | `warehouse.WarehouseDispatchItems` | `SourceLocationId` | `warehouse.WarehouseLocations` | Restrict | Pick location. |
| 121 | `warehouse.WarehouseDispatchPackages` | `WarehouseDispatchId` | `warehouse.WarehouseDispatches` | Cascade | Parent dispatch. |
| 122 | `warehouse.WarehouseDispatchPackages` | `ShipmentPackageId` | `shipping.ShipmentPackages` | Restrict | Loaded package. |
| 123 | `warehouse.WarehouseDispatchPackages` | `ScannedByEmployeeId` | `organization.Employees` | Restrict | Scanning worker. |
| 124 | `warehouse.ParcelHandlingEvents` | `ShipmentPackageId` | `shipping.ShipmentPackages` | Restrict | Scanned package. |
| 125 | `warehouse.ParcelHandlingEvents` | `ShipmentId` | `shipping.Shipments` | Restrict | Master shipment. |
| 126 | `warehouse.ParcelHandlingEvents` | `WarehouseId` | `organization.Warehouses` | Restrict | Scan facility. |
| 127 | `warehouse.ParcelHandlingEvents` | `NextDestinationWarehouseId`| `organization.Warehouses`| SetNull | Next hub. |
| 128 | `warehouse.ParcelHandlingEvents` | `EmployeeId` | `organization.Employees` | Restrict | Scanning employee. |
| 129 | `inventory.InventoryItems` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 130 | `inventory.InventoryItems` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 131 | `inventory.InventoryLots` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 132 | `inventory.InventorySerialNumbers` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 133 | `inventory.InventorySerialNumbers` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 134 | `inventory.InventorySerialNumbers` | `LocationId` | `warehouse.WarehouseLocations` | SetNull | Bin location. |
| 135 | `inventory.InventoryBalances` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 136 | `inventory.InventoryBalances` | `WarehouseLocationId` | `warehouse.WarehouseLocations` | Restrict | Bin location. |
| 137 | `inventory.InventoryBalances` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 138 | `inventory.InventoryBalances` | `LotId` | `inventory.InventoryLots` | SetNull | Lot. |
| 139 | `inventory.InventoryTransactions` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 140 | `inventory.InventoryTransactions` | `PerformedByEmployeeId`| `organization.Employees`| Restrict | Employee. |
| 141 | `inventory.InventoryTransactionItems` | `InventoryTransactionId`| `inventory.InventoryTransactions` | Cascade | Transaction header. |
| 142 | `inventory.InventoryTransactionItems` | `ProductId` | `catalog.Products` | Restrict | Product. |
| 143 | `inventory.InventoryTransactionItems` | `LocationId` | `warehouse.WarehouseLocations` | Restrict | Bin location. |
| 144 | `inventory.InventoryTransactionItems` | `LotId` | `inventory.InventoryLots` | SetNull | Lot. |
| 145 | `inventory.StockAdjustments` | `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 146 | `inventory.StockAdjustments` | `ApprovedByEmployeeId` | `organization.Employees` | SetNull | Approving manager. |
| 147 | `inventory.StockTransfers` | `SourceWarehouseId` | `organization.Warehouses` | Restrict | Source facility. |
| 148 | `inventory.StockTransfers` | `DestinationWarehouseId` | `organization.Warehouses` | Restrict | Destination facility. |
| 149 | `inventory.StockTransfers` | `RequestedByEmployeeId`| `organization.Employees` | Restrict | Requesting employee. |
| 150 | `inventory.StockTransfers` | `ApprovedByEmployeeId` | `organization.Employees` | SetNull | Approving manager. |
| 151 | `inventory.InventoryReservations`| `ProductId` | `catalog.Products` | Restrict | Product. |
| 152 | `inventory.InventoryReservations`| `WarehouseId` | `organization.Warehouses` | Restrict | Facility. |
| 153 | `inventory.InventoryReservations`| `LocationId` | `warehouse.WarehouseLocations` | Restrict | Bin location. |
| 154 | `inventory.InventoryReservations`| `LotId` | `inventory.InventoryLots` | SetNull | Lot. |
| 155 | `inventory.InventoryReservations`| `OrderId` | `sales.Orders` | Restrict | Order. |
| 156 | `finance.ShippingRateCards` | `BaseCurrencyId` | `configuration.Currencies` | Restrict | Base currency. |
| 157 | `finance.ShippingRateRules` | `ShippingRateCardId` | `finance.ShippingRateCards` | Cascade | Rate card. |
| 158 | `finance.ShippingRateRules` | `OriginProvinceId` | `configuration.Provinces` | SetNull | Origin province. |
| 159 | `finance.ShippingRateRules` | `DestinationProvinceId`| `configuration.Provinces` | SetNull | Destination province. |
| 160 | `finance.ShipmentCharges` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 161 | `finance.ShipmentCharges` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 162 | `finance.ShipmentCharges` | `InvoiceId` | `finance.Invoices` | SetNull | Invoice. |
| 163 | `finance.Invoices` | `CustomerId` | `crm.Customers` | Restrict | Customer. |
| 164 | `finance.InvoiceItems` | `InvoiceId` | `finance.Invoices` | Cascade | Invoice. |
| 165 | `finance.InvoiceItems` | `ShipmentId` | `shipping.Shipments` | SetNull | Shipment. |
| 166 | `finance.InvoiceItems` | `ShipmentChargeId` | `finance.ShipmentCharges` | SetNull | Charge line. |
| 167 | `finance.Payments` | `CustomerId` | `crm.Customers` | Restrict | Customer. |
| 168 | `finance.Payments` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 169 | `finance.Payments` | `ConfirmedByUserId` | `identity.UserAccounts` | Restrict | Confirmer user. |
| 170 | `finance.PaymentAllocations` | `PaymentId` | `finance.Payments` | Restrict | Payment. |
| 171 | `finance.PaymentAllocations` | `InvoiceId` | `finance.Invoices` | Restrict | Invoice. |
| 172 | `finance.PaymentAllocations` | `AllocatedByUserId` | `identity.UserAccounts` | SetNull | Accountant user. |
| 173 | `finance.CODCollections` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 174 | `finance.CODCollections` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 175 | `finance.CODCollections` | `CollectedByEmployeeId`| `organization.Employees` | Restrict | Courier. |
| 176 | `finance.CODCollections` | `VerifiedByUserId` | `identity.UserAccounts` | SetNull | Cashier user. |
| 177 | `finance.CODRemittances` | `CustomerId` | `crm.Customers` | Restrict | Customer. |
| 178 | `finance.CODRemittances` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 179 | `finance.CODRemittances` | `RemittedByUserId` | `identity.UserAccounts` | Restrict | Payer user. |
| 180 | `finance.CODRemittanceItems` | `CODRemittanceId` | `finance.CODRemittances` | Cascade | Remittance header. |
| 181 | `finance.CODRemittanceItems` | `CODCollectionId` | `finance.CODCollections` | Restrict | Collection record. |
| 182 | `finance.CODRemittanceItems` | `ShipmentId` | `shipping.Shipments` | Restrict | Shipment. |
| 183 | `finance.CustomerBalances` | `CustomerId` | `crm.Customers` | Cascade | Customer. |
| 184 | `finance.FinancialLedgerEntries` | `CustomerId` | `crm.Customers` | Restrict | Customer. |
| 185 | `finance.FinancialLedgerEntries` | `CurrencyId` | `configuration.Currencies` | Restrict | Currency. |
| 186 | `notification.Notifications` | `TemplateId` | `notification.NotificationTemplates` | SetNull | Template. |
| 187 | `notification.Notifications` | `RecipientUserId` | `identity.UserAccounts` | SetNull | User recipient. |
| 188 | `notification.NotificationDeliveries`| `NotificationId` | `notification.Notifications` | Cascade | Parent notification. |
| 189 | `notification.ScheduledReports`| `ReportDefinitionId`| `notification.ReportDefinitions` | Restrict | Report definition. |
| 190 | `audit.AuditLogs` | `UserId` | `identity.UserAccounts` | SetNull | Acting user. |
| 191 | `audit.FileAttachments` | `UploadedByUserId` | `identity.UserAccounts` | SetNull | Uploader user. |
