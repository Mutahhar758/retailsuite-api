using Retailer.Application.Legacy.Vendors;
using Retailer.Application.Common.Interfaces;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class VendorsController : VersionNeutralApiController
{
    private readonly IVendorService _vendorService;

    public VendorsController(IVendorService vendorService)
    {
        _vendorService = vendorService;
    }

    [HttpGet]
    [MustHavePermission(AppAction.View, AppResource.Vendors)]
    [OpenApiOperation("Get vendors.", "")]
    public async Task<HttpResponseDto<List<VendorResponse>>> GetAsync(CancellationToken cancellationToken)
    {
        var vendors = await _vendorService.GetAsync(cancellationToken);
        return vendors.ToInformationResponse();
    }

    [HttpPost]
    [MustHavePermission(AppAction.Create, AppResource.Vendors)]
    [OpenApiOperation("Create vendor and chart of account.", "")]
    public async Task<HttpResponseDto<string>> CreateAsync(VendorCreateRequest request, CancellationToken cancellationToken)
    {
        var accountCode = await _vendorService.CreateAsync(request, cancellationToken);
        return accountCode.ToInformationResponse("Vendor created.");
    }

    [HttpPut("{account}")]
    [MustHavePermission(AppAction.Update, AppResource.Vendors)]
    [OpenApiOperation("Update vendor details.", "")]
    public async Task<HttpResponseDto<string>> UpdateAsync(string account, VendorUpdateRequest request, CancellationToken cancellationToken)
    {
        await _vendorService.UpdateAsync(account, request, cancellationToken);
        return "Vendor updated.".ToInformationResponse("Vendor updated.");
    }

    [HttpPost("presigned-upload-url")]
    [MustHavePermission(new[] { AppAction.Create, AppAction.Update }, AppResource.Vendors)]
    [OpenApiOperation("Generate pre-signed upload URL for vendor image.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetPresignedUploadUrlAsync([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        var response = await _vendorService.GetPresignedUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }
}
