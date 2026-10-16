namespace Retailer.Application.Legacy.Vendors;

public class VendorBulkUpsertRequest
{
    public List<VendorBulkItemDto> Vendors { get; set; } = new();
}

public class VendorBulkItemDto
{
    public string? Account { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public string? Cnic { get; set; }
    public string? Address { get; set; }
    public string? Qualification { get; set; }
    public string? Fax { get; set; }
    public string? SmsNumber { get; set; }
    public string? Iban { get; set; }
    public bool SmsAlert { get; set; }
    public bool EmailAlert { get; set; }
    public bool Active { get; set; } = true;
    public bool ShowInSales { get; set; }
}
