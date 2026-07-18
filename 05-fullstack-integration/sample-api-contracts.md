# Sample API Contracts for Chat and RAG

This file gives OpenAPI-style endpoint contracts plus Angular and React client snippets for an ASP.NET Core backend serving GenAI features.

---

## 1. Resource model

### Chat message

```yaml
ChatMessage:
  type: object
  required: [role, content]
  properties:
    role:
      type: string
      enum: [system, user, assistant, tool]
    content:
      type: string
    createdAt:
      type: string
      format: date-time
```

### Citation

```yaml
Citation:
  type: object
  required: [id, title, url, excerpt]
  properties:
    id:
      type: string
      example: doc-api-keys#chunk-2
    title:
      type: string
      example: API Key Rotation Policy
    url:
      type: string
      format: uri
    excerpt:
      type: string
    score:
      type: number
      format: double
```

### Usage

```yaml
Usage:
  type: object
  properties:
    inputTokens:
      type: integer
    outputTokens:
      type: integer
    model:
      type: string
    costUsd:
      type: number
```

---

## 2. POST `/api/chat`

General chat endpoint without retrieval.

### Request

```yaml
POST /api/chat
Authorization: Bearer <access-token>
Content-Type: application/json
```

```json
{
  "conversationId": "conv-123",
  "message": "Explain dependency injection in ASP.NET Core.",
  "model": "mistral-large-latest",
  "temperature": 0.2
}
```

### Response

```json
{
  "conversationId": "conv-123",
  "messageId": "msg-456",
  "answer": "Dependency injection is built into ASP.NET Core...",
  "usage": {
    "inputTokens": 310,
    "outputTokens": 180,
    "model": "mistral-large-latest",
    "costUsd": 0.0021
  }
}
```

### OpenAPI-style schema

```yaml
paths:
  /api/chat:
    post:
      summary: Send a chat message
      security:
        - bearerAuth: []
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: "#/components/schemas/ChatRequest"
      responses:
        "200":
          description: Chat response
          content:
            application/json:
              schema:
                $ref: "#/components/schemas/ChatResponse"
        "400":
          description: Invalid request
        "401":
          description: Missing or invalid token
        "429":
          description: Rate limit exceeded
```

---

## 3. POST `/api/rag/query`

Grounded question answering over indexed documents.

### Request

```json
{
  "conversationId": "conv-123",
  "question": "How often should API keys be rotated?",
  "filters": {
    "collection": "security",
    "tags": ["policy"],
    "updatedAfter": "2026-01-01T00:00:00Z"
  },
  "retrieval": {
    "topK": 8,
    "hybrid": true,
    "rerank": true
  }
}
```

### Response

```json
{
  "answer": "API keys must be rotated every 90 days [doc-api-keys#chunk-1].",
  "citations": [
    {
      "id": "doc-api-keys#chunk-1",
      "title": "API Key Rotation Policy",
      "url": "https://docs.example.com/security/api-keys",
      "excerpt": "API keys must be rotated every 90 days.",
      "score": 0.91
    }
  ],
  "retrieval": {
    "query": "How often should API keys be rotated?",
    "rewrittenQuery": null,
    "topK": 8,
    "returned": 3
  },
  "usage": {
    "inputTokens": 1420,
    "outputTokens": 96,
    "model": "mistral-large-latest",
    "costUsd": 0.006
  }
}
```

---

## 4. POST `/api/chat/stream`

Streaming endpoint using SSE-style events over an HTTP response.

### Request

```json
{
  "conversationId": "conv-123",
  "message": "Design a RAG architecture for support docs.",
  "rag": {
    "enabled": true,
    "topK": 5
  }
}
```

### Stream events

```text
event: metadata
data: {"requestId":"req-789","model":"mistral-large-latest"}

event: delta
data: {"text":"A"}

event: delta
data: {"text":" production"}

event: citation
data: {"id":"doc-rag#chunk-4","title":"RAG Guide","url":"https://docs.example.com/rag"}

event: usage
data: {"inputTokens":1200,"outputTokens":260}

event: done
data: {}
```

