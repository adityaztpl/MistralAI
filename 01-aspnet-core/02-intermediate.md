# ASP.NET Core Intermediate

This guide builds on the basics by adding the pieces most real APIs need: EF Core persistence, DTOs, mapping, authentication, authorization, CORS, filters, exception handling, caching, SignalR, background services, and API versioning.

## 1. EF Core fundamentals

Entity Framework Core is Microsoft's modern ORM for .NET. It maps C# entity classes to relational tables and provides LINQ-based querying.

### Entity example

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
}
```

### `DbContext`

```csharp
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
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Name).HasMaxLength(200).IsRequired();
            entity.Property(product => product.Price).HasPrecision(18, 2);

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

### Register EF Core

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
```

For PostgreSQL, use `UseNpgsql`; for SQLite, use `UseSqlite`.

### Interview talking points

- `DbContext` is usually registered as scoped because it tracks changes for one unit of work/request.
- `DbSet<T>` represents a table-like collection.
- EF Core translates LINQ expressions to SQL where possible.
- Use explicit configuration for precision, max lengths, indexes, relationships, and delete behavior.

## 2. Migrations

Migrations track database schema changes over time.

Typical commands:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
dotnet ef migrations add AddProductCategory
dotnet ef database update
```

In production, teams often generate SQL scripts instead of letting the app mutate the database directly:

```bash
dotnet ef migrations script --idempotent -o migration.sql
```

### Interview talking points

- Migrations are code artifacts representing schema evolution.
- Keep migrations reviewed and deterministic.
- Avoid running destructive migrations automatically in production without an explicit deployment process.
- Seed reference data carefully; application data seeding can become environment-specific.

## 3. Relationships

EF Core supports one-to-many, one-to-one, and many-to-many relationships.

### One-to-many

```csharp
modelBuilder.Entity<Category>()
    .HasMany(category => category.Products)
    .WithOne(product => product.Category)
    .HasForeignKey(product => product.CategoryId);
```

### Many-to-many

```csharp
public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Tag> Tags { get; set; } = [];
}

public sealed class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = [];
}

modelBuilder.Entity<Product>()
    .HasMany(product => product.Tags)
    .WithMany(tag => tag.Products);
```

### Loading related data

```csharp
var product = await db.Products
    .Include(product => product.Category)
    .FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
```

Projection is often better for APIs:

```csharp
var product = await db.Products
    .Where(product => product.Id == id)
    .Select(product => new ProductResponse(
        product.Id,
        product.Name,
        product.Price,
        product.Category.Name))
    .FirstOrDefaultAsync(cancellationToken);
```

### Interview talking points

- Prefer projections for read APIs to avoid over-fetching.
- Be careful with lazy loading; it can create N+1 queries.
- Configure delete behavior intentionally.

## 4. Repository and Unit of Work patterns

EF Core's `DbContext` already implements repository-like access through `DbSet<T>` and Unit of Work through `SaveChangesAsync`. Adding custom repositories can be useful, but it can also hide EF Core's strengths.

### Repository example

```csharp
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    void Remove(Product product);
}

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _db.Products.AddAsync(product, cancellationToken);
    }

    public void Remove(Product product)
    {
        _db.Products.Remove(product);
    }
}
```

### Unit of Work example

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public EfUnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
```

### Interview talking points

- Repository can help isolate complex queries or make persistence replaceable.
- Generic repositories often become thin wrappers around EF Core and may reduce expressiveness.
- In Clean Architecture, repositories often live as interfaces in the application layer with EF implementations in infrastructure.

## 5. DTOs and mapping

DTOs define what your API accepts and returns. They prevent over-posting, protect internal models, and let API contracts evolve independently.

```csharp
public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    int CategoryId);

public sealed record UpdateProductRequest(
    string Name,
    decimal Price,
    int CategoryId,
    bool IsActive);

public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    string CategoryName,
    bool IsActive);
