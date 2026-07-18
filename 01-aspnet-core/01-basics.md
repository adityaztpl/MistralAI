# ASP.NET Core Basics

This guide covers the foundation of modern ASP.NET Core API development. The goal is to be able to explain the framework clearly in interviews and write small production-shaped APIs using .NET 8/9 style hosting, dependency injection, middleware, minimal APIs, and controllers.

## 1. C# essentials for ASP.NET Core

ASP.NET Core code is ordinary C#, so interviewers often expect you to be comfortable with the language features used in web APIs.

### Key concepts

- **Types and nullability**: ASP.NET Core projects usually enable nullable reference types. Use `string?` when a value can be missing and validate user input explicitly.
- **Records**: Great for request/response DTOs because they are concise and immutable by default.
- **Async/await**: Almost all I/O should be asynchronous: database calls, HTTP calls, file access, and cache calls.
- **LINQ**: Common for projections and filtering, especially with EF Core.
- **Attributes**: Controllers use attributes heavily for routing, validation, authorization, and OpenAPI metadata.
- **Generics**: Dependency injection, logging, options, repositories, and EF Core all use generics.
- **Extension methods**: ASP.NET Core configuration is built around methods such as `AddControllers`, `UseAuthentication`, and `MapGet`.

### Example: DTOs, records, nullability, async

```csharp
public sealed record CreateTodoRequest(string Title, DateOnly? DueDate);

public sealed record TodoResponse(
    int Id,
    string Title,
    bool IsDone,
    DateOnly? DueDate);

public interface ITodoService
{
    Task<TodoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken);
}
```

### Interview talking points

- `async` avoids blocking request threads while waiting for I/O.
- Records are useful for API contracts, but entities often remain classes because ORMs need identity and change tracking.
- Nullable reference types help you make missing data explicit, but they do not replace runtime validation.

## 2. Project structure

A small ASP.NET Core API may start with a single project:

```text
MyApi/
  Program.cs
  appsettings.json
  appsettings.Development.json
  Controllers/
    ProductsController.cs
  Models/
    Product.cs
  Services/
    ProductService.cs
  MyApi.csproj
```

As the application grows, common folders include:

- `Controllers/`: attribute-routed MVC API controllers.
- `Endpoints/`: minimal API endpoint groups.
- `Models/` or `Entities/`: domain/persistence types.
- `Dtos/` or `Contracts/`: request/response types exposed by the API.
- `Data/`: EF Core `DbContext`, migrations, seed data.
- `Services/`: application services, external integrations.
- `Options/`: configuration-bound classes.
- `Middleware/`: custom middleware.

### Interview talking points

- Keep API contracts separate from EF Core entities to avoid over-posting and accidental schema leakage.
- Folder structure is less important than dependency direction and clarity.
- For larger systems, split into projects such as `Api`, `Application`, `Domain`, and `Infrastructure`.

## 3. `Program.cs` and the minimal hosting model

.NET 6 introduced the minimal hosting model, which remains the standard in .NET 8/9. `Program.cs` configures services and the HTTP pipeline.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register services in the DI container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
```

### What happens at startup?

1. `WebApplication.CreateBuilder(args)` loads configuration, logging, dependency injection, and hosting defaults.
2. Services are registered on `builder.Services`.
3. `builder.Build()` creates the application and freezes service registration.
4. Middleware and endpoints are added to the pipeline.
5. `app.Run()` starts Kestrel and listens for HTTP requests.

### Interview talking points

- The service collection is mutable before `Build()` and effectively immutable afterward.
- Middleware order matters because each middleware wraps the next one.
- `app.Environment` reads the current environment, commonly `Development`, `Staging`, or `Production`.

## 4. Dependency injection

ASP.NET Core has a built-in dependency injection container. You register abstractions and implementations at startup, then request them through constructors, endpoint parameters, filters, middleware, or services.

### Lifetimes

| Lifetime | Created | Typical use |
| --- | --- | --- |
| Singleton | Once per application | stateless services, configuration, caches, clients designed for reuse |
| Scoped | Once per request scope | EF Core `DbContext`, per-request application services |
| Transient | Every resolution | lightweight stateless objects |

```csharp
builder.Services.AddSingleton<ISystemClock, SystemClock>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddTransient<IEmailFormatter, EmailFormatter>();
```

### Constructor injection

```csharp
public sealed class ProductService
{
    private readonly ILogger<ProductService> _logger;
    private readonly IProductRepository _products;

    public ProductService(ILogger<ProductService> logger, IProductRepository products)
    {
        _logger = logger;
        _products = products;
    }

