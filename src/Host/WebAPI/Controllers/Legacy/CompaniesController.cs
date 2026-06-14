using Retailer.Application.Legacy.CompanyDetails;
using Retailer.Infrastructure.Common.Extensions;

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
}
