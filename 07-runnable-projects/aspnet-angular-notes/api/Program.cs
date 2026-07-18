using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var settings = NotesSettings.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(settings.PostgresConnection));
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotesRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = settings.JwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = settings.SigningKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

await Schema.EnsureCreatedAsync(app.Services.GetRequiredService<NpgsqlDataSource>());

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "aspnet-angular-notes",
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/auth/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
{
    var result = await auth.RegisterAsync(request, ct);
    return Results.Created("/auth/me", result);
});

app.MapPost("/auth/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
{
    var result = await auth.LoginAsync(request, ct);
    IResult response = result is null ? Results.Unauthorized() : Results.Ok(result);
    return response;
});

var notes = app.MapGroup("/api/notes").RequireAuthorization();

notes.MapGet("/", async (ClaimsPrincipal user, string? query, NotesRepository repository, CancellationToken ct) =>
{
    var userId = RequiredUserId(user);
    var items = await repository.ListAsync(userId, query, ct);
    return Results.Ok(items);
});

notes.MapPost("/", async (ClaimsPrincipal user, NoteInput input, NotesRepository repository, CancellationToken ct) =>
{
    var userId = RequiredUserId(user);
    var note = await repository.CreateAsync(userId, input.Validated(), ct);
    return Results.Created($"/api/notes/{note.Id}", note);
});

notes.MapPut("/{id:guid}", async (
    ClaimsPrincipal user,
    Guid id,
    NoteInput input,
    NotesRepository repository,
    CancellationToken ct) =>
{
    var userId = RequiredUserId(user);
    var note = await repository.UpdateAsync(userId, id, input.Validated(), ct);
    IResult response = note is null ? Results.NotFound() : Results.Ok(note);
    return response;
});

notes.MapDelete("/{id:guid}", async (
    ClaimsPrincipal user,
    Guid id,
    NotesRepository repository,
    CancellationToken ct) =>
{
    var deleted = await repository.DeleteAsync(RequiredUserId(user), id, ct);
    IResult response = deleted ? Results.NoContent() : Results.NotFound();
    return response;
});

app.Run();

static Guid RequiredUserId(ClaimsPrincipal user)
{
    var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(value, out var userId)
        ? userId
        : throw new UnauthorizedAccessException("Token is missing a valid subject.");
}

public sealed record NotesSettings(
    string PostgresConnection,
    string JwtIssuer,
    string JwtAudience,
    SymmetricSecurityKey SigningKey)
{
    public static NotesSettings FromConfiguration(IConfiguration configuration)
    {
        var connection = configuration["POSTGRES_CONNECTION"]
            ?? "Host=localhost;Port=5433;Database=notes;Username=notes;Password=notes";
        var issuer = configuration["JWT_ISSUER"] ?? "notes-api";
        var audience = configuration["JWT_AUDIENCE"] ?? "notes-web";
        var signingKey = configuration["JWT_SIGNING_KEY"]
            ?? "local-development-signing-key-change-me-32chars";

        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT_SIGNING_KEY must be at least 32 characters.");
        }

        return new NotesSettings(
            connection,
            issuer,
            audience,
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)));
    }
}

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, UserDto User);
public sealed record UserDto(Guid Id, string Email);
public sealed record NoteDto(Guid Id, string Title, string Body, bool IsPinned, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record NoteInput(string Title, string Body, bool IsPinned)
{
    public NoteInput Validated()
    {
        var title = (Title ?? string.Empty).Trim();
        var body = (Body ?? string.Empty).Trim();
        if (title.Length is < 1 or > 160)
        {
            throw new BadHttpRequestException("Title is required and must be 160 characters or less.");
        }

        if (body.Length > 10_000)
        {
            throw new BadHttpRequestException("Body must be 10,000 characters or less.");
        }

        return this with { Title = title, Body = body };
    }
}

public sealed class AuthService(
    NpgsqlDataSource dataSource,
    PasswordHasher hasher,
    NotesSettings settings)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (request.Password is null || request.Password.Length < 8)
        {
            throw new BadHttpRequestException("Password must be at least 8 characters.");
        }

        var userId = Guid.NewGuid();
        var passwordHash = hasher.Hash(request.Password);

        await using var command = dataSource.CreateCommand("""
            insert into users (id, email, password_hash)
            values (@id, @email, @password_hash);
            """);
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("password_hash", passwordHash);

        try
        {
            await command.ExecuteNonQueryAsync(ct);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new BadHttpRequestException("Email is already registered.");
        }

        return IssueToken(new UserDto(userId, email));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        await using var command = dataSource.CreateCommand("""
            select id, email, password_hash
            from users
            where email = @email;
            """);
        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return null;
        }

        var user = new UserDto(reader.GetGuid(0), reader.GetString(1));
        var passwordHash = reader.GetString(2);
        return hasher.Verify(request.Password, passwordHash) ? IssueToken(user) : null;
    }

    private AuthResponse IssueToken(UserDto user)
    {
        var expires = DateTimeOffset.UtcNow.AddHours(2);
        var credentials = new SigningCredentials(settings.SigningKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: settings.JwtIssuer,
            audience: settings.JwtAudience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires, user);
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length < 3 || !normalized.Contains('@'))
        {
            throw new BadHttpRequestException("A valid email is required.");
        }

        return normalized;
    }
}

