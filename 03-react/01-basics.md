# React Basics: JSX, Components, Props, State, Events, Lists, Forms, Effects, Vite

React is a JavaScript/TypeScript library for building user interfaces from components. A component describes what the UI should look like for a given set of props and state. React reconciles those descriptions with the DOM.

Interview talking point:

> React rendering is declarative. Components should behave like pure functions of props and state during render. Side effects belong in event handlers, effects, or framework-specific data APIs.

---

## 1. JSX

JSX is syntax that lets you write markup-like expressions in JavaScript/TypeScript.

```tsx
const title = 'React learner';

export function Welcome() {
  return <h1>Hello, {title}</h1>;
}
```

JSX rules:

- Return one parent element, or use a fragment.
- Use `className`, not `class`.
- Use `htmlFor`, not `for`.
- Use camelCase DOM props like `onClick` and `aria-label` exceptions.
- Put JavaScript expressions inside `{}`.
- Components start with capital letters.

Fragment example:

```tsx
export function Header() {
  return (
    <>
      <h1>Dashboard</h1>
      <p>Welcome back.</p>
    </>
  );
}
```

Conditional expression:

```tsx
function Status({ isOnline }: { isOnline: boolean }) {
  return <span>{isOnline ? 'Online' : 'Offline'}</span>;
}
```

Interview checklist:

- JSX is transformed into function calls.
- JSX expressions must be safe to evaluate during render.
- Rendering does not mean DOM replacement every time; React reconciles.

---

## 2. Components

Components are reusable UI units.

```tsx
type ButtonProps = {
  label: string;
  onClick: () => void;
};

export function Button({ label, onClick }: ButtonProps) {
  return (
    <button type="button" onClick={onClick}>
      {label}
    </button>
  );
}
```

Component design guidance:

- Keep render logic predictable.
- Accept data through props.
- Emit events through callback props.
- Use composition for layout and slots.
- Move reusable non-visual logic into hooks.

Composition example:

```tsx
type CardProps = {
  title: string;
  children: React.ReactNode;
};

function Card({ title, children }: CardProps) {
  return (
    <section>
      <h2>{title}</h2>
      {children}
    </section>
  );
}
```

Interview talking point:

> React does not use inheritance for component reuse. Composition is the default reuse mechanism: pass components, children, and callbacks to assemble behavior.

---

## 3. Props

Props are read-only inputs from a parent component.

```tsx
type UserBadgeProps = {
  name: string;
  role?: 'admin' | 'member';
};

function UserBadge({ name, role = 'member' }: UserBadgeProps) {
  return (
    <p>
      {name} - {role}
    </p>
  );
}
```

Important points:

- Never mutate props.
- Prefer explicit prop types.
- Keep props focused; avoid "god components".
- Derive display values during render when cheap.

Bad:

```tsx
function MutatingUser({ user }: { user: { name: string } }) {
  user.name = user.name.trim();
  return <p>{user.name}</p>;
}
```

Good:

```tsx
function UserName({ user }: { user: { name: string } }) {
  return <p>{user.name.trim()}</p>;
}
```

---

## 4. State

State is data that changes over time and affects rendering.

```tsx
import { useState } from 'react';

export function Counter() {
  const [count, setCount] = useState(0);

  return (
    <button type="button" onClick={() => setCount((value) => value + 1)}>
      Count: {count}
    </button>
  );
}
```

Functional updates:

```tsx
setCount((previous) => previous + 1);
```

Use functional updates when the next state depends on previous state.

State interview points:

- Setting state schedules a render; it does not synchronously mutate the variable in the current render.
- React may batch state updates.
- State should be minimal and derived values should often be calculated during render.
- Do not store something in state if it can be derived from props/state cheaply.

Derived value:

```tsx
const completedCount = todos.filter((todo) => todo.completed).length;
```

---

## 5. Events

React uses synthetic events with camelCase event props.

```tsx
function SearchBox() {
  const [query, setQuery] = useState('');

  return (
    <input
      value={query}
      onChange={(event) => setQuery(event.target.value)}
      placeholder="Search"
    />
  );
}
```

Common event patterns:

- `onClick` for buttons.
- `onChange` for form inputs.
- `onSubmit` for forms.
- Prevent default form submit navigation with `event.preventDefault()`.

```tsx
function LoginForm() {
  const [email, setEmail] = useState('');

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    console.log(email);
  }

  return (
    <form onSubmit={handleSubmit}>
      <input value={email} onChange={(event) => setEmail(event.target.value)} />
      <button type="submit">Sign in</button>
    </form>
  );
}
```

