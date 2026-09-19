using Retailer.Application.Legacy.Reports;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class MilkComparisonHeader
{
    public string CompanyName { get; set; } = "Company";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string ItemTitle { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public PurchaseSupplyComparisonSummaryResponse Summary { get; set; } = new();
}
