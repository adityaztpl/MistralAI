# React Intermediate: Custom Hooks, Context, Router, Data Fetching, Reducers, Performance, TypeScript, Forms

Intermediate React is about extracting reusable logic, managing state boundaries, routing screens, fetching data safely, and avoiding unnecessary renders without turning every component into an optimization exercise.

## 1. Custom hooks

A custom hook is a function whose name starts with `use` and can call other hooks. It extracts reusable stateful logic.

```tsx
import { useEffect, useState } from 'react';

export function useWindowWidth() {
  const [width, setWidth] = useState(() => window.innerWidth);

  useEffect(() => {
    function handleResize() {
      setWidth(window.innerWidth);
    }

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return width;
}
```

Rules:

- Call hooks only at the top level of React components or other hooks.
- Do not call hooks conditionally or inside loops.
- Custom hooks share logic, not state by default. Each call owns its own state.

Interview talking point:

> A custom hook is not a service singleton. It is a reusable recipe for state and effects. If two components call it, they usually get separate state unless the hook uses an external shared store.

---

## 2. Context

Context passes values through the component tree without prop drilling.

```tsx
import { createContext, useContext, useMemo, useState } from 'react';

type Theme = 'light' | 'dark';

type ThemeContextValue = {
  theme: Theme;
  toggleTheme: () => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [theme, setTheme] = useState<Theme>('light');

  const value = useMemo(
    () => ({
      theme,
      toggleTheme: () =>
        setTheme((current) => (current === 'light' ? 'dark' : 'light')),
    }),
    [theme],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const context = useContext(ThemeContext);

  if (!context) {
    throw new Error('useTheme must be used within ThemeProvider');
  }

  return context;
}
```

Context is good for:

- Theme.
- Locale.
- Auth session metadata.
- Current workspace/account.
- Dependency-style values.

Context is not always ideal for:

- Frequently changing large data.
- Server cache.
- State with many independent subscribers.

Interview note:

> Context updates re-render consumers under that provider. For high-frequency updates, consider splitting contexts, memoizing values, or using a dedicated external store.

---

## 3. React Router

React Router maps locations to UI and supports nested layouts, params, loaders/actions in data routers, and protected routes.

Basic route setup:

```tsx
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import { AppLayout } from './AppLayout';
import { Dashboard } from './Dashboard';
import { ProductPage } from './ProductPage';

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: 'products/:productId', element: <ProductPage /> },
    ],
  },
]);

export function App() {
  return <RouterProvider router={router} />;
}
```

Reading params:

```tsx
import { useParams } from 'react-router-dom';

function ProductPage() {
  const { productId } = useParams();
  return <h1>Product {productId}</h1>;
}
```

Navigation:

```tsx
import { Link, useNavigate } from 'react-router-dom';

function ProductsLink() {
  const navigate = useNavigate();

  return (
    <>
      <Link to="/products/42">Open product</Link>
      <button type="button" onClick={() => navigate('/checkout')}>
        Checkout
      </button>
    </>
  );
}
```

Interview checklist:

- Use `Link` for normal navigation.
- Use `useNavigate` for imperative navigation after events.
- Nested routes render into `Outlet`.
- URL state is often better than global state for filters, tabs, and selected IDs.

---

## 4. Data fetching

For simple apps, fetching in an effect can be acceptable. For production server state, use a cache library or framework data APIs when available.

Manual fetch with stale-response protection:

```tsx
function useUser(userId: string) {
  const [state, setState] = useState<{
    status: 'loading' | 'success' | 'error';
    user: { id: string; name: string } | null;
    error: unknown;
  }>({ status: 'loading', user: null, error: null });

  useEffect(() => {
    const controller = new AbortController();
    setState({ status: 'loading', user: null, error: null });

    fetch(`/api/users/${userId}`, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Request failed with ${response.status}`);
        }

        return response.json() as Promise<{ id: string; name: string }>;
      })
      .then((user) => setState({ status: 'success', user, error: null }))
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          setState({ status: 'error', user: null, error });
        }
      });

    return () => controller.abort();
  }, [userId]);

  return state;
}
```

Server state concerns:

- Caching.
- Deduplication.
- Background refetch.
- Pagination/infinite lists.
- Optimistic updates.
- Stale data rules.
- Retry and error handling.

Interview talking point:

> Server data is not the same as client UI state. A library like TanStack Query often handles server cache behavior better than hand-written effects.

---

## 5. `useReducer`

Use `useReducer` when state transitions are complex or event-driven.

```tsx
import { useReducer } from 'react';

