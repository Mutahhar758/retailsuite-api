namespace Retailer.Application.Legacy.Reports;

public class PurchaseBillLineResponse
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal QtyInPack { get; set; }
    public string UnitId { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal TAmount { get; set; }
}
