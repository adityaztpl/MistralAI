# Angular and React Interview Questions

## Study goal

Be able to explain frontend architecture, state management, rendering performance, forms, HTTP integration, auth, and streaming GenAI UX.

---

## 1. Shared SPA fundamentals

### Q1: What happens when a SPA loads?

The browser downloads HTML, JavaScript, and CSS. The framework bootstraps the app, initializes routing/state, renders components, and calls APIs for data.

```text
HTML shell -> JS bundle -> framework bootstrap -> route -> components -> API calls
```

### Q2: CSR vs SSR vs SSG?

| Rendering | Description | Best for |
|---|---|---|
| CSR | Browser renders after JS loads | App dashboards |
| SSR | Server renders HTML per request | SEO, faster first content |
| SSG | Pre-render at build time | Docs, marketing pages |

For authenticated GenAI dashboards, CSR is common; SSR can still help shell performance.

---

## 2. React

### Q3: Explain React component rendering.

React components render when props/state/context change. React builds a virtual tree and reconciles changes to the DOM.

**Red flags:**

- Mutating state directly.
- Overusing global state.
- Putting side effects in render.

---

### Q4: `useEffect` common pitfalls?

`useEffect` runs after render for side effects. Pitfalls include missing dependencies, infinite loops, stale closures, and using effects for derived state.

```tsx
useEffect(() => {
  const controller = new AbortController();

  fetch(`/api/documents/${id}`, { signal: controller.signal })
    .then(r => r.json())
    .then(setDocument);

  return () => controller.abort();
}, [id]);
```

GenAI angle: abort streaming/chat requests when component unmounts or user stops generation.

---

### Q5: Controlled vs uncontrolled components?

Controlled inputs store value in React state. Uncontrolled inputs store value in the DOM and use refs.

Controlled is better for validation and dynamic forms; uncontrolled can be simpler for file inputs or large forms.

---

### Q6: How would you implement streaming chat in React?

Use `fetch`, read `response.body` with `ReadableStream`, parse SSE/chunks, append deltas to the current assistant message, and support `AbortController`.

```tsx
const controller = new AbortController();
setAbortController(controller);

await streamChat(
  input,
  token,
  delta => setMessages(current => appendDelta(current, delta)),
  controller.signal
);
```

State shape:

```ts
type Message = {
  id: string;
  role: "user" | "assistant";
  content: string;
  citations?: Citation[];
  isStreaming?: boolean;
};
```

---

### Q7: React performance tools?

- `memo` for pure components.
- `useMemo` for expensive derived values.
- `useCallback` for stable function props.
- Virtualization for large lists.
- Code splitting.
- Avoid unnecessary context updates.

For chat, virtualize long conversation lists and avoid re-rendering all markdown on every token.

---

## 3. Angular

### Q8: Explain Angular modules/components/services.

Angular apps are built from components for UI, services for shared logic/data access, dependency injection for wiring, and routing for navigation. Modern Angular also supports standalone components.

---

### Q9: What are Angular services and dependency injection?

Services encapsulate reusable logic, such as API clients or auth token providers.

```ts
@Injectable({ providedIn: 'root' })
export class ChatApi {
  constructor(private readonly http: HttpClient) {}
}
```

`providedIn: 'root'` registers a singleton service.

---

### Q10: Observables vs Promises?

Promises represent one future value. Observables can emit multiple values, be canceled through subscription, and compose streams with operators.

Angular `HttpClient` returns Observables. Streaming GenAI responses may use custom Observables around `fetch` or EventSource.

---

### Q11: Angular change detection?

Angular checks bindings when async events occur. `OnPush` change detection reduces checks by updating when inputs change, events fire, or observables emit.

```ts
@Component({
  selector: 'app-chat-message',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `{{ message.content }}`
})
export class ChatMessageComponent {
  @Input({ required: true }) message!: Message;
}
```

---

### Q12: Reactive forms vs template-driven forms?

Reactive forms are explicit, testable, and better for complex validation. Template-driven forms are simpler for small forms.

```ts
form = this.fb.group({
  message: ['', [Validators.required, Validators.maxLength(8000)]],
});
```

---

## 4. Auth and API integration

### Q13: How should a SPA call authenticated APIs?

- Use OAuth/OIDC Authorization Code + PKCE.
- Store tokens according to provider guidance.
- Attach access token to API requests.
- Handle refresh/expiry.
- Do not store provider API keys in the SPA.

```ts
headers: {
  Authorization: `Bearer ${accessToken}`
}
```

---

### Q14: How do you handle API errors in a SPA?

