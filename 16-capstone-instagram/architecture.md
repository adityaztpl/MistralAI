# Architecture Diagrams

Use these diagrams in your portfolio README, interview notes, or system-design practice. They show how the existing CrewAI/Mistral package fits into a production-style full-stack application.

## Context diagram

```mermaid
flowchart LR
    User[Content marketer / reviewer]
    SPA[React or Angular SPA]
    API[ASP.NET Core API]
    Worker[Run worker]
    Crew[Python Instagram Content Creator<br/>CrewAI + Mistral]
    Mistral[Mistral API]
    DB[(Postgres)]
    Store[(Object storage<br/>or safe output volume)]
    IdP[Identity provider]

    User --> SPA
    SPA -->|JWT + JSON| API
    SPA -->|Login| IdP
    API -->|Validate JWT metadata| IdP
    API --> DB
    API -->|Enqueue run| Worker
    Worker --> DB
    Worker --> Crew
    Crew --> Mistral
    Worker --> Store
    API --> Store
    API --> SPA
```

## Run lifecycle

```mermaid
stateDiagram-v2
    [*] --> Queued: POST /api/content-runs
    Queued --> Running: worker dequeues
    Queued --> Cancelled: user cancels before start
    Running --> Succeeded: artifacts + eval complete
    Running --> Failed: crew/provider/artifact error
    Running --> Cancelled: cancellation signal accepted
    Failed --> Queued: explicit retry
    Succeeded --> Reviewed: human feedback submitted
    Reviewed --> [*]
    Cancelled --> [*]
```

## Sequence: create and complete run

```mermaid
sequenceDiagram
    participant U as User
    participant S as SPA
    participant A as ASP.NET API
    participant D as Postgres
    participant Q as Queue
    participant W as Worker
    participant P as Python Crew
    participant M as Mistral
    participant O as Object Storage

    U->>S: Submit brand/topic/posts
    S->>A: POST /api/content-runs (JWT)
    A->>A: Validate JWT, role, budget
    A->>D: Insert ContentRun(status=Queued)
    A->>Q: Enqueue run id
    A-->>S: 201 Created + run id
    S->>A: Poll GET /api/content-runs/{id}
    Q-->>W: Dequeue run id
    W->>D: Mark Running + event
    W->>P: Start crew with safe args
    P->>M: Provider calls
    M-->>P: Model responses
    P-->>W: Output files
    W->>O: Store artifacts
    W->>D: Store artifact metadata
    W->>D: Store eval scores
    W->>D: Mark Succeeded
    S->>A: GET /api/content-runs/{id}
    A->>D: Tenant-scoped query
    A->>O: Load artifact content/links
    A-->>S: Status + outputs + scores
```

## Component responsibilities

```mermaid
flowchart TB
    subgraph Browser
        Dashboard[Dashboard UI]
        SafeRenderer[Safe markdown/text renderer]
    end

    subgraph API["ASP.NET Core API"]
        Auth[JWT auth + policies]
        Runs[ContentRunsController]
        Budget[Budget/quota service]
        ArtifactApi[Artifact reader]
        Feedback[Feedback endpoint]
    end

    subgraph WorkerHost["Worker host"]
        QueueConsumer[Queue consumer]
        ProcessRunner[Python process runner]
        ArtifactWriter[Artifact writer]
        EvalRunner[Eval scorer]
    end

    subgraph Data
        Db[(Run metadata<br/>events feedback usage)]
        Obj[(Artifacts)]
        Secrets[Secret manager]
    end

    Dashboard --> Runs
    Dashboard --> Feedback
    Dashboard --> ArtifactApi
    SafeRenderer --> Dashboard
    Runs --> Auth
    Runs --> Budget
    Runs --> Db
    QueueConsumer --> Db
    ProcessRunner --> Secrets
    ProcessRunner --> ArtifactWriter
    ArtifactWriter --> Obj
    EvalRunner --> Db
    Feedback --> Db
```

## Tenancy and cost-control path

```mermaid
flowchart LR
    Request[Create run request]
    Auth[Validate JWT]
    Caller[Build caller context<br/>sub + tenant_id + roles]
    Policy[Role policy<br/>CanCreateRuns]
    Budget[Quota and budget check]
    Persist[Persist queued run]
    Deny[Deny before provider spend]
    Enqueue[Enqueue job]

    Request --> Auth
    Auth --> Caller
    Caller --> Policy
    Policy -->|allowed| Budget
    Policy -->|denied| Deny
    Budget -->|within limits| Persist
    Budget -->|exceeded| Deny
    Persist --> Enqueue
```

## Evaluation feedback loop

```mermaid
flowchart TD
    Inputs[Brand/topic inputs]
    Crew[Generation crew]
    Outputs[Artifacts]
    DetEval[Deterministic checks]
    Judge[Optional LLM judge]
    Scores[Eval scores]
    Human[Human reviewer feedback]
    PromptImprove[Prompt/rubric improvements]
    Golden[Regression dataset]

    Inputs --> Crew
    Crew --> Outputs
    Outputs --> DetEval
    Outputs --> Judge
    DetEval --> Scores
    Judge --> Scores
    Outputs --> Human
    Scores --> PromptImprove
    Human --> PromptImprove
    PromptImprove --> Golden
    Golden --> Crew
```

## Deployment topology

```mermaid
flowchart TB
    CDN[Static SPA hosting / CDN]
    ApiSvc[API container<br/>autoscaled]
    WorkerSvc[Worker container<br/>scaled by queue depth]
    Db[(Managed Postgres)]
    Queue[(Managed queue)]
    Storage[(Object storage)]
    Secrets[Secret manager]
    Logs[Logs / metrics / traces]
    Provider[Mistral API]

    CDN --> ApiSvc
    ApiSvc --> Db
    ApiSvc --> Queue
    ApiSvc --> Storage
    ApiSvc --> Secrets
    ApiSvc --> Logs
    Queue --> WorkerSvc
    WorkerSvc --> Db
    WorkerSvc --> Storage
    WorkerSvc --> Secrets
    WorkerSvc --> Provider
    WorkerSvc --> Logs
```

## Interview narration

When walking through the architecture:

1. Start with the user journey: "A marketer submits brand/topic input from the SPA."
2. Point out the trust boundary: "The API validates JWT and derives tenant; the client cannot set tenant or provider key."
3. Explain async design: "The API persists a queued run and returns immediately; the worker owns long-running generation."
4. Explain AI boundary: "The worker invokes the existing Python CrewAI/Mistral package with safe arguments and server-side secrets."
5. Explain persistence: "Metadata lives in Postgres; generated markdown artifacts live in object storage."
6. Explain eval: "After generation, deterministic and optional judge scoring create quality signals."
7. Explain operations: "Costs, run events, failures, and feedback are observable and tenant-scoped."

## Architecture trade-offs

| Decision | Simple option | Scalable option | Interview note |
| --- | --- | --- | --- |
| Worker | Hosted service in API | Separate worker container | Separate scaling reduces API contention |
| Python integration | Launch CLI process | Python service or queue worker | CLI reuse is fastest; service boundary scales better |
| Updates | Polling | SSE | Start simple, add realtime when valuable |
| Artifacts | Local output volume | Object storage | Object storage supports multiple workers |
| Eval | Deterministic script | Script + LLM judge + human feedback | Keep explainable checks even with judge |
| Auth | Dev JWT | Real IdP/OIDC | Same API policies should apply |

