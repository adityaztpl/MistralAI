# Angular Basics: Components, Templates, DI, Routing, Forms, HTTP

## What Angular is

Angular is a TypeScript-first application framework for building client-side and full-stack web applications. It provides an opinionated platform: component rendering, declarative templates, dependency injection, routing, forms, HTTP, test utilities, build tooling, and SSR support.

Modern Angular applications should usually start with:

- Standalone components instead of NgModule-heavy architecture.
- `bootstrapApplication()` in `main.ts`.
- Functional providers such as `provideRouter()` and `provideHttpClient()`.
- Signals for local synchronous state when appropriate.
- RxJS for async streams, cancellation, and event sequences.

Interview talking point:

> Angular is more than a view library. It gives teams consistent primitives for UI, routing, forms, dependency injection, HTTP, and testing. That consistency is valuable in large applications because architectural choices are explicit and framework-supported.

---

## 1. Components

A component is the main UI building block. It combines:

- A TypeScript class for state and behavior.
- A template for declarative HTML.
- Optional styles.
- Metadata through `@Component`.

Modern standalone component:

```ts
import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-counter',
  standalone: true,
  template: `
    <section>
      <p>Count: {{ count() }}</p>
      <button type="button" (click)="increment()">Increment</button>
    </section>
  `,
})
export class CounterComponent {
  count = signal(0);

  increment(): void {
    this.count.update((value) => value + 1);
  }
}
```

Key ideas:

- Angular instantiates components as classes.
- Template expressions read component members.
- Event bindings call component methods.
- Standalone components declare their own imports.

Interview checklist:

- Can you explain the role of the component decorator?
- Can you describe component lifecycle at a high level?
- Can you identify what belongs in a component versus a service?

---

## 2. Templates

Angular templates are HTML plus Angular syntax. They are compiled, type-checked, and connected to the component class.

Common syntax:

| Syntax | Name | Example |
| --- | --- | --- |
| `{{ value }}` | Interpolation | `<h1>{{ title }}</h1>` |
| `[property]="expr"` | Property binding | `<img [src]="avatarUrl">` |
| `(event)="handler()"` | Event binding | `<button (click)="save()">Save</button>` |
| `[(ngModel)]="value"` | Two-way binding | `<input [(ngModel)]="name">` |
| `@if` | Built-in control flow | `@if (user) { ... }` |
| `@for` | Built-in loop | `@for (item of items; track item.id) { ... }` |

Modern control flow example:

```html
@if (isLoading()) {
  <p>Loading...</p>
} @else if (items().length === 0) {
  <p>No items yet.</p>
} @else {
  <ul>
    @for (item of items(); track item.id) {
      <li>{{ item.name }}</li>
    }
  </ul>
}
```

Interview talking point:

> Angular templates are not arbitrary JavaScript. They are declarative expressions compiled by Angular, which enables template type checking, security sanitization, and performance optimizations.

---

## 3. Data binding

Angular supports several binding directions.

### Interpolation

```html
<h2>Welcome, {{ userName }}</h2>
```

Use interpolation for text content. Angular escapes interpolated values to protect against XSS.

### Property binding

```html
<button [disabled]="isSaving">Save</button>
```

Use property binding for DOM properties or component inputs.

### Attribute, class, and style binding

```html
<td [attr.colspan]="columnSpan">Total</td>
<button [class.active]="selected">Filter</button>
<p [style.color]="hasError ? 'crimson' : 'inherit'">Status</p>
```

### Event binding

```html
<input (input)="onSearch($event)" />
```

For stronger typing, use template references or reactive forms instead of repeatedly casting raw events.

### Two-way binding

```html
<input [(ngModel)]="email" name="email" />
```

Two-way binding is convenient for simple forms. In larger workflows, reactive forms are easier to validate, test, and compose.

---

## 4. Directives

Directives attach behavior to DOM elements or templates.

Types:

- Component directives: components are directives with templates.
- Attribute directives: change appearance or behavior of an existing element.
- Structural directives: add, remove, or repeat DOM blocks.

Attribute directive example:

