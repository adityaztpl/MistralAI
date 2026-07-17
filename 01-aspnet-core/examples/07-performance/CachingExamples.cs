using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace PerformanceExample.Caching;

public sealed class ProductCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly CatalogDbContext _db;
    private readonly ILogger<ProductCacheService> _logger;
    private readonly SemaphoreSlim _localLock = new(1, 1);

    public ProductCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        CatalogDbContext db,
        ILogger<ProductCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _db = db;
        _logger = logger;
    }

    public async Task<ProductDetail?> GetProductAsync(
        string tenantId,
        int productId,
        CancellationToken cancellationToken)
    {
        var memoryKey = CacheKeys.ProductDetail(tenantId, productId);
        if (_memoryCache.TryGetValue(memoryKey, out ProductDetail? cached))
        {
            return cached;
        }

        await _localLock.WaitAsync(cancellationToken);
        try
        {
            if (_memoryCache.TryGetValue(memoryKey, out cached))
            {
                return cached;
            }

            var distributedJson = await _distributedCache.GetStringAsync(memoryKey, cancellationToken);
            if (distributedJson is not null)
            {
                cached = JsonSerializer.Deserialize<ProductDetail>(distributedJson);
                if (cached is not null)
                {
                    _memoryCache.Set(memoryKey, cached, TimeSpan.FromSeconds(30));
                    return cached;
                }
            }

            var product = await _db.Products
                .AsNoTracking()
                .Where(product => product.TenantId == tenantId && product.Id == productId)
                .Select(product => new ProductDetail(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Category.Name,
                    product.UpdatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            if (product is null)
            {
                return null;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AddJitter(TimeSpan.FromMinutes(5))
            };

            await _distributedCache.SetStringAsync(
                memoryKey,
                JsonSerializer.Serialize(product),
                options,
                cancellationToken);

            _memoryCache.Set(memoryKey, product, TimeSpan.FromSeconds(30));
            return product;
        }
        finally
        {
            _localLock.Release();
        }
    }

    public async Task InvalidateProductAsync(
        string tenantId,
        int productId,
        IOutputCacheStore outputCacheStore,
        CancellationToken cancellationToken)
    {
        var key = CacheKeys.ProductDetail(tenantId, productId);
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);
        await outputCacheStore.EvictByTagAsync(CacheKeys.ProductTag(tenantId, productId), cancellationToken);
        await outputCacheStore.EvictByTagAsync(CacheKeys.ProductListTag(tenantId), cancellationToken);

        _logger.LogInformation("Invalidated product cache for tenant {TenantId} product {ProductId}", tenantId, productId);
    }

    private static TimeSpan AddJitter(TimeSpan duration)
    {
        var jitterSeconds = Random.Shared.Next(0, 30);
        return duration.Add(TimeSpan.FromSeconds(jitterSeconds));
    }
}

public static class CacheKeys
{
    public static string ProductDetail(string tenantId, int productId) =>
        $"tenant:{tenantId}:products:{productId}:detail:v1";

    public static string ProductList(string tenantId, int categoryId, int page, int pageSize) =>
        $"tenant:{tenantId}:products:list:category:{categoryId}:page:{page}:size:{pageSize}:v1";

    public static string ProductTag(string tenantId, int productId) =>
        $"tenant:{tenantId}:product:{productId}";

    public static string ProductListTag(string tenantId) =>
        $"tenant:{tenantId}:products:list";
}

public static class OutputCacheSetup
{
    public static IServiceCollection AddCatalogOutputCaching(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddPolicy("ProductList", policy => policy
                .Expire(TimeSpan.FromSeconds(30))
                .SetVaryByQuery("categoryId", "page", "pageSize")
                .Tag("products"));
        });

        return services;
    }

    public static RouteGroupBuilder MapCachedProductEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (
            string tenantId,
            int categoryId,
            int page,
            int pageSize,
            CatalogDbContext db,
            CancellationToken cancellationToken) =>
        {
            var items = await db.Products
                .AsNoTracking()
                .Where(product => product.TenantId == tenantId && product.CategoryId == categoryId)
                .OrderBy(product => product.Name)
                .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 100))
                .Take(Math.Clamp(pageSize, 1, 100))
                .Select(product => new ProductListItem(product.Id, product.Name, product.Price))
                .ToListAsync(cancellationToken);

            return Results.Ok(items);
        })
        .CacheOutput(policy => policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("tenantId", "categoryId", "page", "pageSize")
            .Tag("products"));

        return group;
    }
}

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
}

public sealed class Product
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record ProductDetail(int Id, string Name, decimal Price, string CategoryName, DateTimeOffset? UpdatedAt);
public sealed record ProductListItem(int Id, string Name, decimal Price);
