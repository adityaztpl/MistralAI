// Hardened teaching sample for 15-security-lab/02-idor-on-documents.md.
// The controller derives caller context from validated claims and applies
// object-level authorization before returning or mutating documents.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecurityLab.IdorDocs;

public sealed record CallerContext(
    string UserId,
    string TenantId,
    bool IsTenantAdmin);

public interface ICallerContextAccessor
{
    CallerContext GetRequiredCaller(ClaimsPrincipal user);
}

public sealed class CallerContextAccessor : ICallerContextAccessor
{
    public CallerContext GetRequiredCaller(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing subject claim.");

        var tenantId = user.FindFirstValue("tenant_id")
            ?? throw new UnauthorizedAccessException("Missing tenant claim.");

        var isTenantAdmin = user.IsInRole("TenantAdmin");
        return new CallerContext(userId, tenantId, isTenantAdmin);
    }
}

public interface IDocumentAuthorizationService
{
    bool CanRead(CallerContext caller, Document document);
    bool CanEdit(CallerContext caller, Document document);
}

public sealed class DocumentAuthorizationService : IDocumentAuthorizationService
{
    public bool CanRead(CallerContext caller, Document document)
    {
        if (!string.Equals(document.TenantId, caller.TenantId, StringComparison.Ordinal))
        {
            return false;
        }

        return caller.IsTenantAdmin ||
            string.Equals(document.OwnerUserId, caller.UserId, StringComparison.Ordinal);
    }

    public bool CanEdit(CallerContext caller, Document document)
    {
        if (!string.Equals(document.TenantId, caller.TenantId, StringComparison.Ordinal))
        {
            return false;
        }

        return caller.IsTenantAdmin ||
            string.Equals(document.OwnerUserId, caller.UserId, StringComparison.Ordinal);
    }
}

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class HardenedDocsController(
    DocumentsDbContext db,
    ICallerContextAccessor callers,
    IDocumentAuthorizationService authorization,
    ILogger<HardenedDocsController> logger) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(string id, CancellationToken ct)
    {
        var caller = callers.GetRequiredCaller(User);

        // Tenant predicate is in the database query so cross-tenant rows are
        // never materialized into application memory for this request.
        var document = await db.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                doc => doc.Id == id && doc.TenantId == caller.TenantId,
                ct);

        if (document is null)
        {
            return NotFound();
        }

        if (!authorization.CanRead(caller, document))
        {
            logger.LogWarning(
                "Denied document read. UserId={UserId} TenantId={TenantId} DocumentId={DocumentId}",
                caller.UserId,
                caller.TenantId,
                id);
            return Forbid();
        }

        return Ok(new
        {
            document.Id,
            document.Title,
            document.Body,
            document.UpdatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(
        string id,
        UpdateDocumentRequest request,
        CancellationToken ct)
    {
        var caller = callers.GetRequiredCaller(User);

        var document = await db.Documents
            .SingleOrDefaultAsync(
                doc => doc.Id == id && doc.TenantId == caller.TenantId,
                ct);

        if (document is null)
        {
            return NotFound();
        }

        if (!authorization.CanEdit(caller, document))
        {
            logger.LogWarning(
                "Denied document edit. UserId={UserId} TenantId={TenantId} DocumentId={DocumentId}",
                caller.UserId,
                caller.TenantId,
                id);
            return Forbid();
        }

        document.Title = request.Title.Trim();
        document.Body = request.Body;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new
        {
            document.Id,
            document.Title,
            document.UpdatedAt
        });
    }
}

