# 02 - SPA Dashboard

The dashboard turns the AI workflow into a usable product. It should make asynchronous work visible, make review easy, and capture feedback that improves future runs.

You can build this with React or Angular. The examples in this section use React because the requested code sketch is `Dashboard.tsx`, but the same design maps cleanly to Angular standalone components and services.

## Learning goals

- Design a create-run form with validation.
- Show queued/running/succeeded/failed states clearly.
- Implement run history and detail views.
- Support polling, SSE, or WebSocket updates.
- Render generated markdown safely.
- Capture human feedback.
- Explain UX trade-offs in interviews.

## Pages

| Page | Purpose |
| --- | --- |
| `/runs/new` | Submit brand/topic inputs |
| `/runs` | History table/cards |
| `/runs/:id` | Status, outputs, artifacts, eval scores, feedback |
| `/settings` | Tenant budget, default model, brand voice profile |

For a first milestone, combine create form and run history on one dashboard page.

## Create-run form

Fields:

- Brand description.
- Topic/campaign focus.
- Number of posts.
- Target audience.
- Tone/voice.
- Model selection from API-provided allowlist.
- Optional content constraints, such as "avoid medical claims."

Client validation improves UX, but the API must enforce the same constraints.

Example validation messages:

- "Brand description must be at least 20 characters."
- "Number of posts must be between 1 and 14."
- "Topic is required."
- "This model is not available for your tenant."

## Run history

Show:

- status badge;
- brand/topic;
- number of posts;
- created by;
- created at;
- duration;
- estimated cost;
- automated quality score;
- feedback status.

Sort newest first. Add filters for status and date.

## Run detail layout

Suggested sections:

1. **Summary header**
   - status, topic, model, score, cost, timestamps.
2. **Progress/events**
   - queued, started, artifacts written, eval completed.
3. **Final content pack**
   - ready-to-copy output.
4. **Captions**
   - per-post captions, CTAs, hashtags.
5. **Visual concepts**
   - image/reel prompts and composition notes.
6. **Research and strategy**
   - collapsible details.
7. **Evaluation**
   - rubric scores and warnings.
8. **Feedback**
   - thumbs, rating, reviewer notes, "use this as example" toggle.

## Status UX

| Status | UI behavior |
| --- | --- |
| `Queued` | Show position or "waiting for worker"; allow cancel |
| `Running` | Show spinner/progress events; allow cancel if supported |
| `Succeeded` | Show outputs, scores, feedback controls |
| `Failed` | Show safe error category and retry option |
| `Cancelled` | Show who/when cancelled and retry option |

Do not leave users staring at a disabled button with no run id. Once the API returns `202` or `201`, navigate to the run detail page.

## Polling vs SSE vs WebSocket

| Option | Use when | Trade-off |
| --- | --- | --- |
| Polling | First version; simple status updates | Extra requests; delayed updates |
| SSE | One-way server-to-client event stream | Great for run events; simpler than WebSocket |
| WebSocket | Bidirectional realtime collaboration | More infrastructure and lifecycle complexity |

For this capstone:

- Start with polling every 2-5 seconds for queued/running runs.
- Add SSE as an advanced milestone.
- Avoid WebSocket unless you add collaborative review.

## Safe rendering

Generated content is untrusted UI input. It may include text copied from user inputs or external sources.

Rules:

- Render plain captions as text.
- If rendering markdown, sanitize raw HTML.
- Validate link protocols.
- Add CSP on the SPA host.
- Avoid `dangerouslySetInnerHTML` unless sanitized and justified.

## Feedback UX

Capture:

- star rating or thumbs up/down;
- per-section score override;
- reviewer notes;
- "brand voice matched?" yes/no;
- "hashtags relevant?" yes/no;
- "safe to publish?" yes/no;
- edited final caption text.

Why this matters:

- human feedback creates a quality signal;
- future prompts can include approved examples;
- eval metrics can be calibrated against human judgment;
- interviews value the product loop, not just generation.

## API client shape

```ts
export type ContentRunStatus =
  | "Queued"
  | "Running"
  | "Succeeded"
  | "Failed"
  | "Cancelled";

export interface CreateContentRunRequest {
  brandDescription: string;
  topic: string;
  numberOfPosts: number;
  tone?: string;
  audience?: string;
  model?: string;
}

export interface ContentRunSummary {
  id: string;
  status: ContentRunStatus;
  brandDescription: string;
  topic: string;
  numberOfPosts: number;
  createdAt: string;
  completedAt?: string;
  qualityScore?: number;
  estimatedCostCents?: number;
}
```

## Example implementation

See:

- [`examples/Dashboard.tsx`](examples/Dashboard.tsx)

The sketch demonstrates:

- create form state;
- POST to API;
- run list loading;
- status badges;
- selected run details;
- feedback submission placeholder.

## Accessibility checklist

- [ ] Form labels are associated with inputs.
- [ ] Errors are announced and placed near fields.
- [ ] Status changes are visible as text, not color only.
- [ ] Buttons have loading text.
- [ ] Generated content sections use headings.
- [ ] Copy buttons announce success.
- [ ] Keyboard-only users can navigate run cards and details.
- [ ] Color contrast works for status badges.

## Testing checklist

- [ ] Empty dashboard shows useful first-run CTA.
- [ ] Form validation blocks invalid values.
- [ ] API validation errors are shown.
- [ ] Create success adds/navigates to run.
- [ ] Polling stops for terminal statuses.
- [ ] Failed run shows safe error.
- [ ] Generated content renders safely.
- [ ] Feedback submission updates UI.
- [ ] User cannot see another tenant's run by URL.

## Interview prompts

- How do you design UX for an AI job that takes minutes?
- What are the pros/cons of polling vs SSE?
- How do you render generated markdown safely?
- What feedback would you capture and why?
- How would you show cost to users without overwhelming them?
- How do you prevent duplicate submissions?