### Error event after partial output

```text
event: error
data: {"code":"provider_timeout","message":"The model provider timed out."}
```

---

## 5. POST `/api/documents`

Upload a document for ingestion.

### Multipart request

```yaml
POST /api/documents
Authorization: Bearer <token>
Content-Type: multipart/form-data

fields:
  collection: security
  tags: policy,api-keys
  file: api-key-policy.pdf
```

### Response

```json
{
  "documentId": "doc-123",
  "status": "queued",
  "ingestionJobId": "job-456"
}
```

---

## 6. GET `/api/documents/{documentId}/ingestion-status`

```json
{
  "documentId": "doc-123",
  "status": "completed",
  "chunksIndexed": 42,
  "warnings": []
}
```

---

## 7. POST `/api/feedback`

Capture answer feedback.

```json
{
  "conversationId": "conv-123",
  "messageId": "msg-456",
  "rating": "thumbs_down",
  "reason": "citation_not_relevant",
  "comment": "The cited page does not mention rotation frequency."
}
```

---

## 8. ASP.NET Core DTOs

```csharp
public sealed record RagQueryRequest(
    string ConversationId,
    string Question,
    RagFilters? Filters,
    RetrievalOptions? Retrieval);

public sealed record RagFilters(
    string? Collection,
    IReadOnlyList<string>? Tags,
    DateTimeOffset? UpdatedAfter);

public sealed record RetrievalOptions(
    int TopK = 5,
    bool Hybrid = true,
    bool Rerank = false);

public sealed record RagQueryResponse(
    string Answer,
    IReadOnlyList<Citation> Citations,
    RetrievalMetadata Retrieval,
    Usage Usage);
```

---

## 9. React client snippet: non-streaming RAG

```tsx
type Citation = {
  id: string;
  title: string;
  url: string;
  excerpt: string;
  score: number;
};

type RagResponse = {
  answer: string;
  citations: Citation[];
};

export async function askRag(question: string, accessToken: string): Promise<RagResponse> {
  const response = await fetch("/api/rag/query", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({
      conversationId: crypto.randomUUID(),
      question,
      retrieval: { topK: 5, hybrid: true, rerank: true },
    }),
  });

  if (!response.ok) {
    throw new Error(`RAG request failed: ${response.status}`);
  }

  return response.json();
}
```

---

## 10. React client snippet: streaming fetch

```tsx
export async function streamChat(
  message: string,
  accessToken: string,
  onDelta: (text: string) => void,
  signal?: AbortSignal
) {
  const response = await fetch("/api/chat/stream", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify({ conversationId: crypto.randomUUID(), message }),
    signal,
  });

  if (!response.body) {
    throw new Error("Streaming not supported by this browser.");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split("\n\n");
    buffer = events.pop() ?? "";

    for (const event of events) {
      const eventName = event.match(/^event: (.+)$/m)?.[1];
      const dataLine = event.match(/^data: (.+)$/m)?.[1];
      if (!eventName || !dataLine) continue;

      const payload = JSON.parse(dataLine);
      if (eventName === "delta") onDelta(payload.text ?? payload.token ?? "");
      if (eventName === "error") throw new Error(payload.message);
    }
  }
}
```

---

## 11. Angular service snippet

```ts
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RagResponse {
  answer: string;
  citations: Array<{
    id: string;
    title: string;
    url: string;
    excerpt: string;
    score: number;
  }>;
}

@Injectable({ providedIn: 'root' })
export class RagClient {
  constructor(private readonly http: HttpClient) {}

  ask(question: string, accessToken: string): Observable<RagResponse> {
    const headers = new HttpHeaders({
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    });

    return this.http.post<RagResponse>(
      '/api/rag/query',
      {
        conversationId: crypto.randomUUID(),
        question,
        retrieval: { topK: 5, hybrid: true, rerank: true },
      },
      { headers }
    );
  }
}
```

