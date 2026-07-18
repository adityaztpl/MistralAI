# 03 - React with Spring Boot

This guide shows how to connect a Vite React TypeScript SPA to a Spring Boot API. It focuses on the parts interviewers ask about: auth state, API clients, interceptors, route protection, error handling, streaming, and environment configuration.

## Recommended stack

- Vite
- React 18+
- TypeScript
- React Router
- Fetch or Axios
- React Hook Form + Zod or Yup for complex forms
- TanStack Query for server state in larger apps
- Vitest + React Testing Library
- Playwright for E2E tests

Minimal setup:

```bash
npm create vite@latest product-catalog-web -- --template react-ts
cd product-catalog-web
npm install react-router-dom
npm install @tanstack/react-query
npm install react-hook-form zod @hookform/resolvers
npm install -D vitest @testing-library/react @testing-library/jest-dom playwright
```

## Folder structure

```text
src/
  app/
    App.tsx
    router.tsx
    queryClient.ts
  auth/
    AuthContext.tsx
    ProtectedRoute.tsx
    authTypes.ts
  api/
    apiClient.ts
    problemDetails.ts
    productsApi.ts
  products/
    ProductListPage.tsx
    ProductDetailPage.tsx
    ProductForm.tsx
  shared/
    components/
    hooks/
    env.ts
  main.tsx
```

Keep domain features grouped. Avoid one giant `components/` folder for everything.

## Environment configuration

Vite exposes variables prefixed with `VITE_`.

`.env.development`:

```env
VITE_API_BASE_URL=http://localhost:8080
VITE_AUTH_STORAGE_KEY=product-catalog-auth
```

`.env.production`:

```env
VITE_API_BASE_URL=https://api.example.com
VITE_AUTH_STORAGE_KEY=product-catalog-auth
```

Typed env wrapper:

```ts
export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL as string,
  authStorageKey: import.meta.env.VITE_AUTH_STORAGE_KEY as string,
};

if (!env.apiBaseUrl) {
  throw new Error("Missing VITE_API_BASE_URL");
}
```

Important:

- Vite env values are bundled into browser JavaScript.
- Never put secrets in `VITE_` variables.
- Only public config belongs in the SPA: API base URL, build version, feature flags safe for users to see.

## Auth models

### Learning default: bearer token in browser storage

Flow:

1. User submits email/password.
2. React calls `POST /api/v1/auth/login`.
3. API returns `accessToken`, `expiresAt`, and user info.
4. React stores auth state.
5. API client adds `Authorization: Bearer <token>`.
6. On `401`, React clears auth and redirects to login.

Production caveat: JavaScript-readable token storage can be stolen by XSS. Mention this in interviews and discuss alternatives:

- in-memory access token plus refresh flow
- secure HTTP-only cookies with CSRF protection
- BFF pattern
- Cognito hosted UI with Authorization Code + PKCE

## Auth context shape

See [`examples/react-client/AuthContext.tsx`](examples/react-client/AuthContext.tsx) for a fuller example.

Core responsibilities:

- Store current user and access token.
- Restore saved auth on app startup.
- Expose `login`, `logout`, and `isAuthenticated`.
- Avoid scattering `localStorage` reads across the app.

```tsx
type AuthUser = {
  id: string;
  email: string;
  roles: string[];
};

type AuthState = {
  accessToken: string | null;
  expiresAt: string | null;
  user: AuthUser | null;
};
```

Interview line:

> I centralize auth state so API clients and route guards have one source of truth. I also treat localStorage token persistence as a demo convenience and call out a cookie/BFF or PKCE approach for production.

## API client patterns

### Fetch wrapper

A fetch wrapper is lightweight and dependency-free:

```ts
export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
  accessToken?: string | null
): Promise<T> {
  const response = await fetch(`${env.apiBaseUrl}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...options.headers,
    },
  });

  if (!response.ok) {
    throw await parseProblemDetails(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}
```

### Axios interceptors

Axios is useful when teams prefer interceptors:

```ts
const client = axios.create({
  baseURL: env.apiBaseUrl,
  headers: { "Content-Type": "application/json" },
});

client.interceptors.request.use((config) => {
  const token = authTokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      authTokenStore.clear();
      window.dispatchEvent(new CustomEvent("auth:expired"));
    }
    return Promise.reject(error);
  }
);
```

Use a small abstraction over either fetch or Axios so components do not know low-level transport details.

## Problem Details in React

Define a client type:

```ts
export type ProblemDetails = {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};
```

Display strategy:

- Field validation errors: map `errors[field]` into form fields.
- Domain conflict: show a specific alert based on `type` or `status`.
- `401`: redirect to login or show session expired.
- `403`: show "not authorized."
- `500`: show generic message and trace ID.

Example form mapping with React Hook Form:

```ts
function applyServerErrors(
  problem: ProblemDetails,
  setError: UseFormSetError<ProductFormValues>
) {
  Object.entries(problem.errors ?? {}).forEach(([field, messages]) => {
    setError(field as keyof ProductFormValues, {
      type: "server",
      message: messages.join(" "),
    });
  });
}
```

## Protected routes

```tsx
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function ProtectedRoute() {
  const location = useLocation();
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
```

Router:

```tsx
createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    element: <ProtectedRoute />,
    children: [
      { path: "/", element: <ProductListPage /> },
      { path: "/products/:id", element: <ProductDetailPage /> },
    ],
  },
]);
```

Route protection improves UX, but the API remains the source of truth. Never rely only on frontend route guards for security.

## Server state with TanStack Query

```ts
export function useProducts(params: ProductSearchParams) {
  const { accessToken } = useAuth();

  return useQuery({
    queryKey: ["products", params],
    queryFn: () => productsApi.list(params, accessToken),
    staleTime: 30_000,
  });
}
```

Benefits:

- request deduping
- loading/error states
- caching
- refetch after mutations
- pagination support

Mutation example:

```ts
const queryClient = useQueryClient();

