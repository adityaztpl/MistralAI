# 01 - Broken JWT Validation

JWTs are compact signed claims. They are safe only when the server validates the signature and the security-relevant claims every time the token is used.

This lab shows a common interview-quality failure: the API decodes a token, reads `sub`, `role`, or `tenant_id`, and treats those claims as truth without validating issuer, audience, signing key, expiration, and algorithm.

## Learning goals

By the end, you should be able to:

- Explain the difference between decoding a JWT and validating a JWT.
- Identify unsafe token parsing patterns in C#.
- Describe why algorithm confusion and disabled validation are severe.
- Configure ASP.NET Core JWT bearer validation.
- Add explicit authorization checks after authentication.
- Design tests for expired, wrong-audience, wrong-issuer, tampered, and missing-claim tokens.

## Broken scenario

A team builds a minimal API facade. The SPA sends `Authorization: Bearer <token>`. The API needs a user id and role, so the developer writes helper code that:

- strips `Bearer`;
- calls `JwtSecurityTokenHandler.ReadJwtToken`;
- extracts claims;
- builds a `ClaimsPrincipal`;
- checks whether `role == "Admin"` for privileged routes.

That helper appears to work in happy-path demos because valid tokens decode successfully. The mistake is that decoding is not validation.

Open the intentionally unsafe sample:

- [`broken-jwt/VulnerableAuth.cs`](broken-jwt/VulnerableAuth.cs)

> The sample is clearly labeled `DO NOT USE IN PROD`. Its purpose is to make the smell obvious in code review.

## High-level exploit explanation

An attacker does not need to break modern cryptography if the API never asks cryptography a question.

At a high level, the unsafe flow is:

1. The API accepts a string from the `Authorization` header.
2. The helper decodes the token payload as JSON-like claim data.
3. The helper trusts those claims.
4. Authorization decisions use attacker-controlled values.

If signature validation, issuer validation, audience validation, lifetime validation, and algorithm restrictions are disabled or bypassed, then a token-shaped string can become a caller identity. That can lead to:

- privilege escalation by claiming a stronger role;
- tenant breakout by claiming another `tenant_id`;
- session extension by using an expired token;
- accepting tokens minted for a different API;
- accepting tokens from a different identity provider.

This lab does not provide exploit payloads. The defensive lesson is enough: claim values are untrusted until validated with the expected issuer metadata and signing keys.

## Broken-code review checklist

Look for these smells:

- `ReadJwtToken(...)` used for authentication.
- `ValidateIssuerSigningKey = false`.
- `ValidateIssuer = false` or empty issuer list.
- `ValidateAudience = false` or accepting every audience.
- `ValidateLifetime = false`.
- `RequireSignedTokens = false`.
- `RequireExpirationTime = false`.
- No `ClockSkew` policy.
- No algorithm allowlist.
- Role checks using raw claim strings from an unvalidated token.
- Catch-all exception handlers that fall back to anonymous or admin behavior.

## Hardened design

Open the hardened sample:

- [`broken-jwt/HardenedAuth.cs`](broken-jwt/HardenedAuth.cs)

The hardened pattern:

1. Uses ASP.NET Core authentication middleware.
2. Configures `JwtBearerOptions.TokenValidationParameters`.
3. Requires signed tokens.
4. Validates issuer and audience.
5. Validates token lifetime.
6. Restricts acceptable algorithms.
7. Maps claims consistently.
8. Uses policy-based authorization for role and tenant requirements.

## Minimum validation requirements

| Control | Why it matters |
| --- | --- |
| Validate signature | Proves the token was issued by a trusted authority and not modified |
| Validate issuer | Prevents accepting tokens from an untrusted identity provider |
| Validate audience | Prevents accepting tokens intended for another API/client |
| Validate lifetime | Prevents use of expired tokens |
| Require expiration | Avoids never-expiring bearer credentials |
| Restrict algorithms | Prevents accepting unexpected weak or incompatible algorithms |
| Validate key id through metadata/JWKS | Supports rotation without hard-coded stale keys |
| Check required claims | Ensures downstream code has stable identity, tenant, and authorization data |

## Claims are not permissions by themselves

Even a valid JWT is not a complete authorization decision.

Example:

- Token says `sub = user-123`.
- Token says `tenant_id = tenant-a`.
- Route is `GET /api/documents/doc-999`.

The API still must check whether `doc-999` belongs to `tenant-a` and whether `user-123` has access to that document. JWT validation establishes caller identity; object-level authorization protects resources.

## Test cases

Create tests that prove:

- missing token returns `401`;
- malformed token returns `401`;
- expired token returns `401`;
- token signed with wrong key returns `401`;
- token with wrong issuer returns `401`;
- token with wrong audience returns `401`;
- token missing `sub` or `tenant_id` returns `401` or `403` depending on policy;
- valid user token cannot call admin endpoint (`403`);
- valid admin token can call admin endpoint;
- valid user token cannot access another tenant's object (`403`/`404`).

## Interview answer framework

Use this structure:

1. **Bug:** "The code decoded JWT claims without validating the token."
2. **Impact:** "An attacker could influence identity, role, tenant, or expiry claims."
3. **Fix:** "Use authentication middleware with strict token validation parameters and policy-based authorization."
4. **Tests:** "Cover tampered, expired, wrong issuer/audience, and missing claim tokens."
5. **Operations:** "Monitor auth failures, key rotation, unusual role/tenant access, and token validation exceptions without logging raw tokens."

## Common follow-up questions

### Should the SPA validate JWTs?

The SPA may decode a token for display-only UX, such as showing a name. It must not be the authority for security. The API validates every protected request.

### Should roles live in JWTs?

Roles can live in JWTs for coarse-grained authorization if they are short-lived and issued by a trusted identity provider. Resource-level permissions often still require database checks because they change more frequently and depend on the target object.

### Should invalid auth return 401 or 403?

- `401 Unauthorized`: caller is missing or has invalid authentication.
- `403 Forbidden`: caller is authenticated but lacks permission.

For object access, some systems return `404` instead of `403` to avoid revealing object existence. Be consistent and document the decision.

