using System.Reflection;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Retailer.Host.Configurations;

internal static class Startup
{
    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        const string configurationsDirectory = "Configurations";
        var env = builder.Environment;

        var jsonFiles = Directory.GetFiles(configurationsDirectory, "*.json");

        foreach (var jsonFile in jsonFiles.OrderBy(file => file.ToLower().EndsWith($"{env.EnvironmentName.ToLower()}.json")))
        {
            string fileName = Path.GetFileName(jsonFile);

            if (fileName.Split(".")[1].Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                builder.Configuration.AddJsonFile(jsonFile, optional: false, reloadOnChange: true);
            }
            else if (fileName.Split(".")[1].Equals(env.EnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                builder.Configuration.AddJsonFile(jsonFile, optional: true, reloadOnChange: true);
            }
        }

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables();

        return builder;
    }
}


