# Angular Testing and Performance Deep Dive: Confidence, Speed, and Production Readiness

Testing and performance reveal senior Angular judgment: ship behavior with confidence, then measure and optimize the real bottleneck.

Interview framing:

> Start with the mental model, name the modern Angular API, explain the trade-off, then give a concrete production example.

---

## 1. Testing strategy

Use the smallest test that gives confidence: pure unit tests, service tests, component tests, integration tests, and a few E2E tests for critical flows.

```ts
await TestBed.configureTestingModule({ imports: [ProductCardComponent] }).compileComponents();
```

Interview checklist:

- Do not test private details.
- Mock network boundaries.
- Keep E2E focused.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 2. Standalone TestBed

Standalone components go in `imports`, not declarations. Providers are overridden at the TestBed boundary.

```ts
TestBed.configureTestingModule({ imports: [TodoComponent], providers: [{ provide: TodoApi, useValue: fakeApi }] });
```

Interview checklist:

- Set required inputs.
- Call detectChanges intentionally.
- Use fakes.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 3. Component behavior

Component tests should assert visible text, ARIA roles, button state, output events, and service calls caused by user actions.

```ts
fixture.componentRef.setInput('product', product);
fixture.detectChanges();
```

Interview checklist:

- Prefer user-visible assertions.
- Trigger real DOM events.
- Cover empty/error states.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 4. Signals and stores

Signal stores can often be tested synchronously because computed values update immediately after state transitions.

```ts
store.add('Write tests');
expect(store.remaining().length).toBe(1);
```

Interview checklist:

- Assert public readonly signals.
- Do not mutate private signals.
- Test computed outputs.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 5. HTTP testing

Use `provideHttpClientTesting()` and `HttpTestingController` to assert requests and flush responses.

```ts
const req = http.expectOne('/api/products');
expect(req.request.method).toBe('GET');
req.flush([]);
```

Interview checklist:

- Verify requests.
- Flush errors too.
- Call `verify`.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 6. Guards and interceptors

Functional APIs can be tested inside an injection context with mocked dependencies.

```ts
const result = TestBed.runInInjectionContext(() => authGuard(route, state));
```

Interview checklist:

- Return UrlTree for redirects.
- Test cloned headers.
- Keep guards thin.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 7. Rendering performance

Rendering performance depends on change detection, DOM size, template work, list identity, and component boundaries.

```ts
@for (item of items(); track item.id) { <app-row [item]="item" /> }
```

Interview checklist:

- Use OnPush.
- Use stable track.
- Avoid template sort/filter calls.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 8. Bundle performance

Initial load improves when heavy routes and components are lazy or deferred.

```ts
{ path: 'admin', loadChildren: () => import('./admin.routes').then((m) => m.adminRoutes) }
```

Interview checklist:

- Lazy-load routes.
- Use @defer.
- Analyze dependencies.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 9. Network and memory

Network performance needs deduping, caching, pagination, and cancellation. Memory performance needs cleanup for subscriptions, listeners, timers, and widgets.

```ts
stream.pipe(takeUntilDestroyed(this.destroyRef)).subscribe();
```

Interview checklist:

- Avoid duplicate cold HTTP subscriptions.
- Debounce search.
- Cleanup external resources.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 10. SSR and monitoring

SSR can improve SEO and perceived load, but hydration consistency and production telemetry matter.

```ts
if (isPlatformBrowser(platformId)) { localStorage.setItem('theme', 'dark'); }
```

Interview checklist:

- Guard browser APIs.
- Measure LCP/INP/CLS.
- Use real user monitoring.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## Final interview drill

Practice answering each topic in this order:

1. Define the concept in one sentence.
2. Explain when you use it.
3. Explain when you avoid it.
4. Write or describe a small Angular 17+ standalone example.
5. Name a testing strategy.
6. Name one production pitfall.

---

## Appendix A. Test selection matrix

| Risk | Best first test | Why |
| --- | --- | --- |
| Pure validation rule | Unit test | Fast and precise |
| Signal store transition | Service/store test | Tests public state contract |
| HTTP endpoint mapping | Service + `HttpTestingController` | Verifies URL, method, body, response mapping |
| Button changes UI | Component test | Matches user-visible behavior |
| Guard redirect | Functional guard test | Fast router decision coverage |
| Interceptor header | HTTP test with interceptor provider | Verifies immutable clone behavior |
| Checkout happy path | E2E | Covers integrated critical revenue flow |
| Visual layout | Visual regression or screenshot test | Unit tests do not catch layout drift |

## Appendix B. Performance triage worksheet

Load problem:

- What is the initial JavaScript size?
- Which chunks are loaded before first interaction?
- Are heavy libraries in the root bundle?
- Would `loadChildren`, `loadComponent`, or `@defer` move work later?
- Would SSR/prerender improve user-perceived first content?

Runtime problem:

- Which interaction is slow?
- How many components are checked?
- Is DOM churn caused by unstable `track`?
- Are templates calling expensive functions?
- Is a large list rendered without virtualization?

Network problem:

- Are duplicate requests caused by multiple cold subscriptions?
- Are independent requests accidentally serialized?
- Are stale requests canceled when route params or search queries change?
- Are large responses paginated or streamed?
- Are cache headers and invalidation rules clear?

Memory problem:

- Are subscriptions cleaned up?
- Are listeners and intervals removed?
- Are third-party widgets destroyed?
- Do singleton services retain component references?
- Does `shareReplay` hold large stale values forever?

## Appendix C. CI and monitoring checklist

- Run unit/component tests on every pull request.
- Include lint/type-check where the repo supports it.
- Enforce bundle budgets for initial chunks.
- Track core web vitals: LCP, INP, CLS.
- Track route transition timings for critical flows.
- Log API correlation IDs without sensitive data.
- Alert on client error spikes after deploy.
- Use production telemetry to confirm performance fixes.
