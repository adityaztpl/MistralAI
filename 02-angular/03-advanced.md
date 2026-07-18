# Angular Advanced: Signals, Zoneless Change Detection, Structural Directives, SSR, Performance, Testing, Architecture, Security

Advanced Angular interviews test whether you can reason about rendering, state, architecture, and production constraints. This guide focuses on the "why" behind modern Angular choices.

## 1. Signals deep dive

Signals are Angular's fine-grained reactive primitive. They represent current values and let Angular track exactly which computations and templates depend on those values.

### Signal types

```ts
import { computed, effect, signal } from '@angular/core';

const count = signal(0); // WritableSignal<number>
const doubled = computed(() => count() * 2); // Signal<number>

effect(() => {
  console.log(`Count changed to ${count()}`);
});

count.update((value) => value + 1);
```

Important details:

- Read a signal by calling it: `count()`.
- Write with `.set(value)` or `.update(fn)`.
- `computed` is lazy and memoized.
- Dependencies are tracked dynamically. If a branch does not read a signal, that signal is not a dependency for that run.
- `effect` is for side effects, not deriving state.

### Dynamic dependency example

```ts
const showDetails = signal(false);
const user = signal({ name: 'Ada', role: 'admin' });

const label = computed(() => {
  if (!showDetails()) {
    return 'Hidden';
  }

  return `${user().name} (${user().role})`;
});
```

When `showDetails()` is false, `label` does not depend on `user()`.

### Inputs, outputs, and models

Modern Angular exposes signal-friendly component APIs:

```ts
import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-user-row',
  standalone: true,
  template: `
    <button type="button" (click)="selected.emit(id())">
      {{ displayName() }}
    </button>
  `,
})
export class UserRowComponent {
  id = input.required<number>();
  firstName = input.required<string>();
  lastName = input.required<string>();
  selected = output<number>();

  displayName = computed(() => `${this.firstName()} ${this.lastName()}`);
}
```

### Signals and RxJS interop

Use `toSignal` when an observable feeds template state:

```ts
import { toSignal } from '@angular/core/rxjs-interop';

products = toSignal(this.productApi.list(), {
  initialValue: [],
});
```

Use `toObservable` when signal changes need RxJS operators:

```ts
import { toObservable } from '@angular/core/rxjs-interop';

query$ = toObservable(this.query).pipe(
  debounceTime(250),
  distinctUntilChanged(),
);
```

Interview talking point:

> Signals are not a replacement for every observable. Signals model current state. Observables model events and asynchronous sequences. A good Angular design often uses both.

### Avoid signal pitfalls

- Do not use `effect` to write derived state that should be `computed`.
- Avoid mutating objects inside a signal without setting a new reference.
- Keep effects small and cleanup-aware.
- Do not hide expensive work inside frequently read computed values.

---

## 2. Zoneless change detection

Historically, Angular used Zone.js to know when async tasks completed and then ran change detection. Modern Angular supports moving toward zoneless apps, where updates are driven by explicit framework notifications such as signals, events, async pipe emissions, and manual marks.

Conceptual bootstrap:

```ts
import { bootstrapApplication } from '@angular/platform-browser';
import { provideExperimentalZonelessChangeDetection } from '@angular/core';
import { AppComponent } from './app/app.component';

bootstrapApplication(AppComponent, {
  providers: [provideExperimentalZonelessChangeDetection()],
});
```

Why zoneless matters:

- Less global monkey-patching.
- More predictable rendering triggers.
- Better alignment with signals.
- Potential performance wins in large apps.

Migration considerations:

- Prefer signals and `AsyncPipe`.
- Avoid assuming arbitrary timers automatically refresh templates.
- Replace manual subscriptions with signal bridges or `ChangeDetectorRef.markForCheck()`.
- Test components that depend on async work.

Interview framing:

> Zoneless Angular shifts the app from "anything async might cause checking" toward explicit reactivity. It rewards components that model state with signals, async pipe, and immutable updates.

---

## 3. Custom structural directives

Structural directives create and destroy embedded views. The `*` syntax is shorthand around an `ng-template`.

Usage:

```html
<section *appHasPermission="'admin'">
  Admin tools
</section>
```

Implementation:

```ts
import {
  Directive,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  input,
} from '@angular/core';
import { AuthzService } from './authz.service';

@Directive({
  selector: '[appHasPermission]',
  standalone: true,
})
export class HasPermissionDirective {
  appHasPermission = input.required<string>();

  private readonly templateRef = inject(TemplateRef<unknown>);
  private readonly viewContainer = inject(ViewContainerRef);
  private readonly authz = inject(AuthzService);
  private hasView = false;

  constructor() {
    effect(() => {
      const allowed = this.authz.hasPermission(this.appHasPermission());

      if (allowed && !this.hasView) {
        this.viewContainer.createEmbeddedView(this.templateRef);
        this.hasView = true;
      } else if (!allowed && this.hasView) {
        this.viewContainer.clear();
        this.hasView = false;
      }
    });
  }
}
```

