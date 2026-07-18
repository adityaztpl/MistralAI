# Testing ASP.NET Core APIs

A hands-on testing guide covering unit tests, integration tests, WebApplicationFactory, fake auth, realistic database providers, Testcontainers patterns, and CI strategy.

## 1. Testing pyramid

Use many unit tests, some integration tests, and few end-to-end tests.

| Test type | Catches | Misses |
| --- | --- | --- |
| Unit | domain rules, validators, handlers | routing, auth, serialization |
| Integration | middleware, DI, model binding, EF, auth | browser behavior |
| E2E | user journeys | precise failure localization |

## 2. Unit tests

Keep domain logic independent.

```csharp
[Fact]
public void AddItem_WhenQuantityInvalid_Throws()
{
    var order = new Order();
    Assert.Throws<ArgumentOutOfRangeException>(() => order.AddItem(1, 0, 10));
}
```

Unit tests should be fast, deterministic, and not require ASP.NET hosting.

## 3. WebApplicationFactory

`WebApplicationFactory<TEntryPoint>` boots the API in memory and provides an `HttpClient`.

```csharp
public sealed class ProductsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ProductsApiTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();
}
```

It tests routing, middleware, DI, auth, JSON, filters, exception handling, and status codes.

## 4. Custom test factory

Override services in `ConfigureWebHost`.

```csharp
builder.ConfigureServices(services =>
{
    services.RemoveAll<DbContextOptions<AppDbContext>>();
    services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
});
```

Set environment to `Testing` and replace external services with fakes.

## 5. EF provider choice

EF InMemory is not relational. SQLite in-memory catches more relational behavior. Testcontainers with SQL Server/PostgreSQL catches provider-specific behavior.

Use the provider that matches the risk: simple logic can use fakes; migrations, constraints, transactions, SQL translation, and indexes need real-ish providers.

## 6. Fake authentication

Use a fake auth handler for integration tests.

```csharp
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim("scope", "products.write") };
    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
}
```

Test no token, missing scope, wrong owner, and success.

## 7. Validation and ProblemDetails

Assert real HTTP responses.

```csharp
var response = await client.PostAsJsonAsync("/api/products", new { name = "", price = -1 });
Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
Assert.Contains("Name", problem!.Errors.Keys);
```

## 8. Concurrency tests

Concurrency test recipe:

1. Seed a row.
2. Read row version.
3. Update once.
4. Attempt stale update with old row version.
5. Assert `409 Conflict` and `ProblemDetails`.

## 9. Testcontainers pattern

Use real database containers for provider accuracy.

```csharp
private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .WithDatabase("catalog_tests")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .Build();
```

Start in fixture initialization and pass connection string to the test factory.

## 10. CI and anti-patterns

CI should run unit tests on every push and integration tests on every PR. Publish test results and keep tests parallel-safe.

Avoid mocking `DbSet<T>`, sharing mutable DB state, calling real external services, using sleeps for async timing, and testing implementation details instead of behavior.

## Interview checklist

- [ ] Explain the concept without code.
- [ ] Implement the smallest working example.
- [ ] Name two production pitfalls.
- [ ] Name one test that catches a regression.
- [ ] Describe the operational signal that reveals a production issue.

## Explain this prompts

- Explain what WebApplicationFactory tests that controller unit tests miss.
- Explain why EF InMemory is risky.
- Explain how to fake authenticated users.
- Explain how to test tenant isolation.
- Explain how to test validation ProblemDetails.
- Explain how to test a concurrency conflict.
- Explain how to isolate test data in parallel runs.

## Hands-on lab: integration-test a secured CRUD API

Build tests for a products API with these behaviors:

- Anonymous read returns `401`.
- Authenticated read with `products.read` returns `200`.
- Authenticated write without `products.write` returns `403`.
- Invalid create request returns `400` with `ValidationProblemDetails`.
- Stale row version update returns `409`.
- Cross-tenant read returns `403` or `404` according to the API's disclosure policy.

### Test fixture responsibilities

- Start the application with environment `Testing`.
- Replace production database with SQLite or a containerized database.
- Replace real authentication with a fake test scheme.
- Replace external HTTP clients with stubs.
- Reset database state between tests.
- Seed only data required by the test.

### Sample auth-state matrix

