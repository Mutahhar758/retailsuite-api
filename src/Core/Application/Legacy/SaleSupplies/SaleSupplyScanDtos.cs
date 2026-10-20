namespace Retailer.Application.Legacy.SaleSupplies;

public class SaleSupplyScanResponse
{
    public List<ScannedSaleSupplyLineDto> Lines { get; set; } = [];
    public int RemainingScansThisMonth { get; set; }
    public int MonthlyLimit { get; set; }
    public int? DocumentId { get; set; }
    public string? AccessUrl { get; set; }
    public string? RawDetectedText { get; set; }
}

public class ScannedSaleSupplyLineDto
{
    public int Seq { get; set; }
    public string RawCustomerName { get; set; } = default!;
    public string? MatchedCustomerId { get; set; }
    public string? MatchedCustomerName { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Discount { get; set; }
    public decimal AddLess { get; set; }
    public decimal Carriage { get; set; }
    public decimal Amount { get; set; }
    public bool IsCustomerMatched { get; set; }
}

public class SaleSupplyAttachmentDto
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public string FileName { get; set; } = default!;
    public string? FileUrl { get; set; }
    public string? FileType { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
}
