// Teaching sketch for 16-capstone-instagram.
// Shows the API facade shape for creating, listing, and inspecting Instagram
// content-generation runs. Adapt namespaces, EF configuration, auth policies,
// and queue implementation to your real project.

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstagramCapstone.Api.ContentRuns;

public enum ContentRunStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

public sealed class ContentRun
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required string CreatedByUserId { get; init; }
    public required string BrandDescription { get; init; }
    public required string Topic { get; init; }
    public int NumberOfPosts { get; init; }
    public string? Tone { get; init; }
    public string? Audience { get; init; }
    public required string Model { get; init; }
    public required string PromptVersion { get; init; }
    public ContentRunStatus Status { get; set; }
    public string? OutputDirectory { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int? EstimatedCostCents { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public List<RunArtifact> Artifacts { get; init; } = [];
}

public sealed class RunArtifact
{
    public long Id { get; init; }
    public required string ContentRunId { get; init; }
    public required string Kind { get; init; }
    public required string Path { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AppDbContext : DbContext
{
    public DbSet<ContentRun> ContentRuns => Set<ContentRun>();
    public DbSet<RunArtifact> RunArtifacts => Set<RunArtifact>();
}

public sealed record CreateContentRunRequest(
    [Required, MinLength(20), MaxLength(2000)] string BrandDescription,
    [Required, MinLength(5), MaxLength(300)] string Topic,
    [Range(1, 14)] int NumberOfPosts,
    [MaxLength(200)] string? Tone,
    [MaxLength(300)] string? Audience,
    [MaxLength(100)] string? Model);

public sealed record ContentRunSummaryResponse(
    string Id,
    ContentRunStatus Status,
    string BrandDescription,
    string Topic,
    int NumberOfPosts,
    string Model,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    int? EstimatedCostCents);

public sealed record ContentRunDetailResponse(
    string Id,
    ContentRunStatus Status,
    string BrandDescription,
    string Topic,
    int NumberOfPosts,
    string? Tone,
    string? Audience,
    string Model,
    string PromptVersion,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<RunArtifactResponse> Artifacts);

public sealed record RunArtifactResponse(
    string Kind,
    string Path,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public sealed record CallerContext(string UserId, string TenantId);

public interface ICallerContextAccessor
{
    CallerContext GetRequiredCaller(ClaimsPrincipal user);
}

public sealed class ClaimsCallerContextAccessor : ICallerContextAccessor
{
    public CallerContext GetRequiredCaller(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing subject claim.");

        var tenantId = user.FindFirstValue("tenant_id")
            ?? throw new UnauthorizedAccessException("Missing tenant_id claim.");

        return new CallerContext(userId, tenantId);
    }
}

public interface IContentRunQueue
{
    ValueTask EnqueueAsync(string runId, CancellationToken cancellationToken);
}

public interface IRunBudgetService
{
    Task<bool> CanCreateRunAsync(
        CallerContext caller,
        int numberOfPosts,
        string model,
        CancellationToken cancellationToken);
}

[ApiController]
[Authorize]
[Route("api/content-runs")]
public sealed class ContentRunsController(
    AppDbContext db,
    ICallerContextAccessor callers,
    IContentRunQueue queue,
    IRunBudgetService budgets,
    ILogger<ContentRunsController> logger) : ControllerBase
{
    private static readonly HashSet<string> AllowedModels = new(StringComparer.OrdinalIgnoreCase)
    {
        "mistral-small-latest",
        "mistral-large-latest"
    };

    [HttpPost]
    [Authorize(Policy = "CanCreateRuns")]
    public async Task<ActionResult<ContentRunSummaryResponse>> CreateRun(
        CreateContentRunRequest request,
        CancellationToken ct)
    {
        var caller = callers.GetRequiredCaller(User);
        var model = string.IsNullOrWhiteSpace(request.Model)
            ? "mistral-large-latest"
            : request.Model.Trim();

        if (!AllowedModels.Contains(model))
        {
            ModelState.AddModelError(nameof(request.Model), "Model is not available.");
            return ValidationProblem(ModelState);
        }

        if (!await budgets.CanCreateRunAsync(caller, request.NumberOfPosts, model, ct))
        {
            return Problem(
                title: "Budget exceeded",
                detail: "This tenant has reached its content-generation budget.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        var run = new ContentRun
        {
            Id = $"run_{Guid.NewGuid():N}",
            TenantId = caller.TenantId,
            CreatedByUserId = caller.UserId,
            BrandDescription = request.BrandDescription.Trim(),
            Topic = request.Topic.Trim(),
            NumberOfPosts = request.NumberOfPosts,
            Tone = request.Tone?.Trim(),
            Audience = request.Audience?.Trim(),
            Model = model,
            PromptVersion = "instagram-crew-v1",
            Status = ContentRunStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ContentRuns.Add(run);
        await db.SaveChangesAsync(ct);
        await queue.EnqueueAsync(run.Id, ct);

        logger.LogInformation(
            "Created content run {RunId} for tenant {TenantId}",
            run.Id,
            caller.TenantId);

        return CreatedAtAction(
            nameof(GetRun),
            new { id = run.Id },
            ToSummary(run));
    }

    [HttpGet]
    [Authorize(Policy = "CanViewRuns")]
    public async Task<ActionResult<IReadOnlyList<ContentRunSummaryResponse>>> ListRuns(
        CancellationToken ct)
    {
        var caller = callers.GetRequiredCaller(User);

        var runs = await db.ContentRuns
            .AsNoTracking()
            .Where(run => run.TenantId == caller.TenantId)
            .OrderByDescending(run => run.CreatedAt)
            .Take(50)
            .Select(run => ToSummary(run))
            .ToListAsync(ct);

        return Ok(runs);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "CanViewRuns")]
    public async Task<ActionResult<ContentRunDetailResponse>> GetRun(
        string id,
        CancellationToken ct)
    {
        var caller = callers.GetRequiredCaller(User);

        var run = await db.ContentRuns
            .AsNoTracking()
            .Include(item => item.Artifacts)
            .Where(item => item.TenantId == caller.TenantId)
            .SingleOrDefaultAsync(item => item.Id == id, ct);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(new ContentRunDetailResponse(
            run.Id,
            run.Status,
            run.BrandDescription,
            run.Topic,
            run.NumberOfPosts,
            run.Tone,
            run.Audience,
            run.Model,
            run.PromptVersion,
            run.CreatedAt,
            run.StartedAt,
            run.CompletedAt,
            run.ErrorCode,
            run.ErrorMessage,
            run.Artifacts
                .OrderBy(artifact => artifact.Kind)
                .Select(artifact => new RunArtifactResponse(
                    artifact.Kind,
                    artifact.Path,
                    artifact.ContentType,
                    artifact.SizeBytes,
                    artifact.CreatedAt))
                .ToList()));
    }

    private static ContentRunSummaryResponse ToSummary(ContentRun run)
    {
        return new ContentRunSummaryResponse(
            run.Id,
            run.Status,
            run.BrandDescription,
            run.Topic,
            run.NumberOfPosts,
            run.Model,
            run.CreatedAt,
            run.CompletedAt,
            run.EstimatedCostCents);
    }
}

