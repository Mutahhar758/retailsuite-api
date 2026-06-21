namespace Retailer.Application.Legacy.CustomerDetails;

public class CustomerUpdateRequest
{
    public string? Title { get; set; }
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
    public string? MediaId { get; set; }
}
