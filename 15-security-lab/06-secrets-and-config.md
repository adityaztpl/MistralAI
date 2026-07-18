# 06 - Secrets and Configuration

Secrets fail quietly until they fail loudly. API keys, connection strings, signing keys, OAuth client secrets, webhook secrets, and model-provider keys must never be treated as ordinary configuration values.

This lab focuses on full-stack and GenAI mistakes: committing keys, putting provider keys in browser bundles, logging secrets, baking secrets into container images, and using long-lived credentials without rotation.

## Learning goals

By the end, you should be able to:

- Identify what counts as a secret.
- Explain why browser apps cannot keep secrets.
- Design local/dev/prod configuration boundaries.
- Use secret managers or managed identity.
- Rotate leaked credentials.
- Prevent secret leaks in logs, images, CI, and prompts.
- Add scanning and review checks.

## What counts as a secret?

| Secret | Examples |
| --- | --- |
| API keys | `MISTRAL_API_KEY`, OpenAI key, Stripe key, SendGrid key |
| Database credentials | connection strings, passwords, SAS tokens |
| Signing material | JWT signing keys, cookie data-protection keys |
| OAuth credentials | client secret, refresh token |
| Webhook secrets | HMAC verification keys |
| Cloud credentials | access key pairs, service account JSON |
| Private endpoints | internal URLs can be sensitive when paired with credentials |

Some values are not secret but still environment-specific, such as public API base URLs, feature flags, and log levels. Keep the distinction clear.

## Broken scenario

A developer wants the Instagram content creator SPA to call Mistral directly, so they add:

```ts
// DO NOT USE IN PROD.
export const MISTRAL_API_KEY = "paste-key-here";
```

The key is now in the JavaScript bundle. Any user can inspect it. Even if minified, the key is public. If committed, it may also live forever in Git history.

Another developer logs the full provider request for debugging, including the `Authorization` header and prompt text. Now keys and potentially sensitive prompt data are in centralized logs.

## High-level impact explanation

Secret leaks can cause:

- unauthorized model usage and cost spikes;
- data access through database or storage credentials;
- account takeover through OAuth refresh tokens;
- forged JWTs or cookies if signing keys leak;
- abuse of email/payment APIs;
- compliance incidents if sensitive prompt data is logged.

The defensive response is not "delete the line." Once a secret is exposed, assume it is compromised and rotate it.

## Safe configuration boundaries

| Environment | Recommended pattern |
| --- | --- |
| Local dev | `.env` excluded from Git; `.env.example` with placeholder names |
| CI | CI secret store; masked variables; least-privileged tokens |
| Staging/prod | Cloud secret manager, Key Vault, Secrets Manager, Vault, Kubernetes secrets with encryption, or managed identity |
| Browser | No secrets. Use public config only. Calls go through backend |
| Container image | No secrets baked into layers. Inject at runtime |

## GenAI-specific rules

- The browser should call your backend, not the model provider directly with a secret key.
- Backend should enforce auth, tenant limits, rate limits, and cost budgets before calling the provider.
- Do not log full prompts by default; prompts may contain user data, retrieved documents, or secrets pasted by users.
- Redact provider keys and auth headers from HTTP logs.
- Store eval datasets carefully if they contain customer examples.
- Use per-environment provider keys so staging leaks do not affect production.
- Use provider usage dashboards and alerts.

## ASP.NET configuration pattern

```csharp
public sealed class MistralOptions
{
    public const string SectionName = "Mistral";

    public required string ApiKey { get; init; }
    public string Model { get; init; } = "mistral-large-latest";
    public int MaxTokens { get; init; } = 4096;
}

builder.Services
    .AddOptions<MistralOptions>()
    .Bind(builder.Configuration.GetSection(MistralOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Mistral API key is required.")
    .ValidateOnStart();
```

`appsettings.json` should contain shape and non-secret defaults:

```json
{
  "Mistral": {
    "Model": "mistral-large-latest",
    "MaxTokens": 4096
  }
}
```

The secret value comes from environment variables or a secret provider:

```bash
Mistral__ApiKey=...
```

## Python configuration pattern

```python
from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    mistral_api_key: str = Field(min_length=20)
    mistral_model: str = "mistral-large-latest"
    max_tokens: int = 4096
```

Keep `.env.example` in source control:

```text
MISTRAL_API_KEY=replace-me
MISTRAL_MODEL=mistral-large-latest
```

Keep `.env` out of source control.

## Rotation playbook

If a secret leaks:

1. **Contain:** disable or restrict the leaked key if possible.
2. **Rotate:** create a new key and update runtime config.
3. **Deploy:** roll services so they use the new key.
4. **Revoke:** delete the old key.
5. **Investigate:** search logs, Git history, artifacts, issue trackers, and images.
6. **Monitor:** review provider usage and suspicious activity.
7. **Prevent:** add scanning, review rules, and least privilege.

Do not wait for proof of abuse.

## CI/CD controls

- Secret scanning on commits and pull requests.
- Dependency scanning for packages that may exfiltrate env vars.
- Masked logs for known secret patterns.
- No `set -x` around commands that echo env vars.
- Separate deploy credentials per environment.
- Short-lived OIDC federation where supported.
- Build images without secrets; inject secrets at runtime.
- SBOM and provenance for production artifacts.

## Logging rules

Never log:

- raw JWTs;
- API keys;
- passwords;
- OAuth refresh tokens;
- full connection strings;
- provider authorization headers;
- full prompts with sensitive user data;
- retrieved documents with regulated data unless explicitly approved.

Prefer structured safe logs:

```json
{
  "event": "llm_request_completed",
  "tenant_id": "tenant_123",
  "model": "mistral-large-latest",
  "input_tokens": 1200,
  "output_tokens": 300,
  "latency_ms": 1850,
  "request_id": "req_abc"
}
```

## Test and review checklist

- [ ] `.env` is ignored.
- [ ] `.env.example` contains placeholders only.
- [ ] Browser bundle has no provider keys.
- [ ] Backend validates required secrets at startup.
- [ ] Secrets are not written to logs.
- [ ] CI masks secret-like values.
- [ ] Container image layers do not contain `.env`.
- [ ] Production uses a secret manager or managed identity.
- [ ] Provider keys are scoped by environment.
- [ ] Rotation runbook exists and has an owner.
- [ ] Usage/cost alerts exist for model providers.

## Interview answer framework

1. **Bug:** "A secret was stored in source/client/logs/image instead of a secret boundary."
2. **Impact:** "Anyone with access could use the credential, causing data exposure or cost."
3. **Fix:** "Move secrets server-side, inject at runtime from a secret manager, rotate leaked values, and scan."
4. **Frontend point:** "A browser app cannot keep a provider API key secret."
5. **Ops:** "Monitor usage, alert on anomalies, and practice rotation."

