# React Advanced: Concurrent Features, Suspense, Server Components, Server State, Stores, Testing, Error Boundaries, Patterns

Advanced React interviews focus on rendering semantics, state boundaries, async UX, architecture, and trade-offs. The strongest answers avoid magic claims and explain what React does and does not guarantee.

## 1. Concurrent rendering mental model

React 18 introduced concurrent rendering capabilities. Concurrent rendering lets React prepare UI updates interruptibly and prioritize urgent work, but it does not make every update automatically faster.

Important ideas:

- Rendering can be interrupted before commit.
- Render logic must stay pure because a render may be restarted.
- Urgent updates, like typing, should not be blocked by expensive non-urgent updates.
- Commit is when DOM mutations and layout effects happen.

Interview talking point:

> Concurrent rendering is about scheduling and responsiveness. It helps React keep urgent interactions responsive while preparing less urgent UI work, but developers still need to model expensive work and async states well.

---

## 2. `startTransition`

`startTransition` marks state updates as non-urgent.

```tsx
import { startTransition, useState } from 'react';

function ProductSearch({ products }: { products: string[] }) {
  const [query, setQuery] = useState('');
  const [filtered, setFiltered] = useState(products);

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    const nextQuery = event.target.value;
    setQuery(nextQuery);

    startTransition(() => {
      setFiltered(
        products.filter((product) =>
          product.toLowerCase().includes(nextQuery.toLowerCase()),
        ),
      );
    });
  }

  return (
    <>
      <input value={query} onChange={handleChange} />
      <ul>
        {filtered.map((product) => (
          <li key={product}>{product}</li>
        ))}
      </ul>
    </>
  );
}
```

Use cases:

- Filtering a large list while keeping input responsive.
- Navigating to a UI state that may suspend.
- Updating expensive derived views after urgent state changes.

Avoid:

- Wrapping every update.
- Using transitions for controlled input values themselves.

---

## 3. `useDeferredValue`

`useDeferredValue` lets a value lag behind during urgent updates.

```tsx
import { useDeferredValue, useMemo, useState } from 'react';

function SearchResults({ items }: { items: string[] }) {
  const [query, setQuery] = useState('');
  const deferredQuery = useDeferredValue(query);

  const filtered = useMemo(
    () =>
      items.filter((item) =>
        item.toLowerCase().includes(deferredQuery.toLowerCase()),
      ),
    [items, deferredQuery],
  );

  const isStale = query !== deferredQuery;

  return (
    <>
      <input value={query} onChange={(event) => setQuery(event.target.value)} />
      <div style={{ opacity: isStale ? 0.6 : 1 }}>
        {filtered.map((item) => (
          <p key={item}>{item}</p>
        ))}
      </div>
    </>
  );
}
```

Interview talking point:

> `useDeferredValue` does not debounce. It lets React keep showing an older value while rendering a newer, lower-priority view.

---

## 4. Suspense

Suspense lets components declare loading boundaries for async dependencies. It is commonly used with code splitting, framework data APIs, server components, or libraries that integrate with Suspense.

Code splitting:

```tsx
import { Suspense, lazy } from 'react';

const AdminPanel = lazy(() => import('./AdminPanel'));

export function AdminRoute() {
  return (
    <Suspense fallback={<p>Loading admin tools...</p>}>
      <AdminPanel />
    </Suspense>
  );
}
```

Nested boundaries:

```tsx
<Suspense fallback={<PageSkeleton />}>
  <ProfileHeader />
  <Suspense fallback={<ActivitySkeleton />}>
    <RecentActivity />
  </Suspense>
</Suspense>
```

Suspense interview notes:

- Suspense handles loading states for components that suspend.
- It is not an error boundary.
- Boundary placement controls UX.
- Frameworks such as Next.js and Remix may provide higher-level data conventions.

---

## 5. React 19-era form and optimistic patterns

React 19 adds and stabilizes APIs commonly discussed around actions, pending form state, and optimistic UI in supporting frameworks/environments.

Conceptual `useActionState` example:

