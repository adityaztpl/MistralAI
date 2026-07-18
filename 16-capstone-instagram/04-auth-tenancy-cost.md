# 04 - Auth, Tenancy, and Cost Controls

An AI content generator becomes a real SaaS-style system when multiple users and tenants can use it safely. Authentication identifies the caller. Tenancy scopes data. Cost controls prevent accidental or abusive provider spend.

## Learning goals

- Design JWT-based API auth for the capstone.
- Enforce tenant isolation in every query and artifact path.
- Add role-based capabilities.
- Track usage and estimate model cost.
- Apply quotas, budgets, and rate limits.
- Explain security/cost trade-offs in interviews.

## Authentication

Use an identity provider or dev JWT issuer. The API should validate:

- issuer;
- audience;
- signature;
- expiration;
- required claims:
  - `sub`;
  - `tenant_id`;
  - `role` or app-specific permissions.

The SPA sends the token to the ASP.NET API. The SPA never receives model-provider secrets.

## Authorization model

Suggested roles:

| Role | Capabilities |
| --- | --- |
| `Viewer` | List and view runs |
| `Creator` | Create runs and submit feedback |
| `Reviewer` | Approve outputs and edit feedback |
| `TenantAdmin` | Manage tenant settings, budgets, and users |

Suggested policies:

- `CanViewRuns`;
- `CanCreateRuns`;
- `CanSubmitFeedback`;
- `CanApproveContent`;
- `CanManageTenantBudget`.

Apply tenant scope even for admins. `TenantAdmin` means admin of the current tenant, not global admin.

## Tenant isolation

Every persistent object should include `TenantId`:

- `ContentRun`;
- `RunArtifact`;
- `RunEvent`;
- `EvalScore`;
- `HumanFeedback`;
- `UsageRecord`;
- `TenantBudget`.

Every query must filter by caller tenant:

```csharp
var run = await db.ContentRuns
    .Where(run => run.TenantId == caller.TenantId)
    .SingleOrDefaultAsync(run => run.Id == runId, ct);
```

Never trust a tenant id from the request body. The tenant id comes from validated auth claims or server-side membership lookup.

## Artifact isolation

Use server-generated paths:

```text
runs/{tenantId}/{runId}/final_content_pack.md
```

Rules:

- `tenantId` and `runId` are server-generated/validated.
- User input never chooses path segments.
- Artifact reads check tenant before returning content.
- Signed URLs, if used, are short-lived and scoped.
- Deleting a tenant includes artifact cleanup policy.

## Cost model

Track at least:

- requested model;
- input tokens;
- output tokens;
- provider request count;
- duration;
- estimated cost;
- run id;
- tenant id;
- user id.

If CrewAI does not expose exact usage for every call, start with estimated cost:

```text
estimated_cost = estimated_input_tokens * input_price
               + estimated_output_tokens * output_price
```

Refine later with provider usage metadata.

## Budgets and quotas

Suggested controls:

| Control | Example |
| --- | --- |
| Per-run post limit | Max 14 posts |
| Per-user concurrency | Max 1 running run per user |
| Per-tenant concurrency | Max 3 running runs per tenant |
| Daily run quota | 20 runs/day/tenant |
| Monthly budget | $50/month/tenant |
| Model allowlist | Small model for free tier, large model for paid tier |
| Max duration | 10 minutes/run |
| Retry limit | 1 automatic retry for transient provider failure |

When a budget is exceeded, fail before enqueueing the job. Do not discover cost controls after calling the provider.

## Rate limiting

Apply rate limits to:

- `POST /api/content-runs`;
- feedback endpoints;
- artifact download endpoints;
- status polling.

For status polling, return cache headers or recommend a polling interval to avoid noisy clients.

## Prompt and brand profile privacy

Brand descriptions, topics, generated captions, and feedback can be confidential. Treat them as tenant data:

- tenant-scoped storage;
- least-privileged logs;
- redacted prompt logging;
- retention policy;
- export/delete support if required.

## Abuse cases

Design controls for:

- user creates many expensive runs;
- user asks for prohibited content;
- user attempts another tenant's run id;
- user tries to inject output paths;
- compromised account generates spam;
- model output includes unsafe claims;
- feedback endpoint used to store abusive content;
- provider key leaks or becomes invalid.

## Observability

Metrics:

- runs created by tenant/user;
- runs succeeded/failed/cancelled;
- average duration;
- provider failures;
- estimated cost by tenant;
- quality score trend;
- feedback approval rate;
- budget denials.

Logs:

- run lifecycle events;
- authorization denials;
- budget denials;
- provider error category;
- worker process exit code.

Traces:

- API create request;
- queue wait;
- worker execution;
- provider calls if instrumented;
- artifact write;
- eval scoring.

## Interview prompts

- How does the API know which tenant a run belongs to?
- Where do you enforce tenant isolation?
- How do you prevent a user from creating unlimited expensive jobs?
- What do you log for an LLM request?
- How do you support multiple model tiers?
- What is the incident response if the provider key leaks?
- How do you distinguish app authorization from model safety?

## Done criteria

- [ ] JWT validation is configured.
- [ ] Caller context derives user and tenant from validated claims.
- [ ] Every run query is tenant-filtered.
- [ ] Artifact paths are server-generated and tenant-scoped.
- [ ] Role policies guard create/review/admin actions.
- [ ] Quotas and budgets are checked before enqueue.
- [ ] Usage records are written for every run.
- [ ] Provider secrets stay server-side.
- [ ] Auth and budget denial tests exist.

