using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Kots;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Kots;

internal class PrepStationService : IPrepStationService
{
    private readonly IRepository<PrepStation> _prepStationRepository;

    public PrepStationService(IRepository<PrepStation> prepStationRepository)
    {
        _prepStationRepository = prepStationRepository;
    }

    public async Task<List<PrepStationDto>> GetPrepStationsAsync(CancellationToken cancellationToken)
    {
        return await _prepStationRepository.GetAll().AsNoTracking()
            .Select(x => new PrepStationDto
            {
                Id = x.Id,
                Name = x.Name,
                Active = x.Active
            }).ToListAsync(cancellationToken);
    }

    public async Task<PrepStationDto> CreatePrepStationAsync(PrepStationCreateRequest request, CancellationToken cancellationToken)
    {
        var prep = new PrepStation
        {
            Id = request.Id.ToUpper(),
            Name = request.Name,
            Active = request.Active
        };
        await _prepStationRepository.AddAsync(prep, true);
        return new PrepStationDto { Id = prep.Id, Name = prep.Name, Active = prep.Active };
    }

    public async Task UpdatePrepStationAsync(string id, PrepStationUpdateRequest request, CancellationToken cancellationToken)
    {
        var prep = await _prepStationRepository.GetByIdAsync(id, cancellationToken);
        if (prep == null)
            throw new NotFoundException($"Prep Station '{id}' not found.");

        prep.Name = request.Name;
        prep.Active = request.Active;
        await _prepStationRepository.UpdateAsync(prep, true);
    }

    public async Task DeletePrepStationAsync(string id, CancellationToken cancellationToken)
    {
        var prep = await _prepStationRepository.GetByIdAsync(id, cancellationToken);
        if (prep != null)
            await _prepStationRepository.DeleteAsync(prep, true);
    }
}