const createProduct = useMutation({
  mutationFn: (input: ProductCreateRequest) => productsApi.create(input, accessToken),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: ["products"] }),
});
```

## Product form against Spring validation

Client-side schema:

```ts
const productSchema = z.object({
  name: z.string().min(1).max(80),
  sku: z.string().min(1).max(40),
  price: z.coerce.number().min(0),
  currency: z.string().length(3),
  quantity: z.coerce.number().int().min(0),
});
```

Spring remains authoritative. Client validation reduces round trips; server validation protects the system.

## Streaming notes

Spring Boot can stream with:

- Server-Sent Events (`text/event-stream`)
- WebSocket/STOMP
- chunked HTTP response

For product catalog, streaming is usually unnecessary. For chat, imports, reports, or progress logs, use SSE first because it is simpler than WebSocket for server-to-client updates.

React SSE example:

```ts
const source = new EventSource(`${env.apiBaseUrl}/api/v1/import-jobs/${jobId}/events`, {
  withCredentials: false,
});

source.addEventListener("progress", (event) => {
  const payload = JSON.parse(event.data) as { processed: number; total: number };
  setProgress(payload);
});

source.onerror = () => {
  source.close();
};
```

Bearer token problem: native `EventSource` does not allow custom Authorization headers. Options:

- use cookie-based auth
- use a short-lived signed stream URL
- use `fetch` with readable streams
- use a polyfill that supports headers
- use WebSocket with auth during connection

Fetch streaming example:

```ts
const response = await fetch(`${env.apiBaseUrl}/api/v1/reports/stream`, {
  headers: { Authorization: `Bearer ${accessToken}` },
});

const reader = response.body?.getReader();
const decoder = new TextDecoder();

while (reader) {
  const { done, value } = await reader.read();
  if (done) break;
  appendChunk(decoder.decode(value, { stream: true }));
}
```

## CORS and local development

Vite dev server runs on one origin, Spring on another:

```text
React:  http://localhost:5173
API:    http://localhost:8080
```

Spring must allow:

- origin `http://localhost:5173`
- methods `GET,POST,PUT,PATCH,DELETE,OPTIONS`
- headers `Authorization,Content-Type,X-Correlation-Id`

For production, allow only the CloudFront domain or app custom domain.

## Build and deployment

Build:

```bash
npm run build
```

Output:

```text
dist/
  index.html
  assets/
```

S3/CloudFront rules:

- Cache hashed assets for a long time.
- Keep `index.html` short-lived or invalidate it on deploy.
- Route unknown paths to `index.html` for client-side routing.
- Do not expose source maps publicly unless intentional.

## Testing strategy

| Test | Focus |
|---|---|
| Component tests | Forms, validation messages, table rendering |
| Hook tests | Auth state, custom API hooks |
| API client tests | Problem Details parsing, auth header |
| Router tests | Protected route redirects |
| E2E | Login, product CRUD, validation error, logout |

Example E2E flow:

1. Visit `/login`.
2. Submit credentials.
3. Assert redirect to products.
4. Create product.
5. Assert product appears in list.
6. Submit invalid product and assert server validation renders on fields.
7. Logout and assert protected route redirects.

## React interview questions

### How do you handle auth in React?

I centralize auth in a provider, expose login/logout/current user, and inject the token through an API client. Protected routes improve UX, but the API enforces security. For production, I avoid casually storing long-lived tokens in localStorage and consider PKCE, secure cookies, or a BFF.

### How do you prevent every component from manually handling loading/error logic?

For server state, I use TanStack Query or a consistent hook layer. Components receive typed data and render loading/error states consistently.

### How do you map Spring validation errors to forms?

Spring returns Problem Details with an `errors` map. The form handler iterates through the map and calls `setError` for matching fields. Unknown/global errors render as an alert.

### How do you configure API URLs?

Use Vite `VITE_` env variables for public config and wrap them in a typed `env.ts`. Never put secrets in frontend env vars because they are bundled into the browser.

### How do you debug a 401 loop?

Check token expiration, issuer/audience/signing key, Authorization header presence, CORS preflight, clock skew, route guard redirects, and whether the client retries a failed refresh indefinitely.

## React completion checklist

- [ ] API base URL is environment-based.
- [ ] Auth state is centralized.
- [ ] API client attaches token and parses Problem Details.
- [ ] Protected routes redirect unauthenticated users.
- [ ] Server validation maps to form fields.
- [ ] Loading, empty, and error states are visible.
- [ ] CORS is configured intentionally.
- [ ] Build output can be hosted on S3/CloudFront.
- [ ] E2E test covers login and product CRUD.
