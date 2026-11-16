using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Retailer.Host.OnPremises;

internal static class OnPremisesConfiguration
{
    /// <summary>
    /// Layers the DPAPI-encrypted store configuration (DB connection string, store identity, unique JWT key)
    /// on top of every other source so it always wins.
    /// </summary>
    internal static WebApplicationBuilder AddOnPremisesSecrets(this WebApplicationBuilder builder)
    {
        var path = builder.Configuration["OnPremises:SecretFile"] ?? OnPremisesSecretStore.DefaultPath;
        var json = OnPremisesSecretStore.Read(path)
            ?? throw new InvalidOperationException(
                $"On-premises configuration '{path}' not found. Run the installer or '--onprem-provision' first.");

        builder.Configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        return builder;
    }

    /// <summary>Cloud-only tenant/licence management controllers must never be exposed on a store server.</summary>
    internal static IMvcBuilder ExcludeCloudControllers(this IMvcBuilder mvc)
    {
        mvc.PartManager.FeatureProviders.Add(new CloudControllerRemover());
        return mvc;
    }

    private sealed class CloudControllerRemover : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers
                .Where(c => string.Equals(c.Name, "TenantsController", StringComparison.Ordinal))
                .ToList())
            {
                feature.Controllers.Remove(controller);
            }
        }
    }
}
