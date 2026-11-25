using System.Text.Json;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Sales.DTOs;
using Logistics.Server.Application.Modules.Sales.Services;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Sales;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

#region Order Service
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IOutboxService? _outbox;

    public OrderService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        INumberSequenceService numberSequenceService,
        IOutboxService? outbox = null)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _numberSequenceService = numberSequenceService;
        _outbox = outbox;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private IQueryable<Order> ApplyBranchFilter(IQueryable<Order> query)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return query;
        }

        var accessibleBranchIds = _currentUserService.AccessibleBranchIds;
        var defaultBranchId = _currentUserService.DefaultBranchId;

        return query.Where(o => accessibleBranchIds.Contains(o.BranchId) || (defaultBranchId.HasValue && o.BranchId == defaultBranchId.Value));
    }

    private async Task<Order> ResolveOrderEntityAsync(string identifier, bool asNoTracking, CancellationToken cancellationToken, bool includeDetails = false)
    {
        var query = asNoTracking ? _context.Orders.AsNoTracking() : _context.Orders.AsQueryable();

        query = query
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Currency);

        if (includeDetails)
        {
            query = query
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.UnitOfMeasure)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Product)
                .Include(o => o.OrderPackages)
                .Include(o => o.OrderAddresses)
                    .ThenInclude(a => a.Country)
                .Include(o => o.OrderAddresses)
                    .ThenInclude(a => a.Province)
                .Include(o => o.OrderAddresses)
                    .ThenInclude(a => a.District)
                .Include(o => o.OrderAddresses)
                    .ThenInclude(a => a.Ward);
        }

        Order? order;
        if (long.TryParse(identifier, out var id))
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(identifier, out var publicId))
        {
            order = await query.FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
        }
        else
        {
            order = await query.FirstOrDefaultAsync(o => o.OrderNumber == identifier, cancellationToken);
        }

        if (order == null)
        {
            throw new NotFoundException("Order", identifier);
        }

        EnsureBranchAccess(order.BranchId);
        return order;
    }

    private async Task<long> ResolveCurrencyIdAsync(long? currencyId, CancellationToken cancellationToken)
    {
        if (currencyId.HasValue && currencyId.Value > 0)
        {
            var exists = await _context.Currencies.AnyAsync(c => c.Id == currencyId.Value, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("Currency", currencyId.Value);
            }
            return currencyId.Value;
        }

        var defaultCurrency = await _context.Currencies.FirstOrDefaultAsync(c => c.IsActive, cancellationToken)
                              ?? await _context.Currencies.FirstOrDefaultAsync(cancellationToken);

        if (defaultCurrency != null)
        {
            return defaultCurrency.Id;
        }

        var vnd = new Currency
        {
            Code = "VND",
            Name = "Vietnamese Dong",
            Symbol = "₫",
            DecimalPlaces = 0,
            IsActive = true
        };
        await _context.Currencies.AddAsync(vnd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vnd.Id;
    }

    private async Task<long> ResolveCountryIdAsync(long? countryId, CancellationToken cancellationToken)
    {
        if (countryId.HasValue && countryId.Value > 0)
        {
            var exists = await _context.Countries.AnyAsync(c => c.Id == countryId.Value, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("Country", countryId.Value);
            }
            return countryId.Value;
        }

        var defaultCountry = await _context.Countries.FirstOrDefaultAsync(cancellationToken);
        if (defaultCountry != null)
        {
            return defaultCountry.Id;
        }

        var vn = new Country
        {
            Code = "VN",
            Name = "Vietnam",
            IsActive = true
        };
        await _context.Countries.AddAsync(vn, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return vn.Id;
    }

    private async Task<long> ResolveUnitOfMeasureIdAsync(long? uomId, CancellationToken cancellationToken)
    {
        if (uomId.HasValue && uomId.Value > 0)
        {
            var exists = await _context.UnitsOfMeasure.AnyAsync(u => u.Id == uomId.Value, cancellationToken);
            if (!exists)
            {
                throw new NotFoundException("UnitOfMeasure", uomId.Value);
            }
            return uomId.Value;
        }

        var defaultUom = await _context.UnitsOfMeasure.FirstOrDefaultAsync(u => u.IsActive, cancellationToken)
                         ?? await _context.UnitsOfMeasure.FirstOrDefaultAsync(cancellationToken);

        if (defaultUom != null)
        {
            return defaultUom.Id;
        }

        var pcs = new UnitOfMeasure
        {
            Code = "PCS",
            Name = "Piece",
            Type = "Count",
            IsActive = true
        };
        await _context.UnitsOfMeasure.AddAsync(pcs, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return pcs.Id;
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(
        string? search,
        string? orderStatus,
        string? paymentStatus,
        long? customerId,
        long? branchId,
        DateTime? dateFrom,
        DateTime? dateTo,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking();
        query = ApplyBranchFilter(query);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(term) ||
                o.Customer.Name.Contains(term) ||
                o.Customer.CustomerCode.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(orderStatus))
        {
            query = query.Where(o => o.OrderStatus == orderStatus);
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus))
        {
            query = query.Where(o => o.PaymentStatus == paymentStatus);
        }

        if (customerId.HasValue && customerId.Value > 0)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (branchId.HasValue && branchId.Value > 0)
        {
            EnsureBranchAccess(branchId.Value);
            query = query.Where(o => o.BranchId == branchId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(o => o.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(o => o.OrderDate <= dateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                PublicId = o.PublicId,
                OrderNumber = o.OrderNumber,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer.Name,
                CustomerCode = o.Customer.CustomerCode,
                BranchId = o.BranchId,
                BranchName = o.Branch.Name,
                BranchCode = o.Branch.Code,
                OrderDate = o.OrderDate,
                RequestedPickupDate = o.RequestedPickupDate,
                RequiredDeliveryDate = o.RequiredDeliveryDate,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalWeightKg = o.TotalWeightKg,
                TotalVolumeCbm = o.TotalVolumeCbm,
                DeclaredValue = o.DeclaredValue,
                CurrencyId = o.CurrencyId,
                CurrencyCode = o.Currency.Code,
                CodAmount = o.CodAmount,
                Notes = o.Notes,
                CancellationReason = o.CancellationReason,
                CancelledAt = o.CancelledAt,
                CancelledBy = o.CancelledBy,
                ItemCount = o.OrderItems.Count,
                PackageCount = o.OrderPackages.Count,
                CreatedAt = o.CreatedAt,
                CreatedBy = o.CreatedBy,
                UpdatedAt = o.UpdatedAt,
                UpdatedBy = o.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>(items, totalCount, request.Page, request.PageSize);
    }

    public async Task<OrderDetailDto?> GetByIdOrPublicIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderEntityAsync(identifier, true, cancellationToken, true);
        return MapToDetailDto(order);
    }

    public async Task<OrderDetailDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await GetByIdOrPublicIdAsync(orderNumber, cancellationToken);
    }

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Verify customer
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException("Customer", request.CustomerId);
        }

        // 2. Resolve branch
        var branchId = request.BranchId ?? _currentUserService.DefaultBranchId ?? customer.BranchId;
        if (branchId <= 0)
        {
            var firstBranch = await _context.Branches.FirstOrDefaultAsync(cancellationToken);
            branchId = firstBranch?.Id ?? throw new ValidationException("BranchId", "A valid branch is required to create an order.");
        }

        EnsureBranchAccess(branchId);

        // 3. Resolve currency
        var currencyId = await ResolveCurrencyIdAsync(request.CurrencyId, cancellationToken);

        // 4. Generate order number
        var orderNumber = await _numberSequenceService.GetNextNumberAsync("ORDER", cancellationToken);

        // 5. Initialize order entity
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            BranchId = branchId,
            OrderDate = DateTime.UtcNow,
            RequestedPickupDate = request.RequestedPickupDate,
            RequiredDeliveryDate = request.RequiredDeliveryDate,
            OrderStatus = "Draft",
            PaymentStatus = "Pending",
            CurrencyId = currencyId,
            CodAmount = request.CodAmount,
            Notes = request.Notes
        };

        // 6. Add addresses if provided
        if (request.PickupAddress != null)
        {
            var pickupCountryId = await ResolveCountryIdAsync(request.PickupAddress.CountryId, cancellationToken);
            order.OrderAddresses.Add(new OrderAddress
            {
                AddressType = "Sender",
                ContactName = request.PickupAddress.ContactName,
                PhoneNumber = request.PickupAddress.PhoneNumber,
                Email = request.PickupAddress.Email,
                AddressLine1 = request.PickupAddress.AddressLine1,
                AddressLine2 = request.PickupAddress.AddressLine2,
                CountryId = pickupCountryId,
                ProvinceId = request.PickupAddress.ProvinceId,
                DistrictId = request.PickupAddress.DistrictId,
                WardId = request.PickupAddress.WardId,
                PostalCode = request.PickupAddress.PostalCode,
                FormattedAddress = FormatAddress(request.PickupAddress)
            });
        }

        if (request.DeliveryAddress != null)
        {
            var deliveryCountryId = await ResolveCountryIdAsync(request.DeliveryAddress.CountryId, cancellationToken);
            order.OrderAddresses.Add(new OrderAddress
            {
                AddressType = "Recipient",
                ContactName = request.DeliveryAddress.ContactName,
                PhoneNumber = request.DeliveryAddress.PhoneNumber,
                Email = request.DeliveryAddress.Email,
                AddressLine1 = request.DeliveryAddress.AddressLine1,
                AddressLine2 = request.DeliveryAddress.AddressLine2,
                CountryId = deliveryCountryId,
                ProvinceId = request.DeliveryAddress.ProvinceId,
                DistrictId = request.DeliveryAddress.DistrictId,
                WardId = request.DeliveryAddress.WardId,
                PostalCode = request.DeliveryAddress.PostalCode,
                FormattedAddress = FormatAddress(request.DeliveryAddress)
            });
        }

        // 7. Add items if provided
        if (request.Items != null && request.Items.Count > 0)
        {
            foreach (var itemReq in request.Items)
            {
                string productName = itemReq.ProductName ?? string.Empty;
                string? sku = itemReq.Sku;
                long uomId = 0;

                if (itemReq.ProductId.HasValue && itemReq.ProductId.Value > 0)
                {
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == itemReq.ProductId.Value, cancellationToken);
                    if (product != null)
                    {
                        if (string.IsNullOrWhiteSpace(productName)) productName = product.Name;
                        sku ??= product.Sku;
                        uomId = product.UnitOfMeasureId;
                    }
                }

                if (string.IsNullOrWhiteSpace(productName))
                {
                    productName = "Order Item";
                }

                if (itemReq.UnitOfMeasureId.HasValue && itemReq.UnitOfMeasureId.Value > 0)
                {
                    uomId = itemReq.UnitOfMeasureId.Value;
                }

                uomId = await ResolveUnitOfMeasureIdAsync(uomId > 0 ? uomId : null, cancellationToken);

                var orderItem = new OrderItem
                {
                    ProductId = itemReq.ProductId > 0 ? itemReq.ProductId : null,
                    ProductName = productName,
                    Sku = sku,
                    Quantity = itemReq.Quantity > 0 ? itemReq.Quantity : 1,
                    UnitOfMeasureId = uomId,
                    UnitPrice = itemReq.UnitPrice,
                    TotalPrice = (itemReq.Quantity > 0 ? itemReq.Quantity : 1) * itemReq.UnitPrice,
                    DeclaredValue = itemReq.DeclaredValue,
                    WeightKg = itemReq.WeightKg,
                    Notes = itemReq.Notes
                };

                order.OrderItems.Add(orderItem);
            }
        }

        // 8. Add packages if provided
        if (request.Packages != null && request.Packages.Count > 0)
        {
            int pkgIdx = 1;
            foreach (var pkgReq in request.Packages)
            {
                var pkgCode = !string.IsNullOrWhiteSpace(pkgReq.PackageCode)
                    ? pkgReq.PackageCode
                    : $"{orderNumber}-PKG{pkgIdx:D2}";

                var volWeight = Math.Round((pkgReq.LengthCm * pkgReq.WidthCm * pkgReq.HeightCm) / 5000m, 3, MidpointRounding.AwayFromZero);

                var orderPkg = new OrderPackage
                {
                    PackageCode = pkgCode,
                    WeightKg = pkgReq.WeightKg,
                    LengthCm = pkgReq.LengthCm,
                    WidthCm = pkgReq.WidthCm,
                    HeightCm = pkgReq.HeightCm,
                    VolumetricWeightKg = volWeight,
                    DeclaredValue = pkgReq.DeclaredValue,
                    Description = pkgReq.Description,
                    Barcode = pkgReq.Barcode,
                    SpecialInstructions = pkgReq.SpecialInstructions
                };

                order.OrderPackages.Add(orderPkg);
                pkgIdx++;
            }
        }

        // 9. Recalculate totals
        RecalculateTotals(order);

        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (_outbox != null)
        {
            _outbox.Enqueue(
                new OrderCreatedIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId, order.BranchId),
                "Order", order.Id.ToString());
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 10. Audit log
        await _auditService.RecordAsync(
            "CREATE",
            "sales",
            "Orders",
            order.Id.ToString(),
            null,
            new { order.OrderNumber, order.CustomerId, order.BranchId, order.OrderStatus },
            null,
            cancellationToken);

        return await GetByIdOrPublicIdAsync(order.Id.ToString(), cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve created order.");
    }

    public async Task<OrderDetailDto> UpdateAsync(string identifier, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderEntityAsync(identifier, false, cancellationToken, true);

        if (order.OrderStatus is "Cancelled" or "Delivered" or "Shipped")
        {
            throw new ConflictException($"Cannot modify order in '{order.OrderStatus}' status.");
        }

        var oldValues = new { order.RequestedPickupDate, order.RequiredDeliveryDate, order.CodAmount, order.Notes };

        order.RequestedPickupDate = request.RequestedPickupDate;
        order.RequiredDeliveryDate = request.RequiredDeliveryDate;
        order.CodAmount = request.CodAmount;
        order.Notes = request.Notes;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "UPDATE",
            "sales",
            "Orders",
            order.Id.ToString(),
            oldValues,
            new { order.RequestedPickupDate, order.RequiredDeliveryDate, order.CodAmount, order.Notes },
            null,
            cancellationToken);

        return await GetByIdOrPublicIdAsync(order.Id.ToString(), cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve updated order.");
    }

    public async Task<OrderDetailDto> ConfirmAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderEntityAsync(identifier, false, cancellationToken, true);

        if (order.OrderStatus != "Draft")
        {
            throw new ConflictException($"Only Draft orders can be confirmed. Current status: '{order.OrderStatus}'.");
        }

        var oldStatus = order.OrderStatus;
        order.OrderStatus = "Confirmed";

        _outbox?.Enqueue(
            new OrderConfirmedIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId, order.BranchId, order.CodAmount),
            "Order", order.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "CONFIRM",
            "sales",
            "Orders",
            order.Id.ToString(),
            new { OrderStatus = oldStatus },
            new { OrderStatus = order.OrderStatus },
            null,
            cancellationToken);

        return await GetByIdOrPublicIdAsync(order.Id.ToString(), cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve confirmed order.");
    }

    public async Task<OrderDetailDto> CancelAsync(string identifier, CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderEntityAsync(identifier, false, cancellationToken, true);

        if (order.OrderStatus == "Cancelled")
        {
            throw new ConflictException("Order is already cancelled.");
        }

        if (order.OrderStatus == "Delivered")
        {
            throw new ConflictException("Cannot cancel an order that has already been delivered.");
        }

        // Check if any linked shipments have already transitioned to InTransit or Delivered
        var hasActiveShipments = await _context.Shipments
            .AnyAsync(s => s.OrderId == order.Id && (s.ShipmentStatus == "InTransit" || s.ShipmentStatus == "Delivered"), cancellationToken);

        if (hasActiveShipments)
        {
            throw new ConflictException("Cannot cancel order because linked shipments are already in transit or delivered.");
        }

        var oldStatus = order.OrderStatus;
        order.OrderStatus = "Cancelled";
        order.CancellationReason = request.Reason;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = _currentUserService.Username ?? _currentUserService.UserId?.ToString() ?? "System";

        _outbox?.Enqueue(
            new OrderCancelledIntegrationEvent(order.Id, order.OrderNumber, order.CustomerId, order.BranchId, request.Reason),
            "Order", order.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "CANCEL",
            "sales",
            "Orders",
            order.Id.ToString(),
            new { OrderStatus = oldStatus },
            new { order.OrderStatus, order.CancellationReason, order.CancelledAt, order.CancelledBy },
            null,
            cancellationToken);

        return await GetByIdOrPublicIdAsync(order.Id.ToString(), cancellationToken)
               ?? throw new InvalidOperationException("Failed to retrieve cancelled order.");
    }

    public static void RecalculateTotals(Order order)
    {
        if (order.OrderPackages != null && order.OrderPackages.Count > 0)
        {
            order.TotalWeightKg = order.OrderPackages.Sum(p => p.WeightKg);
            order.TotalVolumeCbm = order.OrderPackages.Sum(p => (p.LengthCm * p.WidthCm * p.HeightCm) / 1000000m);
        }
        else if (order.OrderItems != null && order.OrderItems.Count > 0)
        {
            order.TotalWeightKg = order.OrderItems.Sum(i => i.WeightKg);
            order.TotalVolumeCbm = 0;
        }
        else
        {
            order.TotalWeightKg = 0;
            order.TotalVolumeCbm = 0;
        }

        if (order.OrderPackages != null && order.OrderPackages.Count > 0)
        {
            order.DeclaredValue = order.OrderPackages.Sum(p => p.DeclaredValue);
        }
        else if (order.OrderItems != null && order.OrderItems.Count > 0)
        {
            order.DeclaredValue = order.OrderItems.Sum(i => i.DeclaredValue > 0 ? i.DeclaredValue : i.TotalPrice);
        }
        else
        {
            order.DeclaredValue = 0;
        }
    }

    private static string FormatAddress(OrderAddressRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.FormattedAddress))
        {
            return req.FormattedAddress;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(req.AddressLine1)) parts.Add(req.AddressLine1.Trim());
        if (!string.IsNullOrWhiteSpace(req.AddressLine2)) parts.Add(req.AddressLine2.Trim());
        return parts.Count > 0 ? string.Join(", ", parts) : "N/A";
    }

    private static OrderDetailDto MapToDetailDto(Order o)
    {
        return new OrderDetailDto
        {
            Id = o.Id,
            PublicId = o.PublicId,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.Name ?? string.Empty,
            CustomerCode = o.Customer?.CustomerCode ?? string.Empty,
            BranchId = o.BranchId,
            BranchName = o.Branch?.Name ?? string.Empty,
            BranchCode = o.Branch?.Code ?? string.Empty,
            OrderDate = o.OrderDate,
            RequestedPickupDate = o.RequestedPickupDate,
            RequiredDeliveryDate = o.RequiredDeliveryDate,
            OrderStatus = o.OrderStatus,
            PaymentStatus = o.PaymentStatus,
            TotalWeightKg = o.TotalWeightKg,
            TotalVolumeCbm = o.TotalVolumeCbm,
            DeclaredValue = o.DeclaredValue,
            CurrencyId = o.CurrencyId,
            CurrencyCode = o.Currency?.Code ?? string.Empty,
            CodAmount = o.CodAmount,
            Notes = o.Notes,
            CancellationReason = o.CancellationReason,
            CancelledAt = o.CancelledAt,
            CancelledBy = o.CancelledBy,
            ItemCount = o.OrderItems?.Count ?? 0,
            PackageCount = o.OrderPackages?.Count ?? 0,
            CreatedAt = o.CreatedAt,
            CreatedBy = o.CreatedBy,
            UpdatedAt = o.UpdatedAt,
            UpdatedBy = o.UpdatedBy,
            Items = o.OrderItems?.Select(i => new OrderItemDto
            {
                Id = i.Id,
                PublicId = i.PublicId,
                OrderId = i.OrderId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                Quantity = i.Quantity,
                UnitOfMeasureId = i.UnitOfMeasureId,
                UnitOfMeasureCode = i.UnitOfMeasure?.Code ?? string.Empty,
                UnitOfMeasureName = i.UnitOfMeasure?.Name ?? string.Empty,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                DeclaredValue = i.DeclaredValue,
                WeightKg = i.WeightKg,
                Notes = i.Notes,
                CreatedAt = i.CreatedAt,
                CreatedBy = i.CreatedBy,
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = i.UpdatedBy
            }).ToList() ?? [],
            Packages = o.OrderPackages?.Select(p => new OrderPackageDto
            {
                Id = p.Id,
                PublicId = p.PublicId,
                OrderId = p.OrderId,
                PackageCode = p.PackageCode,
                WeightKg = p.WeightKg,
                LengthCm = p.LengthCm,
                WidthCm = p.WidthCm,
                HeightCm = p.HeightCm,
                VolumetricWeightKg = p.VolumetricWeightKg,
                DeclaredValue = p.DeclaredValue,
                Description = p.Description,
                Barcode = p.Barcode,
                SpecialInstructions = p.SpecialInstructions,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy,
                UpdatedAt = p.UpdatedAt,
                UpdatedBy = p.UpdatedBy
            }).ToList() ?? [],
            Addresses = o.OrderAddresses?.Select(a => new OrderAddressDto
            {
                Id = a.Id,
                OrderId = a.OrderId,
                AddressType = a.AddressType,
                ContactName = a.ContactName,
                PhoneNumber = a.PhoneNumber,
                Email = a.Email,
                AddressLine1 = a.AddressLine1,
                AddressLine2 = a.AddressLine2,
                CountryId = a.CountryId,
                CountryName = a.Country?.Name ?? string.Empty,
                ProvinceId = a.ProvinceId,
                ProvinceName = a.Province?.Name,
                DistrictId = a.DistrictId,
                DistrictName = a.District?.Name,
                WardId = a.WardId,
                WardName = a.Ward?.Name,
                PostalCode = a.PostalCode,
                FormattedAddress = a.FormattedAddress
            }).ToList() ?? []
        };
    }
}
#endregion

