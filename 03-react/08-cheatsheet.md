# React Interview Cheatsheet

Use this file for final review. It compresses the deep-dive docs into high-signal answers, decision tables, and common pitfalls.

---

## Render model

| Question | Short answer |
| --- | --- |
| What is render? | Calling components to calculate UI from props/state/context. |
| What is commit? | Applying changes to the host environment and running layout/effect phases. |
| Why must render be pure? | React can restart, interrupt, or discard render work before commit. |
| Does re-render mean DOM replacement? | No. React reconciles and commits necessary host changes. |
| What do keys do? | Preserve or reset identity among siblings during reconciliation. |
| What does Strict Mode reveal? | Unsafe render side effects and missing effect cleanup in development. |

Answer template:

> React render should be a pure calculation. State updates schedule work, React renders affected components, reconciles the element tree, and commits the necessary DOM changes. Concurrent rendering means React may pause or abandon render work, so side effects must not happen during render.

---

## State placement

| Need | Use |
| --- | --- |
| One component owns it | `useState` |
| Complex local transitions | `useReducer` |
| Siblings coordinate | Lift state |
| Deep subtree configuration | Context |
| Shareable page state | URL params/search params |
| Remote server data | TanStack Query, route loaders, framework data APIs |
| Lightweight global client state | Zustand |
| Large-team global client state | Redux Toolkit |
| External mutable store | `useSyncExternalStore` |

State answer template:

> I first classify the state. If it is local UI state, I colocate it. If it is shareable navigation state, I put it in the URL. If it is server data, I use a cache/data API. If it is cross-cutting client state, I choose Context, Zustand, or Redux Toolkit based on update frequency and team conventions.

---

## Hooks quick rules

- Hooks must be called in the same order every render.
- Custom hooks share logic, not state by default.
- Effects synchronize with external systems.
- Effects are not for cheap derived data.
- Dependency arrays should include reactive values read by the callback.
- Functional updates solve "next state depends on previous state."
- Refs hold mutable values that do not trigger renders.
- `useMemo` caches values; `useCallback` caches function identity.
- `useTransition` marks non-urgent updates.
- `useDeferredValue` lets a value lag; it is not debounce.

Stale closure answer:

> Each render creates new variables and functions. A callback sees values from the render that created it. I fix stale closures with accurate dependencies, functional state updates, or a ref for long-lived callbacks that need the latest value.

---

## Effects decision table

| Situation | Do this |
| --- | --- |
| Subscribe to external source | `useEffect` with cleanup |
| Start timer | `useEffect` with cleanup |
| Fetch manually | `useEffect` with abort/stale handling |
| Derive `fullName` from props | Compute during render |
| Send analytics for button click | Event handler |
| Measure DOM before paint | `useLayoutEffect` |
| Generate accessible ID | `useId` |
| Read latest value in interval | ref or recreate interval |

Effect answer template:

> I use effects for synchronization after commit. If the logic can run during render or directly in an event handler, it probably does not need an effect.

---

## Context

Good for:

- Theme.
- Locale.
- Auth session metadata.
- Current workspace.
- Feature flags.
- Dependency-style values.

Watch out:

- Provider value identity.
- Broad consumer re-renders.
- Frequently changing large objects.
- Server cache misuse.

Context answer template:

> Context avoids prop drilling for subtree-wide values. It is not automatically selective: consumers re-render when the provider value changes, so I split contexts or use an external store if updates are frequent or slice-based subscriptions matter.

---

## Zustand vs Redux Toolkit

| Topic | Zustand | Redux Toolkit |
| --- | --- | --- |
| Boilerplate | Low | Moderate |
| Conventions | Flexible | Strong |
| DevTools/action trace | Available but less central | Core strength |
| Team scale | Small/medium or flexible teams | Medium/large teams needing consistency |
| Selective subscriptions | Simple selectors | `useSelector` selectors |
| Async conventions | Bring your own | Thunks/listeners/RTK Query |

Answer:

> Zustand is great for lightweight client stores with selective subscriptions. Redux Toolkit is better when a team wants strong conventions, traceable actions, and a mature architecture for complex client state.

---

## TanStack Query

Use for server state:

- Cache.
- Stale time.
- Deduplication.
- Background refetch.
- Pagination/infinite query.
- Mutation state.
- Optimistic updates.
- Invalidation.

Query key rule:

```tsx
useQuery({
  queryKey: ['products', { query, page }],
  queryFn: () => fetchProducts({ query, page }),
});
```

Answer:

> TanStack Query is a server-state cache, not a replacement for all client state. I put URL/search params into query keys and invalidate or update cached data after mutations.

---

## Routing

