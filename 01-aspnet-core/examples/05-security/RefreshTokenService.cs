using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SecurityExample.Auth;

public sealed class RefreshTokenService
{
    private readonly AuthDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        AuthDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        ILogger<RefreshTokenService> logger)
    {
        _db = db;
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<TokenPair> CreateTokenPairAsync(
        ApplicationUser user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var accessToken = await CreateAccessTokenAsync(user, cancellationToken);
        var refreshToken = CreateRefreshToken(user.Id, ipAddress, userAgent);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new TokenPair(accessToken, refreshToken.PlainTextToken, refreshToken.ExpiresAt);
    }

    public async Task<RefreshResult> RefreshAsync(
        string plainTextRefreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(plainTextRefreshToken);
        var now = _timeProvider.GetUtcNow();

        var existing = await _db.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (existing is null)
        {
            _logger.LogWarning("Unknown refresh token used from {IpAddress}", ipAddress);
            return RefreshResult.Invalid("Refresh token is invalid.");
        }

        if (existing.RevokedAt is not null)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoking token family {FamilyId}",
                existing.UserId,
                existing.FamilyId);

            await RevokeFamilyAsync(existing.FamilyId, "reuse detected", cancellationToken);
            return RefreshResult.Invalid("Refresh token reuse detected. Sign in again.");
        }

        if (existing.ExpiresAt <= now)
        {
            existing.Revoke(now, "expired");
            await _db.SaveChangesAsync(cancellationToken);
            return RefreshResult.Invalid("Refresh token expired.");
        }

        var replacement = CreateRefreshToken(existing.UserId, ipAddress, userAgent, existing.FamilyId);
        existing.Replace(now, replacement.Id, ipAddress);

        _db.RefreshTokens.Add(replacement);
        await _db.SaveChangesAsync(cancellationToken);

        var accessToken = await CreateAccessTokenAsync(existing.User, cancellationToken);
        return RefreshResult.Success(new TokenPair(accessToken, replacement.PlainTextToken, replacement.ExpiresAt));
    }

    public async Task RevokeAsync(
        string plainTextRefreshToken,
        string reason,
        CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(plainTextRefreshToken);
        var token = await _db.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);

        if (token is null || token.RevokedAt is not null)
        {
            return;
        }

        token.Revoke(_timeProvider.GetUtcNow(), reason);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        await _db.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.RevocationReason, reason),
                cancellationToken);
    }

    private RefreshToken CreateRefreshToken(
        Guid userId,
        string? ipAddress,
        string? userAgent,
        Guid? familyId = null)
    {
        var plainTextToken = CreateSecureRandomToken();
        var now = _timeProvider.GetUtcNow();

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId ?? Guid.NewGuid(),
            TokenHash = HashToken(plainTextToken),
            PlainTextToken = plainTextToken,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new("tenant_id", user.TenantId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(user.Permissions.Select(permission => new Claim("permission", permission)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = _timeProvider.GetUtcNow().AddMinutes(_jwtOptions.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: _timeProvider.GetUtcNow().UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateSecureRandomToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.UserAgent).HasMaxLength(512);
            entity.Property(token => token.RevocationReason).HasMaxLength(200);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => new { token.UserId, token.FamilyId });
        });
    }
}

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    [NotMapped]
    public string PlainTextToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? ReplacedByIp { get; set; }
    public string? UserAgent { get; set; }

    public void Replace(DateTimeOffset now, Guid replacementTokenId, string? ipAddress)
    {
        RevokedAt = now;
        RevocationReason = "rotated";
        ReplacedByTokenId = replacementTokenId;
        ReplacedByIp = ipAddress;
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        RevokedAt = now;
        RevocationReason = reason;
    }
}

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string TenantId { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;
}

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt);

public sealed record RefreshResult(bool Succeeded, TokenPair? TokenPair, string? Error)
{
    public static RefreshResult Success(TokenPair tokenPair) => new(true, tokenPair, null);
    public static RefreshResult Invalid(string error) => new(false, null, error);
}
