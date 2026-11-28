using Logistics.Server.Domain.Entities.Audit;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Integration;
using Logistics.Server.Domain.Entities.Inventory;
using Logistics.Server.Domain.Entities.Notification;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Domain.Entities.Warehouse;
using Logistics.Shared.Contracts.DevData;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

public partial class DevDataSeeder
{
    // ─────────────────────────────────────────────────────────────────────────
    // 15. Orders + Shipments + Packages + Addresses + History + POD + Charges + Carrier Telemetry
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedOrdersAndShipmentsAsync(DevSeedScale scale, bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.Orders.AnyAsync(ct)) return;

        var branches = await _db.Branches.ToListAsync(ct);
        var warehouses = await _db.Warehouses.ToListAsync(ct);
        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);
        var country = await _db.Countries.FirstAsync(ct);
        var provinces = await _db.Provinces.Include(p => p.Districts).ThenInclude(d => d.Wards).ToListAsync(ct);

        var customers = await _db.Customers.Where(c => c.IsActive).ToListAsync(ct);
        var products = await _db.Products.Where(p => p.IsActive).ToListAsync(ct);
        var carriers = await _db.Carriers.Include(c => c.Services).ToListAsync(ct);
        var carrierSvcs = carriers.SelectMany(c => c.Services).ToList();
        var drivers = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        var opsUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == "ops.manager", ct)
                      ?? await _db.UserAccounts.FirstOrDefaultAsync(ct);

        if (customers.Count == 0 || products.Count == 0 || branches.Count == 0 || warehouses.Count == 0) return;

        var provByBranch = new Dictionary<long, Domain.Entities.Configuration.Province>();
        foreach (var br in branches)
        {
            var code = br.Code switch { "HCM" => "79", "HAN" => "01", "DAN" => "48", "CTO" => "92", _ => "79" };
            provByBranch[br.Id] = provinces.FirstOrDefault(p => p.Code == code) ?? provinces[0];
        }

        var whByBranch = warehouses.GroupBy(w => w.BranchId).ToDictionary(g => g.Key, g => g.ToList());
        var streets = new[] { "Lê Duẩn", "Nguyễn Huệ", "Trần Phú", "Hai Bà Trưng", "Điện Biên Phủ", "Nam Kỳ Khởi Nghĩa", "Võ Văn Kiệt", "Hoàng Diệu" };
        var serviceTypes = new[] { "StandardGround", "StandardGround", "StandardGround", "NextDayExpress", "SameDay", "Economical" };

        decimal CalcBaseFee(decimal weightKg, string svcType) => svcType switch
        {
            "NextDayExpress" => Math.Round(Math.Max(45_000m, 45_000m + Math.Max(0, weightKg - 1m) * 12_000m), 0),
            "SameDay"        => Math.Round(Math.Max(65_000m, 65_000m + Math.Max(0, weightKg - 1m) * 15_000m), 0),
            "Economical"     => Math.Round(Math.Max(15_000m, 18_000m + Math.Max(0, weightKg - 0.5m) * 3_000m), 0),
            _                => weightKg <= 2m
                                    ? Math.Round(Math.Max(20_000m, 25_000m + Math.Max(0, weightKg - 0.5m) * 5_000m), 0)
                                    : Math.Round(Math.Max(35_000m, 35_000m + Math.Max(0, weightKg - 2m) * 8_000m), 0)
        };

        var orderCount = isIncremental
            ? (scale == DevSeedScale.Large ? 160 : 90)
            : (scale == DevSeedScale.Large ? 950 : 600);

        var baseOffset = await _db.Orders.CountAsync(ct) + 1001;
        var existingShipOffset = await _db.Shipments.CountAsync(ct) + 1001;

        var orders = new List<Order>(orderCount);
        var orderTargetDates = new List<(Order Order, DateTime Date)>(orderCount);

        // Pre-plan shipment specs paired to orders so Order.Id -> Shipment.OrderId links cleanly
        var pendingShipments = new List<(int OrderIndex, Shipment Shipment, DateTime TargetDate, bool HasReturn)>();

        for (var i = 0; i < orderCount; i++)
        {
            var seq = baseOffset + i;
            var customer = Pick(customers);
            var originBranch = branches.FirstOrDefault(b => b.Id == customer.BranchId) ?? Pick(branches);
            var originProv = provByBranch[originBranch.Id];
            var destBranch = (i % 3 == 0) ? branches[(branches.IndexOf(originBranch) + 1) % branches.Count] : originBranch;
            var destProv = provByBranch[destBranch.Id];

            var daysAgo = isIncremental ? RndInt(0, 30) : RndInt(0, 179);
            var orderDate = DaysAgo(daysAgo);

            var itemCount = RndInt(1, 3);
            var orderItems = new List<OrderItem>(itemCount);
            decimal sumWeight = 0m;
            decimal sumDeclared = 0m;

            for (var k = 0; k < itemCount; k++)
            {
                var prod = Pick(products);
                var qty = RndInt(1, 4);
                var unitPrice = prod.DeclaredValue > 0 ? prod.DeclaredValue : 150_000m;
                var lineVal = unitPrice * qty;
                var lineWeight = prod.WeightKg * qty;
                sumWeight += lineWeight;
                sumDeclared += lineVal;

                orderItems.Add(new OrderItem
                {
                    ProductId = prod.Id,
                    ProductName = prod.Name,
                    Sku = prod.Sku,
                    Quantity = qty,
                    UnitOfMeasureId = prod.UnitOfMeasureId,
                    UnitPrice = unitPrice,
                    TotalPrice = lineVal,
                    DeclaredValue = lineVal,
                    WeightKg = lineWeight,
                    CreatedBy = "DevSeed"
                });
            }

            var isCod = _rng.Next(10) < 4; // ~40% COD orders
            var codAmount = isCod ? Math.Round(sumDeclared * RndDecimal(0.4m, 0.95m) / 1000m) * 1000m : 0m;
            if (isCod && codAmount < 50_000m) codAmount = 120_000m;

            // Realistic status correlated with age
            var isCancelled = daysAgo > 5 && i % 24 == 0;
            string orderStatus;
            string? shipStatus = null;

            if (isCancelled)
            {
                orderStatus = "Cancelled";
            }
            else if (daysAgo <= 1)
            {
                orderStatus = i % 3 == 0 ? "Confirmed" : "Processing";
                shipStatus = i % 2 == 0 ? "ReadyForPickup" : "PickedUp";
            }
            else if (daysAgo <= 3)
            {
                orderStatus = "Processing";
                shipStatus = "InSortingHub";
            }
            else if (daysAgo <= 6)
            {
                orderStatus = "Shipped";
                shipStatus = i % 2 == 0 ? "InTransit" : "OutForDelivery";
            }
            else
            {
                if (i % 20 == 0)
                {
                    orderStatus = "Shipped";
                    shipStatus = "DeliveryFailed";
                }
                else if (i % 19 == 0)
                {
                    orderStatus = "Shipped";
                    shipStatus = "Returned";
                }
                else
                {
                    orderStatus = "Delivered";
                    shipStatus = "Delivered";
                }
            }

            var pkgLen = 32m;
            var pkgWid = 22m;
            var pkgHgt = 16m;
            var volWeight = Math.Round(pkgLen * pkgWid * pkgHgt / 5000m, 3);
            var volCbm = Math.Round(pkgLen * pkgWid * pkgHgt / 1_000_000m, 4);

            var senderLine = $"{RndInt(1, 500)} {Pick(streets)}";
            var senderDist = originProv.Districts.FirstOrDefault();
            var recipName = $"Người nhận {(isIncremental ? "Bổ sung " : "")}{seq}";
            var recipPhone = $"09{RndInt(0, 8)}{RndInt(1000000, 9999999)}";
            var recipLine = $"{RndInt(1, 350)} {Pick(streets)}";
            var recipDist = destProv.Districts.Count > 0 ? Pick(destProv.Districts.ToList()) : null;

            var order = new Order
            {
                OrderNumber = $"ORD-{orderDate:yyyyMM}-{seq:D6}",
                CustomerId = customer.Id,
                BranchId = originBranch.Id,
                OrderDate = orderDate,
                RequestedPickupDate = orderDate.AddHours(6),
                RequiredDeliveryDate = orderDate.AddDays(4),
                OrderStatus = orderStatus,
                PaymentStatus = orderStatus == "Delivered" ? "Paid" : (orderStatus == "Cancelled" ? "Pending" : "Invoiced"),
                TotalWeightKg = sumWeight,
                TotalVolumeCbm = volCbm,
                DeclaredValue = sumDeclared,
                CurrencyId = vnd.Id,
                CodAmount = codAmount,
                Notes = i % 6 == 0 ? "Gọi điện trước khi giao hàng" : null,
                CancellationReason = isCancelled ? "Khách hàng thay đổi kế hoạch giao hàng" : null,
                CancelledAt = isCancelled ? orderDate.AddHours(4) : null,
                CancelledBy = isCancelled ? "cs.staff" : null,
                CreatedBy = "DevSeed"
            };

            foreach (var item in orderItems)
                order.OrderItems.Add(item);

            order.OrderAddresses.Add(new OrderAddress
            {
                AddressType = "Sender",
                ContactName = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                AddressLine1 = senderLine,
                CountryId = country.Id,
                ProvinceId = originProv.Id,
                DistrictId = senderDist?.Id,
                FormattedAddress = $"{senderLine}, {senderDist?.Name}, {originProv.Name}"
            });
            order.OrderAddresses.Add(new OrderAddress
            {
                AddressType = "Recipient",
                ContactName = recipName,
                PhoneNumber = recipPhone,
                AddressLine1 = recipLine,
                CountryId = country.Id,
                ProvinceId = destProv.Id,
                DistrictId = recipDist?.Id,
                FormattedAddress = $"{recipLine}, {recipDist?.Name}, {destProv.Name}"
            });

            var ordPkg = new OrderPackage
            {
                PackageCode = $"OPK-{seq:D6}-01",
                WeightKg = sumWeight,
                LengthCm = pkgLen,
                WidthCm = pkgWid,
                HeightCm = pkgHgt,
                VolumetricWeightKg = volWeight,
                DeclaredValue = sumDeclared,
                Description = orderItems[0].ProductName,
                Barcode = $"OBAR-{seq:D6}-01",
                CreatedBy = "DevSeed"
            };
            order.OrderPackages.Add(ordPkg);

            orders.Add(order);
            orderTargetDates.Add((order, orderDate));

            // Create shipments for ~88% of non-cancelled orders
            if (shipStatus != null && i % 9 != 8)
            {
                var shipSeq = existingShipOffset + pendingShipments.Count;
                var svcType = Pick(serviceTypes);
                var chargeWeight = Math.Max(sumWeight, volWeight);
                var shippingFee = CalcBaseFee(chargeWeight, svcType);
                var insuranceFee = sumDeclared > 3_000_000m ? Math.Round(sumDeclared * 0.004m / 1000m) * 1000m : 0m;
                var codFee = isCod ? Math.Max(5_000m, Math.Round(codAmount * 0.015m / 1000m) * 1000m) : 0m;
                var fuelSurcharge = Math.Round(shippingFee * 0.05m / 1000m) * 1000m;
                var surchargesTotal = codFee + fuelSurcharge;
                var totalAmount = shippingFee + insuranceFee + surchargesTotal;

                var carrierSvc = (i % 5 != 0 && carrierSvcs.Count > 0) ? Pick(carrierSvcs) : null;
                var trackNum = $"WB{orderDate:yyyyMMdd}{shipSeq:D8}";
                var pickupDate = shipStatus == "ReadyForPickup" ? (DateTime?)null : orderDate.AddHours(10);
                var estDelivery = orderDate.AddDays(svcType == "SameDay" ? 1 : (svcType == "NextDayExpress" ? 2 : 5));
                var actualDelivery = shipStatus == "Delivered"
                    ? (DateTime?)orderDate.AddDays(RndInt(1, svcType == "NextDayExpress" ? 2 : 4))
                    : null;

                var originWh = Pick(whByBranch.GetValueOrDefault(originBranch.Id, warehouses));
                var destWh = Pick(whByBranch.GetValueOrDefault(destBranch.Id, warehouses));
                var driver = Pick(drivers);

                var shipment = new Shipment
                {
                    TrackingNumber = trackNum,
                    CustomerId = customer.Id,
                    OriginBranchId = originBranch.Id,
                    DestinationBranchId = destBranch.Id,
                    AssignedCarrierId = carrierSvc?.CarrierId,
                    CarrierServiceId = carrierSvc?.Id,
                    CarrierTrackingNumber = carrierSvc != null ? $"CR-{trackNum}" : null,
                    ShipmentStatus = shipStatus,
                    ServiceType = svcType,
                    TotalPackages = 1,
                    ActualWeightKg = sumWeight,
                    ChargeableWeightKg = chargeWeight,
                    DeclaredValue = sumDeclared,
                    CodAmount = codAmount,
                    ShippingFee = shippingFee,
                    InsuranceFee = insuranceFee,
                    SurchargesTotal = surchargesTotal,
                    TotalAmount = totalAmount,
                    CurrencyId = vnd.Id,
                    PickupDate = pickupDate,
                    EstimatedDeliveryDate = estDelivery,
                    ActualDeliveryDate = actualDelivery,
                    CreatedBy = "DevSeed"
                };

                // Packages
                shipment.Packages.Add(new ShipmentPackage
                {
                    PackageCode = $"PKG-{shipSeq:D6}-01",
                    Barcode = $"BAR-{trackNum}-01",
                    WeightKg = sumWeight,
                    LengthCm = pkgLen,
                    WidthCm = pkgWid,
                    HeightCm = pkgHgt,
                    VolumetricWeightKg = volWeight,
                    PackageStatus = shipStatus switch
                    {
                        "Delivered" => "Delivered",
                        "InSortingHub" => "Sorted",
                        "ReadyForPickup" or "PickedUp" => "Received",
                        _ => "InTransit"
                    },
                    CreatedBy = "DevSeed"
                });

                // Addresses
                shipment.Addresses.Add(new ShipmentAddress
                {
                    AddressType = "Sender",
                    ContactName = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    AddressLine1 = senderLine,
                    CountryId = country.Id,
                    ProvinceId = originProv.Id,
                    DistrictId = senderDist?.Id,
                    FormattedAddress = $"{senderLine}, {senderDist?.Name}, {originProv.Name}"
                });
                shipment.Addresses.Add(new ShipmentAddress
                {
                    AddressType = "Recipient",
                    ContactName = recipName,
                    PhoneNumber = recipPhone,
                    AddressLine1 = recipLine,
                    CountryId = country.Id,
                    ProvinceId = destProv.Id,
                    DistrictId = recipDist?.Id,
                    FormattedAddress = $"{recipLine}, {recipDist?.Name}, {destProv.Name}"
                });

                // Status Histories
                var ts = orderDate.AddHours(2);
                shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "ReadyForPickup", LocationBranchId = originBranch.Id, ChangedByEmployeeId = driver.Id, ChangedAt = ts, Source = "Internal", Notes = "Tạo vận đơn từ đơn hàng" });
                if (shipStatus != "ReadyForPickup")
                {
                    ts = pickupDate ?? ts.AddHours(6);
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "PickedUp", LocationBranchId = originBranch.Id, LocationWarehouseId = originWh.Id, ChangedByEmployeeId = driver.Id, ChangedAt = ts, Source = "MobileApp", Notes = "Tài xế đã nhận bưu kiện" });
                }
                if (shipStatus is "InSortingHub" or "InTransit" or "OutForDelivery" or "Delivered" or "DeliveryFailed" or "Returned")
                {
                    ts = ts.AddHours(8);
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "InSortingHub", LocationBranchId = originBranch.Id, LocationWarehouseId = originWh.Id, ChangedByEmployeeId = driver.Id, ChangedAt = ts, Source = "Internal", Notes = "Quét nhập hub phân loại" });
                }
                if (shipStatus is "InTransit" or "OutForDelivery" or "Delivered" or "DeliveryFailed" or "Returned")
                {
                    ts = ts.AddHours(14);
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "InTransit", LocationBranchId = destBranch.Id, LocationWarehouseId = destWh.Id, ChangedByEmployeeId = driver.Id, ChangedAt = ts, Source = carrierSvc != null ? "CarrierWebhook" : "Internal", Notes = "Đang luân chuyển tuyến chính" });
                }
                if (shipStatus is "OutForDelivery" or "Delivered" or "DeliveryFailed" or "Returned")
                {
                    ts = ts.AddHours(10);
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "OutForDelivery", LocationBranchId = destBranch.Id, ChangedByEmployeeId = driver.Id, ChangedAt = ts, Source = "MobileApp", Notes = "Bưu tá đang đi phát hàng" });
                }
                if (shipStatus == "Delivered" && actualDelivery.HasValue)
                {
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "Delivered", LocationBranchId = destBranch.Id, ChangedByEmployeeId = driver.Id, ChangedAt = actualDelivery.Value, Source = "MobileApp", Notes = "Giao thành công cho người nhận" });
                    var attempt = new DeliveryAttempt
                    {
                        AttemptNumber = 1,
                        AttemptedByEmployeeId = driver.Id,
                        AttemptedAt = actualDelivery.Value,
                        Status = "Delivered",
                        RecipientContacted = true,
                        Latitude = 10.7769m,
                        Longitude = 106.7009m
                    };
                    var pod = new ProofOfDelivery
                    {
                        DeliveryAttempt = attempt,
                        RecipientName = recipName,
                        RecipientRelation = "Self",
                        DeliveredAt = actualDelivery.Value,
                        Notes = "Đã ký nhận nguyên vẹn",
                        VerifiedByUserId = opsUser?.Id,
                        VerifiedAt = actualDelivery.Value
                    };
                    attempt.ProofOfDelivery = pod;
                    shipment.DeliveryAttempts.Add(attempt);
                    shipment.ProofOfDelivery = pod;
                }
                else if (shipStatus == "DeliveryFailed" || shipStatus == "Returned")
                {
                    var failTime = ts.AddHours(4);
                    shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "DeliveryFailed", LocationBranchId = destBranch.Id, ChangedByEmployeeId = driver.Id, ChangedAt = failTime, Source = "MobileApp", Notes = "Người nhận hẹn lại / từ chối nhận" });
                    shipment.DeliveryAttempts.Add(new DeliveryAttempt
                    {
                        AttemptNumber = 1,
                        AttemptedByEmployeeId = driver.Id,
                        AttemptedAt = failTime,
                        Status = "Failed",
                        FailureReasonCode = shipStatus == "Returned" ? "RecipientRefused" : "RecipientAbsent",
                        FailureNotes = "Không liên lạc được người nhận",
                        RecipientContacted = false
                    });
                    if (shipStatus == "Returned")
                    {
                        shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "Returning", LocationBranchId = destBranch.Id, ChangedAt = failTime.AddDays(1), Source = "Internal", Notes = "Đang hoàn về chi nhánh gửi" });
                        shipment.StatusHistories.Add(new ShipmentStatusHistory { Status = "Returned", LocationBranchId = originBranch.Id, ChangedAt = failTime.AddDays(3), Source = "Internal", Notes = "Đã hoàn trả người gửi" });
                    }
                }

                // Assignments & Linehaul Routes
                shipment.Assignments.Add(new ShipmentAssignment
                {
                    AssignmentType = shipStatus is "OutForDelivery" or "Delivered" or "DeliveryFailed" ? "LastMileDelivery" : "Pickup",
                    AssignedToEmployeeId = driver.Id,
                    AssignedBranchId = destBranch.Id,
                    AssignedVehicleId = $"51C-{RndInt(100, 999)}.{RndInt(10, 99)}",
                    AssignedAt = orderDate.AddHours(4),
                    AssignedByUserId = opsUser?.Id,
                    Status = shipStatus == "Delivered" ? "Completed" : "InProgress",
                    CompletedAt = actualDelivery
                });

                if (originWh.Id != destWh.Id)
                {
                    shipment.Routes.Add(new ShipmentRoute
                    {
                        SequenceOrder = 1,
                        FromWarehouseId = originWh.Id,
                        ToWarehouseId = destWh.Id,
                        TransportType = "Road",
                        CarrierId = carrierSvc?.CarrierId,
                        VehicleNumber = $"51D-{RndInt(100, 999)}.{RndInt(10, 99)}",
                        EstimatedDeparture = orderDate.AddDays(1),
                        ActualDeparture = pickupDate?.AddHours(6),
                        EstimatedArrival = estDelivery.AddHours(-12),
                        ActualArrival = actualDelivery?.AddHours(-6),
                        Status = shipStatus == "Delivered" ? "Arrived" : "Dispatched"
                    });
                }

                // Charges (exactly summing to Shipment.TotalAmount!)
                var markInvoiced = shipStatus == "Delivered" && i % 4 != 3; // Leave ~25% unbilled for unbilled KPI!
                shipment.Charges.Add(new ShipmentCharge { ChargeType = "BaseFreight", Amount = shippingFee, CurrencyId = vnd.Id, Description = $"Cước vận chuyển {svcType}", IsInvoiced = false, CreatedAt = orderDate });
                if (insuranceFee > 0)
                    shipment.Charges.Add(new ShipmentCharge { ChargeType = "Insurance", Amount = insuranceFee, CurrencyId = vnd.Id, Description = "Phí bảo hiểm hàng hóa", IsInvoiced = false, CreatedAt = orderDate });
                if (codFee > 0)
                    shipment.Charges.Add(new ShipmentCharge { ChargeType = "CodFee", Amount = codFee, CurrencyId = vnd.Id, Description = "Phí thu hộ COD", IsInvoiced = false, CreatedAt = orderDate });
                if (fuelSurcharge > 0)
                    shipment.Charges.Add(new ShipmentCharge { ChargeType = "FuelSurcharge", Amount = fuelSurcharge, CurrencyId = vnd.Id, Description = "Phụ phí nhiên liệu (5%)", IsInvoiced = false, CreatedAt = orderDate });

                _ = markInvoiced; // Invoice assignment happens in SeedFinanceDataAsync
                pendingShipments.Add((i, shipment, orderDate, shipStatus == "Returned"));
            }
        }

        // Save orders in chunks and backdate CreatedAt (suppress AutoDetectChanges during bulk insert)
        var prevDetect = _db.ChangeTracker.AutoDetectChangesEnabled;
        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var chunk in orders.Chunk(150))
            {
                _db.Orders.AddRange(chunk);
                await _db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = prevDetect;
        }

        await BackdateCreatedAtAsync<Order>(orderTargetDates.Select(x => (x.Order.Id, x.Date)).ToList(), ct);

        // Attach OrderId + OrderPackageId now that Order IDs are generated
        var shipmentsToAdd = new List<Shipment>(pendingShipments.Count);
        var shipTargetDates = new List<(Shipment Shipment, DateTime Date)>(pendingShipments.Count);

        foreach (var entry in pendingShipments)
        {
            var ord = orders[entry.OrderIndex];
            entry.Shipment.OrderId = ord.Id;
            var ordPkg = ord.OrderPackages.FirstOrDefault();
            var shipPkg = entry.Shipment.Packages.FirstOrDefault();
            if (ordPkg != null && shipPkg != null)
                shipPkg.OrderPackageId = ordPkg.Id;

            shipmentsToAdd.Add(entry.Shipment);
            shipTargetDates.Add((entry.Shipment, entry.TargetDate));
        }

        _db.ChangeTracker.AutoDetectChangesEnabled = false;
        try
        {
            foreach (var chunk in shipmentsToAdd.Chunk(100))
            {
                _db.Shipments.AddRange(chunk);
                await _db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = prevDetect;
        }

        await BackdateCreatedAtAsync<Shipment>(shipTargetDates.Select(x => (x.Shipment.Id, x.Date)).ToList(), ct);

        // ProofOfDelivery explicit links (ensure ShipmentId and DeliveryAttemptId are populated)
        // EF Core handles 1:1 navigation automatically, and let's verify ReturnRequests:
        var returnedShips = pendingShipments.Where(x => x.HasReturn).Take(45).ToList();
        var existingRtnCount = await _db.ReturnRequests.CountAsync(ct);
        for (var r = 0; r < returnedShips.Count; r++)
        {
            var sh = returnedShips[r].Shipment;
            _db.ReturnRequests.Add(new ReturnRequest
            {
                ReturnCode = $"RTN-{sh.PickupDate ?? DateTime.UtcNow:yyyyMM}-{existingRtnCount + r + 1001:D6}",
                OriginalShipmentId = sh.Id,
                ReasonCode = Pick(new[] { "RecipientRefused", "AddressNotFound", "RecipientAbsent", "DamagedGoods" }),
                ReasonDescription = "Không giao được hàng - hoàn trả người gửi",
                Status = r % 3 == 0 ? "InTransit" : "ReturnedToSender",
                RequestedAt = (sh.PickupDate ?? DateTime.UtcNow).AddDays(4),
                ApprovedByUserId = opsUser?.Id,
                ApprovedAt = opsUser != null ? (sh.PickupDate ?? DateTime.UtcNow).AddDays(4) : null,
                CreatedBy = "DevSeed"
            });
        }
        if (returnedShips.Count > 0)
            await _db.SaveChangesAsync(ct);

        // Seed Carrier Tracking Events, API Logs, Webhook Logs & Sync Jobs for carrier-assigned shipments
        await SeedCarrierTelemetryAsync(shipmentsToAdd, carriers, isIncremental, ct);
    }

    private async Task SeedCarrierTelemetryAsync(
        List<Shipment> shipments,
        List<Domain.Entities.Carrier.Carrier> carriers,
        bool isIncremental,
        CancellationToken ct)
    {
        if (!isIncremental && await _db.CarrierTrackingEvents.AnyAsync(ct)) return;
        if (carriers.Count == 0) return;

        var carrierShips = shipments.Where(s => s.AssignedCarrierId.HasValue).Take(250).ToList();
        var trackEvents = new List<CarrierTrackingEvent>();
        var webhookLogs = new List<CarrierWebhookLog>();
        var apiLogs = new List<CarrierApiLog>();
        var syncJobs = new List<CarrierSyncJob>();

        foreach (var sh in carrierShips)
        {
            var cid = sh.AssignedCarrierId!.Value;
            var baseTs = sh.PickupDate ?? DateTime.UtcNow.AddDays(-5);
            trackEvents.Add(new CarrierTrackingEvent
            {
                CarrierId = cid,
                CarrierTrackingNumber = sh.CarrierTrackingNumber ?? sh.TrackingNumber,
                ExternalEventId = $"EVT-{sh.TrackingNumber}-01",
                EventCode = "PICKED",
                EventDescription = "Hãng vận chuyển đã nhận bưu gửi",
                EventTimestamp = baseTs,
                LocationName = "Bưu cục gốc",
                RawPayloadJson = $"{{\"tracking\":\"{sh.TrackingNumber}\",\"status\":\"PICKED\"}}",
                ProcessedStatus = "Processed",
                ProcessedShipmentId = sh.Id,
                ProcessedAt = baseTs.AddMinutes(5)
            });

            if (sh.ShipmentStatus == "Delivered" && sh.ActualDeliveryDate.HasValue)
            {
                trackEvents.Add(new CarrierTrackingEvent
                {
                    CarrierId = cid,
                    CarrierTrackingNumber = sh.CarrierTrackingNumber ?? sh.TrackingNumber,
                    ExternalEventId = $"EVT-{sh.TrackingNumber}-02",
                    EventCode = "DELIVERED",
                    EventDescription = "Hãng vận chuyển báo giao hàng thành công",
                    EventTimestamp = sh.ActualDeliveryDate.Value,
                    LocationName = "Địa chỉ người nhận",
                    RawPayloadJson = $"{{\"tracking\":\"{sh.TrackingNumber}\",\"status\":\"DELIVERED\"}}",
                    ProcessedStatus = "Processed",
                    ProcessedShipmentId = sh.Id,
                    ProcessedAt = sh.ActualDeliveryDate.Value.AddMinutes(3)
                });
            }
        }

        var logCount = isIncremental ? 30 : 120;
        for (var i = 0; i < logCount; i++)
        {
            var c = carriers[i % carriers.Count];
            var ts = DaysAgo(RndInt(0, 60));
            var ok = i % 15 != 14;
            apiLogs.Add(new CarrierApiLog
            {
                CarrierId = c.Id,
                Endpoint = "/v2/shipping-order/create",
                HttpMethod = "POST",
                RequestPayloadMasked = "{\"order_code\":\"***\"}",
                ResponsePayloadMasked = ok ? "{\"code\":200,\"message\":\"Success\"}" : "{\"code\":504,\"message\":\"Gateway Timeout\"}",
                ResponseStatusCode = ok ? 200 : 504,
                DurationMs = RndInt(85, 650),
                Timestamp = ts,
                IsSuccess = ok,
                ErrorMessage = ok ? null : "Carrier sandbox timeout"
            });

            webhookLogs.Add(new CarrierWebhookLog
            {
                CarrierId = c.Id,
                ExternalEventId = $"WHK-{Guid.NewGuid():N}"[..20],
                WebhookUrl = $"/api/v1/integrations/carriers/{c.Code}/webhook",
                HttpMethod = "POST",
                PayloadJson = $"{{\"carrier\":\"{c.Code}\",\"event\":\"TRANSIT\"}}",
                ResponseStatusCode = 200,
                ResponseBody = "{\"accepted\":true}",
                ProcessedSuccessfully = true,
                ProcessedAt = ts,
                ClientIpAddress = "103.21.148.10"
            });
        }

        foreach (var c in carriers)
        {
            for (var j = 0; j < (isIncremental ? 2 : 6); j++)
            {
                var st = DaysAgo(j * 3 + 1);
                var total = RndInt(25, 120);
                syncJobs.Add(new CarrierSyncJob
                {
                    CarrierId = c.Id,
                    JobType = j % 2 == 0 ? "TrackingPoll" : "ManifestSync",
                    StartTime = st,
                    EndTime = st.AddSeconds(RndInt(5, 45)),
                    Status = "Completed",
                    TotalRecordsProcessed = total,
                    TotalSuccessCount = total - (j == 0 ? 1 : 0),
                    TotalFailureCount = j == 0 ? 1 : 0
                });
            }
        }

        _db.CarrierTrackingEvents.AddRange(trackEvents);
        _db.CarrierApiLogs.AddRange(apiLogs);
        _db.CarrierWebhookLogs.AddRange(webhookLogs);
        _db.CarrierSyncJobs.AddRange(syncJobs);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 16. Pickup Requests (120 Standard / 180 Large)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedPickupRequestsAsync(DevSeedScale scale, bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.PickupRequests.AnyAsync(ct)) return;

        var customers = await _db.Customers
            .Include(c => c.CustomerAddresses)
            .Where(c => c.CustomerAddresses.Any())
            .ToListAsync(ct);
        var employees = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        if (customers.Count == 0 || employees.Count == 0) return;

        var count = isIncremental ? 25 : (scale == DevSeedScale.Large ? 180 : 120);
        var offset = await _db.PickupRequests.CountAsync(ct) + 1001;

        var pickups = new List<PickupRequest>(count);
        for (var i = 0; i < count; i++)
        {
            var cus = Pick(customers);
            var addrId = cus.CustomerAddresses.First().AddressId;
            var sched = DateOnly.FromDateTime(DaysAgo(RndInt(0, 90)));
            var status = i % 10 == 9 ? "Pending" : (i % 10 == 8 ? "Assigned" : "Completed");
            var est = RndInt(2, 12);

            pickups.Add(new PickupRequest
            {
                PickupCode = $"PKP-{sched:yyyyMM}-{offset + i:D6}",
                CustomerId = cus.Id,
                RequestedAddressId = addrId,
                ScheduledDate = sched,
                TimeSlot = Pick(new[] { "08:00-12:00", "13:00-17:00", "17:00-20:00" }),
                Status = status,
                AssignedEmployeeId = status == "Pending" ? null : Pick(employees).Id,
                ContactName = cus.Name,
                ContactPhone = cus.PhoneNumber,
                EstimatedParcels = est,
                ActualParcelsPickedUp = status == "Completed" ? Math.Max(1, est - RndInt(0, 2)) : 0,
                CreatedBy = "DevSeed"
            });
        }

        _db.PickupRequests.AddRange(pickups);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 17. Warehouse Operations (Receipts, Dispatches, DispatchPackages, ParcelHandlingEvents)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedWarehouseOperationsAsync(DevSeedScale scale, bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.WarehouseReceipts.AnyAsync(ct)) return;

        var warehouses = await _db.Warehouses
            .Include(w => w.Zones).ThenInclude(z => z.Locations)
            .ToListAsync(ct);
        var branches = await _db.Branches.ToListAsync(ct);
        var employees = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        var products = await _db.Products.ToListAsync(ct);
        var carriers = await _db.Carriers.ToListAsync(ct);
        var shipments = await _db.Shipments
            .Include(s => s.Packages)
            .Where(s => s.ShipmentStatus != "Cancelled")
            .OrderByDescending(s => s.Id)
            .Take(350)
            .ToListAsync(ct);

        if (warehouses.Count == 0 || products.Count == 0 || employees.Count == 0) return;

        var receiptCount = isIncremental ? 30 : (scale == DevSeedScale.Large ? 350 : 210);
        var dispatchCount = isIncremental ? 25 : (scale == DevSeedScale.Large ? 250 : 155);
        var rcpOffset = await _db.WarehouseReceipts.CountAsync(ct) + 1001;
        var dspOffset = await _db.WarehouseDispatches.CountAsync(ct) + 1001;

        var receipts = new List<WarehouseReceipt>(receiptCount);
        var receiptDates = new List<(WarehouseReceipt Rcp, DateTime Dt)>(receiptCount);
        var vendors = new[] { "Nhà cung cấp Alpha VN", "Công ty Phân phối Sao Mai", "Xưởng sản xuất Bình Minh", "Kho Tổng Miền Bắc" };

        for (var i = 0; i < receiptCount; i++)
        {
            var wh = warehouses[i % warehouses.Count];
            var allLocs = wh.Zones.SelectMany(z => z.Locations).ToList();
            var targetLoc = allLocs.Count > 0 ? Pick(allLocs) : null;
            var date = DaysAgo(isIncremental ? RndInt(0, 14) : RndInt(0, 150));
            var status = i % 10 == 9 ? "Receiving" : (i % 15 == 14 ? "Pending" : "Completed");
            var rtype = Pick(new[] { "InventoryInbound", "ParcelInbound", "ParcelInbound", "TransferIn" });

            var receipt = new WarehouseReceipt
            {
                ReceiptNumber = $"RCP-{date:yyyyMM}-{rcpOffset + i:D6}",
                WarehouseId = wh.Id,
                ReceiptType = rtype,
                SourceBranchId = Pick(branches).Id,
                SourceVendor = rtype == "InventoryInbound" ? Pick(vendors) : null,
                ExpectedArrivalDate = date.AddHours(-4),
                ActualArrivalDate = date,
                Status = status,
                ReceivedByEmployeeId = Pick(employees).Id,
                Notes = i % 8 == 0 ? "Kiểm đếm đủ số lượng chứng từ" : null,
                CreatedBy = "DevSeed"
            };

            var lineCount = RndInt(1, 4);
            for (var k = 0; k < lineCount; k++)
            {
                var prod = Pick(products);
                var exp = RndInt(5, 45);
                receipt.ReceiptItems.Add(new WarehouseReceiptItem
                {
                    ProductId = prod.Id,
                    ExpectedQuantity = exp,
                    ReceivedQuantity = status == "Completed" ? exp : (status == "Receiving" ? exp / 2 : 0),
                    DamagedQuantity = status == "Completed" && i % 20 == 0 ? 1 : 0,
                    UnitOfMeasureId = prod.UnitOfMeasureId,
                    TargetLocationId = targetLoc?.Id
                });
            }

            receipts.Add(receipt);
            receiptDates.Add((receipt, date));
        }

        _db.WarehouseReceipts.AddRange(receipts);
        await _db.SaveChangesAsync(ct);
        await BackdateCreatedAtAsync<WarehouseReceipt>(receiptDates.Select(x => (x.Rcp.Id, x.Dt)).ToList(), ct);

        // Warehouse Dispatches (use exact statuses checked by DashboardQueryService: Draft/Picking/Packed/Staged/Dispatched)
        var dispatches = new List<WarehouseDispatch>(dispatchCount);
        var dispatchDates = new List<(WarehouseDispatch Dsp, DateTime Dt)>(dispatchCount);
        var allPackages = shipments.SelectMany(s => s.Packages.Select(p => (s, p))).ToList();

        for (var i = 0; i < dispatchCount; i++)
        {
            var wh = warehouses[i % warehouses.Count];
            var allLocs = wh.Zones.SelectMany(z => z.Locations).ToList();
            if (allLocs.Count == 0) continue;

            var date = DaysAgo(isIncremental ? RndInt(0, 14) : RndInt(0, 140));
            var status = i % 10 == 9 ? "Staged" : (i % 14 == 13 ? "Picking" : "Dispatched");
            var destWh = warehouses[(i + 1) % warehouses.Count];
            var emp = Pick(employees);

            var dispatch = new WarehouseDispatch
            {
                DispatchNumber = $"DSP-{date:yyyyMM}-{dspOffset + i:D6}",
                WarehouseId = wh.Id,
                DispatchType = Pick(new[] { "CourierDispatch", "HubTransfer", "InventoryOutbound" }),
                DestinationWarehouseId = destWh.Id != wh.Id ? destWh.Id : null,
                DestinationBranchId = destWh.BranchId,
                AssignedCarrierId = carriers.Count > 0 ? Pick(carriers).Id : null,
                Status = status,
                DispatchedByEmployeeId = emp.Id,
                DispatchedAt = status == "Dispatched" ? date : null,
                VehicleNumber = $"51C-{RndInt(100, 999)}.{RndInt(10, 99)}",
                CreatedBy = "DevSeed"
            };

            var lineCount = RndInt(1, 3);
            for (var k = 0; k < lineCount; k++)
            {
                var prod = Pick(products);
                var reqQty = RndInt(2, 20);
                dispatch.DispatchItems.Add(new WarehouseDispatchItem
                {
                    ProductId = prod.Id,
                    RequestedQuantity = reqQty,
                    PickedQuantity = status == "Dispatched" ? reqQty : (status == "Staged" ? reqQty : 0),
                    UnitOfMeasureId = prod.UnitOfMeasureId,
                    SourceLocationId = Pick(allLocs).Id
                });
            }

            if (allPackages.Count > 0)
            {
                var pkgPair = allPackages[i % allPackages.Count];
                dispatch.DispatchPackages.Add(new WarehouseDispatchPackage
                {
                    ShipmentPackageId = pkgPair.p.Id,
                    Barcode = pkgPair.p.Barcode,
                    ScannedAt = date,
                    ScannedByEmployeeId = emp.Id,
                    TargetContainerCode = $"BAG-{date:MMdd}-{i % 20 + 1:D2}"
                });
            }

            dispatches.Add(dispatch);
            dispatchDates.Add((dispatch, date));
        }

        _db.WarehouseDispatches.AddRange(dispatches);
        await _db.SaveChangesAsync(ct);
        await BackdateCreatedAtAsync<WarehouseDispatch>(dispatchDates.Select(x => (x.Dsp.Id, x.Dt)).ToList(), ct);

        // Parcel Handling Events (for sorting/hub telemetry)
        var eventTypes = new[] { "Unloaded", "ScannedAtHub", "Sorted", "Staged", "LoadedToVehicle", "Dispatched" };
        var parcelEvents = new List<ParcelHandlingEvent>();
        foreach (var pair in allPackages.Take(isIncremental ? 40 : 220))
        {
            var wh = warehouses.FirstOrDefault(w => w.BranchId == pair.s.OriginBranchId) ?? warehouses[0];
            var emp = Pick(employees);
            var scanAt = (pair.s.PickupDate ?? DateTime.UtcNow.AddDays(-5)).AddHours(3);

            foreach (var evtType in eventTypes)
            {
                parcelEvents.Add(new ParcelHandlingEvent
                {
                    ShipmentPackageId = pair.p.Id,
                    ShipmentId = pair.s.Id,
                    WarehouseId = wh.Id,
                    EventType = evtType,
                    BagNumber = $"BAG-{scanAt:yyyyMMdd}-{wh.Id}",
                    EmployeeId = emp.Id,
                    ScannedAt = scanAt,
                    DeviceId = $"PDA-{wh.Code}-0{RndInt(1, 5)}"
                });
                scanAt = scanAt.AddHours(2);
            }
        }

        if (parcelEvents.Count > 0)
        {
            _db.ParcelHandlingEvents.AddRange(parcelEvents);
            await _db.SaveChangesAsync(ct);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 18. Inventory Data (Items, Lots, Serials, Balances, Transactions, Adjustments, Transfers, Reservations)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedInventoryDataAsync(DevSeedScale scale, bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.InventoryItems.AnyAsync(ct)) return;

        var warehouses = await _db.Warehouses
            .Include(w => w.Zones).ThenInclude(z => z.Locations)
            .Where(w => w.Type != "ParcelHub")
            .ToListAsync(ct);
        if (warehouses.Count == 0)
            warehouses = await _db.Warehouses.Include(w => w.Zones).ThenInclude(z => z.Locations).ToListAsync(ct);

        var products = await _db.Products.Where(p => p.IsActive).ToListAsync(ct);
        var employees = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        var receipts = await _db.WarehouseReceipts.Where(r => r.Status == "Completed").Take(30).ToListAsync(ct);
        var activeOrders = await _db.Orders.Where(o => o.OrderStatus == "Confirmed" || o.OrderStatus == "Processing").Take(40).ToListAsync(ct);

        if (warehouses.Count == 0 || products.Count == 0 || employees.Count == 0) return;

        var existingItemKeys = await _db.InventoryItems
            .Select(i => new { i.ProductId, i.WarehouseId })
            .ToListAsync(ct);
        var existingSet = existingItemKeys.Select(x => (x.ProductId, x.WarehouseId)).ToHashSet();

        var newItems = new List<InventoryItem>();
        var newBalances = new List<InventoryBalance>();
        var newLots = new List<InventoryLot>();
        var newSerials = new List<InventorySerialNumber>();
        var newTxns = new List<InventoryTransaction>();

        var txnOffset = await _db.InventoryTransactions.CountAsync(ct) + 1001;
        var serialOffset = await _db.InventorySerialNumbers.CountAsync(ct) + 1001;
        var lotOffset = await _db.InventoryLots.CountAsync(ct) + 1001;

        // Lots for perishable items
        if (!isIncremental || !await _db.InventoryLots.AnyAsync(ct))
        {
            foreach (var prod in products.Where(p => p.IsPerishable || p.Sku.StartsWith("HLTH")).Take(15))
            {
                for (var l = 0; l < 2; l++)
                {
                    newLots.Add(new InventoryLot
                    {
                        ProductId = prod.Id,
                        LotNumber = $"LOT-{prod.Sku}-{lotOffset++:D4}",
                        ManufactureDate = DateOnly.FromDateTime(DaysAgo(RndInt(30, 120))),
                        ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(RndInt(90, 365))),
                        VendorBatchCode = $"BATCH-{RndInt(1000, 9999)}",
                        Notes = "Lô hàng nhập chuẩn"
                    });
                }
            }
            if (newLots.Count > 0)
            {
                _db.InventoryLots.AddRange(newLots);
                await _db.SaveChangesAsync(ct);
            }
        }

        var prodLimit = scale == DevSeedScale.Large ? products.Count : Math.Min(products.Count, 30);
        foreach (var wh in warehouses)
        {
            var storageLocs = wh.Zones.Where(z => z.ZoneType == "BulkStorage").SelectMany(z => z.Locations).ToList();
            if (storageLocs.Count == 0)
                storageLocs = wh.Zones.SelectMany(z => z.Locations).ToList();
            if (storageLocs.Count == 0) continue;

            var emp = Pick(employees);
            var whProducts = products.Take(prodLimit).ToList();

            foreach (var (prod, idx) in whProducts.Select((p, idx) => (p, idx)))
            {
                var loc = storageLocs[idx % storageLocs.Count];

                if (!existingSet.Contains((prod.Id, wh.Id)))
                {
                    existingSet.Add((prod.Id, wh.Id));
                    var reorderPt = 15;
                    newItems.Add(new InventoryItem
                    {
                        ProductId = prod.Id,
                        WarehouseId = wh.Id,
                        ReorderPoint = reorderPt,
                        SafetyStock = 5,
                        TargetStock = 120,
                        MinimumOrderQuantity = 5,
                        MaximumStock = 500,
                        IsActive = true,
                        CreatedBy = "DevSeed"
                    });

                    // Intentionally put ~15% of SKUs at low stock so dashboard low-stock metric works realistically
                    var onHand = idx % 7 == 6 ? RndInt(3, 12) : RndInt(25, 180);
                    var allocated = Math.Min(onHand / 5, RndInt(0, 15));
                    var damaged = idx % 11 == 0 ? RndInt(1, 3) : 0;

                    newBalances.Add(new InventoryBalance
                    {
                        WarehouseId = wh.Id,
                        WarehouseLocationId = loc.Id,
                        ProductId = prod.Id,
                        QuantityOnHand = onHand,
                        QuantityAllocated = allocated,
                        QuantityDamaged = damaged,
                        LastCountDate = DaysAgo(RndInt(1, 20))
                    });

                    var txnDate = DaysAgo(RndInt(5, 90));
                    var rcp = receipts.Count > 0 ? Pick(receipts) : null;
                    var txn = new InventoryTransaction
                    {
                        TransactionNumber = $"TXN-{txnDate:yyyyMM}-{txnOffset++:D6}",
                        WarehouseId = wh.Id,
                        TransactionType = "Receipt",
                        ReferenceType = "WarehouseReceipt",
                        ReferenceId = rcp?.Id ?? 0,
                        ReferenceNumber = rcp?.ReceiptNumber ?? $"RCP-INIT-{wh.Id}",
                        PerformedByEmployeeId = emp.Id,
                        TransactionDate = txnDate,
                        Notes = $"Nhập tồn đầu kỳ & bổ sung {prod.Sku}",
                        CreatedBy = "DevSeed"
                    };
                    var unitCost = prod.DeclaredValue > 0 ? prod.DeclaredValue : 100_000m;
                    txn.TransactionItems.Add(new InventoryTransactionItem
                    {
                        ProductId = prod.Id,
                        LocationId = loc.Id,
                        Quantity = onHand,
                        UnitCost = unitCost,
                        TotalCost = unitCost * onHand,
                        BalanceBefore = 0,
                        BalanceAfter = onHand
                    });
                    newTxns.Add(txn);
                }
            }

            // Serial numbers for Electronics in primary warehouse
            if (!isIncremental && wh.Code.EndsWith("01"))
            {
                foreach (var elec in products.Where(p => p.Sku.StartsWith("ELEC")).Take(5))
                {
                    for (var s = 0; s < 5; s++)
                    {
                        newSerials.Add(new InventorySerialNumber
                        {
                            ProductId = elec.Id,
                            SerialNumber = $"SN-{wh.Code}-{elec.Sku}-{serialOffset++:D5}",
                            WarehouseId = wh.Id,
                            LocationId = storageLocs[0].Id,
                            Status = s == 4 ? "Dispatched" : "Available",
                            ReceivedAt = DaysAgo(RndInt(10, 80)),
                            DispatchedAt = s == 4 ? DaysAgo(RndInt(1, 8)) : null
                        });
                    }
                }
            }
        }

        if (newItems.Count > 0) _db.InventoryItems.AddRange(newItems);
        if (newBalances.Count > 0) _db.InventoryBalances.AddRange(newBalances);
        if (newSerials.Count > 0) _db.InventorySerialNumbers.AddRange(newSerials);
        await _db.SaveChangesAsync(ct);

        if (newTxns.Count > 0)
        {
            _db.InventoryTransactions.AddRange(newTxns);
            await _db.SaveChangesAsync(ct);
        }

        // Stock Adjustments
        var adjOffset = await _db.StockAdjustments.CountAsync(ct) + 1001;
        var adjCount = isIncremental ? 5 : 24;
        var adjReasons = new[]
        {
            ("CountingVariance", "Chênh lệch kiểm kê định kỳ"),
            ("Damage", "Hàng hư hỏng trong quá trình xếp dỡ"),
            ("FoundGoods", "Phát hiện hàng dư tại khu vực lưu trữ"),
            ("Shrinkage", "Hao hụt tự nhiên hàng tiêu dùng")
        };
        for (var i = 0; i < adjCount; i++)
        {
            var wh = warehouses[i % warehouses.Count];
            var (rc, rd) = adjReasons[i % adjReasons.Length];
            var dt = DaysAgo(RndInt(2, 90));
            _db.StockAdjustments.Add(new StockAdjustment
            {
                AdjustmentNumber = $"ADJ-{dt:yyyyMM}-{adjOffset + i:D6}",
                WarehouseId = wh.Id,
                ReasonCode = rc,
                ReasonDescription = rd,
                Status = i % 6 == 5 ? "Draft" : "Applied",
                ApprovedByEmployeeId = Pick(employees).Id,
                ApprovedAt = dt,
                CreatedBy = "DevSeed"
            });
        }

        // Stock Transfers (between warehouses)
        if (warehouses.Count >= 2)
        {
            var trfOffset = await _db.StockTransfers.CountAsync(ct) + 1001;
            var trfCount = isIncremental ? 4 : 20;
            for (var i = 0; i < trfCount; i++)
            {
                var srcWh = warehouses[i % warehouses.Count];
                var dstWh = warehouses[(i + 1) % warehouses.Count];
                var dt = DaysAgo(RndInt(3, 85));
                var status = i % 5 == 4 ? "InTransit" : "Received";
                _db.StockTransfers.Add(new StockTransfer
                {
                    TransferNumber = $"TRF-{dt:yyyyMM}-{trfOffset + i:D6}",
                    SourceWarehouseId = srcWh.Id,
                    DestinationWarehouseId = dstWh.Id,
                    Status = status,
                    RequestedByEmployeeId = Pick(employees).Id,
                    ApprovedByEmployeeId = Pick(employees).Id,
                    ShippedAt = dt,
                    ReceivedAt = status == "Received" ? dt.AddDays(2) : null,
                    Notes = $"Điều chuyển nội bộ từ {srcWh.Code} sang {dstWh.Code}",
                    CreatedBy = "DevSeed"
                });
            }
        }

        // Inventory Reservations linked to active orders
        if (activeOrders.Count > 0 && warehouses.Count > 0)
        {
            var primaryWh = warehouses[0];
            var loc = primaryWh.Zones.SelectMany(z => z.Locations).FirstOrDefault();
            if (loc != null)
            {
                var existingResOrders = await _db.InventoryReservations.Select(r => r.OrderId).ToHashSetAsync(ct);
                foreach (var ord in activeOrders.Where(o => !existingResOrders.Contains(o.Id)).Take(25))
                {
                    var prod = Pick(products);
                    _db.InventoryReservations.Add(new InventoryReservation
                    {
                        ProductId = prod.Id,
                        WarehouseId = primaryWh.Id,
                        LocationId = loc.Id,
                        OrderId = ord.Id,
                        ReservedQuantity = RndInt(1, 5),
                        Status = "Active",
                        ReservedAt = DaysAgo(1),
                        ExpiresAt = DateTime.UtcNow.AddDays(5),
                        CreatedBy = "DevSeed"
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 19. Finance Data (Invoices, InvoiceItems, Payments, PaymentAllocations, Ledger, CustomerBalances)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedFinanceDataAsync(bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.Invoices.AnyAsync(ct)) return;

        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);
        var finUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == "finance.staff", ct)
                      ?? await _db.UserAccounts.FirstAsync(ct);

        // Find uninvoiced charges belonging to delivered/shipped shipments (keeping ~20% unbilled for unbilled KPI)
        var unbilledCharges = await _db.ShipmentCharges
            .Include(c => c.Shipment)
            .Where(c => !c.IsInvoiced && c.Shipment.ShipmentStatus == "Delivered")
            .OrderBy(c => c.Shipment.CreatedAt)
            .ToListAsync(ct);

        // Leave newest ~20% uninvoiced so AuthoritativeUnbilledBalance > 0!
        var toInvoiceCount = (int)(unbilledCharges.Count * 0.80);
        var chargesToBill = unbilledCharges.Take(toInvoiceCount).ToList();

        var invOffset = await _db.Invoices.CountAsync(ct) + 1001;
        var payOffset = await _db.Payments.CountAsync(ct) + 1001;
        var fleOffset = await _db.FinancialLedgerEntries.CountAsync(ct) + 1001;

        var invoices = new List<Invoice>();
        var invoiceChargeMap = new List<(Invoice Inv, List<ShipmentCharge> Charges)>();

        // Group charges by Customer + Month
        var groups = chargesToBill
            .GroupBy(c => new { c.Shipment.CustomerId, Year = c.CreatedAt.Year, Month = c.CreatedAt.Month })
            .ToList();

        foreach (var (grp, idx) in groups.Select((g, i) => (g, i)))
        {
            var grpCharges = grp.ToList();
            if (grpCharges.Count == 0) continue;

            var invDate = new DateOnly(grp.Key.Year, grp.Key.Month, Math.Min(28, DateTime.DaysInMonth(grp.Key.Year, grp.Key.Month)));
            if (invDate > DateOnly.FromDateTime(DateTime.UtcNow))
                invDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var taxRate = 8m;
            decimal subTotal = 0m;
            decimal taxAmount = 0m;

            var status = idx % 8 == 7 ? "Overdue" : (idx % 5 == 4 ? "Issued" : (idx % 4 == 3 ? "PartiallyPaid" : "Paid"));

            var invoice = new Invoice
            {
                InvoiceNumber = $"INV-{invDate:yyyyMM}-{invOffset + idx:D6}",
                CustomerId = grp.Key.CustomerId,
                InvoiceDate = invDate,
                DueDate = status == "Overdue" ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)) : invDate.AddDays(30),
                PeriodStartDate = new DateOnly(grp.Key.Year, grp.Key.Month, 1),
                PeriodEndDate = invDate,
                TaxRate = taxRate,
                PaidAmount = 0m,
                Status = status,
                PaymentTermsDays = 30,
                CreatedBy = "DevSeed"
            };

            foreach (var ch in grpCharges)
            {
                var lineTax = Math.Round(ch.Amount * taxRate / 100m, 0);
                var lineTotal = ch.Amount + lineTax;
                subTotal += ch.Amount;
                taxAmount += lineTax;

                invoice.InvoiceItems.Add(new InvoiceItem
                {
                    ShipmentId = ch.ShipmentId,
                    ShipmentChargeId = ch.Id,
                    Description = $"{ch.Description} ({ch.Shipment.TrackingNumber})",
                    Quantity = 1,
                    UnitPrice = ch.Amount,
                    Amount = ch.Amount,
                    TaxAmount = lineTax,
                    TotalAmount = lineTotal
                });
            }

            invoice.SubTotal = subTotal;
            invoice.TaxAmount = taxAmount;
            invoice.TotalAmount = subTotal + taxAmount;

            invoices.Add(invoice);
            invoiceChargeMap.Add((invoice, grpCharges));
        }

        if (invoices.Count > 0)
        {
            _db.Invoices.AddRange(invoices);
            await _db.SaveChangesAsync(ct);

            // Mark billed ShipmentCharges
            foreach (var (inv, chs) in invoiceChargeMap)
            {
                foreach (var ch in chs)
                {
                    ch.IsInvoiced = true;
                    ch.InvoiceId = inv.Id;
                }
            }
            await _db.SaveChangesAsync(ct);

            // Create Payments & PaymentAllocations & FinancialLedgerEntries
            var payments = new List<Payment>();
            var ledgerEntries = new List<FinancialLedgerEntry>();
            var customerRunningBal = await _db.FinancialLedgerEntries
                .GroupBy(f => f.CustomerId)
                .Select(g => new { g.Key, Bal = g.Sum(x => x.DebitAmount - x.CreditAmount) })
                .ToDictionaryAsync(x => x.Key, x => x.Bal, ct);

            foreach (var (inv, idx) in invoices.Select((v, i) => (v, i)))
            {
                var bal = customerRunningBal.GetValueOrDefault(inv.CustomerId, 0m) + inv.TotalAmount;
                customerRunningBal[inv.CustomerId] = bal;

                var invTs = inv.InvoiceDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
                ledgerEntries.Add(new FinancialLedgerEntry
                {
                    EntryNumber = $"FLE-{inv.InvoiceDate:yyyyMM}-{fleOffset++:D6}",
                    CustomerId = inv.CustomerId,
                    EntryType = "InvoiceCharge",
                    ReferenceType = "Invoice",
                    ReferenceId = inv.Id,
                    ReferenceNumber = inv.InvoiceNumber,
                    DebitAmount = inv.TotalAmount,
                    CreditAmount = 0m,
                    RunningBalance = bal,
                    CurrencyId = vnd.Id,
                    EntryDate = invTs,
                    Description = $"Ghi nợ hóa đơn {inv.InvoiceNumber}",
                    CreatedBy = "DevSeed"
                });

                if (inv.Status is "Paid" or "PartiallyPaid")
                {
                    var payAmt = inv.Status == "Paid"
                        ? inv.TotalAmount
                        : Math.Round(inv.TotalAmount * 0.5m / 1000m) * 1000m;
                    if (payAmt > inv.TotalAmount) payAmt = inv.TotalAmount;

                    var payDateOnly = inv.InvoiceDate.AddDays(RndInt(3, 18));
                    if (payDateOnly > DateOnly.FromDateTime(DateTime.UtcNow))
                        payDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
                    var payTs = payDateOnly.ToDateTime(new TimeOnly(14, 30), DateTimeKind.Utc);

                    var payNum = $"PAY-{payDateOnly:yyyyMM}-{payOffset + idx:D6}";
                    var payment = new Payment
                    {
                        PaymentNumber = payNum,
                        CustomerId = inv.CustomerId,
                        PaymentDate = payTs,
                        PaymentMethod = Pick(new[] { "BankTransfer", "BankTransfer", "Cash", "InternalCredit" }),
                        Amount = payAmt,
                        CurrencyId = vnd.Id,
                        TransactionReference = $"FT{payDateOnly:yyMMdd}{RndInt(100000, 999999)}",
                        BankName = "Vietcombank",
                        Status = "Confirmed",
                        ConfirmedByUserId = finUser.Id,
                        ConfirmedAt = payTs,
                        CreatedBy = "DevSeed"
                    };

                    payment.Allocations.Add(new PaymentAllocation
                    {
                        InvoiceId = inv.Id,
                        AllocatedAmount = payAmt,
                        AllocatedAt = payTs,
                        AllocatedByUserId = finUser.Id
                    });

                    inv.PaidAmount = payAmt;
                    payments.Add(payment);

                    bal -= payAmt;
                    customerRunningBal[inv.CustomerId] = bal;

                    ledgerEntries.Add(new FinancialLedgerEntry
                    {
                        EntryNumber = $"FLE-{payDateOnly:yyyyMM}-{fleOffset++:D6}",
                        CustomerId = inv.CustomerId,
                        EntryType = "PaymentReceipt",
                        ReferenceType = "Payment",
                        ReferenceId = inv.Id,
                        ReferenceNumber = payNum,
                        DebitAmount = 0m,
                        CreditAmount = payAmt,
                        RunningBalance = bal,
                        CurrencyId = vnd.Id,
                        EntryDate = payTs,
                        Description = $"Thanh toán hóa đơn {inv.InvoiceNumber}",
                        CreatedBy = "DevSeed"
                    });
                }
            }

            if (payments.Count > 0) _db.Payments.AddRange(payments);
            if (ledgerEntries.Count > 0) _db.FinancialLedgerEntries.AddRange(ledgerEntries);
            await _db.SaveChangesAsync(ct);

            await BackdateCreatedAtAsync<Invoice>(invoices.Select(v => (v.Id, v.InvoiceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))).ToList(), ct);
            await BackdateCreatedAtAsync<Payment>(payments.Select(p => (p.Id, p.PaymentDate)).ToList(), ct);
        }

        await ReconcileCustomerBalancesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 20. COD Collections & Remittances
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedCodDataAsync(bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.CODCollections.AnyAsync(ct)) return;

        var vnd = await _db.Currencies.FirstAsync(c => c.Code == "VND", ct);
        var collectors = await _db.Employees.Where(e => e.Status == "Active").ToListAsync(ct);
        var finUser = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == "finance.staff", ct)
                      ?? await _db.UserAccounts.FirstAsync(ct);

        var existingShipmentIds = await _db.CODCollections.Select(c => c.ShipmentId).ToHashSetAsync(ct);
        var codShipments = await _db.Shipments
            .Where(s => s.ShipmentStatus == "Delivered" && s.CodAmount > 0 && !existingShipmentIds.Contains(s.Id))
            .OrderBy(s => s.ActualDeliveryDate)
            .ToListAsync(ct);

        if (codShipments.Count == 0) return;

        var collections = new List<CODCollection>(codShipments.Count);
        for (var i = 0; i < codShipments.Count; i++)
        {
            var sh = codShipments[i];
            var collDate = sh.ActualDeliveryDate ?? DateTime.UtcNow.AddDays(-5);
            // Pipeline distribution: ~70% verified/remitted, ~15% deposited, ~15% driver cash in hand
            var status = i % 10 < 7 ? "VerifiedByCashier" : (i % 10 < 8 ? "DepositedToBranch" : "CollectedByDriver");

            collections.Add(new CODCollection
            {
                ShipmentId = sh.Id,
                CollectedAmount = sh.CodAmount,
                CurrencyId = vnd.Id,
                CollectionDate = collDate,
                CollectedByEmployeeId = Pick(collectors).Id,
                Status = status,
                DriverReceiptNumber = $"DR-{collDate:yyyyMMdd}-{sh.Id:D5}",
                VerifiedByUserId = status == "VerifiedByCashier" ? finUser.Id : null,
                VerifiedAt = status == "VerifiedByCashier" ? collDate.AddHours(12) : null,
                CreatedBy = "DevSeed"
            });
        }

        _db.CODCollections.AddRange(collections);
        await _db.SaveChangesAsync(ct);

        // Create COD Remittances for ~75% of VerifiedByCashier collections, marking those collections RemittedToCustomer
        var verifiedReady = collections
            .Where(c => c.Status == "VerifiedByCashier")
            .Join(codShipments, c => c.ShipmentId, s => s.Id, (c, s) => new { Coll = c, Ship = s })
            .GroupBy(x => x.Ship.CustomerId)
            .ToList();

        var remOffset = await _db.CODRemittances.CountAsync(ct) + 1001;
        var customersById = await _db.Customers.ToDictionaryAsync(c => c.Id, ct);
        var remittances = new List<CODRemittance>();

        foreach (var (grp, idx) in verifiedReady.Select((g, i) => (g, i)))
        {
            // Keep ~25% of customers' verified COD unremitted so TotalCustomerCodHoldingBalance > 0
            if (idx % 4 == 3) continue;

            var items = grp.ToList();
            if (items.Count == 0) continue;

            var remDate = items.Max(x => x.Coll.CollectionDate).AddDays(2);
            if (remDate > DateTime.UtcNow) remDate = DateTime.UtcNow;

            var cus = customersById.GetValueOrDefault(grp.Key);
            decimal totalCod = 0m;
            decimal totalFee = 0m;

            var remittance = new CODRemittance
            {
                RemittanceNumber = $"REM-{remDate:yyyyMM}-{remOffset + remittances.Count:D6}",
                CustomerId = grp.Key,
                RemittanceDate = remDate,
                CurrencyId = vnd.Id,
                PaymentMethod = "BankTransfer",
                PaymentReference = $"COD-REM-{remDate:yyMMdd}-{idx + 1:D4}",
                Status = "Completed",
                BankName = "Ngân hàng TMCP Ngoại Thương (VCB)",
                BankAccount = $"007100{RndInt(1000000, 9999999)}",
                AccountHolderName = cus?.Name ?? "Khách hàng đối tác",
                RemittedByUserId = finUser.Id,
                RemittedAt = remDate,
                CreatedBy = "DevSeed"
            };

            foreach (var x in items)
            {
                var fee = Math.Round(x.Coll.CollectedAmount * 0.015m / 1000m) * 1000m;
                var net = x.Coll.CollectedAmount - fee;
                totalCod += x.Coll.CollectedAmount;
                totalFee += fee;

                remittance.RemittanceItems.Add(new CODRemittanceItem
                {
                    CODCollectionId = x.Coll.Id,
                    ShipmentId = x.Ship.Id,
                    CodAmount = x.Coll.CollectedAmount,
                    DeductedShippingFee = fee,
                    NetAmount = net
                });

                x.Coll.Status = "RemittedToCustomer";
            }

            remittance.TotalCodAmount = totalCod;
            remittance.TotalServiceFeesDeducted = totalFee;
            remittance.NetRemittanceAmount = totalCod - totalFee;
            remittances.Add(remittance);
        }

        if (remittances.Count > 0)
        {
            _db.CODRemittances.AddRange(remittances);
            await _db.SaveChangesAsync(ct);
            await BackdateCreatedAtAsync<CODRemittance>(remittances.Select(r => (r.Id, r.RemittanceDate)).ToList(), ct);
        }

        await BackdateCreatedAtAsync<CODCollection>(collections.Select(c => (c.Id, c.CollectionDate)).ToList(), ct);
        await ReconcileCustomerBalancesAsync(ct);
    }

    private async Task ReconcileCustomerBalancesAsync(CancellationToken ct)
    {
        var customerIds = await _db.Customers.Select(c => c.Id).ToListAsync(ct);
        var existingBalances = await _db.CustomerBalances.ToDictionaryAsync(b => b.CustomerId, ct);

        var outstandingByCus = await _db.Invoices
            .Where(i => i.Status != "Draft" && i.Status != "Cancelled")
            .GroupBy(i => i.CustomerId)
            .Select(g => new { g.Key, Val = g.Sum(i => i.TotalAmount - i.PaidAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Val, ct);

        var unbilledByCus = await _db.ShipmentCharges
            .Where(c => !c.IsInvoiced && c.Shipment.ShipmentStatus != "Cancelled")
            .GroupBy(c => c.Shipment.CustomerId)
            .Select(g => new { g.Key, Val = g.Sum(c => c.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Val, ct);

        var codHoldingByCus = await _db.CODCollections
            .Where(c => c.Status == "VerifiedByCashier")
            .GroupBy(c => c.Shipment.CustomerId)
            .Select(g => new { g.Key, Val = g.Sum(c => c.CollectedAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Val, ct);

        foreach (var cid in customerIds)
        {
            var outAmt = Math.Max(0m, outstandingByCus.GetValueOrDefault(cid, 0m));
            var unbAmt = Math.Max(0m, unbilledByCus.GetValueOrDefault(cid, 0m));
            var codAmt = Math.Max(0m, codHoldingByCus.GetValueOrDefault(cid, 0m));

            if (existingBalances.TryGetValue(cid, out var bal))
            {
                bal.CurrentOutstandingBalance = outAmt;
                bal.UnbilledChargesAmount = unbAmt;
                bal.TotalCodHoldingAmount = codAmt;
                bal.LastUpdatedDate = DateTime.UtcNow;
            }
            else if (outAmt > 0 || unbAmt > 0 || codAmt > 0)
            {
                _db.CustomerBalances.Add(new CustomerBalance
                {
                    CustomerId = cid,
                    CurrentOutstandingBalance = outAmt,
                    UnbilledChargesAmount = unbAmt,
                    TotalCodHoldingAmount = codAmt,
                    LastUpdatedDate = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 21. Notifications, BackgroundJobs, OutboxMessages & AuditLogs
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SeedNotificationsJobsAndAuditAsync(DevSeedScale scale, bool isIncremental, CancellationToken ct)
    {
        if (!isIncremental && await _db.Notifications.AnyAsync(ct)) return;

        var users = await _db.UserAccounts.Take(10).ToListAsync(ct);
        var branches = await _db.Branches.ToListAsync(ct);
        var templates = await _db.NotificationTemplates.ToListAsync(ct);

        if (users.Count > 0 && templates.Count > 0)
        {
            var notifCount = isIncremental ? 20 : (scale == DevSeedScale.Large ? 180 : 110);
            var notifs = new List<Domain.Entities.Notification.Notification>(notifCount);
            for (var i = 0; i < notifCount; i++)
            {
                var u = users[i % users.Count];
                var tmpl = templates[i % templates.Count];
                var ts = DaysAgo(RndInt(0, 45));
                var isRead = i % 5 != 0;

                var notif = new Domain.Entities.Notification.Notification
                {
                    TemplateId = tmpl.Id,
                    RecipientUserId = u.Id,
                    RecipientEmail = u.Email,
                    Title = $"[{tmpl.Name}] Thông báo vận hành #{i + 1}",
                    Content = $"Hệ thống ghi nhận sự kiện {tmpl.Name} được xử lý tự động.",
                    Channel = "InApp",
                    Priority = i % 10 == 0 ? "High" : "Normal",
                    Status = "Delivered",
                    NotificationType = tmpl.Code,
                    BranchId = Pick(branches).Id,
                    IsRead = isRead,
                    ReadAt = isRead ? ts.AddMinutes(15) : null,
                    CreatedBy = "DevSeed"
                };
                notif.Deliveries.Add(new NotificationDelivery
                {
                    Channel = "InApp",
                    AttemptNumber = 1,
                    AttemptedAt = ts,
                    Status = "Success",
                    GatewayResponse = "Delivered to InApp inbox"
                });
                notifs.Add(notif);
            }
            _db.Notifications.AddRange(notifs);
            await _db.SaveChangesAsync(ct);
        }

        if (!isIncremental && !await _db.BackgroundJobLogs.AnyAsync(ct))
        {
            var jobNames = new[] { "OutboxDispatchJob", "ScheduledReportJob", "CarrierTrackingPollJob", "CodOverdueReminderJob", "InvoiceAgingCheckJob" };
            for (var i = 0; i < 40; i++)
            {
                var st = DaysAgo(i / 2);
                var status = i == 3 ? "Warning" : "Success";
                _db.BackgroundJobLogs.Add(new BackgroundJobLog
                {
                    JobName = jobNames[i % jobNames.Length],
                    ExecutionId = Guid.NewGuid().ToString("N"),
                    TriggerType = "Scheduled",
                    StartedAt = st,
                    CompletedAt = st.AddSeconds(RndInt(2, 18)),
                    Status = status,
                    RecordsProcessed = RndInt(10, 150),
                    RecordsSucceeded = RndInt(10, 150),
                    RecordsFailed = status == "Warning" ? 1 : 0,
                    LogDetails = "Completed periodic execution"
                });
            }

            for (var i = 0; i < 50; i++)
            {
                var dt = DaysAgo(RndInt(1, 60));
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = Pick(new[] { "OrderConfirmedEvent", "ShipmentStatusChangedEvent", "DeliveryCompletedEvent", "InvoiceIssuedEvent", "CodCollectedEvent" }),
                    AggregateType = "Shipment",
                    AggregateId = (1000 + i).ToString(),
                    PayloadJson = $"{{\"seeded\":true,\"seq\":{i + 1}}}",
                    Status = "Published",
                    ProcessedAt = dt.AddSeconds(2),
                    CreatedAt = dt
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        var auditCount = isIncremental ? 15 : 80;
        var adminUser = users.FirstOrDefault();
        var auditLogs = new List<AuditLog>(auditCount);
        for (var i = 0; i < auditCount; i++)
        {
            auditLogs.Add(new AuditLog
            {
                UserId = adminUser?.Id,
                Username = adminUser?.Username ?? "admin",
                UserRole = "SUPER_ADMIN",
                Action = Pick(new[] { "Create", "Update", "StatusChange" }),
                TableName = Pick(new[] { "Orders", "Shipments", "Invoices", "CODCollections", "WarehouseReceipts" }),
                SchemaName = Pick(new[] { "sales", "shipping", "finance", "warehouse" }),
                PrimaryKey = (1000 + i).ToString(),
                IpAddress = "127.0.0.1",
                Timestamp = DaysAgo(RndInt(0, 60)),
                CreatedBy = "DevSeed"
            });
        }
        _db.AuditLogs.AddRange(auditLogs);
        await _db.SaveChangesAsync(ct);
    }

    private async Task UpdateNumberSequencesToMaxAsync(CancellationToken ct)
    {
        var sequences = await _db.NumberSequences.ToListAsync(ct);
        var orderNext = await _db.Orders.CountAsync(ct) + 2000;
        var shipNext = await _db.Shipments.CountAsync(ct) + 2000;
        var invNext = await _db.Invoices.CountAsync(ct) + 2000;
        var payNext = await _db.Payments.CountAsync(ct) + 2000;
        var fleNext = await _db.FinancialLedgerEntries.CountAsync(ct) + 2000;
        var rcpNext = await _db.WarehouseReceipts.CountAsync(ct) + 2000;
        var dspNext = await _db.WarehouseDispatches.CountAsync(ct) + 2000;
        var adjNext = await _db.StockAdjustments.CountAsync(ct) + 2000;
        var trfNext = await _db.StockTransfers.CountAsync(ct) + 2000;
        var txnNext = await _db.InventoryTransactions.CountAsync(ct) + 2000;
        var pkpNext = await _db.PickupRequests.CountAsync(ct) + 2000;
        var rtnNext = await _db.ReturnRequests.CountAsync(ct) + 2000;
        var remNext = await _db.CODRemittances.CountAsync(ct) + 2000;

        foreach (var seq in sequences)
        {
            var target = seq.Scope switch
            {
                "ORDER" => orderNext,
                "WAYBILL" => shipNext,
                "INV" => invNext,
                "PAY" => payNext,
                "FLE" => fleNext,
                "RECEIPT" => rcpNext,
                "DISPATCH" => dspNext,
                "ADJUST" => adjNext,
                "TRANSFER" => trfNext,
                "INV-TXN" => txnNext,
                "PICKUP" => pkpNext,
                "RETURN" => rtnNext,
                "REMITTANCE" => remNext,
                _ => seq.NextNumber
            };
            if (seq.NextNumber < target)
                seq.NextNumber = target;
        }
        await _db.SaveChangesAsync(ct);
    }
}
