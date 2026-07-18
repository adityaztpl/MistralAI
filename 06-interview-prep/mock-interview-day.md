# Mock Interview Day: Timed Schedule

Use this as a full rehearsal day for fullstack + GenAI interviews. It simulates a loop with behavioral, coding, frontend, backend, GenAI, and system design rounds.

---

## 1. Full-day schedule

| Time | Round | Focus |
|---|---|---|
| 08:30-08:45 | Setup | Environment, notes, water, timer |
| 08:45-09:15 | Warm-up | Pitch, project summary, one easy coding problem |
| 09:15-10:00 | Round 1 | Behavioral |
| 10:00-10:10 | Break | Reset |
| 10:10-11:00 | Round 2 | Backend/API coding |
| 11:00-11:10 | Break | Reset |
| 11:10-12:00 | Round 3 | Frontend coding |
| 12:00-13:00 | Lunch | No cramming |
| 13:00-13:50 | Round 4 | GenAI/RAG deep dive |
| 13:50-14:00 | Break | Reset |
| 14:00-14:50 | Round 5 | System design AI app |
| 14:50-15:05 | Break | Walk, hydrate |
| 15:05-15:45 | Round 6 | Hiring manager / values |
| 15:45-16:15 | Debrief | Score, notes, follow-ups |
| 16:15-17:00 | Targeted repair | Practice weakest area |

---

## 2. Setup checklist

Before starting:

- [ ] IDE open.
- [ ] Terminal ready.
- [ ] Browser closed except docs you intentionally use.
- [ ] Timer ready.
- [ ] Notebook or scratchpad ready.
- [ ] Portfolio project can run or screenshots are ready.
- [ ] Resume is available.
- [ ] Story bank is available.
- [ ] Water/snack ready.

### Environment sanity

If coding:

- [ ] Can run backend tests.
- [ ] Can run frontend tests.
- [ ] Can start app if needed.
- [ ] Dependencies installed.
- [ ] No unrelated local errors.

---

## 3. Warm-up: 30 minutes

### 5 minutes: breathing and framing

Goal: calm, direct, conversational.

Say out loud:

```text
I will clarify the problem, make progress incrementally, explain trade-offs, and test the important cases.
```

### 10 minutes: "Tell me about yourself"

Practice:

```text
I am a fullstack engineer focused on building reliable product features end to end. My strongest areas are ASP.NET Core APIs, React/Angular frontends, and GenAI features such as RAG, streaming chat, citations, and evaluation. I like working at the boundary between product experience and backend systems, especially where security, observability, and quality measurement matter.
```

### 15 minutes: easy coding

Pick one:

- Parse SSE frames.
- Validate citations.
- Chunk text by paragraphs.
- Build a notes list component.

Stop after 15 minutes even if not perfect.

---

## 4. Round 1: Behavioral, 45 minutes

### Interviewer prompts

1. Tell me about yourself.
2. Tell me about a complex technical project.
3. Tell me about a time you disagreed with someone.
4. Tell me about a failure.
5. Tell me about a time you worked under ambiguity.
6. Why are you interested in this role?
7. What questions do you have for us?

### Candidate goals

- Keep first answer under 2 minutes.
- Use concrete stories.
- Say "I" for personal actions and "we" for team outcomes.
- Include metrics or evidence.
- Include learning.

### Self-score

| Category | Score 1-5 |
|---|---:|
| Concise |
| Clear ownership |
| Good metric/result |
| Good trade-off |
| Good reflection |

### Repair after round

Write:

```text
Answer that rambled:
Missing metric:
Story to replace:
Follow-up I struggled with:
```

---

## 5. Round 2: Backend/API coding, 50 minutes

### Prompt

Implement a tenant-safe notes API plus a RAG query service skeleton.

Requirements:

- `GET /api/notes`
- `POST /api/notes`
- Tenant and owner filters.
- Cancellation tokens.
- Validation.
- RAG query method that retrieves authorized chunks and returns citations.

### Time plan

| Minute | Activity |
|---:|---|
| 0-5 | Clarify requirements and data model |
| 5-15 | DTOs and controller |
| 15-30 | Service/DB query logic |
| 30-40 | RAG method skeleton |
| 40-45 | Tests/edge cases |
| 45-50 | Explain production hardening |

### Things to say while coding

- "I am deriving tenant ID from trusted current-user context, not the request body."
- "I am passing cancellation tokens through async calls."
- "I am keeping the controller thin."
- "For RAG, authorization happens before prompt assembly."

### Follow-up questions

- How would you test tenant isolation?
- How would you add rate limiting?
- How would you handle provider timeout?
- How would you queue ingestion?
- How would you deploy this?

### Self-score

| Category | Score 1-5 |
|---|---:|
| Correctness |
| API design |
| Security/tenant isolation |
| Error handling |
| Communication |
| Tests |

---

## 6. Round 3: Frontend coding, 50 minutes

### Prompt

Build a chat UI that:

- Sends a message.
- Shows user and assistant messages.
- Streams assistant deltas.
- Supports stop generation.
- Shows citations.
- Handles error state.

### Time plan

| Minute | Activity |
|---:|---|
| 0-5 | Clarify event contract |
| 5-15 | Message types and state |
| 15-30 | Stream parser/client |
| 30-40 | UI interactions |
| 40-45 | Stop/error/citations |
| 45-50 | Tests/performance/security |

### Things to say while coding

- "The backend returns structured citations; I will not parse them from answer text."
- "I use AbortController so stop generation cancels the request."
- "I preserve partial output if an error arrives after streaming starts."
- "I would sanitize markdown before rendering model output."

### Follow-up questions

- How do you avoid re-rendering every message on each token?
- How do you handle auth expiry?
- How do you test split SSE frames?
- How do you handle 429 rate limits?

