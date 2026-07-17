# React Hooks Deep Dive

Hooks are React's function-component APIs for state, lifecycle synchronization, context, refs, memoization, external stores, and concurrent rendering signals. Interviewers use hook questions to test whether you understand React's render model, not just whether you can recite API names.

The core idea:

> A hook call is tied to a component render by call order. React stores hook state outside your function, then gives the right slot back on the next render as long as the same hooks are called in the same order.

---

## 1. Rules of hooks

Rules:

- Call hooks only at the top level of React function components or custom hooks.
- Do not call hooks inside loops, conditions, nested functions, or after early returns.
- Custom hooks must start with `use`.
- Hook dependencies should describe every reactive value read by the hook callback.

Why call order matters:

```tsx
function BadExample({ enabled }: { enabled: boolean }) {
  const [name, setName] = useState('');

  if (enabled) {
    // Bad: this hook exists only on some renders.
    useEffect(() => {
      document.title = name;
    }, [name]);
  }

  const [count, setCount] = useState(0);
  return <button onClick={() => setCount(count + 1)}>{count}</button>;
}
```

If `enabled` changes, the second `useState` may no longer line up with the same hook slot. The lint rule catches this because runtime behavior would be unpredictable.

Correct:

```tsx
function GoodExample({ enabled }: { enabled: boolean }) {
  const [name, setName] = useState('');
  const [count, setCount] = useState(0);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    document.title = name;
  }, [enabled, name]);

  return <button onClick={() => setCount((value) => value + 1)}>{count}</button>;
}
```

Interview checklist:

- Mention that hooks are not magic variables; React tracks them by call order.
- Mention lint support from `eslint-plugin-react-hooks`.
- Mention that custom hooks follow the same rules because they call hooks internally.

---

## 2. Render closures and stale values

Every render creates a new set of variables and functions. Event handlers and effects close over the values from the render where they were created.

```tsx
function DelayedCounter() {
  const [count, setCount] = useState(0);

  function handleClick() {
    setTimeout(() => {
      // This logs the count from the render that created handleClick.
      console.log(count);
    }, 1000);
  }

  return <button onClick={handleClick}>Log later</button>;
}
```

This is not inherently a bug; closures are how JavaScript works. It becomes a bug when code assumes a delayed callback sees the latest value.

Fix options:

1. Use functional state updates when the next value depends on the previous value.
2. Use a ref to read the latest value from long-lived callbacks.
3. Recreate subscriptions when dependencies change.

Functional update:

```tsx
setCount((current) => current + 1);
```

Latest ref:

```tsx
function useLatest<T>(value: T) {
  const ref = useRef(value);
  ref.current = value;
  return ref;
}
```

Interview talking point:

> Stale closures are usually solved by making dependencies accurate, using functional updates, or using a ref for a long-lived callback that needs the latest value. I avoid hiding missing dependencies unless I can explain the invariant.

---

## 3. `useState`

Use `useState` for local state that belongs to one component or a small subtree.

```tsx
const [isOpen, setIsOpen] = useState(false);
```

Lazy initial state:

```tsx
const [settings, setSettings] = useState(() => readSettingsFromStorage());
```

Use lazy initialization when computing the initial value is expensive or reads from an external API that should run only for the first render.

Object state:

```tsx
type FormState = {
  email: string;
  password: string;
};

const [form, setForm] = useState<FormState>({ email: '', password: '' });

function updateField(event: React.ChangeEvent<HTMLInputElement>) {
  const { name, value } = event.target;
  setForm((current) => ({ ...current, [name]: value }));
}
```

Common mistakes:

- Mutating arrays or objects in place.
- Storing values that are cheaply derived from existing state.
- Reading state immediately after `setState` and expecting the variable to change in the same render.
- Splitting state so aggressively that transitions become hard to reason about.
- Combining unrelated state so every update touches one large object.

State shape guidance:

- Keep state minimal.
- Keep related fields together when they change together.
- Split unrelated fields when they update independently.
- Prefer derived values during render when calculation is cheap.
- Use discriminated unions for async states.

Async state union:

```ts
type AsyncState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; error: Error };
```

---

## 4. `useReducer`

Use `useReducer` when transitions are event-like, numerous, or easier to test as a pure function.

```tsx
type State = {
  items: string[];
  selectedId: string | null;
};

type Action =
  | { type: 'itemAdded'; title: string }
  | { type: 'itemRemoved'; id: string }
  | { type: 'selectionCleared' };

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'itemAdded': {
      const id = crypto.randomUUID();
      return {
        ...state,
        items: [...state.items, `${id}:${action.title}`],
        selectedId: id,
      };
    }
    case 'itemRemoved':
      return {
        ...state,
        items: state.items.filter((item) => !item.startsWith(`${action.id}:`)),
        selectedId: state.selectedId === action.id ? null : state.selectedId,
      };
    case 'selectionCleared':
      return { ...state, selectedId: null };
    default:
      return state;
  }
}
```

