# Debugging Playbook: React Infinite Rerender

## Symptoms

- Browser freezes or logs "Too many re-renders".
- Component repeatedly fetches data without user action.
- `useEffect` runs on every render even though dependencies look stable.
- State updates cause child components to remount repeatedly.
- React DevTools Profiler shows the same subtree rendering continuously.

## Reproduce

1. Open React DevTools Profiler and record the loop.
2. Add console counters carefully around renders/effects.
3. Disable Strict Mode temporarily only to distinguish dev double-invocation from a real loop; do not use that as the fix.
4. Comment out effects one by one to isolate the loop source.
5. Check network tab for repeated requests and request initiators.

Render/effect counters:

```tsx
function SearchPage() {
  console.count('SearchPage render');

  useEffect(() => {
    console.count('SearchPage effect');
  });

  return null;
}
```

## Diagnose

Common causes:

### State update during render

```tsx
function Bad({ value }: { value: string }) {
  const [state, setState] = useState('');
  setState(value); // bad: render causes state update
  return <div>{state}</div>;
}
```

### Effect depends on unstable object/function

```tsx
const filters = { tag, archived };
useEffect(() => {
  fetchNotes(filters);
}, [filters]); // new object every render
```

### Effect updates its own dependency

```tsx
useEffect(() => {
  setCount(count + 1);
}, [count]);
```

### Parent recreates props

Child effects rerun because parent passes new arrays, objects, or callbacks every render.

### Key changes cause remount

Using unstable keys like `Math.random()` or array index in changing lists can remount components and rerun effects.

## Fix

### Move derived state out of state

If value can be derived from props/state, compute it during render or memoize if expensive.

```tsx
const filtered = useMemo(
  () => notes.filter(note => note.title.includes(query)),
  [notes, query]
);
```

### Stabilize dependencies

```tsx
const filters = useMemo(() => ({ tag, archived }), [tag, archived]);

useEffect(() => {
  fetchNotes(filters);
}, [filters]);
```

or depend on primitives:

```tsx
useEffect(() => {
  fetchNotes({ tag, archived });
}, [tag, archived]);
```

### Guard effect updates

```tsx
useEffect(() => {
  setSelectedId(current => current ?? notes[0]?.id ?? null);
}, [notes]);
```

Functional updates can remove stale dependency needs.

### Use data-fetching libraries carefully

With React Query/TanStack Query:

```tsx
useQuery({
  queryKey: ['notes', { tag, archived }],
  queryFn: () => fetchNotes({ tag, archived })
});
```

Ensure query keys are serializable and stable in meaning.

## Prevention

- Keep render pure: no state updates, subscriptions, or side effects in render.
- Treat effect dependency warnings seriously.
- Prefer derived values over duplicated state.
- Memoize only when identity stability matters or computation is expensive.
- Use stable keys from data IDs.
- Add tests for fetch-once behavior when it is important.
- Use React DevTools Profiler to validate fixes.

## Interview phrasing

> I would isolate whether the loop comes from a render-time state update, an effect dependency, or remounting. I would use React DevTools and counters, then inspect effects that update their own dependencies or depend on newly-created objects/functions. The fix is to keep render pure, derive state where possible, stabilize dependencies, and add guards for effect updates. I would prevent regressions with lint rules for hooks and focused tests around repeated fetch behavior.
