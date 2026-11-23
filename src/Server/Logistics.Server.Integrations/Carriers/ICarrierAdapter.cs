namespace Logistics.Server.Integrations.Carriers;

/// <summary>
/// Abstraction for external carrier API integrations (e.g. GHN, GHTK, Viettel Post).
/// </summary>
public interface ICarrierAdapter
{
    string CarrierCode { get; }
    string CarrierName { get; }
    bool IsAvailable { get; }

    Task<CarrierRateResponse> CalculateRatesAsync(CarrierRateRequest request, CancellationToken cancellationToken = default);
    Task<CarrierCreateShipmentResponse> CreateShipmentAsync(CarrierCreateShipmentRequest request, CancellationToken cancellationToken = default);
    Task<CarrierTrackingInfo> GetTrackingAsync(string trackingNumber, CancellationToken cancellationToken = default);
}