    public async Task<Product?> FindAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading product {ProductId}", id);
        return await _products.GetByIdAsync(id, cancellationToken);
    }
}
```

### Minimal API injection

```csharp
app.MapGet("/products/{id:int}", async (
    int id,
    IProductService products,
    CancellationToken cancellationToken) =>
{
    var product = await products.FindAsync(id, cancellationToken);
    return product is null ? Results.NotFound() : Results.Ok(product);
});
```

### Interview talking points

- Do not inject scoped services into singleton services directly; that creates lifetime bugs.
- Prefer constructor injection for required dependencies.
- `ILogger<T>`, `IConfiguration`, `IOptions<T>`, and `DbContext` are commonly injected.

## 5. Middleware

Middleware is code that runs for every matching HTTP request. Each middleware can inspect, modify, short-circuit, or pass the request to the next middleware.

```csharp
app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    await next(context);
    var elapsed = Stopwatch.GetElapsedTime(started);
    app.Logger.LogInformation(
        "{Method} {Path} completed with {StatusCode} in {ElapsedMs} ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        elapsed.TotalMilliseconds);
});
```

### Common middleware order

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

### Interview talking points

- Middleware order is a common interview topic.
- Authentication identifies the user; authorization decides whether the user may access a resource.
- Exception handling should be early in the pipeline so it can catch downstream failures.

## 6. Routing

Routing maps HTTP requests to endpoints. ASP.NET Core supports route templates, constraints, route groups, and attribute routing.

### Minimal API routing

```csharp
var products = app.MapGroup("/api/products")
    .WithTags("Products");

products.MapGet("/", () => Results.Ok(Array.Empty<ProductResponse>()));

products.MapGet("/{id:int}", (int id) =>
    id <= 0
        ? Results.BadRequest("Id must be positive.")
        : Results.Ok(new ProductResponse(id, "Keyboard", 99.00m)));
```

### Controller routing

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet("{id:int}")]
    public ActionResult<ProductResponse> GetById(int id)
    {
        return Ok(new ProductResponse(id, "Keyboard", 99.00m));
    }
}
```

### Interview talking points

- Route constraints such as `{id:int}` prevent invalid endpoints from matching.
- Attribute routing is explicit and commonly used for APIs.
- Route groups help minimal APIs share prefixes, authorization, tags, and filters.

## 7. Controllers vs minimal APIs

Both approaches are first-class in modern ASP.NET Core.

### Controllers

Use controllers when you want:

- Familiar MVC structure.
- Attribute filters.
- Built-in model validation behavior with `[ApiController]`.
- Large APIs where grouping methods by resource is clearer.

```csharp
[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    public ActionResult<OrderResponse> Create(CreateOrderRequest request)
    {
        var response = new OrderResponse(123, request.CustomerEmail);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:int}")]
    public ActionResult<OrderResponse> GetById(int id) =>
        Ok(new OrderResponse(id, "customer@example.com"));
}
```

### Minimal APIs

Use minimal APIs when you want:

- Low ceremony for small services.
- Functional endpoint definitions.
- Fast startup and compact code.
- Easy route grouping for vertical slices.

```csharp
app.MapPost("/api/orders", (CreateOrderRequest request) =>
{
    var response = new OrderResponse(123, request.CustomerEmail);
    return Results.Created($"/api/orders/{response.Id}", response);
});
```

### Interview talking points

- Minimal APIs are not just for demos; they are production-ready.
- Controllers remain useful for large, attribute-heavy APIs.
- A single application can use both.

## 8. Model binding

Model binding maps incoming HTTP data to C# parameters or models.

### Common binding sources

- Route values: `/products/{id}`
- Query string: `/products?search=phone&page=2`
- Request body: JSON body for `POST` or `PUT`
- Headers: `X-Correlation-Id`
- Services: dependency injection

```csharp
app.MapGet("/api/products/{id:int}", (
    [FromRoute] int id,
    [FromQuery] string? include,
    [FromHeader(Name = "X-Correlation-Id")] string? correlationId,
    ILogger<Program> logger) =>
{
    logger.LogInformation("Correlation id: {CorrelationId}", correlationId);
    return Results.Ok(new { id, include });
});
```

Controller example:

```csharp
[HttpPost]
public ActionResult<ProductResponse> Create([FromBody] CreateProductRequest request)
{
    return Created("/api/products/1", new ProductResponse(1, request.Name, request.Price));
}
```

### Interview talking points

- Complex types usually bind from the body in API controllers.
- Only one parameter can normally bind from the request body.
- Minimal APIs infer binding sources from parameter type and attributes.

## 9. Validation

Validation protects your application from bad input. Basic validation can use data annotations; larger systems often use FluentValidation or endpoint filters.

### Data annotations

```csharp
public sealed class CreateProductRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 999_999)]
    public decimal Price { get; init; }
}
```

With `[ApiController]`, invalid controller models automatically produce HTTP 400 responses with `ProblemDetails`.

### Minimal API endpoint filter validation

```csharp
public sealed class CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

app.MapPost("/api/products", (CreateProductRequest request) =>
{
    return Results.Created("/api/products/1", request);
})
.AddEndpointFilter(async (context, next) =>
{
    var request = context.GetArgument<CreateProductRequest>(0);
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required."],
            ["Price"] = ["Price must be positive."]
        });
    }

    return await next(context);
});
```

