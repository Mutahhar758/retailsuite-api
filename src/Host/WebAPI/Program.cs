using Retailer.Application;
using Retailer.Application.Common.Enums;
using Retailer.Host.Configurations;
using Retailer.Host.Controllers;
using Retailer.Host.OnPremises;
using Retailer.Infrastructure.OnPremises;
using Microsoft.Extensions.Hosting.WindowsServices;
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

// Install-time provisioning (generates per-install secrets) - runs and exits, never starts the web host.
if (OnPremisesProvisioner.IsCliCommand(args))
{
    Environment.ExitCode = OnPremisesProvisioner.Run(args);
    return;
}

string? serviceBootLog = null;
if (WindowsServiceHelpers.IsWindowsService())
{
    var serviceDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "RetailSuite");
    Directory.CreateDirectory(serviceDataDir);
    Directory.SetCurrentDirectory(serviceDataDir);
    serviceBootLog = Path.Combine(serviceDataDir, "boot.log");
    File.WriteAllText(serviceBootLog, $"[{DateTime.Now:HH:mm:ss}] Service process starting...{Environment.NewLine}");
}

void BootTrace(string msg)
{
    if (serviceBootLog is not null)
    {
        File.AppendAllText(serviceBootLog, $"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
    }
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");
try
{
    // When running as a Windows Service the working directory is System32; anchor content root to the exe folder.
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : null,
    });

    builder.AddConfigurations();
    BootTrace("AddConfigurations completed");

    var onPremises = HostingMode.IsOnPremises(builder.Configuration);
    if (onPremises)
    {
        builder.Host.UseWindowsService(options => options.ServiceName = "RetailSuiteLocalServer");
    }

    // Configure Serilog to capture all logs (Console, File, Seq, AppInsights)
    builder.RegisterSerilog();
    BootTrace("RegisterSerilog completed");

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

    var mvcBuilder = builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.Converters.Add(new PakistanDateTimeJsonConverter());
            o.JsonSerializerOptions.Converters.Add(new NullablePakistanDateTimeJsonConverter());
        })
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
    if (onPremises)
    {
        mvcBuilder.ExcludeCloudControllers();
    }

    builder.Services.AddApplication();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddFluentValidationClientsideAdapters();

    builder.Services.AddInfrastructure(builder.Configuration);
    BootTrace("AddInfrastructure completed");

    var app = builder.Build();

    if (onPremises && WindowsServiceHelpers.IsWindowsService())
    {
        // Create DB quickly (<100ms), then let app.Run() notify Windows SCM immediately so it never hits the 30s timeout.
        await app.Services.EnsureOnPremisesDatabaseExistsAsync();
        BootTrace("EnsureOnPremisesDatabaseExistsAsync completed");
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await app.Services.InitializeDatabasesAsync();
                    ApplicationState.SetStarted();
                    Log.Information("On-premises database migration and seeding completed.");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "On-premises background database initialization failed.");
                }
            });
        });
    }
    else
    {
        await app.Services.InitializeDatabasesAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseInfrastructure(builder.Configuration);
    BootTrace("UseInfrastructure completed");
    app.MapEndpoints();

    if (!(onPremises && WindowsServiceHelpers.IsWindowsService()))
    {
        // Set application as started
        ApplicationState.SetStarted();
    }

    BootTrace("Calling app.Run()...");
    Log.Information("Application started and ready to serve requests");
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled exception");
    BootTrace($"FATAL: {ex}");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}