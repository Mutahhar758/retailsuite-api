using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.Brands;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.Brands;

internal class BrandService : IBrandService
{
    private readonly IRepository<Brand> _repository;

    public BrandService(IRepository<Brand> repository)
    {
        _repository = repository;
    }

    public async Task<List<BrandResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new BrandResponse
            {
                Id = x.Id,
                Title = x.Title,
                Active = x.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BrandResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.Title)
            .Select(x => new BrandResponse
            {
                Id = x.Id,
                Title = x.Title,
                Active = x.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(id, cancellationToken);
        if (brand == null) return null;

        return new BrandResponse
        {
            Id = brand.Id,
            Title = brand.Title,
            Active = brand.Active
        };
    }

    public async Task<string> CreateAsync(BrandCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Brand title is required.");

        var existingWithTitle = await _repository.GetAll()
            .AnyAsync(x => x.Title.ToLower() == request.Title.Trim().ToLower(), cancellationToken);
        if (existingWithTitle)
            throw new ConflictException($"Brand '{request.Title}' already exists.");

        string id = request.Id?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(id) && id != "0")
        {
            var existingWithId = await _repository.GetByIdAsync(id, cancellationToken);
            if (existingWithId != null)
            {
                id = string.Empty;
            }
        }

        if (string.IsNullOrWhiteSpace(id) || id == "0")
        {
            var maxId = await _repository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .AsNoTracking()
                .MaxAsync(x => (string?)x.Id, cancellationToken);

            var nextNum = maxId == null ? 1L : (long.TryParse(maxId, out var parsed) ? parsed + 1 : 1L);
            id = nextNum.ToString("D3");
        }

        var brand = new Brand
        {
            Id = id,
            Title = request.Title.Trim(),
            Active = request.Active
        };

        await _repository.AddAsync(brand, true);
        return brand.Id;
    }

    public async Task UpdateAsync(string id, BrandUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Brand title is required.");

        var brand = await _repository.GetByIdAsync(id, cancellationToken);
        if (brand == null)
        {
            await CreateAsync(new BrandCreateRequest
            {
                Id = id,
                Title = request.Title,
                Active = request.Active
            }, cancellationToken);
            return;
        }

        var existingWithTitle = await _repository.GetAll()
            .AnyAsync(x => x.Id != id && x.Title.ToLower() == request.Title.Trim().ToLower(), cancellationToken);
        if (existingWithTitle)
            throw new ConflictException($"Brand '{request.Title}' already exists.");

        brand.Title = request.Title.Trim();
        brand.Active = request.Active;

        await _repository.UpdateAsync(brand, true);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Brand with id '{id}' not found.");

        await _repository.DeleteAsync(brand, true);
    }
}
