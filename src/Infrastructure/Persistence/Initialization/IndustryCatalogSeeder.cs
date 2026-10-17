using System.Text.Json;
using Retailer.Application.Common.Interfaces;
using Retailer.Application.Common.Persistence;
using Retailer.Domain.Common.Enums;
using Retailer.Domain.Legacy;
using Retailer.Shared.Common.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Retailer.Infrastructure.Persistence.Initialization;

public class IndustryCatalogSeeder : ICustomSeeder
{
    private readonly IRepository<Brand> _brandRepository;
    private readonly IRepository<ItemCategory> _categoryRepository;
    private readonly IRepository<ItemDetail> _itemRepository;
    private readonly IRepository<Unit> _unitRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<IndustryCatalogSeeder> _logger;

    public IndustryCatalogSeeder(
        IRepository<Brand> brandRepository,
        IRepository<ItemCategory> categoryRepository,
        IRepository<ItemDetail> itemRepository,
        IRepository<Unit> unitRepository,
        ICurrentTenant currentTenant,
        ILogger<IndustryCatalogSeeder> logger)
    {
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _itemRepository = itemRepository;
        _unitRepository = unitRepository;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Check if categories or items already exist for this tenant
        bool hasCategories = await _categoryRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AnyAsync(cancellationToken);

        if (hasCategories)
        {
            _logger.LogInformation("Item categories already exist for tenant '{TenantId}'. Skipping dynamic catalog seeding.", _currentTenant.Id);
            return;
        }

        // Ensure base units exist first
        await EnsureUnitsAsync(cancellationToken);

        // Resolve business industry and subcategory
        var (industry, subCategory) = ResolveProfile();
        _logger.LogInformation("Seeding dynamic catalog for tenant '{TenantId}' with Industry='{Industry}', SubCategory='{SubCategory}'.",
            _currentTenant.Id, industry, subCategory);

        var (brands, categories, products) = await LoadSeedDataAsync(industry, subCategory, cancellationToken);

        // 1. Seed Brands (if table is empty for tenant)
        bool hasBrands = await _brandRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AnyAsync(cancellationToken);

        if (!hasBrands && brands.Count > 0)
        {
            await _brandRepository.AddRangeAsync(brands);
            await _brandRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} brands for tenant '{TenantId}'.", brands.Count, _currentTenant.Id);
        }

