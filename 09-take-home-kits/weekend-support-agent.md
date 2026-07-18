# Weekend Take-Home: Agentic Support Agent with Human-in-the-Loop

## Interview-ready summary

Build a support agent that can triage customer requests, retrieve policy/product knowledge, draft responses, and request human approval before taking risky actions. The goal is not to build a magic autonomous bot. The goal is to show production judgment: deterministic workflow where possible, agentic reasoning where useful, tool safety, audit logs, escalation, and observability.

## Timebox

2-3 days. Plan for 12-18 focused hours, not an endless weekend.

Recommended split:

| Phase | Time | Goal |
|---|---:|---|
| Requirements and workflow design | 1.5 h | Define support scenarios, tools, HITL gates |
| Project setup | 1 h | API, UI, storage, sample data |
| Knowledge retrieval | 2 h | Ingest docs, retrieve citations |
| Ticket workflow | 3 h | Create, classify, draft, approve, resolve |
| Tool/action layer | 2 h | Safe mock actions with validation and audit |
| Human review UI | 2 h | Queue, diff, approve/reject/edit |
| Tests and eval | 2 h | Workflow tests and safety cases |
| Observability and README | 2 h | Logs, traces, demo script, trade-offs |

## Product brief

A support team receives tickets from customers. The system should:

1. Classify the ticket.
2. Retrieve relevant knowledge base articles.
3. Draft a response with citations.
4. Decide whether human approval is required.
5. Allow a human reviewer to approve, edit, reject, or escalate.
6. Execute only safe mock actions after approval.
7. Keep an audit trail.

## Example scenarios

### Scenario 1: Low-risk FAQ

Customer asks: "How do I reset my API key?"

Expected behavior:

- Classify as `how_to`.
- Retrieve API key rotation article.
- Draft response with citation.
- Mark as auto-send eligible if policy allows.
- Log decision.

### Scenario 2: Billing refund

Customer asks: "Please refund last month's invoice."

Expected behavior:

- Classify as `billing`.
- Retrieve refund policy.
- Draft response.
- Require human approval before any refund action.
- Show proposed action details.
- Audit approval and mock refund execution.

### Scenario 3: Security incident

Customer says: "I think our account was compromised."

Expected behavior:

- Classify as `security` and `high_priority`.
- Do not provide risky instructions beyond documented containment steps.
- Escalate to human immediately.
- Optionally draft internal summary.

## Required features

### Ticket intake

- Create a ticket with customer, subject, message, priority, and channel.
- Store ticket status: `new`, `triaged`, `drafted`, `awaiting_approval`, `approved`, `sent`, `escalated`, `closed`.
- Show ticket history.

### Classification

Classify each ticket into categories such as:

- `how_to`
- `bug_report`
- `billing`
- `account_access`
- `security`
- `feature_request`
- `other`

Include confidence and rationale.

### Retrieval

- Retrieve knowledge base articles or policy snippets.
- Preserve citations.
- Show retrieved sources to the reviewer.

### Drafting

Draft a customer response that:

- Uses an appropriate tone.
- Cites internal sources in reviewer view.
- Does not reveal hidden policies if they are not customer-facing.
- States uncertainty when the answer is not in the knowledge base.

### Human-in-the-loop controls

Human reviewer can:

- Approve draft.
- Edit draft.
- Reject draft with reason.
- Escalate ticket.
- Approve or reject proposed actions.

### Tool/action layer

Implement mock tools, not real external side effects:

- `issue_refund(customerId, invoiceId, amount)`
- `reset_api_key(customerId)`
- `create_bug_report(ticketId, summary)`
- `send_customer_reply(ticketId, body)`

Every tool call must be validated and audited.

### Audit log

Record:

- Actor: system, agent, or human user.
- Action type.
- Input summary.
- Decision.
- Timestamp.
- Related ticket id.
- Model/tool version if applicable.

## Workflow design

A strong implementation uses an explicit state machine. Example:

```text
new
  -> triaged
  -> drafted
  -> awaiting_approval
  -> approved
  -> sent
  -> closed
```

Escalation can occur from any risky state:

```text
new|triaged|drafted|awaiting_approval -> escalated
```

Avoid burying irreversible actions inside free-form LLM output. The model can propose actions; deterministic code validates whether they are allowed.

## Agent architecture

```text
Ticket API
  -> Workflow orchestrator
      -> Classifier node
      -> Retriever node
      -> Draft response node
      -> Risk policy node
      -> Human approval node
      -> Tool execution node
  -> Audit log
  -> Reviewer UI
```

Implementation options:

- LangGraph for explicit node/edge workflow.
- Semantic Kernel planners/functions with deterministic policy checks.
- Plain application service with LLM calls in selected steps.

A plain service is acceptable if the workflow is clear. Do not use an agent framework only for buzzword value.

## Risk policy

Create a simple policy table:

| Category | Auto-send? | Human approval? | Tool action allowed? |
|---|---|---|---|
| how_to | Yes, if confidence high | Optional | No side effect |
| bug_report | No | Yes | Create bug report after approval |
| billing | No | Yes | Refund after approval |
| account_access | No | Yes | Reset key after approval |
| security | No | Always escalate | No automatic action |
| other | No | Yes | No automatic action |

