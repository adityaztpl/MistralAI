# Angular Intermediate: Reactive Forms, RxJS, Interceptors, Guards, State, Signals, Change Detection

This level is about building maintainable application features: form workflows, HTTP concerns, route protection, lazy loading, state flow, and rendering behavior.

## 1. Reactive forms

Reactive forms define the form model in TypeScript. They are preferred when forms are complex, dynamic, validated heavily, or need unit tests.

```ts
import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-profile-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>
        Name
        <input formControlName="name" />
      </label>

      @if (form.controls.name.touched && form.controls.name.hasError('required')) {
        <p>Name is required.</p>
      }

      <label>
        Email
        <input formControlName="email" />
      </label>

      <button type="submit" [disabled]="form.invalid">Save</button>
    </form>
  `,
})
export class ProfileFormComponent {
  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
  });

  constructor(private readonly fb: FormBuilder) {}

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    console.log(this.form.getRawValue());
  }
}
```

### Typed forms

Angular supports strongly typed forms. Prefer `nonNullable` when empty `null` values are not meaningful.

```ts
const form = fb.nonNullable.group({
  title: ['', Validators.required],
  price: [0, [Validators.required, Validators.min(0)]],
});

// Type: { title: string; price: number }
const value = form.getRawValue();
```

### Custom validator

```ts
import { AbstractControl, ValidationErrors } from '@angular/forms';

export function strongPassword(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '');
  const valid =
    value.length >= 12 &&
    /[A-Z]/.test(value) &&
    /[a-z]/.test(value) &&
    /\d/.test(value);

  return valid ? null : { strongPassword: true };
}
```

Interview talking points:

- Template-driven forms put more logic in the template; reactive forms put the model in TypeScript.
- Reactive forms are easier to test because validators and form state are plain objects.
- Use `valueChanges` carefully: debounce, distinct, and unsubscribe or use `takeUntilDestroyed`.

---

## 2. RxJS operators

RxJS is central to Angular because HTTP, router events, form changes, and many Angular APIs use observables.

### Core mental model

- Observable: a stream over time.
- Observer/subscriber: receives values, errors, completion.
- Operator: transforms or controls a stream.
- Subscription: active execution that may need cleanup.

### Common operators

| Operator | Use case |
| --- | --- |
| `map` | Transform each value |
| `filter` | Ignore values that do not match |
| `tap` | Side effect such as logging |
| `debounceTime` | Wait for quiet time, often search input |
| `distinctUntilChanged` | Ignore repeated values |
| `switchMap` | Cancel previous inner work when a new value arrives |
| `mergeMap` | Run inner work concurrently |
| `concatMap` | Queue inner work in order |
| `exhaustMap` | Ignore new triggers while work is active |
| `catchError` | Recover from errors |
| `shareReplay` | Share and cache a source subscription |
| `combineLatest` | Combine latest values from multiple streams |

### Search example

```ts
results$ = this.searchControl.valueChanges.pipe(
  debounceTime(300),
  map((value) => value.trim()),
  distinctUntilChanged(),
  filter((value) => value.length >= 2),
  switchMap((query) =>
    this.products.search(query).pipe(
      catchError(() => of([])),
    ),
  ),
);
```

### Choosing flattening operators

Use this interview-friendly rule:

- `switchMap`: latest wins. Best for search and route-param-driven loading.
- `mergeMap`: all work matters. Best for independent writes or parallel tasks.
- `concatMap`: order matters. Best for sequential saves.
- `exhaustMap`: first wins until done. Best for login or submit button spam protection.

### Cleanup

Prefer template `AsyncPipe` or Angular lifecycle helpers:

```ts
import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export class NotificationsComponent {
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.notifications.stream$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((message) => this.show(message));
  }
}
```

---

## 3. HTTP interceptors

Interceptors observe and transform HTTP requests/responses globally.

Common uses:

- Add auth headers.
- Attach correlation IDs.
- Normalize API errors.
- Retry idempotent requests.
- Show/hide loading indicators.

Functional interceptor:

```ts
import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = localStorage.getItem('access_token');

  if (!token) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    }),
  );
};
```

Registering:

```ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(withInterceptors([authInterceptor]))],
});
```

Interview talking point:

> Http requests are immutable. Interceptors clone requests before changing headers, params, or body. This prevents surprising shared mutation across the chain.

---

## 4. Route guards

Guards control navigation. They should be fast, side-effect-light, and focused on routing decisions.

Common guard types:

- `CanActivateFn`: can this route be entered?
- `CanMatchFn`: can this route definition match? Useful for feature flags or auth.
- `CanDeactivateFn`: can user leave? Useful for unsaved changes.

Functional auth guard:

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isLoggedIn() || router.createUrlTree(['/login']);
};
```

Guard return values can be:

- `boolean`
- `UrlTree`
- `Observable<boolean | UrlTree>`
- `Promise<boolean | UrlTree>`

Interview note:

> A guard is not security by itself. It improves UX in the client, but authorization must be enforced on the server.

---

## 5. Lazy loading

Lazy loading reduces initial JavaScript by loading feature code only when needed.

Standalone component lazy route:

```ts
export const routes: Routes = [
  {
    path: 'settings',
    loadComponent: () =>
      import('./settings/settings.component').then((m) => m.SettingsComponent),
  },
];
```

Feature route lazy loading:

```ts
export const routes: Routes = [
  {
    path: 'admin',
    canMatch: [adminGuard],
    loadChildren: () =>
      import('./admin/admin.routes').then((m) => m.adminRoutes),
  },
];
```

Route-level providers:

```ts
{
  path: 'checkout',
  providers: [CheckoutSessionStore],
  loadComponent: () =>
    import('./checkout/checkout.component').then((m) => m.CheckoutComponent),
}
```

Interview checklist:

- Lazy loading improves initial load but can add navigation delay.
- Preloading strategies can load code after initial render.
- Route-level providers can scope feature state.
- Use `canMatch` to prevent matching lazy routes before loading them.

---

## 6. State with services and `BehaviorSubject`

For moderate app state, a service can act as a small store.

```ts
import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface CartItem {
  productId: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly itemsSubject = new BehaviorSubject<CartItem[]>([]);
  readonly items$ = this.itemsSubject.asObservable();

  add(productId: number): void {
    const current = this.itemsSubject.value;
    const existing = current.find((item) => item.productId === productId);

    const next = existing
      ? current.map((item) =>
          item.productId === productId
            ? { ...item, quantity: item.quantity + 1 }
            : item,
        )
      : [...current, { productId, quantity: 1 }];

    this.itemsSubject.next(next);
  }

  clear(): void {
    this.itemsSubject.next([]);
  }
}
```

Why `BehaviorSubject`:

- Holds current value.
- New subscribers receive the latest value.
- Works well with `AsyncPipe`.

Trade-offs:

- Easy to overuse mutable `.value`.
- No built-in devtools or action history.
- Large apps may need stricter patterns.

---

## 7. Signals intro

Signals are Angular's synchronous reactive state primitive.

```ts
import { computed, signal } from '@angular/core';

const quantity = signal(2);
const price = signal(10);
const total = computed(() => quantity() * price());

quantity.set(3);
console.log(total()); // 30
```

Key APIs:

- `signal(initialValue)`: writable state.
- `computed(() => expression)`: derived state.
- `effect(() => sideEffect())`: side effects when dependencies change.
- `toSignal(observable$)`: bridge observable to signal.
- `toObservable(signal)`: bridge signal to observable.

Component example:

```ts
@Component({
  standalone: true,
  template: `
    <p>{{ fullName() }}</p>
    <button (click)="rename()">Rename</button>
  `,
})
export class UserCardComponent {
  firstName = signal('Ada');
  lastName = signal('Lovelace');
  fullName = computed(() => `${this.firstName()} ${this.lastName()}`);

  rename(): void {
    this.firstName.set('Grace');
    this.lastName.set('Hopper');
  }
}
```

Signals versus observables:

- Signals are great for current synchronous state.
- Observables are great for asynchronous streams over time.
- They interoperate; choose based on the shape of the problem.

---

## 8. NgRx lite overview

NgRx is a Redux-inspired ecosystem for Angular. "NgRx lite" in interviews usually means understanding when a full store is useful and what concepts matter without over-engineering.

Core ideas:

- State is stored in a single or feature-level store.
- Components dispatch actions.
- Reducers calculate new immutable state.
- Selectors derive view data.
- Effects handle async work and dispatch results.

Tiny conceptual reducer:

```ts
export interface ProductsState {
  loading: boolean;
  products: Product[];
  error: string | null;
}

export const initialState: ProductsState = {
  loading: false,
  products: [],
  error: null,
};

export function productsReducer(state = initialState, action: ProductsAction): ProductsState {
  switch (action.type) {
    case 'Products Load':
      return { ...state, loading: true, error: null };
    case 'Products Load Success':
      return { ...state, loading: false, products: action.products };
    case 'Products Load Failure':
      return { ...state, loading: false, error: action.error };
    default:
      return state;
  }
}
```

When NgRx helps:

- Complex shared state across distant features.
- Need devtools, action logs, replay, or strict state transitions.
- Multiple async workflows modify the same state.
- Team benefits from conventions.

When a service store is enough:

- State is feature-local.
- Simple CRUD screens.
- Small team or small app.
- No need for action history.

Interview answer:

> I do not reach for NgRx by default. I start with component state and service state. I introduce NgRx when shared state, debugging needs, and workflow complexity justify the ceremony.

---

## 9. Change detection

Change detection is how Angular keeps the DOM in sync with application state.

### Default strategy

Angular checks a component tree after events, timers, HTTP completion, and other async tasks patched by Zone.js.

### OnPush

