namespace Retailer.Application.Legacy.Sales;

public class CustomerItemSaleRecord
{
    public string VoucherNo { get; set; } = default!;
    public string? VType { get; set; }
    public DateOnly Date { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal? SecQty { get; set; }
    public decimal? SecRate { get; set; }
    public decimal Amount { get; set; }
}

public class CustomerItemPriceHistoryResponse
{
    public bool HasHistory { get; set; }
    public DateOnly? LastSaleDate { get; set; }
    public decimal? LastRate { get; set; }
    public decimal? LastQty { get; set; }
    public decimal? LastSecRate { get; set; }
    public string? LastVoucherNo { get; set; }
    public string? LastVType { get; set; }
    public List<CustomerItemSaleRecord> RecentSales { get; set; } = new();
}
