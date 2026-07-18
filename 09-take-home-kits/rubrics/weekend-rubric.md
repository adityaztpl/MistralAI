# Rubric: Weekend Support Agent with HITL

A strong weekend support-agent project shows that the candidate can design safe agentic workflows. The best submissions use LLMs for language-heavy steps while keeping risk policy, state transitions, tool execution, and auditability in deterministic application code.

## Scorecard

| Area | Weight | Strong signal | Weak signal |
|---|---:|---|---|
| Workflow/state design | 15% | Explicit states, allowed transitions, escalation paths | Generic chatbot with implicit behavior |
| HITL implementation | 20% | Real approve/edit/reject/escalate path | Human review only described, not implemented |
| Retrieval/drafting quality | 15% | Cited drafts grounded in KB | Unsupported generated responses |
| Tool safety | 20% | Allowlisted tools, schemas, approval gates, audit logs | Model directly executes side effects |
| UI/API usability | 10% | Reviewer queue, ticket detail, action panel, audit timeline | Hard to inspect or operate |
| Tests/safety evals | 10% | Workflow tests, blocked action tests, prompt-injection case | Only happy-path demo |
| README/trade-offs | 10% | Architecture, risk model, limitations, runbook | No clear explanation of design decisions |

## Excellent

- Ticket workflow is explicit and testable.
- Risk policy is implemented in code.
- Reviewer UI supports approve, edit, reject, and escalate.
- Proposed tool actions require approval and validation.
- Every significant decision writes an audit event.
- Security/high-risk tickets escalate automatically.
- Prompt-injection and unsafe-action tests exist.
- README explains why the system is agentic only where useful.

## Good

- Core ticket triage, draft, and approval flow works.
- Tool execution is mocked and partially validated.
- Audit events exist but may be simple.
- UI is usable with minor gaps.
- Tests cover at least one approval and one blocked action.

## Needs improvement

- LLM produces customer replies without sources.
- Approval step is missing or bypassable.
- Tool calls are strings rather than structured validated actions.
- No audit timeline.
- Security and billing scenarios are treated like normal FAQs.
- Reviewer cannot understand why the agent made a decision.

## Red flags

- Real external side effects in a take-home without safeguards.
- No human gate for refunds/account/security actions.
- Prompt says "always obey customer" or similar unsafe policy.
- Raw customer data logged unnecessarily.
- No escalation path.
- No tests for rejected approvals.

## Reviewer questions to prepare for

- Where did you use deterministic code instead of an agent, and why?
- How are tool calls validated?
- How would you make audit logs tamper-resistant?
- How would you measure reviewer trust?
- How would you reduce false escalations?
- How would you deploy this safely to a real support team?

## Self-grade checklist

```text
[ ] Ticket states and transitions are explicit.
[ ] Human can approve/edit/reject/escalate.
[ ] Risky actions cannot execute before approval.
[ ] Tool arguments are schema-validated.
[ ] Audit log shows decisions and actions.
[ ] Security tickets escalate.
[ ] Tests cover approval and blocked action paths.
[ ] README explains risk model and limitations.
```