type State = {
  count: number;
};

type Action =
  | { type: 'increment' }
  | { type: 'decrement' }
  | { type: 'reset'; value?: number };

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'increment':
      return { count: state.count + 1 };
    case 'decrement':
      return { count: state.count - 1 };
    case 'reset':
      return { count: action.value ?? 0 };
    default:
      return state;
  }
}

export function Counter() {
  const [state, dispatch] = useReducer(reducer, { count: 0 });

  return (
    <>
      <p>{state.count}</p>
      <button onClick={() => dispatch({ type: 'increment' })}>+</button>
      <button onClick={() => dispatch({ type: 'reset', value: 10 })}>
        Reset to 10
      </button>
    </>
  );
}
```

When reducers help:

- Multiple fields change together.
- Many event types affect the same state.
- You want state transitions centralized and testable.
- You want to pass `dispatch` down instead of many callbacks.

---

## 6. Performance: `memo`, `useMemo`, and when needed

React is fast enough for many normal components without manual memoization. Optimize when there is measurable re-render cost or unstable references cause expensive children to re-render.

### `React.memo`

```tsx
import { memo } from 'react';

type ProductRowProps = {
  product: { id: string; name: string; price: number };
  onSelect: (id: string) => void;
};

export const ProductRow = memo(function ProductRow({
  product,
  onSelect,
}: ProductRowProps) {
  return (
    <button type="button" onClick={() => onSelect(product.id)}>
      {product.name} - ${product.price}
    </button>
  );
});
```

`memo` helps only when props are referentially stable and rendering is meaningfully expensive.

### `useMemo`

```tsx
const visibleProducts = useMemo(
  () =>
    products.filter((product) =>
      product.name.toLowerCase().includes(query.toLowerCase()),
    ),
  [products, query],
);
```

Use `useMemo` for expensive calculations or stable references required by memoized children. Do not use it to hide side effects.

### `useCallback`

```tsx
const handleSelect = useCallback((id: string) => {
  setSelectedId(id);
}, []);
```

Interview answer:

> I use memoization when I can identify a render bottleneck or a reference stability need. Otherwise it adds complexity and can make code harder to read without improving UX.

---

## 7. TypeScript with React

TypeScript improves prop contracts, event typing, API modeling, and ref safety.

Props:

```tsx
type AlertProps = {
  variant: 'success' | 'warning' | 'error';
  children: React.ReactNode;
};

function Alert({ variant, children }: AlertProps) {
  return <div data-variant={variant}>{children}</div>;
}
```

Events:

```tsx
function EmailInput() {
  const [email, setEmail] = useState('');

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    setEmail(event.target.value);
  }

  return <input value={email} onChange={handleChange} />;
}
```

Refs:

```tsx
const inputRef = useRef<HTMLInputElement | null>(null);

function focusInput() {
  inputRef.current?.focus();
}
```

Discriminated unions:

```ts
type LoadState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: unknown };
```

Interview talking point:

> TypeScript should model real states. Discriminated unions are often better than several optional fields because impossible states become unrepresentable.

---

## 8. Controlled forms libraries note

For small forms, controlled inputs with `useState` are fine. For larger forms, libraries reduce boilerplate and improve performance.

Common options:

- React Hook Form: popular, performant, good uncontrolled-input model.
- Formik: established controlled-form approach.
- Zod/Yup/Valibot: schema validation often paired with forms.

When a library helps:

- Many fields.
- Dynamic arrays.
- Complex validation.
- Integration with design system inputs.
- Need touched/dirty/error state.

Interview answer:

> I start simple for small forms. I choose a form library when validation, field arrays, nested data, or performance become significant enough that hand-rolled state would obscure the business logic.

---

## Intermediate interview drill

1. What makes a function a custom hook?
2. Does a custom hook share state between components?
3. What problems does Context solve, and when is it a poor fit?
4. What state belongs in the URL?
5. Why is server state different from client state?
6. When would you choose `useReducer`?
7. When does `React.memo` help?
8. How can TypeScript prevent impossible UI states?
