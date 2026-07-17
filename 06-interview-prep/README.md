# Interview Prep Index

This section prepares you for fullstack + GenAI interviews with question banks, answer structures, behavioral storytelling, coding drills, mock interview schedules, and system design playbooks.

---

## Files

### Core question banks

- [ASP.NET Core questions](questions-aspnet.md)
- [Angular/React questions](questions-angular-react.md)
- [GenAI questions](questions-genai.md)
- [System design for AI apps](system-design-ai-apps.md)

### Deep interview practice

- [Answer frameworks](answer-frameworks.md) - STAR+, technical answer structures, coding, RAG, streaming, security, and system design frameworks.
- [Behavioral and storytelling](behavioral-and-storytelling.md) - story bank templates, behavioral prompts, rubrics, and drills.
- [Coding drills](coding-drills.md) - API, frontend, streaming, and RAG coding drills with solution sketches.
- [Mock interview day](mock-interview-day.md) - full-day and condensed timed schedules with scorecards.

---

## Recommended prep order

1. Read [answer-frameworks.md](answer-frameworks.md) so every answer has structure.
2. Build your story bank with [behavioral-and-storytelling.md](behavioral-and-storytelling.md).
3. Review the question banks:
   - [ASP.NET Core](questions-aspnet.md)
   - [Angular/React](questions-angular-react.md)
   - [GenAI](questions-genai.md)
4. Practice hands-on problems in [coding-drills.md](coding-drills.md).
5. Whiteboard designs from [system-design-ai-apps.md](system-design-ai-apps.md).
6. Run the full schedule in [mock-interview-day.md](mock-interview-day.md).

---

## 4-week interview routine

### Week 1: Backend fundamentals

Goals:

- Practice ASP.NET Core request pipeline, DI, auth, EF Core, and API design.
- Build one small API endpoint with validation, logging, cancellation, and tenant filtering.
- Explain middleware order out loud.

Daily checklist:

- [ ] Answer 5 ASP.NET questions.
- [ ] Complete one API coding drill.
- [ ] Explain auth vs authorization.
- [ ] Explain one production trade-off.
- [ ] Record one 2-minute answer.

Milestone:

- [ ] You can implement a tenant-safe CRUD endpoint in 30-45 minutes.

### Week 2: Frontend fundamentals

Goals:

- Practice React hooks or Angular services/forms/change detection.
- Build a streaming chat UI with stop generation.
- Explain SPA auth flow and API error handling.

Daily checklist:

- [ ] Answer 5 frontend questions.
- [ ] Build one component or hook/service.
- [ ] Handle loading/error/empty states.
- [ ] Practice streaming/cancellation explanation.
- [ ] Mention XSS/model-output sanitization.

Milestone:

- [ ] You can implement a chat UI state model and stream parser under time pressure.

### Week 3: GenAI fundamentals

Goals:

- Explain tokens, embeddings, RAG, tools, agents, and fine-tuning.
- Build a basic RAG demo and evaluate retrieval failures.
- Practice hallucination mitigation answers.

Daily checklist:

- [ ] Draw RAG ingestion/query flow from memory.
- [ ] Explain chunking, hybrid search, reranking, and citations.
- [ ] Complete one RAG coding drill.
- [ ] Add one eval question.
- [ ] Explain one safety risk and mitigation.

Milestone:

- [ ] You can separate retrieval failures from generation failures and propose fixes.

### Week 4: System design and full loop

Goals:

- Whiteboard document Q&A, support agent, conversational analytics, and AI ops copilot.
- Include security, evaluation, cost, and observability.
- Timebox yourself to 35-45 minutes per design.

Daily checklist:

- [ ] Complete one system design prompt.
- [ ] Include auth/tenant isolation.
- [ ] Include deployment and observability.
- [ ] Practice one behavioral story.
- [ ] Run one mock round.

Milestone:

- [ ] You can complete a full mock interview day and identify a targeted repair plan.

---

## Answer frameworks quick reference

Behavioral:

```text
Situation -> Goal -> Constraints -> Options -> Action -> Result -> Reflection
```

Technical:

```text
Definition -> Mental model -> Example -> Trade-off -> Failure mode
```

System design:

```text
Requirements -> Architecture -> Data flow -> Deep dives -> Risks -> Metrics
```

RAG:

```text
Ingest -> Chunk -> Embed -> Retrieve with ACL -> Rerank -> Prompt -> Generate -> Cite -> Evaluate
```

Coding:

```text
Clarify -> Types -> Simple solution -> Edge cases -> Tests -> Harden
```

---

## Interview red flags to avoid

- Saying the frontend should call the LLM provider directly.
- Treating vector search as the whole RAG system.
- Ignoring tenant/ACL filtering.
- Not mentioning evaluation.
- Not supporting cancellation for streaming responses.
- Trusting model-generated tool calls without validation.
- Overusing agents when deterministic code is enough.
- Giving behavioral answers without personal ownership.
- Giving system designs without cost or observability.
- Rendering model HTML without sanitization.

---

## Portfolio pitch template

> I built a fullstack GenAI application with an Angular/React frontend, ASP.NET Core API, and a RAG backend. The API owns authentication, prompt construction, retrieval, streaming, citations, and observability. I used embeddings and hybrid search for retrieval, validated citations, and collected feedback for evaluation. For risky actions, I kept tool execution server-side with authorization and audit logging.

### 30-second version

> My project is a fullstack RAG chat app. The SPA handles chat UX, streaming, citations, and feedback. The ASP.NET Core API validates auth, retrieves tenant-authorized context, builds prompts, calls the model provider, streams responses, and logs quality/cost signals.

### 2-minute version

Cover:

1. Product problem.
2. Architecture.
3. Auth and data isolation.
4. RAG pipeline.
5. Streaming UX.
6. Evaluation and observability.
7. Trade-offs and next improvements.

---

## Final readiness checklist

- [ ] 8 behavioral stories prepared.
- [ ] 3 project pitches prepared: 30 seconds, 2 minutes, 10 minutes.
- [ ] Backend coding drill completed under time.
- [ ] Frontend streaming drill completed under time.
- [ ] RAG flow drawn from memory.
- [ ] System design answer includes requirements, architecture, security, evaluation, cost, observability.
- [ ] Mock interview day completed.
- [ ] Weakest area has a repair plan.

