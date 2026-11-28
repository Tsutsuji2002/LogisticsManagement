using Logistics.Server.Domain.Entities.Audit;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Shared.Contracts.DevData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Logistics.Server.Infrastructure.Services;

public partial class DevDataSeeder
{
    private async Task<List<DevModuleCoverageDto>> BuildCoverageListAsync(CancellationToken ct)
    {
        var list = new List<DevModuleCoverageDto>();

        async Task AddRow(string module, string table, IQueryable<object> query, long stdTarget, long largeTarget)
        {
            var count = await query.CountAsync(ct);
            var status = count == 0 ? "Empty"
                : count >= stdTarget ? "Populated"
                : "Partial";
            list.Add(new DevModuleCoverageDto
            {
                Module = module,
                TableName = table,
                CurrentCount = count,
                StandardTargetCount = stdTarget,
                LargeTargetCount = largeTarget,
                Status = status
            });
        }

        await AddRow("Organization", "Companies", _db.Companies, 1, 1);
        await AddRow("Organization", "Branches", _db.Branches, 4, 4);
        await AddRow("Organization", "Departments", _db.Departments, 12, 12);
        await AddRow("Organization", "Employees", _db.Employees, 36, 36);
        await AddRow("Organization", "Warehouses", _db.Warehouses, 6, 6);
        await AddRow("Identity", "UserAccounts", _db.UserAccounts, 25, 25);
        await AddRow("CRM", "Customers", _db.Customers, 120, 200);
        await AddRow("CRM", "CustomerContacts", _db.CustomerContacts, 150, 250);
        await AddRow("CRM", "CustomerAddresses", _db.CustomerAddresses, 240, 400);
        await AddRow("CRM", "CustomerContracts", _db.CustomerContracts, 40, 55);
        await AddRow("CRM", "CustomerPricingRules", _db.CustomerPricingRules, 40, 55);
        await AddRow("Catalog", "ProductCategories", _db.ProductCategories, 10, 10);
        await AddRow("Catalog", "Products", _db.Products, 55, 55);
        await AddRow("Sales", "Orders", _db.Orders, 600, 950);
        await AddRow("Sales", "OrderItems", _db.OrderItems, 1000, 1600);
        await AddRow("Sales", "OrderPackages", _db.OrderPackages, 600, 950);
        await AddRow("Shipping", "Shipments", _db.Shipments, 500, 800);
        await AddRow("Shipping", "ShipmentStatusHistories", _db.ShipmentStatusHistories, 2000, 3500);
        await AddRow("Shipping", "PickupRequests", _db.PickupRequests, 120, 180);
        await AddRow("Shipping", "DeliveryAttempts", _db.DeliveryAttempts, 350, 550);
        await AddRow("Shipping", "ProofOfDeliveries", _db.ProofOfDeliveries, 300, 500);
        await AddRow("Shipping", "ReturnRequests", _db.ReturnRequests, 20, 35);
        await AddRow("Carrier", "Carriers", _db.Carriers, 4, 4);
        await AddRow("Carrier", "CarrierTrackingEvents", _db.CarrierTrackingEvents, 300, 450);
        await AddRow("Warehouse", "WarehouseLocations", _db.WarehouseLocations, 96, 96);
        await AddRow("Warehouse", "WarehouseReceipts", _db.WarehouseReceipts, 210, 350);
        await AddRow("Warehouse", "WarehouseDispatches", _db.WarehouseDispatches, 155, 250);
        await AddRow("Warehouse", "ParcelHandlingEvents", _db.ParcelHandlingEvents, 1000, 1300);
        await AddRow("Inventory", "InventoryItems", _db.InventoryItems, 100, 180);
        await AddRow("Inventory", "InventoryBalances", _db.InventoryBalances, 100, 180);
        await AddRow("Inventory", "InventoryTransactions", _db.InventoryTransactions, 100, 180);
        await AddRow("Inventory", "StockAdjustments", _db.StockAdjustments, 24, 24);
        await AddRow("Finance", "Invoices", _db.Invoices, 80, 130);
        await AddRow("Finance", "Payments", _db.Payments, 60, 100);
        await AddRow("Finance", "CustomerBalances", _db.CustomerBalances, 60, 100);
        await AddRow("COD", "CODCollections", _db.CODCollections, 120, 200);
        await AddRow("COD", "CODRemittances", _db.CODRemittances, 30, 50);
        await AddRow("Notifications", "Notifications", _db.Notifications, 110, 180);
        await AddRow("Audit", "AuditLogs", _db.AuditLogs, 80, 80);

        return list;
    }

