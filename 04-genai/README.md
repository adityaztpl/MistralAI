# GenAI Study Track

This section is a practical interview and implementation guide for building GenAI applications with Mistral/OpenAI-style APIs, RAG, LangChain, LangGraph, and .NET Semantic Kernel.

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

