using Elasticsearch.Net.Specification.IndicesApi;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript;
using NSwag.CodeGeneration;
using NSwag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Retailer.Application.OpenApi;
using Retailer.Application.Common.Exceptions;

namespace Retailer.Infrastructure.OpenApi;
public class FrontEndTypesGeneratorService : IFrontEndTypesGeneratorService
{
    public async Task<byte[]> GetTypeScriptTypesAsync(string baseUrl)
    {
        OpenApiDocument document = await OpenApiDocument.FromUrlAsync($"{baseUrl}/swagger/v1/swagger.json");

        var settings = new TypeScriptClientGeneratorSettings
        {
            GenerateClientClasses = false,
            GenerateDtoTypes = true,
            GenerateClientInterfaces = true,
            TypeScriptGeneratorSettings =
            {
                TypeStyle = TypeScriptTypeStyle.Interface
            },
        };

        TypeScriptClientGenerator generator = new TypeScriptClientGenerator(document, settings);
        string generatedCode = generator.GenerateFile(ClientGeneratorOutputType.Contracts);

        return SortRequestsAndResponses(generatedCode);
    }

    private byte[] SortRequestsAndResponses(string content)
    {
        Regex interfaceRegex = new Regex(@"export\s+(interface|type)\s+\w+(\s+extends\s+\w+)?\s+\{[^{}]*(\{[^{}]*\}[^{}]*)*\}");
        MatchCollection interfaceMatches = interfaceRegex.Matches(content);

        Regex enumRegex = new Regex(@"export\s+enum\s+\w+\s+{[^}]*}");
        MatchCollection enumMatches = enumRegex.Matches(content);

        if (interfaceMatches.Count == 0 && enumMatches.Count == 0)
        {
            throw new BadRequestException("No interfaces or enums found in the file");
        }

        List<string> enums = new List<string>();
        List<string> requestInterfaces = new List<string>();
        List<string> dtoInterfaces = new List<string>();

        foreach (Match match in enumMatches)
        {
            enums.Add(match.Value);
        }

        foreach (Match match in interfaceMatches)
        {
            string iface = match.Value;
            string interfaceName = Regex.Match(iface, @"interface\s+(\w+)").Groups[1].Value;
            if (!string.IsNullOrEmpty(interfaceName))
            {
                if (interfaceName.EndsWith("Request"))
                {
                    requestInterfaces.Add(iface);
                }
                else if (interfaceName.EndsWith("Dto") || interfaceName.EndsWith("Response") || interfaceName.EndsWith("Filter") ||
                    interfaceName.EndsWith("PaginationMetadata") || interfaceName.EndsWith("Search") || interfaceName.EndsWith("ErrorResult") || interfaceName.EndsWith("ProblemDetails"))
                {
                    dtoInterfaces.Add(iface);
                }
            }
        }

        string updatedContent = string.Join("\n\n", enums) + "\n\n //------------------------- Requests -------------------\n\n" +
                                string.Join("\n\n", requestInterfaces) + "\n\n // ------------------ Responses ---------------------- \n\n" +
                                string.Join("\n\n", dtoInterfaces);

        return Encoding.UTF8.GetBytes(updatedContent);
    }
}