#region Order Item Service
public class OrderItemService : IOrderItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public OrderItemService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Order> ResolveOrderAsync(string orderIdentifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Orders.AsNoTracking() : _context.Orders.AsQueryable();
        query = query.Include(o => o.OrderItems).ThenInclude(i => i.UnitOfMeasure);

        Order? order;
        if (long.TryParse(orderIdentifier, out var id))
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(orderIdentifier, out var publicId))
        {
            order = await query.FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
        }
        else
        {
            order = await query.FirstOrDefaultAsync(o => o.OrderNumber == orderIdentifier, cancellationToken);
        }

        if (order == null)
        {
            throw new NotFoundException("Order", orderIdentifier);
        }

        EnsureBranchAccess(order.BranchId);
        return order;
    }

    public async Task<IReadOnlyList<OrderItemDto>> GetItemsAsync(string orderIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        return order.OrderItems.Select(i => new OrderItemDto
        {
            Id = i.Id,
            PublicId = i.PublicId,
            OrderId = i.OrderId,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Sku = i.Sku,
            Quantity = i.Quantity,
            UnitOfMeasureId = i.UnitOfMeasureId,
            UnitOfMeasureCode = i.UnitOfMeasure?.Code ?? string.Empty,
            UnitOfMeasureName = i.UnitOfMeasure?.Name ?? string.Empty,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice,
            DeclaredValue = i.DeclaredValue,
            WeightKg = i.WeightKg,
            Notes = i.Notes,
            CreatedAt = i.CreatedAt,
            CreatedBy = i.CreatedBy,
            UpdatedAt = i.UpdatedAt,
            UpdatedBy = i.UpdatedBy
        }).ToList();
    }

    public async Task<OrderItemDto?> GetItemByIdAsync(string orderIdentifier, string itemIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        OrderItem? item = null;
        if (long.TryParse(itemIdentifier, out var itemId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        }
        else if (Guid.TryParse(itemIdentifier, out var publicId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.PublicId == publicId);
        }

        if (item == null)
        {
            return null;
        }

        return new OrderItemDto
        {
            Id = item.Id,
            PublicId = item.PublicId,
            OrderId = item.OrderId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Sku = item.Sku,
            Quantity = item.Quantity,
            UnitOfMeasureId = item.UnitOfMeasureId,
            UnitOfMeasureCode = item.UnitOfMeasure?.Code ?? string.Empty,
            UnitOfMeasureName = item.UnitOfMeasure?.Name ?? string.Empty,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            DeclaredValue = item.DeclaredValue,
            WeightKg = item.WeightKg,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            CreatedBy = item.CreatedBy,
            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy
        };
    }

    public async Task<OrderItemDto> AddItemAsync(string orderIdentifier, CreateOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus != "Draft")
        {
            throw new ConflictException($"Order items can only be modified when order is in 'Draft' status. Current: '{order.OrderStatus}'.");
        }

        string productName = request.ProductName ?? string.Empty;
        string? sku = request.Sku;
        long uomId = request.UnitOfMeasureId ?? 0;

        if (request.ProductId.HasValue && request.ProductId.Value > 0)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId.Value, cancellationToken);
            if (product != null)
            {
                if (string.IsNullOrWhiteSpace(productName)) productName = product.Name;
                sku ??= product.Sku;
                if (uomId <= 0) uomId = product.UnitOfMeasureId;
            }
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            productName = "Order Item";
        }

        if (uomId <= 0)
        {
            var defaultUom = await _context.UnitsOfMeasure.FirstOrDefaultAsync(cancellationToken);
            uomId = defaultUom?.Id ?? 1;
        }

        var item = new OrderItem
        {
            OrderId = order.Id,
            ProductId = request.ProductId > 0 ? request.ProductId : null,
            ProductName = productName,
            Sku = sku,
            Quantity = request.Quantity > 0 ? request.Quantity : 1,
            UnitOfMeasureId = uomId,
            UnitPrice = request.UnitPrice,
            TotalPrice = (request.Quantity > 0 ? request.Quantity : 1) * request.UnitPrice,
            DeclaredValue = request.DeclaredValue,
            WeightKg = request.WeightKg,
            Notes = request.Notes
        };

        order.OrderItems.Add(item);
        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "CREATE",
            "sales",
            "OrderItems",
            item.Id.ToString(),
            null,
            new { item.OrderId, item.ProductName, item.Quantity, item.UnitPrice },
            null,
            cancellationToken);

        var uom = await _context.UnitsOfMeasure.FindAsync([item.UnitOfMeasureId], cancellationToken);

        return new OrderItemDto
        {
            Id = item.Id,
            PublicId = item.PublicId,
            OrderId = item.OrderId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Sku = item.Sku,
            Quantity = item.Quantity,
            UnitOfMeasureId = item.UnitOfMeasureId,
            UnitOfMeasureCode = uom?.Code ?? string.Empty,
            UnitOfMeasureName = uom?.Name ?? string.Empty,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            DeclaredValue = item.DeclaredValue,
            WeightKg = item.WeightKg,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            CreatedBy = item.CreatedBy,
            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy
        };
    }

    public async Task<OrderItemDto> UpdateItemAsync(string orderIdentifier, string itemIdentifier, UpdateOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus != "Draft")
        {
            throw new ConflictException($"Order items can only be modified when order is in 'Draft' status. Current: '{order.OrderStatus}'.");
        }

        OrderItem? item = null;
        if (long.TryParse(itemIdentifier, out var itemId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        }
        else if (Guid.TryParse(itemIdentifier, out var publicId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.PublicId == publicId);
        }

        if (item == null)
        {
            throw new NotFoundException("OrderItem", itemIdentifier);
        }

        var oldValues = new { item.Quantity, item.UnitPrice, item.TotalPrice, item.WeightKg, item.DeclaredValue };

        item.Quantity = request.Quantity;
        item.UnitPrice = request.UnitPrice;
        item.TotalPrice = request.Quantity * request.UnitPrice;
        item.DeclaredValue = request.DeclaredValue;
        item.WeightKg = request.WeightKg;
        item.Notes = request.Notes;

        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "UPDATE",
            "sales",
            "OrderItems",
            item.Id.ToString(),
            oldValues,
            new { item.OrderId, item.Quantity, item.UnitPrice, item.TotalPrice },
            null,
            cancellationToken);

        var uom = await _context.UnitsOfMeasure.FindAsync([item.UnitOfMeasureId], cancellationToken);

        return new OrderItemDto
        {
            Id = item.Id,
            PublicId = item.PublicId,
            OrderId = item.OrderId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Sku = item.Sku,
            Quantity = item.Quantity,
            UnitOfMeasureId = item.UnitOfMeasureId,
            UnitOfMeasureCode = uom?.Code ?? string.Empty,
            UnitOfMeasureName = uom?.Name ?? string.Empty,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            DeclaredValue = item.DeclaredValue,
            WeightKg = item.WeightKg,
            Notes = item.Notes,
            CreatedAt = item.CreatedAt,
            CreatedBy = item.CreatedBy,
            UpdatedAt = item.UpdatedAt,
            UpdatedBy = item.UpdatedBy
        };
    }

    public async Task RemoveItemAsync(string orderIdentifier, string itemIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus != "Draft")
        {
            throw new ConflictException($"Order items can only be modified when order is in 'Draft' status. Current: '{order.OrderStatus}'.");
        }

        OrderItem? item = null;
        if (long.TryParse(itemIdentifier, out var itemId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.Id == itemId);
        }
        else if (Guid.TryParse(itemIdentifier, out var publicId))
        {
            item = order.OrderItems.FirstOrDefault(i => i.PublicId == publicId);
        }

        if (item == null)
        {
            throw new NotFoundException("OrderItem", itemIdentifier);
        }

        _context.OrderItems.Remove(item);
        order.OrderItems.Remove(item);

        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "DELETE",
            "sales",
            "OrderItems",
            item.Id.ToString(),
            new { item.OrderId, item.ProductName },
            null,
            null,
            cancellationToken);
    }
}
#endregion

