using Retailer.Application.Common.Interfaces;

namespace Retailer.Application.Legacy.Kots;

public interface IPrepStationService : ITransientService
{
    Task<List<PrepStationDto>> GetPrepStationsAsync(CancellationToken cancellationToken);
    Task<PrepStationDto> CreatePrepStationAsync(PrepStationCreateRequest request, CancellationToken cancellationToken);
    Task UpdatePrepStationAsync(string id, PrepStationUpdateRequest request, CancellationToken cancellationToken);
    Task DeletePrepStationAsync(string id, CancellationToken cancellationToken);
}