Interview talking point:

> Event handlers are a good place for user-triggered side effects. Effects are for synchronizing with external systems after render, not for every piece of app logic.

---

## 6. Lists and keys

Render arrays with `map`.

```tsx
type Todo = {
  id: string;
  title: string;
};

function TodoList({ todos }: { todos: Todo[] }) {
  return (
    <ul>
      {todos.map((todo) => (
        <li key={todo.id}>{todo.title}</li>
      ))}
    </ul>
  );
}
```

Keys help React match children between renders.

Good keys:

- Stable.
- Unique among siblings.
- Based on domain IDs.

Avoid array indexes when list order can change.

```tsx
// Risky if todos can be inserted, removed, or reordered.
todos.map((todo, index) => <li key={index}>{todo.title}</li>);
```

Interview talking point:

> Keys are not passed as normal props. They are used by React's reconciliation algorithm to preserve or reset component identity.

---

## 7. Forms

Controlled inputs store the input value in React state.

```tsx
function SignupForm() {
  const [form, setForm] = useState({
    email: '',
    password: '',
  });

  function updateField(event: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
  }

  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    console.log(form);
  }

  return (
    <form onSubmit={handleSubmit}>
      <input name="email" value={form.email} onChange={updateField} />
      <input
        name="password"
        type="password"
        value={form.password}
        onChange={updateField}
      />
      <button type="submit">Create account</button>
    </form>
  );
}
```

Controlled form benefits:

- UI reflects state exactly.
- Easy validation and conditional UI.
- Predictable submit payload.

Uncontrolled inputs use refs or browser form APIs. They can be simpler for very large forms or when paired with libraries.

---

## 8. Conditional rendering

Common patterns:

```tsx
if (isLoading) {
  return <p>Loading...</p>;
}

return <Dashboard />;
```

```tsx
{error ? <p role="alert">{error.message}</p> : null}
```

```tsx
{isAdmin && <AdminTools />}
```

Be careful with `&&` and numbers:

```tsx
// Renders 0 when count is 0.
{count && <p>Has items</p>}

// Better:
{count > 0 && <p>Has items</p>}
```

---

## 9. `useEffect` basics

`useEffect` runs after React commits a render. Use it to synchronize with external systems such as subscriptions, timers, imperative widgets, or browser APIs.

```tsx
import { useEffect, useState } from 'react';

function Clock() {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), 1000);

    return () => window.clearInterval(id);
  }, []);

  return <time>{now.toLocaleTimeString()}</time>;
}
```

Dependency array rules:

- No dependency array: runs after every commit.
- Empty array: runs after mount, cleanup on unmount.
- Dependencies: reruns when any dependency changes.

Fetching example:

```tsx
function UserProfile({ userId }: { userId: string }) {
  const [name, setName] = useState('');

  useEffect(() => {
    const controller = new AbortController();

    fetch(`/api/users/${userId}`, { signal: controller.signal })
      .then((response) => response.json())
      .then((user: { name: string }) => setName(user.name))
      .catch((error: unknown) => {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          console.error(error);
        }
      });

    return () => controller.abort();
  }, [userId]);

  return <p>{name}</p>;
}
```

Interview talking point:

> Effects are for synchronization, not deriving render data. If you can compute a value from props and state during render, you usually do not need an effect.

---

## 10. Vite setup

Vite is a common way to start a React app quickly.

```bash
npm create vite@latest my-react-app -- --template react-ts
cd my-react-app
npm install
npm run dev
```

Typical files:

```tsx
// src/main.tsx
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
```

Strict Mode helps reveal unsafe patterns in development. In React 18, it intentionally double-invokes some lifecycle-like behavior in development to surface effect cleanup issues.

---

## Common beginner mistakes

- Mutating state arrays or objects in place.
- Using array indexes as keys for dynamic lists.
- Putting derived data in state and keeping it synchronized with effects.
- Missing effect dependencies.
- Fetching in effects without cancellation or stale-response handling.
- Creating components inside components accidentally resetting identity.
- Using `useMemo` everywhere instead of measuring.

## Basic interview drill

1. What is JSX?
2. What is the difference between props and state?
3. Why should React render logic be pure?
4. How do keys affect reconciliation?
5. What is a controlled input?
6. When should you use `useEffect`?
7. Why might Strict Mode run an effect twice in development?

---

## Deep basics expansion: render, commit, and identity

React interviews often start with "basic" questions that are really testing the mental model beneath the API.

