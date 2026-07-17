# React Interview Prep Curriculum

Modern React is a component model for building interactive UIs with declarative rendering, hooks, concurrent features, and a broad ecosystem for routing, server rendering, data fetching, state, and testing.

This curriculum covers React 18+ and React 19-era patterns, with TypeScript examples and interview-oriented explanations.

## Learning path

1. [01-basics.md](./01-basics.md)
   - JSX, components, props, state, events
   - Lists/keys, forms, conditional rendering, `useEffect`, Vite setup
2. [02-intermediate.md](./02-intermediate.md)
   - Custom hooks, Context, React Router, data fetching
   - `useReducer`, performance, TypeScript, form libraries
3. [03-advanced.md](./03-advanced.md)
   - Concurrent features, Suspense, server components overview
   - TanStack Query, Zustand/Redux Toolkit, React Testing Library, error boundaries, patterns

## Example files

- `examples/01-basics/TodoApp.tsx`
- `examples/01-basics/Counter.tsx`
- `examples/02-intermediate/useFetch.ts`
- `examples/02-intermediate/AuthContext.tsx`
- `examples/02-intermediate/ProtectedRoute.tsx`
- `examples/03-advanced/SearchWithDeferred.tsx`
- `examples/03-advanced/ErrorBoundary.tsx`
- `examples/03-advanced/queryExample.tsx`

## Interview strategy

Strong React answers usually:

- Explain rendering as a pure calculation from props and state.
- Keep side effects separate from render logic.
- Clarify what belongs in local state, server cache, URL state, or global client state.
- Mention reconciliation and keys when discussing lists.
- Use hooks carefully and explain dependency arrays.
- Prefer composition over inheritance.
- Know when optimization hooks help and when they add noise.

## Quick checklist

- Can you explain why React components must be pure during render?
- Can you describe controlled inputs and form state?
- Can you choose between `useState`, `useReducer`, Context, and an external store?
- Can you explain when `useEffect` is unnecessary?
- Can you use Suspense/concurrent APIs without claiming they make code automatically faster?
- Can you test components by user behavior instead of implementation details?
