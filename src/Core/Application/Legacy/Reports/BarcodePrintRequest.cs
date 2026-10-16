namespace Retailer.Application.Legacy.Reports;

public class BarcodePrintItemDto
{
    public string? ItemId { get; set; }
    public string? Title { get; set; }
    public string? Barcode { get; set; }
    public decimal Rate { get; set; }
    public int Copies { get; set; } = 1;
    public decimal? Qty { get; set; }
    public string? Unit { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool IsVariableWeight { get; set; }
}

public class BarcodePrintRequest
{
    public List<BarcodePrintItemDto> Items { get; set; } = new();
    public string Layout { get; set; } = "ThermalRoll"; // "ThermalRoll" (50x30mm) or "SheetA4" (24 stickers per sheet)
    public bool IncludePrice { get; set; } = true;
    public bool IncludeTitle { get; set; } = true;
    public bool IncludeCompanyName { get; set; } = true;
    public string? CompanyName { get; set; }
}
