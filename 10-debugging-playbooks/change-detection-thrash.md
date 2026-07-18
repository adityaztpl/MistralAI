# Debugging Playbook: Angular Change Detection Thrash

## Symptoms

- Typing in an input feels laggy even with small data.
- CPU usage spikes while the UI appears idle.
- Components render many times per user action.
- Network calls repeat because template-bound methods or effects fire repeatedly.
- `ExpressionChangedAfterItHasBeenCheckedError` appears during updates.
- Large lists flicker or reset scroll position.

## Reproduce

1. Open Angular DevTools profiler and record the slow interaction.
2. Add temporary render counters to suspected components.
3. Test with production build and development build; dev mode doubles checks and can exaggerate symptoms.
4. Reduce the page to one expensive component and one interaction.
5. Disable polling/timers/websocket handlers to see if thrash stops.
6. Check whether the issue appears only with Zone.js, only zoneless, or only with signals.

Render counter:

```ts
export class RowComponent {
  checks = 0;

  ngDoCheck() {
    this.checks++;
  }
}
```

## Diagnose

### Template work

Look for:

- Function calls in templates: `{{ calculateTotal(items) }}`.
- Getters doing expensive work.
- Pipes that are impure or receive unstable object references.
- `*ngFor`/`@for` without `trackBy`/`track`.

### Subscription patterns

- Nested subscriptions updating state repeatedly.
- Missing `distinctUntilChanged` on high-frequency streams.
- `combineLatest` over streams that emit initial values repeatedly.
- Subscriptions created in methods or template-triggered paths.

### Signals/effects

- Effects that write to signals they also read.
- Computed signals creating new arrays/objects every read.
- Signal updates inside change detection hooks.
- Mixing RxJS and signals without clear boundaries.

### Zone triggers

- Timers, scroll, mousemove, resize, or websocket events inside Angular zone.
- Third-party widgets triggering frequent macro/microtasks.
- Global event listeners updating state too often.

## Fix

### Stabilize list rendering

```html
@for (item of items(); track item.id) {
  <app-row [item]="item" />
}
```

or older syntax:

```html
<div *ngFor="let item of items; trackBy: trackById"></div>
```

```ts
trackById(_: number, item: { id: string }) {
  return item.id;
}
```

### Remove expensive template calls

Move work into a computed signal, memoized selector, or precomputed view model.

```ts
readonly filteredItems = computed(() => {
  const query = this.query().toLowerCase();
  return this.items().filter(x => x.name.toLowerCase().includes(query));
});
```

### Throttle high-frequency streams

```ts
readonly searchResults$ = this.searchControl.valueChanges.pipe(
  debounceTime(250),
  distinctUntilChanged(),
  switchMap(query => this.api.search(query ?? '')),
  shareReplay({ bufferSize: 1, refCount: true })
);
```

### Use OnPush and immutable inputs

```ts
@Component({
  selector: 'app-row',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './row.component.html'
})
export class RowComponent {
  @Input({ required: true }) item!: ItemVm;
}
```

### Move noisy events outside Angular

```ts
this.ngZone.runOutsideAngular(() => {
  fromEvent(window, 'scroll')
    .pipe(auditTime(100), takeUntilDestroyed(this.destroyRef))
    .subscribe(() => {
      this.ngZone.run(() => this.scrollTick.set(Date.now()));
    });
});
```

## Prevention

- Use `OnPush` by default for feature components.
- Require `track`/`trackBy` on large lists.
- Avoid template methods that allocate or compute heavily.
- Use `async` pipe, `takeUntilDestroyed`, or signal interop for cleanup.
- Add performance budgets for large tables/lists.
- Profile before and after changes; do not optimize blindly.
- Review signal effects carefully: effects should orchestrate side effects, not derive state.

## Interview phrasing

> I would profile the interaction with Angular DevTools and identify which components are checked and why. Then I would inspect template calls, unstable inputs, list tracking, RxJS emissions, and signal effects. The likely fixes are to stabilize references, add `trackBy`, move expensive computation into computed view models, throttle streams, and use OnPush. I would prevent regressions with profiling around the hot interaction and review rules for template work and subscriptions.
