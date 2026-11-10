namespace Retailer.Application.Legacy.Reports;

public class SaleBillFilter
{
    public string VoucherNo { get; set; } = string.Empty;
    public string? Layout { get; set; } // "Thermal80mm", "A5Sheet", "A4Sheet"
    public bool? IsDuplicate { get; set; }
    public bool? QrEnabled { get; set; }
}
