using Retailer.Application.Legacy.Vendors;
using Retailer.Infrastructure.Common.Extensions;

namespace Retailer.Host.Controllers.Legacy;

public class VendorsController : VersionNeutralApiController
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    [OpenApiOperation("Get vendors.", "")]
    public async Task<HttpResponseDto<List<VendorResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var vendors = await _vendorService.GetAsync(cancellationToken);
        return vendors.ToInformationResponse();
    }

    [HttpPost]
    [OpenApiOperation("Create vendor and chart of account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(VendorCreateRequest request, CancellationToken cancellationToken)
    {
        var accountCode = await _vendorService.CreateAsync(request, cancellationToken);
        return accountCode.ToInformationResponse("Vendor created.");
    }

    [HttpPut("{account}")]
    [OpenApiOperation("Update vendor details.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        await _vendorService.UpdateAsync(account, request, cancellationToken);
        return "Vendor updated.".ToInformationResponse("Vendor updated.");
    }
}