    private async Task<Dictionary<string, int>> CaptureKeyCountsAsync(CancellationToken ct) => new()
    {
        ["Customers"] = await _db.Customers.CountAsync(ct),
        ["Orders"] = await _db.Orders.CountAsync(ct),
        ["Shipments"] = await _db.Shipments.CountAsync(ct),
        ["WarehouseReceipts"] = await _db.WarehouseReceipts.CountAsync(ct),
        ["WarehouseDispatches"] = await _db.WarehouseDispatches.CountAsync(ct),
        ["InventoryBalances"] = await _db.InventoryBalances.CountAsync(ct),
        ["Invoices"] = await _db.Invoices.CountAsync(ct),
        ["Payments"] = await _db.Payments.CountAsync(ct),
        ["CODCollections"] = await _db.CODCollections.CountAsync(ct),
        ["CODRemittances"] = await _db.CODRemittances.CountAsync(ct)
    };

    private async Task SaveLastSeedMetadataAsync(string mode, string scale, string summary, CancellationToken ct)
    {
        var keys = new Dictionary<string, string>
        {
            ["DevSeed.LastRunAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["DevSeed.LastMode"] = mode,
            ["DevSeed.LastScale"] = scale,
            ["DevSeed.LastSummary"] = summary
        };

        var existing = await _db.SystemSettings
            .Where(s => s.Category == "DevSeed")
            .ToDictionaryAsync(s => s.Key, ct);

        foreach (var (k, v) in keys)
        {
            if (existing.TryGetValue(k, out var entity))
            {
                entity.Value = v;
                entity.UpdatedAt = DateTime.UtcNow;
                entity.UpdatedBy = "DevSeed";
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Category = "DevSeed",
                    Key = k,
                    Value = v,
                    ValueType = "String",
                    Description = "Dev Test Data execution metadata",
                    UpdatedBy = "DevSeed"
                });
            }
        }

        _db.AuditLogs.Add(new AuditLog
        {
            Username = "DevSeed",
            UserRole = "SUPER_ADMIN",
            Action = $"DevSeed.{mode}",
            TableName = "SystemSettings",
            SchemaName = "configuration",
            PrimaryKey = mode,
            NewValuesJson = summary,
            IpAddress = "127.0.0.1",
            Timestamp = DateTime.UtcNow,
            CreatedBy = "DevSeed"
        });

