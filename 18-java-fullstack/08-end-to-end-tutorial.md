# 08 - End-to-End Tutorial: Product Catalog + Auth

Build a portfolio-ready vertical slice:

- Spring Boot API
- PostgreSQL
- Flyway migrations
- JWT authentication
- Product CRUD with ownership checks
- React or Angular SPA
- Docker Compose local run
- AWS deployment outline

This tutorial is written as a practical build sequence rather than a full generated project dump. Use the examples in this section as reference snippets.

## Product requirements

Users can:

1. Register with email and password.
2. Login and receive an access token.
3. Create products with SKU, name, price, currency, quantity, category, and status.
4. List only their own products.
5. Search/filter/sort products.
6. Update product details.
7. Archive/delete products.

Admins can:

1. View aggregate product counts.
2. Optionally view all users/products.

Non-functional requirements:

- Versioned REST API.
- Problem Details error responses.
- Field validation.
- PostgreSQL migrations with Flyway.
- Indexes for list/search paths.
- Dockerized dependencies.
- Frontend route protection.
- AWS-ready deployment shape.

## Step 1: Create Spring Boot API

Use Spring Initializr or CLI with:

- Spring Web
- Spring Security
- OAuth2 Resource Server or custom JWT library
- Spring Data JPA
- PostgreSQL Driver
- Validation
- Flyway
- Actuator
- Lombok optional

Maven dependencies should include:

```xml
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-web</artifactId>
</dependency>
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-security</artifactId>
</dependency>
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-oauth2-resource-server</artifactId>
</dependency>
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-data-jpa</artifactId>
</dependency>
<dependency>
  <groupId>org.springframework.boot</groupId>
  <artifactId>spring-boot-starter-validation</artifactId>
</dependency>
<dependency>
  <groupId>org.flywaydb</groupId>
  <artifactId>flyway-core</artifactId>
</dependency>
<dependency>
  <groupId>org.postgresql</groupId>
  <artifactId>postgresql</artifactId>
  <scope>runtime</scope>
</dependency>
```

## Step 2: Configure application

`application.yml`:

```yaml
spring:
  application:
    name: product-catalog-api
  jpa:
    open-in-view: false
    hibernate:
      ddl-auto: validate
  flyway:
    enabled: true

management:
  endpoint:
    health:
      probes:
        enabled: true
  endpoints:
    web:
      exposure:
        include: health,info,metrics

app:
  cors:
    allowed-origins:
      - http://localhost:5173
      - http://localhost:4200
  jwt:
    issuer: product-catalog-api
    access-token-minutes: 30
```

`application-postgres.yml`:

```yaml
spring:
  datasource:
    url: jdbc:postgresql://localhost:5432/catalog
    username: catalog
    password: catalog
  jpa:
    database-platform: org.hibernate.dialect.PostgreSQLDialect
```

## Step 3: Create Flyway migration

Use [`examples/flyway/V1__products_postgres.sql`](examples/flyway/V1__products_postgres.sql).

Core tables:

- `app_users`
- `categories`
- `products`
- `product_audit_events`

Indexes:

- unique email
- unique `(owner_id, sku)`
- list by owner/status/created
- filter by owner/category/status/name

## Step 4: Model entities

`AppUser`:

```java
@Entity
@Table(name = "app_users")
public class AppUser {
    @Id
    private UUID id;

    @Column(nullable = false, unique = true)
    private String email;

    @Column(name = "password_hash", nullable = false)
    private String passwordHash;

    @Enumerated(EnumType.STRING)
    private Role role;

    private boolean enabled;
}
```

`Product`:

```java
@Entity
@Table(name = "products")
public class Product {
    @Id
    private UUID id;

    @Column(name = "owner_id", nullable = false)
    private UUID ownerId;

    @Column(nullable = false)
    private String sku;

    @Column(nullable = false)
    private String name;

    @Column(nullable = false, precision = 12, scale = 2)
    private BigDecimal price;

    @Column(nullable = false, length = 3)
    private String currency;

    @Column(nullable = false)
    private int quantity;

    @Enumerated(EnumType.STRING)
    private ProductStatus status;

    @Version
    private long version;
}
```

For a richer domain model, add methods:

```java
public void changePrice(BigDecimal newPrice) {
    if (newPrice.signum() < 0) {
        throw new IllegalArgumentException("Price cannot be negative");
    }
    this.price = newPrice;
}

public void archive() {
    if (status == ProductStatus.DELETED) {
        throw new IllegalStateException("Deleted product cannot be archived");
    }
    this.status = ProductStatus.ARCHIVED;
}
```

