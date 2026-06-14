namespace Retailer.Application.Public.Document;
public class DocumentResponse
{
    public int Id { get; set; }

    public string FileType { get; set; } = default!;

    public string ConvertedFileName { get; set; } = default!;

    public string OriginalFileName { get; set; } = default!;

    public string? Path { get; set; }

    public string? AccessURL { get; set; }
}

public class MediaStream
{
    public Stream InputStream { get; set; } = default!;
    public string OriginalFileName { get; set; } = default!;
}
