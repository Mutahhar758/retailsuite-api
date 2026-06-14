using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.CompanyDetails;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.CompanyDetails;

internal class CompanyDetailService : ICompanyDetailService
{
    private readonly IRepository<CompanyDetail> _repository;

    public CompanyDetailService(IRepository<CompanyDetail> repository)
    {
        _repository = repository;
    }

    public async Task<CompanyDetailResponse?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new CompanyDetailResponse
            {
                CompanyName = x.CompanyName,
                UrCompanyName = x.UrCompanyName,
                Descr = x.Descr,
                Address = x.Address,
                Phone = x.Phone,
                Cell = x.Cell,
                Cell2 = x.Cell2,
                ContactHeader = x.ContactHeader
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
