using Retailer.Application.OpenApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Retailer.Host.Controllers;
public class FrontEndCodeGenerationController : VersionNeutralApiController
{
    private readonly IFrontEndTypesGeneratorService _frontEndTypesGeneratorService;
    public FrontEndCodeGenerationController(IFrontEndTypesGeneratorService frontEndTypesGeneratorService)
    {
        this._frontEndTypesGeneratorService = frontEndTypesGeneratorService;
    }

    [HttpGet("GenerateDtoTypes")]
    [AllowAnonymous]

    public async Task<IActionResult> GetTypeScriptTypesAsync()
    {
        string baseUrl = $"{this.Request.Scheme}://{this.Request.Host.Value.ToString()}{this.Request.PathBase.Value.ToString()}";
        var stream = await _frontEndTypesGeneratorService.GetTypeScriptTypesAsync(baseUrl);
        return File(stream, "application/x-typescript");
    }
}
