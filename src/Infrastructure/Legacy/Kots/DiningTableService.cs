using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Kots;
using Retailer.Domain.Legacy;

namespace Retailer.Infrastructure.Legacy.Kots;

internal class DiningTableService : IDiningTableService
{
    private readonly IRepository<DiningTable> _diningTableRepository;

    public DiningTableService(IRepository<DiningTable> diningTableRepository)
    {
        _diningTableRepository = diningTableRepository;
    }

    public async Task<List<DiningTableDto>> GetDiningTablesAsync(CancellationToken cancellationToken)
    {
        return await _diningTableRepository.GetAll().AsNoTracking()
            .Select(x => new DiningTableDto
            {
                Id = x.Id,
                Name = x.Name,
                Capacity = x.Capacity,
                Status = x.Status,
                Active = x.Active
            }).ToListAsync(cancellationToken);
    }

    public async Task<DiningTableDto> CreateDiningTableAsync(DiningTableCreateRequest request, CancellationToken cancellationToken)
    {
        var table = new DiningTable
        {
            Name = request.Name,
            Capacity = request.Capacity,
            Status = "Available",
            Active = request.Active
        };
        await _diningTableRepository.AddAsync(table, true);
        return new DiningTableDto { Id = table.Id, Name = table.Name, Capacity = table.Capacity, Status = table.Status, Active = table.Active };
    }

    public async Task UpdateDiningTableAsync(int id, DiningTableUpdateRequest request, CancellationToken cancellationToken)
    {
        var table = await _diningTableRepository.GetByIdAsync(id, cancellationToken);
        if (table == null)
            throw new NotFoundException($"Dining Table '{id}' not found.");

        table.Name = request.Name;
        table.Capacity = request.Capacity;
        table.Status = request.Status;
        table.Active = request.Active;
        await _diningTableRepository.UpdateAsync(table, true);
    }

    public async Task DeleteDiningTableAsync(int id, CancellationToken cancellationToken)
    {
        var table = await _diningTableRepository.GetByIdAsync(id, cancellationToken);
        if (table != null)
            await _diningTableRepository.DeleteAsync(table, true);
    }
}
