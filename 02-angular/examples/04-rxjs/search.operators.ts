import { HttpClient, HttpParams } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, Injectable, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { Observable, catchError, debounceTime, distinctUntilChanged, map, of, shareReplay, startWith, switchMap } from 'rxjs';

export interface SearchProduct {
  id: number;
  name: string;
  price: number;
}

export type SearchStatus = 'idle' | 'loading' | 'success' | 'error';

export interface SearchState {
  status: SearchStatus;
  query: string;
  products: SearchProduct[];
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProductSearchApi {
  private readonly http = inject(HttpClient);

  search(query: string): Observable<SearchProduct[]> {
    const params = new HttpParams().set('q', query);
    return this.http.get<SearchProduct[]>('/api/products/search', { params });
  }
}

@Component({
  selector: 'app-product-search',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section>
      <label for="product-search">Search products</label>
      <input id="product-search" type="search" [formControl]="queryControl" />

      @if (state(); as vm) {
        @switch (vm.status) {
          @case ('idle') {
            <p>Type at least two characters.</p>
          }
          @case ('loading') {
            <p>Searching for "{{ vm.query }}"...</p>
          }
          @case ('error') {
            <p role="alert">{{ vm.error }}</p>
          }
          @case ('success') {
            @if (vm.products.length === 0) {
              <p>No products matched "{{ vm.query }}".</p>
            } @else {
              <ul>
                @for (product of vm.products; track product.id) {
                  <li>{{ product.name }} - {{ product.price }}</li>
                }
              </ul>
            }
          }
        }
      }
    </section>
  `,
})
export class ProductSearchComponent {
  private readonly productSearchApi = inject(ProductSearchApi);

  readonly queryControl = new FormControl('', { nonNullable: true });

  readonly searchState$ = this.queryControl.valueChanges.pipe(
    startWith(this.queryControl.value),
    map((query) => query.trim()),
    debounceTime(250),
    distinctUntilChanged(),
    switchMap((query): Observable<SearchState> => {
      if (query.length < 2) {
        return of({ status: 'idle', query, products: [], error: null });
      }

      return this.productSearchApi.search(query).pipe(
        map((products): SearchState => ({
          status: 'success',
          query,
          products,
          error: null,
        })),
        startWith({
          status: 'loading',
          query,
          products: [],
          error: null,
        } satisfies SearchState),
        catchError(() =>
          of({
            status: 'error',
            query,
            products: [],
            error: 'Search failed. Please try again.',
          } satisfies SearchState),
        ),
      );
    }),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  readonly state = toSignal(this.searchState$, {
    initialValue: { status: 'idle', query: '', products: [], error: null },
  });
}
