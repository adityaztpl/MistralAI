-- Example PostgreSQL schema for notes + RAG metadata.
-- Requires pgcrypto for gen_random_uuid and pgvector for vector columns.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE app_users (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  email text NOT NULL,
  display_name text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT app_users_email_not_blank CHECK (length(trim(email)) > 0),
  CONSTRAINT app_users_email_unique UNIQUE (email)
);

CREATE TABLE notes (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
  title text NOT NULL,
  body text,
  is_archived boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz,
  CONSTRAINT notes_title_length CHECK (length(trim(title)) BETWEEN 1 AND 120),
  CONSTRAINT notes_body_length CHECK (body IS NULL OR length(body) <= 10000)
);

CREATE INDEX notes_user_archive_updated_idx
ON notes (user_id, is_archived, updated_at DESC)
WHERE deleted_at IS NULL;

CREATE TABLE note_tags (
  note_id uuid NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
  name text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (note_id, name),
  CONSTRAINT note_tags_lower_name CHECK (name = lower(trim(name)) AND length(name) BETWEEN 1 AND 64)
);

CREATE INDEX note_tags_user_name_idx
ON note_tags (user_id, name);

ALTER TABLE notes
ADD COLUMN search_vector tsvector GENERATED ALWAYS AS (
  setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
  setweight(to_tsvector('english', coalesce(body, '')), 'B')
) STORED;

CREATE INDEX notes_search_vector_idx
ON notes USING gin (search_vector);

CREATE TABLE rag_documents (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id uuid NOT NULL,
  source_uri text NOT NULL,
  title text NOT NULL,
  content_hash text NOT NULL,
  status text NOT NULL DEFAULT 'indexed',
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT rag_documents_status_check CHECK (status IN ('pending','indexing','indexed','failed','deleted')),
  CONSTRAINT rag_documents_source_unique UNIQUE (tenant_id, source_uri, content_hash)
);

CREATE INDEX rag_documents_tenant_status_idx
ON rag_documents (tenant_id, status, updated_at DESC);

CREATE TABLE rag_chunks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id uuid NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
  tenant_id uuid NOT NULL,
  chunk_index integer NOT NULL,
  heading text,
  content text NOT NULL,
  token_count integer NOT NULL,
  embedding_model text NOT NULL,
  embedding_dimension integer NOT NULL,
  embedding vector(1024) NOT NULL,
  metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT rag_chunks_token_count_positive CHECK (token_count > 0),
  CONSTRAINT rag_chunks_embedding_dimension_check CHECK (embedding_dimension = 1024),
  CONSTRAINT rag_chunks_document_index_unique UNIQUE (document_id, chunk_index)
);

CREATE INDEX rag_chunks_tenant_document_idx
ON rag_chunks (tenant_id, document_id, chunk_index);

CREATE INDEX rag_chunks_metadata_gin_idx
ON rag_chunks USING gin (metadata);

CREATE INDEX rag_chunks_embedding_hnsw_idx
ON rag_chunks USING hnsw (embedding vector_cosine_ops);

CREATE TABLE chat_sessions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id uuid NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
  title text,
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX chat_sessions_user_updated_idx
ON chat_sessions (user_id, updated_at DESC);

CREATE TABLE chat_messages (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  session_id uuid NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
  role text NOT NULL,
  content text NOT NULL,
  model_name text,
  input_tokens integer,
  output_tokens integer,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT chat_messages_role_check CHECK (role IN ('system','user','assistant','tool'))
);

CREATE INDEX chat_messages_session_created_idx
ON chat_messages (session_id, created_at ASC);

CREATE TABLE message_citations (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  message_id uuid NOT NULL REFERENCES chat_messages(id) ON DELETE CASCADE,
  chunk_id uuid REFERENCES rag_chunks(id) ON DELETE SET NULL,
  citation_label text NOT NULL,
  source_uri text NOT NULL,
  title text NOT NULL,
  heading text,
  snippet text NOT NULL,
  similarity numeric(5,4),
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX message_citations_message_idx
ON message_citations (message_id);

CREATE TABLE ingestion_jobs (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id uuid NOT NULL,
  document_id uuid REFERENCES rag_documents(id) ON DELETE SET NULL,
  source_uri text NOT NULL,
  status text NOT NULL DEFAULT 'queued',
  attempts integer NOT NULL DEFAULT 0,
  last_error text,
  queued_at timestamptz NOT NULL DEFAULT now(),
  started_at timestamptz,
  completed_at timestamptz,
  CONSTRAINT ingestion_jobs_status_check CHECK (status IN ('queued','running','succeeded','failed','cancelled'))
);

CREATE INDEX ingestion_jobs_status_queued_idx
ON ingestion_jobs (status, queued_at ASC);

CREATE TABLE rag_feedback (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  message_id uuid NOT NULL REFERENCES chat_messages(id) ON DELETE CASCADE,
  user_id uuid NOT NULL REFERENCES app_users(id) ON DELETE CASCADE,
  rating integer NOT NULL,
  comment text,
  expected_source_uri text,
  created_at timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT rag_feedback_rating_check CHECK (rating IN (-1, 1))
);

CREATE INDEX rag_feedback_message_idx
ON rag_feedback (message_id);

-- Example vector search. Replace :query_embedding and :tenant_id in your client.
-- SELECT id, document_id, heading, content,
--        1 - (embedding <=> :query_embedding::vector) AS similarity
-- FROM rag_chunks
-- WHERE tenant_id = :tenant_id
-- ORDER BY embedding <=> :query_embedding::vector
-- LIMIT 8;