```

Manual mapping is explicit and dependency-free:

```csharp
public static class ProductMapping
{
    public static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Price,
            product.Category.Name,
            product.IsActive);
    }
}
```

### AutoMapper

```csharp
public sealed class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductResponse>()
            .ForCtorParam("CategoryName", options =>
                options.MapFrom(product => product.Category.Name));

        CreateMap<CreateProductRequest, Product>();
    }
}

builder.Services.AddAutoMapper(typeof(ProductProfile));
```

### Mapster

```csharp
TypeAdapterConfig<Product, ProductResponse>
    .NewConfig()
    .Map(destination => destination.CategoryName, source => source.Category.Name);

builder.Services.AddMapster();
```

### Interview talking points

- Manual mapping is best for small APIs and critical paths.
- Mapping libraries reduce boilerplate but hide some behavior.
- Avoid returning EF entities directly from API endpoints.

## 6. JWT authentication and Identity

Authentication answers "Who are you?" Authorization answers "What are you allowed to do?"

### JWT bearer setup

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Write", policy =>
        policy.RequireClaim("scope", "products.write"));
});

app.UseAuthentication();
app.UseAuthorization();
```

### Protect endpoints

```csharp
app.MapGet("/api/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        Name = user.Identity?.Name,
        Subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
    });
})
.RequireAuthorization();

app.MapPost("/api/products", CreateProduct)
    .RequireAuthorization("Products.Write");
```

Controller:

```csharp
[Authorize]
[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    [Authorize(Policy = "Products.Write")]
    [HttpPost]
    public IActionResult Create(CreateProductRequest request) => Created();
}
```

### ASP.NET Core Identity

Identity provides user management: password hashing, lockout, roles, claims, token generation, and stores backed by EF Core.

```csharp
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
```

### Interview talking points

- JWTs are stateless bearer tokens; protect signing keys carefully.
- Validate issuer, audience, lifetime, and signing key.
- Do not store sensitive data in JWT payloads because clients can decode them.
- Use short token lifetimes plus refresh token strategy for higher security.

## 7. CORS

Cross-Origin Resource Sharing controls which browser origins may call your API.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("https://app.example.com")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Authorization", "Content-Type");
    });
});

app.UseCors("Frontend");
```

### Interview talking points

- CORS is enforced by browsers, not by server-to-server callers.
- Avoid `AllowAnyOrigin()` with credentials.
- Put `UseCors` in the correct middleware order, often before auth endpoints are mapped.

## 8. Filters

Filters are controller/MVC pipeline hooks. They can run before or after actions.

Common types:

- Authorization filters
- Resource filters
- Action filters
- Exception filters
- Result filters

### Action filter example

```csharp
public sealed class LogActionFilter : IActionFilter
{
    private readonly ILogger<LogActionFilter> _logger;

    public LogActionFilter(ILogger<LogActionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        _logger.LogInformation("Executing {Action}", context.ActionDescriptor.DisplayName);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation("Executed {Action}", context.ActionDescriptor.DisplayName);
    }
}
```

Register globally:

```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<LogActionFilter>();
});
```

### Minimal API endpoint filters

```csharp
app.MapPost("/api/products", CreateProduct)
    .AddEndpointFilter(async (context, next) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Before minimal API endpoint");
        return await next(context);
    });
```

### Interview talking points

- Filters are common for controllers; endpoint filters are the minimal API equivalent.
- Prefer middleware for cross-cutting behavior that applies to all requests.
- Prefer filters for endpoint/action-specific concerns.

## 9. Exception handling middleware

Centralized exception handling avoids repeated `try/catch` blocks and gives clients consistent errors.

```csharp
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail,
            Instance = context.Request.Path
        });
    }
}
```

.NET also provides `IExceptionHandler` and `AddExceptionHandler` for structured exception handling.

### Interview talking points

- Do not leak stack traces or sensitive details to clients.
- Log unexpected exceptions once at the boundary.
- Return RFC 7807 `ProblemDetails` for consistent machine-readable errors.

## 10. Caching

Caching improves latency and reduces database load, but introduces invalidation complexity.

### In-memory cache

```csharp
builder.Services.AddMemoryCache();

