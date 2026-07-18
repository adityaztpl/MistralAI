# Security and Authentication Deep Dive

A detailed guide to Identity, JWT, refresh tokens, policies, resource authorization, CORS, and OWASP API security for ASP.NET Core interviews.

## 1. Vocabulary and middleware order

Authentication establishes who the caller is. Authorization decides what the caller may do. Claims are statements about the caller. Policies are named authorization rules.

```csharp
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
```

Authentication must run before authorization so `HttpContext.User` is populated.

## 2. Identity

ASP.NET Core Identity provides password hashing, user stores, roles, claims, lockout, reset tokens, security stamps, and MFA extensibility.

```csharp
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole<Guid>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();
```

Do not invent password hashing or reveal whether an email exists during login/reset.

## 3. JWT bearer tokens

JWT APIs validate issuer, audience, lifetime, signature, and signing key.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
```

JWT payloads are encoded, not encrypted. Do not put secrets in claims.

## 4. Refresh tokens

Access tokens are short-lived bearer credentials sent to APIs. Refresh tokens are longer-lived credentials sent only to auth endpoints.

```text
login -> access token + refresh token
refresh -> validate hash -> revoke old token -> issue new pair
reuse detected -> revoke token family -> require reauthentication
```

Store refresh tokens hashed. Rotate on every use. Revoke on logout and suspicious reuse.

## 5. Policies and permissions

Use policies instead of scattering claim checks.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Write", policy =>
        policy.RequireAuthenticatedUser().RequireClaim("permission", "Products.Write"));
});
```

Roles describe broad job functions. Permissions describe specific actions. Scopes describe delegated API access.

## 6. Resource authorization

Endpoint authorization is not enough for object ownership.

```csharp
var order = await _db.Orders.SingleOrDefaultAsync(order => order.Id == id, cancellationToken);
var result = await _authorization.AuthorizeAsync(User, order, "CanViewOrder");
if (!result.Succeeded)
{
    return Forbid();
}
```

Broken object-level authorization is when users change ids and access resources they do not own.

## 7. CORS and CSRF

