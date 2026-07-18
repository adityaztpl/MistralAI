using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfCoreDeepDiveExample.Orders;

public sealed class OrderRepository
{
    private readonly OrdersDbContext _db;
    private readonly TimeProvider _timeProvider;

    public OrderRepository(OrdersDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<Order?> GetAggregateForUpdateAsync(
        Guid orderId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        return await _db.Orders
            .Include(order => order.Items)
            .SingleOrDefaultAsync(
                order => order.Id == orderId && order.TenantId == tenantId,
                cancellationToken);
    }

    public async Task<OrderDetail?> GetDetailAsync(
        Guid orderId,
        string tenantId,
        CancellationToken cancellationToken)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId && order.TenantId == tenantId)
            .Select(order => new OrderDetail(
                order.Id,
                order.OrderNumber,
                order.CustomerEmail,
                order.Status,
                order.CreatedAt,
                order.Items
                    .OrderBy(item => item.LineNumber)
                    .Select(item => new OrderLineDetail(
                        item.ProductId,
                        item.Sku,
                        item.ProductName,
                        item.Quantity,
                        item.UnitPrice))
                    .ToList(),
                order.Items.Sum(item => item.Quantity * item.UnitPrice),
                Convert.ToBase64String(order.RowVersion)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<OrderListItem>> SearchAsync(
        OrderSearchRequest request,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Orders
            .TagWith("Orders.Search")
            .AsNoTracking()
            .Where(order => order.TenantId == tenantId);

        if (request.Status is not null)
        {
            query = query.Where(order => order.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            query = query.Where(order => order.CustomerEmail == request.CustomerEmail);
        }

        if (request.CreatedAfter is not null)
        {
            query = query.Where(order => order.CreatedAt >= request.CreatedAfter.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderListItem(
                order.Id,
                order.OrderNumber,
                order.CustomerEmail,
                order.Status,
                order.CreatedAt,
                order.Items.Sum(item => item.Quantity * item.UnitPrice)))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderListItem>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<OrderListItem>> GetNextPageByCursorAsync(
        string tenantId,
        DateTimeOffset? cursorCreatedAt,
        Guid? cursorId, // Included so callers can evolve this to provider-specific tie-breaker SQL.
        int pageSize,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(pageSize, 1, 100);

        var query = _db.Orders
            .AsNoTracking()
            .Where(order => order.TenantId == tenantId);

        if (cursorCreatedAt is not null)
        {
            query = query.Where(order => order.CreatedAt < cursorCreatedAt.Value);
        }

        return await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Take(take)
            .Select(order => new OrderListItem(
                order.Id,
                order.OrderNumber,
                order.CustomerEmail,
                order.Status,
                order.CreatedAt,
                order.Items.Sum(item => item.Quantity * item.UnitPrice)))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        await _db.Orders.AddAsync(order, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        return await _db.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task MarkExpiredOrdersAsync(CancellationToken cancellationToken)
    {
        var cutoff = _timeProvider.GetUtcNow().AddMinutes(-30);

        await _db.Orders
            .Where(order => order.Status == OrderStatus.Pending && order.CreatedAt < cutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(order => order.Status, OrderStatus.Expired)
                .SetProperty(order => order.UpdatedAt, _timeProvider.GetUtcNow()),
                cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlySalesSummary>> GetMonthlySalesAsync(
        string tenantId,
        int year,
        CancellationToken cancellationToken)
    {
        return await _db.MonthlySalesSummaries
            .FromSqlInterpolated($"""
                SELECT
                    DATEPART(year, CreatedAt) AS [Year],
                    DATEPART(month, CreatedAt) AS [Month],
                    SUM(Total) AS [Total]
                FROM Orders
                WHERE TenantId = {tenantId}
                  AND DATEPART(year, CreatedAt) = {year}
                  AND Status = {(int)OrderStatus.Paid}
                GROUP BY DATEPART(year, CreatedAt), DATEPART(month, CreatedAt)
                ORDER BY [Year], [Month]
                """)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}

public sealed class OrdersDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public OrdersDbContext(DbContextOptions<OrdersDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<MonthlySalesSummary> MonthlySalesSummaries => Set<MonthlySalesSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
            entity.Property(order => order.CustomerEmail).HasMaxLength(320).IsRequired();
            entity.Property(order => order.TenantId).HasMaxLength(64).IsRequired();
            entity.Property(order => order.RowVersion).IsRowVersion();
            entity.Property(order => order.Status).HasConversion<int>();
            entity.HasQueryFilter(order => order.TenantId == _tenantContext.TenantId);
            entity.HasIndex(order => new { order.TenantId, order.OrderNumber }).IsUnique();
            entity.HasIndex(order => new { order.TenantId, order.Status, order.CreatedAt });
            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Sku).HasMaxLength(64).IsRequired();
            entity.Property(item => item.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(item => new { item.OrderId, item.LineNumber }).IsUnique();
        });

        modelBuilder.Entity<MonthlySalesSummary>(entity =>
        {
            entity.HasNoKey();
            entity.Property(summary => summary.Total).HasPrecision(18, 2);
        });
    }
}

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TenantId { get; private set; } = string.Empty;
    public string OrderNumber { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<OrderItem> Items => _items;

    public static Order Create(string tenantId, string orderNumber, string customerEmail, DateTimeOffset createdAt)
    {
        return new Order
        {
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim(),
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            CreatedAt = createdAt
        };
    }

    public void AddItem(int lineNumber, Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        }

        if (unitPrice <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be positive.");
        }

        _items.Add(new OrderItem(Id, lineNumber, productId, sku, productName, quantity, unitPrice));
    }
}

public sealed class OrderItem
{
    private OrderItem()
    {
    }

    public OrderItem(Guid orderId, int lineNumber, Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        OrderId = orderId;
        LineNumber = lineNumber;
        ProductId = productId;
        Sku = sku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public int LineNumber { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}

public interface ITenantContext
{
    string TenantId { get; }
}

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Shipped = 2,
    Cancelled = 3,
    Expired = 4
}

public sealed record OrderSearchRequest(
    OrderStatus? Status,
    string? CustomerEmail,
    DateTimeOffset? CreatedAfter,
    int Page = 1,
    int PageSize = 25);

public sealed record OrderListItem(
    Guid Id,
    string OrderNumber,
    string CustomerEmail,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    decimal Total);

public sealed record OrderDetail(
    Guid Id,
    string OrderNumber,
    string CustomerEmail,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDetail> Items,
    decimal Total,
    string RowVersion);

public sealed record OrderLineDetail(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record MonthlySalesSummary(int Year, int Month, decimal Total);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
