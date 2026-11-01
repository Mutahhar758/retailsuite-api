using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Legacy.CompanyDetails;
using Retailer.Infrastructure.Auth.Permissions;
using Retailer.Infrastructure.Common.Extensions;
using Retailer.Shared.Authorization;

namespace Retailer.Host.Controllers.Legacy;

public class CompaniesController : VersionNeutralApiController
{
    private readonly ICompanyDetailService _companyDetailService;

    public CompaniesController(ICompanyDetailService companyDetailService)
    {
        _companyDetailService = companyDetailService;
    }

    [HttpGet("current")]
    [OpenApiOperation("Get current company information for authenticated users.", "")]
    public async Task<HttpResponseDto<CompanyDetailResponse>> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var result = await _companyDetailService.GetCurrentAsync(cancellationToken)
            ?? new CompanyDetailResponse();

        return result.ToInformationResponse();
    }

    [HttpPut("current")]
    [MustHavePermission(AppAction.Update, AppResource.Settings)]
    [OpenApiOperation("Update current company information (excluding BOP-managed company name).", "")]
    public async Task<HttpResponseDto<CompanyDetailResponse>> UpdateCurrentAsync(
        CompanyDetailUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _companyDetailService.UpdateCurrentAsync(request, cancellationToken);
        return result.ToInformationResponse("Company information updated successfully.");
    }

    [HttpPost("presigned-upload-url")]
    [MustHavePermission(AppAction.Update, AppResource.Settings)]
    [OpenApiOperation("Generate pre-signed upload URL for company logo.", "")]
    public async Task<HttpResponseDto<PresignedUploadUrlResponse?>> GetLogoUploadUrlAsync(
        [FromQuery] string fileName,
        CancellationToken cancellationToken)
    {
        var response = await _companyDetailService.GetLogoUploadUrlAsync(fileName, cancellationToken);
        return response.ToInformationResponse();
    }
}
