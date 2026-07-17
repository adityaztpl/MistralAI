# React Testing and Performance

Testing and performance are connected: both ask whether the UI behaves correctly under real user conditions. Good tests give confidence during refactors. Good performance work improves measured user experience instead of adding memoization everywhere.

Interview framing:

> I test behavior through accessible user interactions, and I optimize from measurement. I avoid tests that know too much about implementation details and optimizations that do not address a real bottleneck.

---

## 1. Testing pyramid for React

Useful layers:

- **Unit tests:** reducers, selectors, pure helpers, formatters.
- **Hook tests:** reusable hooks with state/effects.
- **Component tests:** user behavior with React Testing Library.
- **Integration tests:** component plus router/store/query provider.
- **End-to-end tests:** real browser workflows with app/backend boundaries.

React-specific guidance:

- Prefer tests that interact through the DOM.
- Query by role, label, text, placeholder, or alt text.
- Use test IDs only when there is no user-facing query.
- Assert behavior and visible output.
- Mock network at the boundary, not internal implementation details.
- Keep pure logic outside components when it deserves direct unit tests.

---

## 2. React Testing Library mental model

React Testing Library encourages tests that resemble how users use the app.

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

Query priority:

1. `getByRole`
2. `getByLabelText`
3. `getByPlaceholderText`
4. `getByText`
5. `getByDisplayValue`
6. `getByAltText`
7. `getByTitle`
8. `getByTestId`

Async queries:

- `findBy...` waits for an element to appear.
- `waitFor` waits for an assertion to pass.
- `queryBy...` checks absence without throwing.

```tsx
expect(await screen.findByText(/loaded/i)).toBeInTheDocument();
await waitFor(() => expect(saveButton).toBeEnabled());
expect(screen.queryByRole('alert')).not.toBeInTheDocument();
```

---

## 3. Testing user events

Use `@testing-library/user-event` for realistic interactions.

```tsx
const user = userEvent.setup();
await user.click(screen.getByRole('button', { name: /save/i }));
await user.type(screen.getByLabelText(/email/i), 'ada@example.com');
await user.keyboard('{Escape}');
```

Why not only `fireEvent`?

- `userEvent` models higher-level interactions.
- Typing triggers keyboard/input/change events closer to browser behavior.
- Clicks include pointer/mouse/focus behavior.

`fireEvent` is still useful for low-level events that `userEvent` does not model well.

---

## 4. Testing async UI and network calls

Prefer Mock Service Worker (MSW) for network behavior in component/integration tests. It intercepts requests at the network boundary so components still use real fetch/query code.

Conceptual MSW handler:

```ts
import { http, HttpResponse } from 'msw';

export const handlers = [
  http.get('/api/products', () =>
    HttpResponse.json([
      { id: 'p1', name: 'Keyboard' },
      { id: 'p2', name: 'Mouse' },
    ]),
  ),
];
```

Component test:

```tsx
it('renders products from the API', async () => {
  render(<ProductsPage />);

  expect(screen.getByText(/loading/i)).toBeInTheDocument();
  expect(await screen.findByText('Keyboard')).toBeInTheDocument();
  expect(screen.getByText('Mouse')).toBeInTheDocument();
});
```

Error state:

```tsx
it('shows an error message when products fail', async () => {
  server.use(
    http.get('/api/products', () => new HttpResponse(null, { status: 500 })),
  );

  render(<ProductsPage />);

  expect(await screen.findByRole('alert')).toHaveTextContent(/failed/i);
});
```

Interview answer:

> I prefer MSW because the component still calls `fetch` or the query library normally. The test controls the network boundary instead of mocking implementation details inside the component.

---

## 5. Testing hooks

Hooks can be tested through components or with `renderHook`.

```tsx
import { act, renderHook } from '@testing-library/react';
import { useCounter } from './useCounter';

it('increments', () => {
  const { result } = renderHook(() => useCounter());

  act(() => {
    result.current.increment();
  });

  expect(result.current.count).toBe(1);
});
```

Testing hooks with providers:

```tsx
function wrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>;
}

renderHook(() => useAuth(), { wrapper });
```

Testing timers:

```tsx
vi.useFakeTimers();

const { result, rerender } = renderHook(
  ({ value }) => useDebounce(value, 300),
  { initialProps: { value: 'a' } },
);

rerender({ value: 'ab' });

act(() => {
  vi.advanceTimersByTime(300);
});

expect(result.current).toBe('ab');
```

Timer cautions:

- Always restore real timers.
- Wrap timer advancement that causes React updates in `act`.
- Prefer fake timers for deterministic debounce/throttle tests.

---