---

## 12. Contract design interview talking points

- Include citations as structured data, not only text.
- Return retrieval metadata for debugging and evaluation.
- Support cancellation for streaming.
- Separate upload/ingestion from query endpoints.
- Make rate limit and provider errors explicit.
- Avoid exposing raw prompts or provider credentials.
- Version contracts if clients depend on response shape.

---

## 13. Standard error contract

Use Problem Details for normal JSON endpoints.

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "The request contains invalid fields.",
  "instance": "/api/rag/query",
  "requestId": "req-123",
  "errors": {
    "question": ["Question is required."]
  }
}
```

### Error status guide

| Status | Meaning | Example |
|---|---|---|
| 400 | Invalid request shape | Missing question |
| 401 | Not authenticated | Missing/expired token |
| 403 | Authenticated but forbidden | Missing scope |
| 404 | Resource not found or not visible | Document ID outside tenant |
| 409 | Conflict | Ingestion already running |
| 413 | Payload too large | File too large |
| 415 | Unsupported media type | Unsupported upload type |
| 422 | Semantically invalid | Invalid retrieval option combination |
| 429 | Rate limited | Too many chat requests |
| 499-style log only | Client canceled | Stop generation clicked |
| 502/503 | Provider unavailable | LLM provider outage |
| 504 | Timeout | Provider or retrieval timeout |

### Streaming errors

If the stream has not started, return a normal status code. If the stream has started, emit:

```text
event: error
data: {"code":"provider_timeout","message":"The model provider timed out.","requestId":"req-123"}
```

---

## 14. Auth and rate-limit headers

### Request headers

```http
Authorization: Bearer <access-token>
X-Correlation-ID: <optional-client-generated-id>
```

### Response headers

```http
X-Correlation-ID: req-123
RateLimit-Limit: 20
RateLimit-Remaining: 12
RateLimit-Reset: 30
```

### Interview points

- The token identifies user, tenant, scopes, and roles.
- The API should not trust tenant IDs from request body for authorization.
- Rate-limit headers let the frontend show helpful retry UI.
- Correlation IDs help support teams connect UI errors to backend traces.

---

## 15. Versioning strategy

For interview projects, path versioning is simple:

```text
/api/v1/rag/query
/api/v1/chat/stream
```

For internal apps, you can also version specific AI behavior:

```json
{
  "promptVersion": "rag-answer-v4",
  "retrievalVersion": "hybrid-rerank-v2",
  "contractVersion": "2026-07-01"
}
```

### What to version

- Public response shape.
- Streaming event names/payloads.
- Prompt template.
- Retrieval strategy.
- Embedding model.
- Tool schemas.

---

## 16. Conversation endpoints

### Create conversation

```http
POST /api/conversations
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "title": "Security policy questions"
}
```

Response:

```json
{
  "conversationId": "conv-123",
  "title": "Security policy questions",
  "createdAt": "2026-07-17T20:00:00Z"
}
```

### List conversations

```http
GET /api/conversations?cursor=abc&pageSize=20
```

```json
{
  "items": [
    {
      "conversationId": "conv-123",
      "title": "Security policy questions",
      "updatedAt": "2026-07-17T20:30:00Z",
      "lastMessagePreview": "API keys must be rotated..."
    }
  ],
  "nextCursor": "def"
}
```

### Get messages

```http
GET /api/conversations/conv-123/messages
```

```json
{
  "items": [
    {
      "messageId": "msg-1",
      "role": "user",
      "content": "How often should API keys be rotated?",
      "createdAt": "2026-07-17T20:29:00Z"
    },
    {
      "messageId": "msg-2",
      "role": "assistant",
      "content": "API keys should be rotated every 90 days [doc-api-keys#chunk-1].",
      "citations": [
        {
          "id": "doc-api-keys#chunk-1",
          "title": "API Key Rotation Policy",
          "url": "https://docs.example.com/security/api-keys",
          "excerpt": "API keys must be rotated every 90 days."
        }
      ],
      "status": "complete",
      "createdAt": "2026-07-17T20:29:10Z"
    }
  ]
}
```

---

## 17. Document management endpoints

### List documents

```http
GET /api/documents?collection=security&status=completed&pageSize=20
```

```json
{
  "items": [
    {
      "documentId": "doc-123",
      "title": "API Key Rotation Policy",
      "collection": "security",
      "status": "completed",
      "chunksIndexed": 42,
      "uploadedAt": "2026-07-17T20:00:00Z",
      "updatedAt": "2026-07-17T20:05:00Z"
    }
  ],
  "nextCursor": null
}
```

### Delete document

```http
DELETE /api/documents/doc-123
```

Response:

```http
204 No Content
```

Deletion requirements:

- Delete or tombstone source document.
- Delete derived chunks/vectors.
- Preserve audit logs according to retention policy.
- Do not break historical conversation display; citations may show "source deleted" if needed.

---

## 18. Ingestion job endpoints

### Get job status

```http
GET /api/ingestion-jobs/job-456
```

```json
{
  "jobId": "job-456",
  "documentId": "doc-123",
  "status": "running",
  "stage": "embedding",
  "attempts": 1,
  "chunksParsed": 42,
  "chunksIndexed": 20,
  "warnings": [],
  "lastError": null,
  "createdAt": "2026-07-17T20:00:00Z",
  "updatedAt": "2026-07-17T20:03:00Z"
}
```

### Retry failed job

```http
POST /api/ingestion-jobs/job-456/retry
```

Response:

```json
{
  "jobId": "job-789",
  "status": "queued"
}
```

Authorization:

- User must have access to the document.
- Admin or document owner can retry.

---

## 19. Feedback endpoint expanded

### Request

```json
{
  "conversationId": "conv-123",
  "messageId": "msg-456",
  "rating": "thumbs_down",
  "reason": "citation_not_relevant",
  "comment": "The cited source does not mention the 90-day policy.",
  "expectedAnswer": "The policy says keys rotate every 90 days."
}
```

### Reason enum

```yaml
FeedbackReason:
  enum:
    - helpful
    - wrong_answer
    - citation_missing
    - citation_not_relevant
    - outdated
    - too_slow
    - unsafe
    - other
