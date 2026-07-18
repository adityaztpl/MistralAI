# GenAI Flashcards

Practice LLM fundamentals, RAG, agents, tools, safety, evaluation, and production operations.

## 1. What is a token?

**Answer:** A token is a chunk of text processed by a language model. Cost, latency, and context limits are usually token-based, so prompt size and retrieved context directly affect performance.

## 2. What is temperature?

**Answer:** Temperature controls sampling randomness. Lower values are more deterministic; higher values are more varied. For factual/support tasks, use lower temperature; for brainstorming, use higher within reason.

## 3. What is top-p?

**Answer:** Top-p nucleus sampling limits choices to a probability mass. It is another randomness control. Usually tune temperature or top-p, not both aggressively.

## 4. What is an embedding?

**Answer:** An embedding is a vector representation of content meaning. Similar content tends to have nearby vectors, enabling semantic search.

## 5. What is RAG?

**Answer:** Retrieval-augmented generation retrieves external context and gives it to the model before answering. It is used for private, changing, source-grounded knowledge.

## 6. Why is RAG more than vector search?

**Answer:** RAG includes ingestion, chunking, metadata, indexing, retrieval, reranking, prompt construction, generation, citation validation, evaluation, and observability.

## 7. What is chunking?

**Answer:** Chunking splits documents into retrieval units. Good chunks preserve meaning, fit context budgets, include metadata, and avoid splitting critical information.

## 8. What is chunk overlap?

**Answer:** Overlap repeats some text between chunks to preserve context across boundaries. Too little loses meaning; too much wastes tokens and duplicates results.

## 9. What metadata should chunks carry?

**Answer:** Source ID, document title, tenant, ACL tags, section path, page/offset, timestamps, version, content type, and any domain fields needed for filtering and citations.

## 10. What is hybrid search?

**Answer:** Hybrid search combines keyword/lexical retrieval with vector semantic retrieval, often with filters and reranking. It improves recall and precision for production RAG.

## 11. What is reranking?

**Answer:** Reranking reorders retrieved candidates using a stronger model or scoring function. It improves final context quality at extra latency/cost.

## 12. What is context precision?

**Answer:** The fraction of retrieved context that is actually relevant to the answer. Low precision means the model sees distracting or misleading chunks.

## 13. What is context recall?

**Answer:** Whether retrieval found the information needed to answer. Low recall means generation cannot be grounded even if the model is strong.

## 14. What is faithfulness?

**Answer:** Faithfulness measures whether the answer is supported by provided context. A fluent answer can still be unfaithful if it invents unsupported claims.

## 15. What is hallucination?

**Answer:** A plausible but unsupported or false model output. Mitigate with grounding, citations, uncertainty, constrained outputs, evals, and not asking the model to know private facts from memory.

## 16. What is prompt injection?

**Answer:** Malicious or conflicting instructions inside user input or retrieved content that try to override system/developer instructions or exfiltrate data.

## 17. How do you mitigate prompt injection?

**Answer:** Separate instructions from data, treat retrieved text as untrusted, enforce tool permissions in code, filter outputs, avoid secret exposure, and log/evaluate attacks.

## 18. What is tool calling?

**Answer:** The model emits structured arguments requesting a server-side function/API. The application validates and executes the tool, then returns results to the model or user.

## 19. Why validate tool arguments?

**Answer:** Models can produce wrong or malicious arguments. Validate schema, authorization, tenant scope, idempotency, and business rules before executing anything.

## 20. What is human-in-the-loop?

**Answer:** A workflow where humans review or approve uncertain/risky actions. It is essential for irreversible operations, compliance decisions, or low-confidence outputs.

## 21. What is an agent?

**Answer:** An LLM-driven workflow that can decide steps, use tools, observe results, and continue toward a goal. Production agents need limits, state, observability, and safety controls.

## 22. When should you avoid agents?

**Answer:** Avoid agents when deterministic code or a simple chain solves the problem. Agents add latency, cost, nondeterminism, and safety risk.

## 23. What is LangChain best for?