- Centralize API client errors.
- Show user-friendly messages.
- Preserve correlation IDs for support.
- Retry only safe/transient operations.
- Handle 401 by re-auth flow.
- Handle 429 with rate-limit UI.

GenAI-specific: a streaming response can fail after partial output, so represent partial answer plus error state.

---

## 5. GenAI UX questions

### Q15: What makes a good chat UI?

- Clear user/assistant turns.
- Streaming answer.
- Stop generation.
- Citations and source preview.
- Feedback controls.
- Copy/export.
- Error and retry states.
- Conversation history.

---

### Q16: How do you display citations?

Treat citations as structured data returned by the API.

```tsx
function CitationList({ citations }: { citations: Citation[] }) {
  return (
    <ul>
      {citations.map(citation => (
        <li key={citation.id}>
          <a href={citation.url}>{citation.title}</a>
          <blockquote>{citation.excerpt}</blockquote>
        </li>
      ))}
    </ul>
  );
}
```

Do not parse arbitrary citation text from the answer if the backend can return structured citations.

---

### Q17: How do you prevent markdown/XSS issues in model output?

- Sanitize rendered markdown/HTML.
- Prefer markdown renderer with safe defaults.
- Disable raw HTML unless sanitized.
- Treat model output as untrusted content.
- Validate links and file previews.

---

### Q18: How do you design optimistic UI for chat?

Immediately append the user's message, add an assistant placeholder with `isStreaming=true`, then replace/append deltas as they arrive. If the request fails, mark the assistant message as failed and offer retry.

---

## 6. System design prompts

Practice these out loud:

1. Design a React chat UI that streams tokens and shows citations.
2. Design an Angular document upload flow with ingestion status polling.
3. Add auth to a SPA + ASP.NET Core GenAI app.
4. Optimize a chat UI that becomes slow after 500 messages.
5. Build a feedback loop for thumbs-up/thumbs-down answer quality.

---

## 7. Advanced frontend scenarios

### Q19: How would you model chat message state for streaming?

Use explicit message status:

```ts
type MessageStatus = "pending" | "streaming" | "complete" | "failed" | "canceled";

type Message = {
  id: string;
  role: "user" | "assistant";
  content: string;
  status: MessageStatus;
  citations: Citation[];
  error?: string;
  requestId?: string;
};
```

State transitions:

```text
submit -> append user message -> append assistant streaming placeholder
delta -> append to assistant content
citation -> append citation
done -> mark complete
error -> mark failed with partial content
abort -> mark canceled
```

### Q20: How do you parse SSE-style events safely?

Key points:

- Read from `ReadableStream`.
- Keep a buffer because frames can split across chunks.
- Split on blank lines.
- Parse `event:` and `data:` lines.
- Treat unknown events as no-ops for forward compatibility.
- Catch JSON parse errors and mark stream failed.

### Q21: How do you prevent chat streaming from making the UI slow?

- Update only the active assistant message.
- Batch deltas with `requestAnimationFrame`.
- Render plain text while streaming, markdown after completion.
- Memoize message components.
- Virtualize long conversation lists.
- Avoid putting fast-changing token state in global context.

### Q22: How should the frontend handle rate limits?

For 429:

- Read `RateLimit-Reset` or `Retry-After`.
- Disable submit temporarily.
- Show user-friendly wait message.
- Do not retry non-idempotent operations automatically.
- Preserve draft message.

### Q23: How do you handle auth expiry during API calls?

- Centralize API client error handling.
- On 401, try token refresh if supported.
- If refresh fails, redirect to login.
- Preserve unsent drafts where possible.
- Avoid infinite refresh loops.

### Q24: How do you test a streaming chat component?

Tests:

- Parser handles split frames.
- Parser handles multiple frames in one chunk.
- Delta updates active assistant message.
- Citation event renders source.
- Error event preserves partial content.
- Stop button calls abort.
- Component aborts on unmount.

### Q25: React vs Angular for this project?

Balanced answer:

> Both can build the app well. React gives a flexible component and hook model with a large ecosystem. Angular gives an opinionated framework with dependency injection, routing, forms, and RxJS patterns built in. The better choice depends on team experience, existing codebase, and product constraints.

### Q26: What frontend security issues are specific to GenAI output?

- Model output is untrusted.
- Markdown rendering can introduce XSS if raw HTML is allowed.
- Links may be malicious or hallucinated.
- Citations should come from structured backend data.
- Do not expose provider keys or hidden system prompts.
- Do not make authorization decisions in the client.

Strong phrase:

> I treat model output like user-generated content: render with safe defaults, sanitize HTML, validate links/previews, and never trust it to drive privileged actions.

