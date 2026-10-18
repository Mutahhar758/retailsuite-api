using Microsoft.EntityFrameworkCore;
using Retailer.Application.Common.Exceptions;
using Retailer.Application.Common.Persistence;
using Retailer.Application.Legacy.CustomerCategories;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;

namespace Retailer.Infrastructure.Legacy.CustomerCategories;

internal class CustomerCategoryService : ICustomerCategoryService
{
    private readonly IRepository<CustomerCategory> _repository;

    public CustomerCategoryService(IRepository<CustomerCategory> repository)
    {
        _repository = repository;
    }

    public async Task<List<CustomerCategoryResponse>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new CustomerCategoryResponse
            {
                Code = x.Id,
                Title = x.Title,
                Description = x.Description,
                Active = x.Active
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CustomerCategoryLookupResponse>> GetLookupAsync(CancellationToken cancellationToken)
    {
        return await _repository.GetAll()
            .AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.Title)
            .Select(x => new CustomerCategoryLookupResponse
            {
                Code = x.Id,
                Title = x.Title
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<string> CreateAsync(CustomerCategoryCreateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        string code = request.Code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            var existingCodes = await _repository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            long maxCode = 0;
            foreach (var c in existingCodes)
            {
                if (long.TryParse(c, out var num) && num > maxCode)
                    maxCode = num;
            }

            code = (maxCode + 1).ToString("D3");
        }
        else
        {
            var exists = await _repository.GetAll()
                .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
                .AnyAsync(x => x.Id == code, cancellationToken);
            if (exists)
                throw new BadRequestException($"Customer category code '{code}' already exists.");
        }

        var customerCategory = new CustomerCategory
        {
            Id = code,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Active = request.Active
        };

        await _repository.AddAsync(customerCategory);
        return customerCategory.Id;
    }

    public async Task UpdateAsync(string code, CustomerCategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new BadRequestException("Title is required.");

        var customerCategory = await _repository.GetByIdAsync(code, cancellationToken);
        if (customerCategory is null)
            throw new NotFoundException($"Customer category code '{code}' not found.");

        customerCategory.Title = request.Title.Trim();
        customerCategory.Description = request.Description?.Trim();
        customerCategory.Active = request.Active;

        await _repository.UpdateAsync(customerCategory);
    }

    public async Task DeleteAsync(string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new BadRequestException("Code is required.");

        var customerCategory = await _repository.GetByIdAsync(code, cancellationToken);
        if (customerCategory is null)
            return;

        await _repository.DeleteAsync(customerCategory);
    }
}
