using System.Reflection;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Narrations;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.EntityFrameworkCore;

namespace Retailer.Infrastructure.Legacy.Narrations;

internal class NarrationService : INarrationService
{
    private readonly IRepository<Narration> _repository;

    public NarrationService(IRepository<Narration> repository)
    {
        _repository = repository;
    }

    public async Task<List<NarrationResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new NarrationResponse
            {
                Code = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(NarrationCreateRequest request, CancellationToken cancellationToken)
    {
        var maxCode = await _repository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var narration = new Narration
        {
            Id = nextCode,
            Title = request.Title
        };

        await _repository.AddAsync(narration);
    }

    public async Task UpdateAsync(string code, NarrationUpdateRequest request, CancellationToken cancellationToken)
    {
        var narration = await _repository.GetByIdAsync(code, cancellationToken);

        if (narration is null)
            throw new NotFoundException($"Narration code '{code}' not found.");

        narration.Title = request.Title;

        await _repository.UpdateAsync(narration);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Code is required.");

        var narration = await _repository.GetByIdAsync(code, cancellationToken);

        if (narration is null)
            return;

        await _repository.DeleteAsync(narration);
    }
}
