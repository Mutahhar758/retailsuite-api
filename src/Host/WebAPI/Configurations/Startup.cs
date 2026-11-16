using System.Reflection;
using Retailer.Host.OnPremises;
using Retailer.Infrastructure.OnPremises;

namespace Retailer.Host.Configurations;

internal static class Startup
{
    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        const string configurationsDirectory = "Configurations";
        var env = builder.Environment;
        var onPremises = HostingMode.IsOnPremises(builder.Configuration);
        var configFolder = Path.Combine(env.ContentRootPath, configurationsDirectory);

        if (Directory.Exists(configFolder))
        {
            var jsonFiles = Directory.GetFiles(configFolder, "*.json");

            foreach (var jsonFile in jsonFiles.OrderBy(file => file.ToLower().EndsWith($"{env.EnvironmentName.ToLower()}.json")))
            {
                string fileName = Path.GetFileName(jsonFile);

                // On-premises never loads cloud master-DB, shared database, or cloud media service settings.
                if (onPremises
                    && (fileName.StartsWith("multitenancy", StringComparison.OrdinalIgnoreCase)
                        || fileName.StartsWith("database", StringComparison.OrdinalIgnoreCase)
                        || fileName.StartsWith("mediaservice", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (fileName.Split(".")[1].Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    builder.Configuration.AddJsonFile(jsonFile, optional: false, reloadOnChange: true);
                }
                else if (fileName.Split(".")[1].Equals(env.EnvironmentName, StringComparison.OrdinalIgnoreCase))
                {
                    builder.Configuration.AddJsonFile(jsonFile, optional: true, reloadOnChange: true);
                }
            }
        }
        else
        {
            // Single-file On-Premises service: load non-secret defaults from embedded resources inside the exe.
            var asm = Assembly.GetExecutingAssembly();
            foreach (var resName in asm.GetManifestResourceNames()
                .Where(n => n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n))
            {
                using var stream = asm.GetManifestResourceStream(resName);
                if (stream is not null)
                {
                    var copy = new MemoryStream();
                    stream.CopyTo(copy);
                    copy.Position = 0;
                    builder.Configuration.AddJsonStream(copy);
                }
            }
        }

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: onPremises, reloadOnChange: !onPremises)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: !onPremises)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables();

        if (onPremises)
        {
            builder.AddOnPremisesSecrets();
        }

        return builder;
    }
}
