using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AdvancedExample.Products;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int CategoryId) : IRequest<ProductResponse>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(2_000);

        RuleFor(command => command.Price)
            .GreaterThan(0)
            .LessThanOrEqualTo(999_999);

        RuleFor(command => command.CategoryId)
            .GreaterThan(0);
    }
}

public sealed class CreateProductCommandHandler
    : IRequestHandler<CreateProductCommand, ProductResponse>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CreateProductCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<ProductResponse> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories
            .AsNoTracking()
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        var duplicateName = await _db.Products
            .AsNoTracking()
            .AnyAsync(product =>
                product.CategoryId == request.CategoryId &&
                product.Name == request.Name,
                cancellationToken);

        if (duplicateName)
        {
            throw new ConflictException(
                $"A product named '{request.Name}' already exists in this category.");
        }

        var product = Product.Create(
            name: request.Name,
            description: request.Description,
            price: request.Price,
            categoryId: request.CategoryId,
            createdByUserId: _currentUser.UserId,
            createdAt: _timeProvider.GetUtcNow());

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.CategoryId,
            product.CreatedAt);
    }
}

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validators = _validators.ToArray();
        if (validators.Length == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        if (failures.Count > 0)
        {
            throw new RequestValidationException(failures);
        }

        return await next();
    }
}

public static class CreateProductEndpoint
{
    public static RouteGroupBuilder MapProductCommandEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", async (
            CreateProductCommand command,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var response = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/products/{response.Id}", response);
        })
        .WithName("CreateProduct")
        .Produces<ProductResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireAuthorization("Products.Write");

        return group;
    }
}

public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ICurrentUser
{
    string UserId { get; }
}

public sealed class Product
{
    private Product()
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int CategoryId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;

    public static Product Create(
        string name,
        string? description,
        decimal price,
        int categoryId,
        string createdByUserId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");
        }

        return new Product
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = price,
            CategoryId = categoryId,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt
        };
    }
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int CategoryId,
    DateTimeOffset CreatedAt);

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} '{key}' was not found.")
    {
    }
}

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}

/*
Program.cs registration sketch:

builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<CreateProductCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<CreateProductCommandValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddSingleton(TimeProvider.System);

var products = app.MapGroup("/api/products");
products.MapProductCommandEndpoints();
*/

