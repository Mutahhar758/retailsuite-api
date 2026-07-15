using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.ItemCategories;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Retailer.Application.Common.Interfaces;
using System.IO;

namespace Retailer.Infrastructure.Legacy.ItemCategories;

internal class ItemCategoryService : IItemCategoryService
{
    private readonly IRepository<ItemCategory> _repository;
    private readonly IMediaServiceClient _mediaServiceClient;

    public ItemCategoryService(IRepository<ItemCategory> repository, IMediaServiceClient mediaServiceClient)
    {
        _repository = repository;
        _mediaServiceClient = mediaServiceClient;
    }

    public async Task<List<ItemCategoryResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        var itemCategories = await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ItemCategoryResponse
            {
                Code = x.Id,
                Title = x.Title,
                Active = x.Active,
                MediaId = x.MediaId
            })
            .ToListAsync(cancellationToken);

        await PopulateMediaUrlsAsync(itemCategories, cancellationToken);
        return itemCategories;
    }

    public async Task<List<ItemCategoryLookupResponse>> GetLookupAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new ItemCategoryLookupResponse
            {
                Code = x.Id,
                Title = x.Title
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
            Active = request.Active,
            MediaId = request.MediaId
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
        itemCategory.MediaId = request.MediaId;

        await _repository.UpdateAsync(itemCategory);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        var itemCategory = await _repository.GetByIdAsync(code, cancellationToken);

        if (itemCategory is null)
            throw new NotFoundException($"Item category code '{code}' not found.");

        await _repository.DeleteAsync(itemCategory);
    }

    public async Task<PresignedUploadUrlResponse?> GetPresignedUploadUrlAsync(string fileName, CancellationToken cancellationToken)
    {
        var cleanFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName != fileName || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            throw new BadRequestException("Invalid file name. Only plain file names without paths are allowed.");
        }

        return await _mediaServiceClient.GetUploadUrlAsync(cleanFileName, "category", cancellationToken);
    }

    private async Task PopulateMediaUrlsAsync(List<ItemCategoryResponse> items, CancellationToken cancellationToken)
    {
        var tasks = items
            .Where(x => !string.IsNullOrEmpty(x.MediaId))
            .Select(async item =>
            {
                try
                {
                    var sasResponse = await _mediaServiceClient.GetViewTokenAsync(item.MediaId!, 24, cancellationToken);
                    if (sasResponse != null)
                    {
                        item.MediaUrl = sasResponse.ViewUrl;
                    }
                }
                catch
                {
                    // Fail-safe: ignore media service exceptions to keep main application running
                }
            });
        await Task.WhenAll(tasks);
    }
}