#region Order Package Service
public class OrderPackageService : IOrderPackageService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public OrderPackageService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Order> ResolveOrderAsync(string orderIdentifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Orders.AsNoTracking() : _context.Orders.AsQueryable();
        query = query.Include(o => o.OrderPackages);

        Order? order;
        if (long.TryParse(orderIdentifier, out var id))
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(orderIdentifier, out var publicId))
        {
            order = await query.FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
        }
        else
        {
            order = await query.FirstOrDefaultAsync(o => o.OrderNumber == orderIdentifier, cancellationToken);
        }

        if (order == null)
        {
            throw new NotFoundException("Order", orderIdentifier);
        }

        EnsureBranchAccess(order.BranchId);
        return order;
    }

    public async Task<IReadOnlyList<OrderPackageDto>> GetPackagesAsync(string orderIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        return order.OrderPackages.Select(p => new OrderPackageDto
        {
            Id = p.Id,
            PublicId = p.PublicId,
            OrderId = p.OrderId,
            PackageCode = p.PackageCode,
            WeightKg = p.WeightKg,
            LengthCm = p.LengthCm,
            WidthCm = p.WidthCm,
            HeightCm = p.HeightCm,
            VolumetricWeightKg = p.VolumetricWeightKg,
            DeclaredValue = p.DeclaredValue,
            Description = p.Description,
            Barcode = p.Barcode,
            SpecialInstructions = p.SpecialInstructions,
            CreatedAt = p.CreatedAt,
            CreatedBy = p.CreatedBy,
            UpdatedAt = p.UpdatedAt,
            UpdatedBy = p.UpdatedBy
        }).ToList();
    }

    public async Task<OrderPackageDto?> GetPackageByIdAsync(string orderIdentifier, string packageIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        OrderPackage? package = null;
        if (long.TryParse(packageIdentifier, out var pkgId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.Id == pkgId);
        }
        else if (Guid.TryParse(packageIdentifier, out var publicId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PublicId == publicId);
        }
        else
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PackageCode == packageIdentifier);
        }

        if (package == null)
        {
            return null;
        }

        return new OrderPackageDto
        {
            Id = package.Id,
            PublicId = package.PublicId,
            OrderId = package.OrderId,
            PackageCode = package.PackageCode,
            WeightKg = package.WeightKg,
            LengthCm = package.LengthCm,
            WidthCm = package.WidthCm,
            HeightCm = package.HeightCm,
            VolumetricWeightKg = package.VolumetricWeightKg,
            DeclaredValue = package.DeclaredValue,
            Description = package.Description,
            Barcode = package.Barcode,
            SpecialInstructions = package.SpecialInstructions,
            CreatedAt = package.CreatedAt,
            CreatedBy = package.CreatedBy,
            UpdatedAt = package.UpdatedAt,
            UpdatedBy = package.UpdatedBy
        };
    }

    public async Task<OrderPackageDto> AddPackageAsync(string orderIdentifier, CreateOrderPackageRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus is not "Draft" and not "Confirmed")
        {
            throw new ConflictException($"Packages can only be added when order is in 'Draft' or 'Confirmed' status. Current: '{order.OrderStatus}'.");
        }

        var pkgIdx = order.OrderPackages.Count + 1;
        var pkgCode = !string.IsNullOrWhiteSpace(request.PackageCode)
            ? request.PackageCode
            : $"{order.OrderNumber}-PKG{pkgIdx:D2}";

        var volWeight = Math.Round((request.LengthCm * request.WidthCm * request.HeightCm) / 5000m, 3, MidpointRounding.AwayFromZero);

        var package = new OrderPackage
        {
            OrderId = order.Id,
            PackageCode = pkgCode,
            WeightKg = request.WeightKg,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            VolumetricWeightKg = volWeight,
            DeclaredValue = request.DeclaredValue,
            Description = request.Description,
            Barcode = request.Barcode,
            SpecialInstructions = request.SpecialInstructions
        };

        order.OrderPackages.Add(package);
        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "CREATE",
            "sales",
            "OrderPackages",
            package.Id.ToString(),
            null,
            new { package.OrderId, package.PackageCode, package.WeightKg },
            null,
            cancellationToken);

        return new OrderPackageDto
        {
            Id = package.Id,
            PublicId = package.PublicId,
            OrderId = package.OrderId,
            PackageCode = package.PackageCode,
            WeightKg = package.WeightKg,
            LengthCm = package.LengthCm,
            WidthCm = package.WidthCm,
            HeightCm = package.HeightCm,
            VolumetricWeightKg = package.VolumetricWeightKg,
            DeclaredValue = package.DeclaredValue,
            Description = package.Description,
            Barcode = package.Barcode,
            SpecialInstructions = package.SpecialInstructions,
            CreatedAt = package.CreatedAt,
            CreatedBy = package.CreatedBy,
            UpdatedAt = package.UpdatedAt,
            UpdatedBy = package.UpdatedBy
        };
    }

    public async Task<OrderPackageDto> UpdatePackageAsync(string orderIdentifier, string packageIdentifier, UpdateOrderPackageRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus is not "Draft" and not "Confirmed")
        {
            throw new ConflictException($"Packages can only be modified when order is in 'Draft' or 'Confirmed' status. Current: '{order.OrderStatus}'.");
        }

        OrderPackage? package = null;
        if (long.TryParse(packageIdentifier, out var pkgId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.Id == pkgId);
        }
        else if (Guid.TryParse(packageIdentifier, out var publicId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PublicId == publicId);
        }
        else
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PackageCode == packageIdentifier);
        }

        if (package == null)
        {
            throw new NotFoundException("OrderPackage", packageIdentifier);
        }

        var oldValues = new { package.WeightKg, package.LengthCm, package.WidthCm, package.HeightCm, package.DeclaredValue };

        var volWeight = Math.Round((request.LengthCm * request.WidthCm * request.HeightCm) / 5000m, 3, MidpointRounding.AwayFromZero);

        package.WeightKg = request.WeightKg;
        package.LengthCm = request.LengthCm;
        package.WidthCm = request.WidthCm;
        package.HeightCm = request.HeightCm;
        package.VolumetricWeightKg = volWeight;
        package.DeclaredValue = request.DeclaredValue;
        package.Description = request.Description;
        package.Barcode = request.Barcode;
        package.SpecialInstructions = request.SpecialInstructions;

        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "UPDATE",
            "sales",
            "OrderPackages",
            package.Id.ToString(),
            oldValues,
            new { package.OrderId, package.PackageCode, package.WeightKg },
            null,
            cancellationToken);

        return new OrderPackageDto
        {
            Id = package.Id,
            PublicId = package.PublicId,
            OrderId = package.OrderId,
            PackageCode = package.PackageCode,
            WeightKg = package.WeightKg,
            LengthCm = package.LengthCm,
            WidthCm = package.WidthCm,
            HeightCm = package.HeightCm,
            VolumetricWeightKg = package.VolumetricWeightKg,
            DeclaredValue = package.DeclaredValue,
            Description = package.Description,
            Barcode = package.Barcode,
            SpecialInstructions = package.SpecialInstructions,
            CreatedAt = package.CreatedAt,
            CreatedBy = package.CreatedBy,
            UpdatedAt = package.UpdatedAt,
            UpdatedBy = package.UpdatedBy
        };
    }

    public async Task RemovePackageAsync(string orderIdentifier, string packageIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus is not "Draft" and not "Confirmed")
        {
            throw new ConflictException($"Packages can only be modified when order is in 'Draft' or 'Confirmed' status. Current: '{order.OrderStatus}'.");
        }

        OrderPackage? package = null;
        if (long.TryParse(packageIdentifier, out var pkgId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.Id == pkgId);
        }
        else if (Guid.TryParse(packageIdentifier, out var publicId))
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PublicId == publicId);
        }
        else
        {
            package = order.OrderPackages.FirstOrDefault(p => p.PackageCode == packageIdentifier);
        }

        if (package == null)
        {
            throw new NotFoundException("OrderPackage", packageIdentifier);
        }

        _context.OrderPackages.Remove(package);
        order.OrderPackages.Remove(package);

        OrderService.RecalculateTotals(order);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync(
            "DELETE",
            "sales",
            "OrderPackages",
            package.Id.ToString(),
            new { package.OrderId, package.PackageCode },
            null,
            null,
            cancellationToken);
    }
}
#endregion

