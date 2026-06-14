using Microsoft.AspNetCore.Http;

namespace Retailer.Application.Public.Document;
public class MultipleDocumentRequest
{
    public List<IFormFile> Documents { get; set; }
    public string Path { get; set; }
}