## 6. Testing routing

Use memory routers for component tests.

```tsx
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

function renderWithRouter(initialEntries = ['/products/42']) {
  const router = createMemoryRouter(
    [
      {
        path: '/products/:productId',
        element: <ProductDetailsPage />,
      },
    ],
    { initialEntries },
  );

  render(<RouterProvider router={router} />);
}
```

Test navigation:

```tsx
it('navigates after login', async () => {
  const user = userEvent.setup();
  renderWithRouter(['/login']);

  await user.type(screen.getByLabelText(/email/i), 'ada@example.com');
  await user.click(screen.getByRole('button', { name: /sign in/i }));

  expect(await screen.findByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
});
```

---

## 7. Testing state management

Reducers:

```ts
it('adds an item', () => {
  expect(cartReducer({ items: [] }, { type: 'itemAdded', productId: 'p1' })).toEqual({
    items: [{ productId: 'p1', quantity: 1 }],
  });
});
```

Selectors:

```ts
expect(selectCartTotal(state)).toBe(42);
```

Zustand:

- Reset store between tests.
- Test actions directly for store logic.
- Test components through rendered behavior.

Redux Toolkit:

- Test reducers as pure functions.
- Use a real store in integration tests.
- Avoid mocking `useSelector`/`useDispatch` unless there is a very specific reason.

TanStack Query:

- Create a fresh `QueryClient` per test.
- Disable retries when testing errors.
- Clear query cache between tests.
- Test loading, success, error, and mutation states.

```tsx
function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}
```

---

## 8. Accessibility in tests

Accessible tests are more resilient because they query the UI the way assistive technologies do.

Good:

```tsx
screen.getByRole('button', { name: /save profile/i });
screen.getByLabelText(/email address/i);
screen.getByRole('alert');
```

If it is hard to query something accessibly, the component may need better markup:

- Add a `<label>` for inputs.
- Use semantic buttons/links.
- Add `aria-label` for icon-only buttons.
- Use `role="alert"` for important error messages.
- Ensure dialog and menu patterns follow accessibility expectations.

Interview answer:

> Testing Library's query priority nudges the component toward accessible markup. If a button cannot be found by role and name, that is often a product bug, not just a test problem.

---

## 9. Performance mental model

React performance issues usually come from:

- Too much work during render.
- Too many components rendering for one update.
- Expensive calculations repeated unnecessarily.
- Large lists without virtualization.
- Large bundles.
- Slow network waterfalls.
- Hydration cost.
- Layout thrashing or expensive DOM work.
- Unstable references causing memoized children to re-render.

Render lifecycle:

1. State/props/context update schedules work.
2. React renders affected components.
3. React diffs element output.
4. React commits host changes.
5. Effects run after commit.

Important distinction:

- A component function running is not the same as DOM replacement.
- Re-rendering is not automatically bad.
- Optimization should target user-visible latency, CPU, memory, network, or interaction responsiveness.

---

## 10. Measuring performance

Tools:

- React DevTools Profiler.
- Browser Performance panel.
- Web Vitals.
- Bundle analyzer.
- Production monitoring.
- User timing marks for custom flows.

React Profiler answers:

- What rendered?
- Why did it render?
- How long did it take?
- Which commits were expensive?

Interview answer:

> I profile first. If the bottleneck is render CPU, I look at state placement, memoization, expensive calculations, and list virtualization. If it is network or bundle size, React memoization will not solve it.

---

## 11. Memoization

`React.memo`:

```tsx
const ProductCard = memo(function ProductCard({ product, onAdd }: ProductCardProps) {
  return (
    <article>
      <h2>{product.name}</h2>
      <button onClick={() => onAdd(product.id)}>Add</button>
    </article>
  );
});
```

`useMemo`:

```tsx
const visibleProducts = useMemo(
  () => expensiveFilter(products, filters),
  [products, filters],
);
```

`useCallback`:

```tsx
const handleAdd = useCallback((productId: string) => {
  addToCart(productId);
}, [addToCart]);
```

Memoization works best when:

- Props are stable.
- Render cost is meaningful.
- The memoized component often receives the same props.
- The dependency list is correct.

Memoization fails when:

- A parent passes new object/array/function values every render.
- The component is cheap.
- State/context updates inside the memoized component still change.
- Custom comparison functions become expensive or incorrect.

---

## 12. State colocation and render scope

Moving state down can reduce re-render scope.

```tsx
function Page() {
  return (
    <>
      <ExpensiveDashboard />
      <SearchBox />
    </>
  );
}

function SearchBox() {
  const [query, setQuery] = useState('');
  return <input value={query} onChange={(event) => setQuery(event.target.value)} />;
}
```

