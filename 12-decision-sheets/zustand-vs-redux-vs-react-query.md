# Zustand vs Redux vs React Query

## The decision

React apps often mix different kinds of state. The key interview move is to separate:

- **Server state/cache**: remote data owned by the backend.
- **Client/UI state**: local selections, modals, filters, drafts, wizard steps.
- **Global client state**: auth shell, theme, feature flags, cross-page UI state.

React Query is primarily for server state. Zustand and Redux are primarily for client state.

## Quick default

- Use **TanStack Query/React Query** for server data fetching, caching, synchronization, retries, invalidation, pagination, mutations, and loading/error states.
- Use **Zustand** for lightweight global client state with minimal boilerplate.
- Use **Redux Toolkit** when you need strict predictable state transitions, devtools, middleware, complex cross-team state, or existing Redux investment.

## Comparison

| Force | React Query | Zustand | Redux Toolkit |
|---|---|---|---|
| Best for | Server cache | Lightweight client/global state | Complex client/global state |
| Owns fetching | Yes | No | Possible, often RTK Query if server state |
| Boilerplate | Low | Very low | Moderate, much lower with RTK than old Redux |
| Devtools | Query devtools | Simple devtools integrations | Excellent Redux DevTools |
| Learning | Query/invalidation mental model | Small API | Actions/reducers/slices/middleware |
| Team governance | Good for API data | Requires store conventions | Strong conventions |
| Offline/persistence | Possible | Possible | Possible; more structured |

## Use React Query when

- Data comes from the server and must be cached.
- You need refetching, retries, stale times, background refresh, pagination, optimistic updates, or mutation invalidation.
- Multiple components need the same API data.
- You want to avoid duplicating remote data in global state.

Do not copy React Query data into Zustand/Redux unless you have a strong reason. That creates two sources of truth.

## Use Zustand when

- You need a small global store for UI state.
- You want simple selectors and direct actions.
- State is not complex enough to justify Redux.
- You need state outside React components but do not need heavy middleware.

Examples:

- Sidebar open/closed.
- Selected workspace.
- Draft assistant settings.
- Local chat composer state.
- Feature tour progress.

## Use Redux Toolkit when

- Many teams touch the same shared state.
- State transitions are complex and benefit from strict reducers.
- You need time-travel debugging, middleware, event-like action logs, or auditability.
- You already have Redux and migration cost is high.
- You use RTK Query for server state and want one official Redux ecosystem.

Examples:

- Complex editor state.
- Multi-step workflows with undo/redo.
- Large enterprise apps with established Redux conventions.
- Apps where action history is valuable for debugging.

## Trade-offs

React Query:

- Great server state model, but not a general-purpose UI state store.
- Requires understanding freshness, invalidation, query keys, and optimistic updates.

Zustand:

- Simple and productive, but can become informal without store boundaries.
- Easy to mutate too much global state if the team lacks conventions.

Redux Toolkit:

- Strong predictability and tooling, but more ceremony than Zustand.
- Old Redux stereotypes are outdated; RTK reduces boilerplate significantly.

## Interview answer script

```text
I would first classify the state. If the data is owned by the server, I use React Query so caching, loading states, retries, invalidation, and mutations are handled consistently. For lightweight global UI state, I use Zustand because it is small and direct. If the app has complex shared state across many teams, needs strict transition logs, middleware, or sophisticated debugging, I use Redux Toolkit.

The main mistake I avoid is putting server cache into a global client store and then manually keeping it in sync. I want one source of truth for each kind of state.
```

## Practical rules

- URL state for shareable filters and navigation.
- Local component state for isolated UI details.
- React Query for API data.
- Zustand for small shared UI/application state.
- Redux Toolkit for complex shared state with strong governance.
- Avoid global state as a reflex; use it when sharing has clear value.
