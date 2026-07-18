# 01 - Spring Boot Basics

Target stack: Spring Boot 3.x, Spring Framework 6.x, Java 17+. This guide focuses on the foundation expected in interviews and in day-one production work.

## Outcomes

- [ ] Create and explain a Boot 3 application structure.
- [ ] Use component scanning and dependency injection correctly.
- [ ] Build REST controllers with validation, DTOs, and global errors.
- [ ] Configure profiles and application properties safely.
- [ ] Expose basic Actuator endpoints and document APIs with OpenAPI.

## Spring Boot overview

Spring Boot is an opinionated application platform on top of the Spring Framework. It keeps Spring core concepts such as inversion of control, dependency injection, AOP, transactions, and MVC, while adding auto-configuration, starters, embedded servers, configuration conventions, Actuator, and production defaults that reduce setup code.

**Interview focus**

- Know the difference between Spring Framework and Spring Boot.
- Explain starter dependencies as curated dependency sets, not runtime magic.
- Explain auto-configuration as conditional bean registration based on classpath, beans, and properties.

**Production checklist**

- [ ] Use Spring Boot 3.x with Java 17+.
- [ ] Prefer explicit business design over relying on generated scaffolding.
- [ ] Know how to inspect auto-configuration with `--debug` or the Actuator conditions endpoint.

**Common pitfalls**

- Thinking Boot replaces Spring; it packages and configures Spring.
- Treating every auto-configured bean as untouchable; most can be overridden deliberately.

## Project structure

A conventional Boot project keeps the main application class near the root package so component scanning covers controllers, services, repositories, configuration, and adapters. A common package layout is `api`, `application`, `domain`, `persistence`, `security`, `config`, and `integration`, but choose boundaries that reflect the product.

**Interview focus**

- Why the main class package matters.
- How Maven/Gradle source sets map to runtime code and tests.
- How to organize packages by feature versus layer.

**Production checklist**

- [ ] Place `DemoApplication` in a top-level package such as `com.example.inventory`.
- [ ] Keep test fixtures under `src/test/java` and test resources under `src/test/resources`.
- [ ] Separate generated code, migrations, and static docs intentionally.

**Code example**

```text
src/main/java/com/example/inventory
  DemoApplication.java
  product/api/ProductController.java
  product/application/ProductService.java
  product/domain/Product.java
  product/persistence/ProductEntity.java
src/main/resources
  application.yml
  db/migration/V1__init.sql
```

**Common pitfalls**

- Creating `com.example.app.Application` while code lives in a sibling package not scanned by default.
- Using package names like `controller`, `service`, `repository` only, which can make large systems hard to navigate.

## `@SpringBootApplication`

`@SpringBootApplication` is a convenience annotation composed of `@SpringBootConfiguration`, `@EnableAutoConfiguration`, and `@ComponentScan`. It declares the application configuration class, enables conditional auto-configuration, and scans for Spring-managed components from the current package downward.

**Interview focus**

- State the three composed annotations.
- Explain why moving the main class can make beans disappear.
- Explain how exclusions work for unwanted auto-configuration.

**Production checklist**

- [ ] Keep the main class minimal.
- [ ] Use `@ConfigurationPropertiesScan` when using typed config classes.
- [ ] Prefer property-driven customization before excluding auto-configurations.

**Code example**

```java
@SpringBootApplication
@ConfigurationPropertiesScan
public class DemoApplication {
    public static void main(String[] args) {
        SpringApplication.run(DemoApplication.class, args);
    }
}
```

**Common pitfalls**

- Putting business logic in the main class.
- Using broad `scanBasePackages` to paper over poor package organization.

## Stereotype annotations

`@RestController`, `@Service`, `@Repository`, and `@Component` all register beans, but they communicate intent. `@RestController` combines `@Controller` and `@ResponseBody`. `@Repository` participates in persistence exception translation. `@Service` marks business/application operations. `@Component` is the generic fallback.

**Interview focus**

- Explain why stereotypes are more than decoration.
- Know that `@Repository` can translate vendor exceptions into Spring data access exceptions.
- Explain `@Controller` versus `@RestController`.

**Production checklist**

- [ ] Use the most specific stereotype that matches intent.
- [ ] Keep controllers thin and services focused on use cases.
- [ ] Avoid making DTOs/entities Spring components.

**Code example**

```java
@RestController
@RequestMapping("/api/products")
class ProductController { }

@Service
class ProductService { }

@Repository
interface ProductRepository extends JpaRepository<ProductEntity, UUID> { }
```

**Common pitfalls**

- Annotating everything with `@Component` and losing semantic clarity.
- Putting validation, persistence, and HTTP logic in the same class.

## Dependency injection

Dependency injection lets objects declare dependencies instead of constructing them. Spring creates and wires beans in the application context. Constructor injection is the default recommendation because required dependencies are explicit, immutable, and test-friendly.

**Interview focus**

- Constructor injection versus field injection.
- Bean scopes and lifecycle basics.
- How Spring resolves multiple candidates with `@Primary` and `@Qualifier`.

**Production checklist**

