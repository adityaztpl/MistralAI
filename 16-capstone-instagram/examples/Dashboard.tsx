// Teaching sketch for 16-capstone-instagram.
// A compact React dashboard for creating Instagram content runs, listing
// history, inspecting details, and submitting feedback.

import { FormEvent, useEffect, useMemo, useState } from "react";

type ContentRunStatus = "Queued" | "Running" | "Succeeded" | "Failed" | "Cancelled";

interface CreateContentRunRequest {
  brandDescription: string;
  topic: string;
  numberOfPosts: number;
  tone?: string;
  audience?: string;
  model?: string;
}

interface ContentRunSummary {
  id: string;
  status: ContentRunStatus;
  brandDescription: string;
  topic: string;
  numberOfPosts: number;
  model: string;
  createdAt: string;
  completedAt?: string;
  estimatedCostCents?: number;
}

interface RunArtifact {
  kind: string;
  path: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

interface ContentRunDetail extends ContentRunSummary {
  tone?: string;
  audience?: string;
  promptVersion: string;
  startedAt?: string;
  errorCode?: string;
  errorMessage?: string;
  artifacts: RunArtifact[];
}

interface FeedbackRequest {
  rating: number;
  approvedForPublishing: boolean;
  notes: string;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      // Real app: attach access token from your auth library.
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Request failed with ${response.status}`);
  }

  return (await response.json()) as T;
}

export function Dashboard() {
  const [runs, setRuns] = useState<ContentRunSummary[]>([]);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [selectedRun, setSelectedRun] = useState<ContentRunDetail | null>(null);
  const [form, setForm] = useState<CreateContentRunRequest>({
    brandDescription: "",
    topic: "",
    numberOfPosts: 3,
    tone: "warm, useful, concise",
    audience: "",
    model: "mistral-large-latest",
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activeRunIds = useMemo(
    () => runs.filter((run) => run.status === "Queued" || run.status === "Running").map((run) => run.id),
    [runs],
  );

  useEffect(() => {
    void loadRuns();
  }, []);

  useEffect(() => {
    if (!selectedRunId) {
      setSelectedRun(null);
      return;
    }

    void loadRunDetail(selectedRunId);
  }, [selectedRunId]);

  useEffect(() => {
    if (activeRunIds.length === 0) {
      return;
    }

    const interval = window.setInterval(() => {
      void loadRuns();
      if (selectedRunId && activeRunIds.includes(selectedRunId)) {
        void loadRunDetail(selectedRunId);
      }
    }, 3000);

    return () => window.clearInterval(interval);
  }, [activeRunIds, selectedRunId]);

  async function loadRuns() {
    try {
      setRuns(await api<ContentRunSummary[]>("/api/content-runs"));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load runs.");
    }
  }

  async function loadRunDetail(id: string) {
    try {
      setSelectedRun(await api<ContentRunDetail>(`/api/content-runs/${id}`));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load run.");
    }
  }

  async function submitRun(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      const created = await api<ContentRunSummary>("/api/content-runs", {
        method: "POST",
        body: JSON.stringify(form),
      });

      setRuns((current) => [created, ...current]);
      setSelectedRunId(created.id);
      setForm((current) => ({ ...current, topic: "" }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create run.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function submitFeedback(feedback: FeedbackRequest) {
    if (!selectedRunId) {
      return;
    }

    await api(`/api/content-runs/${selectedRunId}/feedback`, {
      method: "POST",
      body: JSON.stringify(feedback),
    });
  }

  return (
    <main className="dashboard">
      <header>
        <h1>Instagram Content Creator</h1>
        <p>Create CrewAI/Mistral content runs, review artifacts, and capture feedback.</p>
      </header>

      {error && (
        <section role="alert" className="error">
          {error}
        </section>
      )}

      <section className="grid">
        <form onSubmit={submitRun} className="panel" aria-label="Create content run">
          <h2>New run</h2>

          <label>
            Brand description
            <textarea
              minLength={20}
              maxLength={2000}
              required
              value={form.brandDescription}
              onChange={(event) => setForm({ ...form, brandDescription: event.target.value })}
              placeholder="A specialty coffee brand for remote workers who care about ritual and quality"
            />
          </label>

          <label>
            Topic
            <input
              minLength={5}
              maxLength={300}
              required
              value={form.topic}
              onChange={(event) => setForm({ ...form, topic: event.target.value })}
              placeholder="Morning brew routines for deep work"
            />
          </label>

          <label>
            Number of posts
            <input
              type="number"
              min={1}
              max={14}
              value={form.numberOfPosts}
              onChange={(event) => setForm({ ...form, numberOfPosts: Number(event.target.value) })}
            />
          </label>

          <label>
            Audience
            <input
              maxLength={300}
              value={form.audience ?? ""}
              onChange={(event) => setForm({ ...form, audience: event.target.value })}
              placeholder="Remote workers, founders, and creators"
            />
          </label>

          <label>
            Tone
            <input
              maxLength={200}
              value={form.tone ?? ""}
              onChange={(event) => setForm({ ...form, tone: event.target.value })}
              placeholder="Warm, expert, concise"
            />
          </label>

          <label>
            Model
            <select
              value={form.model}
              onChange={(event) => setForm({ ...form, model: event.target.value })}
            >
              <option value="mistral-large-latest">Mistral Large</option>
              <option value="mistral-small-latest">Mistral Small</option>
            </select>
          </label>

          <button type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Creating run..." : "Create run"}
          </button>
        </form>

        <section className="panel" aria-label="Run history">
          <h2>Run history</h2>
          {runs.length === 0 ? (
            <p>No runs yet. Create one to start building your content calendar.</p>
          ) : (
            <ul className="run-list">
              {runs.map((run) => (
                <li key={run.id}>
                  <button
                    type="button"
                    className={run.id === selectedRunId ? "selected run-card" : "run-card"}
                    onClick={() => setSelectedRunId(run.id)}
                  >
                    <span className={`badge badge-${run.status.toLowerCase()}`}>{run.status}</span>
                    <strong>{run.topic}</strong>
                    <span>{run.numberOfPosts} posts</span>
                    <span>{new Date(run.createdAt).toLocaleString()}</span>
                    {typeof run.estimatedCostCents === "number" && (
                      <span>${(run.estimatedCostCents / 100).toFixed(2)} est.</span>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </section>
      </section>

      <RunDetail run={selectedRun} onSubmitFeedback={submitFeedback} />
    </main>
  );
}

function RunDetail({
  run,
  onSubmitFeedback,
}: {
  run: ContentRunDetail | null;
  onSubmitFeedback: (feedback: FeedbackRequest) => Promise<void>;
}) {
  const [notes, setNotes] = useState("");
  const [rating, setRating] = useState(4);
  const [approved, setApproved] = useState(false);
  const [feedbackSaved, setFeedbackSaved] = useState(false);

  if (!run) {
    return (
      <section className="panel">
        <h2>Run detail</h2>
        <p>Select a run to inspect status, artifacts, scores, and feedback.</p>
      </section>
    );
  }

  async function saveFeedback(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await onSubmitFeedback({
      rating,
      approvedForPublishing: approved,
      notes,
    });
    setFeedbackSaved(true);
  }

  return (
    <section className="panel run-detail">
      <header>
        <h2>{run.topic}</h2>
        <span className={`badge badge-${run.status.toLowerCase()}`}>{run.status}</span>
      </header>

      <dl>
        <dt>Model</dt>
        <dd>{run.model}</dd>
        <dt>Prompt version</dt>
        <dd>{run.promptVersion}</dd>
        <dt>Brand</dt>
        <dd>{run.brandDescription}</dd>
        {run.errorCode && (
          <>
            <dt>Error</dt>
            <dd>
              {run.errorCode}: {run.errorMessage}
            </dd>
          </>
        )}
      </dl>

      <h3>Artifacts</h3>
      {run.artifacts.length === 0 ? (
        <p>Artifacts will appear when the run finishes.</p>
      ) : (
        <ul>
          {run.artifacts.map((artifact) => (
            <li key={artifact.kind}>
              <strong>{artifact.kind}</strong> - {artifact.contentType}, {artifact.sizeBytes} bytes
            </li>
          ))}
        </ul>
      )}

      {run.status === "Succeeded" && (
        <form onSubmit={saveFeedback} className="feedback-form">
          <h3>Human feedback</h3>

          <label>
            Rating
            <input
              type="number"
              min={1}
              max={5}
              value={rating}
              onChange={(event) => setRating(Number(event.target.value))}
            />
          </label>

          <label>
            <input
              type="checkbox"
              checked={approved}
              onChange={(event) => setApproved(event.target.checked)}
            />
            Approved for publishing after normal review
          </label>

          <label>
            Notes
            <textarea
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              placeholder="What worked? What needs editing? Did brand voice match?"
            />
          </label>

          <button type="submit">Save feedback</button>
          {feedbackSaved && <span role="status">Feedback saved.</span>}
        </form>
      )}
    </section>
  );
}