Reducer advantages:

- Centralizes transitions.
- Makes complex updates easier to test.
- Reduces callback prop explosion by passing `dispatch`.
- Pairs well with Context for medium-sized client state.

Reducer cautions:

- Reducers should be pure.
- Avoid async work inside reducers.
- Do not use a reducer just to look "advanced"; simple `useState` is often clearer.

Interview answer:

> I choose `useReducer` when state transitions have names, multiple fields change together, or I want to test the transition logic independently. I keep side effects in event handlers, effects, or middleware-like layers, not inside the reducer.

---

## 5. `useEffect`

`useEffect` runs after React commits a render. It synchronizes the component with external systems.

Good effect use cases:

- Subscriptions.
- Timers.
- Browser APIs.
- Imperative widgets.
- Logging/analytics that depends on committed UI.
- Manual data fetching when framework/cache APIs are not being used.

Bad effect use cases:

- Deriving state from props/state.
- Responding to a user event that can be handled directly in the event handler.
- Resetting state that could be reset by keying a subtree.
- Computing expensive values that belong in `useMemo`.

Subscription pattern:

```tsx
useEffect(() => {
  const unsubscribe = chatClient.subscribe(roomId, (message) => {
    setMessages((current) => [...current, message]);
  });

  return unsubscribe;
}, [roomId]);
```

Fetch pattern with cancellation:

```tsx
useEffect(() => {
  const controller = new AbortController();

  async function load() {
    try {
      setState({ status: 'loading' });
      const response = await fetch(`/api/users/${userId}`, {
        signal: controller.signal,
      });

      if (!response.ok) {
        throw new Error(`Request failed with ${response.status}`);
      }

      const user = (await response.json()) as User;
      setState({ status: 'success', data: user });
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }

      setState({
        status: 'error',
        error: error instanceof Error ? error : new Error(String(error)),
      });
    }
  }

  void load();

  return () => controller.abort();
}, [userId]);
```

Strict Mode note:

React 18 Strict Mode intentionally mounts, unmounts, and remounts components in development to reveal missing cleanup. Code should tolerate setup-cleanup-setup.

Dependency array reasoning:

- Include props, state, context, and functions declared in the component that are read by the effect.
- Stable values such as state setters and refs do not need to be listed for correctness.
- If adding a dependency causes a loop, the effect is often doing derivation or creating unstable values.
- Do not silence the lint rule until you can explain the invariant.

Common effect refactors:

```tsx
// Avoid: derived state in an effect.
useEffect(() => {
  setFullName(`${firstName} ${lastName}`);
}, [firstName, lastName]);

// Prefer:
const fullName = `${firstName} ${lastName}`;
```

```tsx
// Avoid: event-specific work in an effect.
useEffect(() => {
  if (submitted) {
    sendAnalytics('signup');
  }
}, [submitted]);

// Prefer:
function handleSubmit() {
  sendAnalytics('signup');
  submitForm();
}
```

---

## 6. `useLayoutEffect` and `useInsertionEffect`

`useLayoutEffect` runs after DOM mutations but before the browser paints. Use it when you must measure layout and synchronously update before paint.

```tsx
function Tooltip({ targetId }: { targetId: string }) {
  const ref = useRef<HTMLDivElement | null>(null);
  const [top, setTop] = useState(0);

  useLayoutEffect(() => {
    const target = document.getElementById(targetId);
    const tooltip = ref.current;

    if (!target || !tooltip) {
      return;
    }

    const rect = target.getBoundingClientRect();
    setTop(rect.bottom + window.scrollY);
  }, [targetId]);

  return <div ref={ref} style={{ top, position: 'absolute' }}>Tooltip</div>;
}
```

Use it sparingly because synchronous layout work can block paint.

`useInsertionEffect` is for CSS-in-JS libraries that need to insert styles before layout effects. Application code rarely needs it.

Interview answer:

> I default to `useEffect`. I reach for `useLayoutEffect` only for pre-paint measurement or DOM adjustments where a visual flicker would be incorrect. I almost never use `useInsertionEffect` in app code.

---

## 7. `useRef`

Refs hold mutable values that do not trigger re-renders.

DOM ref:

```tsx
const inputRef = useRef<HTMLInputElement | null>(null);

function focus() {
  inputRef.current?.focus();
}
```

Instance-like mutable value:

```tsx
const retryCountRef = useRef(0);
retryCountRef.current += 1;
```

