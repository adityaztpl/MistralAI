# Angular 17+ Interview Cheatsheet

Use this for final review. Each answer should include mental model, modern API, trade-off, and example.

---

## 1. Modern Angular defaults

- Standalone components.
- `bootstrapApplication`.
- `provideRouter` and `provideHttpClient`.
- Functional guards and interceptors.
- Built-in control flow: `@if`, `@for`, `@switch`, `@defer`.
- Signals for current state.
- RxJS for async streams.
- OnPush for most reusable components.

## 2. Component API

```ts
product = input.required<Product>();
selected = output<string>();
label = computed(() => this.product().name);
```

Checklist:

- Inputs are data.
- Outputs are events.
- Services hold reusable logic.
- Standalone components import template dependencies.
- Keep templates simple.

## 3. Template syntax

| Syntax | Meaning |
| --- | --- |
| `{{ value }}` | Interpolation |
| `[prop]="expr"` | Property binding |
| `(event)="handler()"` | Event binding |
| `@if` | Conditional block |
| `@for (...; track id)` | Loop with stable identity |
| `@empty` | Empty list state |
| `@defer` | Deferred heavy UI |

## 4. DI scopes

- Root: app-wide singleton.
- Route: feature lifetime.
- Component: subtree lifetime.
- Injection token: runtime representation for config/interfaces.

Interview line:

> Provider scope controls service lifetime and sharing.

## 5. Routing

```ts
{ path: 'products', loadChildren: () => import('./products.routes').then((m) => m.productRoutes) }
```

Guard returns:

- `boolean`
- `UrlTree`
- `Observable<boolean | UrlTree>`
- `Promise<boolean | UrlTree>`

Reminder: guards are UX; server authorization secures data.

## 6. HTTP

- `HttpClient` observables are cold.
- Requests start on subscription.
- Interceptors clone immutable requests.
- Retry idempotent reads carefully.
- Map domain errors in feature services.

## 7. RxJS operator choices

| Need | Operator |
| --- | --- |
| Transform | `map` |
| Filter | `filter` |
| Side effect | `tap` |
| Typeahead pause | `debounceTime` |
| Ignore duplicates | `distinctUntilChanged` |
| Latest wins | `switchMap` |
| Parallel work | `mergeMap` |
| Ordered queue | `concatMap` |
| Ignore duplicate while active | `exhaustMap` |
| Recover | `catchError` |
| Share latest | `shareReplay` |

Flattening drill:

- Search: `switchMap`.
- Login/payment submit: `exhaustMap`.
- Ordered saves: `concatMap`.
- Parallel uploads: `mergeMap`.

## 8. Signals

```ts
count = signal(0);
doubled = computed(() => this.count() * 2);
effect(() => console.log(this.count()));
```

- Read with `signal()`.
- Write with `.set` or `.update`.
- Derive with `computed`.
- Side effects with `effect`.
- Keep writable service signals private.
- Update arrays/objects immutably.

## 9. Signals versus RxJS

Signals:

- Current synchronous state.
- Derived view models.
- Template reads.

RxJS:

- Async streams.
- Cancellation.
- Debounce/throttle.
- Retry/backoff.
- Router/form events.

## 10. Zoneless

Zoneless updates are driven by explicit notifications:

- Signal writes.
- Events.
- Async pipe emissions.
- Framework APIs.
- `markForCheck`.

Migration:

- Use OnPush.
- Use signals and async pipe.
- Audit timers/callbacks.
- Test async UI.

## 11. Forms

- Template-driven: simple static forms.
- Reactive: typed, dynamic, validated, testable forms.
- `value` omits disabled controls.
- `getRawValue()` includes disabled controls.
- Cross-field validators belong on groups.
- Async validators must complete.
- Server validation is authoritative.

## 12. Testing

- Standalone components go in TestBed `imports`.
- Services/fakes go in `providers`.
- Use `HttpTestingController` for HTTP.
- Use fake timers for debounce.
- Assert behavior, not private details.
- Keep E2E for critical flows.

## 13. Performance

Rendering:

- OnPush.
- Signals/computed.
- Stable `track`.
- Avoid template work.
- Virtualize large lists.

Bundle:

- Lazy routes.
- `@defer`.
- Analyze dependencies.
- Enforce budgets.

Network:

- Avoid duplicate cold subscriptions.
- Debounce and cancel stale requests.
- Cache intentionally.
- Paginate large data.

Memory:

- Cleanup subscriptions/listeners/timers.
- Destroy widgets.
- Avoid retaining components in singletons.

