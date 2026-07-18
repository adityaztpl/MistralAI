# Signals vs RxJS in Angular

## The decision

Angular now has two major reactive tools:

- **Signals**: synchronous fine-grained state values with dependency tracking, computed values, and effects.
- **RxJS**: asynchronous streams over time with rich operators for events, HTTP, websockets, cancellation, composition, and backpressure-like control.

The strong answer is not "signals replace RxJS." They overlap in state management but shine in different places.

## Quick default

- Use **signals** for component-local or service-local state that the template reads synchronously.
- Use **RxJS** for async event streams, HTTP composition, websockets, debounced search, cancellation, retries, and complex time-based flows.
- Bridge between them at boundaries when needed.

## Comparison

| Force | Signals | RxJS |
|---|---|---|
| Mental model | Value that notifies dependents | Stream of values over time |
| Best for | UI state, derived state, template reactivity | Async workflows and event streams |
| Sync read | Easy: call signal | Need subscription/async pipe/value bridge |
| Time operators | Minimal | Extensive: debounce, switchMap, mergeMap, retry |
| Cancellation | Not primary | Strong via unsubscription/switchMap/takeUntilDestroyed |
| Template fit | Very strong | Strong with async pipe |
| Learning curve | Smaller | Larger but powerful |

## Use signals when

- State is local and synchronous.
- Template needs derived values.
- You want fine-grained updates without broad observable chains.
- You are modeling UI state: selected tab, form mode, filtered local list, loading flags, optimistic draft state.
- You want computed values instead of manual subscriptions.

Example mental model:

```text
source signal changes -> computed values invalidate -> template reads updated values
```

## Use RxJS when

- You are handling HTTP or websocket streams.
- You need debouncing, throttling, buffering, retries, `switchMap`, `concatMap`, or `mergeMap`.
- You need cancellation of in-flight work.
- You compose multiple async sources.
- You consume Angular APIs that expose observables.
- You need robust event pipelines.

Example mental model:

```text
user input stream -> debounce -> distinctUntilChanged -> switchMap HTTP -> render result
```

## Bridging guidance

- Observable -> signal: useful when a component wants to render async data as a signal.
- Signal -> observable: useful when signal changes should feed an RxJS pipeline.
- Keep bridging at clear boundaries, not randomly throughout the code.

## Trade-offs

Signals:

- Cleaner for synchronous UI state.
- Easier to read than many nested subscriptions.
- Not a replacement for RxJS operators or stream cancellation.

RxJS:

- Powerful for async complexity.
- Can become unreadable if overused for simple state.
- Requires lifecycle discipline; use `async` pipe or `takeUntilDestroyed`.

## Interview answer script

```text
I use signals for synchronous UI state and derived values because they are simple to read and integrate well with Angular templates. I use RxJS for asynchronous streams like HTTP, websockets, debounced search, retries, and cancellation because operators like switchMap and retry model time and concurrency well.

Signals reduce the need to use observables for every piece of component state, but they do not replace RxJS for async workflows. In production I would choose the simplest abstraction per boundary and bridge intentionally.
```

## Common mistakes

- Rewriting every observable as a signal without considering cancellation.
- Using RxJS subjects as mutable state containers when signals would be clearer.
- Creating effects for business logic that should be explicit service methods.
- Forgetting to clean up subscriptions.
- Mixing signals and observables in every layer with no convention.

## Good closing line

"Signals are a state primitive; RxJS is a stream composition library. I use each where its mental model matches the problem."