```

### Why this matters

Feedback should be tied to:

- Prompt version.
- Model version.
- Retrieval strategy.
- Retrieved chunk IDs.
- User/tenant hash.
- Latency and token usage.

That makes feedback actionable instead of anecdotal.

---

## 20. Admin quality endpoint

For internal/admin users:

```http
GET /api/admin/rag-quality?from=2026-07-01&to=2026-07-17
```

```json
{
  "totalQuestions": 1280,
  "thumbsDownRate": 0.12,
  "emptyRetrievalRate": 0.08,
  "citationValidationFailureRate": 0.03,
  "p95LatencyMs": 5200,
  "averageCostUsd": 0.0042,
  "topFailureReasons": [
    { "reason": "citation_not_relevant", "count": 43 },
    { "reason": "wrong_answer", "count": 38 }
  ]
}
```

This endpoint is not needed for a tiny demo, but describing it shows production thinking.

---

## 21. Contract checklist

- [ ] Every endpoint has auth requirements.
- [ ] Tenant comes from token/current user, not request body.
- [ ] Request DTOs have length limits.
- [ ] Responses include structured IDs for citations and messages.
- [ ] Errors are consistent.
- [ ] Streaming event names are documented.
- [ ] Rate-limit behavior is documented.
- [ ] Upload and ingestion are separate.
- [ ] Feedback captures reason categories.
- [ ] Admin metrics connect quality, latency, and cost.