## 14. Security

- Angular escapes interpolation.
- Angular sanitizes dangerous bindings.
- Avoid sanitizer bypass APIs.
- Server authorization is required.
- Client validation is UX only.
- Token storage is a threat-model trade-off.
- Use CSRF protection for cookie-authenticated writes.

## 15. SSR and hydration

Benefits:

- Faster perceived first content.
- SEO.
- Social previews.

Risks:

- Server/client DOM mismatch.
- Browser API access on server.
- Duplicate data fetching.
- Personalized cache leaks.

## 16. Rapid-fire answers

1. What starts HTTP? Subscription.
2. Typeahead operator? `switchMap`.
3. Login double-click operator? `exhaustMap`.
4. Ordered saves? `concatMap`.
5. Read signal? Call it.
6. Derived signal? `computed`.
7. Side effect? `effect`.
8. Cleanup subscription? `AsyncPipe`, `toSignal`, or `takeUntilDestroyed`.
9. Guard security? No, server authorization.
10. Interceptor mutation? Clone immutable request.
11. OnPush triggers? Input ref, event, async pipe, signal read, manual mark.
12. First performance step? Measure.

## 17. Final checklist

- Explain standalone bootstrap.
- Build components with inputs/outputs/signals.
- Use built-in control flow.
- Scope providers intentionally.
- Lazy-load routes.
- Write interceptors and guards.
- Choose RxJS operators.
- Bridge RxJS and signals.
- Build typed reactive forms.
- Test components/services/HTTP.
- Debug OnPush and performance.
- Discuss SSR, hydration, and security.

## 18. Architecture prompts

Use this structure for open-ended design questions:

1. Clarify the route, user, and data requirements.
2. Define state ownership: local, feature, shared, server, persisted.
3. Pick rendering strategy: CSR, SSR, prerender, or hybrid.
4. Pick state tools: component signals, service store, RxJS, NgRx.
5. Define API and DTO boundaries.
6. Define loading, empty, success, error, and retry states.
7. Define accessibility requirements.
8. Define testing boundaries.
9. Define performance risks.
10. Define security boundaries.

Strong phrase:

> I start with the smallest state tool that preserves clarity, then introduce heavier architecture only when sharing, debugging, or workflow complexity justify it.

## 19. Debugging prompts

Stale UI:

- Did the state actually change?
- Did the template read the changed state?
- Was a reference mutated in place?
- Is the component OnPush?
- Did an observable emit through `AsyncPipe` or `toSignal`?
- In zoneless mode, was Angular explicitly notified?

Duplicate HTTP:

- Is the observable cold?
- Are multiple template bindings subscribing?
- Is `shareReplay` appropriate?
- Should the request live in a store?
- Is there a refresh/invalidation strategy?

Form bug:

- Is the control enabled or disabled?
- Are validators attached to the right control/group?
- Was `updateValueAndValidity()` called after changing validators?
- Are errors hidden until touched/submitted?
- Are server errors mapped to the right field path?

## 20. Code review checklist

Components:

- Small public API.
- No unnecessary service dependencies in presentational components.
- Required inputs set and documented by type.
- Stable `@for track`.
- Accessible labels and button names.

Services/stores:

- Private writable state.
- Public readonly signals/observables.
- Domain methods instead of external mutation.
- Explicit error/loading state.
- No component instances retained in singletons.

RxJS:

- Correct flattening operator.
- Error handling placed correctly.
- Long-lived subscriptions cleaned up.
- Subjects not exposed publicly.
- Shared HTTP cached intentionally.

Forms:

- Typed controls.
- Cross-field validators on groups.
- Async validators complete.
- Server validation handled.
- Errors are accessible.

Security/performance:

- No sanitizer bypass without review.
- Guards not treated as backend security.
- No heavy work in templates.
- Lazy/defer heavy feature code.
- Measurement plan for performance claims.

## 21. Last-minute answer formulas

Trade-off answer:

> I would use X when the requirement is A because it gives B. I would avoid it when C because the cost is D. In Angular 17+, the concrete API is E.

Bug answer:

> I would reproduce the issue, identify whether it is state, template notification, network, or lifecycle, add a focused failing test if practical, fix the smallest boundary, then verify with the relevant test or measurement.

Performance answer:

> I would classify the bottleneck first: load, network, rendering, or memory. Then I would measure with browser tools or telemetry, make one targeted change, and measure again.

Security answer:

> I separate client UX from enforcement. Angular can hide UI and sanitize templates, but server authorization, validation, and a clear token/CSRF strategy enforce security.
