package com.example.demo;

import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.PositiveOrZero;
import jakarta.validation.constraints.Size;
import java.math.BigDecimal;
import java.net.URI;
import java.time.Clock;
import java.time.Instant;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.server.ResponseStatusException;

@RestController
@RequestMapping("/api/products")
@Tag(name = "Products", description = "Product catalog endpoints for Spring Boot interview practice")
class ProductController {

    private final ProductService productService;

    ProductController(ProductService productService) {
        this.productService = productService;
    }

    @Operation(summary = "Create a product", description = "Validates input, enforces unique SKU, and returns 201 with a Location header.")
    @ApiResponse(responseCode = "201", description = "Product created")
    @ApiResponse(responseCode = "400", description = "Validation error")
    @ApiResponse(responseCode = "409", description = "SKU already exists")
    @PostMapping
    ResponseEntity<ProductResponse> create(@Valid @RequestBody CreateProductRequest request) {
        ProductResponse created = productService.create(request);
        URI location = URI.create("/api/products/" + created.id());
        return ResponseEntity.created(location).body(created);
    }

    @Operation(summary = "Get product by id")
    @GetMapping("/{id}")
    ProductResponse getById(@PathVariable UUID id) {
        return productService.getById(id);
    }

    @Operation(summary = "Search products by optional text")
    @GetMapping
    List<ProductResponse> search(@RequestParam(defaultValue = "") String q) {
        return productService.search(q);
    }

    @Operation(summary = "Replace mutable product fields")
    @PutMapping("/{id}")
    ProductResponse update(@PathVariable UUID id, @Valid @RequestBody UpdateProductRequest request) {
        return productService.update(id, request);
    }
}

@Service
class ProductService {

    private final ProductCatalog catalog;
    private final Clock clock;

    ProductService(ProductCatalog catalog, Clock clock) {
        this.catalog = catalog;
        this.clock = clock;
    }

    ProductResponse create(CreateProductRequest request) {
        String normalizedSku = normalizeSku(request.sku());
        if (catalog.findBySku(normalizedSku).isPresent()) {
            throw new DuplicateSkuException(normalizedSku);
        }

        Product product = new Product(
            UUID.randomUUID(),
            normalizedSku,
            request.name().trim(),
            request.description(),
            request.price(),
            request.quantityOnHand(),
            Instant.now(clock),
            Instant.now(clock)
        );
        catalog.save(product);
        return ProductResponse.from(product);
    }

    ProductResponse getById(UUID id) {
        return catalog.findById(id)
            .map(ProductResponse::from)
            .orElseThrow(() -> new ProductNotFoundException(id));
    }

    List<ProductResponse> search(String query) {
        String normalized = query == null ? "" : query.trim().toLowerCase(Locale.ROOT);
        return catalog.findAll().stream()
            .filter(product -> normalized.isBlank()
                || product.name().toLowerCase(Locale.ROOT).contains(normalized)
                || product.sku().toLowerCase(Locale.ROOT).contains(normalized))
            .sorted(Comparator.comparing(Product::name))
            .map(ProductResponse::from)
            .toList();
    }

    ProductResponse update(UUID id, UpdateProductRequest request) {
        Product existing = catalog.findById(id).orElseThrow(() -> new ProductNotFoundException(id));
        Product updated = new Product(
            existing.id(),
            existing.sku(),
            request.name().trim(),
            request.description(),
            request.price(),
            request.quantityOnHand(),
            existing.createdAt(),
            Instant.now(clock)
        );
        catalog.save(updated);
        return ProductResponse.from(updated);
    }

    private String normalizeSku(String sku) {
        return sku.trim().toUpperCase(Locale.ROOT);
    }
}

@Service
class ProductCatalog {

    private final Map<UUID, Product> productsById = new ConcurrentHashMap<>();
    private final Map<String, UUID> idsBySku = new ConcurrentHashMap<>();

    Optional<Product> findById(UUID id) {
        return Optional.ofNullable(productsById.get(id));
    }

    Optional<Product> findBySku(String sku) {
        UUID id = idsBySku.get(sku);
        return id == null ? Optional.empty() : findById(id);
    }

    List<Product> findAll() {
        return List.copyOf(productsById.values());
    }

    void save(Product product) {
        productsById.put(product.id(), product);
        idsBySku.put(product.sku(), product.id());
    }
}

record Product(
    UUID id,
    String sku,
    String name,
    String description,
    BigDecimal price,
    int quantityOnHand,
    Instant createdAt,
    Instant updatedAt
) {
}

@Schema(name = "CreateProductRequest")
record CreateProductRequest(
    @NotBlank
    @Pattern(regexp = "[A-Za-z0-9-]{4,40}", message = "SKU must be 4-40 letters, digits, or dashes")
    String sku,

    @NotBlank
    @Size(max = 120)
    String name,

    @Size(max = 2000)
    String description,

    @NotNull
    @DecimalMin(value = "0.01")
    BigDecimal price,

    @PositiveOrZero
    int quantityOnHand
) {
}

@Schema(name = "UpdateProductRequest")
record UpdateProductRequest(
    @NotBlank
    @Size(max = 120)
    String name,

    @Size(max = 2000)
    String description,

    @NotNull
    @DecimalMin(value = "0.01")
    BigDecimal price,

    @PositiveOrZero
    int quantityOnHand
) {
}

record ProductResponse(
    UUID id,
    String sku,
    String name,
    String description,
    BigDecimal price,
    int quantityOnHand,
    Instant createdAt,
    Instant updatedAt,
    Map<String, String> links
) {
    static ProductResponse from(Product product) {
        Map<String, String> links = new LinkedHashMap<>();
        links.put("self", "/api/products/" + product.id());
        links.put("collection", "/api/products");
        return new ProductResponse(
            product.id(),
            product.sku(),
            product.name(),
            product.description(),
            product.price(),
            product.quantityOnHand(),
            product.createdAt(),
            product.updatedAt(),
            links
        );
    }
}

@ResponseStatus(HttpStatus.NOT_FOUND)
class ProductNotFoundException extends RuntimeException {
    ProductNotFoundException(UUID id) {
        super("Product %s was not found".formatted(id));
    }
}

@ResponseStatus(HttpStatus.CONFLICT)
class DuplicateSkuException extends RuntimeException {
    DuplicateSkuException(String sku) {
        super("Product SKU %s already exists".formatted(sku));
    }
}
