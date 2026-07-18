# 04 - Angular with Spring Boot

This guide shows how to connect an Angular standalone application to a Spring Boot API. It focuses on modern Angular patterns: standalone components, functional interceptors, route guards, typed services, environments, reactive forms, and Spring Problem Details validation.

## Recommended stack

- Angular 17+ or 18+
- Standalone components
- Angular Router
- Angular `HttpClient`
- Functional interceptors
- Reactive Forms
- Signals for local UI state where useful
- RxJS for async streams and HTTP composition
- Jasmine/Karma or Jest for unit tests
- Playwright or Cypress for E2E tests

Create app:

```bash
npm create @angular@latest product-catalog-web
cd product-catalog-web
ng add @angular-eslint/schematics
```

Or:

```bash
ng new product-catalog-web --standalone --routing --style=scss
```

## Folder structure

```text
src/app/
  app.config.ts
  app.routes.ts
  core/
    auth/
      auth.service.ts
      auth.interceptor.ts
      auth.guard.ts
      auth.models.ts
    api/
      problem-details.ts
      api-error.service.ts
  features/
    products/
      product-list.page.ts
      product-detail.page.ts
      product-form.component.ts
      products.service.ts
      product.models.ts
  shared/
    components/
    form-errors/
environments/
  environment.ts
  environment.development.ts
```

Use feature folders for business areas and `core` for singleton cross-cutting services.

## Environment configuration

`environment.development.ts`:

```ts
export const environment = {
  production: false,
  apiBaseUrl: "http://localhost:8080",
  authStorageKey: "product-catalog-auth",
};
```

`environment.ts`:

```ts
export const environment = {
  production: true,
  apiBaseUrl: "https://api.example.com",
  authStorageKey: "product-catalog-auth",
};
```

Important:

- Angular environment values are bundled into browser JavaScript.
- Do not put secrets in Angular environment files.
- Production environment values should be public runtime config only.

For containerized deployments where the API URL changes per environment, consider a runtime `assets/config.json` loaded before bootstrap instead of rebuilding the SPA for every environment.

## Auth service

Core responsibilities:

- login/register calls
- token storage
- current user state
- logout
- expiration checks

Signal-based sketch:

```ts
@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly storageKey = environment.authStorageKey;
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly authState = signal<AuthState>(this.loadStoredState());
  readonly isAuthenticated = computed(() => Boolean(this.authState().accessToken));

  login(request: LoginRequest): Observable<AuthState> {
    return this.http.post<AuthState>(`${environment.apiBaseUrl}/api/v1/auth/login`, request).pipe(
      tap((state) => this.setAuthState(state))
    );
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    this.authState.set({ accessToken: null, expiresAt: null, user: null });
    this.router.navigateByUrl("/login");
  }

  token(): string | null {
    return this.authState().accessToken;
  }

  private setAuthState(state: AuthState): void {
    localStorage.setItem(this.storageKey, JSON.stringify(state));
    this.authState.set(state);
  }

  private loadStoredState(): AuthState {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) return { accessToken: null, expiresAt: null, user: null };
    try {
      return JSON.parse(raw) as AuthState;
    } catch {
      localStorage.removeItem(this.storageKey);
      return { accessToken: null, expiresAt: null, user: null };
    }
  }
}
```

Production caveat: localStorage is JavaScript-readable. For production, discuss Cognito PKCE, secure cookies, refresh token rotation, or BFF.

## Functional interceptor

See [`examples/angular-client/auth.interceptor.ts`](examples/angular-client/auth.interceptor.ts).

```ts
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const authRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.logout();
      }
      return throwError(() => error);
    })
  );
};
```

Register in `app.config.ts`:

```ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
};
```

Interceptor rules:

- Add auth headers in one place.
- Handle global `401` in one place.
- Do not swallow errors silently.
- Avoid creating infinite login/logout navigation loops.

## Route guards

See [`examples/angular-client/auth.guard.ts`](examples/angular-client/auth.guard.ts).

```ts
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(["/login"], {
    queryParams: { returnUrl: state.url },
  });
};
```

