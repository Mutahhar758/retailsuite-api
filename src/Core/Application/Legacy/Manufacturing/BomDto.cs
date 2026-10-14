namespace Retailer.Application.Legacy.Manufacturing;

public class BomListFilter
{
    public string? Keyword { get; set; }
    public string? FinishedItemId { get; set; }
    public bool? IsActive { get; set; }
}

public class BomLineRequest
{
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public decimal Qty { get; set; }
    public string? UnitId { get; set; }
    public decimal ScrapPercentage { get; set; }
    public bool IsOptional { get; set; }
}

public class BomCreateRequest
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string FinishedItemId { get; set; } = default!;
    public decimal OutputQty { get; set; } = 1.0m;
    public string? UnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = true;
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Notes { get; set; }
    public List<BomLineRequest> Lines { get; set; } = [];
}

public class BomUpdateRequest
{
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string FinishedItemId { get; set; } = default!;
    public decimal OutputQty { get; set; } = 1.0m;
    public string? UnitId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = true;
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Notes { get; set; }
    public List<BomLineRequest> Lines { get; set; } = [];
}

public class BomLineResponse
{
    public int Id { get; set; }
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public string RawItemTitle { get; set; } = default!;
    public string? RawItemBarcode { get; set; }
    public decimal Qty { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public decimal ScrapPercentage { get; set; }
    public bool IsOptional { get; set; }
    public decimal CurrentRate { get; set; }
    public decimal TotalEstimatedCost { get; set; }
}

public class BomResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string FinishedItemId { get; set; } = default!;
    public string FinishedItemTitle { get; set; } = default!;
    public string? FinishedItemBarcode { get; set; }
    public decimal OutputQty { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal TotalEstimatedMaterialCost { get; set; }
    public decimal TotalEstimatedCost { get; set; }
    public decimal UnitEstimatedCost { get; set; }
    public string? Notes { get; set; }
    public List<BomLineResponse> Lines { get; set; } = [];
}