## Step 5: Repositories

```java
public interface ProductRepository extends JpaRepository<Product, UUID> {
    Optional<Product> findByIdAndOwnerId(UUID id, UUID ownerId);

    boolean existsByOwnerIdAndSku(UUID ownerId, String sku);

    Page<Product> findByOwnerIdAndStatus(UUID ownerId, ProductStatus status, Pageable pageable);
}
```

For search, use `JpaSpecificationExecutor`:

```java
public interface ProductRepository extends JpaRepository<Product, UUID>, JpaSpecificationExecutor<Product> {
    Optional<Product> findByIdAndOwnerId(UUID id, UUID ownerId);
    boolean existsByOwnerIdAndSku(UUID ownerId, String sku);
}
```

Specification sketch:

```java
public final class ProductSpecifications {
    public static Specification<Product> visibleTo(UUID ownerId) {
        return (root, query, cb) -> cb.equal(root.get("ownerId"), ownerId);
    }

    public static Specification<Product> hasStatus(ProductStatus status) {
        return (root, query, cb) -> status == null ? cb.conjunction() : cb.equal(root.get("status"), status);
    }

    public static Specification<Product> matchesQuery(String queryText) {
        return (root, query, cb) -> {
            if (queryText == null || queryText.isBlank()) return cb.conjunction();
            String pattern = "%" + queryText.toLowerCase(Locale.ROOT) + "%";
            return cb.or(
                cb.like(cb.lower(root.get("name")), pattern),
                cb.like(cb.lower(root.get("sku")), pattern)
            );
        };
    }
}
```

## Step 6: Auth API

Endpoints:

```text
POST /api/v1/auth/register
POST /api/v1/auth/login
GET  /api/v1/auth/me
```

Request:

```json
{
  "email": "demo@example.com",
  "password": "CorrectHorseBatteryStaple1!"
}
```

Response:

```json
{
  "accessToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresAt": "2026-07-18T09:00:00Z",
  "user": {
    "id": "8a337fb6-4d6b-443b-8e2e-8d915e54fd08",
    "email": "demo@example.com",
    "roles": ["USER"]
  }
}
```

Use BCrypt:

```java
@Bean
PasswordEncoder passwordEncoder() {
    return new BCryptPasswordEncoder();
}
```

See [`examples/spring/AuthController.java`](examples/spring/AuthController.java).

## Step 7: Security configuration

Rules:

- Permit register/login and health.
- Authenticate product endpoints.
- Enforce admin endpoints by role.
- Stateless sessions for bearer token API.
- CORS configured from properties.

Add method security:

```java
@EnableMethodSecurity
@Configuration
class SecurityConfig {
}
```

## Step 8: Product API

Endpoints:

```text
GET    /api/v1/products
GET    /api/v1/products/{id}
POST   /api/v1/products
PUT    /api/v1/products/{id}
DELETE /api/v1/products/{id}
```

Create request:

```json
{
  "sku": "SHOE-001",
  "name": "Trail Shoe",
  "description": "Lightweight trail shoe",
  "price": 89.99,
  "currency": "USD",
  "quantity": 12,
  "categoryId": "2e4e1a5f-f6c8-4e39-8d11-f5a442516b1d"
}
```

Validation:

- name required, max 80
- SKU required, max 40
- price non-negative
- currency 3 characters
- quantity non-negative

Service:

- derive `ownerId` from authenticated user
- check duplicate SKU
- enforce ownership on get/update/delete
- publish domain/audit event if desired

See [`examples/spring/ProductService.java`](examples/spring/ProductService.java).

## Step 9: Problem Details

Implement `@RestControllerAdvice` for:

- `MethodArgumentNotValidException` -> `400`
- `ConstraintViolationException` -> `400`
- `ProductNotFoundException` -> `404`
- `DuplicateSkuException` -> `409`
- `ObjectOptimisticLockingFailureException` -> `409`
- `AccessDeniedException` -> `403`
- fallback -> `500`

Frontend depends on this stable shape.

## Step 10: React client option

Create:

```bash
npm create vite@latest catalog-web -- --template react-ts
cd catalog-web
npm install react-router-dom @tanstack/react-query react-hook-form zod @hookform/resolvers
```

