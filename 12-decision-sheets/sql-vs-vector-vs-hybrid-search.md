# SQL vs Vector vs Hybrid Search

## The decision

Search architecture depends on what users ask and what matching means:

- **SQL search**: structured filtering, exact matches, joins, sorting, permissions.
- **Vector search**: semantic similarity over embeddings.
- **Hybrid search**: combines lexical/structured filters with vector similarity and often reranking.

For RAG, hybrid retrieval is often the production default.

## Quick default

- Use **SQL** for structured data, exact constraints, transactional queries, and reporting.
- Use **vector search** for semantic matching across unstructured text/images/code when exact terms may differ.
- Use **hybrid search** when users need semantic relevance plus keywords, metadata filters, permissions, freshness, and ranking quality.

## Comparison

| Force | SQL | Vector | Hybrid |
|---|---|---|---|
| Exact filters | Excellent | Weak alone | Excellent with metadata filters |
| Semantic meaning | Weak unless FTS/custom | Excellent | Excellent |
| Keyword precision | Good with FTS | Weak | Strong |
| Joins/transactions | Excellent | Not the purpose | Usually SQL plus vector index/search engine |
| RAG citations | Possible | Common | Strongest when chunks carry metadata |
| ACL filtering | Natural | Must be designed | Required before/inside retrieval |
| Ranking | Deterministic/custom | Similarity score | Combined score + rerank |

## Use SQL when

- Data is relational and structured.
- The query asks for exact values, ranges, joins, aggregations, or constraints.
- You need transactional consistency.
- You need strong permissions and tenant filtering.
- Users search known fields: status, date, customer, invoice number, owner.

SQL full-text search can be enough for many products. Do not add embeddings before you have a semantic need.

## Use vector search when

- Users ask natural language questions over unstructured content.
- Exact keywords differ from relevant content.
- You need similarity: "documents like this," "policy about travel expenses," "code that handles auth renewal."
- You are building RAG over articles, PDFs, tickets, docs, transcripts, or code.

Vector search failure modes:

- Similar but wrong chunks.
- Missing exact keyword matches.
- Poor chunking.
- No metadata filters.
- Embedding model mismatch.
- No reranking.
- Treating cosine similarity as truth.

## Use hybrid search when

- Users expect both semantic recall and lexical precision.
- Domain terms, IDs, product names, or error codes matter.
- You need tenant/ACL/date/type filters.
- RAG answers must cite source chunks reliably.
- Retrieval quality matters enough to tune ranking.

Typical hybrid pipeline:

```text
Normalize query
  -> apply tenant/ACL/metadata filters
  -> keyword/BM25 retrieval
  -> vector retrieval
  -> merge candidates
  -> rerank top N
  -> select diverse chunks
  -> generate answer with citations
  -> validate citations
```

## Trade-offs

SQL:

- Reliable and explainable, but not semantic by default.
- Great for structured facts, weak for fuzzy meaning.

Vector:

- Strong semantic recall, but weaker exactness and explainability.
- Requires embedding generation, index maintenance, and evals.

Hybrid:

- Best quality for many RAG/search systems, but more moving parts.
- Needs ranking evaluation and operational monitoring.

## Interview answer script

```text
I start with the query type. If users need exact structured filters, joins, or reporting, I use SQL. If they ask natural-language questions over unstructured text, I use vector search. For production RAG, I usually prefer hybrid retrieval because it combines semantic recall with keyword precision, metadata filters, ACL enforcement, and reranking.

The key is to evaluate retrieval, not just generate embeddings. I would measure recall@k, context precision, citation accuracy, latency, and answer faithfulness before trusting the system.
```

## Metrics to mention

- Recall@k for known-answer questions.
- Mean reciprocal rank (MRR).
- Context precision and context recall.
- Citation correctness.
- Answer faithfulness.
- Latency p50/p95.
- Index freshness lag.
- Cost per query.
- No-result and low-confidence rates.
