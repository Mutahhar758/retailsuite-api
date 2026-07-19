namespace Retailer.Application.Legacy.Kots;

public class PrepStationDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Active { get; set; }
}

public class PrepStationCreateRequest
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Active { get; set; } = true;
}

public class PrepStationUpdateRequest
{
    public string Name { get; set; } = default!;
    public bool Active { get; set; }
}
