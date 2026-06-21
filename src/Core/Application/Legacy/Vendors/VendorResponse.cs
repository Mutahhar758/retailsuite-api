namespace Retailer.Application.Legacy.Vendors;

public class VendorResponse
{
    public string Account { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Fax { get; set; }
    public string? Cnic { get; set; }
    public string? Address { get; set; }
    public string? Qualification { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? SmsNumber { get; set; }
    public string? Iban { get; set; }
    public bool SmsAlert { get; set; }
    public bool EmailAlert { get; set; }
    public bool Active { get; set; }
    public bool ShowInSales { get; set; }
    public string? MediaId { get; set; }
    public string? MediaUrl { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
