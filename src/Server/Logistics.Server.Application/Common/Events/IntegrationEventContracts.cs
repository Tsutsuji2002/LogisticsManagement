namespace Logistics.Server.Application.Common.Events;

/// <summary>
/// Stable integration event type identifiers persisted in OutboxMessages.
/// Decoupled from CLR assembly-qualified names to preserve version compatibility.
/// </summary>
public static class IntegrationEventTypes
{
    // Sales
    public const string OrderCreated = "sales.order.created.v1";
    public const string OrderConfirmed = "sales.order.confirmed.v1";
    public const string OrderCancelled = "sales.order.cancelled.v1";

    // Shipping
    public const string ShipmentCreated = "shipping.shipment.created.v1";
    public const string PickupScheduled = "shipping.pickup.scheduled.v1";
    public const string ShipmentStatusChanged = "shipping.shipment.status-changed.v1";
    public const string DeliveryCompleted = "shipping.delivery.completed.v1";
    public const string DeliveryFailed = "shipping.delivery.failed.v1";
    public const string ReturnInitiated = "shipping.return.initiated.v1";

    // Carrier Integration
    public const string CarrierBookingOutcome = "carrier.booking.outcome.v1";
    public const string CarrierTrackingReceived = "carrier.tracking.received.v1";
    public const string CarrierSyncFailed = "carrier.sync.failed.v1";

    // Warehouse & Inventory
    public const string ParcelHandled = "warehouse.parcel.handled.v1";
    public const string InventoryReceiptCompleted = "warehouse.receipt.completed.v1";

    // Finance
    public const string ShipmentChargeCreated = "finance.charge.created.v1";
    public const string InvoiceIssued = "finance.invoice.issued.v1";
    public const string PaymentConfirmed = "finance.payment.confirmed.v1";

    // COD
    public const string CodCollected = "cod.collection.verified.v1";
    public const string CodRemitted = "cod.remittance.confirmed.v1";
    public const string CodDiscrepancyDetected = "cod.discrepancy.detected.v1";
}

/// <summary>
/// Base interface for stable cross-module integration events.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime OccurredAt { get; }
}

public abstract record IntegrationEventBase(string EventType) : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// ── Sales Events ──
public sealed record OrderCreatedIntegrationEvent(
    long OrderId,
    string OrderNumber,
    long CustomerId,
    long BranchId) : IntegrationEventBase(IntegrationEventTypes.OrderCreated);

public sealed record OrderConfirmedIntegrationEvent(
    long OrderId,
    string OrderNumber,
    long CustomerId,
    long BranchId,
    decimal CodAmount) : IntegrationEventBase(IntegrationEventTypes.OrderConfirmed);

public sealed record OrderCancelledIntegrationEvent(
    long OrderId,
    string OrderNumber,
    long CustomerId,
    long BranchId,
    string Reason) : IntegrationEventBase(IntegrationEventTypes.OrderCancelled);

// ── Shipping Events ──
public sealed record ShipmentCreatedIntegrationEvent(
    long ShipmentId,
    string TrackingNumber,
    long? OrderId,
    long CustomerId,
    long OriginBranchId,
    long DestinationBranchId) : IntegrationEventBase(IntegrationEventTypes.ShipmentCreated);

public sealed record PickupScheduledIntegrationEvent(
    long PickupRequestId,
    string RequestNumber,
    long CustomerId,
    long BranchId,
    DateTime ScheduledDate) : IntegrationEventBase(IntegrationEventTypes.PickupScheduled);

public sealed record ShipmentStatusChangedIntegrationEvent(
    long ShipmentId,
    string TrackingNumber,
    long CustomerId,
    long OriginBranchId,
    long DestinationBranchId,
    string PreviousStatus,
    string NewStatus) : IntegrationEventBase(IntegrationEventTypes.ShipmentStatusChanged);

public sealed record DeliveryCompletedIntegrationEvent(
    long ShipmentId,
    string TrackingNumber,
    long CustomerId,
    long DestinationBranchId,
    DateTime DeliveredAt,
    string? RecipientName) : IntegrationEventBase(IntegrationEventTypes.DeliveryCompleted);

public sealed record DeliveryFailedIntegrationEvent(
    long ShipmentId,
    string TrackingNumber,
    long CustomerId,
    long DestinationBranchId,
    string FailureReasonCode) : IntegrationEventBase(IntegrationEventTypes.DeliveryFailed);

public sealed record ReturnInitiatedIntegrationEvent(
    long ReturnRequestId,
    string ReturnNumber,
    long ShipmentId,
    long CustomerId,
    long BranchId,
    string Reason) : IntegrationEventBase(IntegrationEventTypes.ReturnInitiated);

// ── Carrier Integration Events ──
public sealed record CarrierBookingOutcomeIntegrationEvent(
    long ShipmentId,
    string CarrierCode,
    bool Succeeded,
    string? CarrierTrackingNumber,
    string? ErrorSummary) : IntegrationEventBase(IntegrationEventTypes.CarrierBookingOutcome);

public sealed record CarrierTrackingReceivedIntegrationEvent(
    string CarrierCode,
    string CarrierTrackingNumber,
    string EventCode,
    DateTime EventTimestamp) : IntegrationEventBase(IntegrationEventTypes.CarrierTrackingReceived);

public sealed record CarrierSyncFailedIntegrationEvent(
    long CarrierId,
    string CarrierCode,
    long SyncJobId,
    int FailureCount) : IntegrationEventBase(IntegrationEventTypes.CarrierSyncFailed);

// ── Warehouse Events ──
public sealed record ParcelHandledIntegrationEvent(
    long HandlingEventId,
    long ShipmentId,
    long WarehouseId,
    string HandlingEventType) : IntegrationEventBase(IntegrationEventTypes.ParcelHandled);

public sealed record InventoryReceiptCompletedIntegrationEvent(
    long ReceiptId,
    string ReceiptNumber,
    long WarehouseId,
    long BranchId) : IntegrationEventBase(IntegrationEventTypes.InventoryReceiptCompleted);

// ── Finance Events ──
public sealed record ShipmentChargeCreatedIntegrationEvent(
    long ShipmentChargeId,
    long ShipmentId,
    long CustomerId,
    decimal TotalAmount) : IntegrationEventBase(IntegrationEventTypes.ShipmentChargeCreated);

public sealed record InvoiceIssuedIntegrationEvent(
    long InvoiceId,
    string InvoiceNumber,
    long CustomerId,
    long BranchId,
    decimal TotalAmount) : IntegrationEventBase(IntegrationEventTypes.InvoiceIssued);

public sealed record PaymentConfirmedIntegrationEvent(
    long PaymentId,
    string PaymentNumber,
    long CustomerId,
    long BranchId,
    decimal Amount) : IntegrationEventBase(IntegrationEventTypes.PaymentConfirmed);

// ── COD Events ──
public sealed record CodCollectedIntegrationEvent(
    long CollectionId,
    long ShipmentId,
    string TrackingNumber,
    long CustomerId,
    long BranchId,
    decimal CollectedAmount) : IntegrationEventBase(IntegrationEventTypes.CodCollected);

public sealed record CodRemittedIntegrationEvent(
    long RemittanceId,
    string RemittanceNumber,
    long CustomerId,
    long BranchId,
    decimal NetRemittanceAmount) : IntegrationEventBase(IntegrationEventTypes.CodRemitted);

public sealed record CodDiscrepancyDetectedIntegrationEvent(
    long EntityId,
    string Scope,
    long BranchId,
    decimal DiscrepancyAmount) : IntegrationEventBase(IntegrationEventTypes.CodDiscrepancyDetected);

