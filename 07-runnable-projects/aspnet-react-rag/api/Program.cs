using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector.Npgsql;

var builder = WebApplication.CreateBuilder(args);

var ragOptions = RagOptions.FromConfiguration(builder.Configuration);
builder.Services.AddSingleton(ragOptions);

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(DemoBearerHandler.Scheme)
    .AddScheme<AuthenticationSchemeOptions, DemoBearerHandler>(DemoBearerHandler.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder(DemoBearerHandler.Scheme)
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton(sp =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(ragOptions.PostgresConnection);
    dataSourceBuilder.UseVector();
    return dataSourceBuilder.Build();
});

builder.Services.AddHttpClient<OpenAiCompatibleClient>((sp, http) =>
{
    var options = sp.GetRequiredService<RagOptions>();
    http.BaseAddress = new Uri(options.LlmBaseUrl.TrimEnd('/') + "/");
    http.Timeout = TimeSpan.FromMinutes(3);
    http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
});

builder.Services.AddScoped<RagRepository>();
builder.Services.AddScoped<RagService>();

var app = builder.Build();

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", (RagOptions options) => Results.Ok(new
{
    status = "ok",
    service = "aspnet-react-rag",
    utc = DateTimeOffset.UtcNow,
    chatModel = options.ChatModel,
    embeddingModel = options.EmbeddingModel,
    embeddingDimensions = options.EmbeddingDimensions
}));

app.MapRagEndpoints();

app.Run();

public sealed record RagOptions(
    string ApiKey,
    string LlmBaseUrl,
    string ChatModel,
    string EmbeddingModel,
    int EmbeddingDimensions,
    string PostgresConnection,
    string DemoToken)
{
    public static RagOptions FromConfiguration(IConfiguration configuration)
    {
        string Required(string key)
        {
            var value = configuration[key] ?? Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{key} is required.");
            }

            return value;
        }

        return new RagOptions(
            ApiKey: Required("MISTRAL_API_KEY"),
            LlmBaseUrl: configuration["LLM_BASE_URL"] ?? "https://api.mistral.ai/v1",
            ChatModel: configuration["CHAT_MODEL"] ?? "mistral-small-latest",
            EmbeddingModel: configuration["EMBEDDING_MODEL"] ?? "mistral-embed",
            EmbeddingDimensions: int.TryParse(configuration["EMBEDDING_DIMENSIONS"], out var dimensions)
                ? dimensions
                : 1024,
            PostgresConnection: Required("POSTGRES_CONNECTION"),
            DemoToken: configuration["AUTH_DEMO_TOKEN"] ?? "dev-token");
    }
}

public sealed class DemoBearerHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    RagOptions ragOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Scheme = "DemoBearer";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = header["Bearer ".Length..].Trim();
        if (!TimeConstantEquals(token, ragOptions.DemoToken))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid bearer token."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "demo-user"),
            new Claim("tenant_id", "demo"),
            new Claim("scope", "rag:read rag:write")
        };
        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
    }

    private static bool TimeConstantEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }
}

