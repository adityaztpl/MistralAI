# Angular Interview Prep Curriculum

Modern Angular is a TypeScript-first application framework with component rendering, declarative templates, dependency injection, routing, forms, HTTP, testing utilities, SSR/hydration support, and fine-grained reactivity through signals. This section is organized for interview preparation from fundamentals through advanced architecture using Angular 17+ standalone patterns.

## Learning path

1. [01-basics.md](./01-basics.md)
   - Components, templates, binding, directives, pipes
   - Standalone APIs versus NgModules
   - Services, dependency injection, routing, template-driven forms, HTTP
   - Lifecycle, component APIs, built-in control flow, and beginner production vocabulary
2. [02-intermediate.md](./02-intermediate.md)
   - Reactive forms, RxJS, interceptors, guards, lazy loading
   - Service-backed state, signals intro, NgRx lite, change detection
   - Feature architecture, route-level providers, HTTP error handling, and debugging playbooks
3. [03-advanced.md](./03-advanced.md)
   - Signals deep dive, zoneless change detection, custom structural directives
   - SSR/hydration, performance, testing, micro-frontends, XSS security
   - Architecture decision frameworks, advanced security, and system-design drills
4. [04-rxjs-deep-dive.md](./04-rxjs-deep-dive.md)
   - Observable mental models, operator categories, flattening choices, error placement
   - Subjects, stream combination, router/forms streams, RxJS/signals interop, testing
5. [05-signals-and-zoneless.md](./05-signals-and-zoneless.md)
   - Writable signals, computed values, effects, signal component APIs, signal stores
   - OnPush, zoneless mental model, migration, stale-template debugging
6. [06-forms-and-validation.md](./06-forms-and-validation.md)
   - Typed reactive forms, custom validators, async validation, dynamic forms
   - Conditional controls, accessibility, server validation, testing
7. [07-testing-and-performance.md](./07-testing-and-performance.md)
   - Standalone TestBed, component/service/HTTP/guard/interceptor tests
   - Rendering, bundle, network, memory, SSR/hydration performance
8. [08-cheatsheet.md](./08-cheatsheet.md)
   - Rapid final review for APIs, RxJS choices, signals, forms, testing, performance, security

## Example files

- `examples/01-basics/hello.component.ts`
- `examples/01-basics/todo.service.ts`
- `examples/02-intermediate/auth.interceptor.ts`
- `examples/02-intermediate/auth.guard.ts`
- `examples/02-intermediate/product-form.component.ts`
- `examples/03-advanced/counter.signals.ts`
- `examples/03-advanced/resource-loader.ts`
- `examples/04-rxjs/search.operators.ts`
- `examples/04-rxjs/exhaust-map-login.ts`
- `examples/05-signals/shop.store.ts`
- `examples/06-forms/dynamic-form.component.ts`
- `examples/07-testing/todo.component.spec.ts`
- `examples/07-testing/todo.service.spec.ts`

## Recommended study order

1. Read basics and build a standalone component with a service.
2. Build a lazy route, functional guard, interceptor, and typed reactive form.
3. Practice RxJS flattening decisions until operator choice follows workflow intent.
4. Rebuild a small feature with a signal store and OnPush components.
5. Review dynamic forms with accessibility and server-error mapping.
6. Write tests for a component, signal store, HTTP service, guard, and interceptor.
7. Work through advanced prompts: checkout, dashboard, public catalog, real-time notifications.
8. Use the cheatsheet for final rapid review.

## Interview strategy

When answering Angular questions:

- Start with the mental model: components render templates, services hold reusable logic, dependency injection wires collaborators, routing composes screens.
- Name the modern API first: standalone components, `bootstrapApplication`, functional guards/interceptors, built-in control flow, signals, `input()`/`output()` where relevant.
- Explain trade-offs: template-driven versus reactive forms, RxJS streams versus signals, `OnPush` versus default/zoneless change detection, NgRx versus service state.
- Include a concrete example: interviewers often want to see that you can translate concepts into production code.
- Mention testing, accessibility, performance, and security when discussing user-facing features.
- Be precise about client/server boundaries: guards and client validation improve UX; server authorization and server validation enforce rules.

## Quick checklist

- Can you explain how Angular updates the DOM after an event, async task, signal update, or observable emission?
- Can you choose between standalone components and NgModules in a legacy app?
- Can you build a route with lazy loading, a guard, a resolver, and route-level providers?
- Can you design a form with typed controls, validation, accessible errors, and server-error mapping?
- Can you describe how to avoid memory leaks with RxJS and component lifecycles?
- Can you choose the correct RxJS flattening operator for search, login, ordered saves, and parallel uploads?
- Can you design a signal store with private writable state and public computed values?
- Can you explain zoneless Angular and how to migrate safely?
- Can you test standalone components, services, HTTP clients, guards, and interceptors?
- Can you make performance-oriented choices after measuring the actual bottleneck?