```ts
import { Directive, ElementRef, HostListener, inject } from '@angular/core';

@Directive({
  selector: '[appHighlight]',
  standalone: true,
})
export class HighlightDirective {
  private readonly element = inject(ElementRef<HTMLElement>);

  @HostListener('mouseenter')
  show(): void {
    this.element.nativeElement.style.backgroundColor = 'lemonchiffon';
  }

  @HostListener('mouseleave')
  hide(): void {
    this.element.nativeElement.style.backgroundColor = '';
  }
}
```

Structural directive usage, old and new:

```html
<!-- Modern built-in control flow -->
@if (isLoggedIn) {
  <app-dashboard />
}

<!-- Traditional structural directive -->
<app-dashboard *ngIf="isLoggedIn"></app-dashboard>
```

Interview talking point:

> Attribute directives are good for reusable DOM behavior. Structural directives shape the DOM by controlling embedded views. Angular's newer `@if`, `@for`, and `@switch` remove much of the need for common structural directives in app templates.

---

## 5. Pipes

Pipes transform values in templates.

Built-in examples:

```html
<p>{{ today | date: 'mediumDate' }}</p>
<p>{{ price | currency: 'USD' }}</p>
<p>{{ user.name | uppercase }}</p>
```

Custom pipe:

```ts
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'initials',
  standalone: true,
})
export class InitialsPipe implements PipeTransform {
  transform(name: string): string {
    return name
      .split(/\s+/)
      .filter(Boolean)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  }
}
```

Pipe interview notes:

- Pure pipes run when their input reference changes.
- Impure pipes run more often and can hurt performance.
- Put expensive transformations in computed signals, selectors, or memoized functions when templates get complex.

---

## 6. Modules versus standalone components

Older Angular applications centered around NgModules:

```ts
@NgModule({
  declarations: [AppComponent],
  imports: [BrowserModule, RouterModule.forRoot(routes)],
  bootstrap: [AppComponent],
})
export class AppModule {}
```

Modern Angular favors standalone APIs:

```ts
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

bootstrapApplication(AppComponent, {
  providers: [provideRouter(routes), provideHttpClient()],
});
```

Standalone component imports:

```ts
@Component({
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  template: `<a routerLink="/products">{{ total | currency }}</a>`,
})
export class SummaryComponent {
  total = 42;
}
```

How to answer in interviews:

- Standalone components reduce boilerplate and make dependencies local to a component.
- NgModules still exist in many production apps and third-party libraries.
- You can migrate incrementally.
- Lazy loading is simpler with standalone route-level `loadComponent` and `loadChildren`.

---

## 7. Services and dependency injection

Services hold reusable logic that is not directly about rendering a template:

- API clients
- Feature state
- Authentication helpers
- Logging
- Mapping and formatting
- Cross-component coordination

Root-provided service:

```ts
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private todos = ['Learn components', 'Practice DI'];

  list(): string[] {
    return [...this.todos];
  }

  add(todo: string): void {
    this.todos.push(todo);
  }
}
```

Injecting a service:

```ts
import { Component, inject } from '@angular/core';
import { TodoService } from './todo.service';

@Component({
  selector: 'app-todos',
  standalone: true,
  template: `
    @for (todo of todos; track todo) {
      <p>{{ todo }}</p>
    }
  `,
})
export class TodosComponent {
  private readonly todoService = inject(TodoService);
  todos = this.todoService.list();
}
```

Important DI concepts:

- Providers tell Angular how to create dependencies.
- Injectors form a hierarchy.
- `providedIn: 'root'` creates an application-wide singleton in most cases.
- Component-level providers create instances scoped to that component subtree.
- Injection tokens represent values or interfaces that do not exist at runtime.

Injection token example:

```ts
import { InjectionToken } from '@angular/core';

export interface AppConfig {
  apiBaseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
```

Interview talking point:

> Angular DI makes dependencies explicit and testable. Instead of manually constructing classes, components ask the injector for collaborators. Provider scope controls lifetime and sharing.

---

## 8. Routing basics

Angular Router maps URL paths to components.

Route config:

