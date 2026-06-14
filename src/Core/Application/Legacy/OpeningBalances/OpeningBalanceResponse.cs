namespace Retailer.Application.Legacy.OpeningBalances;

public class OpeningBalanceResponse
{
    public string ParentCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Bal { get; set; }
}