Previous value:

```tsx
function usePrevious<T>(value: T) {
  const ref = useRef<T | undefined>(undefined);

  useEffect(() => {
    ref.current = value;
  }, [value]);

  return ref.current;
}
```

Ref cautions:

- Do not use refs to store render data that should update the UI.
- Ref mutation during render is usually a smell except for stable lazy initialization patterns.
- Refs bypass React's reactive model, so use them intentionally.

---

## 8. `useMemo`, `useCallback`, and `memo`

`useMemo` caches a calculation result between renders.

```tsx
const visibleItems = useMemo(
  () => items.filter((item) => item.name.includes(query)),
  [items, query],
);
```

`useCallback` caches a function reference.

```tsx
const handleSelect = useCallback((id: string) => {
  setSelectedId(id);
}, []);
```

`React.memo` skips rendering a component when props compare equal.

```tsx
const ProductRow = memo(function ProductRow({ product }: { product: Product }) {
  return <li>{product.name}</li>;
});
```

When memoization helps:

- Expensive calculations.
- Expensive child renders.
- Stable references required by memoized children.
- Stable inputs required by effects in child hooks.
- Large lists after profiling.

When it hurts:

- It hides simple data flow.
- Dependencies are incorrect.
- The comparison costs more than rendering.
- Values change every render anyway.

Interview answer:

> `useCallback` does not make the function faster; it makes the function identity stable. I use memoization when identity stability or measured render cost matters.

---

## 9. `useContext`

`useContext` reads the nearest provider value above the component.

```tsx
const ThemeContext = createContext<ThemeContextValue | null>(null);

function useTheme() {
  const value = useContext(ThemeContext);

  if (!value) {
    throw new Error('useTheme must be used within ThemeProvider');
  }

  return value;
}
```

Context update behavior:

- Consumers re-render when the provider value changes.
- Memoizing the provider value avoids unnecessary updates from new object identity.
- Splitting contexts can reduce broad re-renders.
- Context is not a server cache and is not automatically selective.

Provider value pattern:

```tsx
const value = useMemo(
  () => ({ user, signOut }),
  [user, signOut],
);
```

Interview answer:

> Context solves prop drilling for values that are naturally scoped to a subtree. For frequently changing large state, I consider splitting context or using an external store with selective subscriptions.

---

## 10. `useId`

`useId` creates stable IDs that work with server rendering and hydration.

```tsx
function EmailField() {
  const id = useId();

  return (
    <>
      <label htmlFor={id}>Email</label>
      <input id={id} type="email" />
    </>
  );
}
```

Use it for accessibility relationships, not for list keys. List keys should come from data.

---

## 11. `useTransition`

`useTransition` marks updates as non-urgent and exposes pending state.

```tsx
function ProductSearch({ products }: { products: Product[] }) {
  const [query, setQuery] = useState('');
  const [filtered, setFiltered] = useState(products);
  const [isPending, startTransition] = useTransition();

  function handleChange(event: React.ChangeEvent<HTMLInputElement>) {
    const nextQuery = event.target.value;
    setQuery(nextQuery);

    startTransition(() => {
      setFiltered(
        products.filter((product) =>
          product.name.toLowerCase().includes(nextQuery.toLowerCase()),
        ),
      );
    });
  }

  return (
    <>
      <input value={query} onChange={handleChange} />
      {isPending && <p>Updating results...</p>}
      <ProductList products={filtered} />
    </>
  );
}
```

Use transitions for:

- Expensive non-urgent updates.
- Navigation-like updates that may suspend.
- Keeping input responsive while rendering a large result area.

Avoid:

- Controlled input value updates inside the transition.
- Wrapping every state update.
- Assuming transitions make CPU-heavy calculations cheap.

---

## 12. `useDeferredValue`

`useDeferredValue` lets a value lag behind while React works on lower-priority rendering.

```tsx
const deferredQuery = useDeferredValue(query);
const isStale = query !== deferredQuery;
```

It is useful when a child tree is expensive and can show slightly stale data while the user is typing.

Difference from debounce:

- Debounce waits for quiet time before updating.
- `useDeferredValue` lets React schedule lower-priority rendering.
- Debounce is time-based; deferred rendering is scheduler-based.
- You can combine them, but they solve different problems.

---

## 13. `useSyncExternalStore`

`useSyncExternalStore` is for reading external stores safely with concurrent rendering.

```tsx
type Listener = () => void;

const listeners = new Set<Listener>();
let online = navigator.onLine;

function subscribe(listener: Listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

window.addEventListener('online', () => {
  online = true;
  listeners.forEach((listener) => listener());
});

window.addEventListener('offline', () => {
  online = false;
  listeners.forEach((listener) => listener());
});

function getSnapshot() {
  return online;
}

export function useOnlineStatus() {
  return useSyncExternalStore(subscribe, getSnapshot, () => true);
}
```

