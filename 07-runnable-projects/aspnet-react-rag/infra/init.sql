create extension if not exists vector;
create extension if not exists pgcrypto;

create table if not exists documents (
    id uuid primary key default gen_random_uuid(),
    tenant_id text not null default 'demo',
    source text not null,
    title text not null,
    chunk_index integer not null,
    content text not null,
    token_estimate integer not null,
    metadata jsonb not null default '{}'::jsonb,
    embedding vector(1024) not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    deleted_at timestamptz null,
    constraint documents_chunk_index_nonnegative check (chunk_index >= 0),
    constraint documents_content_not_blank check (length(trim(content)) > 0)
);

create index if not exists ix_documents_tenant_source
    on documents (tenant_id, source)
    where deleted_at is null;

create index if not exists ix_documents_metadata_gin
    on documents using gin (metadata)
    where deleted_at is null;

-- ivfflat is a practical local default. For production, tune lists/probes against real data.
create index if not exists ix_documents_embedding_ivfflat
    on documents using ivfflat (embedding vector_cosine_ops)
    with (lists = 100)
    where deleted_at is null;

create table if not exists ingestion_batches (
    id uuid primary key default gen_random_uuid(),
    tenant_id text not null default 'demo',
    source text not null,
    title text not null,
    chunk_count integer not null default 0,
    status text not null default 'completed',
    error_message text null,
    created_at timestamptz not null default now(),
    constraint ingestion_batches_status_check
        check (status in ('queued', 'processing', 'completed', 'failed'))
);

create or replace function set_updated_at()
returns trigger language plpgsql as $$
begin
    new.updated_at = now();
    return new;
end;
$$;

drop trigger if exists documents_set_updated_at on documents;
create trigger documents_set_updated_at
before update on documents
for each row execute function set_updated_at();

insert into documents (source, title, chunk_index, content, token_estimate, metadata, embedding)
values (
    'sample-handbook',
    'Incident Review Policy',
    0,
    'Every severity-one incident requires an incident commander, a customer impact summary, a minute-by-minute timeline, contributing factors, and follow-up owners within five business days.',
    32,
    '{"team":"platform","type":"policy","demo":true}'::jsonb,
    array_fill(0.001::real, array[1024])::vector
)
on conflict do nothing;