Interview talking point:

> Structural directives are lower-level than components. They are useful when reusable behavior must control whether a template exists, not just how an element looks.

---

## 4. SSR and hydration

Server-side rendering produces HTML on the server so the browser receives useful content before JavaScript finishes loading. Hydration reuses that server-rendered DOM and attaches Angular behavior without fully re-rendering from scratch.

Benefits:

- Faster first contentful paint.
- Better SEO for public pages.
- Better previews for crawlers and social bots.
- Improved perceived performance on slow devices.

Trade-offs:

- Server cost and operational complexity.
- Browser-only APIs need guards.
- State transfer must avoid duplicate fetching.
- Hydration mismatches can cause bugs.

Basic provider:

```ts
import { provideClientHydration } from '@angular/platform-browser';

bootstrapApplication(AppComponent, {
  providers: [provideClientHydration()],
});
```

Browser API guard:

```ts
import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID, inject } from '@angular/core';

const platformId = inject(PLATFORM_ID);

if (isPlatformBrowser(platformId)) {
  localStorage.setItem('theme', 'dark');
}
```

Interview talking point:

> SSR improves initial delivery, but hydration correctness depends on the server and client rendering the same DOM for the first pass. Browser-only side effects should run after the app is safely on the client.

---

## 5. Performance

Angular performance is usually improved by reducing unnecessary work, shipping less JavaScript, and avoiding repeated DOM churn.

### Rendering performance checklist

- Use `ChangeDetectionStrategy.OnPush` for pure presentational components.
- Use signals for local state and derived values.
- Use `@for (...; track item.id)` for stable identity.
- Avoid calling expensive functions in templates.
- Split large components into focused children.
- Use `AsyncPipe` instead of manual subscription state.
- Use virtual scrolling for large lists.

### Bundle performance checklist

- Lazy load feature routes.
- Avoid importing large libraries into root.
- Prefer route-level providers for feature-only services.
- Audit bundle output when adding dependencies.
- Use modern image formats and responsive images.

### Runtime example with `@defer`

```html
@defer (on viewport) {
  <app-heavy-chart [data]="chartData()" />
} @placeholder {
  <p>Chart will load when visible.</p>
} @loading {
  <p>Loading chart...</p>
}
```

### Track expression

```html
@for (message of messages(); track message.id) {
  <app-message-row [message]="message" />
}
```

Interview answer:

> I first measure the problem. If rendering is slow, I inspect change detection, list identity, template work, and component boundaries. If loading is slow, I inspect bundle size, lazy loading, network waterfalls, and SSR/hydration opportunities.

---

## 6. Testing with Jasmine or Jest

Angular tests typically use TestBed to configure component dependencies.

### Component test

```ts
import { render, screen } from '@testing-library/angular';
import { CounterComponent } from './counter.component';

it('increments the counter', async () => {
  await render(CounterComponent);

  screen.getByRole('button', { name: /increment/i }).click();

  expect(screen.getByText(/count: 1/i)).toBeTruthy();
});
```

### Service test

```ts
import { TestBed } from '@angular/core/testing';
import { TodoService } from './todo.service';

describe(TodoService.name, () => {
  it('adds a todo', () => {
    const service = TestBed.inject(TodoService);

    service.add('Write tests');

    expect(service.list()).toContain('Write tests');
  });
});
```

### HTTP test

```ts
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

beforeEach(() => {
  TestBed.configureTestingModule({
    providers: [ProductApi, provideHttpClient(), provideHttpClientTesting()],
  });
});

it('loads products', () => {
  const api = TestBed.inject(ProductApi);
  const http = TestBed.inject(HttpTestingController);

  api.getProducts().subscribe((products) => {
    expect(products.length).toBe(1);
  });

  http.expectOne('/api/products').flush([{ id: 1, name: 'Keyboard', price: 99 }]);
});
```

Testing interview points:

- Prefer user-visible assertions for components.
- Mock network boundaries, not implementation details.
- Test validators as pure functions.
- Test route guards as functions with mocked dependencies.
- Keep state-store tests focused on inputs and outputs.

---

## 7. Micro-frontends note

Micro-frontends split a large frontend into independently owned and sometimes independently deployed pieces. Angular teams commonly use module federation, custom elements, iframes, or route-level composition.

Potential benefits:

- Independent team ownership.
- Smaller deploy blast radius.
- Different release cadences.
- Gradual migration from legacy stacks.

Costs:

- Shared dependency versioning.
- Cross-app routing and auth consistency.
- Duplicate runtime bundles.
- Design system drift.
- Harder local development and testing.

Interview talking point:

> I would not choose micro-frontends just because the codebase is large. I would choose them when organizational boundaries and deployment independence matter enough to justify runtime and operational complexity.

---

## 8. Security and XSS

Angular protects against many XSS risks by escaping interpolated values and sanitizing dangerous bindings.

Safe by default:

```html
<p>{{ userProvidedText }}</p>
```

Potentially dangerous:

```html
<div [innerHTML]="htmlFromUser"></div>
```

