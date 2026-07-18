package com.example.demo.product.persistence;

import jakarta.persistence.CascadeType;
import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.FetchType;
import jakarta.persistence.Id;
import jakarta.persistence.Index;
import jakarta.persistence.JoinColumn;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.OneToMany;
import jakarta.persistence.PrePersist;
import jakarta.persistence.PreUpdate;
import jakarta.persistence.Table;
import jakarta.persistence.Version;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.LinkedHashSet;
import java.util.Objects;
import java.util.Set;
import java.util.UUID;

@Entity
@Table(
    name = "product",
    indexes = {
        @Index(name = "ux_product_sku", columnList = "sku", unique = true),
        @Index(name = "ix_product_status_updated", columnList = "status, updated_at")
    }
)
public class ProductEntity {

    @Id
    @Column(nullable = false, updatable = false)
    private UUID id;

    @Column(nullable = false, length = 40, unique = true)
    private String sku;

    @Column(nullable = false, length = 120)
    private String name;

    @Column(length = 2000)
    private String description;

    @Column(nullable = false, precision = 12, scale = 2)
    private BigDecimal price;

    @Column(name = "quantity_on_hand", nullable = false)
    private int quantityOnHand;

    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 20)
    private ProductStatus status = ProductStatus.DRAFT;

    @Version
    @Column(nullable = false)
    private long version;

    @Column(name = "created_at", nullable = false, updatable = false)
    private Instant createdAt;

    @Column(name = "updated_at", nullable = false)
    private Instant updatedAt;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "category_id", nullable = false)
    private CategoryEntity category;

    @OneToMany(mappedBy = "product", cascade = CascadeType.ALL, orphanRemoval = true)
    private Set<ProductTagEntity> tags = new LinkedHashSet<>();

    protected ProductEntity() {
    }

    public ProductEntity(UUID id, String sku, String name, BigDecimal price, CategoryEntity category) {
        this.id = Objects.requireNonNull(id, "id is required");
        this.sku = requireText(sku, "sku");
        this.name = requireText(name, "name");
        this.price = requirePositive(price);
        this.category = Objects.requireNonNull(category, "category is required");
    }

    @PrePersist
    void prePersist() {
        Instant now = Instant.now();
        createdAt = now;
        updatedAt = now;
    }

    @PreUpdate
    void preUpdate() {
        updatedAt = Instant.now();
    }

    public void publish() {
        if (price.signum() <= 0) {
            throw new IllegalStateException("Published products must have a positive price");
        }
        if (quantityOnHand < 0) {
            throw new IllegalStateException("Published products cannot have negative stock");
        }
        status = ProductStatus.ACTIVE;
    }

    public void retire() {
        status = ProductStatus.RETIRED;
    }

    public void adjustStock(int delta) {
        int nextQuantity = quantityOnHand + delta;
        if (nextQuantity < 0) {
            throw new IllegalArgumentException("Insufficient stock for SKU " + sku);
        }
        quantityOnHand = nextQuantity;
    }

    public void addTag(String name) {
        ProductTagEntity tag = new ProductTagEntity(UUID.randomUUID(), this, requireText(name, "tag name"));
        tags.add(tag);
    }

    public void removeTag(String name) {
        tags.removeIf(tag -> tag.getName().equalsIgnoreCase(name));
    }

    public UUID getId() {
        return id;
    }

    public String getSku() {
        return sku;
    }

    public String getName() {
        return name;
    }

    public BigDecimal getPrice() {
        return price;
    }

    public ProductStatus getStatus() {
        return status;
    }

    public long getVersion() {
        return version;
    }

    public CategoryEntity getCategory() {
        return category;
    }

    public Set<ProductTagEntity> getTags() {
        return Set.copyOf(tags);
    }

    private static String requireText(String value, String field) {
        if (value == null || value.isBlank()) {
            throw new IllegalArgumentException(field + " is required");
        }
        return value.trim();
    }

    private static BigDecimal requirePositive(BigDecimal value) {
        if (value == null || value.signum() <= 0) {
            throw new IllegalArgumentException("price must be positive");
        }
        return value;
    }
}

@Entity
@Table(name = "category")
class CategoryEntity {

    @Id
    @Column(nullable = false, updatable = false)
    private UUID id;

    @Column(nullable = false, unique = true, length = 80)
    private String code;

    @Column(nullable = false, length = 120)
    private String displayName;

    protected CategoryEntity() {
    }

    CategoryEntity(UUID id, String code, String displayName) {
        this.id = id;
        this.code = code;
        this.displayName = displayName;
    }

    UUID getId() {
        return id;
    }
}

@Entity
@Table(name = "product_tag")
class ProductTagEntity {

    @Id
    @Column(nullable = false, updatable = false)
    private UUID id;

    @ManyToOne(fetch = FetchType.LAZY, optional = false)
    @JoinColumn(name = "product_id", nullable = false)
    private ProductEntity product;

    @Column(nullable = false, length = 60)
    private String name;

    protected ProductTagEntity() {
    }

    ProductTagEntity(UUID id, ProductEntity product, String name) {
        this.id = id;
        this.product = product;
        this.name = name;
    }

    String getName() {
        return name;
    }
}

enum ProductStatus {
    DRAFT,
    ACTIVE,
    RETIRED
}
