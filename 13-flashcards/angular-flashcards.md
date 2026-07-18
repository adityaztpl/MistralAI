# Angular Flashcards

Practice modern Angular: standalone APIs, signals, RxJS, routing, forms, and performance.

## 1. What is a standalone component?

**Answer:** A component that declares its own imports and can be used without an NgModule. Modern Angular favors standalone APIs because they reduce module ceremony and make lazy loading and feature boundaries clearer.

## 2. What is dependency injection in Angular?

**Answer:** DI provides services and values to components, directives, pipes, guards, and other services. Providers can be app-wide, route-scoped, component-scoped, or platform-scoped, which controls lifetime and sharing.

## 3. What is change detection?

**Answer:** Angular updates the DOM when application state changes. Modern Angular can use signals and zoneless patterns, while traditional apps use zone.js to trigger checks after async events.

## 4. What is OnPush?

**Answer:** OnPush limits component checks to input reference changes, events, async pipe emissions, signal updates, or manual marks. It improves performance when state is immutable and data flow is clear.

## 5. What are signals?

**Answer:** Signals are synchronous reactive values with dependency tracking. Templates and computed values update when signals change. They are excellent for local UI state and derived values.

## 6. What is computed?

**Answer:** A computed signal derives a value from other signals and recalculates only when dependencies change. It replaces many manual derived-state subscriptions.

## 7. What is effect?

**Answer:** An effect runs side-effect code when signals it reads change. Use sparingly for integration side effects, not for core business logic that should be explicit.

## 8. When use RxJS instead of signals?

**Answer:** Use RxJS for async streams over time: HTTP composition, websockets, debounce, retry, cancellation, switchMap, mergeMap, and event pipelines. Signals are better for synchronous state.

## 9. What is the async pipe?

**Answer:** The async pipe subscribes to an observable or promise, renders the latest value, and unsubscribes automatically. It reduces manual subscription leaks.

## 10. What is takeUntilDestroyed?

**Answer:** An Angular helper for completing subscriptions when an injection context is destroyed. It helps avoid leaks for imperative subscriptions.

## 11. What is a reactive form?

**Answer:** A model-driven form built from FormControl, FormGroup, and validators. It is explicit, testable, and better for complex validation and dynamic forms than template-driven forms.

## 12. Template-driven vs reactive forms?

**Answer:** Template-driven forms are simpler for small forms and rely heavily on templates. Reactive forms define the form model in TypeScript, scale better for complex validation, and are easier to unit test.

## 13. What is an HTTP interceptor?

**Answer:** A function or class that intercepts HTTP requests and responses for auth tokens, correlation IDs, logging, retries, and error mapping.

## 14. What is a route guard?

**Answer:** A guard controls navigation based on auth, permissions, unsaved changes, or feature flags. Modern Angular supports functional guards.

## 15. What is lazy loading?

**Answer:** Loading route code only when needed to reduce initial bundle size. Angular supports lazy route configs and standalone component loading.

## 16. What is a resolver?

**Answer:** A resolver fetches data before route activation. It can simplify components but may delay navigation; consider skeleton loading for slower data.

## 17. What is content projection?

**Answer:** Content projection lets a parent pass markup into a child component using ng-content. It supports reusable layout and design-system components.

## 18. What are directives?

**Answer:** Directives attach behavior to DOM elements or templates. Attribute directives change behavior/appearance; structural directives change DOM structure.

## 19. What are pipes?

**Answer:** Pipes transform values in templates. Pure pipes are cached by input identity and should be side-effect free.

## 20. How do Angular services manage state?

**Answer:** Services can hold shared state with signals, observables, or plain fields plus methods. Keep boundaries clear and avoid turning every service into a global mutable bag.

## 21. What is NgRx?

**Answer:** NgRx is a Redux-inspired state management library for Angular. It fits complex shared state, predictable actions/reducers/effects, and strong devtools, but adds ceremony.

## 22. How do you secure Angular apps?

**Answer:** Keep tokens safe, use server-side auth enforcement, avoid unsafe HTML, rely on Angular sanitization, protect routes for UX only, and never put secrets in the frontend.

## 23. What is XSS?

**Answer:** Cross-site scripting injects malicious script into a page. Angular escapes template values by default, but bypassSecurityTrust APIs and innerHTML require extreme care.

## 24. What is SSR/hydration?

**Answer:** Server-side rendering produces HTML on the server for faster first paint and SEO; hydration attaches client behavior to that HTML. Watch for browser-only APIs and state mismatches.

## 25. What is zoneless Angular?

**Answer:** A setup that avoids zone.js and relies on explicit reactivity like signals to trigger updates. It can improve performance and predictability but requires modern patterns.

## 26. How do you optimize Angular performance?

**Answer:** Lazy load routes, use OnPush/signals, trackBy for lists, avoid heavy template functions, split bundles, virtualize large lists, and measure with profiling tools.

## 27. What is trackBy?

**Answer:** A function that gives Angular stable identities for list items so DOM nodes are reused correctly. It improves performance and prevents UI state loss in repeated lists.

## 28. How do you test Angular components?

**Answer:** Use TestBed or component testing tools, assert visible behavior, mock services at boundaries, and test forms, outputs, routing, and async states.

## 29. What is a functional interceptor or guard?

**Answer:** A modern Angular API that defines interceptors or guards as functions, often using inject() for dependencies. It reduces class ceremony.

## 30. What is inject()?

**Answer:** A function that retrieves dependencies from Angular DI within an injection context. It is common in standalone APIs, functional guards, interceptors, and services.

## 31. What is a feature module in legacy Angular?

**Answer:** A module grouping related declarations, providers, and routes. Standalone APIs reduce the need, but legacy apps still use modules heavily.

## 32. How should API errors be handled in Angular?

**Answer:** Map errors centrally in interceptors/services, show user-friendly messages, preserve correlation IDs for support, and avoid leaking backend internals.

## 33. What belongs in a component vs service?

**Answer:** Components own presentation and user interactions. Services own reusable business logic, data access, shared state, and integration boundaries.

## 34. How do you handle auth in Angular?

**Answer:** Use an auth service, interceptors for tokens when appropriate, guards for UX navigation, server-side authorization for real protection, refresh handling, and safe logout.

## 35. What is a smart vs presentational component?

**Answer:** Smart components fetch/manage state; presentational components render inputs and emit outputs. It improves reuse and testability, though modern patterns can be flexible.

## 36. What is a good Angular interview closing point?

**Answer:** Emphasize clear data flow, DI, reactive state choice, testing, performance measurement, and server-side security enforcement.