Angular sanitizes `innerHTML`, but the safest approach is to avoid rendering user-controlled HTML. If rich text is required, sanitize on the server and client with a trusted sanitizer policy.

### DomSanitizer caution

```ts
import { DomSanitizer } from '@angular/platform-browser';

// Only do this for content that is truly trusted.
trustedHtml = this.sanitizer.bypassSecurityTrustHtml(adminAuthoredHtml);
```

`bypassSecurityTrustHtml` disables Angular's sanitizer for that value. It should be rare and reviewed.

### Other client security topics

- Use server-side authorization for every protected API.
- Store tokens carefully; understand XSS versus CSRF trade-offs.
- Use Content Security Policy where possible.
- Do not put secrets in frontend code.
- Validate file uploads on the server.
- Avoid logging sensitive data.

Interview answer:

> Angular's template compiler and sanitizer reduce XSS risk, but they do not make arbitrary HTML safe. The strongest defense is not rendering untrusted HTML; when it is required, sanitize it deliberately and avoid bypass APIs unless the source is genuinely trusted.

---

## Advanced interview drill

1. How do signals track dependencies?
2. Why should derived state usually be `computed` instead of an `effect` that writes another signal?
3. What changes when Angular runs zoneless?
4. How does hydration differ from a normal client render?
5. What are the trade-offs of micro-frontends?
6. How would you investigate an Angular performance issue?
7. What makes a good component test?
8. Why is `bypassSecurityTrustHtml` risky?

---


## 9. Advanced signal architecture

Advanced signal architecture treats the application as a graph of source state, derived state, and side effects.

Recommended layering:

1. Writable signals hold source-of-truth state.
2. Computed signals derive view models.
3. Effects synchronize with imperative systems.
4. RxJS handles async sequences, cancellation, and retries.
5. Components read signals directly in templates.

```ts
readonly vm = computed(() => ({
  title: this.filters().query ? `Results for ${this.filters().query}` : 'All products',
  products: this.filteredProducts(),
  canClearFilters: this.hasFilters(),
}));
```

Checklist:

- Avoid effects that derive state.
- Use `untracked` only for incidental reads.
- Keep effects idempotent and cleanup-aware.
- Avoid deep mutation inside signal values.

## 10. Zoneless migration strategy

Zoneless migration is incremental.

1. Turn on OnPush in leaf components.
2. Replace subscribe-and-assign fields with `AsyncPipe`, `toSignal`, or signals.
3. Audit timers, third-party callbacks, and browser APIs.
4. Use signal writes or `markForCheck()` at imperative boundaries.
5. Test async UI workflows.

Interview answer:

> Zoneless is less about a provider and more about explicit reactivity. I remove hidden Zone.js assumptions before enabling it broadly.

## 11. SSR, hydration, and consistency

Hydration depends on the server and client first render matching.

Risks:

- `Date.now()` or random values in first render.
- Browser-only APIs on the server.
- Different locale/timezone output.
- Duplicate client refetch before hydration settles.
- Third-party scripts mutating Angular-owned DOM.

Checklist:

- Guard browser APIs.
- Reuse transfer state.
- Avoid leaking per-user data through caches.
- Monitor hydration mismatch warnings.

## 12. Performance investigation playbook

Classify the issue before optimizing.

| Problem | Investigate |
| --- | --- |
| Slow initial load | bundles, network, SSR, cache |
| Slow interaction | change detection, DOM churn, template work |
| Slow data | backend, duplicate requests, waterfall |
| Memory growth | subscriptions, listeners, cached values |

Checklist:

- Measure before changing code.
- Change one thing at a time.
- Use stable `track`.
- Virtualize large lists.
- Lazy-load heavy routes and defer heavy UI.

## 13. Advanced testing strategy

Use the smallest useful test.

- Pure tests for validators and mappers.
- Service tests for stores and API clients.
- Component tests for rendered behavior.
- Integration tests for routed slices.
- E2E tests for critical journeys.

Interview checklist:

- Test standalone components through `imports`.
- Replace providers with fakes.
- Use `HttpTestingController`.
- Avoid private implementation assertions.

## 14. Advanced security model

Angular helps with XSS but does not replace application security.

Layers:

- Template escaping and sanitization.
- CSP and Trusted Types where possible.
- Server authorization for every protected API.
- CSRF protection for cookie-authenticated writes.
- Careful token storage based on threat model.
- Avoid sanitizer bypass APIs unless content is truly trusted.

## 15. Architecture decision framework

For open-ended design prompts:

1. Clarify requirements.
2. Identify state shape and ownership.
3. Choose rendering strategy.
4. Choose state tools.
5. Define feature boundaries.
6. Define loading/error states.
7. Define testing strategy.
8. Define performance and security constraints.

## 16. Advanced anti-patterns

- Global stores for route-local state.
- Effects used as an event bus.
- Sanitizer bypass as a shortcut.
- Micro-frontends for simple code organization.
- Blanket retry on all HTTP methods.
- Optimizations without measurements.