### Render vs commit

Render is React calling your components to compute what the UI should be. Commit is React applying the necessary changes to the host environment, such as the browser DOM.

```tsx
function Greeting({ name }: { name: string }) {
  console.log('rendering');
  return <h1>Hello, {name}</h1>;
}
```

The function can run many times. That does not mean React replaced the DOM many times. React compares the output and commits what changed.

Interview answer:

> A render is a calculation. A commit is when React applies the result. This distinction matters because render must be pure, while effects and DOM updates happen after commit.

### Component identity

React preserves state by component type and position in the tree. Keys refine identity among siblings.

```tsx
function ProfileSwitcher({ userId }: { userId: string }) {
  return <ProfileForm key={userId} userId={userId} />;
}
```

Adding `key={userId}` tells React to reset `ProfileForm` state when the user changes. Without the key, React may preserve state because the component type and position are the same.

Use keys intentionally:

- Preserve identity for stable list items.
- Reset identity for forms/wizards tied to a different entity.
- Avoid random keys because they force remounts every render.

### Components inside components

Avoid defining components inside other components unless you intentionally want a new component type each render.

```tsx
function Page() {
  function InlinePanel() {
    return <section>Panel</section>;
  }

  return <InlinePanel />;
}
```

`InlinePanel` is recreated on every `Page` render. If it holds state, that state can reset unexpectedly. Move it outside or pass data through props.

---

## Deep basics expansion: state updates and batching

State setters schedule an update. They do not mutate the variable in the current render.

```tsx
function Counter() {
  const [count, setCount] = useState(0);

  function incrementTwiceWrong() {
    setCount(count + 1);
    setCount(count + 1);
  }

  function incrementTwiceRight() {
    setCount((current) => current + 1);
    setCount((current) => current + 1);
  }

  return (
    <>
      <button onClick={incrementTwiceWrong}>Wrong</button>
      <button onClick={incrementTwiceRight}>Right</button>
    </>
  );
}
```

Use functional updates whenever the next state depends on the previous state. This is especially important when multiple updates can be batched.

Automatic batching:

- React batches many updates during event handlers.
- React 18 expanded automatic batching to more async contexts.
- Batching reduces unnecessary renders.
- If you need to respond after the DOM updates, use effects or specific APIs rather than assuming immediate mutation.

Interview answer:

> I treat state as a snapshot for the current render. If I need to update based on the previous value, I pass an updater function.

---

## Deep basics expansion: forms and accessibility

Accessible markup helps users and makes tests stronger.

Good form basics:

```tsx
function EmailField() {
  const [email, setEmail] = useState('');
  const error = email && !email.includes('@') ? 'Enter a valid email.' : null;

  return (
    <div>
      <label htmlFor="email">Email</label>
      <input
        id="email"
        type="email"
        value={email}
        aria-invalid={error ? 'true' : 'false'}
        aria-describedby={error ? 'email-error' : undefined}
        onChange={(event) => setEmail(event.target.value)}
      />
      {error && (
        <p id="email-error" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}
```

Accessibility checklist:

- Use real `<button>` elements for actions.
- Use `<a>`/router links for navigation.
- Label form controls.
- Keep keyboard interaction in mind.
- Use semantic headings and landmarks.
- Do not remove focus outlines without replacing them.
- Use `aria-*` to enhance semantics, not to replace native HTML.

Interview answer:

> React does not change HTML accessibility fundamentals. I start with semantic elements, labels, keyboard behavior, and visible focus, then add ARIA only when native semantics are insufficient.

---

## Basic "why did this bug happen?" checklist

- Did state get mutated instead of replaced?
- Did a list use unstable keys?
- Did a component remount because its key or type changed?
- Did code rely on state changing synchronously after a setter?
- Did an effect derive state that could be computed during render?
- Did Strict Mode reveal missing cleanup?
- Did an input switch between controlled and uncontrolled?
- Did an event handler accidentally execute during render, such as `onClick={save()}`?
- Did a conditional render accidentally output `0` with `count && <List />`?
- Did a form submit reload the page because `preventDefault` was missing?

## Expanded basic interview checklist

- Explain JSX transformation at a high level.
- Explain component composition and `children`.
- Explain props immutability.
- Explain state snapshots and functional updates.
- Explain render vs commit.
- Explain keys and state preservation.
- Explain controlled inputs and validation.
- Explain why effects are not for every state change.
- Explain Strict Mode double-invocation in development.
- Explain how accessibility affects component design and tests.
