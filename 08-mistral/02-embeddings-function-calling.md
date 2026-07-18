# Embeddings and Function Calling with Mistral

Embeddings and function calling solve different problems:

- **Embeddings** turn text into vectors so you can search by meaning.
- **Function calling** lets a model request structured calls to tools that your system executes.

Together, they power serious AI applications: RAG, support assistants, workflow automation, data
lookup, and agentic orchestration.

## 1. Embeddings mental model

An embedding is a list of numbers representing semantic meaning. Similar text produces vectors that
are close under a distance metric such as cosine distance.

Typical lifecycle:

1. Normalize source document.
2. Split document into chunks.
3. Embed each chunk.
4. Store chunk text, metadata, ACLs, and vector.
5. Embed user query.
6. Retrieve nearest chunks.
7. Feed retrieved context into chat completion.

## 2. Embedding request shape

```http
POST https://api.mistral.ai/v1/embeddings
Authorization: Bearer $MISTRAL_API_KEY
Content-Type: application/json
```

```json
{
  "model": "mistral-embed",
  "input": [
    "First document chunk",
    "Second document chunk"
  ]
}
```

Design notes:

- Batch inputs when safe to reduce overhead.
- Validate embedding dimension before insert.
- Store the embedding model name with indexed data.
- Re-embed when the model or chunking strategy changes.

## 3. Chunking strategy

Bad chunking produces bad RAG even with a strong model.

| Source | Better chunking strategy |
| --- | --- |
| Markdown | Split by headings, then token budget |
| PDFs | Preserve page numbers and section titles |
| Code | Split by symbol/function/class |
| Tickets | Split by message/author/timestamp |
| Tables | Store row-level records plus table metadata |
| Policies | Split by section and subsection |

Chunk metadata should include:

- `source_id`
- `title`
- `section`
- `page`
- `url`
- `tenant_id`
- `access_control`
- `created_at`
- `embedding_model`
- `chunk_version`

## 4. Vector storage choices

| Option | Strength | Tradeoff |
| --- | --- | --- |
| PostgreSQL + pgvector | Familiar SQL, transactions, metadata joins | Scaling/tuning needed at very high volume |
| Dedicated vector DB | Search scale and vector features | Extra infrastructure and consistency model |
| Search engine hybrid | Combines lexical and vector search | More operational complexity |
| In-memory index | Simple demos and tests | Not durable or multi-user |

Interview answer:

> I start with pgvector when the dataset and latency requirements fit PostgreSQL because it keeps
> metadata, ACLs, and vectors in one transactional system. I move to a dedicated vector service when
> index size, latency, multi-region search, or specialized ranking features justify the added system.

## 5. Retrieval query pattern

```sql
select id, title, content, metadata, embedding <=> @query_embedding as distance
from document_chunks
where tenant_id = @tenant_id
  and deleted_at is null
  and acl_group = any(@user_groups)
order by embedding <=> @query_embedding
limit 8;
```

Production additions:

- Metadata filters before vector sort.
- Hybrid full-text rank.
- Rerank top 30 down to top 6.
- Deduplicate adjacent chunks.
- Enforce ACLs in SQL, not prompt text.

## 6. Prompt construction for RAG

Good prompt:

```text
You are a grounded assistant.
Answer only from the context below.
If the answer is missing, say what is missing.
Cite sources as [1], [2], ...

Context:
[1] title=Incident Policy source=handbook page=4
Severity-one incidents require...

Question:
What must a severity-one review include?
```

Bad prompt:

```text
Here is a lot of text. Answer the user.
```

Why the good version works:

- It states the grounding contract.
- It defines citation format.
- It isolates context from instructions.
- It gives stable citation ids.
- It gives an explicit no-answer rule.

## 7. RAG evaluation

Measure:

- Retrieval recall: did the correct chunk appear?
- Answer faithfulness: is the answer supported?
- Citation accuracy: do citations point to supporting text?
- Refusal quality: does it say "not enough context" when appropriate?
- Latency and cost.

Create an eval table:

| Question | Expected source | Expected facts | Should answer? |
| --- | --- | --- | --- |
| What is the refund SLA? | billing-policy.md | 5 business days | yes |
| What is the CEO's phone number? | none | none | no |

