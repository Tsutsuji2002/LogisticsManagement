using Logistics.Server.Domain.Common;
using Logistics.Server.Domain.Entities.Carrier;
using Logistics.Server.Domain.Entities.Configuration;
using Logistics.Server.Domain.Entities.Crm;
using Logistics.Server.Domain.Entities.Finance;
using Logistics.Server.Domain.Entities.Organization;
using Logistics.Server.Domain.Entities.Sales;

namespace Logistics.Server.Domain.Entities.Shipping;

public class Shipment : BaseEntity
{
    public string TrackingNumber { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public long CustomerId { get; set; }
    public long OriginBranchId { get; set; }
    public long DestinationBranchId { get; set; }
    public long? AssignedCarrierId { get; set; }
    public long? CarrierServiceId { get; set; }
    public string? CarrierTrackingNumber { get; set; }
    public string ShipmentStatus { get; set; } = "Draft"; // Draft, ReadyForPickup, PickedUp, InSortingHub, InTransit, OutForDelivery, Delivered, DeliveryFailed, Returning, Returned, Cancelled
    public string ServiceType { get; set; } = "StandardGround"; // SameDay, NextDayExpress, StandardGround, Economical
    public int TotalPackages { get; set; } = 1;
    public decimal ActualWeightKg { get; set; }
    public decimal ChargeableWeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
    public decimal CodAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal InsuranceFee { get; set; }
    public decimal SurchargesTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public long CurrencyId { get; set; }
    public DateTime? PickupDate { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Order? Order { get; set; }
    public virtual Customer Customer { get; set; } = null!;
    public virtual Branch OriginBranch { get; set; } = null!;
    public virtual Branch DestinationBranch { get; set; } = null!;
    public virtual Carrier.Carrier? AssignedCarrier { get; set; }
    public virtual CarrierService? CarrierService { get; set; }
    public virtual Currency Currency { get; set; } = null!;
    public virtual ICollection<ShipmentPackage> Packages { get; set; } = [];
    public virtual ICollection<ShipmentAddress> Addresses { get; set; } = [];
    public virtual ICollection<ShipmentStatusHistory> StatusHistories { get; set; } = [];
    public virtual ICollection<ShipmentAssignment> Assignments { get; set; } = [];
    public virtual ICollection<DeliveryAttempt> DeliveryAttempts { get; set; } = [];
    public virtual ProofOfDelivery? ProofOfDelivery { get; set; }
    public virtual ICollection<ShipmentRoute> Routes { get; set; } = [];
    public virtual ICollection<ShipmentCharge> Charges { get; set; } = [];
}
