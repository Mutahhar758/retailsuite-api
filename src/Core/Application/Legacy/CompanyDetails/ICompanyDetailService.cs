using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.CompanyDetails;

public interface ICompanyDetailService : ITransientService
{
    Task<CompanyDetailResponse?> GetCurrentAsync(CancellationToken cancellationToken);
    Task<CompanyDetailResponse> UpdateCurrentAsync(CompanyDetailUpdateRequest request, CancellationToken cancellationToken);
    Task<PresignedUploadUrlResponse?> GetLogoUploadUrlAsync(string fileName, CancellationToken cancellationToken);
}
