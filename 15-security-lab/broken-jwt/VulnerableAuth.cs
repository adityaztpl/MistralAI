// DO NOT USE IN PROD.
// Intentionally vulnerable teaching sample for 15-security-lab/01-broken-jwt.md.
// Problem: this code decodes JWT claims and treats them as authenticated identity.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SecurityLab.BrokenJwt;

public static class VulnerableAuth
{
    public static ClaimsPrincipal? BuildPrincipalFromHeader(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer "))
        {
            return null;
        }

        var rawToken = header["Bearer ".Length..].Trim();
        var handler = new JwtSecurityTokenHandler();

        // VULNERABLE: ReadJwtToken only parses the token. It does not validate
        // the signature, issuer, audience, lifetime, or algorithm.
        var token = handler.ReadJwtToken(rawToken);

        var identity = new ClaimsIdentity(token.Claims, authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }

    public static string? GetUserId(HttpRequest request)
    {
        var principal = BuildPrincipalFromHeader(request);
        return principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static bool IsAdmin(HttpRequest request)
    {
        var principal = BuildPrincipalFromHeader(request);

        // VULNERABLE: this role value came from unvalidated attacker-controlled
        // token data. A role check is only meaningful after authentication.
        return principal?.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, "Admin", StringComparison.OrdinalIgnoreCase)) == true;
    }
}

[ApiController]
[Route("api/vulnerable/admin")]
public sealed class VulnerableAdminController : ControllerBase
{
    [HttpGet("report")]
    public IActionResult GetAdminReport()
    {
        if (!VulnerableAuth.IsAdmin(Request))
        {
            return Unauthorized();
        }

        return Ok(new
        {
            Message = "Sensitive admin report",
            Warning = "This endpoint trusted an unvalidated JWT claim."
        });
    }
}

