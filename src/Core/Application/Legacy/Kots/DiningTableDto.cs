namespace Retailer.Application.Legacy.Kots;

public class DiningTableDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public string Status { get; set; } = default!;
    public bool Active { get; set; }
}

public class DiningTableCreateRequest
{
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public bool Active { get; set; } = true;
}

public class DiningTableUpdateRequest
{
    public string Name { get; set; } = default!;
    public int Capacity { get; set; }
    public string Status { get; set; } = default!;
    public bool Active { get; set; }
}
