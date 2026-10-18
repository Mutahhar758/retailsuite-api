using Retailer.Application.Legacy.CustomerCategories;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class CustomerCategoriesController : VersionNeutralApiController
{
    private readonly ICustomerCategoryService _customerCategoryService;

    public CustomerCategoriesController(ICustomerCategoryService customerCategoryService)
    {
        _customerCategoryService = customerCategoryService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.CustomerCategories)]
    [OpenApiOperation("Get customer categories.", "")]
    public async Task<HttpResponseDto<List<CustomerCategoryResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var categories = await _customerCategoryService.GetActiveAsync(cancellationToken);
        return categories.ToInformationResponse();
    }

    [HttpGet("lookup")]
    [OpenApiOperation("Get active customer categories lookup.", "")]
    public async Task<HttpResponseDto<List<CustomerCategoryLookupResponse>>> GetLookupAsync(CancellationToken cancellationToken)
    {
        var categories = await _customerCategoryService.GetLookupAsync(cancellationToken);
        return categories.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.CustomerCategories)]
    [OpenApiOperation("Create a customer category.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(CustomerCategoryCreateRequest request, CancellationToken cancellationToken)
    {
        var code = await _customerCategoryService.CreateAsync(request, cancellationToken);
        return code.ToInformationResponse("Customer category created.");
    }

    [HttpPut("{code}")]
    [MustHavePermission(AppAction.Update, AppResource.CustomerCategories)]
    [OpenApiOperation("Update a customer category.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string code, CustomerCategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        await _customerCategoryService.UpdateAsync(code, request, cancellationToken);
        return "Customer category updated.".ToInformationResponse("Customer category updated.");
    }

    [HttpDelete("{code}")]
    [MustHavePermission(AppAction.Delete, AppResource.CustomerCategories)]
    [OpenApiOperation("Delete a customer category.", "")]
    public async Task<HttpResponseDto<string>> DeleteAsync(string code, CancellationToken cancellationToken)
    {
        await _customerCategoryService.DeleteAsync(code, cancellationToken);
        return "Customer category deleted.".ToInformationResponse("Customer category deleted.");
    }
}