        await _db.SaveChangesAsync(ct);
    }

    private Task InvalidateDashboardCachesAsync(CancellationToken ct)
    {
        if (_cache is InMemoryCacheService mem)
            mem.Clear();
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Safe Transactional Reset (preserves Roles, Permissions, RolePermissions, admin bootstrap)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ResetTestDataAsync(CancellationToken ct)
    {
        _logger.LogWarning("DevDataSeeder: resetting operational & test data inside transaction...");

        async Task ClearSet<T>(DbSet<T> set) where T : class
        {
            if (_db.Database.IsRelational())
                await set.ExecuteDeleteAsync(ct);
            else
                set.RemoveRange(await set.ToListAsync(ct));
        }

        // Delete in strict reverse FK order
        await ClearSet(_db.NotificationDeliveries);
        await ClearSet(_db.Notifications);
        await ClearSet(_db.ScheduledReports);
        await ClearSet(_db.ReportDefinitions);
        await ClearSet(_db.NotificationTemplates);
        await ClearSet(_db.BackgroundJobLogs);
        await ClearSet(_db.OutboxMessages);
        await ClearSet(_db.AuditLogs);

        await ClearSet(_db.CODRemittanceItems);
        await ClearSet(_db.CODRemittances);
        await ClearSet(_db.CODCollections);
        await ClearSet(_db.FinancialLedgerEntries);
        await ClearSet(_db.PaymentAllocations);
        await ClearSet(_db.Payments);
        await ClearSet(_db.InvoiceItems);
        await ClearSet(_db.ShipmentCharges);
        await ClearSet(_db.Invoices);
        await ClearSet(_db.CustomerBalances);
        await ClearSet(_db.ShippingRateRules);

        await ClearSet(_db.InventoryReservations);
        await ClearSet(_db.StockTransfers);
        await ClearSet(_db.StockAdjustments);
        await ClearSet(_db.InventoryTransactionItems);
        await ClearSet(_db.InventoryTransactions);
        await ClearSet(_db.InventoryBalances);
        await ClearSet(_db.InventorySerialNumbers);
        await ClearSet(_db.InventoryLots);
        await ClearSet(_db.InventoryItems);

        await ClearSet(_db.ParcelHandlingEvents);
        await ClearSet(_db.WarehouseDispatchPackages);
        await ClearSet(_db.WarehouseDispatchItems);
        await ClearSet(_db.WarehouseDispatches);
        await ClearSet(_db.WarehouseReceiptItems);
        await ClearSet(_db.WarehouseReceipts);

        await ClearSet(_db.CarrierTrackingEvents);
        await ClearSet(_db.CarrierWebhookLogs);
        await ClearSet(_db.CarrierApiLogs);
        await ClearSet(_db.CarrierSyncJobs);

        await ClearSet(_db.ReturnRequests);
        await ClearSet(_db.ProofOfDeliveries);
        await ClearSet(_db.DeliveryAttempts);
        await ClearSet(_db.ShipmentRoutes);
        await ClearSet(_db.ShipmentAssignments);
        await ClearSet(_db.ShipmentStatusHistories);
        await ClearSet(_db.ShipmentAddresses);
        await ClearSet(_db.ShipmentPackages);
        await ClearSet(_db.Shipments);
        await ClearSet(_db.PickupRequests);

        await ClearSet(_db.OrderPackages);
        await ClearSet(_db.OrderAddresses);
        await ClearSet(_db.OrderItems);
        await ClearSet(_db.Orders);

        await ClearSet(_db.CustomerPricingRules);
        await ClearSet(_db.CustomerContracts);
        await ClearSet(_db.CustomerAddresses);
        await ClearSet(_db.CustomerContacts);
        await ClearSet(_db.Customers);

        await ClearSet(_db.ShippingRateCards);
        await ClearSet(_db.CarrierTrackingMappings);
        await ClearSet(_db.CarrierConfigurations);
        await ClearSet(_db.CarrierServices);
        await ClearSet(_db.Carriers);

        await ClearSet(_db.Products);
        await ClearSet(_db.ProductCategories);

        // Preserve 'admin' bootstrap account while cleaning test accounts & employees
        var adminId = await _db.UserAccounts
            .Where(u => u.Username == "admin")
            .Select(u => (long?)u.Id)
            .FirstOrDefaultAsync(ct);

        if (_db.Database.IsRelational())
        {
            await _db.LoginHistories.Where(x => x.UserId != adminId).ExecuteDeleteAsync(ct);
            await _db.RefreshTokens.Where(x => x.UserId != adminId).ExecuteDeleteAsync(ct);
            await _db.UserSessions.Where(x => x.UserId != adminId).ExecuteDeleteAsync(ct);
            await _db.UserBranchAccesses.ExecuteDeleteAsync(ct);
            await _db.UserRoles.Where(x => x.UserId != adminId).ExecuteDeleteAsync(ct);
            await _db.UserAccounts.Where(x => x.Id != adminId).ExecuteDeleteAsync(ct);
        }
        else
        {
            _db.LoginHistories.RemoveRange(await _db.LoginHistories.Where(x => x.UserId != adminId).ToListAsync(ct));
            _db.RefreshTokens.RemoveRange(await _db.RefreshTokens.Where(x => x.UserId != adminId).ToListAsync(ct));
            _db.UserSessions.RemoveRange(await _db.UserSessions.Where(x => x.UserId != adminId).ToListAsync(ct));
            _db.UserBranchAccesses.RemoveRange(await _db.UserBranchAccesses.ToListAsync(ct));
            _db.UserRoles.RemoveRange(await _db.UserRoles.Where(x => x.UserId != adminId).ToListAsync(ct));
            _db.UserAccounts.RemoveRange(await _db.UserAccounts.Where(x => x.Id != adminId).ToListAsync(ct));
        }

        await ClearSet(_db.Employees);
        await ClearSet(_db.Departments);
        await ClearSet(_db.WarehouseLocations);
        await ClearSet(_db.WarehouseZones);
        await ClearSet(_db.Warehouses);
        await ClearSet(_db.Branches);
        await ClearSet(_db.Companies);
        await ClearSet(_db.Addresses);
        await ClearSet(_db.NumberSequences);

        if (!_db.Database.IsRelational())
            await _db.SaveChangesAsync(ct);

        _db.ChangeTracker.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Automated Data Integrity Verification
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<DevTestDataIntegrityReportDto> VerifyIntegrityAsync(CancellationToken ct = default)
    {
        EnsureDevSafety(isApiCall: true, isDestructive: false, confirmReset: false);

        var checks = new List<DevIntegrityCheckItemDto>();

        // 1. Orders without items or addresses
        var ordersWithoutItems = await _db.Orders.CountAsync(o => !o.OrderItems.Any(), ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Sales",
            CheckName = "Mọi đơn hàng đều có ít nhất 1 mặt hàng",
            Passed = ordersWithoutItems == 0,
            ViolationCount = ordersWithoutItems,
            Details = ordersWithoutItems == 0 ? "Tất cả đơn hàng đều hợp lệ." : $"{ordersWithoutItems} đơn hàng thiếu OrderItems."
        });

        // 2. Order total consistency
        var orderWeightMismatch = await _db.Orders
            .Where(o => o.OrderItems.Any())
            .CountAsync(o => Math.Abs(o.DeclaredValue - o.OrderItems.Sum(i => i.DeclaredValue)) > 1m, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Sales",
            CheckName = "Tổng giá trị khai báo đơn hàng = tổng chi tiết OrderItems",
            Passed = orderWeightMismatch == 0,
            ViolationCount = orderWeightMismatch,
            Details = orderWeightMismatch == 0 ? "Giá trị khai báo khớp 100%." : $"{orderWeightMismatch} đơn hàng lệch tổng."
        });

        // 3. Delivered shipments must have POD
        var deliveredWithoutPod = await _db.Shipments
            .CountAsync(s => s.ShipmentStatus == "Delivered" && s.ProofOfDelivery == null, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Shipping",
            CheckName = "Vận đơn đã giao (Delivered) đều có ProofOfDelivery",
            Passed = deliveredWithoutPod == 0,
            ViolationCount = deliveredWithoutPod,
            Details = deliveredWithoutPod == 0 ? "Tất cả vận đơn đã giao đều có POD." : $"{deliveredWithoutPod} vận đơn thiếu POD."
        });

        // 4. Shipment charges sum = Shipment.TotalAmount
        var shipmentChargeMismatch = await _db.Shipments
            .Where(s => s.Charges.Any())
            .CountAsync(s => Math.Abs(s.TotalAmount - s.Charges.Sum(c => c.Amount)) > 1m, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Finance",
            CheckName = "Tổng cước Shipment.TotalAmount = tổng ShipmentCharges",
            Passed = shipmentChargeMismatch == 0,
            ViolationCount = shipmentChargeMismatch,
            Details = shipmentChargeMismatch == 0 ? "Cước vận đơn khớp 100%." : $"{shipmentChargeMismatch} vận đơn lệch cước."
        });

        // 5. Invoice math: SubTotal + TaxAmount == TotalAmount & PaidAmount <= TotalAmount
        var invoiceMathMismatch = await _db.Invoices
            .CountAsync(i => Math.Abs((i.SubTotal + i.TaxAmount) - i.TotalAmount) > 1m || i.PaidAmount > i.TotalAmount + 0.01m, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Finance",
            CheckName = "Công thức Hóa đơn (SubTotal + Tax = Total & Paid <= Total)",
            Passed = invoiceMathMismatch == 0,
            ViolationCount = invoiceMathMismatch,
            Details = invoiceMathMismatch == 0 ? "Tất cả hóa đơn cân đối chính xác." : $"{invoiceMathMismatch} hóa đơn sai số."
        });

        // 6. Payment allocations <= Payment.Amount
        var overAllocatedPayments = await _db.Payments
            .Where(p => p.Allocations.Any())
            .CountAsync(p => p.Allocations.Sum(a => a.AllocatedAmount) > p.Amount + 0.01m, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Finance",
            CheckName = "Phân bổ thanh toán <= Số tiền thanh toán",
            Passed = overAllocatedPayments == 0,
            ViolationCount = overAllocatedPayments,
            Details = overAllocatedPayments == 0 ? "Tất cả phân bổ thanh toán hợp lệ." : $"{overAllocatedPayments} thanh toán phân bổ vượt mức."
        });

        // 7. COD Consistency: Remitted <= Collected & Remittance Net = Total - Fee
        var totalCollected = await _db.CODCollections.SumAsync(c => (decimal?)c.CollectedAmount, ct) ?? 0m;
        var totalRemittedGross = await _db.CODRemittances
            .Where(r => r.Status == "Completed")
            .SumAsync(r => (decimal?)r.TotalCodAmount, ct) ?? 0m;
        var remittanceFormulaErrors = await _db.CODRemittances
            .CountAsync(r => Math.Abs((r.TotalCodAmount - r.TotalServiceFeesDeducted) - r.NetRemittanceAmount) > 1m, ct);
        var codOk = totalRemittedGross <= totalCollected + 0.01m && remittanceFormulaErrors == 0;
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "COD",
            CheckName = "Đối soát COD (Đã hoàn trả <= Đã thu & công thức Net chính xác)",
            Passed = codOk,
            ViolationCount = (totalRemittedGross > totalCollected + 0.01m ? 1 : 0) + remittanceFormulaErrors,
            Details = $"Đã thu: {totalCollected:N0} ₫ | Đã đối soát hoàn: {totalRemittedGross:N0} ₫"
        });

        // 8. Inventory Balance consistency
        var invalidBalances = await _db.InventoryBalances
            .CountAsync(b => b.QuantityOnHand < 0 || b.QuantityAllocated < 0 || b.QuantityAllocated > b.QuantityOnHand, ct);
        checks.Add(new DevIntegrityCheckItemDto
        {
            Category = "Inventory",
            CheckName = "Tồn kho hợp lệ (OnHand >= Allocated >= 0)",
            Passed = invalidBalances == 0,
            ViolationCount = invalidBalances,
            Details = invalidBalances == 0 ? "Tất cả số dư tồn kho hợp lệ." : $"{invalidBalances} vị trí kho có số dư âm hoặc phân bổ vượt tồn."
        });

        return new DevTestDataIntegrityReportDto
        {
            AllPassed = checks.All(c => c.Passed),
            CheckedAtUtc = DateTime.UtcNow,
            Checks = checks
        };
    }
}
