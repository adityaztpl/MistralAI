# React Interview Prep Curriculum

Modern React is a component model for building interactive UIs from declarative, composable pieces. Strong interview answers explain the rendering model, state ownership, effects, concurrency, data fetching, routing, testing, and performance without treating React as magic.

This curriculum covers React 18 and React 19-era patterns with TypeScript examples. It is written for interview preparation, but the same principles apply to production apps: keep renders pure, colocate state, separate server state from client state, make loading and error states explicit, and optimize from evidence.

## Learning path

1. [01-basics.md](./01-basics.md)
   - JSX, components, props, state, events
   - Lists/keys, forms, conditional rendering, `useEffect`, Vite setup
   - Render purity, reconciliation, Strict Mode, common beginner traps
2. [02-intermediate.md](./02-intermediate.md)
   - Custom hooks, Context, React Router, data fetching
   - `useReducer`, performance, TypeScript, form libraries
   - Dependency arrays, URL state, server-state boundaries
3. [03-advanced.md](./03-advanced.md)
   - Concurrent features, Suspense, server components overview
   - TanStack Query, Zustand/Redux Toolkit, React Testing Library, error boundaries, scalable patterns
4. [04-hooks-deep-dive.md](./04-hooks-deep-dive.md)
   - Hook execution model, stale closures, effects, refs, reducers
   - `useTransition`, `useDeferredValue`, `useSyncExternalStore`, React 19 form hooks
   - Custom hook design, dependency reasoning, hook interview checklists
5. [05-state-management.md](./05-state-management.md)
   - State taxonomy: local, derived, URL, server cache, global client state
   - Context, Zustand, Redux Toolkit, TanStack Query
   - Decision matrices, trade-offs, architecture answers
6. [06-routing-and-data.md](./06-routing-and-data.md)
   - React Router layouts, params, search params, protected routes
   - Data routers, loaders/actions, lazy routes, error boundaries
   - SPA vs SSR vs streaming vs Server Components, data loading strategies
7. [07-testing-and-performance.md](./07-testing-and-performance.md)
   - React Testing Library, user-event, async tests, hook tests, network mocking
   - Performance mental model, Profiler, memoization, virtualization, Suspense/code splitting
   - Interview-ready testing and performance checklists
8. [08-cheatsheet.md](./08-cheatsheet.md)
   - Fast recall tables, common interview questions, answer templates
   - Hook dependency heuristics, state placement rules, testing and performance shortcuts

## Example files

Basics:

- `examples/01-basics/TodoApp.tsx`
- `examples/01-basics/Counter.tsx`

Intermediate:

- `examples/02-intermediate/useFetch.ts`
- `examples/02-intermediate/AuthContext.tsx`
- `examples/02-intermediate/ProtectedRoute.tsx`

Advanced:

- `examples/03-advanced/SearchWithDeferred.tsx`
- `examples/03-advanced/ErrorBoundary.tsx`
- `examples/03-advanced/queryExample.tsx`

Hooks deep dive:

- `examples/04-hooks/useDebounce.ts`
- `examples/04-hooks/useLocalStorage.ts`

State management:

- `examples/05-state/cartStore.ts`
- `examples/05-state/authSlice.ts`

Routing and data:

- `examples/06-routing/routerSetup.tsx`

Testing:

- `examples/07-testing/TodoApp.test.tsx`
- `examples/07-testing/useFetch.test.ts`

## How to study this section

Use three passes:

1. **Concept pass:** Read the docs and summarize each section in your own words.
2. **Implementation pass:** Rebuild the examples without looking, then compare details.
3. **Interview pass:** Answer the drills aloud with trade-offs, not just definitions.

For each topic, practice this answer shape:

1. Define the concept in one or two sentences.
2. Explain why it exists.
3. Show a small example or production scenario.
4. Name at least one pitfall.
5. Explain how you would test or validate it.

## React 18/19 mental model to keep repeating

- Render is a pure calculation from props, state, and context.
- Commit is when React applies changes to the host environment.
- Effects run after commit and synchronize with external systems.
- State updates schedule work; React may batch and prioritize that work.
- Concurrent rendering allows render work to be paused, restarted, or abandoned before commit.
- Strict Mode development behavior intentionally reveals unsafe render/effect assumptions.
- Suspense is a loading boundary for things that integrate with Suspense; it is not a catch-all async mechanism.
- Server Components are not just SSR; they change what code ships to the client and where data can be read.
- React 19 form/action APIs are strongest when the framework understands actions, routing, and server execution.

## Interview strategy

Strong React answers usually:

- Explain rendering as a pure calculation from props and state.
- Keep side effects separate from render logic.
- Clarify what belongs in local state, server cache, URL state, or global client state.
- Mention reconciliation and keys when discussing lists.
- Use hooks carefully and explain dependency arrays instead of hand-waving them away.
- Prefer composition over inheritance.
- Know when optimization hooks help and when they add noise.
- Describe loading, empty, error, and success states explicitly.
- Consider accessibility in component APIs and tests.
- Distinguish framework responsibilities from core React responsibilities.

## Maximum-detail interview checklist

Rendering and components:

- Can you explain render vs commit?
- Can you explain why components must be pure during render?
- Can you describe how keys preserve or reset identity?
- Can you explain why defining a component inside another component can reset child state?
- Can you discuss controlled and uncontrolled component trade-offs?

Hooks:

- Can you state the rules of hooks and why call order matters?
- Can you identify stale closure bugs?
- Can you explain when an effect is unnecessary?
- Can you design a custom hook API with clear inputs and outputs?
- Can you compare `useMemo`, `useCallback`, `memo`, refs, and derived values?

State:

- Can you choose between local state, lifted state, reducer state, Context, Zustand, Redux Toolkit, URL state, and TanStack Query?
- Can you explain why server state is different from client state?
- Can you model impossible UI states with TypeScript discriminated unions?
- Can you explain optimistic updates and rollback behavior?

Routing and data:

- Can you explain nested layouts and `Outlet`?
- Can you choose between route loaders, component fetching, framework data APIs, and query libraries?
- Can you explain how URL search params improve shareability and back/forward behavior?
- Can you place route-level error and loading boundaries?

Testing and performance:

- Can you test components by user behavior instead of implementation details?
- Can you mock network calls at a realistic boundary?
- Can you use fake timers safely for debounce/throttle behavior?
- Can you explain what causes a re-render and when that matters?
- Can you profile before optimizing?
- Can you explain virtualization, code splitting, Suspense boundaries, and memoization trade-offs?

## Common high-signal interview phrases

- "I would keep this state local until another component needs to coordinate with it."
- "That sounds like server state, so I would prefer a cache like TanStack Query over a global client store."
- "I would put this in the URL because it should survive refresh, be shareable, and work with browser navigation."
- "This effect is synchronizing with an external system; if it is only deriving data, I would compute during render."
- "I would measure with the React Profiler before adding memoization."
- "A transition marks non-urgent updates; it does not make slow code fast by itself."
- "Strict Mode double-invocation in development is a signal to make cleanup and render purity correct."
