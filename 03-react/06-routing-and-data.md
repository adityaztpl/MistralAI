# React Routing and Data

Routing is not only "which component renders for this URL." Modern React routing defines layout boundaries, data ownership, loading states, error states, code splitting, navigation behavior, and sometimes server/client execution boundaries.

Interview framing:

> I treat the route tree as an architecture tool. It decides what state belongs in the URL, where layout persists, where data loads, where errors are caught, and where loading UI appears.

---

## 1. Routing concepts

Core terms:

- **Route:** mapping from a URL pattern to UI and data behavior.
- **Route param:** dynamic segment such as `/products/:productId`.
- **Search params:** query string values such as `?page=2&sort=name`.
- **Nested route:** child route rendered inside a parent layout.
- **Layout route:** route that renders shared structure and an `Outlet`.
- **Index route:** default child for a path.
- **Protected route:** route requiring auth/permissions.
- **Data router:** router that can run loaders/actions before rendering route elements.
- **Navigation state:** pending/submitting/idle state during route changes or form submissions.

State ownership:

- Path params identify resources.
- Search params describe page state.
- Route loaders can own data needed to render a route.
- Query libraries can own server cache across route boundaries.
- Component state owns transient widget details.

---

## 2. Basic React Router setup

```tsx
import {
  createBrowserRouter,
  Link,
  Outlet,
  RouterProvider,
} from 'react-router-dom';

function RootLayout() {
  return (
    <>
      <header>
        <nav>
          <Link to="/">Home</Link>
          <Link to="/products">Products</Link>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </>
  );
}

const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'products', element: <ProductsPage /> },
      { path: 'products/:productId', element: <ProductDetailsPage /> },
    ],
  },
]);

export function App() {
  return <RouterProvider router={router} />;
}
```

Interview notes:

- Use `Link` or `NavLink` for user navigation.
- Use `useNavigate` for imperative navigation after an event.
- Nested routes render in `Outlet`.
- Layout routes keep shared UI stable across child navigation.

---

## 3. Params and search params

Route params:

```tsx
function ProductDetailsPage() {
  const { productId } = useParams<'productId'>();

  if (!productId) {
    throw new Error('productId route param is required');
  }

  return <ProductDetails productId={productId} />;
}
```

Search params:

```tsx
function ProductsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('query') ?? '';
  const page = Number(searchParams.get('page') ?? '1');

  function updateQuery(nextQuery: string) {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);
      next.set('page', '1');

      if (nextQuery) {
        next.set('query', nextQuery);
      } else {
        next.delete('query');
      }

      return next;
    });
  }

  return (
    <>
      <input value={query} onChange={(event) => updateQuery(event.target.value)} />
      <ProductsList query={query} page={page} />
    </>
  );
}
```

When to use search params:

- Search query.
- Pagination.
- Sort.
- Filters.
- View mode.
- Page-level tabs.

When not to:

- Passwords/secrets.
- Very large data.
- Purely transient details like hover state.
- Complex form drafts unless product requirements demand shareability.

---

## 4. Protected routes

Simple element guard:

```tsx
function RequireAuth({ children }: { children: React.ReactNode }) {
  const { user, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return <p>Checking session...</p>;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return children;
}
```

Route usage:

```tsx
{
  path: 'account',
  element: (
    <RequireAuth>
      <AccountPage />
    </RequireAuth>
  ),
}
```

Data-router loader guard:

```tsx
async function accountLoader({ request }: LoaderFunctionArgs) {
  const user = await getCurrentUser(request);

  if (!user) {
    throw redirect('/login');
  }

  return user;
}
```

Interview answer:

> Client-side route guards protect UI, not secrets. Real authorization must happen on the server. The client guard improves UX by redirecting before showing protected screens.

---

## 5. Data routers: loaders and actions

React Router data routers can load data before route render.

Loader:

```tsx
type Product = {
  id: string;
  name: string;
};

async function productsLoader({ request }: LoaderFunctionArgs) {
  const url = new URL(request.url);
  const query = url.searchParams.get('query') ?? '';
  const response = await fetch(`/api/products?query=${encodeURIComponent(query)}`);

  if (!response.ok) {
    throw new Response('Failed to load products', { status: response.status });
  }

  return (await response.json()) as Product[];
}

function ProductsRoute() {
  const products = useLoaderData() as Product[];

  return (
    <ul>
      {products.map((product) => (
        <li key={product.id}>{product.name}</li>
      ))}
    </ul>
  );
}
```

Action:

```tsx
async function createProductAction({ request }: ActionFunctionArgs) {
  const formData = await request.formData();
  const name = String(formData.get('name') ?? '').trim();

  if (!name) {
    return { error: 'Name is required' };
  }

  await createProduct({ name });
  return redirect('/products');
}
```

Form:

```tsx
function NewProductRoute() {
  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  return (
    <Form method="post">
      <input name="name" />
      <button disabled={isSubmitting}>Create</button>
    </Form>
  );
}
```

Data-router strengths:

- Data dependencies live with routes.
- Navigation can wait for data.
- Route error boundaries handle loader/action failures.
- Forms can submit without manual event plumbing.
- Search params naturally trigger loader revalidation.

Data-router cautions:

- Cache behavior is not the same as TanStack Query by default.
- Loaders are route-scoped, not arbitrary component caches.
- Component-level interactions may still need query/mutation hooks.

---

## 6. Route error boundaries

Route-level errors should produce useful UI without crashing the entire app.

```tsx
function RouteErrorBoundary() {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    return (
      <section>
        <h1>{error.status}</h1>
        <p>{error.statusText}</p>
      </section>
    );
  }

  return (
    <section>
      <h1>Something went wrong</h1>
      <p>Please try again.</p>
    </section>
  );
}
```

