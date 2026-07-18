# 15-security-lab: Sandbox Security Teaching Lab

This section is a deliberately sandbox-oriented security lab for full-stack and GenAI interview preparation. Each lab moves through the same sequence:

1. **Broken implementation** - inspect code that looks plausible but is unsafe.
2. **Exploit explanation** - reason about the failure mode at a high level, without weaponized payloads or operational guidance.
3. **Hardened implementation** - replace the unsafe pattern with production-grade controls.
4. **Interview checklist** - practice explaining the risk, mitigation, trade-offs, and tests.

The goal is to learn how to recognize insecure designs and defend systems. These examples are not penetration-test playbooks.

## Safety rules

Follow these rules while studying or teaching this lab:

- Run examples only in a local disposable sandbox.
- Do not point examples at real users, real tenants, real documents, real credentials, or public services.
- Treat all "vulnerable" samples as intentionally unsafe teaching artifacts.
- Do not copy vulnerable snippets into production projects.
- Keep exploit discussion at the conceptual level: what trust boundary failed, what an attacker could gain, and which control prevents it.
- Prefer unit tests and code review exercises over live exploitation.
- If you find a similar issue in a real system, use your organization's responsible disclosure and incident process.

## Lab order

| Order | Lab | Core failure | Hardened habit |
| --- | --- | --- | --- |
| 1 | [Broken JWT](01-broken-jwt.md) | Trusting unsigned/weakly validated tokens | Validate issuer, audience, signature, lifetime, algorithm, and claims |
| 2 | [IDOR on documents](02-idor-on-documents.md) | Authorizing by object ID only | Enforce object-level authorization on every read/write |
| 3 | [Prompt injection](03-prompt-injection.md) | Treating retrieved/user text as instructions | Separate instructions from data, constrain tools, verify outputs |
| 4 | [Insecure tool calling](04-insecure-tool-calling.md) | Letting the model execute arbitrary actions | Use an allowlisted tool gateway with policy and human approval |
| 5 | [CORS and XSS in an SPA](05-cors-and-xss-spa.md) | Browser trust boundaries misunderstood | Narrow CORS, encode output, avoid dangerous sinks, use CSP |
| 6 | [Secrets and config](06-secrets-and-config.md) | Secrets in source, logs, clients, or images | Centralize secret management and rotation |
| 7 | [Lab checklist](07-lab-checklist.md) | Missing proof that controls work | Turn lessons into review, test, and interview checklists |

## How to run the lab

Use this as a code-reading and design-review exercise:

1. Read the guide first.
2. Open the vulnerable sample and mark each trust boundary.
3. Write down the failure in one sentence.
4. Compare against the hardened sample.
5. Design tests that prove the hardened version blocks the original failure.
6. Practice a 2-minute interview answer:
   - "What was the bug?"
   - "How could it be abused?"
   - "How did you fix it?"
   - "How would you monitor for it?"

## Threat-model template

Use this template for every lab:

| Question | Notes |
| --- | --- |
| Asset | What data, capability, or user action matters? |
| Actor | Anonymous user, authenticated user, tenant admin, compromised dependency, model output? |
| Entry point | HTTP endpoint, prompt field, retrieval corpus, tool call, config file, build pipeline? |
| Trust boundary | Where does untrusted data become trusted? |
| Broken assumption | What did the code assume that is not guaranteed? |
| Impact | Confidentiality, integrity, availability, cost, compliance, brand harm? |
| Preventive control | Validation, authorization, escaping, sandboxing, allowlist, secret manager? |
| Detective control | Logs, alerts, audit events, anomaly detection, eval failures? |
| Test | Unit, integration, policy, red-team, static scan, dependency scan? |

## Production hardening themes

Across all labs, the safe designs repeat the same themes:

- **Authenticate identity, authorize actions.** Authentication says who the caller is; authorization decides what this identity can do to this object now.
- **Validate tokens server-side.** Never trust client-provided identity without cryptographic and claim validation.
- **Treat model output as untrusted.** LLMs produce suggestions, not authority. Policy must live in deterministic code.
- **Treat retrieved text as data.** RAG context may contain hostile or stale instructions.
- **Keep secrets out of source and clients.** Browser apps and mobile apps cannot keep provider keys secret.
- **Default deny.** CORS origins, tools, tenant scopes, file paths, and outbound calls should be allowlisted.
- **Log safely.** Audit security decisions without writing secrets, raw tokens, or sensitive prompt payloads into logs.

## Interview prompts

Practice answering:

- Explain JWT validation beyond "check the token exists."
- How do you prevent IDOR in a document API?
- Why is prompt injection different from SQL injection and why is it still serious?
- Where should tool authorization live in an AI agent system?
- Why is `Access-Control-Allow-Origin: *` dangerous with credentials?
- What is the difference between XSS prevention and CORS?
- How do you rotate a leaked secret?
- Which security checks belong in CI/CD?