- [ ] Use constructor injection, often with a single constructor and no `@Autowired` needed.
- [ ] Keep dependencies interfaces where a real alternate implementation exists.
- [ ] Use `@Qualifier` only when names are part of the design.

**Code example**

```java
@Service
class ProductService {
    private final ProductRepository products;

    ProductService(ProductRepository products) {
        this.products = products;
    }
}
```

**Common pitfalls**

- Field injection hides required collaborators and makes unit tests awkward.
- Injecting the application context into business code to manually look up beans.

## Bean lifecycle and configuration beans

Spring beans are created, dependency-injected, post-processed, initialized, and eventually destroyed with the application context. `@Configuration` classes and `@Bean` methods are useful for third-party objects or carefully constructed infrastructure components.

**Interview focus**

- Difference between component scanning and `@Bean` methods.
- When `@PostConstruct` is useful and when it hides work.
- What singleton means in Spring: one bean instance per application context, not global JVM singleton.

**Production checklist**

- [ ] Use `@Bean` for SDK clients, mappers, clocks, and infrastructure objects.
- [ ] Avoid expensive work in constructors; use lifecycle hooks sparingly.
- [ ] Make beans thread-safe unless scoped otherwise.

**Code example**

```java
@Configuration
class TimeConfig {
    @Bean
    Clock clock() {
        return Clock.systemUTC();
    }
}
```

**Common pitfalls**

- Doing network calls during bean creation without timeouts.
- Assuming request-scoped state can be stored in singleton fields.

## `application.yml`

`application.yml` is externalized configuration. Boot binds properties from files, environment variables, command-line arguments, config server sources, and more using a defined precedence order. Sensitive values should come from secret managers or environment variables, not source control.

**Interview focus**

- Property precedence and relaxed binding.
- YAML profile documents and `spring.config.activate.on-profile`.
- Typed configuration with `@ConfigurationProperties`.

**Production checklist**

- [ ] Keep defaults safe and non-secret.
- [ ] Prefer typed config records/classes to scattered `@Value` strings.
- [ ] Document required environment variables.

**Code example**

```yaml
server:
  port: ${SERVER_PORT:8080}
spring:
  application:
    name: inventory-service
app:
  external-tax-api:
    base-url: ${TAX_API_BASE_URL:http://localhost:9000}
    timeout: 2s
```

**Common pitfalls**

- Checking in passwords.
- Using `@Value` everywhere, making refactors and validation harder.

## Profiles

Profiles select environment-specific beans and configuration. They are useful for dev/test/prod differences, database vendors, local integrations, and test fakes. They should not create completely different applications.

**Interview focus**

- How `spring.profiles.active` is set.
- Difference between profile-specific files and profile conditions on beans.
- Why profile sprawl is dangerous.

**Production checklist**

- [ ] Name profiles by concern: `local`, `test`, `postgres`, `mssql`, `aws`.
- [ ] Keep business behavior consistent across profiles.
- [ ] Use tests to prove important profile combinations start.

**Code example**

```yaml
---
spring:
  config:
    activate:
      on-profile: local
logging:
  level:
    com.example: DEBUG
```

**Common pitfalls**

- Using profiles for feature flags that should be runtime-configurable.
- Only testing the local profile.

## Actuator basics

Spring Boot Actuator exposes operational endpoints for health, info, metrics, env, beans, mappings, conditions, and more. In production, expose only what operations need and secure sensitive endpoints.

**Interview focus**

- Health groups and readiness/liveness probes.
- Difference between application health and dependency health.
- Why `/actuator/env` and `/actuator/beans` are sensitive.

**Production checklist**

- [ ] Expose `health`, `info`, `metrics`, and `prometheus` intentionally.
- [ ] Use Kubernetes liveness/readiness probes carefully.
- [ ] Add useful build and git info when available.

**Code example**

```yaml
management:
  endpoints:
    web:
      exposure:
        include: health,info,metrics,prometheus
  endpoint:
    health:
      probes:
        enabled: true
      show-details: when_authorized
```

**Common pitfalls**

- Publicly exposing all Actuator endpoints.
- Marking service healthy while a required database dependency is unavailable.

## REST controllers

REST controllers translate HTTP concepts into application use cases. They should validate input, call services, map output, set correct status codes, and avoid leaking persistence concerns. REST is about resources, representations, idempotency, and status semantics.

**Interview focus**

- GET/POST/PUT/PATCH/DELETE semantics.
- Status codes for create, validation failure, not found, conflict, and no content.
- Request body DTOs versus path/query parameters.

**Production checklist**

- [ ] Use `@RequestMapping` at class level and method-specific annotations.
- [ ] Return `ResponseEntity` when status or headers matter.
- [ ] Keep controllers thin; business rules belong in services/domain.

**Code example**

```java
@PostMapping
ResponseEntity<ProductResponse> create(@Valid @RequestBody CreateProductRequest request) {
    ProductResponse created = service.create(request);
    URI location = URI.create("/api/products/" + created.id());
    return ResponseEntity.created(location).body(created);
}
```

**Common pitfalls**

- Returning 200 for every outcome.
- Encoding business errors as strings without machine-readable fields.

## Jakarta validation

