namespace Retailer.Application.OpenApi;

public interface IFrontEndTypesGeneratorService : ISingletonService
{
    Task<byte[]> GetTypeScriptTypesAsync(string baseUrl);
}