```ts
import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'home',
  },
  {
    path: 'home',
    loadComponent: () =>
      import('./home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'products/:id',
    loadComponent: () =>
      import('./product-detail/product-detail.component').then(
        (m) => m.ProductDetailComponent,
      ),
  },
];
```

Router shell:

```ts
import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterLink, RouterOutlet],
  template: `
    <nav>
      <a routerLink="/home">Home</a>
      <a routerLink="/products/42">Product 42</a>
    </nav>

    <router-outlet />
  `,
})
export class AppComponent {}
```

Reading route params:

```ts
import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  standalone: true,
  template: `<p>Product id: {{ productId }}</p>`,
})
export class ProductDetailComponent {
  private readonly route = inject(ActivatedRoute);
  productId = this.route.snapshot.paramMap.get('id');
}
```

Interview checklist:

- Explain route matching order.
- Know the difference between `routerLink` and `router.navigate`.
- Know where guards, resolvers, route params, query params, and lazy loading fit.
- Prefer lazy loading for feature routes that are not needed on initial render.

---

## 9. Template-driven forms

Template-driven forms are declared mostly in the template using `FormsModule` and `ngModel`. They are useful for simple forms and quick prototypes.

```ts
import { Component } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [FormsModule],
  template: `
    <form #form="ngForm" (ngSubmit)="submit(form)">
      <label>
        Email
        <input
          type="email"
          name="email"
          required
          email
          [(ngModel)]="model.email"
          #email="ngModel"
        />
      </label>

      @if (email.invalid && email.touched) {
        <p class="error">Enter a valid email.</p>
      }

      <label>
        Display name
        <input name="displayName" required [(ngModel)]="model.displayName" />
      </label>

      <button type="submit" [disabled]="form.invalid">Create account</button>
    </form>
  `,
})
export class SignupComponent {
  model = {
    email: '',
    displayName: '',
  };

  submit(form: NgForm): void {
    if (form.invalid) {
      return;
    }

    console.log('Submit', this.model);
  }
}
```

Template-driven form interview points:

- Good for simple forms.
- Validation attributes live in the template.
- The form model is inferred by Angular.
- Reactive forms are preferred for complex validation, dynamic controls, and testability.

---

## 10. HTTP client introduction

Angular's HTTP client returns RxJS observables.

App-level provider:

```ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient()],
});
```

Service using `HttpClient`:

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Product {
  id: number;
  name: string;
  price: number;
}

@Injectable({ providedIn: 'root' })
export class ProductApi {
  private readonly http = inject(HttpClient);

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>('/api/products');
  }
}
```

Component consumption:

```ts
import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ProductApi } from './product-api.service';