Route config:

```tsx
{
  path: 'products',
  element: <ProductsRoute />,
  loader: productsLoader,
  errorElement: <RouteErrorBoundary />,
}
```

Interview notes:

- Error boundaries catch render errors below them.
- Data routers can route loader/action errors to `errorElement`.
- Error boundaries do not catch async errors in arbitrary event handlers unless surfaced into render/router state.

---

## 7. Lazy routes and code splitting

Route-level code splitting keeps initial bundles smaller.

```tsx
const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        path: 'admin',
        lazy: async () => {
          const module = await import('./routes/AdminRoute');
          return {
            Component: module.AdminRoute,
            loader: module.adminLoader,
            ErrorBoundary: module.AdminErrorBoundary,
          };
        },
      },
    ],
  },
]);
```

Suspense with `lazy`:

```tsx
const SettingsPage = lazy(() => import('./SettingsPage'));

function SettingsRoute() {
  return (
    <Suspense fallback={<p>Loading settings...</p>}>
      <SettingsPage />
    </Suspense>
  );
}
```

Placement guidance:

- Split at route boundaries first.
- Put loading UI where it preserves layout stability.
- Avoid over-splitting tiny components.
- Preload likely next routes when product behavior justifies it.

---

## 8. TanStack Query with routing

TanStack Query and routers often work together:

- Router owns URL and layout.
- Query owns server cache.
- Search params become query key inputs.
- Route transitions can prefetch likely data.

```tsx
function ProductsPage() {
  const [searchParams] = useSearchParams();
  const query = searchParams.get('query') ?? '';

  const productsQuery = useQuery({
    queryKey: ['products', { query }],
    queryFn: () => fetchProducts({ query }),
    staleTime: 60_000,
  });

  if (productsQuery.isPending) return <ProductsSkeleton />;
  if (productsQuery.isError) return <ErrorMessage error={productsQuery.error} />;

  return <ProductsList products={productsQuery.data} />;
}
```

Prefetch:

```tsx
function ProductLink({ product }: { product: Product }) {
  const queryClient = useQueryClient();

  return (
    <Link
      to={`/products/${product.id}`}
      onMouseEnter={() => {
        void queryClient.prefetchQuery({
          queryKey: ['product', product.id],
          queryFn: () => fetchProduct(product.id),
        });
      }}
    >
      {product.name}
    </Link>
  );
}
```

Interview answer:

> I avoid duplicating route-loader data and query-cache data unless there is a clear hydration strategy. If using TanStack Query, I make URL params part of the query key.

---

## 9. SPA, SSR, streaming, and Server Components

SPA:

- Browser downloads JavaScript and renders UI client-side.
- Simple deployment.
- Initial load and SEO can require extra work.

SSR:

- Server renders HTML for the initial request.
- Improves first content and SEO.
- Requires hydration on the client for interactivity.

Streaming SSR:

- Server can send HTML in chunks.
- Suspense boundaries can reveal content progressively.
- Improves perceived performance for slow data.

React Server Components:

- Components execute on the server and do not ship their JS to the client.
- Can access server-only resources.
- Cannot use state/effects/event handlers.
- Compose with Client Components at interaction boundaries.

Hydration:

- React attaches event handlers to server-rendered HTML.
- Markup must match between server and client.
- `useId` helps generate stable accessible IDs across hydration.
- Browser-only values should be read after mount or guarded carefully.

Interview answer:

> SSR sends HTML; RSC changes the component execution and bundle model. They are often used together in frameworks, but they solve different problems.

---

## 10. Data loading strategy matrix

| Strategy | Best for | Trade-offs |
| --- | --- | --- |
| Fetch in component effect | Small apps, isolated widgets | Manual cache/cancel/error handling |
| Custom `useFetch` hook | Reused simple fetch logic | Can become an incomplete query library |
| React Router loader | Route-owned data | Route-scoped cache semantics |
| TanStack Query | Shared server cache | Requires query key/invalidation discipline |
| Framework server data API | SSR/RSC/server-side needs | Framework-specific conventions |
| Server Components | Server-only data and reduced JS | Requires RSC-capable framework and boundaries |

---

## 11. Loading UX

Good loading states are specific and stable.

Patterns:

- Route-level skeleton for full page transitions.
- Inline spinner for small widgets.
- Optimistic UI for mutations where failure is recoverable.
- Pending navigation indicator for route changes.
- Stale-while-revalidate UI for cached data.
- Suspense boundaries around independently loadable regions.

Avoid:

- Flashing global spinners for every small request.
- Layout shift from skeletons with wrong dimensions.
- Hiding previous useful data during background refresh.
- Failing to expose loading state to assistive technology when needed.

---

## 12. Interview checklist

- Explain path params vs search params.
- Explain nested layouts and `Outlet`.
- Explain why URL state can be better than global state.
- Explain client route guards vs server authorization.
- Compare component fetching, route loaders, TanStack Query, and framework data APIs.
- Explain where loading and error boundaries belong.
- Explain route-level code splitting.
- Explain SSR vs RSC vs hydration.
- Explain how route params become query keys.
- Explain how you would test routing behavior with a memory router.

## Routing interview drill

1. What belongs in the URL?
2. How do nested routes affect layout persistence?
3. How would you protect a route?
4. What is the difference between loader errors and render errors?
5. When would you use TanStack Query with React Router?
6. How do you avoid duplicate data sources?
7. What causes hydration mismatch?
8. Where would you put a Suspense boundary?
9. How would you test navigation after login?
10. How do Server Components change routing/data decisions?