| Test user | Claims | Expected |
| --- | --- | --- |
| anonymous | none | `401` |
| reader | `products.read`, tenant A | read tenant A |
| writer | `products.write`, tenant A | create/update tenant A |
| wrong tenant | `products.read`, tenant B | denied tenant A |
| admin | `products.read`, `products.write`, `users.manage` | admin flows only |

## Testcontainers workflow

1. Start database container in fixture initialization.
2. Configure `WebApplicationFactory` with container connection string.
3. Apply migrations or `EnsureCreated` for sample apps.
4. Seed data.
5. Run tests.
6. Dispose container.

```csharp
public async Task InitializeAsync()
{
    await _container.StartAsync();
    await using var scope = Factory.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

When to choose Testcontainers:

- Migrations matter.
- Provider-specific SQL matters.
- Constraints and transactions matter.
- You need confidence close to production.

When not to choose it:

- Pure domain rules.
- Simple validators.
- Fast mapper tests.
- Tests that run thousands of times locally.

## Integration test anti-flakiness checklist

- [ ] No shared mutable rows without reset.
- [ ] No dependency on current wall-clock time without `TimeProvider`.
- [ ] No `Task.Delay` sleeps for eventual behavior.
- [ ] No calls to real third-party services.
- [ ] Unique ids per test where parallelism is enabled.
- [ ] Database reset is deterministic.
- [ ] Assertions wait on explicit observable state.
- [ ] Logs are captured for failed tests.

## What not to mock

Avoid mocking these in high-value integration tests:

- MVC model binding.
- Authorization middleware.
- JSON serialization.
- EF SQL translation.
- Database constraints.
- Exception middleware.

Mock or fake these instead:

- Email/SMS providers.
- Payment gateways.
- Time.
- Current user for unit tests.
- Slow external APIs.

## Testing `ProblemDetails`

Good assertions:

```csharp
Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
Assert.Contains("Name", problem!.Errors.Keys);
```

Avoid asserting volatile fields such as exact trace id, timestamps, or property ordering.

## Testing authorization ownership

Recipe:

1. Seed tenant A resource.
2. Create client with tenant B claim.
3. Call tenant A resource.
4. Assert denied.
5. Assert response does not leak sensitive details.
6. Assert logs contain denial event without token values if logs are captured.

## Contract testing options

- Validate generated OpenAPI document.
- Compile generated client SDK.
- Assert representative JSON response shape.
- Use consumer-driven contracts for external consumers.
- Keep snapshots focused and scrub volatile fields.

## Performance tests in the test strategy

Performance tests are not normal unit tests. Use them to catch:

- Accidental N+1 query counts.
- Response payload growth.
- Slow serialization paths.
- Expensive authorization handlers.
- Cache miss storms under load.

For endpoint query counts, capture EF logs or diagnostics and assert a bounded number of commands for representative data.

## Pull request testing checklist

- [ ] Unit tests cover new domain branches.
- [ ] Integration tests cover endpoint behavior.
- [ ] Auth failures are tested.
- [ ] Validation failures are tested.
- [ ] Database constraints or migrations are tested where relevant.
- [ ] Tests avoid real external services.
- [ ] Tests are deterministic locally and in CI.
- [ ] Failure messages explain behavior, not implementation trivia.

## Final testing flashcards

- `WebApplicationFactory` tests the real ASP.NET Core pipeline.
- EF InMemory is not relational.
- SQLite in-memory needs an open connection.
- Fake auth should supply realistic claims.
- Unit tests should not require the web host.
- Integration tests should assert HTTP behavior.
- Testcontainers trade speed for provider accuracy.
- Avoid sleeps; control time.
- Reset state between tests.
- Test security failures, not only happy paths.

## Extra interview scenarios

### Scenario: flaky integration test

Investigate in this order:

1. Does the test share database state?
2. Does it depend on wall-clock time?
3. Does it rely on test execution order?
4. Does it call a real external dependency?
5. Does it assert a volatile value such as trace id or timestamp?
6. Does it use `Task.Delay` instead of observing state?
7. Does parallel execution reuse the same unique key?

### Scenario: endpoint works locally but fails in CI

Common causes:

- Missing environment variables.
- Different database provider.
- Race in migration or seeding.
- Case-sensitive filesystem/path issue.
- Different culture/time zone.
- Test assumes local development secrets.

A strong answer explains how to make the environment explicit and deterministic.
