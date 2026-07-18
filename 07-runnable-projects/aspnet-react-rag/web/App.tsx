import { FormEvent, useMemo, useRef, useState } from "react";
import type { CSSProperties } from "react";
import { Citation, ingestDocument, streamChat } from "./api";

type Message = {
  id: string;
  role: "user" | "assistant";
  content: string;
  citations?: Citation[];
};

const sampleDocument = `Every severity-one incident requires an incident commander, a customer impact summary, a minute-by-minute timeline, contributing factors, and follow-up owners within five business days.

Follow-up owners must be named individuals, not teams. The review should focus on learning and system improvement rather than blame.`;

export default function App() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [question, setQuestion] = useState("What must be included in a severity-one incident review?");
  const [source, setSource] = useState("sample-handbook");
  const [title, setTitle] = useState("Incident Review Policy");
  const [content, setContent] = useState(sampleDocument);
  const [status, setStatus] = useState("Ready");
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);

  const latestCitations = useMemo(
    () => [...messages].reverse().find((message) => message.citations?.length)?.citations ?? [],
    [messages],
  );

  async function onIngest(event: FormEvent) {
    event.preventDefault();
    setStatus("Ingesting document...");

    try {
      const result = await ingestDocument({
        source,
        title,
        content,
        metadata: {
          demo: true,
          uploadedFrom: "react-spa",
          uploadedAt: new Date().toISOString(),
        },
      });
      setStatus(`Ingested ${result.chunkCount} chunk(s) from ${result.source}.`);
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Ingestion failed.");
    }
  }

  async function onAsk(event: FormEvent) {
    event.preventDefault();
    if (!question.trim() || isStreaming) {
      return;
    }

    const userMessage: Message = {
      id: crypto.randomUUID(),
      role: "user",
      content: question.trim(),
    };
    const assistantId = crypto.randomUUID();
    const assistantMessage: Message = {
      id: assistantId,
      role: "assistant",
      content: "",
      citations: [],
    };

    setMessages((current) => [...current, userMessage, assistantMessage]);
    setQuestion("");
    setIsStreaming(true);
    setStatus("Streaming answer...");

    const controller = new AbortController();
    abortRef.current = controller;

    try {
      await streamChat(
        {
          message: userMessage.content,
          conversationId: "demo-conversation",
          topK: 6,
        },
        (event) => {
          if (event.type === "citations") {
            setMessages((current) =>
              current.map((message) =>
                message.id === assistantId ? { ...message, citations: event.citations } : message,
              ),
            );
          }

          if (event.type === "token") {
            setMessages((current) =>
              current.map((message) =>
                message.id === assistantId
                  ? { ...message, content: message.content + event.text }
                  : message,
              ),
            );
          }

          if (event.type === "done") {
            setStatus(`Completed with ${event.citationCount} citation(s) using ${event.model}.`);
          }

          if (event.type === "error") {
            setStatus(event.message);
          }
        },
        controller.signal,
      );
    } catch (error) {
      if (controller.signal.aborted) {
        setStatus("Streaming cancelled.");
      } else {
        setStatus(error instanceof Error ? error.message : "Chat failed.");
      }
    } finally {
      setIsStreaming(false);
      abortRef.current = null;
    }
  }

  function cancelStreaming() {
    abortRef.current?.abort();
  }

  return (
    <main style={styles.shell}>
      <section style={styles.header}>
        <div>
          <p style={styles.eyebrow}>ASP.NET Core + React + pgvector</p>
          <h1 style={styles.title}>RAG Chat Demo</h1>
          <p style={styles.subtitle}>
            Ingest a document, ask a grounded question, stream the model answer, and inspect citations.
          </p>
        </div>
        <div style={styles.status}>{status}</div>
      </section>

      <section style={styles.grid}>
        <form onSubmit={onIngest} style={styles.card}>
          <h2>1. Ingest context</h2>
          <label style={styles.label}>
            Source
            <input value={source} onChange={(event) => setSource(event.target.value)} style={styles.input} />
          </label>
          <label style={styles.label}>
            Title
            <input value={title} onChange={(event) => setTitle(event.target.value)} style={styles.input} />
          </label>
          <label style={styles.label}>
            Content
            <textarea
              value={content}
              onChange={(event) => setContent(event.target.value)}
              rows={10}
              style={styles.textarea}
            />
          </label>
          <button type="submit" style={styles.button}>
            Ingest document
          </button>
        </form>

        <section style={styles.card}>
          <h2>2. Ask questions</h2>
          <div style={styles.chatWindow}>
            {messages.length === 0 && (
              <p style={styles.empty}>Ask the sample question after ingesting the document.</p>
            )}
            {messages.map((message) => (
              <article
                key={message.id}
                style={{
                  ...styles.message,
                  alignSelf: message.role === "user" ? "flex-end" : "flex-start",
                  background: message.role === "user" ? "#dbeafe" : "#f8fafc",
                }}
              >
                <strong>{message.role === "user" ? "You" : "Assistant"}</strong>
                <p style={styles.messageText}>{message.content || "..."}</p>
              </article>
            ))}
          </div>

          <form onSubmit={onAsk} style={styles.askForm}>
            <input
              value={question}
              onChange={(event) => setQuestion(event.target.value)}
              placeholder="Ask a grounded question"
              style={styles.input}
            />
            <button type="submit" disabled={isStreaming} style={styles.button}>
              Ask
            </button>
            <button type="button" onClick={cancelStreaming} disabled={!isStreaming} style={styles.secondaryButton}>
              Cancel
            </button>
          </form>
        </section>
      </section>

      <section style={styles.card}>
        <h2>Latest citations</h2>
        {latestCitations.length === 0 ? (
          <p style={styles.empty}>Citations appear after the API retrieves matching chunks.</p>
        ) : (
          <ol style={styles.citationList}>
            {latestCitations.map((citation, index) => (
              <li key={citation.id} style={styles.citation}>
                <strong>
                  [{index + 1}] {citation.title}
                </strong>
                <span>
                  {" "}
                  from <code>{citation.source}</code>, chunk {citation.chunkIndex}, distance{" "}
                  {citation.distance.toFixed(4)}
                </span>
                <p>{citation.excerpt}</p>
              </li>
            ))}
          </ol>
        )}
      </section>
    </main>
  );
}

