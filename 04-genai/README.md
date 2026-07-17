# GenAI Study Track

This section is a practical interview and implementation guide for building GenAI applications with Mistral/OpenAI-style APIs, RAG, LangChain, LangGraph, .NET Semantic Kernel, prompt engineering, vector databases, agents, evaluation, and production operations.

The track is intentionally broad and deep: read it as both a study guide and a field manual for designing real GenAI features.

## Recommended order

1. [Foundations](01-foundations.md)
   - Tokens, embeddings, temperature, context windows
   - Prompt engineering and hallucination mitigation
   - Tools vs RAG vs fine-tuning

2. [RAG](02-rag.md)
   - Ingestion, chunking, embeddings, vector stores
   - Retrieval, reranking, hybrid search, citations
   - Evaluation with faithfulness and context precision

3. [LangChain](03-langchain.md)
   - LCEL chains
   - Prompts, loaders, splitters, retrievers
   - Tools, agents, memory, output parsers

4. [LangGraph](04-langgraph.md)
   - State machines and graph workflows
   - Nodes, edges, conditional routing
   - Checkpoints, human-in-the-loop, multi-agent systems

5. [.NET Semantic Kernel](05-dotnet-semantic-kernel.md)
   - ASP.NET Core integration
   - Plugins/functions
   - RAG and streaming to SPA frontends

6. [Prompt Engineering](06-prompt-engineering.md)
   - Prompt layers, task contracts, few-shot examples
   - Structured output, prompt injection defense
   - Prompt versioning and regression testing

7. [Embeddings and Vector Databases](07-embeddings-and-vector-dbs.md)
   - Similarity metrics, chunking, metadata, ANN indexes
   - Vector DB selection, hybrid search, reranking
   - Embedding lifecycle, re-indexing, retrieval metrics

8. [Agents and Tools](08-agents-and-tools.md)
   - Tool schemas, ReAct loops, graph agents
   - Side-effect safety, authorization, human approval
   - Agent evaluation and observability

9. [Evaluation and Observability](09-evaluation-and-observability.md)
   - Golden datasets, RAG metrics, LLM-as-judge
   - Safety/red-team evaluation
   - Tracing, scorecards, release gates

10. [Production Patterns](10-production-patterns.md)
    - Cost controls, caching, guardrails
    - Streaming UX and APIs
    - Multi-tenant isolation, reliability, security

11. [Cheat Sheet](11-cheatsheet.md)
    - Fast definitions, decision tables, checklists
    - Interview Q&A and whiteboard diagrams

## Code examples

```text
examples/
  01-rag/
    basic_rag.py
    hybrid_retriever.py
  02-langchain/
    lcel_chain.py
    rag_chain.py
    tool_agent.py
  03-langgraph/
    simple_graph.py
    agentic_rag.py
    multi_agent.py
  04-dotnet-semantic-kernel/
    ChatController.cs
    RagService.cs
    StreamingChat.cs
  05-prompts/
    few_shot_and_cot.py
  06-vectors/
    chunking_strategies.py
    pgvector_notes.md
  07-agents/
    react_agent.py
  08-eval/
    ragas_style_eval.py
  09-production/
    streaming_api.py
    guardrails.py
```

## How to study

For each topic:

1. Read the concept guide.
2. Draw the architecture from memory.
3. Run or trace the example code.
4. Explain the trade-offs out loud.
5. Answer the interview questions without notes.
6. Modify one example for a realistic product scenario.

## Interview positioning

Strong GenAI fullstack candidates can explain:

- Why RAG is an application architecture, not just vector search.
- How to choose chunk size and metadata.
- Why citations need validation.
- How to secure tool calling.
- How streaming changes API and frontend design.
- Why agents need state, limits, observability, and human approval.
- When a .NET team should use Semantic Kernel versus a Python AI service.
- How to evaluate prompts, retrieval, generated answers, and agent behavior separately.
- How to control cost with token budgets, model routing, caching, and loop limits.
- How to keep multi-tenant data isolated across retrieval, prompts, caches, logs, and tools.

## Capstone design prompts

Use these to practice end-to-end system design:

1. Design a customer-support RAG assistant for a multi-tenant SaaS company.
2. Add tool calling so the assistant can look up invoice status but cannot issue refunds without approval.
3. Add streaming responses to an ASP.NET Core + React application.
4. Define prompt, retrieval, and safety evals that gate releases.
5. Add cost controls for a launch where usage may spike 10x.
6. Design a migration from simple LCEL RAG to a LangGraph agentic RAG workflow.

## Suggested study path by role

| Role focus | Must-read files |
|---|---|
| Fullstack product engineer | 01, 02, 06, 09, 10, 11 |
| Python AI engineer | 02, 03, 04, 07, 08, 09 |
| .NET backend engineer | 01, 02, 05, 10, 11 |
| Staff/system design interview | All files, especially 08-10 |