**Answer:** Composing LLM calls, prompts, retrievers, tools, output parsers, and RAG pipelines, especially for quick Python/JS development.

## 24. What is LangGraph best for?

**Answer:** Stateful graph workflows with loops, conditional routing, checkpoints, retries, human approval, and multi-agent coordination.

## 25. What is Semantic Kernel best for?

**Answer:** Integrating LLM functions/plugins into .NET applications with dependency injection, strong typing, and enterprise service architecture.

## 26. What is streaming?

**Answer:** Sending partial model output to the client as it is generated. It improves perceived latency but requires cancellation, partial error handling, and UI state management.

## 27. How do you stream to a browser?

**Answer:** Use SSE, chunked fetch, WebSockets, or SignalR. The backend should own provider calls, auth, budget, and cancellation.

## 28. What are citations in RAG?

**Answer:** References to source chunks/documents supporting the answer. Citations should be validated against retrieved context, not invented from model memory.

## 29. What is an eval set?

**Answer:** A curated set of test questions, expected answers, sources, and rubrics used to measure retrieval and generation quality over time.

## 30. What is online feedback?

**Answer:** User ratings, corrections, accepted suggestions, escalations, and behavior signals collected in production. It complements offline evals but can be noisy.

## 31. What metrics matter for LLM apps?

**Answer:** Latency, cost, token usage, retrieval recall/precision, faithfulness, refusal rate, escalation rate, tool success/failure, user satisfaction, and safety incidents.

## 32. How do you control LLM cost?

**Answer:** Set token budgets, cache where safe, choose smaller models for simple tasks, summarize context, limit retries, rate-limit tenants, and monitor cost per feature/user.

## 33. What is model routing?

**Answer:** Choosing different models based on task complexity, cost, latency, privacy, or quality needs. Simple classification may use a small model; hard reasoning may use a larger one.

## 34. What is structured output?

**Answer:** Asking the model to return JSON or schema-constrained data. Validate it server-side and handle retries/repair for malformed outputs.

## 35. What is function calling vs JSON mode?

**Answer:** Function calling requests a named tool with arguments. JSON mode/schema output returns structured data but may not imply an action. Both still need validation.

## 36. What is fine-tuning?

**Answer:** Training a model on examples to improve behavior, style, or task performance. It is not the default solution for fresh private knowledge.

## 37. When use fine-tuning?

**Answer:** Use it for consistent tone, extraction, classification, domain phrasing, or repeated tasks when prompts/RAG are insufficient and you have high-quality data.

## 38. What is a vector database?

**Answer:** A system optimized for storing embeddings and retrieving nearest neighbors, often with metadata filtering. Examples include pgvector-backed Postgres, Pinecone, Weaviate, Qdrant, and others.

## 39. What is pgvector?

**Answer:** A Postgres extension for storing vectors and running similarity search. It is attractive when you already use Postgres and want transactional metadata plus vector search together.

## 40. What is ACL filtering in RAG?

**Answer:** Applying permissions so users only retrieve chunks they are allowed to see. It must happen before or during retrieval, not after answer generation.

## 41. What is context window management?

**Answer:** Selecting and compressing prompt content to fit model limits while preserving relevant instructions, retrieved evidence, conversation history, and tool results.

## 42. What is summarization memory?

**Answer:** Compressing prior conversation into a summary to save context. It can lose details, so keep critical facts structured when needed.

## 43. How should secrets be handled in LLM apps?

**Answer:** Keep provider keys server-side, use environment/secret stores, avoid logging prompts with secrets, redact sensitive data, and never expose keys to SPA clients.

## 44. What is PII redaction?

**Answer:** Detecting and removing/masking personally identifiable information before storage, logging, or model calls when policy requires it. Balance privacy with task usefulness.

## 45. What is output moderation?

**Answer:** Checking generated content for safety, policy, privacy, or compliance issues before showing or acting on it.

## 46. What is an LLM trace?

**Answer:** A structured record of the request, retrieval, prompt, model call, tool calls, response, cost, latency, and errors. It supports debugging and evals.