        // 2. Seed Item Categories
        if (categories.Count > 0)
        {
            await _categoryRepository.AddRangeAsync(categories);
            await _categoryRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} categories for tenant '{TenantId}'.", categories.Count, _currentTenant.Id);
        }

        // 3. Seed Products (ItemDetail)
        bool hasItems = await _itemRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AnyAsync(cancellationToken);

        if (!hasItems && products.Count > 0)
        {
            await _itemRepository.AddRangeAsync(products);
            await _itemRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} products for tenant '{TenantId}'.", products.Count, _currentTenant.Id);
        }
    }

    private (string Industry, string SubCategory) ResolveProfile()
    {
        string industry = _currentTenant.IndustryType?.Trim().ToLowerInvariant() ?? string.Empty;
        string subCategory = _currentTenant.BusinessSubType?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(industry))
        {
            if (_currentTenant.HasVariablePackFeature)
                industry = "wanda";
            else if (_currentTenant.HasMobileShopFeature)
                industry = "mobile";
            else
                industry = "general";
        }

        if (industry == "general")
        {
            if (string.IsNullOrWhiteSpace(subCategory))
            {
                subCategory = "general_store";
            }
            else if (subCategory.Contains("bakery"))
            {
                subCategory = "bakery";
            }
            else if (subCategory.Contains("milk") || subCategory.Contains("dairy"))
            {
                subCategory = "milkshop";
            }
            else
            {
                subCategory = "general_store";
            }
        }

        return (industry, subCategory);
    }

    private async Task EnsureUnitsAsync(CancellationToken cancellationToken)
    {
        bool hasUnits = await _unitRepository.GetAll()
            .IgnoreQueryFilters([GlobalQueryFilterConstants.SoftDelete])
            .AnyAsync(cancellationToken);

        if (!hasUnits)
        {
            var units = new List<Unit>
            {
                new() { Id = "001", Title = "Litre" },
                new() { Id = "002", Title = "Kilogram" },
                new() { Id = "003", Title = "Gram" },
                new() { Id = "004", Title = "Pack" },
                new() { Id = "005", Title = "Piece" },
                new() { Id = "006", Title = "Dozen" },
                new() { Id = "007", Title = "Bag" }
            };

            await _unitRepository.AddRangeAsync(units);
            await _unitRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(List<Brand> Brands, List<ItemCategory> Categories, List<ItemDetail> Products)> LoadSeedDataAsync(
        string industry, string subCategory, CancellationToken cancellationToken)
    {
        string fileName = industry switch
        {
            "wanda" => "wanda",
            "mobile" => "mobile",
            _ => subCategory switch
            {
                "bakery" => "general_bakery",
                "milkshop" => "general_milkshop",
                _ => "general_store"
            }
        };

        string? filePath = FindSeedDataFile(fileName);
        if (filePath != null && File.Exists(filePath))
        {
            try
            {
                _logger.LogInformation("Loading seed catalog from JSON file: {FilePath}", filePath);
                string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<CatalogSeedDocument>(json, options);

                if (doc != null)
                {
                    return MapDocument(doc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse seed data JSON from {FilePath}.", filePath);
            }
        }
        else
        {
            _logger.LogWarning("Seed data JSON file '{FileName}.json' was not found. Looked in bin and source tree.", fileName);
        }

        return (new List<Brand>(), new List<ItemCategory>(), new List<ItemDetail>());
    }

    private string? FindSeedDataFile(string fileName)
    {
        // 1. Output bin directory: e.g. AppContext.BaseDirectory/Persistence/Initialization/SeedData/{fileName}.json
        var path1 = Path.Combine(AppContext.BaseDirectory, "Persistence", "Initialization", "SeedData", $"{fileName}.json");
        if (File.Exists(path1)) return path1;

        // 2. Base directory SeedData directly: e.g. AppContext.BaseDirectory/SeedData/{fileName}.json
        var path2 = Path.Combine(AppContext.BaseDirectory, "SeedData", $"{fileName}.json");
        if (File.Exists(path2)) return path2;

        // 3. Search up from CurrentDirectory / BaseDirectory to source tree (allows dev-time live JSON modifications)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Infrastructure", "Persistence", "Initialization", "SeedData", $"{fileName}.json");
            if (File.Exists(candidate)) return candidate;

            candidate = Path.Combine(dir.FullName, "retailsuite-api", "src", "Infrastructure", "Persistence", "Initialization", "SeedData", $"{fileName}.json");
            if (File.Exists(candidate)) return candidate;

            dir = dir.Parent;
        }

        // 4. Try current working directory
        var cwd = Directory.GetCurrentDirectory();
        var candidateCwd = Path.Combine(cwd, "src", "Infrastructure", "Persistence", "Initialization", "SeedData", $"{fileName}.json");
        if (File.Exists(candidateCwd)) return candidateCwd;

        return null;
    }

    private static (List<Brand> Brands, List<ItemCategory> Categories, List<ItemDetail> Products) MapDocument(CatalogSeedDocument doc)
    {
        var brands = (doc.Brands ?? new List<SeedBrandDto>()).Select(b => new Brand
        {
            Id = b.Id,
            Title = b.Title,
            Active = b.Active
        }).ToList();

        var categories = (doc.Categories ?? new List<SeedCategoryDto>()).Select(c => new ItemCategory
        {
            Id = c.Id,
            Title = c.Title,
            ItemType = c.ItemType,
            Active = c.Active
        }).ToList();

        var products = (doc.Products ?? new List<SeedProductDto>()).Select(p =>
        {
            ItemType itemType = ItemType.Product;
            if (!string.IsNullOrWhiteSpace(p.ItemType) && Enum.TryParse<ItemType>(p.ItemType, true, out var parsedType))
            {
                itemType = parsedType;
            }

            string barcode = !string.IsNullOrWhiteSpace(p.Barcode)
                ? p.Barcode.Trim()
                : (int.TryParse(p.CategoryId, out var catNum) ? catNum.ToString() : (p.CategoryId ?? "001")) + p.Id;

            return new ItemDetail
            {
                Id = p.Id,
                Title = p.Title,
                Barcode = barcode,
                ItemCategoryId = p.CategoryId,
                BrandId = p.BrandId,
                ItemType = itemType,
                PrimaryUnitId = p.PrimaryUnitId,
                SecondaryUnitId = p.SecondaryUnitId,
                DefaultUnitId = p.DefaultUnitId,
                QtyInPack = p.QtyInPack,
                PriRate = p.PriRate,
                SecRate = p.SecRate,
                RequireImei = p.RequireImei,
                Ram = p.Ram,
                Storage = p.Storage,
                Color = p.Color
            };
        }).ToList();

        return (brands, categories, products);
    }

    #region JSON Document DTOs
    private class CatalogSeedDocument
    {
        public string Industry { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SeedBrandDto> Brands { get; set; } = new();
        public List<SeedCategoryDto> Categories { get; set; } = new();
        public List<SeedProductDto> Products { get; set; } = new();
    }

    private class SeedBrandDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }

    private class SeedCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ItemType { get; set; }
        public bool Active { get; set; } = true;
    }

    private class SeedProductDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? CategoryId { get; set; }
        public string? BrandId { get; set; }
        public string? ItemType { get; set; }
        public string? PrimaryUnitId { get; set; }
        public string? SecondaryUnitId { get; set; }
        public string? DefaultUnitId { get; set; }
        public decimal? QtyInPack { get; set; }
        public decimal PriRate { get; set; }
        public decimal SecRate { get; set; }
        public bool? RequireImei { get; set; }
        public string? Ram { get; set; }
        public string? Storage { get; set; }
        public string? Color { get; set; }
    }
    #endregion
}