Risk rules should be code, not only prompt instructions.

## Data model sketch

```sql
CREATE TABLE tickets (
  id TEXT PRIMARY KEY,
  customer_id TEXT NOT NULL,
  subject TEXT NOT NULL,
  message TEXT NOT NULL,
  status TEXT NOT NULL,
  category TEXT,
  priority TEXT NOT NULL,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE draft_responses (
  id TEXT PRIMARY KEY,
  ticket_id TEXT NOT NULL,
  body TEXT NOT NULL,
  citations_json TEXT NOT NULL,
  risk_level TEXT NOT NULL,
  requires_approval INTEGER NOT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE approvals (
  id TEXT PRIMARY KEY,
  ticket_id TEXT NOT NULL,
  draft_response_id TEXT,
  reviewer_id TEXT NOT NULL,
  decision TEXT NOT NULL,
  reason TEXT,
  edited_body TEXT,
  created_at TEXT NOT NULL
);

CREATE TABLE audit_events (
  id TEXT PRIMARY KEY,
  ticket_id TEXT NOT NULL,
  actor_type TEXT NOT NULL,
  action TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  created_at TEXT NOT NULL
);
```

## API contract sketch

```http
POST /api/tickets
GET  /api/tickets?status=awaiting_approval
GET  /api/tickets/{id}
POST /api/tickets/{id}/triage
POST /api/tickets/{id}/draft
POST /api/tickets/{id}/approve
POST /api/tickets/{id}/reject
POST /api/tickets/{id}/escalate
GET  /api/tickets/{id}/audit
```

Approval request:

```json
{
  "draftResponseId": "draft_123",
  "decision": "approve",
  "editedBody": "Optional human-edited response",
  "approvedActions": [
    {
      "tool": "issue_refund",
      "arguments": {
        "customerId": "cus_123",
        "invoiceId": "inv_456",
        "amount": 25.00
      }
    }
  ]
}
```

## UI expectations

Minimum reviewer UI:

- Ticket list with status/category/priority.
- Ticket detail view.
- Original customer message.
- Agent classification and rationale.
- Retrieved source snippets.
- Draft response editor.
- Proposed actions panel.
- Approve/reject/escalate buttons.
- Audit timeline.

This can be plain but must be usable.

## Prompting guidance

Separate prompts by task.

### Classification prompt

```text
Classify the support ticket into exactly one category from the allowed list.
Return JSON with category, confidence, priority, and rationale.
Do not propose tool actions in this step.
```

### Draft prompt

```text
Draft a support response using only the retrieved knowledge base excerpts.
If the answer is not supported, say that a human will follow up.
Do not promise refunds, credits, security changes, or account actions.
Cite source ids in the reviewer metadata.
```

### Action proposal prompt

```text
Given the ticket, draft, and policy, propose zero or more actions.
Return only structured JSON matching the schema.
The application will validate and require human approval before execution.
```

## Tests

High-value tests:

1. FAQ ticket can be classified and drafted with a citation.
2. Billing ticket requires approval before refund action.
3. Security ticket is escalated and no tool action executes.
4. Rejected draft does not send a reply.
5. Approved edited draft sends the edited body, not the original.
6. Every state transition writes an audit event.
7. Tool arguments are validated before execution.
8. Prompt injection in KB article does not override system policy.

## Observability

Log structured events:

- `ticket.created`
- `ticket.classified`
- `retrieval.completed`
- `draft.created`
- `approval.decision_recorded`
- `tool.proposed`
- `tool.executed`
- `ticket.escalated`

Track metrics:

- Time to draft.
- Approval rate.
- Rejection/edit rate.
- Escalation rate.
- Retrieval no-hit rate.
- Tool proposal blocked rate.
- Model cost per ticket.

## Security considerations

- Provider keys stay server-side.
- Tool execution is allowlisted and schema-validated.
- Human approval is required for money, security, account access, or customer-visible irreversible actions.
- Audit logs are append-only in production.
- Customer data should not be logged raw in production.
- Retrieved KB content is untrusted and cannot change system policy.
- Access controls should ensure reviewers only see permitted tickets.

## Common mistakes

- Letting the model directly decide and execute refunds.
- No audit trail.
- No explicit state transitions.
- Treating human review as a README note instead of a real UI/API path.
- Hiding sources from the reviewer.
- Building a generic chatbot instead of a support workflow.
- No tests for blocked actions.
- No seed data, making the demo hard to run.

## Stretch goals

- LangGraph visualization of workflow states.
- Real-time updates with SSE or SignalR.
- Reviewer assignment and SLA timers.
- Feedback loop that stores reviewer edits for evaluation.
- Prompt/version registry.
- Red-team evals for prompt injection and unsafe action requests.
- Queue-backed background drafting.
- Role-based access control.

## Submission phrasing

> I modeled the support agent as an explicit workflow rather than an unconstrained autonomous chatbot. The LLM classifies tickets, retrieves policy context, and drafts responses, but deterministic policy code decides which actions need approval. Risky tools are schema-validated, allowlisted, and audited. The human reviewer can approve, edit, reject, or escalate. With more time I would add role-based reviewer assignment, prompt/version tracking, and production-grade append-only audit storage.
