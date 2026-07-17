# React State Management

State management interviews are rarely about naming the most powerful library. They are about choosing the smallest state owner that preserves correctness, user experience, testability, and team maintainability.

Strong default:

> Start with local state. Lift state only when coordination requires it. Put shareable navigation state in the URL. Put remote data in a server-state cache. Use global client state for cross-cutting client-only state with real sharing needs.

---

## 1. State taxonomy

| State kind | Examples | Best home |
| --- | --- | --- |
| Local UI state | input value, menu open, active tab inside a card | `useState` / `useReducer` |
| Derived state | filtered list, total price, validation summary | compute during render / `useMemo` if expensive |
| Lifted shared state | selected item shared by siblings | nearest common parent |
| URL state | filters, pagination, selected route entity, tab in a page | route params/search params |
| Server state | products, user profile from API, permissions from backend | TanStack Query, route loaders, framework data APIs |
| Global client state | auth session metadata, cart, theme, feature flags, wizard progress | Context, Zustand, Redux Toolkit |
| External system state | online status, media query, browser storage | custom hook, `useSyncExternalStore` |
| Form state | field values, touched/dirty/errors | local state or form library |

State placement questions:

1. Who reads it?
2. Who writes it?
3. Does it need to survive refresh?
4. Should it be shareable through a link?
5. Is it owned by the server?
6. Does it need caching, invalidation, retries, or deduplication?
7. Does it change frequently?
8. Would broad re-renders be a problem?
9. Is the transition logic simple or event-heavy?
10. How will it be tested?

---

## 2. Local state and lifting state

Local state is the default.

```tsx
function SearchInput() {
  const [query, setQuery] = useState('');

  return (
    <input
      value={query}
      onChange={(event) => setQuery(event.target.value)}
      placeholder="Search"
    />
  );
}
```

Lift state when multiple components need to coordinate.

```tsx
function ProductsPage() {
  const [selectedProductId, setSelectedProductId] = useState<string | null>(null);

  return (
    <>
      <ProductsList selectedId={selectedProductId} onSelect={setSelectedProductId} />
      <ProductDetails productId={selectedProductId} />
    </>
  );
}
```

Do not lift state just because it "might be useful later." Lifting increases coupling and can increase render scope.

---

## 3. Derived state

Derived values are usually not state.

```tsx
const subtotal = cartItems.reduce(
  (sum, item) => sum + item.price * item.quantity,
  0,
);
```

Avoid synchronizing derived state with an effect:

```tsx
// Avoid.
useEffect(() => {
  setFilteredProducts(
    products.filter((product) => product.name.includes(query)),
  );
}, [products, query]);
```

Prefer:

```tsx
const filteredProducts = useMemo(
  () => products.filter((product) => product.name.includes(query)),
  [products, query],
);
```

Use `useMemo` when the calculation is expensive or reference stability matters. Otherwise a direct calculation is simpler.

---

## 4. URL state

URL state is ideal when state should be:

- Shareable.
- Bookmarkable.
- Preserved on refresh.
- Integrated with browser back/forward.
- Visible to analytics or server rendering.

Examples:

- `?query=shoes`
- `?page=3`
- `/products/:productId`
- `?sort=price-asc`
- `?tab=billing`

React Router search params:

```tsx
function ProductFilters() {
  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('query') ?? '';

  function updateQuery(nextQuery: string) {
    setSearchParams((current) => {
      const next = new URLSearchParams(current);

      if (nextQuery) {
        next.set('query', nextQuery);
      } else {
        next.delete('query');
      }

      next.set('page', '1');
      return next;
    });
  }

  return <input value={query} onChange={(event) => updateQuery(event.target.value)} />;
}
```

Interview answer:

> If a filter affects what the page represents, I usually put it in the URL. If it is only a transient widget detail, I keep it local.

---

## 5. Context

Context passes values through a subtree without threading props through every layer.

Good Context use cases:

- Theme.
- Locale.
- Auth session metadata.
- Current organization/workspace.
- Feature flags.
- Design-system configuration.
- Dependency injection for API clients.

Context is less ideal for:

- Large frequently changing objects.
- Server cache.
- Independent subscriptions to tiny slices.
- High-frequency state such as mouse position.

Typed Context pattern:

```tsx
import { createContext, useContext, useMemo, useState } from 'react';

type AuthUser = {
  id: string;
  email: string;
};

type AuthContextValue = {
  user: AuthUser | null;
  signIn: (user: AuthUser) => void;
  signOut: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      signIn: setUser,
      signOut: () => setUser(null),
    }),
    [user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
```

Context performance tools:

- Split state and actions into separate contexts.
- Split unrelated concerns into separate providers.
- Memoize provider values.
- Move providers lower in the tree.
- Use an external store when selective subscriptions matter.

Split context example:

