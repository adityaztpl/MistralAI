# MistralAI

This repository contains two complementary workstreams:

| Area | What it is | Start here |
|------|------------|------------|
| **Prep curriculum** | Full-stack + Gen AI interview prep (ASP.NET, Angular, React, RAG/LangChain/LangGraph) | Sections below |
| **Instagram content creator** | CrewAI + Mistral multi-agent content pack generator | [`projects/instagram-content-creator.md`](projects/instagram-content-creator.md) |

---

# Full-Stack + Gen AI Prep Curriculum

Interview and hands-on prep from **basic → advanced** covering:

| Track | Stack |
|-------|--------|
| Backend | ASP.NET Core / .NET |
| Frontend | Angular (standalone + signals) |
| Frontend | React (hooks + concurrent features) |
| Gen AI | RAG, LangChain, LangGraph, Semantic Kernel |

Start here → follow the roadmap → practice with examples → drill interview questions.

## Quick start

1. Read [`00-roadmap/learning-path.md`](00-roadmap/learning-path.md) (week-by-week plan)
2. Pick a track below and work basic → intermediate → advanced
3. Run/study the `examples/` folders alongside each section
4. Finish with [`06-interview-prep/`](06-interview-prep/) and a project from [`05-fullstack-integration/project-ideas.md`](05-fullstack-integration/project-ideas.md)

## Curriculum map

```text
00-roadmap/                  Learning path (all tracks)
01-aspnet-core/              .NET / ASP.NET Core API
02-angular/                  Angular SPA
03-react/                    React SPA
04-genai/                    RAG · LangChain · LangGraph · Semantic Kernel
05-fullstack-integration/    Architecture · API contracts · projects
06-interview-prep/           Q&A · AI system design
projects/                    Standalone app docs (Instagram creator)
src/                         Instagram content creator package
```

### 1. ASP.NET Core

| Level | Guide | Examples |
|-------|-------|----------|
| Basics | [01-basics.md](01-aspnet-core/01-basics.md) | Minimal APIs, controllers |
| Intermediate | [02-intermediate.md](01-aspnet-core/02-intermediate.md) | EF Core, JWT, DTOs |
| Advanced | [03-advanced.md](01-aspnet-core/03-advanced.md) | CQRS/MediatR, rate limits, resilience |

→ [Section README](01-aspnet-core/README.md)

### 2. Angular

| Level | Guide | Focus |
|-------|-------|-------|
| Basics | [01-basics.md](02-angular/01-basics.md) | Components, DI, routing, forms |
| Intermediate | [02-intermediate.md](02-angular/02-intermediate.md) | RxJS, guards, interceptors, signals |
| Advanced | [03-advanced.md](02-angular/03-advanced.md) | Zoneless CD, SSR, performance |

→ [Section README](02-angular/README.md)

### 3. React

| Level | Guide | Focus |
|-------|-------|-------|
| Basics | [01-basics.md](03-react/01-basics.md) | JSX, state, effects, forms |
| Intermediate | [02-intermediate.md](03-react/02-intermediate.md) | Hooks, Context, Router, TS |
| Advanced | [03-advanced.md](03-react/03-advanced.md) | Suspense, Query, Zustand/RTK |

→ [Section README](03-react/README.md)

### 4. Gen AI

| Topic | Guide | Examples |
|-------|-------|----------|
| LLM foundations | [01-foundations.md](04-genai/01-foundations.md) | Tokens, prompts, tools vs RAG |
| RAG | [02-rag.md](04-genai/02-rag.md) | Chunking, vectors, hybrid search |
| LangChain | [03-langchain.md](04-genai/03-langchain.md) | LCEL, tools, agents |
| LangGraph | [04-langgraph.md](04-genai/04-langgraph.md) | State graphs, agentic RAG |
| .NET Semantic Kernel | [05-dotnet-semantic-kernel.md](04-genai/05-dotnet-semantic-kernel.md) | ASP.NET chat + streaming |

→ [Section README](04-genai/README.md)

### 5. Full-stack integration

- [Architecture](05-fullstack-integration/architecture.md) — ASP.NET + Angular/React + Gen AI
- [API contracts](05-fullstack-integration/sample-api-contracts.md) — Chat/RAG endpoints + client snippets
- [Project ideas](05-fullstack-integration/project-ideas.md) — Beginner → advanced builds

### 6. Interview prep

- [ASP.NET questions](06-interview-prep/questions-aspnet.md)
- [Angular / React questions](06-interview-prep/questions-angular-react.md)
- [Gen AI questions](06-interview-prep/questions-genai.md)
- [AI app system design](06-interview-prep/system-design-ai-apps.md)

## Suggested study order

```text
ASP.NET basics → React or Angular basics
       ↓
ASP.NET intermediate (EF + JWT) → Frontend intermediate (auth + HTTP)
       ↓
Gen AI foundations → RAG → LangChain → LangGraph
       ↓
Wire ASP.NET + SPA + RAG (streaming chat)
       ↓
Advanced backend/frontend + interview drills + capstone project
```

You can do **Angular and React in parallel** after ASP.NET basics, or pick one frontend for depth.

## How to use the code examples

| Folder | Language | How to study |
|--------|----------|--------------|
| `01-aspnet-core/examples/` | C# | Paste into a `dotnet new webapi` project |
| `02-angular/examples/` | TypeScript | Drop into an `ng new` standalone app |
| `03-react/examples/` | TSX | Drop into a Vite + React + TS app |
| `04-genai/examples/` | Python + C# | `pip install` deps noted in each file; SK samples go in ASP.NET |

Many Gen AI examples need API keys (`OPENAI_API_KEY`, `MISTRAL_API_KEY`, etc.). Prefer env vars — never commit secrets.

## Capstone shape (target architecture)

```text
┌─────────────┐     JWT      ┌──────────────────┐     tools      ┌─────────────────┐
│ Angular or  │ ───────────► │ ASP.NET Core API │ ─────────────► │ LangGraph / RAG │
│ React SPA   │ ◄── SSE/WS ─ │ + Semantic Kernel│                │ (Python or .NET)│
└─────────────┘              └────────┬─────────┘                └────────┬────────┘
                                      │                                   │
                                      ▼                                   ▼
                               SQL / Identity                      Vector DB (pgvector)
```

Build this using the contracts in `05-fullstack-integration/`.

---

# Instagram Content Creator (app)

Multi-agent Instagram content generator (CrewAI + Mistral). Full setup, run commands, and config live in:

→ [`projects/instagram-content-creator.md`](projects/instagram-content-creator.md)

Quick run:

```bash
python -m venv .venv
source .venv/bin/activate
pip install -e ".[dev]"
cp .env.example .env   # set MISTRAL_API_KEY
instagram-content -d "Your brand" -t "Weekly topic" -n 3
```

Sample pack: [`examples/sample_run/`](examples/sample_run/)

---

## License / purpose

Curriculum material is for interview and skills prep (teaching-oriented samples, not a production template).
The Instagram creator under `src/` is a runnable CrewAI application — see its project doc for usage.
