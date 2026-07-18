# Debugging Playbook: JWT 401 Loop

## Symptoms

- User logs in successfully, then immediately gets redirected to login.
- SPA repeatedly calls `/refresh` or `/login` until the browser/network tab floods.
- API returns `401 Unauthorized` even though an `Authorization: Bearer` header is present.
- Refresh token succeeds but the next request still uses the old access token.
- Works locally but fails in staging or behind HTTPS/proxy.
- Cookies are present in browser storage but not sent to the API.

## Reproduce

1. Open a clean browser profile or incognito window.
2. Capture a HAR/network trace for login, protected API request, refresh request, and redirect.
3. Inspect response status, `WWW-Authenticate`, `Set-Cookie`, `Authorization`, `Origin`, and `Access-Control-Allow-Credentials` headers.
4. Decode the JWT header/payload locally without trusting it:

```bash
node -e "const t=process.argv[1].split('.')[1]; console.log(JSON.parse(Buffer.from(t,'base64url').toString()))" "$TOKEN"
```

5. Compare token claims to API validation config: issuer, audience, expiration, clock skew, signing key id.
6. Disable refresh logic temporarily to see the original failing request.

## Diagnose

### Token validity

Check:

- `iss` exactly matches configured issuer.
- `aud` exactly matches configured audience.
- `exp` is in the future and server clocks are synchronized.
- Signing key and `kid` match current key set.
- Algorithm is expected and not downgraded.
- Required claims/roles/scopes exist.

ASP.NET Core logging to enable:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.IdentityModel": "Information"
    }
  }
}
```

### Header/cookie transport

- Is the frontend sending `Authorization: Bearer <token>`?
- If using cookies, are `HttpOnly`, `Secure`, `SameSite`, domain, and path correct?
- Are cross-site cookies blocked because `SameSite=None; Secure` is missing?
- Is the API configured for credentials in CORS?
- Is a reverse proxy stripping `Authorization`?

### Refresh loop logic

- Does the interceptor retry the refresh endpoint itself?
- Are multiple failed requests triggering parallel refreshes?
- Is the new token stored before retrying the original request?
- Is there a guard to stop after one retry?
- Does logout clear all token storage?

Problematic pattern:

```ts
catchError(err => {
  if (err.status === 401) {
    return refreshToken().pipe(switchMap(() => http.request(req)));
  }
  return throwError(() => err);
});
```

This can loop if `refreshToken()` also returns 401 and is intercepted.

## Fix

### API validation fix

- Correct issuer/audience config.
- Update signing keys/JWKS cache.
- Use reasonable clock skew, not huge skew.
- Ensure auth middleware order is correct:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### SPA interceptor fix

- Do not intercept login/refresh endpoints for refresh retries.
- Add a single-flight refresh lock.
- Mark retried requests to avoid infinite retry.
- On refresh failure, clear auth state and redirect once.

Angular-style sketch:

```ts
if (error.status === 401 && !isAuthEndpoint(req) && !req.headers.has('x-auth-retried')) {
  return auth.refreshOnce().pipe(
    switchMap(token => next.handle(req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
        'x-auth-retried': 'true'
      }
    }))),
    catchError(refreshError => {
      auth.logout();
      return throwError(() => refreshError);
    })
  );
}
```

### Cookie fix

- Cross-site SPA/API cookie auth needs:
  - `SameSite=None`
  - `Secure=true`
  - API CORS `AllowCredentials()`
  - frontend request `credentials: 'include'` or Angular `withCredentials: true`
- Use HTTPS locally if testing `Secure` cookies.

## Prevention

- Add integration tests for expired token, invalid audience, missing role, and refresh failure.
- Add frontend tests for "refresh called once" and "redirect once".
- Log auth failures with correlation IDs, not raw tokens.
- Monitor 401 rate by endpoint and deployment version.
- Document token lifetime and refresh behavior.
- Add a runbook entry for rotating signing keys.

## Interview phrasing

> I would capture the network sequence first so I can distinguish a bad token from a bad refresh loop. Then I would decode the JWT claims and compare issuer, audience, expiration, and key id against API validation. On the client I would check that the interceptor does not retry auth endpoints and that refresh is single-flight with a one-retry guard. The prevention is auth integration tests plus monitoring for 401 spikes and refresh-loop behavior.
