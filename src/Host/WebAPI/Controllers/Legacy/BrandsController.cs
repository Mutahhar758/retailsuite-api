using Retailer.Application.Legacy.Brands;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class BrandsController : VersionNeutralApiController
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Brands)]
    [OpenApiOperation("Get all brands.", "")]
    public async Task<HttpResponseDto<List<BrandResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _brandService.GetAllAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("active")]
    [MustHavePermission(AppAction.View, AppResource.Brands)]
    [OpenApiOperation("Get active brands.", "")]
    public async Task<HttpResponseDto<List<BrandResponse>>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var result = await _brandService.GetActiveAsync(cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpGet("{id}")]
    [MustHavePermission(AppAction.View, AppResource.Brands)]
    [OpenApiOperation("Get brand by id.", "")]
    public async Task<HttpResponseDto<BrandResponse?>> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var result = await _brandService.GetByIdAsync(id, cancellationToken);
        return result.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.Brands)]
    [OpenApiOperation("Create a new brand.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(BrandCreateRequest request, CancellationToken cancellationToken)
    {
        var id = await _brandService.CreateAsync(request, cancellationToken);
        return id.ToInformationResponse("Brand created successfully.");
    }

    [HttpPut("{id}")]
    [MustHavePermission(AppAction.Update, AppResource.Brands)]
    [OpenApiOperation("Update an existing brand.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string id, BrandUpdateRequest request, CancellationToken cancellationToken)
    {
        await _brandService.UpdateAsync(id, request, cancellationToken);
        return "Brand updated successfully.".ToInformationResponse("Brand updated successfully.");
    }

    [HttpDelete("{id}")]
    [MustHavePermission(AppAction.Delete, AppResource.Brands)]
    [OpenApiOperation("Delete a brand.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _brandService.DeleteAsync(id, cancellationToken);
        return "Brand deleted successfully.".ToInformationResponse("Brand deleted successfully.");
    }
}