```tsx
const ThemeValueContext = createContext<Theme | null>(null);
const ThemeActionsContext = createContext<(() => void) | null>(null);
```

Interview answer:

> Context is a propagation mechanism, not a complete state-management strategy. It is excellent for subtree-wide values, but every consumer observes provider value changes.

---

## 6. Context with reducer

Context plus reducer is useful for medium-complexity client state when you want explicit transitions without adding an external library.

```tsx
type CartItem = {
  productId: string;
  quantity: number;
};

type CartState = {
  items: CartItem[];
};

type CartAction =
  | { type: 'itemAdded'; productId: string }
  | { type: 'itemRemoved'; productId: string }
  | { type: 'cartCleared' };

function cartReducer(state: CartState, action: CartAction): CartState {
  switch (action.type) {
    case 'itemAdded': {
      const existing = state.items.find((item) => item.productId === action.productId);

      if (existing) {
        return {
          items: state.items.map((item) =>
            item.productId === action.productId
              ? { ...item, quantity: item.quantity + 1 }
              : item,
          ),
        };
      }

      return {
        items: [...state.items, { productId: action.productId, quantity: 1 }],
      };
    }
    case 'itemRemoved':
      return {
        items: state.items.filter((item) => item.productId !== action.productId),
      };
    case 'cartCleared':
      return { items: [] };
    default:
      return state;
  }
}
```

When to outgrow it:

- Many independent subscribers.
- Middleware-like workflows.
- DevTools/time travel matters.
- Complex async workflows.
- Many teams need strict conventions.

---

## 7. Zustand

Zustand is a small external store library with a simple API and selective subscriptions.

Basic store:

```ts
import { create } from 'zustand';

type CartItem = {
  productId: string;
  quantity: number;
};

type CartStore = {
  items: CartItem[];
  addItem: (productId: string) => void;
  removeItem: (productId: string) => void;
  clear: () => void;
};

export const useCartStore = create<CartStore>((set) => ({
  items: [],
  addItem: (productId) =>
    set((state) => {
      const existing = state.items.find((item) => item.productId === productId);

      if (existing) {
        return {
          items: state.items.map((item) =>
            item.productId === productId
              ? { ...item, quantity: item.quantity + 1 }
              : item,
          ),
        };
      }

      return { items: [...state.items, { productId, quantity: 1 }] };
    }),
  removeItem: (productId) =>
    set((state) => ({
      items: state.items.filter((item) => item.productId !== productId),
    })),
  clear: () => set({ items: [] }),
}));
```

Selective subscription:

```tsx
const itemCount = useCartStore((state) =>
  state.items.reduce((count, item) => count + item.quantity, 0),
);
```

Zustand strengths:

- Minimal boilerplate.
- Components subscribe to selected slices.
- No provider required for simple client apps.
- Good for UI stores, carts, wizards, app shell state.
- Easy incremental adoption.

Zustand cautions:

- Fewer conventions than Redux Toolkit.
- Teams must define folder and action patterns.
- Persisted stores need migration/versioning plans.
- Server rendering requires care to avoid cross-request shared state.

Interview answer:

> I reach for Zustand when I need a lightweight client store with selective subscriptions and the team does not need Redux's stronger conventions.

---

## 8. Redux Toolkit

Redux Toolkit is the recommended way to write Redux. It provides `configureStore`, `createSlice`, Immer-powered immutable updates, good TypeScript ergonomics, and standard middleware/devtools setup.

Slice:

```ts
import { createSlice, type PayloadAction } from '@reduxjs/toolkit';

type AuthUser = {
  id: string;
  email: string;
  roles: string[];
};

type AuthState = {
  user: AuthUser | null;
  status: 'anonymous' | 'authenticated';
};

const initialState: AuthState = {
  user: null,
  status: 'anonymous',
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    signedIn(state, action: PayloadAction<AuthUser>) {
      state.user = action.payload;
      state.status = 'authenticated';
    },
    signedOut(state) {
      state.user = null;
      state.status = 'anonymous';
    },
  },
});

export const { signedIn, signedOut } = authSlice.actions;
export const authReducer = authSlice.reducer;
```

Store setup:

```ts
import { configureStore } from '@reduxjs/toolkit';

export const store = configureStore({
  reducer: {
    auth: authReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

Typed hooks:

```ts
import { useDispatch, useSelector, type TypedUseSelectorHook } from 'react-redux';

export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
```

Redux Toolkit strengths:

- Strong conventions.
- DevTools and action traces.
- Predictable state transitions.
- Mature ecosystem.
- Good for large teams and complex workflows.
- RTK Query option for server state.

Redux Toolkit cautions:

- More structure than small apps need.
- Selectors and slice boundaries require design discipline.
- Boilerplate is lower than classic Redux but not zero.
- Do not put every form field or server response in Redux by default.

Interview answer:

> Redux Toolkit is a good choice when conventions, traceability, and team-scale predictability matter. I would not use it as a dumping ground for all app data.

---

## 9. TanStack Query

TanStack Query manages server state: remote data that can be cached, invalidated, refetched, retried, paginated, and synchronized with mutations.

Query setup:

```tsx
import {
  QueryClient,
  QueryClientProvider,
  useQuery,
} from '@tanstack/react-query';

