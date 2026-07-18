# 2-Hour Take-Home: Notes API

## Interview-ready summary

Build a small notes API that demonstrates disciplined backend fundamentals: clean endpoints, validation, persistence, error handling, tests, and a README that makes the reviewer productive in minutes.

The scope is intentionally small. The signal comes from how you prioritize correctness and clarity under time pressure.

## Timebox

Maximum: 2 hours of implementation time.

Recommended split:

| Phase | Minutes | Goal |
|---|---:|---|
| Read prompt and sketch design | 10 | Decide entities, endpoints, and storage |
| Project setup | 15 | Create runnable API and test project |
| Core CRUD | 40 | Implement notes endpoints and persistence |
| Validation and errors | 20 | Add request validation and consistent responses |
| Tests | 25 | Cover happy path plus important failures |
| README and cleanup | 10 | Document setup, assumptions, and trade-offs |

## Product brief

Create an API for a user to manage personal notes. A note has:

- `id`: server-generated identifier.
- `title`: required, 1-120 characters.
- `body`: optional, up to 10,000 characters.
- `tags`: optional list of lower-case tag names.
- `isArchived`: boolean.
- `createdAt`: UTC timestamp.
- `updatedAt`: UTC timestamp.

The reviewer should be able to create, list, get, update, archive, unarchive, and delete notes.

## Required endpoints

Use RESTful JSON endpoints. Suggested contract:

```http
POST   /api/notes
GET    /api/notes
GET    /api/notes/{id}
PUT    /api/notes/{id}
PATCH  /api/notes/{id}/archive
PATCH  /api/notes/{id}/unarchive
DELETE /api/notes/{id}
```

### Create note

Request:

```json
{
  "title": "Prepare RAG demo",
  "body": "Add citations and streaming before submitting.",
  "tags": ["rag", "interview"]
}
```

Response: `201 Created` with `Location` header and the created note.

### List notes

Support practical filters:

```http
GET /api/notes?search=rag&tag=interview&archived=false&sort=updatedAt_desc&page=1&pageSize=20
```

Minimum required behavior:

- Default to non-archived notes unless `archived=true` or `archived=all` is passed.
- Search title and body case-insensitively.
- Filter by one tag.
- Return stable pagination metadata.

Response shape:

```json
{
  "items": [
    {
      "id": "3d7c9b0c-5a5a-44ac-a2a1-1df16d1a5b3b",
      "title": "Prepare RAG demo",
      "body": "Add citations and streaming before submitting.",
      "tags": ["interview", "rag"],
      "isArchived": false,
      "createdAt": "2026-07-18T04:00:00Z",
      "updatedAt": "2026-07-18T04:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 1,
  "totalPages": 1
}
```

### Update note

Use full replacement with `PUT` or partial replacement with `PATCH`; document your choice. For a 2-hour take-home, full replacement is simpler and fine.

Rules:

- `404` if the note does not exist.
- `400` or `422` for validation failures.
- `updatedAt` changes on update.
- Tags are normalized: trim whitespace, lower-case, de-duplicate, sort for stable responses.

### Delete note

A hard delete is acceptable. If you implement soft delete, document it.

## Functional requirements

- Implement the required endpoints.
- Persist data using SQLite, PostgreSQL, or an in-memory repository with clear notes. SQLite is a strong default because it is durable and easy to run.
- Validate inputs.
- Return consistent errors.
- Use UTC timestamps.
- Include at least 4 tests.
- Include a README with setup and design notes.

## Constraints

- Do not use frontend code unless you finish early.
- Do not implement authentication unless explicitly required by a company prompt.
- Do not spend time on Docker if local commands are enough.
- Do not add a complex architecture that obscures the solution.
- Do not commit secrets or machine-specific paths.

## Starter hints: ASP.NET Core

A clean 2-hour .NET implementation can use:

- Minimal APIs grouped under `/api/notes`.
- EF Core with SQLite.
- Request/response DTOs.
- `ProblemDetails` for errors.
- xUnit + `WebApplicationFactory` integration tests.

### Entity sketch

```csharp
public sealed class Note
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<NoteTag> Tags { get; set; } = new();
}

public sealed class NoteTag
{
    public Guid NoteId { get; set; }
    public Note Note { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
}
```