## 8. Function calling mental model

Function calling is not magic execution. The loop is:

1. Developer defines tools with JSON schema.
2. User asks a question.
3. Model returns normal text or tool calls.
4. Application validates tool-call arguments.
5. Application executes allowed tools.
6. Application appends tool results as `tool` messages.
7. Model produces final answer or asks for another tool.

## 9. Tool schema example

```json
{
  "type": "function",
  "function": {
    "name": "get_order_status",
    "description": "Look up the status of one order owned by the current user.",
    "parameters": {
      "type": "object",
      "properties": {
        "order_id": {
          "type": "string",
          "description": "Public order id, for example ORD-1001"
        }
      },
      "required": ["order_id"],
      "additionalProperties": false
    }
  }
}
```

Schema guidelines:

- Keep names stable and descriptive.
- Add clear descriptions.
- Use enums for finite choices.
- Use `additionalProperties: false` when supported by your validator.
- Validate again in application code.
- Never expose tools the user is not allowed to use.

## 10. Tool choice

Common modes:

- `auto`: model decides whether to call a tool.
- `none`: model must answer without tools.
- `any` / `required`: model must call at least one tool.
- Specific tool: force a named function.

Use cases:

- Search assistant: `auto`.
- Form extractor requiring DB write: specific tool after user confirmation.
- Test harness: force a tool to validate schema.
- Plain FAQ answer: `none`.

## 11. Parallel tool calls

Parallel tool calls can reduce latency when tools are independent:

- Get weather for Paris and London.
- Fetch account profile and feature flags.
- Retrieve multiple documents.

Disable or serialize when:

- Tools mutate shared state.
- Order matters.
- One tool result determines the next tool.
- External systems have tight rate limits.

## 12. Security rules for tools

Always enforce:

- Authentication before tool access.
- Authorization inside the tool.
- Input validation after model output.
- Output size limits before returning to model.
- Timeouts for every external call.
- Idempotency for writes.
- Human confirmation for high-risk actions.

Never let the model:

- Choose raw SQL.
- Choose arbitrary URLs for internal network access.
- Override authorization.
- Execute shell commands without a sandbox and approval path.
- See secrets in tool responses.

## 13. Tool result design

Return compact JSON:

```json
{
  "order_id": "ORD-1001",
  "status": "shipped",
  "carrier": "UPS",
  "eta": "2026-07-21"
}
```

Avoid:

- Huge HTML blobs.
- Full database records with private fields.
- Stack traces.
- Secrets or tokens.
- Ambiguous free text when structured data is available.

## 14. Failure handling

Tool call can fail because:

- Arguments are invalid.
- User is unauthorized.
- Record is missing.
- External service times out.
- Tool has side-effect conflict.

Return a safe tool result:

```json
{
  "error": "not_found",
  "message": "No order with that id belongs to the current user."
}
```

Then let the model explain the result in user-friendly language.

## 15. Combining embeddings and tools

Patterns:

- **RAG then tool**: retrieve policy, then calculate result.
- **Tool then RAG**: fetch account tier, then retrieve tier-specific docs.
- **RAG as a tool**: expose `search_knowledge_base(query, filters)` to an agent loop.
- **Tool-verified RAG**: model drafts answer, tool checks facts against source-of-truth.

Example workflow:

1. User asks: "Can customer C001 get a refund?"
2. Tool fetches customer plan and purchase date.
3. RAG retrieves refund policy for that plan.
4. Model answers with policy citation and computed eligibility.

## 16. Interview questions

1. Why are embeddings useful for RAG?
2. Why does vector search not solve authorization?
3. How do you handle embedding model changes?
4. What is the difference between tool calling and tool execution?
5. How do you prevent a model from calling dangerous tools?
6. How do you test a tool-call loop?
7. When should parallel tool calls be disabled?
8. How do you evaluate RAG quality?

## 17. Strong answers

- **Embedding changes**: version chunks by embedding model and re-index in the background.
- **Authorization**: filter by tenant and ACL before retrieval; never rely on prompt rules.
- **Tool safety**: validate schemas, enforce auth, bound side effects, and require confirmation for risky actions.
- **RAG evaluation**: maintain expected-answer datasets and measure retrieval, faithfulness, citations, latency, and cost.

