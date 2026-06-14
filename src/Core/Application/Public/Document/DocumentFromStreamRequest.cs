namespace Retailer.Application.Public.Document;
public class DocumentFromStreamRequest
{
    public byte[] Document { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public string Extension { get; set; } = default!;
    public string? Path { get; set; }
}
