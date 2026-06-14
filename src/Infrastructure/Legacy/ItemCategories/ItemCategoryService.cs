using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.ItemCategories;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.ItemCategories;

internal class ItemCategoryService : IItemCategoryService
{
    private readonly IRepository<ItemCategory> _repository;

    public ItemCategoryService(IRepository<ItemCategory> repository)
    {
        _repository = repository;
    }

    public async Task<List<ItemCategoryResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ItemCategoryResponse
            {
                Code = x.Id,
                Title = x.Title,
                Active = x.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ItemCategoryCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        var maxCode = await _repository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AsNoTracking()
            .MaxAsync(x => (long?)Convert.ToInt64(x.Id), cancellationToken) ?? 0;

        var nextCode = (maxCode + 1).ToString("D3");

        var itemCategory = new ItemCategory
        {
            Id = nextCode,
            Title = request.Title,
            Active = request.Active
        };

        await _repository.AddAsync(itemCategory);
    }

    public async Task UpdateAsync(string code, ItemCategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        var itemCategory = await _repository.GetByIdAsync(code, cancellationToken);

        if (itemCategory is null)
            throw new NotFoundException($"Item category code '{code}' not found.");

        itemCategory.Title = request.Title;
        itemCategory.Active = request.Active;

        await _repository.UpdateAsync(itemCategory);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        var itemCategory = await _repository.GetByIdAsync(code, cancellationToken);

        if (itemCategory is null)
            throw new NotFoundException($"Item category code '{code}' not found.");

        await _repository.DeleteAsync(itemCategory);
    }
}
