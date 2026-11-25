using System.Text.Json;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Common.Models;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Shipping.DTOs;
using Logistics.Server.Application.Modules.Shipping.Services;
using Logistics.Server.Domain.Entities.Shipping;
using Logistics.Server.Infrastructure.Persistence;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Server.Infrastructure.Services;

// ═══════════════════════════════════════════════════════
// Helper & Invariants
// ═══════════════════════════════════════════════════════
internal static class ShipmentStateHelper
{
    private static readonly Dictionary<string, HashSet<string>> ValidTransitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Draft"]          = new(StringComparer.OrdinalIgnoreCase) { "ReadyForPickup", "Cancelled" },
            ["ReadyForPickup"] = new(StringComparer.OrdinalIgnoreCase) { "PickedUp", "Cancelled" },
            ["PickedUp"]       = new(StringComparer.OrdinalIgnoreCase) { "InSortingHub", "InTransit", "Cancelled" },
            ["InSortingHub"]   = new(StringComparer.OrdinalIgnoreCase) { "InTransit" },
            ["InTransit"]      = new(StringComparer.OrdinalIgnoreCase) { "InSortingHub", "OutForDelivery" },
            ["OutForDelivery"] = new(StringComparer.OrdinalIgnoreCase) { "Delivered", "DeliveryFailed" },
            ["DeliveryFailed"] = new(StringComparer.OrdinalIgnoreCase) { "OutForDelivery", "Returning", "Cancelled" },
            ["Returning"]      = new(StringComparer.OrdinalIgnoreCase) { "Returned" },
            ["Delivered"]      = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Returned"]       = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["Cancelled"]      = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

    public static bool IsValidTransition(string from, string to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool IsTerminal(string status)
        => string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Returned", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase);

    public static bool IsEditable(string status)
        => string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "ReadyForPickup", StringComparison.OrdinalIgnoreCase);
}

