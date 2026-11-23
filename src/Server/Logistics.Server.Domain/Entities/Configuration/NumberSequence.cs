namespace Logistics.Server.Domain.Entities.Configuration;

public class NumberSequence
{
    public long Id { get; set; }
    public string Scope { get; set; } = string.Empty; // ORDER, WAYBILL, INVOICE, RECEIPT, DISPATCH
    public string Prefix { get; set; } = string.Empty;
    public string? Suffix { get; set; }
    public long NextNumber { get; set; } = 1;
    public int Increment { get; set; } = 1;
    public int NumberLength { get; set; } = 8;
    public string CurrentFormat { get; set; } = "{PREFIX}-{YYYYMM}-{NUMBER}";
    public string ResetFrequency { get; set; } = "None"; // None, Yearly, Monthly, Daily
    public DateOnly? LastResetDate { get; set; }
}
