using System.ComponentModel.DataAnnotations;
using IntermediateExample.Auth;
using IntermediateExample.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntermediateExample.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.ProductsRead)]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext db, ILogger<ProductsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<ProductListItemResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductListItemResponse>>> List(
        [FromQuery] ProductQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var products = _db.Products
            .AsNoTracking()
            .Where(product => query.IncludeInactive || product.IsActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.Where(product =>
                product.Name.Contains(query.Search) ||
                (product.Description != null && product.Description.Contains(query.Search)));
        }

        if (query.CategoryId is not null)
        {
            products = products.Where(product => product.CategoryId == query.CategoryId);
        }

        var total = await products.CountAsync(cancellationToken);

        var items = await products
            .OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductListItemResponse(
                product.Id,
                product.Name,
                product.Price,
                product.Category.Name,
                product.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<ProductListItemResponse>(items, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductDetailResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.CategoryId,
                product.Category.Name,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt,
                Convert.ToBase64String(product.RowVersion)))
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [Authorize(Policy = AuthPolicies.ProductsWrite)]
    [HttpPost]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDetailResponse>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Category does not exist.");
            return ValidationProblem(ModelState);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            IsActive = true
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} created", product.Id);

        var response = await GetProductDetailAsync(product.Id, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    [Authorize(Policy = AuthPolicies.ProductsWrite)]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDetailResponse>> Update(
        int id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(
            product => product.Id == id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var categoryExists = await _db.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            ModelState.AddModelError(nameof(request.CategoryId), "Category does not exist.");
            return ValidationProblem(ModelState);
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(nameof(request.RowVersion), "RowVersion must be a base64 string.");
            return ValidationProblem(ModelState);
        }

        _db.Entry(product)
            .Property(existing => existing.RowVersion)
            .OriginalValue = rowVersion;

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        product.IsActive = request.IsActive;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Product was modified by another request.",
                Detail = "Reload the product and retry the update with the latest row version.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var response = await GetProductDetailAsync(product.Id, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = AuthPolicies.ProductsWrite)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(
            product => product.Id == id,
            cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product {ProductId} deleted", id);
        return NoContent();
    }

    private async Task<ProductDetailResponse> GetProductDetailAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductDetailResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.CategoryId,
                product.Category.Name,
                product.IsActive,
                product.CreatedAt,
                product.UpdatedAt,
                Convert.ToBase64String(product.RowVersion)))
            .SingleAsync(cancellationToken);
    }
}

public sealed record ProductQuery(
    string? Search,
    int? CategoryId,
    bool IncludeInactive = false,
    int Page = 1,
    int PageSize = 25);

public sealed record CreateProductRequest(
    [Required]
    [StringLength(200, MinimumLength = 2)]
    string Name,

    [StringLength(2_000)]
    string? Description,

    [Range(0.01, 999_999)]
    decimal Price,

    [Range(1, int.MaxValue)]
    int CategoryId);

public sealed record UpdateProductRequest(
    [Required]
    [StringLength(200, MinimumLength = 2)]
    string Name,

    [StringLength(2_000)]
    string? Description,

    [Range(0.01, 999_999)]
    decimal Price,

    [Range(1, int.MaxValue)]
    int CategoryId,

    bool IsActive,

    [Required]
    string RowVersion);

public sealed record ProductListItemResponse(
    int Id,
    string Name,
    decimal Price,
    string CategoryName,
    bool IsActive);

public sealed record ProductDetailResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int CategoryId,
    string CategoryName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string RowVersion);

public sealed record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

