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