### DTO sketch

```csharp
public sealed record CreateNoteRequest(
    string Title,
    string? Body,
    IReadOnlyList<string>? Tags);

public sealed record UpdateNoteRequest(
    string Title,
    string? Body,
    IReadOnlyList<string>? Tags,
    bool IsArchived);

public sealed record NoteResponse(
    Guid Id,
    string Title,
    string? Body,
    IReadOnlyList<string> Tags,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

### Validation sketch

```csharp
static List<string> ValidateNoteInput(string title, string? body, IReadOnlyList<string>? tags)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(title))
        errors.Add("Title is required.");
    else if (title.Trim().Length > 120)
        errors.Add("Title must be 120 characters or fewer.");

    if (body is { Length: > 10_000 })
        errors.Add("Body must be 10,000 characters or fewer.");

    if (tags is not null && tags.Count > 20)
        errors.Add("A note can have at most 20 tags.");

    return errors;
}
```

### Endpoint sketch

```csharp
var notes = app.MapGroup("/api/notes");

notes.MapPost("/", async (CreateNoteRequest request, AppDbContext db, TimeProvider clock) =>
{
    var errors = ValidateNoteInput(request.Title, request.Body, request.Tags);
    if (errors.Count > 0) return Results.ValidationProblem(errors.ToDictionary(e => e, e => new[] { e }));

    var now = clock.GetUtcNow();
    var note = new Note
    {
        Id = Guid.NewGuid(),
        Title = request.Title.Trim(),
        Body = string.IsNullOrWhiteSpace(request.Body) ? null : request.Body.Trim(),
        CreatedAt = now,
        UpdatedAt = now,
        Tags = NormalizeTags(request.Tags).Select(name => new NoteTag { Name = name }).ToList()
    };

    db.Notes.Add(note);
    await db.SaveChangesAsync();

    var response = ToResponse(note);
    return Results.Created($"/api/notes/{note.Id}", response);
});
```

## Starter hints: TypeScript/Node alternative

A simple Express/Fastify implementation can use:

- Zod for request validation.
- Prisma + SQLite.
- Vitest or Jest + Supertest.
- A repository layer only if it simplifies tests.

## Testing expectations

Minimum useful tests:

1. Create note returns `201`, normalized tags, and timestamps.
2. Create note rejects missing or too-long title.
3. List notes filters by tag and archived status.
4. Update note changes `updatedAt` and preserves `createdAt`.
5. Get/update/delete unknown id returns `404`.

Integration tests are more valuable than isolated unit tests for this assignment because the API contract is the product.

## Error response guidance

Prefer a consistent shape, for example RFC 7807 `ProblemDetails`:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "title": ["Title is required."]
  }
}
```

## README expectations

Include:

- Tech stack.
- Setup commands.
- How to run tests.
- Example curl commands.
- Assumptions.
- What you would improve with more time.

Example:

```markdown
## Trade-offs

I used SQLite to make the project runnable without external services. I implemented full replacement updates because the assignment is timeboxed and the contract is easy to reason about. With more time I would add authentication, optimistic concurrency, and OpenAPI examples.
```

## Common mistakes

- Building a UI before the API is solid.
- Returning EF entities directly, exposing persistence shape.
- Missing pagination metadata.
- Using local time instead of UTC.
- Not testing error cases.
- Hiding setup steps in IDE configuration.
- Failing to normalize tags, causing duplicate tags like `RAG`, `rag`, and ` rag `.

## Stretch goals if you finish early

Choose at most one or two:

- OpenAPI/Swagger examples.
- Docker Compose for API + PostgreSQL.
- Optimistic concurrency with a row version.
- Import/export notes as Markdown.
- Basic rate limiting.
- Simple frontend list/create page.
- Full-text search index if using PostgreSQL.

## Submission phrasing

> I focused on a complete API contract, validation, SQLite persistence, and integration tests. I intentionally kept the architecture small because the prompt was timeboxed. The main trade-off is that authentication and concurrency controls are documented but not implemented. The next production step would be tenant-aware auth, audit logging for deletes, and a search index for larger note volumes.
