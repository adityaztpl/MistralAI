# React Flashcards

Practice modern React with hooks, rendering, server state, state libraries, and testing.

## 1. What is React core mental model?

**Answer:** A React component is a pure description of UI for given props and state. React renders components, compares output, and updates the DOM. Side effects should not happen during render.

## 2. What are props?

**Answer:** Props are inputs passed from parent to child. They should be treated as read-only and help components stay reusable and predictable.

## 3. What is state?

**Answer:** State is data that changes over time and affects rendering. Keep it as local as possible and lift it only when multiple components need it.

## 4. What is useState?

**Answer:** A hook for local component state. Updates schedule a re-render. Use functional updates when next state depends on previous state.

## 5. What is useEffect for?

**Answer:** useEffect synchronizes React with external systems such as subscriptions, timers, DOM APIs, analytics, or network requests. It is often unnecessary for deriving render state.

## 6. What is the dependency array?

**Answer:** It tells React when an effect should re-run based on referenced values. Missing dependencies can create stale bugs; unnecessary dependencies can cause extra effects.

## 7. What is useMemo?

**Answer:** useMemo caches an expensive calculated value between renders when dependencies are unchanged. Use it for measured performance issues or referential stability, not as a default.

## 8. What is useCallback?

**Answer:** useCallback returns a stable function reference when dependencies do not change. It is useful with memoized children or hook dependencies, but overuse adds complexity.

## 9. What is useReducer?

**Answer:** A hook for state transitions described by actions and a reducer. It fits complex local state, state machines, and workflows better than many related useState calls.

## 10. What is Context?

**Answer:** Context passes values through the tree without prop drilling. It is good for stable global-ish values like theme/auth shell, but frequent updates can cause broad renders if not split carefully.

## 11. What is reconciliation?

**Answer:** React compares previous and next render output to decide DOM updates. Keys help it match list items correctly.

## 12. Why are keys important?

**Answer:** Keys give stable identity to list items. Bad keys like array indexes in mutable lists can cause wrong state reuse and rendering bugs.

## 13. What is controlled input?

**Answer:** An input whose value is driven by React state. It gives explicit control and validation but can require performance care in large forms.

## 14. What is an uncontrolled input?

**Answer:** The DOM owns the input value, often read through refs or form APIs. It can be simpler for some forms and is used by libraries like React Hook Form for performance.

## 15. What is a custom hook?

**Answer:** A function that uses hooks to share stateful logic between components. It should hide implementation details while keeping inputs/outputs clear.

## 16. What is Suspense?

**Answer:** Suspense lets components wait for async resources and show fallback UI. It is used by frameworks/data libraries and does not automatically make slow work fast.

## 17. What are transitions?

**Answer:** Transitions mark updates as non-urgent so React can keep urgent interactions responsive. They help with expensive UI updates but do not replace data optimization.

## 18. What is useDeferredValue?

**Answer:** It lets a value lag behind urgent input updates, useful for keeping typing responsive while expensive results update later.

## 19. What is an error boundary?

**Answer:** A component that catches rendering errors in its subtree and shows fallback UI. It does not catch async errors in event handlers unless those errors enter render state.

## 20. What is React Query for?

**Answer:** React Query manages server state: fetching, caching, refetching, retries, mutations, stale times, and invalidation. It avoids duplicating API data in global client state.

## 21. What is staleTime?

**Answer:** The duration React Query considers cached data fresh. During fresh time it avoids unnecessary refetching. Choose based on data volatility and UX needs.

## 22. What is query invalidation?

**Answer:** Marking cached queries stale after a mutation so they refetch. It keeps server data consistent without manually copying responses everywhere.

## 23. When use Zustand?

**Answer:** Use Zustand for lightweight global client/UI state such as selected workspace, sidebar state, draft settings, or local app preferences.

## 24. When use Redux Toolkit?

**Answer:** Use Redux Toolkit for complex shared client state requiring strict transitions, middleware, devtools, auditability, or large-team governance.

## 25. What is server state vs client state?

**Answer:** Server state is remote data owned by the backend and cached by the frontend. Client state is UI/application state owned by the frontend. Mixing them creates sync bugs.

## 26. What is prop drilling?

**Answer:** Passing props through intermediate components that do not use them. Solve with composition, context, or colocating state; do not reflexively use global state.

## 27. What is composition?

**Answer:** Building UIs by combining components and passing children/render props/configuration. React favors composition over inheritance.

## 28. How do you optimize React performance?

**Answer:** Measure first, keep state local, avoid unnecessary effects, use stable keys, split components, virtualize long lists, memoize selectively, and optimize server data caching.

## 29. What causes unnecessary renders?

**Answer:** State placed too high, context values changing too often, unstable object/function props, derived state stored redundantly, and parent renders cascading into expensive children.

## 30. How do you test React components?

**Answer:** Use React Testing Library to test behavior visible to users: text, roles, interactions, async states, and error paths. Avoid testing implementation details.

## 31. What is hydration?

**Answer:** Hydration attaches React behavior to server-rendered HTML. Mismatches happen when server and client render different output, often due to time/random/browser-only APIs.

## 32. What is a server component?

**Answer:** In React frameworks, server components render on the server and can access server resources without shipping their code to the client. Client components handle interactivity.

## 33. What is a ref?

**Answer:** A ref stores a mutable value that persists across renders without causing re-render. Commonly used for DOM access, timers, or imperative handles.

## 34. What is stale closure?

**Answer:** A function captures old values from a previous render. It commonly appears in effects, timers, and callbacks when dependencies or functional updates are wrong.

## 35. How should auth be handled in React SPAs?

**Answer:** The frontend manages UX state, but the server enforces auth. Prefer secure token/cookie strategies, avoid secrets in the client, handle refresh/logout, and protect routes only as UX.

## 36. What makes a strong React interview answer?

**Answer:** Explain render purity, state placement, effect purpose, server/client state separation, testing by behavior, and measured performance optimization.