If `query` is in `Page`, the expensive dashboard may render on every keystroke. If only `SearchBox` needs it, keep it there.

Composition trick:

```tsx
function Panel({ children }: { children: React.ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <section>
      <button onClick={() => setIsOpen((value) => !value)}>Toggle</button>
      {isOpen ? children : null}
    </section>
  );
}
```

Passing children can prevent parent state from forcing expensive child creation in some layouts.

---

## 13. Lists and virtualization

For very large lists, rendering fewer DOM nodes matters more than memoizing each row.

Virtualization libraries render only visible items plus overscan.

Concept:

```tsx
function VirtualizedProducts({ products }: { products: Product[] }) {
  return (
    <Virtualizer
      count={products.length}
      estimateSize={() => 72}
      renderItem={(index) => <ProductRow product={products[index]} />}
    />
  );
}
```

Interview notes:

- Stable keys still matter.
- Row heights affect complexity.
- Accessibility and keyboard navigation need attention.
- Infinite loading is separate from virtualization but often combined.

---

## 14. Code splitting and Suspense

Use route-level code splitting first.

```tsx
const AdminPage = lazy(() => import('./AdminPage'));

function AdminRoute() {
  return (
    <Suspense fallback={<AdminSkeleton />}>
      <AdminPage />
    </Suspense>
  );
}
```

Good boundaries:

- Preserve page shell.
- Show meaningful skeletons.
- Avoid hiding already useful content.
- Match independently loading regions.

Bad boundaries:

- One giant app-level fallback for every small load.
- Tiny boundaries that flicker constantly.
- Fallbacks that cause layout shift.

---

## 15. Concurrent rendering performance APIs

`startTransition`:

- Marks updates as non-urgent.
- Helps keep urgent interactions responsive.
- Does not make expensive work cheaper.

`useDeferredValue`:

- Lets a value lag while React renders a lower-priority tree.
- Useful for expensive result areas.
- Not the same as debounce.

`Suspense`:

- Coordinates loading boundaries for code/data that can suspend.
- Boundary placement controls perceived performance.

Interview answer:

> Concurrent features are scheduling tools. They improve responsiveness when work can be prioritized, but I still need to reduce unnecessary work and avoid network waterfalls.

---

## 16. Bundle and network performance

React render optimization cannot fix:

- Huge JavaScript bundles.
- Slow API responses.
- Waterfall data fetching.
- Unoptimized images.
- Missing CDN/cache headers.
- Slow third-party scripts.

Tactics:

- Route-level splitting.
- Lazy-load rarely used widgets.
- Analyze dependencies.
- Prefer server rendering or RSC where appropriate.
- Preload critical resources.
- Cache API responses.
- Avoid duplicate data requests.
- Use image optimization.

---

## 17. Anti-patterns

Testing anti-patterns:

- Testing implementation details such as internal state variable names.
- Mocking every child component by default.
- Using snapshots as the main behavior test.
- Overusing `data-testid`.
- Not testing error and loading states.
- Ignoring accessibility.

Performance anti-patterns:

- Adding `useMemo` everywhere.
- Using array indexes as keys for dynamic lists.
- Storing derived data in state and synchronizing with effects.
- Globalizing state too early.
- Putting every value in Context.
- Rendering thousands of DOM nodes when virtualization is needed.
- Blocking input updates with expensive synchronous work.

---

## Testing checklist

- Does the test use user-visible queries?
- Does it cover loading, success, empty, and error states?
- Does it interact with the UI like a user?
- Does it avoid implementation details?
- Does it use realistic providers/router/store/query setup?
- Does it control network behavior at the boundary?
- Does it clean up timers, mocks, and stores?
- Would the test survive a refactor that preserves behavior?

## Performance checklist

- Have you measured the bottleneck?
- Is the issue CPU, network, bundle, memory, layout, or hydration?
- Can state move closer to where it is used?
- Can expensive derived work be memoized?
- Can a large list be virtualized?
- Can code be split by route?
- Can server data be prefetched, cached, or streamed?
- Are Suspense boundaries placed around meaningful regions?
- Do context values update too broadly?
- Are memoized props actually stable?

## Interview drill

1. What should React component tests assert?
2. Why prefer `userEvent` over `fireEvent` for most interactions?
3. How do you test a component that fetches data?
4. How do you test a custom hook with timers?
5. How do you test React Router navigation?
6. What causes React performance problems?
7. When does `React.memo` help?
8. Why might `useCallback` do nothing useful?
9. How do `startTransition` and `useDeferredValue` help responsiveness?
10. What would you check before optimizing a slow page?
