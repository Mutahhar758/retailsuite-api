using Retailer.Application.Legacy.Reports;

namespace Retailer.Infrastructure.Reporting.QuestPdf.Models;

public class CustomerBalanceRecoveryHeader
{
    public string CompanyName { get; set; } = "Company";
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string DateBasis { get; set; } = "Clearing Date";
    public string BalanceFilter { get; set; } = "All";
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public CustomerBalanceRecoverySummaryResponse Summary { get; set; } = new();
}
