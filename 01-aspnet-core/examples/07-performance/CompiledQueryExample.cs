using Microsoft.EntityFrameworkCore;

namespace PerformanceExample.CompiledQueries;

public sealed class CompiledProductQueries
{
    private static readonly Func<CatalogDbContext, string, int, Task<ProductDetail?>> ProductById =
        EF.CompileAsyncQuery((CatalogDbContext db, string tenantId, int productId) =>
            db.Products
                .AsNoTracking()
                .Where(product => product.TenantId == tenantId && product.Id == productId)
                .Select(product => new ProductDetail(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Category.Name,
                    product.InventoryOnHand))
                .SingleOrDefault());

    private static readonly Func<CatalogDbContext, string, int, int, IAsyncEnumerable<ProductListItem>> ActiveProductsByCategory =
        EF.CompileAsyncQuery((CatalogDbContext db, string tenantId, int categoryId, int take) =>
            db.Products
                .AsNoTracking()
                .Where(product =>
                    product.TenantId == tenantId &&
                    product.CategoryId == categoryId &&
                    product.IsActive)
                .OrderBy(product => product.Name)
                .ThenBy(product => product.Id)
                .Take(take)
                .Select(product => new ProductListItem(
                    product.Id,
                    product.Name,
                    product.Price)));

    private readonly CatalogDbContext _db;

    public CompiledProductQueries(CatalogDbContext db)
    {
        _db = db;
    }

    public Task<ProductDetail?> GetProductByIdAsync(
        string tenantId,
        int productId,
        CancellationToken cancellationToken)
    {
        // Compiled query delegates do not accept CancellationToken directly in EF. The query still
        // executes asynchronously; use compiled queries only on measured hot paths where this tradeoff is acceptable.
        return ProductById(_db, tenantId, productId);
    }

    public async Task<IReadOnlyList<ProductListItem>> GetActiveProductsByCategoryAsync(
        string tenantId,
        int categoryId,
        int take,
        CancellationToken cancellationToken)
    {
        var items = new List<ProductListItem>();
        await foreach (var item in ActiveProductsByCategory(_db, tenantId, categoryId, Math.Clamp(take, 1, 100))
            .WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        return items;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(product => new { product.TenantId, product.CategoryId, product.IsActive, product.Name });
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);
        });
    }
}

public sealed class Product
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public int InventoryOnHand { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record ProductDetail(int Id, string Name, decimal Price, string CategoryName, int InventoryOnHand);
public sealed record ProductListItem(int Id, string Name, decimal Price);