CORS controls browser cross-origin JavaScript calls. It is not authentication and does not protect server-to-server APIs.

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins("https://app.example.com")
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Authorization", "Content-Type"));
});
```

Cookie-auth APIs must consider CSRF because browsers attach cookies automatically.

## 8. OWASP API risks

Map risks to mitigations:

- Broken object-level authorization -> resource checks and tenant filters.
- Broken authentication -> Identity hardening, MFA, lockout, token validation.
- Mass assignment -> request DTOs and server-owned fields.
- Unrestricted resource consumption -> pagination, rate limits, cancellation.
- Injection -> parameterized SQL and validation.
- Security misconfiguration -> environment gates, secure headers, no open admin Swagger.
- Unsafe API consumption -> timeouts, schema validation, allowlists.

## 9. Secrets and Data Protection

Use managed secret stores in production. Use user secrets in development.

```bash
dotnet user-secrets set "Jwt:SigningKey" "development-only-secret-at-least-32-chars"
```

Data Protection keys must be shared across instances when using cookies, antiforgery, or protected payloads.

## 10. Audit logging

Security logs should include user id, tenant id, action, resource id, result, client info, and trace id. Never log passwords, access tokens, refresh tokens, or full connection strings.

```csharp
_logger.LogWarning("Authorization failed for user {UserId} on order {OrderId} trace {TraceId}", userId, orderId, traceId);
```

## Interview checklist

- [ ] Explain the concept without code.
- [ ] Implement the smallest working example.
- [ ] Name two production pitfalls.
- [ ] Name one test that catches a regression.
- [ ] Describe the operational signal that reveals a production issue.

## Explain this prompts

- Explain the difference between authentication and authorization.
- Explain JWT validation step by step.
- Explain refresh token rotation and reuse detection.
- Explain object-level authorization.
- Explain why CORS is not authorization.
- Explain mass assignment and DTO mitigation.
- Explain how you would secure an admin endpoint.
- Explain what you would log for failed authorization.

## Hands-on lab: secure an order endpoint

Build `GET /api/orders/{id}` with layered security.

### Requirements

- Caller must be authenticated.
- Caller must have `Orders.Read` permission.
- Caller must belong to the same tenant as the order.
- The response must not reveal internal payment provider ids.
- Failed authorization should not log tokens.
- Cross-tenant access must be covered by integration tests.

### Endpoint sketch

```csharp
group.MapGet("/{id:guid}", async (
    Guid id,
    OrdersDbContext db,
    IAuthorizationService authorization,
    ClaimsPrincipal user,
    CancellationToken cancellationToken) =>
{
    var order = await db.Orders
        .AsNoTracking()
        .Where(order => order.Id == id)
        .Select(order => new OrderAuthorizationResource(order.Id, order.TenantId, order.CustomerUserId))
        .SingleOrDefaultAsync(cancellationToken);

    if (order is null)
    {
        return Results.NotFound();
    }

    var auth = await authorization.AuthorizeAsync(user, order, "Orders.SameTenant");
    if (!auth.Succeeded)
    {
        return Results.Forbid();
    }

    var response = await db.Orders
        .AsNoTracking()
        .Where(existing => existing.Id == id)
        .Select(existing => new OrderDetailResponse(existing.Id, existing.OrderNumber, existing.Total))
        .SingleAsync(cancellationToken);

    return Results.Ok(response);
})
.RequireAuthorization("Orders.Read");
```

### Tests to write

- No token returns `401`.
- Token without `Orders.Read` returns `403`.
- Token with `Orders.Read` but wrong tenant returns `403` or deliberately `404`.
- Token with correct permission and tenant returns `200`.
- Response body does not contain internal fields.

## Security pitfall matrix

| Pitfall | Example | Safer pattern |
| --- | --- | --- |
| long-lived JWT | 24-hour admin token | short access token + refresh rotation |
| unhashed refresh token | DB leak gives valid tokens | SHA-256/HMAC hash at rest |
| role-only checks | `Admin` means everything | permissions/policies per action |
| no object auth | user changes route id | resource authorization and tenant filters |
| wildcard CORS | any site can call with browser | exact origins and no wildcard credentials |
| DTO over-posting | client sets `IsAdmin` | request DTOs with server-owned fields omitted |
| logging tokens | bearer token in logs | structured logs with token redaction |
| disabled issuer validation | token for another API accepted | validate issuer/audience/signature |
| public Swagger admin ops | attackers discover endpoints | environment gate and auth |
| secrets in JSON | committed signing key | user secrets/managed vault |

## JWT review checklist

- [ ] Is the signing algorithm expected and strong?
- [ ] Is issuer validation enabled?
- [ ] Is audience validation enabled?
- [ ] Is lifetime validation enabled with small clock skew?
- [ ] Is signing key or authority trusted?
- [ ] Are claims minimal and non-sensitive?
- [ ] Is access token lifetime short?
- [ ] Is refresh token storage hashed?
- [ ] Is refresh token rotation implemented?
- [ ] Is reuse detection implemented?
- [ ] Are logout and administrative revocation supported?

## CORS decision guide

Use CORS when browser JavaScript from one origin must call an API on another origin.

Do not use CORS as:

- User authentication.
- Service-to-service authorization.
- CSRF protection by itself.
- A replacement for rate limiting.

Common production policy:

```csharp
policy.WithOrigins("https://app.example.com")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")
    .SetPreflightMaxAge(TimeSpan.FromHours(1));
```

## OWASP scenario drills

### Broken object-level authorization

A user calls `/api/accounts/other-user-id`. The endpoint checks only `[Authorize]`.

Fix:

- Filter by authenticated subject or tenant.
- Use resource authorization after loading.
- Add integration tests with two users.

### Mass assignment

A request DTO includes `IsAdmin` or `TenantId` and maps directly to an entity.

Fix:

- Remove server-owned fields from request DTOs.
- Set tenant/user fields from authenticated claims.
- Add tests proving clients cannot alter protected fields.

### Unrestricted resource consumption

An export endpoint can request unlimited rows.

Fix:

- Require pagination or async export jobs.
- Add rate limits.
- Enforce maximum date ranges.
- Use cancellation tokens and timeouts.

## Audit event examples

- Login succeeded.
- Login failed.
- MFA challenge failed.
- Refresh token reused.
- Permission denied.
- Admin changed user permissions.
- Secret/key rotation completed.
- Cross-tenant access denied.

Each audit event should include actor, action, resource, result, timestamp, trace id, and source IP where appropriate.

## Final security flashcards

- `401` means not authenticated.
- `403` means authenticated but not allowed.
- JWTs are signed, not encrypted.
- Bearer means possession is enough.
- Refresh tokens are credentials.
- CORS is browser enforcement.
- CSRF matters when cookies authenticate unsafe methods.
- Resource authorization prevents route-id attacks.
- DTOs prevent mass assignment.
- Secrets belong in managed configuration providers.