| Concept | Use |
| --- | --- |
| Path params | Resource identity |
| Search params | Filters, sort, page, view |
| `Link` | Normal navigation |
| `useNavigate` | Imperative navigation after events |
| `Outlet` | Nested route rendering |
| Loader | Route-owned data before render |
| Action | Route form/mutation handling |
| `errorElement` | Route-level error UI |

Protected route answer:

> Client route guards protect UX, not data. The server must enforce authorization. The client can redirect unauthenticated users and preserve the intended destination.

SSR/RSC answer:

> SSR sends HTML for initial render and hydrates on the client. Server Components execute on the server and can avoid shipping component JavaScript to the browser. They often coexist in frameworks but solve different problems.

---

## Testing

Query priority:

1. Role.
2. Label text.
3. Placeholder.
4. Text.
5. Display value.
6. Alt text.
7. Title.
8. Test ID.

Testing answer template:

> I test behavior from the user's perspective with React Testing Library and `userEvent`. I query accessible UI, mock network at the boundary with MSW when possible, and cover loading, success, error, and important edge states.

Async testing:

```tsx
expect(await screen.findByText(/loaded/i)).toBeInTheDocument();
await waitFor(() => expect(button).toBeEnabled());
expect(screen.queryByRole('alert')).not.toBeInTheDocument();
```

Hook testing:

```tsx
const { result } = renderHook(() => useCounter());

act(() => {
  result.current.increment();
});
```

---

## Performance

Main causes:

- Too much render work.
- Too broad render scope.
- Expensive calculations.
- Unstable props into memoized children.
- Huge lists.
- Large bundles.
- Network waterfalls.
- Hydration cost.

Optimization order:

1. Measure.
2. Identify bottleneck type.
3. Colocate state.
4. Remove unnecessary effects/derived state.
5. Memoize expensive work or stable references.
6. Virtualize large lists.
7. Split code by route.
8. Improve data loading/caching.

Memo answer:

> `useCallback` stabilizes function identity; it does not make the function faster. `React.memo` helps only when props are stable and rendering is meaningfully expensive.

Concurrent answer:

> `startTransition` and `useDeferredValue` are scheduling tools. They help prioritize urgent interactions, but they do not reduce CPU work by themselves.

---

## React 19-era quick notes

- `useActionState`: form/action state plus pending flag.
- `useFormStatus`: pending status from a form subtree.
- `useOptimistic`: temporary optimistic UI state before confirmation.
- `use`: reads promises/context in supported async rendering patterns.
- Actions and Server Components are most powerful in frameworks that understand server execution, routing, and streaming.

Answer:

> React 19 leans into actions, async rendering, and optimistic UI. I would explain the core API but also note that framework support determines how these patterns are used in production.

---

## Common bugs and fixes

| Bug | Fix |
| --- | --- |
| Missing effect dependency | Include dependency or restructure logic |
| Infinite effect loop | Move object/function inside effect, memoize, or remove derived state |
| Stale interval callback | Functional update or latest-value ref |
| Input lag from filtering | Keep input urgent; transition/defer expensive result UI |
| Dynamic list uses index keys | Use stable IDs |
| Context causes broad re-renders | Split contexts, memoize value, or use external store |
| Server data stored globally | Use server-state cache |
| Tests fail after refactor | Test behavior, not implementation |
| `React.memo` ineffective | Stabilize props or remove unnecessary memo |
| Hydration mismatch | Avoid client-only values during server render |

---

## Rapid-fire Q&A

**Props vs state?** Props are parent-provided read-only inputs. State is component-owned data that changes over time.

**Controlled vs uncontrolled input?** Controlled input value is driven by React state. Uncontrolled input value is held by the DOM and read through refs/form APIs.

**Why keys?** Keys help React match siblings across renders to preserve or reset identity.

**What does Suspense catch?** Suspense handles loading for components/data that suspend. It is not an error boundary.

**What do error boundaries catch?** Render/lifecycle errors below them. Not arbitrary async/event errors unless surfaced into render.

**Custom hook state shared?** No. Each call owns its own hook state unless the hook uses a shared external store.

**When use reducer?** When transitions are complex, named, or involve multiple related fields.

**When use ref?** DOM access or mutable values that should not trigger render.

**When use URL state?** When state should be shareable, bookmarkable, refresh-safe, or navigation-aware.

**When use external store?** When client state is shared across distant components and Context/local state is not a good fit.

---

## Final interview checklist

- Explain render/commit/effects clearly.
- Mention Strict Mode development behavior.
- Explain hook call order and stale closures.
- Classify state before choosing a tool.
- Put server data in a server-state solution.
- Put shareable page state in the URL.
- Explain Context update behavior.
- Compare Zustand and Redux Toolkit with trade-offs.
- Use route boundaries for layout/data/error/loading.
- Test through user-visible behavior.
- Measure before optimizing.
- Mention accessibility.
- Keep answers specific to product constraints.
