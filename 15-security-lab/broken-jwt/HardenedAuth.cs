// Hardened teaching sample for 15-security-lab/01-broken-jwt.md.
// This snippet shows the shape of production JWT validation in ASP.NET Core.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SecurityLab.BrokenJwt;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public required string Authority { get; init; }
    public required string Audience { get; init; }
    public required string Issuer { get; init; }
    public string[] AllowedAlgorithms { get; init; } = ["RS256"];
}

public static class HardenedAuth
{
    public static IServiceCollection AddHardenedJwtAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        var authOptions = configuration
            .GetSection(AuthOptions.SectionName)
            .Get<AuthOptions>()
            ?? throw new InvalidOperationException("Missing Auth configuration.");

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authOptions.Authority;
                options.Audience = authOptions.Audience;
                options.RequireHttpsMetadata = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuer = authOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var token = context.SecurityToken as JwtSecurityToken;
                        var configured = context.HttpContext.RequestServices
                            .GetRequiredService<IOptions<AuthOptions>>()
                            .Value;

                        if (token is null ||
                            !configured.AllowedAlgorithms.Contains(
                                token.Header.Alg,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            context.Fail("Unexpected token signing algorithm.");
                            return Task.CompletedTask;
                        }

                        var principal = context.Principal;
                        if (principal?.FindFirstValue(JwtRegisteredClaimNames.Sub) is null ||
                            principal.FindFirstValue("tenant_id") is null)
                        {
                            context.Fail("Required identity claims are missing.");
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Log exception type/correlation id in real apps, not raw tokens.
                        context.NoResult();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser().RequireRole("Admin"));

            options.AddPolicy("TenantMember", policy =>
                policy.RequireAuthenticatedUser().RequireClaim("tenant_id"));
        });

        return services;
    }
}

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class HardenedAdminController : ControllerBase
{
    [HttpGet("report")]
    public IActionResult GetAdminReport()
    {
        return Ok(new
        {
            Message = "Sensitive admin report",
            Subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            Tenant = User.FindFirstValue("tenant_id")
        });
    }
}

