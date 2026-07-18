using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace AdvancedExample.RateLimiting;

public static class RateLimitingSetup
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .Validate(options => options.AuthenticatedPermitLimit > 0,
                "Authenticated permit limit must be positive.")
            .Validate(options => options.AnonymousPermitLimit > 0,
                "Anonymous permit limit must be positive.")
            .ValidateOnStart();

        var limits = configuration
            .GetSection(RateLimitOptions.SectionName)
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfterValue)
                    ? retryAfterValue
                    : TimeSpan.FromSeconds(30);

                context.HttpContext.Response.Headers.RetryAfter =
                    Math.Ceiling(retryAfter.TotalSeconds)
                        .ToString(CultureInfo.InvariantCulture);

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    Type = "https://httpstatuses.com/429",
                    Title = "Too many requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "The client has sent too many requests. Retry after the indicated delay.",
                    RetryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds),
                    TraceId = context.HttpContext.TraceIdentifier
                }, cancellationToken);
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value
                        ?? httpContext.User.Identity.Name
                        ?? "authenticated";

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: $"user:{userId}",
                        factory: _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = limits.AuthenticatedPermitLimit,
                            TokensPerPeriod = limits.AuthenticatedTokensPerPeriod,
                            ReplenishmentPeriod = TimeSpan.FromSeconds(limits.ReplenishmentSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                }

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? IPAddress.None.ToString();

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"ip:{ipAddress}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AnonymousPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.AddPolicy(RateLimitPolicies.ExpensiveOperations, httpContext =>
            {
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? $"expensive:user:{httpContext.User.FindFirst("sub")?.Value ?? "unknown"}"
                    : $"expensive:ip:{httpContext.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = limits.ExpensivePermitLimit,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 5,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        return app.UseRateLimiter();
    }
}

public static class RateLimitPolicies
{
    public const string ExpensiveOperations = "ExpensiveOperations";
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int AnonymousPermitLimit { get; init; } = 60;
    public int AuthenticatedPermitLimit { get; init; } = 600;
    public int AuthenticatedTokensPerPeriod { get; init; } = 100;
    public int ExpensivePermitLimit { get; init; } = 10;
    public int ReplenishmentSeconds { get; init; } = 10;
}

/*
appsettings.json:

{
  "RateLimiting": {
    "AnonymousPermitLimit": 60,
    "AuthenticatedPermitLimit": 600,
    "AuthenticatedTokensPerPeriod": 100,
    "ExpensivePermitLimit": 10,
    "ReplenishmentSeconds": 10
  }
}

Program.cs:

builder.Services.AddApiRateLimiting(builder.Configuration);

app.UseApiRateLimiting();

app.MapPost("/api/reports/rebuild", RebuildReport)
    .RequireRateLimiting(RateLimitPolicies.ExpensiveOperations);
*/