### Self-score

| Category | Score 1-5 |
|---|---:|
| State model |
| Streaming parser |
| Cancellation |
| UX states |
| Security/XSS |
| Communication |

---

## 7. Lunch reset, 60 minutes

Rules:

- Do not cram.
- Eat something stable.
- Walk for at least 5 minutes.
- Review only your top 5 reminders.

Top reminders:

1. Backend is trust boundary.
2. Tenant filters before prompt assembly.
3. Retrieval quality is measured separately from generation.
4. Streaming requires cancellation end to end.
5. Every design needs security, evaluation, cost, and observability.

---

## 8. Round 4: GenAI/RAG deep dive, 50 minutes

### Prompts

1. Explain RAG end to end.
2. How do you choose chunk size?
3. Dense search vs hybrid search?
4. What is reranking?
5. How do you evaluate RAG?
6. How do you prevent hallucinations?
7. Tools vs RAG vs fine-tuning?
8. How do you secure tool calling?

### Whiteboard flow

```text
Ingestion:
docs -> parse -> chunk -> embed -> vector store with metadata

Query:
question -> rewrite/embed -> retrieve with ACL filters -> rerank -> prompt -> model -> citations -> validation -> feedback
```

### Must-say points

- "Vector search is only one part of RAG."
- "Authorization filters happen before the model sees context."
- "Hybrid search helps exact IDs, codes, and names."
- "Evaluation separates retrieval failures from generation failures."
- "Prompt injection is mitigated with application controls, not prompt text alone."

### Self-score

| Category | Score 1-5 |
|---|---:|
| RAG clarity |
| Retrieval depth |
| Evaluation |
| Safety/security |
| Production realism |
| Concision |

---

## 9. Round 5: System design AI app, 50 minutes

### Prompt

Design a multi-tenant document Q&A platform for enterprise customers.

### Time plan

| Minute | Activity |
|---:|---|
| 0-5 | Requirements and constraints |
| 5-10 | APIs and core user flows |
| 10-20 | High-level architecture |
| 20-30 | RAG deep dive |
| 30-38 | Security, auth, tenancy |
| 38-44 | Scaling, cost, observability |
| 44-48 | Evaluation/failure modes |
| 48-50 | Summary |

### Opening questions

- Who are the users?
- What document types?
- What data sensitivity?
- Need citations?
- Need streaming?
- Latency target?
- Scale: tenants, docs, queries/day?
- Compliance requirements?

### Architecture checklist

- [ ] SPA/mobile client.
- [ ] ASP.NET Core API.
- [ ] Auth provider.
- [ ] SQL database.
- [ ] Blob storage.
- [ ] Ingestion queue.
- [ ] Worker.
- [ ] Vector store.
- [ ] LLM provider.
- [ ] Observability.
- [ ] Admin/eval dashboard.

### Must-say risks

- Cross-tenant leakage.
- Prompt injection.
- Hallucinated citations.
- Provider outage.
- Cost spike.
- Stale index.
- Slow ingestion.
- Exact-match retrieval failures.

---

## 10. Round 6: Hiring manager / values, 40 minutes

### Prompts

1. What kind of team environment helps you do your best work?
2. How do you prioritize when everything is urgent?
3. How do you communicate risk?
4. Tell me about a time you influenced without authority.
5. How do you handle feedback?
6. What are your growth areas?
7. What questions do you have for me?

### Strong questions to ask

- How do you evaluate the quality of AI features before rollout?
- What are the biggest reliability or data challenges for this product?
- How do product, design, and engineering collaborate on AI UX?
- What does success look like for this role in the first 6 months?
- How do you balance experimentation with production safety?

---

## 11. Debrief template

Immediately after the mock loop:

```text
Overall score:
Best round:
Weakest round:
Biggest technical gap:
Biggest communication gap:
Story that worked:
Story that failed:
Coding mistake pattern:
System design missing area:
One thing to practice tomorrow:
```

### Scorecard

| Round | Score 1-5 | Notes |
|---|---:|---|
| Behavioral |  |  |
| Backend coding |  |  |
| Frontend coding |  |  |
| GenAI deep dive |  |  |
| System design |  |  |
| Hiring manager |  |  |

---

## 12. Targeted repair block, 45 minutes

Pick only one weak area.

### If behavioral was weak

- Rewrite one story.
- Practice 30-second and 2-minute versions.
- Add metric and learning.

### If backend coding was weak

- Re-implement tenant-safe notes endpoint.
- Add one test.
- Explain cancellation and auth.

### If frontend coding was weak

- Re-implement SSE parser.
- Add stop-generation state.
- Explain XSS handling.

### If GenAI was weak

- Whiteboard RAG from memory.
- Explain eval metrics.
- Compare RAG/tools/fine-tuning.

### If system design was weak

- Redraw architecture.
- Add security/eval/observability.
- Practice 2-minute summary.

---

## 13. Two-hour condensed schedule

Use when you do not have a full day.

| Time | Round |
|---|---|
| 00:00-00:10 | Pitch + project summary |
| 00:10-00:35 | Backend or frontend coding |
| 00:35-01:00 | GenAI/RAG deep dive |
| 01:00-01:35 | System design |
| 01:35-01:50 | Behavioral story |
| 01:50-02:00 | Debrief |

---

## 14. Final day-before checklist

- [ ] Resume reviewed.
- [ ] Story bank reviewed.
- [ ] Portfolio project pitch ready.
- [ ] RAG architecture can be drawn from memory.
- [ ] Backend trust-boundary explanation ready.
- [ ] Streaming/cancellation explanation ready.
- [ ] 3 questions for interviewers ready.
- [ ] Sleep plan prioritized.

