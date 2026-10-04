namespace Retailer.Application.Legacy.Brands;

public class BrandResponse
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public bool Active { get; set; } = true;
}

public class BrandCreateRequest
{
    public string? Id { get; set; }
    public string Title { get; set; } = default!;
    public bool Active { get; set; } = true;
}

public class BrandUpdateRequest
{
    public string Title { get; set; } = default!;
    public bool Active { get; set; } = true;
}