```tsx
import { useActionState } from 'react';

type FormState = {
  message: string | null;
};

async function saveProfile(
  previousState: FormState,
  formData: FormData,
): Promise<FormState> {
  const displayName = String(formData.get('displayName') ?? '').trim();

  if (!displayName) {
    return { message: 'Display name is required.' };
  }

  await fetch('/api/profile', {
    method: 'POST',
    body: JSON.stringify({ displayName }),
  });

  return { message: 'Saved.' };
}

function ProfileForm() {
  const [state, formAction, isPending] = useActionState(saveProfile, {
    message: null,
  });

  return (
    <form action={formAction}>
      <input name="displayName" />
      <button disabled={isPending}>Save</button>
      {state.message && <p>{state.message}</p>}
    </form>
  );
}
```

Optimistic UI idea:

```tsx
import { useOptimistic } from 'react';

function Comments({ comments }: { comments: string[] }) {
  const [optimisticComments, addOptimisticComment] = useOptimistic(
    comments,
    (current, optimisticComment: string) => [...current, optimisticComment],
  );

  async function submit(formData: FormData) {
    const text = String(formData.get('comment') ?? '');
    addOptimisticComment(text);
    await fetch('/api/comments', { method: 'POST', body: formData });
  }

  return (
    <form action={submit}>
      {optimisticComments.map((comment, index) => (
        <p key={`${comment}-${index}`}>{comment}</p>
      ))}
      <input name="comment" />
      <button>Post</button>
    </form>
  );
}
```

Interview framing:

> React's action-oriented APIs are most powerful when paired with frameworks that understand server actions, routing, and streaming. In plain client apps, libraries and explicit event handlers are still common.

---

## 6. Server Components overview

React Server Components (RSC) render on the server and send a component payload to the client. They can access server-only resources and avoid shipping their JavaScript to the browser.

Server Component strengths:

- Fetch data close to the database or internal service.
- Keep secrets and server-only dependencies out of the client bundle.
- Reduce client JavaScript.
- Compose server-rendered and client-interactive parts.

Client Component needs:

- State.
- Effects.
- Browser APIs.
- Event handlers.

Next.js-style example:

```tsx
// Server component by default in an RSC framework.
export default async function ProductPage({ id }: { id: string }) {
  const product = await getProduct(id);

  return (
    <>
      <h1>{product.name}</h1>
      <AddToCartButton productId={product.id} />
    </>
  );
}
```

```tsx
'use client';

import { useState } from 'react';

export function AddToCartButton({ productId }: { productId: string }) {
  const [added, setAdded] = useState(false);

  return (
    <button type="button" onClick={() => setAdded(true)}>
      {added ? 'Added' : `Add ${productId} to cart`}
    </button>
  );
}
```

RSC interview points:

- Server Components are not SSR alone.
- They do not have effects or browser event handlers.
- Props crossing from server to client must be serializable.
- Use Client Components at interaction boundaries.

---

## 7. TanStack Query

TanStack Query manages server state: caching, deduplication, retries, background refetching, mutation state, invalidation, and pagination.

```tsx
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query';

const queryClient = new QueryClient();

async function fetchProducts() {
  const response = await fetch('/api/products');

  if (!response.ok) {
    throw new Error('Failed to load products');
  }

  return (await response.json()) as Array<{ id: string; name: string }>;
}

function Products() {
  const { data = [], isPending, isError, error } = useQuery({
    queryKey: ['products'],
    queryFn: fetchProducts,
    staleTime: 60_000,
  });

  if (isPending) return <p>Loading...</p>;
  if (isError) return <p>{error.message}</p>;

  return data.map((product) => <p key={product.id}>{product.name}</p>);
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Products />
    </QueryClientProvider>
  );
}
```

Mutation invalidation:

```tsx
const queryClient = useQueryClient();

const mutation = useMutation({
  mutationFn: createProduct,
  onSuccess: () => {
    void queryClient.invalidateQueries({ queryKey: ['products'] });
  },
});
```

Interview talking point:

> TanStack Query is not a global client state store. It is a server-state cache. I still keep ephemeral UI state local or in a client store.

---

## 8. Zustand and Redux Toolkit

External stores help when client state is shared across distant components and Context becomes too broad or noisy.

### Zustand

