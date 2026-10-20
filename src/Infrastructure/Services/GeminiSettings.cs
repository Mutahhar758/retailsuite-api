namespace Retailer.Infrastructure.Services;

public class GeminiSettings
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-1.5-flash";
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";
    public int DefaultMonthlyScanLimit { get; set; } = 50;
}
