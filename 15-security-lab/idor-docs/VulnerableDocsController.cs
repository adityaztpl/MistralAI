// DO NOT USE IN PROD.
// Intentionally vulnerable teaching sample for 15-security-lab/02-idor-on-documents.md.
// Problem: authenticated callers can fetch/update documents by id without
// object-level authorization.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SecurityLab.IdorDocs;

public sealed class Document
{
    public required string Id { get; init; }
    public required string TenantId { get; init; }
    public required string OwnerUserId { get; init; }
    public required string Title { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class DocumentsDbContext : DbContext
{
    public DbSet<Document> Documents => Set<Document>();
}

public sealed record UpdateDocumentRequest(string Title, string Body);

[ApiController]
[Authorize]
[Route("api/vulnerable/documents")]
public sealed class VulnerableDocsController(DocumentsDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(string id, CancellationToken ct)
    {
        // VULNERABLE: authentication is present, but the query does not check
        // whether the caller belongs to the document tenant or has access to it.
        var document = await db.Documents.FindAsync([id], ct);
        return document is null ? NotFound() : Ok(document);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDocument(
        string id,
        UpdateDocumentRequest request,
        CancellationToken ct)
    {
        // VULNERABLE: same IDOR bug on writes. A caller who knows an id can
        // modify another user's document.
        var document = await db.Documents.FindAsync([id], ct);
        if (document is null)
        {
            return NotFound();
        }

        document.Title = request.Title;
        document.Body = request.Body;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(document);
    }
}