public sealed class ProductLookupService
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _db;

    public ProductLookupService(IMemoryCache cache, AppDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public Task<ProductResponse?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync($"products:{id}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await _db.Products
                .Where(product => product.Id == id)
                .Select(product => new ProductResponse(
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Category.Name,
                    product.IsActive))
                .FirstOrDefaultAsync(cancellationToken);
        });
    }
}
```

### Output caching

```csharp
builder.Services.AddOutputCache();

app.UseOutputCache();

app.MapGet("/api/products", GetProducts)
    .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)));
```

### Interview talking points

- In-memory cache is per app instance; distributed cache is shared across instances.
- Cache invalidation should be explicit after writes.
- Use short TTLs for frequently changing data.

## 11. SignalR introduction

SignalR enables realtime server-to-client communication over WebSockets with fallbacks.

### Hub

```csharp
public sealed class NotificationsHub : Hub
{
    public async Task JoinProductGroup(int productId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"product:{productId}");
    }
}
```

### Register and map

```csharp
builder.Services.AddSignalR();

app.MapHub<NotificationsHub>("/hubs/notifications");
```

### Send messages

```csharp
public sealed class ProductNotifier
{
    private readonly IHubContext<NotificationsHub> _hub;

    public ProductNotifier(IHubContext<NotificationsHub> hub)
    {
        _hub = hub;
    }

    public Task ProductUpdatedAsync(int productId)
    {
        return _hub.Clients
            .Group($"product:{productId}")
            .SendAsync("ProductUpdated", new { productId });
    }
}
```

### Interview talking points

- SignalR abstracts WebSockets and connection management.
- Use groups for targeted notifications.
- In multi-server deployments, use a backplane such as Redis or Azure SignalR Service.

## 12. Background services and `IHostedService`

Background services run alongside the web host. Use them for scheduled jobs, queues, polling, or asynchronous processing.

```csharp
public sealed class ProductSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductSyncWorker> _logger;

    public ProductSyncWorker(IServiceScopeFactory scopeFactory, ILogger<ProductSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var count = await db.Products.CountAsync(stoppingToken);
            _logger.LogInformation("Product sync saw {Count} products", count);
        }
    }
}

builder.Services.AddHostedService<ProductSyncWorker>();
```

### Interview talking points

- A background service is singleton, so create scopes before using scoped dependencies.
- Honor cancellation tokens for graceful shutdown.
- For durable work queues, consider Hangfire, Quartz.NET, Azure Queue Storage, RabbitMQ, or Kafka.

## 13. API versioning

APIs need versioning when clients cannot all upgrade at the same time.

Popular strategies:

- URL path: `/api/v1/products`
- Query string: `/api/products?api-version=1.0`
- Header: `X-Api-Version: 1.0`
- Media type: `Accept: application/json;v=1`

### Example with URL versioning

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsV1Controller : ControllerBase
{
    [HttpGet("{id:int}")]
    public ActionResult<ProductResponse> GetById(int id)
    {
        return Ok(new ProductResponse(id, "Keyboard", 99.00m, "Accessories", true));
    }
}
```

### Interview talking points

- Version only when contract changes are breaking.
- Prefer additive changes when possible.
- Document supported and deprecated versions.
- Versioning strategy should match client needs and gateway conventions.

## 14. Intermediate interview drill

Practice answering these questions:

- Why is `DbContext` scoped?
- When should you use `Include` versus projection?
- What problems do DTOs solve?
- Is a repository always necessary with EF Core?
- What JWT validation parameters matter most?
- What is the difference between authentication and authorization?
- Why can CORS fail in a browser even when the API works from Postman?
- Middleware or filter: which one should handle exceptions?
- What are the risks of caching data?
- How does a `BackgroundService` use scoped services safely?
- What is an API breaking change?

