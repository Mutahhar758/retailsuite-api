using Retailer.Application.Legacy.CustomerDetails;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class CustomersController : VersionNeutralApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Customers)]
    [OpenApiOperation("Get customers.", "")]
    public async Task<HttpResponseDto<List<CustomerResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAsync(cancellationToken);
        return customers.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.Customers)]
    [OpenApiOperation("Create customer and chart of account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        var accountCode = await _customerService.CreateAsync(request, cancellationToken);
        return accountCode.ToInformationResponse("Customer created.");
    }

    [HttpPut("{account}")]
    [MustHavePermission(AppAction.Update, AppResource.Customers)]
    [OpenApiOperation("Update customer details.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        await _customerService.UpdateAsync(account, request, cancellationToken);
        return "Customer updated.".ToInformationResponse("Customer updated.");
    }

    [HttpPost("presigned-upload-url")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.Customers)]
    [OpenApiOperation("Generate pre-signed upload URL for customer image.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetPresignedUploadUrlAsync([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var response = await _customerService.GetPresignedUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }

    [HttpGet("supply-items")]
    [MustHavePermission(AppAction.View, AppResource.Customers)]
    [OpenApiOperation("Get customer supply items.", "")]
    public async Task<HttpResponseDto<List<CustomerSupplyItemDto>>> GetSupplyItemsAsync([FromQuery] string? customerId, [FromQuery] string? itemId, CancellationToken cancellationToken)
    {
        var items = await _customerService.GetSupplyItemsAsync(customerId, itemId, cancellationToken);
        return items.ToInformationResponse();
    }
}