const queryClient = new QueryClient();

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ProductsPage />
    </QueryClientProvider>
  );
}
```

Query:

```tsx
type Product = {
  id: string;
  name: string;
};

async function fetchProducts(): Promise<Product[]> {
  const response = await fetch('/api/products');

  if (!response.ok) {
    throw new Error(`Failed to fetch products: ${response.status}`);
  }

  return (await response.json()) as Product[];
}

function ProductsPage() {
  const productsQuery = useQuery({
    queryKey: ['products'],
    queryFn: fetchProducts,
    staleTime: 60_000,
  });

  if (productsQuery.isPending) return <p>Loading...</p>;
  if (productsQuery.isError) return <p role="alert">{productsQuery.error.message}</p>;

  return (
    <ul>
      {productsQuery.data.map((product) => (
        <li key={product.id}>{product.name}</li>
      ))}
    </ul>
  );
}
```

Mutation:

```tsx
function AddProductForm() {
  const queryClient = useQueryClient();

  const createProductMutation = useMutation({
    mutationFn: createProduct,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['products'] });
    },
  });

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        const formData = new FormData(event.currentTarget);
        createProductMutation.mutate({ name: String(formData.get('name')) });
      }}
    >
      <input name="name" />
      <button disabled={createProductMutation.isPending}>Create</button>
    </form>
  );
}
```

TanStack Query strengths:

- Cache and stale-time control.
- Request deduplication.
- Background refetch.
- Retry logic.
- Mutation state.
- Invalidation.
- Pagination and infinite queries.
- Optimistic updates.
- Devtools.

TanStack Query cautions:

- It is not a client UI store.
- Query keys are an API; design them carefully.
- Stale/cache times should match product expectations.
- Optimistic updates need rollback handling.
- Server-rendered apps need hydration/dehydration setup.

Interview answer:

> TanStack Query owns server-state concerns. I still keep local UI state local and use URL state for shareable page state.

---

## 10. Optimistic updates

Optimistic updates show the expected result before the server confirms it.

TanStack Query optimistic pattern:

```tsx
const mutation = useMutation({
  mutationFn: updateTodo,
  onMutate: async (updatedTodo) => {
    await queryClient.cancelQueries({ queryKey: ['todos'] });
    const previousTodos = queryClient.getQueryData<Todo[]>(['todos']);

    queryClient.setQueryData<Todo[]>(['todos'], (current = []) =>
      current.map((todo) =>
        todo.id === updatedTodo.id ? { ...todo, ...updatedTodo } : todo,
      ),
    );

    return { previousTodos };
  },
  onError: (_error, _updatedTodo, context) => {
    queryClient.setQueryData(['todos'], context?.previousTodos);
  },
  onSettled: () => {
    void queryClient.invalidateQueries({ queryKey: ['todos'] });
  },
});
```

Optimistic checklist:

- Can the operation fail?
- How do you roll back?
- What if the server returns different data?
- What if multiple optimistic updates overlap?
- Does the UI need a pending marker?
- Is there an audit/compliance reason to wait for confirmation?

---

## 11. Decision matrix

| Need | Pick |
| --- | --- |
| One component controls one value | `useState` |
| Many related local transitions | `useReducer` |
| Siblings coordinate | Lift state |
| Deep subtree needs stable app value | Context |
| Shareable filter/page/sort state | URL params/search params |
| API data with caching/invalidation | TanStack Query or framework data API |
| Lightweight global client store | Zustand |
| Large-team global client state with conventions | Redux Toolkit |
| Server data in Redux-style app | RTK Query or TanStack Query |
| External mutable source | `useSyncExternalStore` |

## State management interview checklist

- Define client state vs server state.
- Explain why derived state is often not state.
- Discuss URL state for shareability and navigation.
- Explain Context's propagation behavior.
- Compare Zustand and Redux Toolkit without declaring a universal winner.
- Explain why TanStack Query is not a global store replacement.
- Mention optimistic updates and invalidation.
- Mention SSR/RSC implications for global stores.
- Explain how you would test reducers, selectors, hooks, and UI behavior.

## Common interview questions

1. When would you avoid Context?
2. How do you prevent unnecessary Context re-renders?
3. Zustand vs Redux Toolkit?
4. Redux Toolkit vs classic Redux?
5. TanStack Query vs Redux?
6. Where should filters live?
7. How do you model loading/error/success states?
8. How would you persist a cart?
9. How would you handle auth state?
10. What changes in SSR or Server Components?
