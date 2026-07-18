package com.example.demo.product.persistence;

import jakarta.persistence.LockModeType;
import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.EntityGraph;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Lock;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;
import org.springframework.stereotype.Repository;

@Repository
public interface ProductRepository extends JpaRepository<ProductEntity, UUID> {

    Optional<ProductEntity> findBySku(String sku);

    boolean existsBySku(String sku);

    @EntityGraph(attributePaths = {"category", "tags"})
    @Query("select p from ProductEntity p where p.id = :id")
    Optional<ProductEntity> findDetailedById(@Param("id") UUID id);

    @Query("""
        select p
        from ProductEntity p
        where (:status is null or p.status = :status)
          and (:term is null or lower(p.name) like lower(concat('%', :term, '%')) or lower(p.sku) like lower(concat('%', :term, '%')))
        """)
    Page<ProductEntity> search(
        @Param("status") ProductStatus status,
        @Param("term") String term,
        Pageable pageable
    );

    @Query("""
        select p.id as id, p.sku as sku, p.name as name, p.price as price, p.status as status
        from ProductEntity p
        where p.status = com.example.demo.product.persistence.ProductStatus.ACTIVE
        order by p.name asc
        """)
    Page<ProductSummaryProjection> findActiveSummaries(Pageable pageable);

    @Lock(LockModeType.OPTIMISTIC_FORCE_INCREMENT)
    @Query("select p from ProductEntity p where p.id = :id")
    Optional<ProductEntity> findForStockAdjustment(@Param("id") UUID id);

    @Modifying(clearAutomatically = true, flushAutomatically = true)
    @Query("""
        update ProductEntity p
        set p.status = com.example.demo.product.persistence.ProductStatus.RETIRED,
            p.updatedAt = :now
        where p.status = com.example.demo.product.persistence.ProductStatus.ACTIVE
          and p.updatedAt < :cutoff
        """)
    int retireStaleProducts(@Param("cutoff") Instant cutoff, @Param("now") Instant now);

    @Query(value = """
        select p.*
        from product p
        where p.price between :minPrice and :maxPrice
        order by p.updated_at desc, p.id desc
        offset :offset rows fetch next :limit rows only
        """, nativeQuery = true)
    List<ProductEntity> findByPriceRangeNative(
        @Param("minPrice") BigDecimal minPrice,
        @Param("maxPrice") BigDecimal maxPrice,
        @Param("offset") int offset,
        @Param("limit") int limit
    );
}

interface ProductSummaryProjection {
    UUID getId();

    String getSku();

    String getName();

    BigDecimal getPrice();

    ProductStatus getStatus();
}
