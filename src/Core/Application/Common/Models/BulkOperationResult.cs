namespace Retailer.Application.Common.Models;

public class BulkOperationResult
{
    public int TotalProcessed { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<BulkItemError> Errors { get; set; } = new();
}

public class BulkItemError
{
    public int RowIndex { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
