package com.example.catalog.products;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.Locale;
import java.util.Set;
import java.util.UUID;

import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageRequest;
import org.springframework.data.domain.Pageable;
import org.springframework.data.domain.Sort;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class ProductService {
    private static final int MAX_PAGE_SIZE = 100;
    private static final Set<String> ALLOWED_SORT_FIELDS = Set.of("name", "sku", "price", "createdAt", "updatedAt");

    private final ProductRepository productRepository;
    private final ProductMapper productMapper;

    public ProductService(ProductRepository productRepository, ProductMapper productMapper) {
        this.productRepository = productRepository;
        this.productMapper = productMapper;
    }

    @Transactional(readOnly = true)
    public PageResponse<ProductResponse> search(ProductSearchRequest request, AuthenticatedUser user) {
        Pageable pageable = toPageable(request.page(), request.size(), request.sortField(), request.sortDirection());

        Page<Product> page = productRepository.findAll(
            ProductSpecifications.visibleTo(user.id())
                .and(ProductSpecifications.hasStatus(request.status()))
                .and(ProductSpecifications.matchesQuery(request.query())),
            pageable
        );

        return PageResponse.from(page.map(productMapper::toResponse));
    }

    @Transactional(readOnly = true)
    public ProductResponse get(UUID productId, AuthenticatedUser user) {
        Product product = productRepository.findByIdAndOwnerId(productId, user.id())
            .orElseThrow(() -> new ProductNotFoundException(productId));

        return productMapper.toResponse(product);
    }

    @Transactional
    public ProductResponse create(ProductCreateRequest request, AuthenticatedUser user) {
        if (productRepository.existsByOwnerIdAndSku(user.id(), normalizeSku(request.sku()))) {
            throw new DuplicateSkuException(request.sku());
        }

        Product product = new Product();
        product.setId(UUID.randomUUID());
        product.setOwnerId(user.id());
        product.setSku(normalizeSku(request.sku()));
        product.setName(request.name().trim());
        product.setDescription(request.description());
        product.setPrice(requireNonNegative(request.price(), "price"));
        product.setCurrency(request.currency().toUpperCase(Locale.ROOT));
        product.setQuantity(request.quantity());
        product.setStatus(ProductStatus.ACTIVE);
        product.setCreatedAt(Instant.now());
        product.setUpdatedAt(Instant.now());

        Product saved = productRepository.save(product);
        return productMapper.toResponse(saved);
    }

    @Transactional
    public ProductResponse update(UUID productId, ProductUpdateRequest request, AuthenticatedUser user) {
        Product product = productRepository.findByIdAndOwnerId(productId, user.id())
            .orElseThrow(() -> new ProductNotFoundException(productId));

        String newSku = normalizeSku(request.sku());
        if (!product.getSku().equals(newSku) && productRepository.existsByOwnerIdAndSku(user.id(), newSku)) {
            throw new DuplicateSkuException(request.sku());
        }

        product.setSku(newSku);
        product.setName(request.name().trim());
        product.setDescription(request.description());
        product.setPrice(requireNonNegative(request.price(), "price"));
        product.setCurrency(request.currency().toUpperCase(Locale.ROOT));
        product.setQuantity(request.quantity());
        product.setUpdatedAt(Instant.now());

        return productMapper.toResponse(product);
    }

    @Transactional
    public void archive(UUID productId, AuthenticatedUser user) {
        Product product = productRepository.findByIdAndOwnerId(productId, user.id())
            .orElseThrow(() -> new ProductNotFoundException(productId));

        product.archive();
        product.setUpdatedAt(Instant.now());
    }

    private Pageable toPageable(int page, int size, String sortField, Sort.Direction direction) {
        String safeSortField = sortField == null || sortField.isBlank() ? "createdAt" : sortField;
        if (!ALLOWED_SORT_FIELDS.contains(safeSortField)) {
            throw new InvalidSortException(safeSortField);
        }

        int safePage = Math.max(page, 0);
        int safeSize = Math.min(Math.max(size, 1), MAX_PAGE_SIZE);
        Sort.Direction safeDirection = direction == null ? Sort.Direction.DESC : direction;

        return PageRequest.of(safePage, safeSize, Sort.by(safeDirection, safeSortField));
    }

    private static String normalizeSku(String sku) {
        return sku.trim().toUpperCase(Locale.ROOT);
    }

    private static BigDecimal requireNonNegative(BigDecimal value, String fieldName) {
        if (value.signum() < 0) {
            throw new IllegalArgumentException(fieldName + " cannot be negative");
        }
        return value;
    }
}
