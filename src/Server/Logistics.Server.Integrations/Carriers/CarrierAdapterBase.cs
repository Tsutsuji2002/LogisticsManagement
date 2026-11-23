namespace Logistics.Server.Integrations.Carriers;

/// <summary>
/// Base class providing default behaviors and simulated responses for carrier integration adapters.
/// </summary>
public abstract class CarrierAdapterBase : ICarrierAdapter
{
    public abstract string CarrierCode { get; }
    public abstract string CarrierName { get; }
    public virtual bool IsAvailable => true;

    public virtual Task<CarrierRateResponse> CalculateRatesAsync(CarrierRateRequest request, CancellationToken cancellationToken = default)
    {
        var options = new List<CarrierRateOption>
        {
            new("STANDARD", $"{CarrierName} Standard Ground", 30000m, 25000m, 5000m, 2),
            new("EXPRESS", $"{CarrierName} Express Air", 55000m, 48000m, 7000m, 1)
        };
        return Task.FromResult(new CarrierRateResponse(true, options));
    }

    public virtual Task<CarrierCreateShipmentResponse> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var trackingNumber = $"{CarrierCode}{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        var orderCode = $"ORD_{CarrierCode}_{DateTime.UtcNow.Ticks % 1000000}";
        return Task.FromResult(new CarrierCreateShipmentResponse(
            Success: true,
            CarrierTrackingNumber: trackingNumber,
            CarrierOrderCode: orderCode,
            TotalFee: 35000m,
            LabelUrl: $"https://api.{CarrierCode.ToLowerInvariant()}.vn/labels/{trackingNumber}.pdf"));
    }

    public virtual Task<CarrierTrackingInfo> GetTrackingAsync(string trackingNumber, CancellationToken cancellationToken = default)
    {
        var events = new List<CarrierTrackingEventInfo>
        {
            new("PICKED_UP", "Parcel received at origin hub", "Origin Hub", DateTime.UtcNow.AddHours(-10)),
            new("IN_TRANSIT", "In transit between sorting facilities", "Central Sorting Hub", DateTime.UtcNow.AddHours(-4)),
            new("OUT_FOR_DELIVERY", "Out for delivery with courier", "Local Station", DateTime.UtcNow.AddHours(-1))
        };
        return Task.FromResult(new CarrierTrackingInfo(trackingNumber, CarrierCode, "OUT_FOR_DELIVERY", events));
    }
}
