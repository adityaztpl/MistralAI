# 07 - Security Lab Checklist

Use this checklist after completing the security labs. It turns the lessons into code review prompts, test ideas, and interview drills.

## Universal review questions

Ask these on every feature:

- Who is the caller?
- How is identity authenticated?
- Which tenant, user, or organization scope applies?
- What object is being accessed?
- Is authorization checked on the server for that exact object and action?
- Which inputs are untrusted?
- Which outputs are rendered in a browser?
- Which actions cause side effects?
- Which secrets are required?
- What is logged?
- What happens when validation fails?
- What monitoring would reveal abuse?

## Authentication checklist

- [ ] JWTs are validated, not merely decoded.
- [ ] Issuer is validated.
- [ ] Audience is validated.
- [ ] Signature is validated with trusted keys.
- [ ] Lifetime and expiration are validated.
- [ ] Algorithms are restricted to expected values.
- [ ] Required claims are enforced.
- [ ] Raw tokens are not logged.
- [ ] Auth failures return `401`.
- [ ] Key rotation path is understood.

## Authorization checklist

- [ ] Authentication and authorization are discussed separately.
- [ ] Tenant id comes from validated caller context, not request body.
- [ ] Object queries include tenant/user/permission constraints.
- [ ] Read, update, delete, download, export, and job-status endpoints all authorize.
- [ ] Bulk list endpoints filter before pagination.
- [ ] Admin bypasses are explicit policies and audited.
- [ ] Cross-tenant access tests exist.
- [ ] Denied attempts produce safe audit logs.
- [ ] `403` vs `404` policy is documented.

## Prompt-injection checklist

- [ ] Retrieved content is labeled as untrusted evidence.
- [ ] Tool outputs are treated as untrusted text.
- [ ] Prompts tell the model not to follow instructions inside evidence.
- [ ] Sensitive instructions are not exposed in normal responses.
- [ ] Model output is validated against a schema.
- [ ] Evals include malicious retrieved documents.
- [ ] Prompt logs are redacted or sampled safely.
- [ ] The app has refusal behavior for instruction conflicts.

## Tool-calling checklist

- [ ] Tool names are allowlisted.
- [ ] Tool arguments are schema-validated.
- [ ] Model cannot provide caller id, tenant id, roles, or secrets.
- [ ] Tool execution is bound to authenticated caller context.
- [ ] High-impact tools require human confirmation.
- [ ] Dangerous broad tools are avoided.
- [ ] Tool loops have max steps and budgets.
- [ ] Every tool decision is audited.
- [ ] Replays and stale confirmations are rejected.
- [ ] Tool failures are safe and observable.

## Browser security checklist

- [ ] Production CORS origins are exact.
- [ ] Credentialed CORS does not use wildcard origins.
- [ ] API auth still protects endpoints regardless of CORS.
- [ ] React `dangerouslySetInnerHTML` is avoided or tightly sanitized.
- [ ] Angular sanitizer bypasses are not used for untrusted content.
- [ ] Markdown rendering disables or sanitizes raw HTML.
- [ ] Links allow only safe protocols.
- [ ] CSP is configured.
- [ ] Stored user/AI content has XSS tests.
- [ ] Browser token storage trade-offs are documented.

## Secrets checklist

- [ ] No secrets in source code.
- [ ] No secrets in browser bundles.
- [ ] No secrets in logs.
- [ ] No secrets in container image layers.
- [ ] `.env` is ignored.
- [ ] `.env.example` uses placeholders.
- [ ] Production secrets come from a secret manager or managed identity.
- [ ] Provider keys are environment-scoped.
- [ ] Startup validates required config.
- [ ] Rotation runbook exists.
- [ ] Usage and cost alerts are configured.

## Interview drills

Practice these as 2-minute answers:

1. "A JWT is present in the request. What still needs to happen?"
2. "How can an authenticated user still access another user's document?"
3. "Why is prompt injection hard to eliminate completely?"
4. "Where should AI tool authorization live?"
5. "What is CORS and what does it not protect against?"
6. "How do you safely render AI-generated markdown?"
7. "Why can't a React app keep a Mistral API key secret?"
8. "A secret was committed yesterday. What is your incident response?"
9. "How do you test tenant isolation?"
10. "How do you monitor an AI app for unsafe tool use?"

## Whiteboard exercise: secure AI document assistant

Design an app with:

- React or Angular SPA.
- ASP.NET Core API.
- JWT auth.
- Tenant-scoped documents.
- RAG over uploaded docs.
- Tool that creates a support ticket draft.
- Human approval before sending the ticket.
- Mistral/OpenAI provider key stored server-side.

Your diagram should show:

- browser origin;
- API origin and CORS policy;
- auth provider;
- database;
- vector store;
- LLM provider;
- tool gateway;
- audit logs;
- secret manager.

## Done criteria

You are ready to move on when you can:

- [ ] Review the vulnerable samples and identify the exact trust-boundary failure.
- [ ] Explain each hardened sample without reading it line by line.
- [ ] Write at least three tests per lab.
- [ ] Give a concise incident response for leaked secrets.
- [ ] Design an AI tool gateway with confirmation.
- [ ] Explain why model output is untrusted.
- [ ] Explain security trade-offs in terms of impact and usability.

