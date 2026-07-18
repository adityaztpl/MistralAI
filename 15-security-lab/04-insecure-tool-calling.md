# 04 - Insecure Tool Calling

Tool calling lets a model ask application code to do work: search, read files, create tickets, send emails, run jobs, update records, or call APIs. The security mistake is letting the model become the authorization layer.

The model can propose an action. It must not be the component that decides whether the action is allowed.

## Learning goals

By the end, you should be able to:

- Explain why LLM tool calls need a server-side gateway.
- Classify tools by impact and approval requirement.
- Validate tool names and arguments with schemas.
- Bind tool execution to the authenticated caller and tenant.
- Add idempotency, audit logs, and rate limits.
- Design a human-confirmation flow for high-impact tools.

## Broken scenario

An AI operations assistant receives a user message and has a tool named `run_command`. The developer exposes:

```text
run_command(command: string)
http_request(method: string, url: string, body: string)
database_query(sql: string)
send_email(to: string, subject: string, body: string)
```

The app forwards whichever tool call the model emits. That is equivalent to giving untrusted generated text a remote-control handle into production.

## High-level exploit explanation

At a high level, an insecure tool-calling design can fail when:

1. User or retrieved content manipulates the model into choosing a risky tool.
2. The model emits broad arguments, such as a raw URL, shell command, SQL statement, or recipient address.
3. Application code executes the tool without policy checks.
4. The side effect happens under the application's privileges, not the attacker's limited privileges.

Potential impacts:

- data exfiltration through outbound HTTP;
- unauthorized email or ticket creation;
- destructive admin operations;
- cross-tenant reads or writes;
- cost spikes from loops or expensive tools;
- SSRF if URL tools can reach internal metadata or services;
- compliance violations through unreviewed external communication.

This lab avoids operational exploit payloads. The defensive lesson is to replace broad execution tools with narrow, allowlisted, policy-checked capabilities.

## Tool risk tiers

| Tier | Examples | Default control |
| --- | --- | --- |
| Read-only public | Search public docs, get weather | Allow with schema, rate limit, logging |
| Read-only private | Read customer profile, retrieve tenant docs | Require caller/tenant authorization |
| Low-impact write | Save draft, create internal note | Authorize and audit |
| High-impact write | Send email, issue refund, delete record | Authorize, validate, require confirmation |
| Dangerous execution | Shell, raw SQL, arbitrary HTTP | Avoid; replace with narrow tools |

## Hardened tool gateway snippet

This Python snippet shows the shape of a deterministic gateway. It is intentionally framework-light so you can adapt it to LangChain, LangGraph, Semantic Kernel, CrewAI, or custom orchestration.

```python
from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum
from typing import Any, Callable


class ToolImpact(StrEnum):
    READ = "read"
    LOW_WRITE = "low_write"
    HIGH_WRITE = "high_write"


@dataclass(frozen=True)
class Caller:
    user_id: str
    tenant_id: str
    roles: set[str]


@dataclass(frozen=True)
class ToolSpec:
    name: str
    impact: ToolImpact
    allowed_roles: set[str]
    required_fields: set[str]
    handler: Callable[[Caller, dict[str, Any]], dict[str, Any]]


class ToolGateway:
    def __init__(self, tools: list[ToolSpec]) -> None:
        self._tools = {tool.name: tool for tool in tools}

    def execute(
        self,
        *,
        caller: Caller,
        tool_name: str,
        arguments: dict[str, Any],
        confirmation_id: str | None = None,
    ) -> dict[str, Any]:
        tool = self._tools.get(tool_name)
        if tool is None:
            return {"status": "denied", "reason": "unknown_tool"}

        if caller.roles.isdisjoint(tool.allowed_roles):
            return {"status": "denied", "reason": "role_not_allowed"}

        missing = tool.required_fields - set(arguments)
        if missing:
            return {"status": "denied", "reason": f"missing_fields:{sorted(missing)}"}

        # Example tenant binding: the model cannot choose tenant_id.
        arguments = {**arguments, "tenant_id": caller.tenant_id}

        if tool.impact == ToolImpact.HIGH_WRITE and confirmation_id is None:
            pending_id = save_pending_action(caller, tool.name, arguments)
            return {
                "status": "needs_confirmation",
                "pending_action_id": pending_id,
            }

        audit_tool_decision(caller, tool.name, tool.impact, "approved")
        return {
            "status": "ok",
            "result": tool.handler(caller, arguments),
        }


def save_pending_action(caller: Caller, tool_name: str, args: dict[str, Any]) -> str:
    # Persist caller, tenant, tool, args hash, expiry, and display summary.
    return f"pending:{caller.tenant_id}:{caller.user_id}:{tool_name}"


def audit_tool_decision(
    caller: Caller,
    tool_name: str,
    impact: ToolImpact,
    decision: str,
) -> None:
    # Real implementation writes a structured audit event without secrets.
    print(
        {
            "user_id": caller.user_id,
            "tenant_id": caller.tenant_id,
            "tool": tool_name,
            "impact": impact,
            "decision": decision,
        }
    )
```

## Design rules

### 1. Do not expose raw execution tools

Avoid tools like:

- `run_shell(command)`;
- `execute_sql(sql)`;
- `fetch_url(url)`;
- `call_api(method, url, headers, body)`.

Prefer narrow tools:

- `create_support_ticket(customer_id, summary, severity)`;
- `get_order_status(order_id)`;
- `draft_refund(customer_id, order_id, amount_cents, reason)`;
- `search_policy_documents(query, tenant_id)`.

### 2. Bind to caller context

The gateway receives `caller` from the authenticated request. The model does not provide:

- user id;
- tenant id;
- roles;
- approval state;
- secret API keys;
- internal base URLs.

### 3. Validate arguments

Validate:

- required fields;
- types;
- enum values;
- length limits;
- numeric limits;
- tenant ownership;
- target object existence;
- idempotency key;
- policy-specific constraints.

### 4. Add confirmation for high-impact actions

High-impact tool calls should produce a pending action:

1. Model proposes action.
2. Gateway validates it.
3. User sees a human-readable summary.
4. User confirms.
5. Gateway revalidates and executes.
6. Audit log records the final decision.

### 5. Keep secrets out of tool arguments

Tool handlers should obtain credentials from server-side secret stores or managed identity. The model should never see or choose provider keys, database passwords, or service tokens.

## Test cases

- Unknown tool is denied.
- Tool name with casing/spacing tricks is denied unless exactly allowlisted.
- Missing required argument is denied.
- Extra unrecognized argument is ignored or denied by policy.
- Caller without role cannot execute tool.
- Model-provided tenant id is ignored.
- Cross-tenant object id is denied.
- High-impact write requires confirmation.
- Confirmation id cannot be replayed by another user.
- Tool loop stops at configured max steps.
- Outbound URL tools, if any, reject private IPs and non-allowlisted hosts.

## Interview answer framework

1. **Bug:** "The model could choose and execute tools without deterministic authorization."
2. **Impact:** "Prompt injection or model error could cause unauthorized reads, writes, emails, refunds, or internal calls."
3. **Fix:** "Use a tool gateway with allowlisted tools, schemas, caller-bound policy, audit logs, limits, and confirmation."
4. **Trade-off:** "More guardrails reduce autonomy but make side effects explainable and reviewable."
5. **Tests:** "Exercise unknown tools, invalid args, role denial, tenant denial, and confirmation replay."

