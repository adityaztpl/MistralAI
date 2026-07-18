using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace TestingExample.Products;

public sealed class ProductsControllerTests : IClassFixture<ProductsApiFactory>
{
    private readonly ProductsApiFactory _factory;

    public ProductsControllerTests(ProductsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListProducts_WithReadScope_ReturnsSeededProducts()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient("products.read");

        var response = await client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ProductListItemResponse>>();
        Assert.NotNull(page);
        Assert.Contains(page!.Items, product => product.Name == "Mechanical Keyboard");
    }

    [Fact]
    public async Task CreateProduct_WithoutWriteScope_ReturnsForbidden()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient("products.read");

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "USB Microphone",
            149.99m,
            1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WhenNameIsMissing_ReturnsValidationProblem()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient("products.write");

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            "",
            149.99m,
            1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(nameof(CreateProductRequest.Name), problem!.Errors.Keys);
    }

    [Fact]
    public async Task UpdateProduct_WithStaleRowVersion_ReturnsConflict()
    {
        await _factory.ResetDatabaseAsync();
        using var client = _factory.CreateAuthenticatedClient("products.read", "products.write");

        var original = await client.GetFromJsonAsync<ProductDetailResponse>("/api/products/1");
        Assert.NotNull(original);

        var firstUpdate = await client.PutAsJsonAsync("/api/products/1", new UpdateProductRequest(
            "Mechanical Keyboard Pro",
            159.99m,
            1,
            original!.RowVersion));
        firstUpdate.EnsureSuccessStatusCode();

        var staleUpdate = await client.PutAsJsonAsync("/api/products/1", new UpdateProductRequest(
            "Mechanical Keyboard Ultra",
            179.99m,
            1,
            original.RowVersion));

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
        var problem = await staleUpdate.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Product was modified by another request.", problem!.Title);
    }
}

public sealed class ProductsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(params string[] scopes)
    {
        var client = CreateClient();
        var scopeHeader = string.Join(' ', scopes);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        client.DefaultRequestHeaders.Add(TestAuthHandler.ScopeHeaderName, scopeHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, "user-1");
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantIdHeaderName, "tenant-a");
        return client;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await ResetDatabaseAsync();
    }

    public new async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.Categories.Add(new Category { Id = 1, Name = "Electronics" });
        db.Products.Add(new Product
        {
            Id = 1,
            Name = "Mechanical Keyboard",
            Price = 129.99m,
            CategoryId = 1,
            TenantId = "tenant-a"
        });

        await db.SaveChangesAsync();
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string ScopeHeaderName = "X-Test-Scopes";
    public const string UserIdHeaderName = "X-Test-UserId";
    public const string TenantIdHeaderName = "X-Test-TenantId";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var scopes = Request.Headers[ScopeHeaderName]
            .ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Request.Headers[UserIdHeaderName].FirstOrDefault() ?? "test-user"),
            new("tenant_id", Request.Headers[TenantIdHeaderName].FirstOrDefault() ?? "tenant-a")
        };

        claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.RowVersion).IsRowVersion();
        });
    }
}

public sealed class Product
{
    public int Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record CreateProductRequest(string Name, decimal Price, int CategoryId);
public sealed record UpdateProductRequest(string Name, decimal Price, int CategoryId, string RowVersion);
public sealed record ProductListItemResponse(int Id, string Name, decimal Price);
public sealed record ProductDetailResponse(int Id, string Name, decimal Price, int CategoryId, string RowVersion);
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
