export type Citation = {
  id: string;
  source: string;
  title: string;
  chunkIndex: number;
  distance: number;
  excerpt: string;
  metadata: Record<string, unknown>;
};

export type ChatRequest = {
  message: string;
  conversationId?: string;
  topK: number;
  filters?: Record<string, string>;
};

export type IngestRequest = {
  source: string;
  title: string;
  content: string;
  metadata?: Record<string, unknown>;
};

export type IngestResponse = {
  batchId: string;
  source: string;
  title: string;
  chunkCount: number;
  documentIds: string[];
};

export type StreamEvent =
  | { type: "citations"; citations: Citation[] }
  | { type: "token"; text: string }
  | { type: "done"; model: string; citationCount: number }
  | { type: "error"; message: string };

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";
const DEMO_TOKEN = import.meta.env.VITE_DEMO_TOKEN ?? "dev-token";

function authHeaders(extra?: HeadersInit): HeadersInit {
  return {
    Authorization: `Bearer ${DEMO_TOKEN}`,
    ...extra,
  };
}

export async function ingestDocument(payload: IngestRequest): Promise<IngestResponse> {
  const response = await fetch(`${API_BASE_URL}/api/ingest`, {
    method: "POST",
    headers: authHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function streamChat(
  payload: ChatRequest,
  onEvent: (event: StreamEvent) => void,
  signal?: AbortSignal,
): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/chat/stream`, {
    method: "POST",
    headers: authHeaders({ "Content-Type": "application/json", Accept: "text/event-stream" }),
    body: JSON.stringify(payload),
    signal,
  });

  if (!response.ok || !response.body) {
    throw new Error(await response.text());
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { value, done } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });
    const frames = buffer.split("\n\n");
    buffer = frames.pop() ?? "";

    for (const frame of frames) {
      const parsed = parseSseFrame(frame);
      if (parsed) {
        onEvent(parsed);
      }
    }
  }
}

function parseSseFrame(frame: string): StreamEvent | null {
  const eventLine = frame.split("\n").find((line) => line.startsWith("event:"));
  const dataLine = frame.split("\n").find((line) => line.startsWith("data:"));
  if (!eventLine || !dataLine) {
    return null;
  }

  const eventName = eventLine.slice("event:".length).trim();
  const data = JSON.parse(dataLine.slice("data:".length).trim());

  if (eventName === "citations") {
    return { type: "citations", citations: data as Citation[] };
  }

  if (eventName === "token") {
    return { type: "token", text: data.text ?? "" };
  }

  if (eventName === "done") {
    return {
      type: "done",
      model: data.model ?? "unknown",
      citationCount: data.citationCount ?? 0,
    };
  }

  if (eventName === "error") {
    return { type: "error", message: data.message ?? "Unknown streaming error" };
  }

  return null;
}

