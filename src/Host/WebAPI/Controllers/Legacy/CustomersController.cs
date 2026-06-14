using Retailer.Application.Legacy.CustomerDetails;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class CustomersController : VersionNeutralApiController
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [OpenApiOperation("Get customers.", "")]
    public async Task<HttpResponseDto<List<CustomerResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAsync(cancellationToken);
        return customers.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create customer and chart of account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(CustomerCreateRequest request, CancellationToken cancellationToken)
    {
        var accountCode = await _customerService.CreateAsync(request, cancellationToken);
        return accountCode.ToInformationResponse("Customer created.");
    }

    [HttpPut("{account}")]
    [OpenApiOperation("Update customer details.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, CustomerUpdateRequest request, CancellationToken cancellationToken)
    {
        await _customerService.UpdateAsync(account, request, cancellationToken);
        return "Customer updated.".ToInformationResponse("Customer updated.");
    }
}
