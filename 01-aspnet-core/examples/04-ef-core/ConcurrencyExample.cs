using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreDeepDiveExample.Concurrency;

public static class ProductConcurrencyEndpoints
{
    public static RouteGroupBuilder MapProductConcurrencyEndpoints(this RouteGroupBuilder group)
    {
        group.MapPut("/{id:guid}", UpdateProductAsync)
            .WithName("UpdateProductWithConcurrency")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound, ValidationProblem, Conflict<ProblemDetails>>> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        ProductsDbContext db,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Id)] = ["Route id and body id must match."]
            });
        }

        if (!ConvertRowVersion(request.RowVersion, out var rowVersion))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.RowVersion)] = ["RowVersion must be a base64 string returned by the API."]
            });
        }

        var product = await db.Products.SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        db.Entry(product).Property(existing => existing.RowVersion).OriginalValue = rowVersion;

        product.Rename(request.Name);
        product.ChangePrice(request.Price);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var databaseValues = await db.Entry(product).GetDatabaseValuesAsync(cancellationToken);
            if (databaseValues is null)
            {
                return TypedResults.Conflict(new ProblemDetails
                {
                    Title = "Product was deleted by another request.",
                    Status = StatusCodes.Status409Conflict,
                    Detail = "Reload the list before retrying."
                });
            }

            var current = (Product)databaseValues.ToObject();
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Product was modified by another request.",
                Status = StatusCodes.Status409Conflict,
                Detail = "Reload the product and retry with the latest row version.",
                Extensions =
                {
                    ["current"] = new ProductResponse(
                        current.Id,
                        current.Name,
                        current.Price,
                        Convert.ToBase64String(current.RowVersion))
                }
            });
        }

        return TypedResults.Ok(ProductResponse.From(product));
    }

    private static bool ConvertRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}

public sealed class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.RowVersion).IsRowVersion();
            entity.HasIndex(product => product.Name).IsUnique();
        });
    }
}

public sealed class Product
{
    private Product()
    {
    }

    public Product(string name, decimal price)
    {
        Id = Guid.NewGuid();
        Rename(name);
        ChangePrice(price);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        Name = name.Trim();
    }

    public void ChangePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");
        }

        Price = price;
    }
}

public sealed record UpdateProductRequest(Guid Id, string Name, decimal Price, string RowVersion);

public sealed record ProductResponse(Guid Id, string Name, decimal Price, string RowVersion)
{
    public static ProductResponse From(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            Convert.ToBase64String(product.RowVersion));
    }
}
