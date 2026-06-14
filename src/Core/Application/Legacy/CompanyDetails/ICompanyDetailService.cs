namespace Retailer.Application.Legacy.CompanyDetails;

public interface ICompanyDetailService : ITransientService
{
    Task<CompanyDetailResponse?> GetCurrentAsync(CancellationToken cancellationToken);
}
