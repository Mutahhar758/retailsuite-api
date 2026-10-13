namespace Retailer.Application.Legacy.Reports;

public class CustomerBillLineResponse
{
    public DateOnly Date { get; set; }
    public string VNo { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string UnitId { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal AddLess { get; set; }
    public decimal Carriage { get; set; }
    public decimal Amount { get; set; }
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? PtaStatus { get; set; }
    public int? WarrantyMonths { get; set; }
    public string? ConditionNote { get; set; }
}
