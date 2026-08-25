using Retailer.Application;
using Retailer.Application.Common.Enums;
using Retailer.Host.Configurations;
using Retailer.Host.Controllers;
using Retailer.Infrastructure;
using Retailer.Infrastructure.Common;
using Retailer.Infrastructure.Common.Convertor;
using Retailer.Infrastructure.Logging.Serilog;
using Retailer.Infrastructure.State;
using FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text.Json.Serialization;

[assembly: ApiConventionType(typeof(ApiConventions))]

StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddConfigurations();

    // Configure Serilog to capture all logs (Console, File, Seq, AppInsights)
    builder.RegisterSerilog();

    // Configure Application Insights if connection string exists
    string? appInsightsConnectionString = builder.Configuration.GetSection("Serilog:WriteTo:0:Args:connectionString").Value;
    if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.EnableAdaptiveSampling = false;
            options.EnableQuickPulseMetricStream = true;
            options.ConnectionString = appInsightsConnectionString;
            options.EnableDebugLogger = false;
        });
    }

    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = new Dictionary<string, string>();
                foreach (var pair in context.ModelState)
                {
                    if (pair.Value.Errors.Count > 0)
                    {
                        errors.Add(pair.Key, string.Join(Environment.NewLine, pair.Value.Errors.Select(error => error.ErrorMessage).ToList()));
                    }
                }

                return new BadRequestObjectResult(new HttpResponseDto<object>
                {
                    Metadata =
                    new HttpResponseMetadata
                    {
                        StatusCode = StatusCodes.Status400BadRequest,
                        Type = HttpResponseType.Error.ToString(),
                        Message = "Invalid Request",
                        ValidationErrors = errors.Select(x => new Dictionary<string, string> { { x.Key, x.Value } }).ToArray()
                    }
                });
            };
        });
    builder.Services.AddApplication();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    await app.Services.InitializeDatabasesAsync();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseInfrastructure(builder.Configuration);
    app.MapEndpoints();

    // Set application as started
    ApplicationState.SetStarted();
    
    Log.Information("Application started and ready to serve requests");
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}