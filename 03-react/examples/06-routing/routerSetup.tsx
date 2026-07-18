import {
  Form,
  Link,
  Navigate,
  Outlet,
  RouterProvider,
  createBrowserRouter,
  isRouteErrorResponse,
  redirect,
  useLoaderData,
  useLocation,
  useNavigation,
  useRouteLoaderData,
  useRouteError,
  type ActionFunctionArgs,
  type LoaderFunctionArgs,
} from 'react-router-dom';

type Product = {
  id: string;
  name: string;
  priceCents: number;
};

type Session = {
  user: {
    id: string;
    email: string;
  } | null;
};

async function getSession(): Promise<Session> {
  const response = await fetch('/api/session');

  if (!response.ok) {
    return { user: null };
  }

  return (await response.json()) as Session;
}

async function requireUser() {
  const session = await getSession();

  if (!session.user) {
    throw redirect('/login');
  }

  return session.user;
}

async function fetchProducts(request: Request): Promise<Product[]> {
  const url = new URL(request.url);
  const query = url.searchParams.get('query') ?? '';

  const response = await fetch(`/api/products?query=${encodeURIComponent(query)}`);

  if (!response.ok) {
    throw new Response('Failed to load products', { status: response.status });
  }

  return (await response.json()) as Product[];
}

export async function rootLoader() {
  return getSession();
}

export async function productsLoader({ request }: LoaderFunctionArgs) {
  return fetchProducts(request);
}

export async function accountLoader() {
  return requireUser();
}

export async function createProductAction({ request }: ActionFunctionArgs) {
  await requireUser();

  const formData = await request.formData();
  const name = String(formData.get('name') ?? '').trim();
  const priceCents = Number(formData.get('priceCents') ?? '0');

  if (!name || !Number.isFinite(priceCents) || priceCents <= 0) {
    return { error: 'Name and price are required.' };
  }

  const response = await fetch('/api/products', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ name, priceCents }),
  });

  if (!response.ok) {
    throw new Response('Failed to create product', { status: response.status });
  }

  return redirect('/products');
}

function RootLayout() {
  const session = useLoaderData() as Session;
  const navigation = useNavigation();
  const isNavigating = navigation.state !== 'idle';

  return (
    <>
      <header>
        <nav aria-label="Primary navigation">
          <Link to="/">Home</Link>
          <Link to="/products">Products</Link>
          <Link to="/account">Account</Link>
        </nav>
        <p>{session.user ? `Signed in as ${session.user.email}` : 'Anonymous'}</p>
        {isNavigating && <p aria-live="polite">Loading next page...</p>}
      </header>
      <main>
        <Outlet />
      </main>
    </>
  );
}

function ProductsRoute() {
  const products = useLoaderData() as Product[];

  return (
    <section>
      <h1>Products</h1>
      <Form role="search">
        <label htmlFor="query">Search products</label>
        <input id="query" name="query" />
        <button type="submit">Search</button>
      </Form>
      <ul>
        {products.map((product) => (
          <li key={product.id}>
            <Link to={`/products/${product.id}`}>{product.name}</Link>
          </li>
        ))}
      </ul>
    </section>
  );
}

function NewProductRoute() {
  const navigation = useNavigation();
  const isSubmitting = navigation.state === 'submitting';

  return (
    <section>
      <h1>New product</h1>
      <Form method="post">
        <label htmlFor="name">Name</label>
        <input id="name" name="name" />
        <label htmlFor="priceCents">Price cents</label>
        <input id="priceCents" name="priceCents" inputMode="numeric" />
        <button disabled={isSubmitting}>Create product</button>
      </Form>
    </section>
  );
}

function RequireAuth({ children }: { children: React.ReactNode }) {
  const session = useRouteLoaderData('root') as Session | undefined;
  const location = useLocation();

  if (!session?.user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return children;
}

function RouteErrorBoundary() {
  const error = useRouteError();

  if (isRouteErrorResponse(error)) {
    return (
      <section>
        <h1>{error.status}</h1>
        <p>{error.data || error.statusText}</p>
      </section>
    );
  }

  return (
    <section>
      <h1>Something went wrong</h1>
      <p>Please retry or contact support.</p>
    </section>
  );
}

function HomeRoute() {
  return <h1>Home</h1>;
}

function AccountRoute() {
  return <h1>Account</h1>;
}

function LoginRoute() {
  return <h1>Login</h1>;
}

export const router = createBrowserRouter([
  {
    id: 'root',
    path: '/',
    element: <RootLayout />,
    loader: rootLoader,
    errorElement: <RouteErrorBoundary />,
    children: [
      { index: true, element: <HomeRoute /> },
      {
        path: 'products',
        children: [
          {
            index: true,
            element: <ProductsRoute />,
            loader: productsLoader,
          },
          {
            path: 'new',
            element: <NewProductRoute />,
            action: createProductAction,
          },
        ],
      },
      {
        path: 'account',
        loader: accountLoader,
        element: (
          <RequireAuth>
            <AccountRoute />
          </RequireAuth>
        ),
      },
      { path: 'login', element: <LoginRoute /> },
    ],
  },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