@Component({
  standalone: true,
  imports: [AsyncPipe],
  template: `
    @if (products$ | async; as products) {
      @for (product of products; track product.id) {
        <p>{{ product.name }}</p>
      }
    } @else {
      <p>Loading...</p>
    }
  `,
})
export class ProductListComponent {
  private readonly productApi = inject(ProductApi);
  products$ = this.productApi.getProducts();
}
```

Interview talking point:

> Angular HTTP returns cold observables. The request starts when something subscribes, such as `AsyncPipe`, an explicit `subscribe`, or conversion with `toSignal`. Prefer `AsyncPipe` or lifecycle-aware helpers to avoid leaking subscriptions.

---

## Common beginner mistakes

- Mutating state and expecting `OnPush` components to update without changing references or using signals.
- Forgetting to import directives/pipes into standalone components.
- Putting API calls directly in many components instead of centralizing them in services.
- Subscribing manually without cleanup.
- Using `any` instead of modeling API responses.
- Treating template-driven forms as the best choice for every form.

## Basic interview drill

Be ready to answer:

1. What is the difference between interpolation and property binding?
2. What is a standalone component?
3. How does dependency injection improve testability?
4. When would you use a service?
5. What does `router-outlet` do?
6. What are template-driven forms good for?
7. Why does `HttpClient.get<T>()` return an observable?

---


## 11. Modern standalone application anatomy

A modern Angular 17+ app is usually bootstrapped with `bootstrapApplication()` and configured with functional providers.

```ts
bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
  ],
});
```

Key files:

- `main.ts`: application bootstrap and root providers.
- `app.routes.ts`: route tree and lazy boundaries.
- `app.component.ts`: root shell with navigation and `RouterOutlet`.
- `core/`: app-wide infrastructure such as auth, interceptors, logging, configuration.
- `features/`: route-owned screens and state.
- `shared/`: reusable UI without feature ownership.

Interview checklist:

- Can you explain standalone bootstrap without `AppModule`?
- Can you name `provideRouter()` and `provideHttpClient()`?
- Can you explain root providers versus route-level providers?
- Can you describe why lazy routes reduce initial JavaScript?

## 12. Component API design basics

Good components expose a narrow contract: inputs for data, outputs for events, and services for shared dependencies.

```ts
@Component({
  selector: 'app-user-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<button (click)="selected.emit(user().id)">{{ label() }}</button>`,
})
export class UserBadgeComponent {
  user = input.required<{ id: string; firstName: string; lastName: string }>();
  selected = output<string>();
  label = computed(() => `${this.user().firstName} ${this.user().lastName}`);
}
```

Design checklist:

- Inputs should describe parent-owned data.
- Outputs should describe user or component events, not commands.
- Presentational components should not fetch their own feature data.
- OnPush works best when inputs are immutable and templates read signals or async pipe output.

## 13. Lifecycle hooks in practical order

Angular lifecycle hooks help when construction, input changes, view readiness, or cleanup matters.

| Hook | Typical use |
| --- | --- |
| `ngOnChanges` | React to input changes in decorator-era APIs |
| `ngOnInit` | Start initial non-template work |
| `ngAfterViewInit` | Integrate with view children or imperative DOM libraries |
| `ngOnDestroy` | Cleanup subscriptions, listeners, timers, observers |

Modern signals reduce lifecycle boilerplate because derived state can be declared:

```ts
name = input.required<string>();
greeting = computed(() => `Hello, ${this.name()}`);
```

Lifecycle checklist:

- Do not rely on input values in the constructor.
- Prefer `computed` for simple input-derived state.
- Use `DestroyRef` and `takeUntilDestroyed` for subscription cleanup.
- Use `ngAfterViewInit` only when the view must exist.

## 14. Template safety and built-in control flow

Angular templates are compiled and type-checked. Keep expressions simple and stable.

```html
@if (selectedUser(); as user) {
  <h2>{{ user.name }}</h2>
  <a [routerLink]="['/users', user.id]">Open profile</a>
} @else {
  <p>Select a user.</p>
}

@for (order of orders(); track order.id) {
  <app-order-row [order]="order" />
} @empty {
  <p>No orders yet.</p>
}
```

Interview checklist:

- Explain why `track item.id` reduces DOM churn.
- Know that `@if` can narrow nullable values.
- Know `@empty` for empty list states.
- Know `@defer` for heavy UI that can load later.
- Recognize legacy `*ngIf` and `*ngFor`.

## 15. Beginner-to-production decision table

| Question | Beginner answer | Production answer |
| --- | --- | --- |
| Where do API calls live? | Component | Feature API service and store |
| How do I show async data? | Manual subscribe | `AsyncPipe`, `toSignal`, or lifecycle-aware subscription |
| How do I share state? | Global variable | Service store or NgRx when justified |
| How do I hide admin UI? | `@if (isAdmin)` | Guard for UX and server authorization for data |
| How do I optimize lists? | Hope framework handles it | Stable `track`, split rows, virtualize large data |

## 16. Basic architecture vocabulary

- Smart component: owns feature orchestration.
- Presentational component: receives inputs and emits events.
- Provider scope: injector boundary controlling service lifetime.
- Cold observable: work starts on subscription.
- Signal: synchronous current value with dependency tracking.
- Computed signal: memoized derived state.
- Guard: router decision function, not a security boundary.
- Interceptor: HTTP middleware around requests and responses.

## 17. Basic red flags in interviews

- Saying signals replace RxJS.
- Saying guards secure backend data.
- Subscribing manually for every async value.
- Treating standalone as the same thing as provider scope.
- Mutating arrays in OnPush/signal state and expecting reliable updates.
