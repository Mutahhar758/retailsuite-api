using Retailer.Domain.Common.Enums;

namespace Retailer.Application.Legacy.Manufacturing;

public class ProductionListFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? FinishedItemId { get; set; }
    public string? VoucherNo { get; set; }
    public ProductionStatus? Status { get; set; }
    public string? VType { get; set; }
}

public class ProductionLineRequest
{
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public string? UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsIssued { get; set; } = true;
}

public class ProductionCreateRequest
{
    public DateOnly Date { get; set; }
    public string VType { get; set; } = "MF";
    public int? BomMasterId { get; set; }
    public string FinishedItemId { get; set; } = default!;
    public decimal TargetQty { get; set; }
    public decimal ProducedQty { get; set; }
    public string? UnitId { get; set; }
    public string? BatchNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Narration { get; set; }
    public string? Remarks { get; set; }
    public ProductionStatus Status { get; set; } = ProductionStatus.Completed;
    public string? CurrentStage { get; set; }
    public List<ProductionLineRequest> Lines { get; set; } = [];
}

public class ProductionUpdateRequest
{
    public DateOnly Date { get; set; }
    public int? BomMasterId { get; set; }
    public string FinishedItemId { get; set; } = default!;
    public decimal TargetQty { get; set; }
    public decimal ProducedQty { get; set; }
    public string? UnitId { get; set; }
    public string? BatchNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal PackagingCost { get; set; }
    public string? Narration { get; set; }
    public string? Remarks { get; set; }
    public ProductionStatus Status { get; set; } = ProductionStatus.Completed;
    public string? CurrentStage { get; set; }
    public List<ProductionLineRequest> Lines { get; set; } = [];
}

public class ProductionLineResponse
{
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public string RawItemTitle { get; set; } = default!;
    public string? RawItemBarcode { get; set; }
    public decimal PlannedQty { get; set; }
    public decimal ActualQty { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AvailableStock { get; set; }
    public bool IsIssued { get; set; }
}

public class ProductionResponse
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string VType { get; set; } = "MF";
    public string VoucherNo { get; set; } = default!;
    public int? BomMasterId { get; set; }
    public string? BomTitle { get; set; }
    public string FinishedItemId { get; set; } = default!;
    public string FinishedItemTitle { get; set; } = default!;
    public string? FinishedItemBarcode { get; set; }
    public decimal TargetQty { get; set; }
    public decimal ProducedQty { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public string? BatchNo { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal TotalMaterialCost { get; set; }
    public decimal TotalOverheadCost { get; set; }
    public decimal TotalProductionCost { get; set; }
    public decimal UnitProductionCost { get; set; }
    public string? NarrationId { get; set; }
    public string? Narration { get; set; }
    public string? Remarks { get; set; }
    public ProductionStatus Status { get; set; }
    public string? CurrentStage { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
    public List<ProductionLineResponse> Lines { get; set; } = [];
}

public class ProductionEstimateRequest
{
    public int? BomMasterId { get; set; }
    public string FinishedItemId { get; set; } = default!;
    public decimal TargetQty { get; set; }
}

public class ProductionEstimateLine
{
    public int Seq { get; set; }
    public string RawItemId { get; set; } = default!;
    public string RawItemTitle { get; set; } = default!;
    public string? RawItemBarcode { get; set; }
    public decimal RequiredQty { get; set; }
    public string? UnitId { get; set; }
    public string? UnitTitle { get; set; }
    public decimal ScrapPercentage { get; set; }
    public decimal AvailableStock { get; set; }
    public bool IsShortage { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
}

public class ProductionEstimateResponse
{
    public decimal TargetQty { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal EstimatedLaborCost { get; set; }
    public decimal EstimatedOverheadCost { get; set; }
    public decimal EstimatedPackagingCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public List<ProductionEstimateLine> Lines { get; set; } = [];
}
