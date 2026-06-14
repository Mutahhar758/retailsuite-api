using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Retailer.Infrastructure.OpenApi;

/// <summary>
/// Adds the X-Tenant-ID header parameter to API operations that don't have authentication requirements.
/// Authorized endpoints already have tenant ID available in JWT claims, so X-Tenant-ID is not needed for them.
/// </summary>
public class TenantIdHeaderOperationProcessor : IOperationProcessor
{
    private const string HeaderName = "X-Tenant-ID";

    public bool Process(OperationProcessorContext context)
    {
        // Skip adding X-Tenant-ID if the endpoint already requires authentication (has security requirements)
        if (context.OperationDescription.Operation.Security?.Any() == true)
        {
            return true;
        }

        var parameters = context.OperationDescription.Operation.Parameters;

        // Avoid duplicates
        if (parameters.Any(p => p.Kind == OpenApiParameterKind.Header && p.Name == HeaderName))
        {
            return true;
        }

        parameters.Add(new OpenApiParameter
        {
            Name = HeaderName,
            Kind = OpenApiParameterKind.Header,
            Description = "The tenant identifier. Required for all tenant-scoped endpoints.",
            IsRequired = false,
            Schema = new NJsonSchema.JsonSchema
            {
                Type = NJsonSchema.JsonObjectType.String
            }
        });

        return true;
    }
}