#region Order Address Service
public class OrderAddressService : IOrderAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public OrderAddressService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private void EnsureBranchAccess(long branchId)
    {
        if (_currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin"))
        {
            return;
        }

        if (!_currentUserService.HasBranchAccess(branchId))
        {
            throw new ForbiddenAccessException($"You do not have access to branch id '{branchId}'.");
        }
    }

    private async Task<Order> ResolveOrderAsync(string orderIdentifier, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = asNoTracking ? _context.Orders.AsNoTracking() : _context.Orders.AsQueryable();
        query = query.Include(o => o.OrderAddresses)
                     .ThenInclude(a => a.Country)
                     .Include(o => o.OrderAddresses)
                     .ThenInclude(a => a.Province)
                     .Include(o => o.OrderAddresses)
                     .ThenInclude(a => a.District)
                     .Include(o => o.OrderAddresses)
                     .ThenInclude(a => a.Ward);

        Order? order;
        if (long.TryParse(orderIdentifier, out var id))
        {
            order = await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        else if (Guid.TryParse(orderIdentifier, out var publicId))
        {
            order = await query.FirstOrDefaultAsync(o => o.PublicId == publicId, cancellationToken);
        }
        else
        {
            order = await query.FirstOrDefaultAsync(o => o.OrderNumber == orderIdentifier, cancellationToken);
        }

        if (order == null)
        {
            throw new NotFoundException("Order", orderIdentifier);
        }

        EnsureBranchAccess(order.BranchId);
        return order;
    }

    public async Task<IReadOnlyList<OrderAddressDto>> GetAddressesAsync(string orderIdentifier, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        return order.OrderAddresses.Select(a => new OrderAddressDto
        {
            Id = a.Id,
            OrderId = a.OrderId,
            AddressType = a.AddressType,
            ContactName = a.ContactName,
            PhoneNumber = a.PhoneNumber,
            Email = a.Email,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            CountryId = a.CountryId,
            CountryName = a.Country?.Name ?? string.Empty,
            ProvinceId = a.ProvinceId,
            ProvinceName = a.Province?.Name,
            DistrictId = a.DistrictId,
            DistrictName = a.District?.Name,
            WardId = a.WardId,
            WardName = a.Ward?.Name,
            PostalCode = a.PostalCode,
            FormattedAddress = a.FormattedAddress
        }).ToList();
    }

    public async Task<OrderAddressDto?> GetAddressByTypeAsync(string orderIdentifier, string addressType, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, true, cancellationToken);

        var a = order.OrderAddresses.FirstOrDefault(addr => addr.AddressType.Equals(addressType, StringComparison.OrdinalIgnoreCase));
        if (a == null) return null;

        return new OrderAddressDto
        {
            Id = a.Id,
            OrderId = a.OrderId,
            AddressType = a.AddressType,
            ContactName = a.ContactName,
            PhoneNumber = a.PhoneNumber,
            Email = a.Email,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            CountryId = a.CountryId,
            CountryName = a.Country?.Name ?? string.Empty,
            ProvinceId = a.ProvinceId,
            ProvinceName = a.Province?.Name,
            DistrictId = a.DistrictId,
            DistrictName = a.District?.Name,
            WardId = a.WardId,
            WardName = a.Ward?.Name,
            PostalCode = a.PostalCode,
            FormattedAddress = a.FormattedAddress
        };
    }

    public async Task<OrderAddressDto> SetAddressAsync(string orderIdentifier, string addressType, OrderAddressRequest request, CancellationToken cancellationToken = default)
    {
        var order = await ResolveOrderAsync(orderIdentifier, false, cancellationToken);

        if (order.OrderStatus is not "Draft" and not "Confirmed")
        {
            throw new ConflictException($"Addresses can only be modified when order is in 'Draft' or 'Confirmed' status. Current: '{order.OrderStatus}'.");
        }

        var normalizedType = addressType.Equals("Pickup", StringComparison.OrdinalIgnoreCase) || addressType.Equals("Sender", StringComparison.OrdinalIgnoreCase)
            ? "Sender"
            : "Recipient";

        long countryId = 0;
        if (request.CountryId.HasValue && request.CountryId.Value > 0)
        {
            countryId = request.CountryId.Value;
        }
        else
        {
            var defaultCountry = await _context.Countries.FirstOrDefaultAsync(cancellationToken);
            countryId = defaultCountry?.Id ?? 1;
        }

        var formatted = !string.IsNullOrWhiteSpace(request.FormattedAddress)
            ? request.FormattedAddress
            : $"{request.AddressLine1}{(string.IsNullOrWhiteSpace(request.AddressLine2) ? "" : $", {request.AddressLine2}")}";

        var address = order.OrderAddresses.FirstOrDefault(a => a.AddressType.Equals(normalizedType, StringComparison.OrdinalIgnoreCase));

        if (address == null)
        {
            address = new OrderAddress
            {
                OrderId = order.Id,
                AddressType = normalizedType,
                ContactName = request.ContactName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                CountryId = countryId,
                ProvinceId = request.ProvinceId,
                DistrictId = request.DistrictId,
                WardId = request.WardId,
                PostalCode = request.PostalCode,
                FormattedAddress = formatted
            };
            order.OrderAddresses.Add(address);
        }
        else
        {
            var oldValues = new { address.ContactName, address.PhoneNumber, address.AddressLine1, address.CountryId };

            address.ContactName = request.ContactName;
            address.PhoneNumber = request.PhoneNumber;
            address.Email = request.Email;
            address.AddressLine1 = request.AddressLine1;
            address.AddressLine2 = request.AddressLine2;
            address.CountryId = countryId;
            address.ProvinceId = request.ProvinceId;
            address.DistrictId = request.DistrictId;
            address.WardId = request.WardId;
            address.PostalCode = request.PostalCode;
            address.FormattedAddress = formatted;

            await _auditService.RecordAsync(
                "UPDATE",
                "sales",
                "OrderAddresses",
                address.Id.ToString(),
                oldValues,
                new { address.ContactName, address.PhoneNumber, address.AddressLine1 },
                null,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (address.Id > 0)
        {
            await _auditService.RecordAsync(
                "SET",
                "sales",
                "OrderAddresses",
                address.Id.ToString(),
                null,
                new { address.OrderId, address.AddressType, address.ContactName, address.PhoneNumber },
                null,
                cancellationToken);
        }

        var country = await _context.Countries.FindAsync([address.CountryId], cancellationToken);
        var province = address.ProvinceId.HasValue ? await _context.Provinces.FindAsync([address.ProvinceId.Value], cancellationToken) : null;
        var district = address.DistrictId.HasValue ? await _context.Districts.FindAsync([address.DistrictId.Value], cancellationToken) : null;
        var ward = address.WardId.HasValue ? await _context.Wards.FindAsync([address.WardId.Value], cancellationToken) : null;

        return new OrderAddressDto
        {
            Id = address.Id,
            OrderId = address.OrderId,
            AddressType = address.AddressType,
            ContactName = address.ContactName,
            PhoneNumber = address.PhoneNumber,
            Email = address.Email,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            CountryId = address.CountryId,
            CountryName = country?.Name ?? string.Empty,
            ProvinceId = address.ProvinceId,
            ProvinceName = province?.Name,
            DistrictId = address.DistrictId,
            DistrictName = district?.Name,
            WardId = address.WardId,
            WardName = ward?.Name,
            PostalCode = address.PostalCode,
            FormattedAddress = address.FormattedAddress
        };
    }
}
#endregion