Use cases:

- External stores.
- Browser APIs with subscriptions.
- Integrating state libraries with React.

Interview answer:

> `useSyncExternalStore` gives React a subscribe function and a snapshot function so external mutable data can be read consistently during concurrent rendering.

---

## 14. `useImperativeHandle`

`useImperativeHandle` customizes the instance value exposed by a ref.

```tsx
type DialogHandle = {
  open: () => void;
  close: () => void;
};

const Dialog = forwardRef<DialogHandle>(function Dialog(_, ref) {
  const [isOpen, setIsOpen] = useState(false);

  useImperativeHandle(
    ref,
    () => ({
      open: () => setIsOpen(true),
      close: () => setIsOpen(false),
    }),
    [],
  );

  return isOpen ? <div role="dialog">Dialog</div> : null;
});
```

Use sparingly for imperative integration points such as focus, animation, media controls, or third-party widgets.

---

## 15. React 19-era hooks: `useActionState`, `useFormStatus`, `useOptimistic`, and `use`

React 19 introduces or stabilizes APIs commonly discussed around actions, forms, optimistic UI, and reading async resources in supported environments.

`useActionState`:

```tsx
import { useActionState } from 'react';

type State = {
  error: string | null;
};

async function submitProfile(_: State, formData: FormData): Promise<State> {
  const displayName = String(formData.get('displayName') ?? '').trim();

  if (!displayName) {
    return { error: 'Display name is required.' };
  }

  await saveProfile(displayName);
  return { error: null };
}

function ProfileForm() {
  const [state, action, isPending] = useActionState(submitProfile, {
    error: null,
  });

  return (
    <form action={action}>
      <input name="displayName" />
      <button disabled={isPending}>Save</button>
      {state.error && <p role="alert">{state.error}</p>}
    </form>
  );
}
```

`useOptimistic`:

```tsx
const [optimisticTodos, addOptimisticTodo] = useOptimistic(
  todos,
  (current, title: string) => [...current, { id: 'optimistic', title }],
);
```

`useFormStatus` is read inside a form subtree to access pending submission state.

`use` can read promises or context in supported patterns, especially with Server Components and Suspense-enabled data flows.

Interview framing:

> These APIs are part of React's action and async rendering direction. In production, their exact shape depends heavily on the framework and environment, so I would explain both the core React concept and the framework integration.

---

## 16. Custom hook design

A good custom hook has a focused responsibility, clear inputs, clear outputs, and predictable ownership of state/effects.

Good API:

```tsx
function useDebounce<T>(value: T, delayMs: number): T
```

Questions to ask:

- Does each caller need independent state?
- Should the hook expose state, commands, or both?
- Does it need cleanup?
- Does it need to handle SSR?
- Should it accept an `enabled` option?
- Are returned functions stable?
- How will it be tested?

Custom hook with command API:

```tsx
function useDisclosure(initialOpen = false) {
  const [isOpen, setIsOpen] = useState(initialOpen);

  const open = useCallback(() => setIsOpen(true), []);
  const close = useCallback(() => setIsOpen(false), []);
  const toggle = useCallback(() => setIsOpen((value) => !value), []);

  return { isOpen, open, close, toggle };
}
```

Anti-patterns:

- Returning a huge object with unrelated responsibilities.
- Hiding global shared mutable state behind a hook name without documenting it.
- Swallowing errors that callers need.
- Omitting dependencies to reduce re-renders.
- Mixing server-state caching, UI state, and routing in one hook.

---

## Hook dependency decision table

| Situation | Recommended approach |
| --- | --- |
| Effect reads a prop/state value | Include it in dependencies |
| Effect sets state based on previous state | Use functional update |
| Long-lived callback needs latest value | Use a ref or recreate subscription |
| Object/function dependency changes too often | Move creation inside effect or memoize at source |
| Value is derived from props/state | Compute during render or `useMemo` if expensive |
| Event-specific side effect | Put it in the event handler |
| External subscription | Effect with cleanup and accurate dependencies |

## Hooks interview drill

1. Why can hooks not be called conditionally?
2. What is a stale closure? Show a fix.
3. When is `useEffect` the wrong tool?
4. What does Strict Mode reveal about effects?
5. When would you use `useReducer` instead of `useState`?
6. When does `useMemo` help?
7. Is `useCallback` a performance optimization by itself?
8. What is the difference between debounce and `useDeferredValue`?
9. What problem does `useSyncExternalStore` solve?
10. How would you design and test a custom hook?
