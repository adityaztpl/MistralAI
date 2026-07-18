# Interview Prep Index

This section prepares you for fullstack + GenAI interviews with practical question banks and system design prompts.

## Files

- [ASP.NET Core questions](questions-aspnet.md)
- [Angular/React questions](questions-angular-react.md)
- [GenAI questions](questions-genai.md)
- [System design for AI apps](system-design-ai-apps.md)

## 4-week interview routine

### Week 1: Backend fundamentals

- Practice ASP.NET Core request pipeline, DI, auth, EF Core, and API design.
- Build one small API endpoint with validation, logging, and cancellation.
- Explain middleware order out loud.

### Week 2: Frontend fundamentals

- Practice React hooks or Angular services/forms/change detection.
- Build a streaming chat UI with stop-generation.
- Explain SPA auth flow and API error handling.

### Week 3: GenAI fundamentals

- Explain tokens, embeddings, RAG, tools, agents, and fine-tuning.
- Build a basic RAG demo and evaluate retrieval failures.
- Practice hallucination mitigation answers.

### Week 4: System design

- Whiteboard document Q&A, support agent, and conversational analytics.
- Include security, evaluation, cost, and observability.
- Timebox yourself to 35-45 minutes per design.

## Answer framework

For behavioral and technical answers, use:

```text
Context -> Decision -> Trade-off -> Result -> What I would improve
```

For system design:

```text
Requirements -> Architecture -> Data flow -> Deep dives -> Risks -> Metrics
```

## Interview red flags to avoid

- Saying the frontend should call the LLM provider directly.
- Treating vector search as the whole RAG system.
- Ignoring tenant/ACL filtering.
- Not mentioning evaluation.
- Not supporting cancellation for streaming responses.
- Trusting model-generated tool calls without validation.
- Overusing agents when deterministic code is enough.

## Portfolio pitch template

> I built a fullstack GenAI application with an Angular/React frontend, ASP.NET Core API, and a RAG backend. The API owns authentication, prompt construction, retrieval, streaming, citations, and observability. I used embeddings and hybrid search for retrieval, validated citations, and collected feedback for evaluation. For risky actions, I kept tool execution server-side with authorization and audit logging.