### Interview talking points

- Validate external input at the boundary.
- Use DTO validation for syntax and shape; use domain rules inside application/domain services.
- Return consistent validation responses, usually `400 Bad Request` with `ProblemDetails`.

## 10. Configuration and `appsettings`

ASP.NET Core combines configuration from multiple providers.

Typical configuration order includes:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User secrets in development
4. Environment variables
5. Command-line arguments

```json
{
  "Catalog": {
    "DefaultPageSize": 25,
    "MaxPageSize": 100
  },
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=Catalog;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### Options pattern

```csharp
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    public int DefaultPageSize { get; init; } = 25;
    public int MaxPageSize { get; init; } = 100;
}

builder.Services
    .AddOptions<CatalogOptions>()
    .Bind(builder.Configuration.GetSection(CatalogOptions.SectionName))
    .Validate(options => options.DefaultPageSize > 0, "Default page size must be positive.")
    .ValidateOnStart();
```

Use it in a service:

```csharp
public sealed class ProductQueryService
{
    private readonly CatalogOptions _options;

    public ProductQueryService(IOptions<CatalogOptions> options)
    {
        _options = options.Value;
    }
}
```

### Interview talking points

- Environment variables override JSON configuration and are common in containers.
- Do not store production secrets in `appsettings.json`.
- The options pattern gives strongly typed, validated configuration.

## 11. Logging

ASP.NET Core logging is structured. Prefer message templates over string interpolation.

```csharp
public sealed class CheckoutService
{
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(ILogger<CheckoutService> logger)
    {
        _logger = logger;
    }

    public void CompleteOrder(int orderId, decimal total)
    {
        _logger.LogInformation(
            "Order {OrderId} completed with total {Total}",
            orderId,
            total);
    }
}
```

### Log levels

- `Trace`: very detailed diagnostics.
- `Debug`: development diagnostics.
- `Information`: normal application events.
- `Warning`: unexpected but recoverable events.
- `Error`: failed operation.
- `Critical`: application or system failure.

### Interview talking points

- Structured logs preserve fields such as `OrderId` for searching and dashboards.
- Avoid logging secrets, tokens, passwords, or personal data.
- Include correlation IDs for tracing requests across services.

## 12. HTTP and REST basics

ASP.NET Core APIs sit on top of HTTP.

### Common methods

| Method | Purpose | Idempotent |
| --- | --- | --- |
| GET | Read a resource | Yes |
| POST | Create or execute an action | No |
| PUT | Replace a resource | Yes |
| PATCH | Partially update a resource | Usually |
| DELETE | Delete a resource | Yes |

### Common status codes

| Status | Meaning |
| --- | --- |
| 200 OK | Successful read or update |
| 201 Created | Resource created; include `Location` header |
| 204 No Content | Successful action with no response body |
| 400 Bad Request | Invalid input |
| 401 Unauthorized | Missing or invalid authentication |
| 403 Forbidden | Authenticated but not allowed |
| 404 Not Found | Resource does not exist |
| 409 Conflict | State conflict, duplicate key, concurrency issue |
| 500 Internal Server Error | Unhandled server failure |

### RESTful CRUD shape

```csharp
app.MapGet("/api/products", () => Results.Ok(products));
app.MapGet("/api/products/{id:int}", (int id) => Results.Ok(product));
app.MapPost("/api/products", (CreateProductRequest request) => Results.Created("/api/products/42", product));
app.MapPut("/api/products/{id:int}", (int id, UpdateProductRequest request) => Results.NoContent());
app.MapDelete("/api/products/{id:int}", (int id) => Results.NoContent());
```

### Interview talking points

- REST focuses on resources and representations.
- `POST` is not idempotent by default; `PUT` should be idempotent.
- Use `ProblemDetails` for machine-readable error responses.

## 13. Swagger/OpenAPI

OpenAPI describes your API in a standard format. Swagger UI provides an interactive browser UI for development and testing.

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Catalog API",
        Version = "v1",
        Description = "Product catalog service"
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

Minimal API metadata:

```csharp
app.MapPost("/api/products", (CreateProductRequest request) =>
{
    var response = new ProductResponse(1, request.Name, request.Price);
    return Results.Created($"/api/products/{response.Id}", response);
})
.WithName("CreateProduct")
.WithTags("Products")
.Produces<ProductResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem();
```

### Interview talking points

- OpenAPI is useful for documentation, client generation, and contract review.
- Keep examples and response metadata accurate.
- Secure Swagger in production or disable it if not needed.

## 14. Basics interview drill

Practice answering these out loud:

- What is the difference between `builder.Services` and `app.Use...`?
- Why does middleware order matter?
- When would you choose minimal APIs over controllers?
- How does model binding know where values come from?
- What happens when `[ApiController]` validation fails?
- What is the difference between `401` and `403`?
- Why should services be async for database and network calls?
- How do configuration providers override each other?
- What should not be logged?
- What does Swagger/OpenAPI provide beyond a nice UI?

