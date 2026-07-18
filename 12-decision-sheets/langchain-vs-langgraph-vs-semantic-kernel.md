# LangChain vs LangGraph vs Semantic Kernel

## The decision

These tools solve related but different LLM application problems:

- **LangChain** helps compose prompts, models, retrievers, tools, output parsers, and chains.
- **LangGraph** helps build stateful, controllable, cyclic, multi-step agent workflows as graphs.
- **Semantic Kernel** helps .NET and enterprise teams integrate LLM functions/plugins into applications, especially when ASP.NET and C# are first-class.

## Quick default

- Use **LangChain** when you need quick composition of RAG chains, tools, loaders, retrievers, and model calls, especially in Python/JS ecosystems.
- Use **LangGraph** when workflow control matters: loops, retries, reflection, multi-agent collaboration, checkpoints, human-in-the-loop, and durable state.
- Use **Semantic Kernel** when the application is primarily .NET/C#, needs plugin/function integration, and should live naturally inside ASP.NET architecture.

## Comparison

| Force | LangChain | LangGraph | Semantic Kernel |
|---|---|---|---|
| Core model | Chain composition | Stateful graph workflows | Kernel, plugins, functions |
| Best for | RAG, tools, quick LLM pipelines | Agentic workflows with control | .NET enterprise integration |
| State | Usually passed through chains/memory | Explicit graph state | App/service state plus kernel context |
| Cycles | Possible but not the main abstraction | First-class | Possible through orchestration code |
| Human approval | Add manually | Natural checkpoint/interruption pattern | Add through app workflow |
| Language fit | Python/JS strong | Python/JS strong | C#/.NET strong |
| Interview framing | Composition library | Workflow runtime/model | Enterprise integration SDK |

## When to choose LangChain

Use LangChain when:

- You need a RAG pipeline quickly.
- You need loaders, splitters, retrievers, prompt templates, or output parsers.
- The workflow is mostly linear: retrieve -> prompt -> call model -> parse.
- You want a broad ecosystem and examples.
- You are prototyping or building a Python AI service.

Caution: avoid hiding important application behavior inside opaque chains. Keep auth, tenant filtering, evaluation, logging, and retries explicit.

## When to choose LangGraph

Use LangGraph when:

- The agent needs to decide among steps repeatedly.
- The workflow has loops, retries, reflection, or escalation.
- You need checkpoints and resumability.
- Humans approve risky actions before execution.
- You need deterministic control around an otherwise probabilistic model.
- You want to test graph nodes independently.

Common examples:

- Research agent with plan -> search -> read -> critique -> revise.
- Support copilot with retrieve -> diagnose -> maybe call tool -> wait for approval -> act.
- Agentic RAG with query rewrite -> retrieve -> grade documents -> regenerate query if weak -> answer.

## When to choose Semantic Kernel

Use Semantic Kernel when:

- The product is primarily ASP.NET Core or C#.
- You want LLM functions/plugins to look like normal application services.
- Enterprise developers need strong typing, dependency injection, and .NET observability.
- You need to expose business capabilities as functions under server-side authorization.
- You want to integrate streaming chat, RAG services, and plugins in one .NET service.

Caution: do not assume an SDK replaces architecture. You still need retrieval quality, cost controls, evals, audit logs, and prompt/tool safety.

## Trade-offs

LangChain:

- Broad ecosystem, fast prototyping.
- Can become hard to debug if too many abstractions are nested.
- Production apps often wrap or narrow its usage behind internal services.

LangGraph:

- Excellent control for agent workflows.
- More design effort than a simple chain.
- Best when graph state and transitions are explicit and tested.

Semantic Kernel:

- Great .NET fit and enterprise integration story.
- Ecosystem differs from Python-first LLM tooling.
- Complex agent workflows may still require custom orchestration or a graph runtime.

## Interview answer script

```text
I separate composition from orchestration. For a straightforward RAG or tool pipeline, LangChain is a good fit because it gives retrievers, prompts, parsers, and model wrappers. If the workflow becomes stateful with loops, retries, human approval, or multi-agent coordination, I move toward LangGraph because the graph makes state and transitions explicit. If the host application is ASP.NET/.NET, I strongly consider Semantic Kernel so plugins, DI, auth, and observability fit the existing platform.

The main trade-off is speed of prototyping versus operational control. In production I would keep security, tenant filtering, logging, evaluation, and cost limits outside of magical framework calls and test each step independently.
```

## Strong follow-up: production checklist

- Explicit state schema.
- Tool allowlist and argument validation.
- Tenant and ACL filters before retrieval/tool execution.
- Timeouts, retries, and circuit breakers.
- Token and cost budgets per request/user/tenant.
- Trace spans for model call, retrieval, rerank, tool call, and response streaming.
- Offline evals plus online feedback.
- Human approval for irreversible actions.
