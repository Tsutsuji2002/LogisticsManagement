# Carrier Provider Specification

## 1. Carrier Adapter Interface (`ICarrierAdapter`)

External carriers are integrated via the `ICarrierAdapter` interface defined in `Logistics.Server.Integrations.Carriers`:

```csharp
public interface ICarrierAdapter
{
    string CarrierCode { get; }
    string CarrierName { get; }
    bool IsAvailable { get; }

    Task<CarrierRateResponse> CalculateRatesAsync(
        CarrierRateRequest request, 
        CancellationToken cancellationToken = default);

    Task<CarrierCreateShipmentResponse> CreateShipmentAsync(
        CarrierCreateShipmentRequest request, 
        CancellationToken cancellationToken = default);

    Task<CarrierTrackingInfo> GetTrackingAsync(
        string trackingNumber, 
        CancellationToken cancellationToken = default);
}
```

## 2. Canonical Request & Response Models

### 2.1 Rate Calculation
- **`CarrierRateRequest`**:
  - `OriginPostalCode`, `DestinationPostalCode`
  - `OriginProvinceId`, `DestinationProvinceId`
  - `WeightKg`, `LengthCm`, `WidthCm`, `HeightCm`
  - `DeclaredValue`, `CodAmount`
- **`CarrierRateResponse`**:
  - `Success`: Boolean flag
  - `Options`: List of `CarrierRateOption` (`ServiceCode`, `ServiceName`, `TotalFee`, `ShippingFee`, `InsuranceFee`, `EstimatedDays`)
  - `ErrorMessage`: Optional failure description

### 2.2 Shipment Booking
- **`CarrierCreateShipmentRequest`**:
  - `ReferenceCode`: Internal shipment tracking number
  - `ServiceCode`: Carrier service code (e.g. `STANDARD`, `FAST`, `EXPRESS`)
  - `SenderName`, `SenderPhone`, `SenderAddress`
  - `RecipientName`, `RecipientPhone`, `RecipientAddress`
  - `WeightKg`, `CodAmount`, `DeclaredValue`, `Note`
- **`CarrierCreateShipmentResponse`**:
  - `Success`: Boolean flag
  - `CarrierTrackingNumber`: Waybill number allocated by carrier
  - `CarrierOrderCode`: Internal identifier in carrier system
  - `TotalFee`: Charged tariff
  - `LabelUrl`: Waybill/shipping label PDF download link
  - `ErrorMessage`: Optional error details

### 2.3 Tracking Information
- **`CarrierTrackingInfo`**:
  - `TrackingNumber`: Carrier tracking identifier
  - `CarrierCode`: Carrier identifier
  - `CurrentStatus`: Normalized or raw carrier status code
  - `Events`: Chronological list of `CarrierTrackingEventInfo` (`StatusCode`, `StatusDescription`, `Location`, `Timestamp`)

## 3. Registered Carrier Adapters

| Carrier Code | Carrier Name | Adapter Implementation | Base URL Default |
|:---|:---|:---|:---|
| `GHN` | Giao Hang Nhanh | `GhnCarrierAdapter` | `https://api.ghn.vn/v2` |
| `GHTK` | Giao Hang Tiet Kiem | `GhtkCarrierAdapter` | `https://api.ghtk.vn` |
| `VTPOST` | Viettel Post | `ViettelPostCarrierAdapter` | `https://api.viettelpost.vn` |
| `VNPOST` | Vietnam Post | `CarrierAdapterBase` extension | `https://api.vnpost.vn` |