// ═══════════════════════════════════════════════════════
// 1. Shipment Service
// ═══════════════════════════════════════════════════════
public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IOutboxService? _outbox;

    public ShipmentService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        INumberSequenceService numberSequenceService)
        INumberSequenceService numberSequenceService,
        IOutboxService? outbox = null)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _numberSequenceService = numberSequenceService;
        _outbox = outbox;
    }

    private bool IsSuperAdmin() =>
        _currentUserService.IsInRole(AppRoles.SuperAdmin) || _currentUserService.IsInRole("Admin");

    private IQueryable<Shipment> ApplyBranchFilter(IQueryable<Shipment> query)
    {
        if (IsSuperAdmin()) return query;
        var accessibleBranchIds = _currentUserService.AccessibleBranchIds;
        return query.Where(s => accessibleBranchIds.Contains(s.OriginBranchId) || accessibleBranchIds.Contains(s.DestinationBranchId));
    }

    private async Task<Shipment> FindShipmentAsync(string identifier, CancellationToken ct, bool includeDetails = false)
    {
        IQueryable<Shipment> query = _context.Shipments;
        if (includeDetails)
        {
            query = query
                .Include(s => s.Packages)
                .Include(s => s.Addresses).ThenInclude(a => a.Country)
                .Include(s => s.Addresses).ThenInclude(a => a.Province)
                .Include(s => s.Addresses).ThenInclude(a => a.District)
                .Include(s => s.Addresses).ThenInclude(a => a.Ward)
                .Include(s => s.StatusHistories).ThenInclude(h => h.LocationBranch)
                .Include(s => s.StatusHistories).ThenInclude(h => h.LocationWarehouse)
                .Include(s => s.Assignments).ThenInclude(a => a.AssignedToEmployee)
                .Include(s => s.Assignments).ThenInclude(a => a.AssignedBranch)
                .Include(s => s.Routes).ThenInclude(r => r.FromWarehouse)
                .Include(s => s.Routes).ThenInclude(r => r.ToWarehouse)
                .Include(s => s.Routes).ThenInclude(r => r.Carrier)
                .Include(s => s.Order)
                .Include(s => s.Customer)
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.AssignedCarrier)
                .Include(s => s.CarrierService)
                .Include(s => s.Currency);
        }
        else
        {
            query = query
                .Include(s => s.Order)
                .Include(s => s.Customer)
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.AssignedCarrier)
                .Include(s => s.CarrierService)
                .Include(s => s.Currency);
        }

        query = ApplyBranchFilter(query);

        Shipment? shipment = null;
        if (long.TryParse(identifier, out var id))
            shipment = await query.FirstOrDefaultAsync(s => s.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            shipment = await query.FirstOrDefaultAsync(s => s.PublicId == guid, ct);
        else
            shipment = await query.FirstOrDefaultAsync(s => s.TrackingNumber == identifier, ct);

        return shipment ?? throw new NotFoundException("Shipment", identifier);
    }

    public async Task<PagedResult<ShipmentDto>> GetShipmentsAsync(
        string? search, string? shipmentStatus, string? serviceType, long? customerId,
        long? originBranchId, long? destinationBranchId, long? carrierId,
        DateTime? dateFrom, DateTime? dateTo, PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Shipments
            .Include(s => s.Order)
            .Include(s => s.Customer)
            .Include(s => s.OriginBranch)
            .Include(s => s.DestinationBranch)
            .Include(s => s.AssignedCarrier)
            .Include(s => s.CarrierService)
            .Include(s => s.Currency)
            .AsQueryable();

        query = ApplyBranchFilter(query);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.TrackingNumber.Contains(search)
                || (s.CarrierTrackingNumber != null && s.CarrierTrackingNumber.Contains(search))
                || s.Customer.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(shipmentStatus))
            query = query.Where(s => s.ShipmentStatus == shipmentStatus);
        if (!string.IsNullOrWhiteSpace(serviceType))
            query = query.Where(s => s.ServiceType == serviceType);
        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);
        if (originBranchId.HasValue)
            query = query.Where(s => s.OriginBranchId == originBranchId.Value);
        if (destinationBranchId.HasValue)
            query = query.Where(s => s.DestinationBranchId == destinationBranchId.Value);
        if (carrierId.HasValue)
            query = query.Where(s => s.AssignedCarrierId == carrierId.Value);
        if (dateFrom.HasValue)
            query = query.Where(s => s.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(s => s.CreatedAt <= dateTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        return new PagedResult<ShipmentDto>(items, totalCount, page, pageSize);
    }

    public async Task<ShipmentDetailDto?> GetByIdOrTrackingAsync(string identifier, CancellationToken cancellationToken = default)
    {
        try
        {
            var s = await FindShipmentAsync(identifier, cancellationToken, includeDetails: true);
            return MapToDetail(s);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ShipmentDto>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var shipments = await _context.Shipments
            .Include(s => s.Order)
            .Include(s => s.Customer)
            .Include(s => s.OriginBranch)
            .Include(s => s.DestinationBranch)
            .Include(s => s.AssignedCarrier)
            .Include(s => s.CarrierService)
            .Include(s => s.Currency)
            .Where(s => s.OrderId == orderId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => MapToDto(s))
            .ToListAsync(cancellationToken);

        return shipments;
    }

    public async Task<ShipmentDetailDto> CreateFromOrderAsync(CreateShipmentFromOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.OrderPackages)
            .Include(o => o.OrderAddresses)
            .Include(o => o.Customer)
            .Include(o => o.Branch)
            .Include(o => o.Currency)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (string.Equals(order.OrderStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Cannot create a shipment from a cancelled order.");
        if (string.Equals(order.OrderStatus, "Draft", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Order must be confirmed before a shipment can be created. Current status: Draft.");

        var originBranchId = request.OriginBranchId > 0 ? request.OriginBranchId : order.BranchId;
        var destBranchId = request.DestinationBranchId;

        var destBranch = await _context.Branches.FindAsync(new object[] { destBranchId }, cancellationToken)
            ?? throw new NotFoundException("Branch", destBranchId);

        // Check currency
        var currencyId = request.CurrencyId ?? order.CurrencyId;

        // Generate tracking number
        var trackingNumber = await _numberSequenceService.GetNextNumberAsync("WAYBILL", cancellationToken);
        var actor = _currentUserService.Username ?? _currentUserService.UserId?.ToString() ?? "System";

        var shipment = new Shipment
        {
            TrackingNumber = trackingNumber,
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            OriginBranchId = originBranchId,
            DestinationBranchId = destBranchId,
            AssignedCarrierId = request.AssignedCarrierId,
            CarrierServiceId = request.CarrierServiceId,
            ShipmentStatus = "ReadyForPickup",
            ServiceType = string.IsNullOrWhiteSpace(request.ServiceType) ? "StandardGround" : request.ServiceType,
            TotalPackages = order.OrderPackages.Count > 0 ? order.OrderPackages.Count : 1,
            ActualWeightKg = order.OrderPackages.Sum(p => p.WeightKg),
            ChargeableWeightKg = order.OrderPackages.Sum(p => Math.Max(p.WeightKg, p.VolumetricWeightKg)),
            DeclaredValue = request.DeclaredValue > 0 ? request.DeclaredValue : order.DeclaredValue,
            CodAmount = request.CodAmount > 0 ? request.CodAmount : order.CodAmount,
            ShippingFee = 0,
            InsuranceFee = 0,
            SurchargesTotal = 0,
            TotalAmount = 0,
            CurrencyId = currencyId,
            PickupDate = request.PickupDate,
            EstimatedDeliveryDate = request.EstimatedDeliveryDate,
            Notes = request.Notes,
            CreatedBy = actor
        };

        // Populate Packages from OrderPackages
        int pkgIdx = 1;
        foreach (var op in order.OrderPackages)
        {
            shipment.Packages.Add(new ShipmentPackage
            {
                OrderPackageId = op.Id,
                PackageCode = string.IsNullOrWhiteSpace(op.PackageCode) ? $"{trackingNumber}-PKG{pkgIdx:D2}" : op.PackageCode,
                Barcode = op.Barcode ?? $"{trackingNumber}-{pkgIdx:D3}",
                WeightKg = op.WeightKg,
                LengthCm = op.LengthCm,
                WidthCm = op.WidthCm,
                HeightCm = op.HeightCm,
                VolumetricWeightKg = op.VolumetricWeightKg,
                PackageStatus = "Created",
                CreatedBy = actor
            });
            pkgIdx++;
        }

        // Copy Addresses from Order
        var defaultCountry = await _context.Countries.FirstOrDefaultAsync(cancellationToken);
        long countryId = defaultCountry?.Id ?? 1;

        foreach (var oa in order.OrderAddresses)
        {
            var addrType = oa.AddressType switch
            {
                "Sender" => "Sender",
                "Recipient" => "Recipient",
                "Pickup" => "Sender",
                _ => null
            };
            if (addrType == null) continue;

            if (shipment.Addresses.Any(a => a.AddressType == addrType)) continue;

            shipment.Addresses.Add(new ShipmentAddress
            {
                AddressType = addrType,
                ContactName = oa.ContactName,
                PhoneNumber = oa.PhoneNumber,
                Email = oa.Email,
                AddressLine1 = oa.AddressLine1,
                AddressLine2 = oa.AddressLine2,
                CountryId = oa.CountryId > 0 ? oa.CountryId : countryId,
                ProvinceId = oa.ProvinceId,
                DistrictId = oa.DistrictId,
                WardId = oa.WardId,
                PostalCode = oa.PostalCode,
                FormattedAddress = oa.FormattedAddress
            });
        }

        // Initial status history
        shipment.StatusHistories.Add(new ShipmentStatusHistory
        {
            Status = "ReadyForPickup",
            Notes = $"Created from Order {order.OrderNumber}.",
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = _currentUserService.UserId,
            Source = "Internal",
            CreatedAt = DateTime.UtcNow
        });

        await _context.Shipments.AddAsync(shipment, cancellationToken);

        // Update order status to Processing if it was Confirmed
        if (string.Equals(order.OrderStatus, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            order.OrderStatus = "Processing";
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = actor;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (_outbox != null)
        {
            _outbox.Enqueue(
                new ShipmentCreatedIntegrationEvent(shipment.Id, shipment.TrackingNumber, shipment.OrderId,
                    shipment.CustomerId, shipment.OriginBranchId, shipment.DestinationBranchId),
                "Shipment", shipment.Id.ToString());
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _auditService.RecordAsync("Create", "shipping", "Shipments", shipment.Id.ToString(),
            null, new { shipment.TrackingNumber, shipment.OrderId, shipment.ShipmentStatus }, null, cancellationToken);

        return (await GetByIdOrTrackingAsync(shipment.TrackingNumber, cancellationToken))!;
    }

    public async Task<ShipmentDetailDto> UpdateAsync(string identifier, UpdateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await FindShipmentAsync(identifier, cancellationToken);

        if (!ShipmentStateHelper.IsEditable(shipment.ShipmentStatus))
            throw new ConflictException($"Shipment '{shipment.TrackingNumber}' in status '{shipment.ShipmentStatus}' cannot be updated.");

        var actor = _currentUserService.Username ?? _currentUserService.UserId?.ToString() ?? "System";
        var oldValues = new { shipment.ServiceType, shipment.CodAmount, shipment.ShippingFee, shipment.Notes };

        if (!string.IsNullOrWhiteSpace(request.ServiceType)) shipment.ServiceType = request.ServiceType;
        if (request.AssignedCarrierId.HasValue) shipment.AssignedCarrierId = request.AssignedCarrierId;
        if (request.CarrierServiceId.HasValue) shipment.CarrierServiceId = request.CarrierServiceId;
        if (request.CarrierTrackingNumber != null) shipment.CarrierTrackingNumber = request.CarrierTrackingNumber;
        if (request.PickupDate.HasValue) shipment.PickupDate = request.PickupDate;
        if (request.EstimatedDeliveryDate.HasValue) shipment.EstimatedDeliveryDate = request.EstimatedDeliveryDate;

        shipment.DeclaredValue = request.DeclaredValue;
        shipment.CodAmount = request.CodAmount;
        shipment.ShippingFee = request.ShippingFee;
        shipment.InsuranceFee = request.InsuranceFee;
        shipment.SurchargesTotal = request.SurchargesTotal;
        shipment.TotalAmount = request.ShippingFee + request.InsuranceFee + request.SurchargesTotal;
        shipment.Notes = request.Notes;
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Update", "shipping", "Shipments", shipment.Id.ToString(),
            oldValues, new { shipment.ServiceType, shipment.ShippingFee, shipment.TotalAmount }, null, cancellationToken);

        return (await GetByIdOrTrackingAsync(shipment.TrackingNumber, cancellationToken))!;
    }

    public async Task<ShipmentDetailDto> CancelAsync(string identifier, CancelShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await FindShipmentAsync(identifier, cancellationToken);

        if (ShipmentStateHelper.IsTerminal(shipment.ShipmentStatus))
            throw new ConflictException($"Shipment '{shipment.TrackingNumber}' is already in terminal status '{shipment.ShipmentStatus}'.");

        if (!ShipmentStateHelper.IsValidTransition(shipment.ShipmentStatus, "Cancelled"))
            throw new ConflictException($"Cannot cancel shipment in status '{shipment.ShipmentStatus}'.");

        var actor = _currentUserService.Username ?? _currentUserService.UserId?.ToString() ?? "System";
        var previousStatus = shipment.ShipmentStatus;

        shipment.ShipmentStatus = "Cancelled";
        shipment.Notes = string.IsNullOrWhiteSpace(shipment.Notes)
            ? $"Cancelled: {request.Reason}"
            : $"{shipment.Notes} | Cancelled: {request.Reason}";
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            ShipmentId = shipment.Id,
            Status = "Cancelled",
            Notes = request.Reason,
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Source = "Internal",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Cancel", "shipping", "Shipments", shipment.Id.ToString(),
            new { Status = previousStatus }, new { Status = "Cancelled", request.Reason }, null, cancellationToken);

        return (await GetByIdOrTrackingAsync(shipment.TrackingNumber, cancellationToken))!;
    }

    public async Task<ShipmentDetailDto> TransitionStatusAsync(string identifier, ShipmentStatusTransitionRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await FindShipmentAsync(identifier, cancellationToken);

        if (!ShipmentStateHelper.IsValidTransition(shipment.ShipmentStatus, request.TargetStatus))
            throw new ConflictException($"Cannot transition shipment from '{shipment.ShipmentStatus}' to '{request.TargetStatus}'.");

        var actor = _currentUserService.Username ?? _currentUserService.UserId?.ToString() ?? "System";
        var previousStatus = shipment.ShipmentStatus;

        shipment.ShipmentStatus = request.TargetStatus;
        if (string.Equals(request.TargetStatus, "PickedUp", StringComparison.OrdinalIgnoreCase))
            shipment.PickupDate = DateTime.UtcNow;
        if (string.Equals(request.TargetStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
            shipment.ActualDeliveryDate = DateTime.UtcNow;

        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
        {
            ShipmentId = shipment.Id,
            Status = request.TargetStatus,
            SubStatus = request.SubStatus,
            Notes = request.Notes,
            LocationBranchId = request.LocationBranchId,
            LocationWarehouseId = request.LocationWarehouseId,
            ChangedByUserId = _currentUserService.UserId,
            ChangedAt = DateTime.UtcNow,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Internal" : request.Source,
            CreatedAt = DateTime.UtcNow
        });

        _outbox?.Enqueue(
            new ShipmentStatusChangedIntegrationEvent(shipment.Id, shipment.TrackingNumber, shipment.CustomerId,
                shipment.OriginBranchId, shipment.DestinationBranchId, previousStatus, request.TargetStatus),
            "Shipment", shipment.Id.ToString());

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("StatusChange", "shipping", "Shipments", shipment.Id.ToString(),
            new { Status = previousStatus }, new { Status = request.TargetStatus, request.Notes }, null, cancellationToken);

        return (await GetByIdOrTrackingAsync(shipment.TrackingNumber, cancellationToken))!;
    }

    public async Task<IReadOnlyList<ShipmentStatusHistoryDto>> GetStatusHistoryAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var shipment = await FindShipmentAsync(identifier, cancellationToken);

        var histories = await _context.ShipmentStatusHistories
            .Include(h => h.LocationBranch)
            .Include(h => h.LocationWarehouse)
            .Where(h => h.ShipmentId == shipment.Id)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => new ShipmentStatusHistoryDto
            {
                Id = h.Id,
                ShipmentId = h.ShipmentId,
                Status = h.Status,
                SubStatus = h.SubStatus,
                LocationWarehouseId = h.LocationWarehouseId,
                LocationWarehouseName = h.LocationWarehouse != null ? h.LocationWarehouse.Name : null,
                LocationBranchId = h.LocationBranchId,
                LocationBranchName = h.LocationBranch != null ? h.LocationBranch.Name : null,
                Notes = h.Notes,
                ChangedByUserId = h.ChangedByUserId.HasValue ? h.ChangedByUserId.Value.ToString() : null,
                ChangedByEmployeeId = h.ChangedByEmployeeId,
                ChangedAt = h.ChangedAt,
                Source = h.Source,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return histories;
    }

    private static ShipmentDto MapToDto(Shipment s) => new()
    {
        Id = s.Id,
        PublicId = s.PublicId,
        TrackingNumber = s.TrackingNumber,
        OrderId = s.OrderId,
        OrderNumber = s.Order?.OrderNumber,
        CustomerId = s.CustomerId,
        CustomerName = s.Customer?.Name ?? string.Empty,
        OriginBranchId = s.OriginBranchId,
        OriginBranchName = s.OriginBranch?.Name ?? string.Empty,
        DestinationBranchId = s.DestinationBranchId,
        DestinationBranchName = s.DestinationBranch?.Name ?? string.Empty,
        AssignedCarrierId = s.AssignedCarrierId,
        CarrierName = s.AssignedCarrier?.Name,
        CarrierServiceId = s.CarrierServiceId,
        CarrierServiceName = s.CarrierService?.ServiceName,
        CarrierTrackingNumber = s.CarrierTrackingNumber,
        ShipmentStatus = s.ShipmentStatus,
        ServiceType = s.ServiceType,
        TotalPackages = s.TotalPackages,
        ActualWeightKg = s.ActualWeightKg,
        ChargeableWeightKg = s.ChargeableWeightKg,
        DeclaredValue = s.DeclaredValue,
        CodAmount = s.CodAmount,
        ShippingFee = s.ShippingFee,
        InsuranceFee = s.InsuranceFee,
        SurchargesTotal = s.SurchargesTotal,
        TotalAmount = s.TotalAmount,
        CurrencyId = s.CurrencyId,
        CurrencyCode = s.Currency?.Code ?? string.Empty,
        PickupDate = s.PickupDate,
        EstimatedDeliveryDate = s.EstimatedDeliveryDate,
        ActualDeliveryDate = s.ActualDeliveryDate,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
        CreatedBy = s.CreatedBy,
        UpdatedAt = s.UpdatedAt,
        UpdatedBy = s.UpdatedBy
    };

    private static ShipmentDetailDto MapToDetail(Shipment s) => new()
    {
        Id = s.Id,
        PublicId = s.PublicId,
        TrackingNumber = s.TrackingNumber,
        OrderId = s.OrderId,
        OrderNumber = s.Order?.OrderNumber,
        CustomerId = s.CustomerId,
        CustomerName = s.Customer?.Name ?? string.Empty,
        OriginBranchId = s.OriginBranchId,
        OriginBranchName = s.OriginBranch?.Name ?? string.Empty,
        DestinationBranchId = s.DestinationBranchId,
        DestinationBranchName = s.DestinationBranch?.Name ?? string.Empty,
        AssignedCarrierId = s.AssignedCarrierId,
        CarrierName = s.AssignedCarrier?.Name,
        CarrierServiceId = s.CarrierServiceId,
        CarrierServiceName = s.CarrierService?.ServiceName,
        CarrierTrackingNumber = s.CarrierTrackingNumber,
        ShipmentStatus = s.ShipmentStatus,
        ServiceType = s.ServiceType,
        TotalPackages = s.TotalPackages,
        ActualWeightKg = s.ActualWeightKg,
        ChargeableWeightKg = s.ChargeableWeightKg,
        DeclaredValue = s.DeclaredValue,
        CodAmount = s.CodAmount,
        ShippingFee = s.ShippingFee,
        InsuranceFee = s.InsuranceFee,
        SurchargesTotal = s.SurchargesTotal,
        TotalAmount = s.TotalAmount,
        CurrencyId = s.CurrencyId,
        CurrencyCode = s.Currency?.Code ?? string.Empty,
        PickupDate = s.PickupDate,
        EstimatedDeliveryDate = s.EstimatedDeliveryDate,
        ActualDeliveryDate = s.ActualDeliveryDate,
        Notes = s.Notes,
        CreatedAt = s.CreatedAt,
        CreatedBy = s.CreatedBy,
        UpdatedAt = s.UpdatedAt,
        UpdatedBy = s.UpdatedBy,
        Packages = s.Packages.Select(p => new ShipmentPackageDto
        {
            Id = p.Id,
            PublicId = p.PublicId,
            ShipmentId = p.ShipmentId,
            OrderPackageId = p.OrderPackageId,
            PackageCode = p.PackageCode,
            Barcode = p.Barcode,
            WeightKg = p.WeightKg,
            LengthCm = p.LengthCm,
            WidthCm = p.WidthCm,
            HeightCm = p.HeightCm,
            VolumetricWeightKg = p.VolumetricWeightKg,
            PackageStatus = p.PackageStatus,
            SealNumber = p.SealNumber,
            CreatedAt = p.CreatedAt,
            CreatedBy = p.CreatedBy,
            UpdatedAt = p.UpdatedAt,
            UpdatedBy = p.UpdatedBy
        }).ToList(),
        Addresses = s.Addresses.Select(a => new ShipmentAddressDto
        {
            Id = a.Id,
            ShipmentId = a.ShipmentId,
            AddressType = a.AddressType,
            ContactName = a.ContactName,
            PhoneNumber = a.PhoneNumber,
            Email = a.Email,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            CountryId = a.CountryId,
            CountryName = a.Country?.Name,
            ProvinceId = a.ProvinceId,
            ProvinceName = a.Province?.Name,
            DistrictId = a.DistrictId,
            DistrictName = a.District?.Name,
            WardId = a.WardId,
            WardName = a.Ward?.Name,
            PostalCode = a.PostalCode,
            FormattedAddress = a.FormattedAddress
        }).ToList(),
        StatusHistory = s.StatusHistories.OrderByDescending(h => h.ChangedAt).Select(h => new ShipmentStatusHistoryDto
        {
            Id = h.Id,
            ShipmentId = h.ShipmentId,
            Status = h.Status,
            SubStatus = h.SubStatus,
            LocationWarehouseId = h.LocationWarehouseId,
            LocationWarehouseName = h.LocationWarehouse?.Name,
            LocationBranchId = h.LocationBranchId,
            LocationBranchName = h.LocationBranch?.Name,
            Notes = h.Notes,
            ChangedByUserId = h.ChangedByUserId.HasValue ? h.ChangedByUserId.Value.ToString() : null,
            ChangedByEmployeeId = h.ChangedByEmployeeId,
            ChangedAt = h.ChangedAt,
            Source = h.Source,
            CreatedAt = h.CreatedAt
        }).ToList(),
        Assignments = s.Assignments.Select(a => new ShipmentAssignmentDto
        {
            Id = a.Id,
            ShipmentId = a.ShipmentId,
            AssignmentType = a.AssignmentType,
            AssignedToEmployeeId = a.AssignedToEmployeeId,
            AssignedToEmployeeName = a.AssignedToEmployee?.FullName ?? string.Empty,
            AssignedBranchId = a.AssignedBranchId,
            AssignedBranchName = a.AssignedBranch?.Name ?? string.Empty,
            AssignedVehicleId = a.AssignedVehicleId != null ? (long.TryParse(a.AssignedVehicleId, out var vid) ? vid : null) : null,
            VehicleNumber = a.AssignedVehicleId,
            AssignedAt = a.AssignedAt,
            AssignedByUserId = a.AssignedByUserId.HasValue ? a.AssignedByUserId.Value.ToString() : null,
            Status = a.Status,
            CompletedAt = a.CompletedAt,
            Notes = a.Notes
        }).ToList(),
        Routes = s.Routes.OrderBy(r => r.SequenceOrder).Select(r => new ShipmentRouteDto
        {
            Id = r.Id,
            ShipmentId = r.ShipmentId,
            SequenceOrder = r.SequenceOrder,
            FromWarehouseId = r.FromWarehouseId,
            FromWarehouseName = r.FromWarehouse?.Name ?? string.Empty,
            ToWarehouseId = r.ToWarehouseId,
            ToWarehouseName = r.ToWarehouse?.Name ?? string.Empty,
            TransportType = r.TransportType,
            CarrierId = r.CarrierId,
            CarrierName = r.Carrier?.Name,
            VehicleNumber = r.VehicleNumber,
            EstimatedDeparture = r.EstimatedDeparture,
            ActualDeparture = r.ActualDeparture,
            EstimatedArrival = r.EstimatedArrival,
            ActualArrival = r.ActualArrival,
            Status = r.Status
        }).ToList()
    };
}

// ═══════════════════════════════════════════════════════
// 2. Shipment Package Service
// ═══════════════════════════════════════════════════════
public class ShipmentPackageService : IShipmentPackageService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ShipmentPackageService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.Include(x => x.Packages).FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.Include(x => x.Packages).FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.Include(x => x.Packages).FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static ShipmentPackageDto MapPackage(ShipmentPackage p) => new()
    {
        Id = p.Id,
        PublicId = p.PublicId,
        ShipmentId = p.ShipmentId,
        OrderPackageId = p.OrderPackageId,
        PackageCode = p.PackageCode,
        Barcode = p.Barcode,
        WeightKg = p.WeightKg,
        LengthCm = p.LengthCm,
        WidthCm = p.WidthCm,
        HeightCm = p.HeightCm,
        VolumetricWeightKg = p.VolumetricWeightKg,
        PackageStatus = p.PackageStatus,
        SealNumber = p.SealNumber,
        CreatedAt = p.CreatedAt,
        CreatedBy = p.CreatedBy,
        UpdatedAt = p.UpdatedAt,
        UpdatedBy = p.UpdatedBy
    };

    public async Task<IReadOnlyList<ShipmentPackageDto>> GetPackagesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        return shipment.Packages.Select(MapPackage).ToList();
    }

    public async Task<ShipmentPackageDto?> GetPackageByIdAsync(string shipmentIdentifier, string packageIdentifier, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        ShipmentPackage? pkg = null;
        if (long.TryParse(packageIdentifier, out var pid))
            pkg = shipment.Packages.FirstOrDefault(p => p.Id == pid);
        else if (Guid.TryParse(packageIdentifier, out var pguid))
            pkg = shipment.Packages.FirstOrDefault(p => p.PublicId == pguid);
        else
            pkg = shipment.Packages.FirstOrDefault(p => p.PackageCode == packageIdentifier || p.Barcode == packageIdentifier);

        return pkg == null ? null : MapPackage(pkg);
    }

    public async Task<ShipmentPackageDto> AddPackageAsync(string shipmentIdentifier, AddShipmentPackageRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (!ShipmentStateHelper.IsEditable(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot add packages to shipment in status '{shipment.ShipmentStatus}'.");

        var actor = _currentUserService.Username ?? "System";
        var volWeight = (request.LengthCm * request.WidthCm * request.HeightCm) / 5000m;
        var pkg = new ShipmentPackage
        {
            ShipmentId = shipment.Id,
            OrderPackageId = request.OrderPackageId,
            PackageCode = $"{shipment.TrackingNumber}-PKG{(shipment.TotalPackages + 1):D2}",
            Barcode = $"{shipment.TrackingNumber}-{(shipment.TotalPackages + 1):D3}",
            WeightKg = request.WeightKg,
            LengthCm = request.LengthCm,
            WidthCm = request.WidthCm,
            HeightCm = request.HeightCm,
            VolumetricWeightKg = volWeight,
            PackageStatus = "Created",
            SealNumber = request.SealNumber,
            CreatedBy = actor
        };

        shipment.TotalPackages++;
        shipment.ActualWeightKg += request.WeightKg;
        shipment.ChargeableWeightKg += Math.Max(request.WeightKg, volWeight);
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        _context.ShipmentPackages.Add(pkg);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Create", "shipping", "ShipmentPackages", pkg.Id.ToString(), null, new { pkg.PackageCode, pkg.WeightKg }, null, cancellationToken);

        return MapPackage(pkg);
    }

    public async Task<ShipmentPackageDto> UpdatePackageAsync(string shipmentIdentifier, string packageIdentifier, UpdateShipmentPackageRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (!ShipmentStateHelper.IsEditable(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot update packages on shipment in status '{shipment.ShipmentStatus}'.");

        ShipmentPackage? pkg = null;
        if (long.TryParse(packageIdentifier, out var pid))
            pkg = shipment.Packages.FirstOrDefault(p => p.Id == pid);
        else if (Guid.TryParse(packageIdentifier, out var pguid))
            pkg = shipment.Packages.FirstOrDefault(p => p.PublicId == pguid);
        else
            pkg = shipment.Packages.FirstOrDefault(p => p.PackageCode == packageIdentifier);

        if (pkg == null)
            throw new NotFoundException("ShipmentPackage", packageIdentifier);

        var oldWeight = pkg.WeightKg;
        var oldVolWeight = pkg.VolumetricWeightKg;
        var actor = _currentUserService.Username ?? "System";

        pkg.WeightKg = request.WeightKg;
        pkg.LengthCm = request.LengthCm;
        pkg.WidthCm = request.WidthCm;
        pkg.HeightCm = request.HeightCm;
        pkg.VolumetricWeightKg = (request.LengthCm * request.WidthCm * request.HeightCm) / 5000m;
        pkg.SealNumber = request.SealNumber;
        if (!string.IsNullOrWhiteSpace(request.PackageStatus)) pkg.PackageStatus = request.PackageStatus;
        pkg.UpdatedAt = DateTime.UtcNow;
        pkg.UpdatedBy = actor;

        shipment.ActualWeightKg = shipment.ActualWeightKg - oldWeight + request.WeightKg;
        shipment.ChargeableWeightKg = shipment.ChargeableWeightKg - Math.Max(oldWeight, oldVolWeight) + Math.Max(pkg.WeightKg, pkg.VolumetricWeightKg);
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Update", "shipping", "ShipmentPackages", pkg.Id.ToString(),
            new { Weight = oldWeight }, new { pkg.WeightKg }, null, cancellationToken);

        return MapPackage(pkg);
    }

    public async Task RemovePackageAsync(string shipmentIdentifier, string packageIdentifier, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (!ShipmentStateHelper.IsEditable(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot remove packages from shipment in status '{shipment.ShipmentStatus}'.");

        ShipmentPackage? pkg = null;
        if (long.TryParse(packageIdentifier, out var pid))
            pkg = shipment.Packages.FirstOrDefault(p => p.Id == pid);
        else if (Guid.TryParse(packageIdentifier, out var pguid))
            pkg = shipment.Packages.FirstOrDefault(p => p.PublicId == pguid);
        else
            pkg = shipment.Packages.FirstOrDefault(p => p.PackageCode == packageIdentifier);

        if (pkg == null)
            throw new NotFoundException("ShipmentPackage", packageIdentifier);

        var actor = _currentUserService.Username ?? "System";
        shipment.TotalPackages = Math.Max(0, shipment.TotalPackages - 1);
        shipment.ActualWeightKg = Math.Max(0, shipment.ActualWeightKg - pkg.WeightKg);
        shipment.ChargeableWeightKg = Math.Max(0, shipment.ChargeableWeightKg - Math.Max(pkg.WeightKg, pkg.VolumetricWeightKg));
        shipment.UpdatedAt = DateTime.UtcNow;
        shipment.UpdatedBy = actor;

        _context.ShipmentPackages.Remove(pkg);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Delete", "shipping", "ShipmentPackages", pkg.Id.ToString(), new { pkg.PackageCode }, null, null, cancellationToken);
    }
}

// ═══════════════════════════════════════════════════════
// 3. Shipment Address Service
// ═══════════════════════════════════════════════════════
public class ShipmentAddressService : IShipmentAddressService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ShipmentAddressService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.Include(x => x.Addresses).ThenInclude(a => a.Country).Include(x => x.Addresses).ThenInclude(a => a.Province).Include(x => x.Addresses).ThenInclude(a => a.District).Include(x => x.Addresses).ThenInclude(a => a.Ward).FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.Include(x => x.Addresses).ThenInclude(a => a.Country).Include(x => x.Addresses).ThenInclude(a => a.Province).Include(x => x.Addresses).ThenInclude(a => a.District).Include(x => x.Addresses).ThenInclude(a => a.Ward).FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.Include(x => x.Addresses).ThenInclude(a => a.Country).Include(x => x.Addresses).ThenInclude(a => a.Province).Include(x => x.Addresses).ThenInclude(a => a.District).Include(x => x.Addresses).ThenInclude(a => a.Ward).FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static ShipmentAddressDto MapAddress(ShipmentAddress a) => new()
    {
        Id = a.Id,
        ShipmentId = a.ShipmentId,
        AddressType = a.AddressType,
        ContactName = a.ContactName,
        PhoneNumber = a.PhoneNumber,
        Email = a.Email,
        AddressLine1 = a.AddressLine1,
        AddressLine2 = a.AddressLine2,
        CountryId = a.CountryId,
        CountryName = a.Country?.Name,
        ProvinceId = a.ProvinceId,
        ProvinceName = a.Province?.Name,
        DistrictId = a.DistrictId,
        DistrictName = a.District?.Name,
        WardId = a.WardId,
        WardName = a.Ward?.Name,
        PostalCode = a.PostalCode,
        FormattedAddress = a.FormattedAddress
    };

    public async Task<IReadOnlyList<ShipmentAddressDto>> GetAddressesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        return shipment.Addresses.Select(MapAddress).ToList();
    }

    public async Task<ShipmentAddressDto?> GetAddressByTypeAsync(string shipmentIdentifier, string addressType, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var addr = shipment.Addresses.FirstOrDefault(a => string.Equals(a.AddressType, addressType, StringComparison.OrdinalIgnoreCase));
        return addr == null ? null : MapAddress(addr);
    }

    public async Task<ShipmentAddressDto> SetAddressAsync(string shipmentIdentifier, string addressType, UpdateShipmentAddressRequest request, CancellationToken cancellationToken = default)
    {
        var validTypes = new[] { "Sender", "Recipient", "ReturnAddress" };
        if (!validTypes.Contains(addressType, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Address type must be one of: {string.Join(", ", validTypes)}.");

        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (ShipmentStateHelper.IsTerminal(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot modify addresses on a shipment in terminal status '{shipment.ShipmentStatus}'.");

        var existing = shipment.Addresses.FirstOrDefault(a => string.Equals(a.AddressType, addressType, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.ContactName = request.ContactName;
            existing.PhoneNumber = request.PhoneNumber;
            existing.Email = request.Email;
            existing.AddressLine1 = request.AddressLine1;
            existing.AddressLine2 = request.AddressLine2;
            existing.CountryId = request.CountryId;
            existing.ProvinceId = request.ProvinceId;
            existing.DistrictId = request.DistrictId;
            existing.WardId = request.WardId;
            existing.PostalCode = request.PostalCode;
            existing.FormattedAddress = string.IsNullOrWhiteSpace(existing.FormattedAddress)
                ? $"{request.AddressLine1}, {request.PostalCode}" : existing.FormattedAddress;
        }
        else
        {
            existing = new ShipmentAddress
            {
                ShipmentId = shipment.Id,
                AddressType = addressType,
                ContactName = request.ContactName,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                CountryId = request.CountryId,
                ProvinceId = request.ProvinceId,
                DistrictId = request.DistrictId,
                WardId = request.WardId,
                PostalCode = request.PostalCode,
                FormattedAddress = $"{request.AddressLine1}, {request.PostalCode}"
            };
            _context.ShipmentAddresses.Add(existing);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Upsert", "shipping", "ShipmentAddresses", existing.Id.ToString(), null, new { addressType, request.ContactName }, null, cancellationToken);

        var country = await _context.Countries.FindAsync(new object[] { request.CountryId }, cancellationToken);
        existing.Country = country!;
        return MapAddress(existing);
    }
}

// ═══════════════════════════════════════════════════════
// 4. Pickup Request Service
// ═══════════════════════════════════════════════════════
public class PickupRequestService : IPickupRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INumberSequenceService _numberSequenceService;

    public PickupRequestService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        INumberSequenceService numberSequenceService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _numberSequenceService = numberSequenceService;
    }

    private static PickupRequestDto MapDto(PickupRequest p) => new()
    {
        Id = p.Id,
        PublicId = p.PublicId,
        PickupCode = p.PickupCode,
        CustomerId = p.CustomerId,
        CustomerName = p.Customer?.Name ?? string.Empty,
        RequestedAddressId = p.RequestedAddressId,
        FormattedAddress = p.RequestedAddress?.FormattedAddress ?? string.Empty,
        ScheduledDate = p.ScheduledDate,
        TimeSlot = p.TimeSlot,
        Status = p.Status,
        AssignedEmployeeId = p.AssignedEmployeeId,
        AssignedEmployeeName = p.AssignedEmployee?.FullName,
        ContactName = p.ContactName,
        ContactPhone = p.ContactPhone,
        EstimatedParcels = p.EstimatedParcels,
        ActualParcelsPickedUp = p.ActualParcelsPickedUp,
        Notes = p.Notes,
        CreatedAt = p.CreatedAt,
        CreatedBy = p.CreatedBy,
        UpdatedAt = p.UpdatedAt,
        UpdatedBy = p.UpdatedBy
    };

    private async Task<PickupRequest> FindAsync(string identifier, CancellationToken ct)
    {
        var query = _context.PickupRequests
            .Include(p => p.Customer)
            .Include(p => p.RequestedAddress)
            .Include(p => p.AssignedEmployee);

        PickupRequest? pr = null;
        if (long.TryParse(identifier, out var id)) pr = await query.FirstOrDefaultAsync(p => p.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid)) pr = await query.FirstOrDefaultAsync(p => p.PublicId == guid, ct);
        else pr = await query.FirstOrDefaultAsync(p => p.PickupCode == identifier, ct);

        return pr ?? throw new NotFoundException("PickupRequest", identifier);
    }

    public async Task<PagedResult<PickupRequestDto>> GetPickupRequestsAsync(
        string? search, string? status, long? customerId, long? assignedEmployeeId,
        DateOnly? dateFrom, DateOnly? dateTo, PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PickupRequests
            .Include(p => p.Customer)
            .Include(p => p.RequestedAddress)
            .Include(p => p.AssignedEmployee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.PickupCode.Contains(search) || p.ContactName.Contains(search) || p.ContactPhone.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);
        if (assignedEmployeeId.HasValue)
            query = query.Where(p => p.AssignedEmployeeId == assignedEmployeeId.Value);
        if (dateFrom.HasValue)
            query = query.Where(p => p.ScheduledDate >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(p => p.ScheduledDate <= dateTo.Value);

        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapDto(p))
            .ToListAsync(cancellationToken);

        return new PagedResult<PickupRequestDto>(items, total, page, pageSize);
    }

    public async Task<PickupRequestDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        try { return MapDto(await FindAsync(identifier, cancellationToken)); }
        catch (NotFoundException) { return null; }
    }

    public async Task<PickupRequestDto> CreateAsync(CreatePickupRequestRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { request.CustomerId }, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        var address = await _context.Addresses.FindAsync(new object[] { request.RequestedAddressId }, cancellationToken)
            ?? throw new NotFoundException("Address", request.RequestedAddressId);

        var pickupCode = await _numberSequenceService.GetNextNumberAsync("PICKUP", cancellationToken);
        var actor = _currentUserService.Username ?? "System";

        var pr = new PickupRequest
        {
            PickupCode = pickupCode,
            CustomerId = request.CustomerId,
            RequestedAddressId = request.RequestedAddressId,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            ScheduledDate = request.ScheduledDate,
            TimeSlot = request.TimeSlot,
            Status = "Pending",
            EstimatedParcels = request.EstimatedParcels,
            Notes = request.Notes,
            CreatedBy = actor
        };

        await _context.PickupRequests.AddAsync(pr, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        pr.Customer = customer;
        pr.RequestedAddress = address;

        await _auditService.RecordAsync("Create", "shipping", "PickupRequests", pr.Id.ToString(), null, new { pr.PickupCode, pr.Status }, null, cancellationToken);
        return MapDto(pr);
    }

    public async Task<PickupRequestDto> UpdateAsync(string identifier, UpdatePickupRequestRequest request, CancellationToken cancellationToken = default)
    {
        var pr = await FindAsync(identifier, cancellationToken);
        if (pr.Status == "Completed" || pr.Status == "Cancelled")
            throw new ConflictException($"Cannot update a pickup request in status '{pr.Status}'.");

        var actor = _currentUserService.Username ?? "System";
        pr.ScheduledDate = request.ScheduledDate;
        pr.TimeSlot = request.TimeSlot;
        pr.ContactName = request.ContactName;
        pr.ContactPhone = request.ContactPhone;
        pr.EstimatedParcels = request.EstimatedParcels;
        pr.Notes = request.Notes;
        pr.UpdatedAt = DateTime.UtcNow;
        pr.UpdatedBy = actor;

        if (request.AssignedEmployeeId.HasValue && request.AssignedEmployeeId != pr.AssignedEmployeeId)
        {
            var emp = await _context.Employees.FindAsync(new object[] { request.AssignedEmployeeId.Value }, cancellationToken)
                ?? throw new NotFoundException("Employee", request.AssignedEmployeeId!.Value);
            if (!string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Cannot assign an inactive employee.");
            pr.AssignedEmployeeId = request.AssignedEmployeeId;
            if (pr.Status == "Pending") pr.Status = "Assigned";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapDto(pr);
    }

    public async Task<PickupRequestDto> AssignDriverAsync(string identifier, long employeeId, CancellationToken cancellationToken = default)
    {
        var pr = await FindAsync(identifier, cancellationToken);
        if (pr.Status == "Completed" || pr.Status == "Cancelled")
            throw new ConflictException($"Cannot assign driver to a pickup request in status '{pr.Status}'.");

        var emp = await _context.Employees.FindAsync(new object[] { employeeId }, cancellationToken)
            ?? throw new NotFoundException("Employee", employeeId);
        if (!string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Cannot assign an inactive employee.");

        var actor = _currentUserService.Username ?? "System";
        pr.AssignedEmployeeId = employeeId;
        pr.AssignedEmployee = emp;
        pr.Status = "Assigned";
        pr.UpdatedAt = DateTime.UtcNow;
        pr.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("AssignDriver", "shipping", "PickupRequests", pr.Id.ToString(), null, new { pr.Status, employeeId }, null, cancellationToken);
        return MapDto(pr);
    }

    public async Task<PickupRequestDto> MarkDriverArrivedAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var pr = await FindAsync(identifier, cancellationToken);
        if (!new[] { "Pending", "Assigned" }.Contains(pr.Status, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Cannot mark driver arrived for pickup request in status '{pr.Status}'.");

        var actor = _currentUserService.Username ?? "System";
        pr.Status = "DriverArrived";
        pr.UpdatedAt = DateTime.UtcNow;
        pr.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("DriverArrived", "shipping", "PickupRequests", pr.Id.ToString(), null, new { pr.Status }, null, cancellationToken);
        return MapDto(pr);
    }

    public async Task<PickupRequestDto> CompleteAsync(string identifier, CompletePickupRequest request, CancellationToken cancellationToken = default)
    {
        var pr = await FindAsync(identifier, cancellationToken);
        if (!new[] { "Assigned", "DriverArrived" }.Contains(pr.Status, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Cannot complete pickup request from status '{pr.Status}'.");

        var actor = _currentUserService.Username ?? "System";
        pr.Status = "Completed";
        pr.ActualParcelsPickedUp = request.ActualParcelsPickedUp;
        pr.Notes = request.Notes ?? pr.Notes;
        pr.UpdatedAt = DateTime.UtcNow;
        pr.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Complete", "shipping", "PickupRequests", pr.Id.ToString(), null, new { pr.Status, pr.ActualParcelsPickedUp }, null, cancellationToken);
        return MapDto(pr);
    }

    public async Task<PickupRequestDto> CancelAsync(string identifier, string reason, CancellationToken cancellationToken = default)
    {
        var pr = await FindAsync(identifier, cancellationToken);
        if (pr.Status == "Completed" || pr.Status == "Cancelled")
            throw new ConflictException($"Cannot cancel a pickup request in status '{pr.Status}'.");

        var actor = _currentUserService.Username ?? "System";
        pr.Status = "Cancelled";
        pr.Notes = string.IsNullOrWhiteSpace(pr.Notes) ? $"Cancelled: {reason}" : $"{pr.Notes} | Cancelled: {reason}";
        pr.UpdatedAt = DateTime.UtcNow;
        pr.UpdatedBy = actor;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Cancel", "shipping", "PickupRequests", pr.Id.ToString(), null, new { pr.Status, reason }, null, cancellationToken);
        return MapDto(pr);
    }
}

// ═══════════════════════════════════════════════════════
// 5. Shipment Assignment Service
// ═══════════════════════════════════════════════════════
public class ShipmentAssignmentService : IShipmentAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ShipmentAssignmentService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.Include(x => x.Assignments).ThenInclude(a => a.AssignedToEmployee).Include(x => x.Assignments).ThenInclude(a => a.AssignedBranch).FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.Include(x => x.Assignments).ThenInclude(a => a.AssignedToEmployee).Include(x => x.Assignments).ThenInclude(a => a.AssignedBranch).FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.Include(x => x.Assignments).ThenInclude(a => a.AssignedToEmployee).Include(x => x.Assignments).ThenInclude(a => a.AssignedBranch).FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static ShipmentAssignmentDto MapDto(ShipmentAssignment a) => new()
    {
        Id = a.Id,
        ShipmentId = a.ShipmentId,
        AssignmentType = a.AssignmentType,
        AssignedToEmployeeId = a.AssignedToEmployeeId,
        AssignedToEmployeeName = a.AssignedToEmployee?.FullName ?? string.Empty,
        AssignedBranchId = a.AssignedBranchId,
        AssignedBranchName = a.AssignedBranch?.Name ?? string.Empty,
        AssignedVehicleId = a.AssignedVehicleId != null ? (long.TryParse(a.AssignedVehicleId, out var vid) ? vid : null) : null,
        VehicleNumber = a.AssignedVehicleId,
        AssignedAt = a.AssignedAt,
        AssignedByUserId = a.AssignedByUserId.HasValue ? a.AssignedByUserId.Value.ToString() : null,
        Status = a.Status,
        CompletedAt = a.CompletedAt,
        Notes = a.Notes
    };

    public async Task<IReadOnlyList<ShipmentAssignmentDto>> GetAssignmentsAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        return s.Assignments.OrderByDescending(a => a.AssignedAt).Select(MapDto).ToList();
    }

    public async Task<ShipmentAssignmentDto?> GetAssignmentByIdAsync(string shipmentIdentifier, long assignmentId, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var assignment = s.Assignments.FirstOrDefault(a => a.Id == assignmentId);
        return assignment == null ? null : MapDto(assignment);
    }

    public async Task<ShipmentAssignmentDto> CreateAssignmentAsync(string shipmentIdentifier, CreateShipmentAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (ShipmentStateHelper.IsTerminal(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot assign employees to a shipment in terminal status '{shipment.ShipmentStatus}'.");

        var emp = await _context.Employees.Include(e => e.Branch).FirstOrDefaultAsync(e => e.Id == request.AssignedToEmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee", request.AssignedToEmployeeId);
        if (!string.Equals(emp.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Cannot assign an inactive employee.");

        var duplicate = shipment.Assignments.FirstOrDefault(a =>
            string.Equals(a.AssignmentType, request.AssignmentType, StringComparison.OrdinalIgnoreCase)
            && a.AssignedToEmployeeId == request.AssignedToEmployeeId
            && !new[] { "Completed", "Failed" }.Contains(a.Status, StringComparer.OrdinalIgnoreCase));
        if (duplicate != null)
            throw new ConflictException($"Employee is already actively assigned as '{request.AssignmentType}' on this shipment.");

        var branchId = request.AssignedBranchId > 0 ? request.AssignedBranchId : emp.BranchId;
        var branch = await _context.Branches.FindAsync(new object[] { branchId }, cancellationToken)
            ?? throw new NotFoundException("Branch", branchId);

        var assignment = new ShipmentAssignment
        {
            ShipmentId = shipment.Id,
            AssignmentType = request.AssignmentType,
            AssignedToEmployeeId = emp.Id,
            AssignedBranchId = branchId,
            AssignedVehicleId = request.AssignedVehicleId?.ToString(),
            AssignedAt = DateTime.UtcNow,
            AssignedByUserId = _currentUserService.UserId,
            Status = "Assigned",
            Notes = request.Notes
        };
        assignment.AssignedToEmployee = emp;
        assignment.AssignedBranch = branch;

        _context.ShipmentAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Create", "shipping", "ShipmentAssignments", assignment.Id.ToString(), null, new { assignment.AssignmentType, assignment.AssignedToEmployeeId }, null, cancellationToken);

        return MapDto(assignment);
    }

    public async Task<ShipmentAssignmentDto> UpdateAssignmentAsync(string shipmentIdentifier, long assignmentId, UpdateShipmentAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var assignment = shipment.Assignments.FirstOrDefault(a => a.Id == assignmentId)
            ?? throw new NotFoundException("ShipmentAssignment", assignmentId);

        var validStatuses = new[] { "Assigned", "Accepted", "InProgress", "Completed", "Failed" };
        if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Invalid assignment status '{request.Status}'.");

        assignment.Status = request.Status;
        if (request.AssignedVehicleId.HasValue) assignment.AssignedVehicleId = request.AssignedVehicleId.Value.ToString();
        assignment.Notes = request.Notes ?? assignment.Notes;
        if (string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            assignment.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapDto(assignment);
    }
}

// ═══════════════════════════════════════════════════════
// 6. Delivery Attempt Service
// ═══════════════════════════════════════════════════════
public class DeliveryAttemptService : IDeliveryAttemptService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IOutboxService? _outbox;

    public DeliveryAttemptService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    public DeliveryAttemptService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IOutboxService? outbox = null)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _outbox = outbox;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.Include(x => x.DeliveryAttempts).ThenInclude(a => a.AttemptedByEmployee).Include(x => x.DeliveryAttempts).ThenInclude(a => a.ProofOfDelivery).FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.Include(x => x.DeliveryAttempts).ThenInclude(a => a.AttemptedByEmployee).Include(x => x.DeliveryAttempts).ThenInclude(a => a.ProofOfDelivery).FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.Include(x => x.DeliveryAttempts).ThenInclude(a => a.AttemptedByEmployee).Include(x => x.DeliveryAttempts).ThenInclude(a => a.ProofOfDelivery).FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static DeliveryAttemptDto MapDto(DeliveryAttempt a) => new()
    {
        Id = a.Id,
        ShipmentId = a.ShipmentId,
        TrackingNumber = a.Shipment?.TrackingNumber ?? string.Empty,
        AttemptNumber = a.AttemptNumber,
        AttemptedByEmployeeId = a.AttemptedByEmployeeId,
        AttemptedByEmployeeName = a.AttemptedByEmployee?.FullName ?? string.Empty,
        AttemptedAt = a.AttemptedAt,
        Status = a.Status,
        FailureReasonCode = a.FailureReasonCode,
        FailureNotes = a.FailureNotes,
        NextScheduledAttemptDate = a.NextScheduledAttemptDate,
        RecipientContacted = a.RecipientContacted,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        ProofOfDelivery = a.ProofOfDelivery != null ? new ProofOfDeliveryDto
        {
            Id = a.ProofOfDelivery.Id,
            ShipmentId = a.ProofOfDelivery.ShipmentId,
            DeliveryAttemptId = a.ProofOfDelivery.DeliveryAttemptId,
            RecipientName = a.ProofOfDelivery.RecipientName,
            RecipientRelation = a.ProofOfDelivery.RecipientRelation,
            RecipientNationalId = a.ProofOfDelivery.RecipientNationalId,
            DeliveredAt = a.ProofOfDelivery.DeliveredAt,
            SignatureAttachmentId = a.ProofOfDelivery.SignatureAttachmentId,
            PhotoAttachmentId = a.ProofOfDelivery.PhotoAttachmentId,
            Notes = a.ProofOfDelivery.Notes,
            Latitude = a.ProofOfDelivery.Latitude,
            Longitude = a.ProofOfDelivery.Longitude,
            VerifiedByUserId = a.ProofOfDelivery.VerifiedByUserId.HasValue ? a.ProofOfDelivery.VerifiedByUserId.Value.ToString() : null,
            VerifiedAt = a.ProofOfDelivery.VerifiedAt
        } : null
    };

    public async Task<IReadOnlyList<DeliveryAttemptDto>> GetAttemptsAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        return s.DeliveryAttempts.OrderByDescending(a => a.AttemptNumber).Select(MapDto).ToList();
    }

    public async Task<DeliveryAttemptDto?> GetAttemptByIdAsync(string shipmentIdentifier, long attemptId, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var attempt = s.DeliveryAttempts.FirstOrDefault(a => a.Id == attemptId);
        return attempt == null ? null : MapDto(attempt);
    }

    public async Task<DeliveryAttemptDto> RecordAttemptAsync(string shipmentIdentifier, RecordDeliveryAttemptRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);

        if (!string.Equals(shipment.ShipmentStatus, "OutForDelivery", StringComparison.OrdinalIgnoreCase))
            throw new ConflictException($"Cannot record delivery attempt. Shipment status is '{shipment.ShipmentStatus}' (expected: OutForDelivery).");

        var emp = await _context.Employees.FindAsync(new object[] { request.AttemptedByEmployeeId }, cancellationToken)
            ?? throw new NotFoundException("Employee", request.AttemptedByEmployeeId);

        if (string.Equals(request.Status, "Failed", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.FailureReasonCode))
            throw new ConflictException("A failure reason code is required when recording a failed delivery attempt.");

        var actor = _currentUserService.Username ?? "System";
        var attemptNumber = (shipment.DeliveryAttempts.Any() ? shipment.DeliveryAttempts.Max(a => a.AttemptNumber) : 0) + 1;

        var attempt = new DeliveryAttempt
        {
            ShipmentId = shipment.Id,
            AttemptNumber = attemptNumber,
            AttemptedByEmployeeId = emp.Id,
            AttemptedAt = request.AttemptedAt,
            Status = request.Status,
            FailureReasonCode = request.FailureReasonCode,
            FailureNotes = request.FailureNotes,
            NextScheduledAttemptDate = request.NextScheduledAttemptDate,
            RecipientContacted = request.RecipientContacted,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };
        attempt.AttemptedByEmployee = emp;
        attempt.Shipment = shipment;

        if (string.Equals(request.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            shipment.ShipmentStatus = "Delivered";
            shipment.ActualDeliveryDate = request.AttemptedAt;
            shipment.UpdatedAt = DateTime.UtcNow;
            shipment.UpdatedBy = actor;
            _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
            {
                ShipmentId = shipment.Id,
                Status = "Delivered",
                Notes = $"Delivery recorded by employee {emp.FullName}.",
                ChangedByUserId = _currentUserService.UserId,
                ChangedAt = DateTime.UtcNow,
                Source = "Internal",
                CreatedAt = DateTime.UtcNow
            });
        }
        else if (string.Equals(request.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            shipment.ShipmentStatus = "DeliveryFailed";
            shipment.UpdatedAt = DateTime.UtcNow;
            shipment.UpdatedBy = actor;
            _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
            {
                ShipmentId = shipment.Id,
                Status = "DeliveryFailed",
                Notes = $"Delivery failed: {request.FailureReasonCode}. {request.FailureNotes}",
                ChangedByUserId = _currentUserService.UserId,
                ChangedAt = DateTime.UtcNow,
                Source = "Internal",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (string.Equals(request.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            _outbox?.Enqueue(
                new DeliveryCompletedIntegrationEvent(shipment.Id, shipment.TrackingNumber, shipment.CustomerId,
                    shipment.DestinationBranchId, request.AttemptedAt, null),
                "Shipment", shipment.Id.ToString());
        }
        else if (string.Equals(request.Status, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            _outbox?.Enqueue(
                new DeliveryFailedIntegrationEvent(shipment.Id, shipment.TrackingNumber, shipment.CustomerId,
                    shipment.DestinationBranchId, request.FailureReasonCode ?? "Unknown"),
                "Shipment", shipment.Id.ToString());
        }

        _context.DeliveryAttempts.Add(attempt);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Create", "shipping", "DeliveryAttempts", attempt.Id.ToString(), null, new { attempt.AttemptNumber, attempt.Status, attempt.FailureReasonCode }, null, cancellationToken);

        return MapDto(attempt);
    }
}

// ═══════════════════════════════════════════════════════
// 7. Proof of Delivery Service
// ═══════════════════════════════════════════════════════
public class ProofOfDeliveryService : IProofOfDeliveryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ProofOfDeliveryService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static ProofOfDeliveryDto MapDto(ProofOfDelivery pod) => new()
    {
        Id = pod.Id,
        ShipmentId = pod.ShipmentId,
        DeliveryAttemptId = pod.DeliveryAttemptId,
        RecipientName = pod.RecipientName,
        RecipientRelation = pod.RecipientRelation,
        RecipientNationalId = pod.RecipientNationalId,
        DeliveredAt = pod.DeliveredAt,
        SignatureAttachmentId = pod.SignatureAttachmentId,
        PhotoAttachmentId = pod.PhotoAttachmentId,
        Notes = pod.Notes,
        Latitude = pod.Latitude,
        Longitude = pod.Longitude,
        VerifiedByUserId = pod.VerifiedByUserId.HasValue ? pod.VerifiedByUserId.Value.ToString() : null,
        VerifiedAt = pod.VerifiedAt
    };

    public async Task<ProofOfDeliveryDto?> GetByShipmentAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var pod = await _context.ProofOfDeliveries.FirstOrDefaultAsync(p => p.ShipmentId == s.Id, cancellationToken);
        return pod == null ? null : MapDto(pod);
    }

    public async Task<ProofOfDeliveryDto?> GetByAttemptIdAsync(string shipmentIdentifier, long attemptId, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var pod = await _context.ProofOfDeliveries.FirstOrDefaultAsync(p => p.ShipmentId == s.Id && p.DeliveryAttemptId == attemptId, cancellationToken);
        return pod == null ? null : MapDto(pod);
    }

    public async Task<ProofOfDeliveryDto> CreateAsync(string shipmentIdentifier, long attemptId, CreateProofOfDeliveryRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);

        var attempt = await _context.DeliveryAttempts.FirstOrDefaultAsync(a =>
            a.Id == attemptId && a.ShipmentId == shipment.Id, cancellationToken)
            ?? throw new NotFoundException("DeliveryAttempt", attemptId);

        var existing = await _context.ProofOfDeliveries.AnyAsync(p => p.ShipmentId == shipment.Id, cancellationToken);
        if (existing)
            throw new ConflictException("A proof of delivery already exists for this shipment.");

        if (request.SignatureAttachmentId.HasValue)
        {
            var sig = await _context.FileAttachments.FindAsync(new object[] { request.SignatureAttachmentId.Value }, cancellationToken);
            if (sig == null) throw new NotFoundException("FileAttachment", request.SignatureAttachmentId!.Value);
        }
        if (request.PhotoAttachmentId.HasValue)
        {
            var photo = await _context.FileAttachments.FindAsync(new object[] { request.PhotoAttachmentId.Value }, cancellationToken);
            if (photo == null) throw new NotFoundException("FileAttachment", request.PhotoAttachmentId!.Value);
        }

        var pod = new ProofOfDelivery
        {
            ShipmentId = shipment.Id,
            DeliveryAttemptId = attemptId,
            RecipientName = request.RecipientName,
            RecipientRelation = request.RecipientRelation,
            RecipientNationalId = request.RecipientNationalId,
            DeliveredAt = request.DeliveredAt,
            SignatureAttachmentId = request.SignatureAttachmentId,
            PhotoAttachmentId = request.PhotoAttachmentId,
            Notes = request.Notes,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        _context.ProofOfDeliveries.Add(pod);

        if (!string.Equals(shipment.ShipmentStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            var actor = _currentUserService.Username ?? "System";
            shipment.ShipmentStatus = "Delivered";
            shipment.ActualDeliveryDate = request.DeliveredAt;
            shipment.UpdatedAt = DateTime.UtcNow;
            shipment.UpdatedBy = actor;

            _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
            {
                ShipmentId = shipment.Id,
                Status = "Delivered",
                Notes = $"Delivered to {request.RecipientName} ({request.RecipientRelation}).",
                ChangedByUserId = _currentUserService.UserId,
                ChangedAt = DateTime.UtcNow,
                Source = "Internal",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Create", "shipping", "ProofOfDeliveries", pod.Id.ToString(), null, new { pod.RecipientName, pod.DeliveredAt }, null, cancellationToken);

        return MapDto(pod);
    }
}

// ═══════════════════════════════════════════════════════
// 8. Return Request Service
// ═══════════════════════════════════════════════════════
public class ReturnRequestService : IReturnRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly INumberSequenceService _numberSequenceService;
    private readonly IOutboxService? _outbox;

    public ReturnRequestService(
        ApplicationDbContext context,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        INumberSequenceService numberSequenceService)
        INumberSequenceService numberSequenceService,
        IOutboxService? outbox = null)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _numberSequenceService = numberSequenceService;
        _outbox = outbox;
    }

    private static ReturnRequestDto MapDto(ReturnRequest r) => new()
    {
        Id = r.Id,
        PublicId = r.PublicId,
        ReturnCode = r.ReturnCode,
        OriginalShipmentId = r.OriginalShipmentId,
        OriginalTrackingNumber = r.OriginalShipment?.TrackingNumber ?? string.Empty,
        NewReturnShipmentId = r.NewReturnShipmentId,
        NewReturnTrackingNumber = r.NewReturnShipment?.TrackingNumber,
        ReasonCode = r.ReasonCode,
        ReasonDescription = r.ReasonDescription,
        Status = r.Status,
        RequestedAt = r.RequestedAt,
        ApprovedByUserId = r.ApprovedByUserId.HasValue ? r.ApprovedByUserId.Value.ToString() : null,
        ApprovedAt = r.ApprovedAt,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        CreatedBy = r.CreatedBy,
        UpdatedAt = r.UpdatedAt,
        UpdatedBy = r.UpdatedBy
    };

    private async Task<ReturnRequest> FindAsync(string identifier, CancellationToken ct)
    {
        var query = _context.ReturnRequests
            .Include(r => r.OriginalShipment)
            .Include(r => r.NewReturnShipment);

        ReturnRequest? rr = null;
        if (long.TryParse(identifier, out var id)) rr = await query.FirstOrDefaultAsync(r => r.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid)) rr = await query.FirstOrDefaultAsync(r => r.PublicId == guid, ct);
        else rr = await query.FirstOrDefaultAsync(r => r.ReturnCode == identifier, ct);

        return rr ?? throw new NotFoundException("ReturnRequest", identifier);
    }

    public async Task<PagedResult<ReturnRequestDto>> GetReturnRequestsAsync(
        string? search, string? status, long? customerId,
        DateTime? dateFrom, DateTime? dateTo, PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReturnRequests
            .Include(r => r.OriginalShipment)
            .Include(r => r.NewReturnShipment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.ReturnCode.Contains(search) || r.ReasonCode.Contains(search) || r.OriginalShipment.TrackingNumber.Contains(search));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);
        if (customerId.HasValue)
            query = query.Where(r => r.OriginalShipment.CustomerId == customerId.Value);
        if (dateFrom.HasValue)
            query = query.Where(r => r.RequestedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(r => r.RequestedAt <= dateTo.Value);

        var total = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapDto(r))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReturnRequestDto>(items, total, page, pageSize);
    }

    public async Task<ReturnRequestDto?> GetByIdOrCodeAsync(string identifier, CancellationToken cancellationToken = default)
    {
        try { return MapDto(await FindAsync(identifier, cancellationToken)); }
        catch (NotFoundException) { return null; }
    }

    public async Task<ReturnRequestDto> CreateAsync(CreateReturnRequestRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == request.OriginalShipmentId, cancellationToken)
            ?? throw new NotFoundException("Shipment", request.OriginalShipmentId);

        if (!new[] { "Delivered", "DeliveryFailed", "Cancelled" }.Contains(shipment.ShipmentStatus, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Return requests can only be created for Delivered, DeliveryFailed, or Cancelled shipments. Current status: '{shipment.ShipmentStatus}'.");

        var activeReturn = await _context.ReturnRequests.AnyAsync(r =>
            r.OriginalShipmentId == shipment.Id &&
            !new[] { "Rejected", "ReturnedToSender" }.Contains(r.Status), cancellationToken);
        if (activeReturn)
            throw new ConflictException("An active return request already exists for this shipment.");

        var returnCode = await _numberSequenceService.GetNextNumberAsync("RETURN", cancellationToken);
        var actor = _currentUserService.Username ?? "System";

        var rr = new ReturnRequest
        {
            ReturnCode = returnCode,
            OriginalShipmentId = shipment.Id,
            ReasonCode = request.ReasonCode,
            ReasonDescription = request.ReasonDescription,
            Status = "Requested",
            RequestedAt = DateTime.UtcNow,
            Notes = request.Notes,
            CreatedBy = actor
        };
        rr.OriginalShipment = shipment;

        _context.ReturnRequests.Add(rr);
        await _context.SaveChangesAsync(cancellationToken);

        if (_outbox != null)
        {
            _outbox.Enqueue(
                new ReturnInitiatedIntegrationEvent(rr.Id, rr.ReturnCode, shipment.Id, shipment.CustomerId,
                    shipment.DestinationBranchId, rr.ReasonCode),
                "ReturnRequest", rr.Id.ToString());
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _auditService.RecordAsync("Create", "shipping", "ReturnRequests", rr.Id.ToString(), null, new { rr.ReturnCode, rr.ReasonCode }, null, cancellationToken);
        return MapDto(rr);
    }

    public async Task<ReturnRequestDto> ProcessAsync(string identifier, ProcessReturnRequestRequest request, CancellationToken cancellationToken = default)
    {
        var rr = await FindAsync(identifier, cancellationToken);
        var actor = _currentUserService.Username ?? "System";

        if (string.Equals(request.Decision, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(rr.Status, "Requested", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException($"Can only approve return requests in 'Requested' status. Current: '{rr.Status}'.");

            rr.Status = "Approved";
            rr.ApprovedByUserId = _currentUserService.UserId;
            rr.ApprovedAt = DateTime.UtcNow;
            rr.Notes = request.Notes ?? rr.Notes;
            rr.UpdatedAt = DateTime.UtcNow;
            rr.UpdatedBy = actor;

            // Trigger returning state on original shipment
            var shipment = rr.OriginalShipment;
            if (shipment != null && shipment.ShipmentStatus != "Returning" && shipment.ShipmentStatus != "Returned")
            {
                shipment.ShipmentStatus = "Returning";
                shipment.UpdatedAt = DateTime.UtcNow;
                shipment.UpdatedBy = actor;
                _context.ShipmentStatusHistories.Add(new ShipmentStatusHistory
                {
                    ShipmentId = shipment.Id,
                    Status = "Returning",
                    Notes = $"Return request {rr.ReturnCode} approved.",
                    ChangedByUserId = _currentUserService.UserId,
                    ChangedAt = DateTime.UtcNow,
                    Source = "Internal",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else if (string.Equals(request.Decision, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (!new[] { "Requested", "Approved" }.Contains(rr.Status, StringComparer.OrdinalIgnoreCase))
                throw new ConflictException($"Can only reject return requests in 'Requested' or 'Approved' status. Current: '{rr.Status}'.");

            rr.Status = "Rejected";
            rr.Notes = request.Notes ?? rr.Notes;
            rr.UpdatedAt = DateTime.UtcNow;
            rr.UpdatedBy = actor;
        }
        else
        {
            throw new ConflictException($"Invalid decision '{request.Decision}'. Expected 'Approved' or 'Rejected'.");
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Process", "shipping", "ReturnRequests", rr.Id.ToString(), null, new { rr.Status, request.Decision }, null, cancellationToken);
        return MapDto(rr);
    }
}

// ═══════════════════════════════════════════════════════
// 9. Shipment Route Service
// ═══════════════════════════════════════════════════════
public class ShipmentRouteService : IShipmentRouteService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public ShipmentRouteService(ApplicationDbContext context, IAuditService auditService, ICurrentUserService currentUserService)
    {
        _context = context;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    private async Task<Shipment> GetShipmentAsync(string identifier, CancellationToken ct)
    {
        Shipment? s = null;
        if (long.TryParse(identifier, out var id))
            s = await _context.Shipments.Include(x => x.Routes).ThenInclude(r => r.FromWarehouse).Include(x => x.Routes).ThenInclude(r => r.ToWarehouse).Include(x => x.Routes).ThenInclude(r => r.Carrier).FirstOrDefaultAsync(x => x.Id == id, ct);
        else if (Guid.TryParse(identifier, out var guid))
            s = await _context.Shipments.Include(x => x.Routes).ThenInclude(r => r.FromWarehouse).Include(x => x.Routes).ThenInclude(r => r.ToWarehouse).Include(x => x.Routes).ThenInclude(r => r.Carrier).FirstOrDefaultAsync(x => x.PublicId == guid, ct);
        else
            s = await _context.Shipments.Include(x => x.Routes).ThenInclude(r => r.FromWarehouse).Include(x => x.Routes).ThenInclude(r => r.ToWarehouse).Include(x => x.Routes).ThenInclude(r => r.Carrier).FirstOrDefaultAsync(x => x.TrackingNumber == identifier, ct);

        return s ?? throw new NotFoundException("Shipment", identifier);
    }

    private static ShipmentRouteDto MapDto(ShipmentRoute r) => new()
    {
        Id = r.Id,
        ShipmentId = r.ShipmentId,
        SequenceOrder = r.SequenceOrder,
        FromWarehouseId = r.FromWarehouseId,
        FromWarehouseName = r.FromWarehouse?.Name ?? string.Empty,
        ToWarehouseId = r.ToWarehouseId,
        ToWarehouseName = r.ToWarehouse?.Name ?? string.Empty,
        TransportType = r.TransportType,
        CarrierId = r.CarrierId,
        CarrierName = r.Carrier?.Name,
        VehicleNumber = r.VehicleNumber,
        EstimatedDeparture = r.EstimatedDeparture,
        ActualDeparture = r.ActualDeparture,
        EstimatedArrival = r.EstimatedArrival,
        ActualArrival = r.ActualArrival,
        Status = r.Status
    };

    public async Task<IReadOnlyList<ShipmentRouteDto>> GetRoutesAsync(string shipmentIdentifier, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        return s.Routes.OrderBy(r => r.SequenceOrder).Select(MapDto).ToList();
    }

    public async Task<ShipmentRouteDto?> GetRouteByIdAsync(string shipmentIdentifier, long routeId, CancellationToken cancellationToken = default)
    {
        var s = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var r = s.Routes.FirstOrDefault(x => x.Id == routeId);
        return r == null ? null : MapDto(r);
    }

    public async Task<ShipmentRouteDto> AddRouteAsync(string shipmentIdentifier, AddShipmentRouteRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (ShipmentStateHelper.IsTerminal(shipment.ShipmentStatus))
            throw new ConflictException($"Cannot add routes to a shipment in terminal status '{shipment.ShipmentStatus}'.");

        var fromWarehouse = await _context.Warehouses.FindAsync(new object[] { request.FromWarehouseId }, cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.FromWarehouseId);
        var toWarehouse = await _context.Warehouses.FindAsync(new object[] { request.ToWarehouseId }, cancellationToken)
            ?? throw new NotFoundException("Warehouse", request.ToWarehouseId);

        var route = new ShipmentRoute
        {
            ShipmentId = shipment.Id,
            SequenceOrder = request.SequenceOrder,
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            TransportType = request.TransportType,
            CarrierId = request.CarrierId,
            VehicleNumber = request.VehicleNumber,
            EstimatedDeparture = request.EstimatedDeparture,
            EstimatedArrival = request.EstimatedArrival,
            Status = "Planned"
        };
        route.FromWarehouse = fromWarehouse;
        route.ToWarehouse = toWarehouse;

        _context.ShipmentRoutes.Add(route);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.RecordAsync("Create", "shipping", "ShipmentRoutes", route.Id.ToString(), null, new { route.SequenceOrder, route.TransportType }, null, cancellationToken);
        return MapDto(route);
    }

    public async Task<ShipmentRouteDto> UpdateRouteAsync(string shipmentIdentifier, long routeId, UpdateShipmentRouteRequest request, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        var route = shipment.Routes.FirstOrDefault(r => r.Id == routeId)
            ?? throw new NotFoundException("ShipmentRoute", routeId);

        var validStatuses = new[] { "Planned", "Dispatched", "Arrived", "Delayed", "Cancelled" };
        if (!string.IsNullOrWhiteSpace(request.Status) && !validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            throw new ConflictException($"Invalid route status '{request.Status}'.");

        if (!string.IsNullOrWhiteSpace(request.Status)) route.Status = request.Status;
        if (request.VehicleNumber != null) route.VehicleNumber = request.VehicleNumber;
        if (request.EstimatedDeparture.HasValue) route.EstimatedDeparture = request.EstimatedDeparture;
        if (request.ActualDeparture.HasValue) route.ActualDeparture = request.ActualDeparture;
        if (request.EstimatedArrival.HasValue) route.EstimatedArrival = request.EstimatedArrival;
        if (request.ActualArrival.HasValue) route.ActualArrival = request.ActualArrival;

        if (string.Equals(request.Status, "Dispatched", StringComparison.OrdinalIgnoreCase) && !route.ActualDeparture.HasValue)
            route.ActualDeparture = DateTime.UtcNow;
        if (string.Equals(request.Status, "Arrived", StringComparison.OrdinalIgnoreCase) && !route.ActualArrival.HasValue)
            route.ActualArrival = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return MapDto(route);
    }

    public async Task RemoveRouteAsync(string shipmentIdentifier, long routeId, CancellationToken cancellationToken = default)
    {
        var shipment = await GetShipmentAsync(shipmentIdentifier, cancellationToken);
        if (ShipmentStateHelper.IsTerminal(shipment.ShipmentStatus))
            throw new ConflictException("Cannot remove routes from a terminal shipment.");

        var route = shipment.Routes.FirstOrDefault(r => r.Id == routeId)
            ?? throw new NotFoundException("ShipmentRoute", routeId);

        _context.ShipmentRoutes.Remove(route);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync("Delete", "shipping", "ShipmentRoutes", routeId.ToString(), new { route.SequenceOrder }, null, null, cancellationToken);
    }
}