Build:

- `AuthContext`
- `apiClient`
- `ProtectedRoute`
- `LoginPage`
- `ProductListPage`
- `ProductForm`

Use examples:

- [`examples/react-client/AuthContext.tsx`](examples/react-client/AuthContext.tsx)
- [`examples/react-client/apiClient.ts`](examples/react-client/apiClient.ts)

Minimum route flow:

```text
/login
/products
/products/new
/products/:id
/products/:id/edit
```

## Step 11: Angular client option

Create:

```bash
ng new catalog-web --standalone --routing --style=scss
```

Build:

- `AuthService`
- `authInterceptor`
- `authGuard`
- `ProductsService`
- `LoginPage`
- `ProductListPage`
- `ProductFormComponent`

Use examples:

- [`examples/angular-client/auth.interceptor.ts`](examples/angular-client/auth.interceptor.ts)
- [`examples/angular-client/auth.guard.ts`](examples/angular-client/auth.guard.ts)

Minimum route flow:

```text
/login
/products
/products/new
/products/:id
/products/:id/edit
```

## Step 12: Docker local run

Use [`examples/docker/docker-compose.yml`](examples/docker/docker-compose.yml).

Run:

```bash
docker compose up --build
```

Expected:

- API: <http://localhost:8080/actuator/health>
- PostgreSQL: `localhost:5432`
- React dev server: <http://localhost:5173>
- Angular dev server: <http://localhost:4200>

## Step 13: Tests

Backend:

- service unit tests for duplicate SKU and ownership
- controller tests for validation and auth
- repository integration tests with Testcontainers
- Flyway migration smoke test

Frontend:

- auth context/service tests
- interceptor/API client tests
- protected route/guard tests
- product form validation tests
- E2E login + create product

Example backend test cases:

```text
register rejects duplicate email
login rejects bad password
create product rejects duplicate sku per owner
user cannot read another user's product
invalid product request returns Problem Details errors
list products returns only current user's products
optimistic locking conflict maps to 409
```

## Step 14: AWS deploy outline

### API

1. Build Docker image with [`examples/docker/api.Dockerfile`](examples/docker/api.Dockerfile).
2. Push image to ECR.
3. Create RDS PostgreSQL.
4. Store DB password and JWT secret in Secrets Manager.
5. Create ECS task definition.
6. Create ECS service behind ALB.
7. Configure health check `/actuator/health/readiness`.
8. Configure API domain and TLS.

### SPA

1. Build React or Angular app.
2. Upload static assets to S3.
3. Serve through CloudFront.
4. Configure SPA route fallback.
5. Set `VITE_API_BASE_URL` or Angular environment API URL to `https://api.example.com`.
6. Invalidate CloudFront after deploy.

### CI/CD

Use [`examples/aws/github-actions-deploy.yml`](examples/aws/github-actions-deploy.yml) as a starting point.

## Demo script for interviews

1. Show architecture diagram.
2. Start Docker Compose.
3. Run migrations from clean database.
4. Register a user.
5. Login and show token response.
6. Create product.
7. Show product list with pagination.
8. Trigger validation error and show Problem Details on frontend form.
9. Show ownership protection with another user.
10. Show database indexes/migration.
11. Explain AWS deployment.
12. Discuss production improvements.

## Production improvements to mention

- Cognito or BFF for stronger auth.
- Refresh token rotation or short-lived access tokens.
- Rate limiting.
- Audit logging.
- Structured JSON logs.
- OpenTelemetry tracing.
- Testcontainers in CI.
- Blue/green deployment.
- Controlled migration job.
- WAF for public endpoints.
- Secrets rotation.
- CloudFront security headers.
- DB read replicas only if read pressure justifies.

## Finished project README outline

```text
# Product Catalog

## Features
## Architecture
## Tech stack
## Local setup
## Environment variables
## API endpoints
## Database schema
## Auth flow
## Tests
## AWS deployment
## Trade-offs
## Known limitations
## Next steps
```

## Completion checklist

- [ ] API runs locally.
- [ ] Flyway creates schema from empty database.
- [ ] Register/login works.
- [ ] Product CRUD works with ownership checks.
- [ ] Problem Details is consistent.
- [ ] React or Angular route protection works.
- [ ] Server validation renders in forms.
- [ ] Docker Compose starts dependencies.
- [ ] At least one integration test uses PostgreSQL.
- [ ] AWS deploy plan is documented.
