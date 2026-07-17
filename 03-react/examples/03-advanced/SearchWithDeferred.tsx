import { useDeferredValue, useMemo, useState } from 'react';

type Product = {
  id: string;
  name: string;
  category: string;
};

type SearchWithDeferredProps = {
  products: Product[];
};

export function SearchWithDeferred({ products }: SearchWithDeferredProps) {
  const [query, setQuery] = useState('');
  const deferredQuery = useDeferredValue(query);

  const filteredProducts = useMemo(() => {
    const normalizedQuery = deferredQuery.trim().toLowerCase();

    if (!normalizedQuery) {
      return products;
    }

    return products.filter((product) =>
      `${product.name} ${product.category}`.toLowerCase().includes(normalizedQuery),
    );
  }, [deferredQuery, products]);

  const isStale = query !== deferredQuery;

  return (
    <section>
      <label htmlFor="product-search">Search products</label>
      <input
        id="product-search"
        value={query}
        onChange={(event) => setQuery(event.target.value)}
      />

      <p aria-live="polite">
        Showing {filteredProducts.length} of {products.length}
      </p>

      <ul style={{ opacity: isStale ? 0.6 : 1 }}>
        {filteredProducts.map((product) => (
          <li key={product.id}>
            {product.name} <small>({product.category})</small>
          </li>
        ))}
      </ul>
    </section>
  );
}
