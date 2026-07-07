namespace Retailer.Application.Identity.Roles;

public class PermissionDto
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string Resource { get; set; } = default!;
    public bool IsBasic { get; set; }
}
