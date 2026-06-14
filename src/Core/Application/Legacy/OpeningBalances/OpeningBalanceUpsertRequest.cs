namespace Retailer.Application.Legacy.OpeningBalances;

public class OpeningBalanceUpsertRequest
{
    public string Account { get; set; } = string.Empty;
    public decimal? DrAmount { get; set; }
    public decimal? CrAmount { get; set; }
}
