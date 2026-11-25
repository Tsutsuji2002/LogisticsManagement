using System.Text.Json;
using Logistics.Server.Application.Common.Events;
using Logistics.Server.Application.Common.Interfaces;
using Logistics.Server.Application.Modules.Notification.Services;

namespace Logistics.Server.Infrastructure.Services;

/// <summary>
/// Idempotent Outbox Event Handlers translating cross-module integration events into notifications.
/// Uses deterministic EventKeys ("outbox:{MessageId}:...") so duplicate deliveries never create duplicate notifications.
/// </summary>
public abstract class OutboxNotificationHandlerBase<TEvent> : IOutboxEventHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected readonly INotificationService NotificationService;

    protected OutboxNotificationHandlerBase(INotificationService notificationService)
    {
        NotificationService = notificationService;
    }

    public abstract string EventType { get; }

    public async Task HandleAsync(OutboxEventContext context, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<TEvent>(context.PayloadJson, JsonOptions);
        if (payload == null) return;

        await HandleTypedAsync(context, payload, cancellationToken);
    }

    protected abstract Task HandleTypedAsync(OutboxEventContext context, TEvent evt, CancellationToken cancellationToken);
}

public sealed class OrderConfirmedOutboxHandler : OutboxNotificationHandlerBase<OrderConfirmedIntegrationEvent>
{
    public OrderConfirmedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.OrderConfirmed;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, OrderConfirmedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Order Confirmed: {evt.OrderNumber}",
            content: $"Order {evt.OrderNumber} has been confirmed and is ready for fulfillment (COD: {evt.CodAmount:N0}).",
            notificationType: "OrderConfirmed",
            referenceType: "Order",
            referenceId: evt.OrderId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:order-confirmed",
            cancellationToken: ct);
}

public sealed class OrderCancelledOutboxHandler : OutboxNotificationHandlerBase<OrderCancelledIntegrationEvent>
{
    public OrderCancelledOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.OrderCancelled;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, OrderCancelledIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Order Cancelled: {evt.OrderNumber}",
            content: $"Order {evt.OrderNumber} was cancelled. Reason: {evt.Reason}",
            notificationType: "OrderCancelled",
            priority: "High",
            referenceType: "Order",
            referenceId: evt.OrderId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:order-cancelled",
            cancellationToken: ct);
}

public sealed class ShipmentCreatedOutboxHandler : OutboxNotificationHandlerBase<ShipmentCreatedIntegrationEvent>
{
    public ShipmentCreatedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.ShipmentCreated;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, ShipmentCreatedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.OriginBranchId,
            title: $"Shipment Created: {evt.TrackingNumber}",
            content: $"New waybill {evt.TrackingNumber} has been created and is awaiting pickup.",
            notificationType: "ShipmentCreated",
            referenceType: "Shipment",
            referenceId: evt.ShipmentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:shipment-created",
            cancellationToken: ct);
}

public sealed class ShipmentStatusChangedOutboxHandler : OutboxNotificationHandlerBase<ShipmentStatusChangedIntegrationEvent>
{
    public ShipmentStatusChangedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.ShipmentStatusChanged;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, ShipmentStatusChangedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.DestinationBranchId,
            title: $"Shipment {evt.TrackingNumber}: {evt.NewStatus}",
            content: $"Shipment {evt.TrackingNumber} transitioned from {evt.PreviousStatus} to {evt.NewStatus}.",
            notificationType: "ShipmentStatusChanged",
            referenceType: "Shipment",
            referenceId: evt.ShipmentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:shipment-status",
            cancellationToken: ct);
}

public sealed class DeliveryCompletedOutboxHandler : OutboxNotificationHandlerBase<DeliveryCompletedIntegrationEvent>
{
    public DeliveryCompletedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.DeliveryCompleted;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, DeliveryCompletedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.DestinationBranchId,
            title: $"Delivered: {evt.TrackingNumber}",
            content: $"Shipment {evt.TrackingNumber} was successfully delivered{(string.IsNullOrEmpty(evt.RecipientName) ? "" : $" to {evt.RecipientName}")}.",
            notificationType: "DeliveryCompleted",
            referenceType: "Shipment",
            referenceId: evt.ShipmentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:delivery-completed",
            cancellationToken: ct);
}

public sealed class DeliveryFailedOutboxHandler : OutboxNotificationHandlerBase<DeliveryFailedIntegrationEvent>
{
    public DeliveryFailedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.DeliveryFailed;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, DeliveryFailedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.DestinationBranchId,
            title: $"Delivery Failed: {evt.TrackingNumber}",
            content: $"Delivery attempt for {evt.TrackingNumber} failed ({evt.FailureReasonCode}). Action required.",
            notificationType: "DeliveryFailed",
            priority: "High",
            referenceType: "Shipment",
            referenceId: evt.ShipmentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:delivery-failed",
            cancellationToken: ct);
}

public sealed class ReturnInitiatedOutboxHandler : OutboxNotificationHandlerBase<ReturnInitiatedIntegrationEvent>
{
    public ReturnInitiatedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.ReturnInitiated;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, ReturnInitiatedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Return Initiated: {evt.ReturnNumber}",
            content: $"Return request {evt.ReturnNumber} created for shipment {evt.ShipmentId}. Reason: {evt.Reason}",
            notificationType: "ReturnInitiated",
            priority: "High",
            referenceType: "ReturnRequest",
            referenceId: evt.ReturnRequestId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:return-initiated",
            cancellationToken: ct);
}