const styles: Record<string, CSSProperties> = {
  shell: {
    maxWidth: 1180,
    margin: "0 auto",
    padding: "32px",
    fontFamily: "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, sans-serif",
    color: "#0f172a",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    gap: 24,
    alignItems: "flex-start",
    marginBottom: 24,
  },
  eyebrow: {
    color: "#2563eb",
    fontWeight: 700,
    textTransform: "uppercase",
    letterSpacing: 1,
    margin: 0,
  },
  title: {
    fontSize: 42,
    margin: "8px 0",
  },
  subtitle: {
    maxWidth: 720,
    color: "#475569",
    margin: 0,
  },
  status: {
    padding: "10px 14px",
    borderRadius: 999,
    background: "#ecfeff",
    color: "#155e75",
    fontWeight: 700,
    whiteSpace: "nowrap",
  },
  grid: {
    display: "grid",
    gridTemplateColumns: "minmax(320px, 0.9fr) minmax(420px, 1.1fr)",
    gap: 24,
    alignItems: "start",
  },
  card: {
    border: "1px solid #e2e8f0",
    borderRadius: 18,
    padding: 20,
    background: "white",
    boxShadow: "0 12px 30px rgba(15, 23, 42, 0.06)",
  },
  label: {
    display: "grid",
    gap: 6,
    marginBottom: 14,
    fontWeight: 700,
  },
  input: {
    width: "100%",
    boxSizing: "border-box",
    border: "1px solid #cbd5e1",
    borderRadius: 12,
    padding: "11px 12px",
    fontSize: 15,
  },
  textarea: {
    width: "100%",
    boxSizing: "border-box",
    border: "1px solid #cbd5e1",
    borderRadius: 12,
    padding: "11px 12px",
    fontSize: 15,
    resize: "vertical",
  },
  button: {
    border: 0,
    borderRadius: 12,
    padding: "11px 16px",
    background: "#2563eb",
    color: "white",
    fontWeight: 800,
    cursor: "pointer",
  },
  secondaryButton: {
    border: "1px solid #cbd5e1",
    borderRadius: 12,
    padding: "11px 16px",
    background: "white",
    color: "#334155",
    fontWeight: 800,
    cursor: "pointer",
  },
  chatWindow: {
    minHeight: 360,
    maxHeight: 520,
    overflowY: "auto",
    display: "flex",
    flexDirection: "column",
    gap: 12,
    padding: 12,
    border: "1px solid #e2e8f0",
    borderRadius: 14,
    background: "#f8fafc",
  },
  empty: {
    color: "#64748b",
    margin: 0,
  },
  message: {
    maxWidth: "82%",
    borderRadius: 14,
    padding: "10px 12px",
    border: "1px solid #e2e8f0",
  },
  messageText: {
    whiteSpace: "pre-wrap",
    margin: "6px 0 0",
  },
  askForm: {
    display: "grid",
    gridTemplateColumns: "1fr auto auto",
    gap: 10,
    marginTop: 14,
  },
  citationList: {
    display: "grid",
    gap: 12,
    paddingLeft: 22,
  },
  citation: {
    color: "#334155",
  },
};