public sealed class NotesRepository(NpgsqlDataSource dataSource)
{
    public async Task<IReadOnlyList<NoteDto>> ListAsync(Guid userId, string? query, CancellationToken ct)
    {
        var sql = """
            select id, title, body, is_pinned, created_at, updated_at
            from notes
            where user_id = @user_id
            """;

        await using var command = dataSource.CreateCommand();
        command.Parameters.AddWithValue("user_id", userId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            sql += " and (title ilike @query or body ilike @query)";
            command.Parameters.AddWithValue("query", $"%{query.Trim()}%");
        }

        sql += " order by is_pinned desc, updated_at desc;";
        command.CommandText = sql;

        var notes = new List<NoteDto>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            notes.Add(ReadNote(reader));
        }

        return notes;
    }

    public async Task<NoteDto> CreateAsync(Guid userId, NoteInput input, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            insert into notes (user_id, title, body, is_pinned)
            values (@user_id, @title, @body, @is_pinned)
            returning id, title, body, is_pinned, created_at, updated_at;
            """);
        command.Parameters.AddWithValue("user_id", userId);
        AddNoteParameters(command, input);

        await using var reader = await command.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);
        return ReadNote(reader);
    }

    public async Task<NoteDto?> UpdateAsync(Guid userId, Guid id, NoteInput input, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            update notes
            set title = @title,
                body = @body,
                is_pinned = @is_pinned,
                updated_at = now()
            where id = @id and user_id = @user_id
            returning id, title, body, is_pinned, created_at, updated_at;
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("user_id", userId);
        AddNoteParameters(command, input);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadNote(reader) : null;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand("""
            delete from notes
            where id = @id and user_id = @user_id;
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("user_id", userId);
        return await command.ExecuteNonQueryAsync(ct) == 1;
    }

    private static void AddNoteParameters(NpgsqlCommand command, NoteInput input)
    {
        command.Parameters.AddWithValue("title", input.Title);
        command.Parameters.AddWithValue("body", input.Body);
        command.Parameters.AddWithValue("is_pinned", input.IsPinned);
    }

    private static NoteDto ReadNote(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetBoolean(3),
        reader.GetFieldValue<DateTimeOffset>(4),
        reader.GetFieldValue<DateTimeOffset>(5));
}

public sealed class PasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 210_000;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashBytes);

        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encoded)
    {
        var parts = encoded.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256" || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

public static class Schema
{
    public static async Task EnsureCreatedAsync(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand("""
            create extension if not exists pgcrypto;

            create table if not exists users (
                id uuid primary key,
                email text not null unique,
                password_hash text not null,
                created_at timestamptz not null default now()
            );

            create table if not exists notes (
                id uuid primary key default gen_random_uuid(),
                user_id uuid not null references users(id) on delete cascade,
                title text not null,
                body text not null default '',
                is_pinned boolean not null default false,
                created_at timestamptz not null default now(),
                updated_at timestamptz not null default now(),
                constraint notes_title_not_blank check (length(trim(title)) > 0),
                constraint notes_title_length check (length(title) <= 160),
                constraint notes_body_length check (length(body) <= 10000)
            );

            create index if not exists ix_notes_user_updated
                on notes (user_id, is_pinned desc, updated_at desc);

            create index if not exists ix_notes_user_title
                on notes (user_id, lower(title));
            """);
        await command.ExecuteNonQueryAsync();
    }
}