public sealed class CarrierBookingOutcomeOutboxHandler : OutboxNotificationHandlerBase<CarrierBookingOutcomeIntegrationEvent>
{
    public CarrierBookingOutcomeOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.CarrierBookingOutcome;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, CarrierBookingOutcomeIntegrationEvent evt, CancellationToken ct)
    {
        if (evt.Succeeded) return Task.CompletedTask;

        return NotificationService.CreateSystemNotificationAsync(
            title: $"Carrier Booking Failed ({evt.CarrierCode})",
            content: $"Failed to book shipment {evt.ShipmentId} with {evt.CarrierCode}: {evt.ErrorSummary}",
            notificationType: "CarrierBookingFailed",
            priority: "High",
            referenceType: "Shipment",
            referenceId: evt.ShipmentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:carrier-booking-failed",
            cancellationToken: ct);
    }
}

public sealed class CarrierSyncFailedOutboxHandler : OutboxNotificationHandlerBase<CarrierSyncFailedIntegrationEvent>
{
    public CarrierSyncFailedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.CarrierSyncFailed;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, CarrierSyncFailedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateSystemNotificationAsync(
            title: $"Carrier Sync Failures ({evt.CarrierCode})",
            content: $"Tracking sync job #{evt.SyncJobId} for carrier {evt.CarrierCode} recorded {evt.FailureCount} failures.",
            notificationType: "CarrierSyncFailed",
            priority: "High",
            referenceType: "CarrierSyncJob",
            referenceId: evt.SyncJobId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:carrier-sync-failed",
            cancellationToken: ct);
}

public sealed class InventoryReceiptCompletedOutboxHandler : OutboxNotificationHandlerBase<InventoryReceiptCompletedIntegrationEvent>
{
    public InventoryReceiptCompletedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.InventoryReceiptCompleted;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, InventoryReceiptCompletedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Inventory Receipt Completed: {evt.ReceiptNumber}",
            content: $"Warehouse receipt {evt.ReceiptNumber} has been finalized and stock balances updated.",
            notificationType: "InventoryReceiptCompleted",
            referenceType: "WarehouseReceipt",
            referenceId: evt.ReceiptId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:inv-receipt",
            cancellationToken: ct);
}

public sealed class InvoiceIssuedOutboxHandler : OutboxNotificationHandlerBase<InvoiceIssuedIntegrationEvent>
{
    public InvoiceIssuedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.InvoiceIssued;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, InvoiceIssuedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Invoice Issued: {evt.InvoiceNumber}",
            content: $"Invoice {evt.InvoiceNumber} for total amount {evt.TotalAmount:N0} has been formally issued.",
            notificationType: "InvoiceIssued",
            referenceType: "Invoice",
            referenceId: evt.InvoiceId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:invoice-issued",
            cancellationToken: ct);
}

public sealed class PaymentConfirmedOutboxHandler : OutboxNotificationHandlerBase<PaymentConfirmedIntegrationEvent>
{
    public PaymentConfirmedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.PaymentConfirmed;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, PaymentConfirmedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"Payment Confirmed: {evt.PaymentNumber}",
            content: $"Payment {evt.PaymentNumber} for {evt.Amount:N0} has been confirmed.",
            notificationType: "PaymentConfirmed",
            referenceType: "Payment",
            referenceId: evt.PaymentId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:payment-confirmed",
            cancellationToken: ct);
}

public sealed class CodCollectedOutboxHandler : OutboxNotificationHandlerBase<CodCollectedIntegrationEvent>
{
    public CodCollectedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.CodCollected;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, CodCollectedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"COD Verified: {evt.TrackingNumber}",
            content: $"COD amount {evt.CollectedAmount:N0} for shipment {evt.TrackingNumber} has been verified by cashier.",
            notificationType: "CodVerified",
            referenceType: "CODCollection",
            referenceId: evt.CollectionId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:cod-verified",
            cancellationToken: ct);
}

public sealed class CodRemittedOutboxHandler : OutboxNotificationHandlerBase<CodRemittedIntegrationEvent>
{
    public CodRemittedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.CodRemitted;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, CodRemittedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"COD Remittance Confirmed: {evt.RemittanceNumber}",
            content: $"COD remittance batch {evt.RemittanceNumber} (Net: {evt.NetRemittanceAmount:N0}) has been settled to customer.",
            notificationType: "CodRemitted",
            referenceType: "CODRemittance",
            referenceId: evt.RemittanceId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:cod-remitted",
            cancellationToken: ct);
}

public sealed class CodDiscrepancyDetectedOutboxHandler : OutboxNotificationHandlerBase<CodDiscrepancyDetectedIntegrationEvent>
{
    public CodDiscrepancyDetectedOutboxHandler(INotificationService notificationService) : base(notificationService) { }
    public override string EventType => IntegrationEventTypes.CodDiscrepancyDetected;

    protected override Task HandleTypedAsync(OutboxEventContext ctx, CodDiscrepancyDetectedIntegrationEvent evt, CancellationToken ct) =>
        NotificationService.CreateForBranchAsync(
            branchId: evt.BranchId,
            title: $"COD Discrepancy Alert ({evt.Scope})",
            content: $"A COD discrepancy of {evt.DiscrepancyAmount:N0} was detected for {evt.Scope} #{evt.EntityId}.",
            notificationType: "CodDiscrepancy",
            priority: "Urgent",
            referenceType: evt.Scope,
            referenceId: evt.EntityId,
            eventKeyPrefix: $"outbox:{ctx.MessageId}:cod-discrepancy",
            cancellationToken: ct);
}