Routes:

```ts
export const routes: Routes = [
  { path: "login", loadComponent: () => import("./core/auth/login.page").then(m => m.LoginPage) },
  {
    path: "products",
    canActivate: [authGuard],
    loadChildren: () => import("./features/products/product.routes").then(m => m.PRODUCT_ROUTES),
  },
  { path: "", pathMatch: "full", redirectTo: "products" },
];
```

Route guards are not security. They prevent accidental navigation and improve UX. The API must enforce authorization.

## Typed product service

```ts
@Injectable({ providedIn: "root" })
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/products`;

  list(params: ProductSearchParams): Observable<PageResponse<ProductSummary>> {
    const httpParams = new HttpParams()
      .set("page", params.page)
      .set("size", params.size)
      .set("sort", `${params.sortField},${params.sortDirection}`)
      .set("query", params.query ?? "");

    return this.http.get<PageResponse<ProductSummary>>(this.baseUrl, { params: httpParams });
  }

  get(id: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/${id}`);
  }

  create(request: ProductCreateRequest): Observable<ProductDetail> {
    return this.http.post<ProductDetail>(this.baseUrl, request);
  }

  update(id: string, request: ProductUpdateRequest): Observable<ProductDetail> {
    return this.http.put<ProductDetail>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

Keep HTTP in services, not components.

## Problem Details type

```ts
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
```

Angular `HttpErrorResponse.error` contains the response body. Check shape before assuming it is Problem Details because network errors or proxies may return non-JSON.

```ts
export function isProblemDetails(value: unknown): value is ProblemDetails {
  return Boolean(
    value &&
      typeof value === "object" &&
      "title" in value &&
      "status" in value
  );
}
```

## Reactive forms against Spring validation

Form:

```ts
readonly form = this.fb.nonNullable.group({
  name: ["", [Validators.required, Validators.maxLength(80)]],
  sku: ["", [Validators.required, Validators.maxLength(40)]],
  price: [0, [Validators.required, Validators.min(0)]],
  currency: ["USD", [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
  quantity: [0, [Validators.required, Validators.min(0)]],
});
```

Submit:

```ts
save(): void {
  if (this.form.invalid) {
    this.form.markAllAsTouched();
    return;
  }

  this.products.create(this.form.getRawValue()).subscribe({
    next: (product) => this.router.navigate(["/products", product.id]),
    error: (error: HttpErrorResponse) => this.applyApiError(error),
  });
}
```

Apply server errors:

```ts
private applyApiError(error: HttpErrorResponse): void {
  const problem = error.error;
  if (!isProblemDetails(problem)) {
    this.globalError.set("Something went wrong. Please try again.");
    return;
  }

  Object.entries(problem.errors ?? {}).forEach(([field, messages]) => {
    const control = this.form.get(field);
    if (control) {
      control.setErrors({
        ...(control.errors ?? {}),
        server: messages.join(" "),
      });
    }
  });

  if (!problem.errors) {
    this.globalError.set(problem.detail ?? problem.title);
  }
}
```

Template:

```html
<input formControlName="sku" />
@if (form.controls.sku.errors?.["server"]) {
  <p class="field-error">{{ form.controls.sku.errors?.["server"] }}</p>
}
```

## Error handling strategy

| Error | UI behavior |
|---|---|
| `400` validation | Show field errors |
| `401` | Clear auth and redirect to login |
| `403` | Show unauthorized page/message |
| `404` | Show not found view |
| `409` | Show conflict message, keep form values |
| `500` | Show generic error and trace ID |

Avoid showing raw stack traces or backend exception class names to users.

## Loading state

For simple components:

```ts
readonly loading = signal(false);
readonly error = signal<string | null>(null);
readonly products = signal<ProductSummary[]>([]);
```

For observable-heavy screens, use a view model stream:

```ts
readonly vm$ = this.searchParams$.pipe(
  switchMap((params) =>
    this.productsService.list(params).pipe(
      map((page) => ({ state: "loaded" as const, page })),
      startWith({ state: "loading" as const }),
      catchError((error) => of({ state: "error" as const, error }))
    )
  )
);
```

Use `switchMap` for search/filter requests so stale requests do not overwrite newer results.

## Streaming with Angular

Angular `HttpClient` can observe progress for downloads/uploads, but browser-native SSE is often simpler for server-to-client events.

```ts
streamJob(jobId: string): Observable<JobProgress> {
  return new Observable<JobProgress>((subscriber) => {
    const source = new EventSource(`${environment.apiBaseUrl}/api/v1/import-jobs/${jobId}/events`);

    source.addEventListener("progress", (event) => {
      subscriber.next(JSON.parse((event as MessageEvent).data));
    });

    source.onerror = (error) => {
      subscriber.error(error);
      source.close();
    };

    return () => source.close();
  });
}
```

Bearer token caveat: native `EventSource` cannot set custom Authorization headers. Use cookie auth, a signed stream URL, a polyfill, fetch streams, or WebSocket when needed.

## CORS with Angular local dev

```text
Angular: http://localhost:4200
Spring:  http://localhost:8080
```

Spring CORS should allow `http://localhost:4200` in development. Production should allow only your CloudFront/app domain.

Angular proxy alternative for local dev:

`proxy.conf.json`:

```json
{
  "/api": {
    "target": "http://localhost:8080",
    "secure": false,
    "changeOrigin": true
  }
}
```

Run:

```bash
ng serve --proxy-config proxy.conf.json
```

This reduces local CORS friction but does not remove the need for production CORS configuration.

## Build and AWS hosting

Build:

```bash
ng build --configuration production
```

Deploy contents of:

```text
dist/<project-name>/browser/
```

CloudFront/S3 settings:

- Serve `index.html` for unknown routes.
- Cache hashed JS/CSS assets aggressively.
- Keep `index.html` short-lived or invalidate it on deploy.
- Set security headers with CloudFront response headers policy.

## Testing strategy

| Test | Focus |
|---|---|
| Component | Form validation, table rendering, error display |
| Service | HTTP params, API URLs, response mapping |
| Interceptor | Authorization header, 401 handling |
| Guard | Authenticated vs unauthenticated navigation |
| E2E | Login, create product, validation errors, logout |

Guard test idea:

```ts
it("redirects anonymous users to login", () => {
  authService.authState.set({ accessToken: null, expiresAt: null, user: null });
  const result = TestBed.runInInjectionContext(() => authGuard(route, state));
  expect(result).toEqual(router.createUrlTree(["/login"], { queryParams: { returnUrl: state.url } }));
});
```

## Angular interview questions

### How do interceptors help?

They centralize cross-cutting HTTP behavior such as auth headers, correlation IDs, response error handling, and metrics. This keeps components focused on UI.

### How do guards differ from backend auth?

Guards prevent navigation in the browser. They are UX controls, not security controls. The Spring API must validate tokens and enforce roles/ownership for every protected endpoint.

### How do you handle Spring validation errors?

Spring returns Problem Details with an `errors` map. Angular maps each field to the corresponding `FormControl.setErrors({ server: message })`, while non-field errors render as a global alert.

### When use signals vs RxJS?

Signals are great for local synchronous UI state. RxJS remains useful for HTTP streams, cancellation with `switchMap`, debounced search, WebSocket/SSE wrappers, and composition of async events.

### How do you avoid memory leaks?

Prefer the `async` pipe, `takeUntilDestroyed`, and framework-managed subscriptions. Clean up manual subscriptions and close SSE/WebSocket connections.

## Angular completion checklist

- [ ] API URL comes from environment config.
- [ ] Auth service centralizes user/token state.
- [ ] Interceptor attaches bearer token.
- [ ] Guard protects private routes.
- [ ] Spring Problem Details maps to reactive forms.
- [ ] Product service owns HTTP calls.
- [ ] Search/filter requests cancel stale responses.
- [ ] CORS is configured for dev and production.
- [ ] Build output can run behind S3/CloudFront with route fallback.
- [ ] Tests cover guard, interceptor, validation, and core product flow.
