using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace SecurityExample.Authorization;

public static class Permissions
{
    public const string ProductsRead = "Products.Read";
    public const string ProductsWrite = "Products.Write";
    public const string ProductsDelete = "Products.Delete";
    public const string OrdersRead = "Orders.Read";
    public const string OrdersManage = "Orders.Manage";
    public const string UsersManage = "Users.Manage";

    public static readonly string[] All =
    [
        ProductsRead,
        ProductsWrite,
        ProductsDelete,
        OrdersRead,
        OrdersManage,
        UsersManage
    ];
}

public static class PolicyNames
{
    public const string ProductsRead = "Products.Read";
    public const string ProductsWrite = "Products.Write";
    public const string ProductsDelete = "Products.Delete";
    public const string OrdersRead = "Orders.Read";
    public const string OrdersManage = "Orders.Manage";
    public const string UsersManage = "Users.Manage";
    public const string SameTenantOrder = "Orders.SameTenant";
}

public static class PermissionPolicyRegistration
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(PolicyNames.ProductsRead, policy =>
                policy.RequirePermission(Permissions.ProductsRead));

            options.AddPolicy(PolicyNames.ProductsWrite, policy =>
                policy.RequirePermission(Permissions.ProductsWrite));

            options.AddPolicy(PolicyNames.ProductsDelete, policy =>
                policy.RequirePermission(Permissions.ProductsDelete));

            options.AddPolicy(PolicyNames.OrdersRead, policy =>
                policy.RequirePermission(Permissions.OrdersRead));

            options.AddPolicy(PolicyNames.OrdersManage, policy =>
                policy.RequirePermission(Permissions.OrdersManage));

            options.AddPolicy(PolicyNames.UsersManage, policy =>
                policy.RequirePermission(Permissions.UsersManage));

            options.AddPolicy(PolicyNames.SameTenantOrder, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new SameTenantOrderRequirement());
            });
        });

        services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
        services.AddSingleton<IAuthorizationHandler, SameTenantOrderHandler>();
        return services;
    }

    private static AuthorizationPolicyBuilder RequirePermission(
        this AuthorizationPolicyBuilder builder,
        string permission)
    {
        builder.RequireAuthenticatedUser();
        builder.Requirements.Add(new PermissionRequirement(permission));
        return builder;
    }
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (HasPermission(context.User, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user.Claims.Any(claim =>
            claim.Type == "permission" &&
            string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record SameTenantOrderRequirement : IAuthorizationRequirement;

public sealed class SameTenantOrderHandler : AuthorizationHandler<SameTenantOrderRequirement, OrderAuthorizationResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantOrderRequirement requirement,
        OrderAuthorizationResource resource)
    {
        var userTenant = context.User.FindFirstValue("tenant_id");
        var canManageOrders = context.User.Claims.Any(claim =>
            claim.Type == "permission" &&
            claim.Value == Permissions.OrdersManage);

        if (canManageOrders && userTenant == resource.TenantId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed record OrderAuthorizationResource(Guid OrderId, string TenantId, string CustomerUserId);

public static class AuthorizationEndpointExamples
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}", async (
            Guid id,
            IOrderLookup orders,
            IAuthorizationService authorization,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var order = await orders.GetAuthorizationResourceAsync(id, cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            var result = await authorization.AuthorizeAsync(user, order, PolicyNames.SameTenantOrder);
            if (!result.Succeeded)
            {
                return Results.Forbid();
            }

            var detail = await orders.GetDetailAsync(id, cancellationToken);
            return Results.Ok(detail);
        })
        .RequireAuthorization(PolicyNames.OrdersRead);

        return group;
    }
}

public interface IOrderLookup
{
    Task<OrderAuthorizationResource?> GetAuthorizationResourceAsync(Guid orderId, CancellationToken cancellationToken);
    Task<object?> GetDetailAsync(Guid orderId, CancellationToken cancellationToken);
}