```ts
import { create } from 'zustand';

type CartStore = {
  items: Record<string, number>;
  add: (productId: string) => void;
  clear: () => void;
};

export const useCartStore = create<CartStore>((set) => ({
  items: {},
  add: (productId) =>
    set((state) => ({
      items: {
        ...state.items,
        [productId]: (state.items[productId] ?? 0) + 1,
      },
    })),
  clear: () => set({ items: {} }),
}));
```

Zustand strengths:

- Small API.
- Selective subscriptions.
- Good for client UI/session state.

### Redux Toolkit

```ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';

type FiltersState = {
  query: string;
  inStockOnly: boolean;
};

const filtersSlice = createSlice({
  name: 'filters',
  initialState: { query: '', inStockOnly: false } satisfies FiltersState,
  reducers: {
    queryChanged(state, action: PayloadAction<string>) {
      state.query = action.payload;
    },
    inStockOnlyToggled(state) {
      state.inStockOnly = !state.inStockOnly;
    },
  },
});

export const { queryChanged, inStockOnlyToggled } = filtersSlice.actions;
export const filtersReducer = filtersSlice.reducer;
```

Redux Toolkit strengths:

- Strong conventions.
- DevTools and action history.
- Predictable workflows for large teams.
- RTK Query option for server data.

Interview answer:

> I choose local state first, then URL state or server cache when appropriate. I use external client stores for shared client state that is too broad for local state and too update-heavy for simple Context.

---

## 9. Testing with React Testing Library

React Testing Library encourages tests that resemble how users interact with the app.

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TodoApp } from './TodoApp';

it('adds a todo', async () => {
  const user = userEvent.setup();
  render(<TodoApp />);

  await user.type(screen.getByLabelText(/new todo/i), 'Write tests');
  await user.click(screen.getByRole('button', { name: /add/i }));

  expect(screen.getByText('Write tests')).toBeInTheDocument();
});
```

Testing guidance:

- Query by role, label, text, placeholder, or test ID as a last resort.
- Assert visible behavior, not internal state.
- Mock network at the boundary with MSW or test doubles.
- Keep reducers and pure helpers unit-tested separately.
- Test error and loading states.

Interview talking point:

> Good React tests give confidence from the user's point of view. If a refactor preserves behavior, the test should usually keep passing.

---

## 10. Error boundaries

Error boundaries catch rendering errors below them in the tree. They do not catch async errors in event handlers or rejected promises unless those errors are surfaced during render.

Class-based boundary:

```tsx
import { Component, ReactNode } from 'react';

type Props = {
  children: ReactNode;
};

type State = {
  hasError: boolean;
};

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false };

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: unknown) {
    console.error(error);
  }

  render() {
    if (this.state.hasError) {
      return <p>Something went wrong.</p>;
    }

    return this.props.children;
  }
}
```

Placement:

- Around route segments.
- Around risky widgets.
- Around micro-frontend boundaries.
- Near Suspense boundaries for cohesive fallback UX.

---

## 11. Scalable React patterns

### Container/presentational split

Keep data and side effects near route/container components; keep visual components reusable.

```tsx
function ProductsRoute() {
  const productsQuery = useProducts();
  return <ProductsView products={productsQuery.data ?? []} />;
}

function ProductsView({ products }: { products: Product[] }) {
  return products.map((product) => <ProductCard key={product.id} product={product} />);
}
```

### Compound components

```tsx
function Tabs({ children }: { children: React.ReactNode }) {
  return <div role="tablist">{children}</div>;
}

Tabs.Tab = function Tab({ children }: { children: React.ReactNode }) {
  return <button role="tab">{children}</button>;
};
```

### State colocation

Put state as close as possible to where it is used. Lift state only when multiple components need to coordinate.

### Avoid over-abstracting

Bad abstractions hide the UI flow. Good abstractions remove repeated complexity while keeping behavior easy to trace.

---

## Advanced interview drill

1. What does concurrent rendering change about render purity?
2. When would you use `startTransition`?
3. Is `useDeferredValue` the same as debounce?
4. What does Suspense handle, and what does it not handle?
5. How are Server Components different from SSR?
6. Why is TanStack Query not a replacement for all state management?
7. When would you choose Zustand versus Redux Toolkit?
8. What do error boundaries catch?
9. What makes a React Testing Library test resilient?
