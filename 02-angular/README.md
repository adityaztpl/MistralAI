# Angular Interview Prep Curriculum

Modern Angular is a component-first framework with a strong TypeScript story, batteries-included routing, forms, HTTP, dependency injection, and an increasingly fine-grained reactivity model through signals. This section is organized for interview preparation from fundamentals through advanced architecture.

## Learning path

1. [01-basics.md](./01-basics.md)
   - Components, templates, binding, directives, pipes
   - Standalone APIs versus NgModules
   - Services, dependency injection, routing, template-driven forms, HTTP
2. [02-intermediate.md](./02-intermediate.md)
   - Reactive forms, RxJS, interceptors, guards, lazy loading
   - Service-backed state, signals intro, NgRx lite, change detection
3. [03-advanced.md](./03-advanced.md)
   - Signals deep dive, zoneless change detection, custom structural directives
   - SSR/hydration, performance, testing, micro-frontends, XSS security

## Example files

- `examples/01-basics/hello.component.ts`
- `examples/01-basics/todo.service.ts`
- `examples/02-intermediate/auth.interceptor.ts`
- `examples/02-intermediate/auth.guard.ts`
- `examples/02-intermediate/product-form.component.ts`
- `examples/03-advanced/counter.signals.ts`
- `examples/03-advanced/resource-loader.ts`

## Interview strategy

When answering Angular questions:

- Start with the mental model: components render templates, services hold reusable logic, dependency injection wires collaborators, routing composes screens.
- Name the modern API first: standalone components, `bootstrapApplication`, functional guards/interceptors, signals, `input()`/`output()` where relevant.
- Explain trade-offs: template-driven versus reactive forms, RxJS streams versus signals, `OnPush` versus default change detection, NgRx versus service state.
- Include a concrete example: interviewers often want to see that you can translate concepts into production code.
- Mention testing and security when discussing user-facing features.

## Quick checklist

- Can you explain how Angular updates the DOM after an event, async task, signal update, or observable emission?
- Can you choose between standalone components and NgModules in a legacy app?
- Can you build a route with lazy loading, a guard, and an HTTP interceptor?
- Can you design a form with validation and meaningful error messages?
- Can you describe how to avoid memory leaks with RxJS and component lifecycles?
- Can you make performance-oriented choices without prematurely optimizing?
