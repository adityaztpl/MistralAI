import { Injectable, computed, signal } from '@angular/core';

export interface ShopProduct {
  id: string;
  name: string;
  category: string;
  price: number;
  rating: number;
}

export interface CartLine {
  product: ShopProduct;
  quantity: number;
}

export interface ShopFilters {
  query: string;
  category: string | null;
  minRating: number;
}

const initialProducts: ShopProduct[] = [
  { id: 'keyboard', name: 'Mechanical Keyboard', category: 'accessories', price: 129, rating: 5 },
  { id: 'mouse', name: 'Wireless Mouse', category: 'accessories', price: 59, rating: 4 },
  { id: 'monitor', name: '4K Monitor', category: 'displays', price: 399, rating: 5 },
  { id: 'stand', name: 'Laptop Stand', category: 'workspace', price: 49, rating: 4 },
];

@Injectable({ providedIn: 'root' })
export class ShopStore {
  private readonly products = signal<ShopProduct[]>(initialProducts);
  private readonly cart = signal<CartLine[]>([]);
  private readonly filters = signal<ShopFilters>({ query: '', category: null, minRating: 0 });

  readonly allProducts = this.products.asReadonly();
  readonly cartLines = this.cart.asReadonly();
  readonly currentFilters = this.filters.asReadonly();

  readonly categories = computed(() =>
    Array.from(new Set(this.products().map((product) => product.category))).sort(),
  );

  readonly filteredProducts = computed(() => {
    const { query, category, minRating } = this.filters();
    const normalizedQuery = query.trim().toLowerCase();

    return this.products().filter((product) => {
      const matchesQuery =
        normalizedQuery.length === 0 ||
        product.name.toLowerCase().includes(normalizedQuery);
      const matchesCategory = category === null || product.category === category;
      const matchesRating = product.rating >= minRating;

      return matchesQuery && matchesCategory && matchesRating;
    });
  });

  readonly cartCount = computed(() =>
    this.cart().reduce((total, line) => total + line.quantity, 0),
  );

  readonly cartSubtotal = computed(() =>
    this.cart().reduce((total, line) => total + line.product.price * line.quantity, 0),
  );

  readonly viewModel = computed(() => ({
    products: this.filteredProducts(),
    categories: this.categories(),
    filters: this.filters(),
    cartLines: this.cart(),
    cartCount: this.cartCount(),
    cartSubtotal: this.cartSubtotal(),
    emptyMessage:
      this.filteredProducts().length === 0
        ? 'No products match the current filters.'
        : null,
  }));

  setQuery(query: string): void {
    this.filters.update((filters) => ({ ...filters, query }));
  }

  setCategory(category: string | null): void {
    this.filters.update((filters) => ({ ...filters, category }));
  }

  setMinRating(minRating: number): void {
    this.filters.update((filters) => ({
      ...filters,
      minRating: Math.max(0, Math.min(5, Math.trunc(minRating))),
    }));
  }

  clearFilters(): void {
    this.filters.set({ query: '', category: null, minRating: 0 });
  }

  addToCart(productId: string): void {
    const product = this.products().find((candidate) => candidate.id === productId);

    if (!product) {
      return;
    }

    this.cart.update((lines) => {
      const existing = lines.find((line) => line.product.id === productId);

      if (existing) {
        return lines.map((line) =>
          line.product.id === productId
            ? { ...line, quantity: line.quantity + 1 }
            : line,
        );
      }

      return [...lines, { product, quantity: 1 }];
    });
  }

  setQuantity(productId: string, quantity: number): void {
    const normalizedQuantity = Math.max(0, Math.trunc(quantity));

    this.cart.update((lines) => {
      if (normalizedQuantity === 0) {
        return lines.filter((line) => line.product.id !== productId);
      }

      return lines.map((line) =>
        line.product.id === productId
          ? { ...line, quantity: normalizedQuantity }
          : line,
      );
    });
  }
}
