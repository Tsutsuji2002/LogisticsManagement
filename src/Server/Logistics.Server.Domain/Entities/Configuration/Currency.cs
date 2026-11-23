namespace Logistics.Server.Domain.Entities.Configuration;

public class Currency
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty; // VND, USD, etc.
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty; // ₫, $, etc.
    public int DecimalPlaces { get; set; }
    public decimal ExchangeRateToDefault { get; set; } = 1.0m;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
