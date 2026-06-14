using Retailer.Application.Common.Enums;

namespace Retailer.Application.Common.Models;
public class HttpResponseMetadata
{
    public string Type { get; set; } = HttpResponseType.Information.ToString();
    public int StatusCode { get; set; }
    public string? Message { get; set; }
    public object? ValidationErrors { get; set; }
    public string? ErrorId { get; set; }
}

public class HttpResponseDto<T>
{
    public T? Body { get; set; }
    public HttpResponseMetadata Metadata { get; set; } = default!;
}