`ChangeDetectionStrategy.OnPush` skips checks unless:

- An input reference changes.
- An event happens in the component view.
- An observable bound with `AsyncPipe` emits.
- A signal read in the template changes.
- Code manually marks the view for check.

```ts
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-product-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h3>{{ product().name }}</h3>`,
})
export class ProductCardComponent {
  product = input.required<{ id: number; name: string }>();
}
```

Performance-friendly state updates:

```ts
this.products.update((products) =>
  products.map((product) =>
    product.id === id ? { ...product, selected: true } : product,
  ),
);
```

Interview talking points:

- OnPush is easier when data is immutable.
- Signals integrate naturally with OnPush because Angular tracks template signal reads.
- Avoid heavy function calls in templates.
- `track` in `@for` prevents unnecessary DOM churn.
- Change detection is not the same as rendering; Angular can check state without recreating DOM nodes.

---

## Intermediate interview drill

Practice concise answers:

1. Why choose reactive forms over template-driven forms?
2. Which RxJS flattening operator would you use for search? For save buttons?
3. How do interceptors preserve request immutability?
4. What should and should not live in a route guard?
5. How does lazy loading change the initial bundle?
6. When is a service store sufficient?
7. How do signals and observables differ?
8. What triggers an OnPush component to update?

---


## 10. Feature architecture at intermediate level

A maintainable feature usually separates routes, pages, API services, stores, and presentational components.

```text
features/products/
  products.routes.ts
  product-list.page.ts
  product-api.service.ts
  product-store.service.ts
  product-card.component.ts
```

Route-level providers are useful when feature state should reset with navigation.

```ts
export const productRoutes: Routes = [
  {
    path: '',
    providers: [ProductStore],
    loadComponent: () => import('./product-list.page').then((m) => m.ProductListPage),
  },
];
```

Interview checklist:

- Can you explain root versus route-level provider lifetime?
- Can you keep API mapping out of presentational components?
- Can you split a feature into page, store, API, and UI components?

## 11. HTTP error handling and retries

Interceptors handle cross-cutting transport concerns; feature services should still map domain errors.

```ts
getProduct(id: string) {
  return this.http.get<Product>(`/api/products/${id}`).pipe(
    catchError((error) => throwError(() => mapApiError(error))),
  );
}
```

Retry checklist:

- Retry idempotent reads when failure is likely transient.
- Avoid blind retry for writes unless the backend supports idempotency.
- Add backoff and user-facing failure state.
- Do not swallow errors globally in an interceptor.

## 12. Guards, resolvers, and route UX

Use guards for navigation decisions and resolvers for critical data that must exist before activation.

```ts
export const productResolver: ResolveFn<Product> = (route) => {
  const api = inject(ProductApi);
  return api.getProduct(route.paramMap.get('id')!);
};
```

Interview checklist:

- Return `UrlTree` instead of imperatively navigating from a guard.
- Use `canMatch` to prevent matching a lazy route.
- Avoid blocking navigation for large optional data.
- Remember server authorization is required.

## 13. Intermediate RxJS patterns

Stream shape matters more than memorizing operators.

```ts
readonly state$ = this.reloadClicks.pipe(
  startWith(undefined),
  switchMap(() =>
    this.api.list().pipe(
      map((data) => ({ loading: false, data, error: null })),
      startWith({ loading: true, data: [], error: null }),
      catchError(() => of({ loading: false, data: [], error: 'Load failed.' })),
    ),
  ),
);
```

Checklist:

- Catch errors inside `switchMap` for long-lived sources.
- Use `switchMap` for latest-wins workflows.
- Use `concatMap` when ordering matters.
- Use `exhaustMap` for duplicate submit protection.

## 14. Signal service stores

Signals make feature stores concise when the UI needs current state.

```ts
@Injectable()
export class ProductFiltersStore {
  private readonly query = signal('');
  private readonly category = signal<string | null>(null);
  readonly filters = computed(() => ({ query: this.query().trim(), category: this.category() }));
  setQuery(query: string): void { this.query.set(query); }
  setCategory(category: string | null): void { this.category.set(category); }
}
```

Store checklist:

- Keep writable signals private.
- Expose readonly signals or computed values.
- Use methods for state transitions.
- Use RxJS at async boundaries.

## 15. Change detection debugging

When UI is stale, ask:

1. Did state actually change?
2. Did the template read that state?
3. Was an object mutated in place?
4. Is the component OnPush?
5. Did an observable emit through `AsyncPipe` or `toSignal`?
6. In zoneless mode, was Angular notified?

Common fix:

```ts
this.items.update((items) => [...items, newItem]);
```

## 16. Intermediate anti-patterns

- Components that own unrelated API, auth, routing, validation, and rendering concerns.
- Public writable subjects or signals in services.
- Guards with complex side effects.
- `shareReplay` caches without refresh strategy.
- `track $index` for mutable server data.
- Manual subscriptions without cleanup.
