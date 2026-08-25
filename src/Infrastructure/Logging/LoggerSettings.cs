namespace Retailer.Infrastructure.Logging;

public class LoggerSettings
{
    public string AppName { get; set; } = "Demo.WebAPI";
    public string ElasticSearchUrl { get; set; } = string.Empty;
    public string SeqServerUrl { get; set; } = "http://localhost:5341";
    public string? SeqApiKey { get; set; }
    public bool WriteToFile { get; set; } = false;
    public bool StructuredConsoleLogging { get; set; } = false;
    public string MinimumLogLevel { get; set; } = "Information";
}
