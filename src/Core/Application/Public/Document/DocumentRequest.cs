using Microsoft.AspNetCore.Http;

namespace Retailer.Application.Public.Document;
public class DocumentRequest
{
    public IFormFile DocumentFile { get; set; } = default!;
    public string? Path { get; set; }
}