Spring Boot 3 uses Jakarta namespaces (`jakarta.validation`) because Spring Framework 6 moved from Java EE to Jakarta EE. Bean Validation annotations on DTOs express input constraints and integrate with MVC when `@Valid` is used.

**Interview focus**

- `javax.*` versus `jakarta.*` in Boot 3.
- Difference between syntactic validation and business validation.
- Validation groups and custom validators at a high level.

**Production checklist**

- [ ] Validate external input at boundaries.
- [ ] Keep messages user-safe and avoid exposing internals.
- [ ] Still enforce database constraints; validation is not a substitute.

**Code example**

```java
public record CreateProductRequest(
    @NotBlank @Size(max = 120) String name,
    @NotBlank @Pattern(regexp = "[A-Z0-9-]{4,40}") String sku,
    @NotNull @Positive BigDecimal price
) {}
```

**Common pitfalls**

- Forgetting `@Valid` on controller parameters.
- Putting entity-specific validation on API DTOs that have different lifecycle needs.

## Exception handling with `@ControllerAdvice`

A global exception handler centralizes translation from Java exceptions to HTTP error responses. Spring 6 supports RFC 7807-style `ProblemDetail`, which is a strong default for consistent API errors.

**Interview focus**

- Difference between throwing domain exceptions and HTTP exceptions.
- How `MethodArgumentNotValidException` is handled.
- Why error response shape matters for clients.

**Production checklist**

- [ ] Map expected business exceptions to precise statuses.
- [ ] Log server errors, not every client validation failure.
- [ ] Include correlation/request IDs when available.

**Code example**

```java
@RestControllerAdvice
class GlobalExceptionHandler {
    @ExceptionHandler(ProductNotFoundException.class)
    ProblemDetail notFound(ProductNotFoundException ex) {
        ProblemDetail detail = ProblemDetail.forStatus(HttpStatus.NOT_FOUND);
        detail.setTitle("Product not found");
        detail.setDetail(ex.getMessage());
        return detail;
    }
}
```

**Common pitfalls**

- Catching `Exception` and returning 400 for everything.
- Leaking stack traces or SQL errors to clients.

## DTOs and API contracts

DTOs decouple external representations from persistence and domain internals. They allow versioned contracts, safe validation, controlled serialization, and prevention of over-posting attacks.

**Interview focus**

- Why returning entities is risky.
- Input DTOs versus output DTOs.
- Where mapping should live.

**Production checklist**

- [ ] Create request DTOs for commands and response DTOs for reads.
- [ ] Avoid exposing lazy-loaded relationships accidentally.
- [ ] Use immutable records for simple DTOs.

**Code example**

```java
public record ProductResponse(UUID id, String sku, String name, BigDecimal price, Instant updatedAt) {}
```

**Common pitfalls**

- Using one DTO for create, update, and response when fields differ.
- Letting JSON shape follow database shape by accident.

## OpenAPI and springdoc

OpenAPI documents HTTP contracts in a language-neutral format. `springdoc-openapi` can generate specs from Spring MVC annotations and validation metadata, but the generated result still needs review and explicit examples for high-value APIs.

**Interview focus**

- OpenAPI spec versus Swagger UI.
- How annotations document summary, responses, schemas, and security.
- Why generated docs are not a replacement for good API design.

**Production checklist**

- [ ] Document auth requirements and error responses.
- [ ] Add examples for complex request/response bodies.
- [ ] Publish the spec in CI for client teams.

**Code example**

```java
@Operation(summary = "Create a product")
@ApiResponse(responseCode = "201", description = "Product created")
@PostMapping
ResponseEntity<ProductResponse> create(@Valid @RequestBody CreateProductRequest request) {
    ProductResponse created = productService.create(request);
    URI location = URI.create("/api/products/" + created.id());
    return ResponseEntity.created(location).body(created);
}
```

**Common pitfalls**

- Relying on default generated names that do not help clients.
- Forgetting to document non-200 responses.

## Hands-on lab: minimal product API

1. Create a Boot 3 project with `spring-boot-starter-web`, `spring-boot-starter-validation`, and `spring-boot-starter-actuator`.
2. Add `ProductController` with `POST /api/products` and `GET /api/products/{id}`.
3. Use request/response records and validation annotations.
4. Implement a small in-memory service for the first iteration.
5. Add `GlobalExceptionHandler` returning `ProblemDetail`.
6. Add `management.endpoints.web.exposure.include=health,info,metrics`.
7. Add springdoc and verify `/v3/api-docs` and Swagger UI locally.

## Interview checklist

- [ ] I can expand `@SpringBootApplication` from memory.
- [ ] I can explain why package placement affects scanning.
- [ ] I can explain constructor injection and bean candidate resolution.
- [ ] I can design a controller that returns 201 with Location for resource creation.
- [ ] I can distinguish validation errors from business rule conflicts.
- [ ] I can describe `ProblemDetail` and why structured error payloads matter.
- [ ] I can describe profiles without turning them into feature flags.
- [ ] I can secure Actuator exposure at a high level.
- [ ] I can explain why DTOs protect API contracts.
- [ ] I can explain how OpenAPI helps API consumers and where generated specs fall short.
