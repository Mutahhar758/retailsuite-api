namespace Retailer.Application.Common.Models;

public class LookupResponse
{
    public LookupResponse()
    {
    }

    public LookupResponse(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
