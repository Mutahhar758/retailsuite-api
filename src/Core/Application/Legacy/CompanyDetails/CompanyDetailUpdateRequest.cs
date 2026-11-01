namespace Retailer.Application.Legacy.CompanyDetails;

/// <summary>
/// Request payload for updating tenant company information.
/// Note: CompanyName is intentionally excluded as it is centrally managed by BOP.
/// </summary>
public class CompanyDetailUpdateRequest
{
    public string? UrCompanyName { get; set; }
    public string? Descr { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Cell { get; set; }
    public string? Cell2 { get; set; }
    public string? ContactHeader { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Ntn { get; set; }
    public string? Strn { get; set; }
    public string? LogoMediaId { get; set; }
}
