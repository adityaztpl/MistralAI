# Debugging Playbook: CORS Preflight Failures

## Symptoms

- Browser blocks request with "No Access-Control-Allow-Origin header".
- API works from curl/Postman but fails from the SPA.
- `OPTIONS` request returns 404, 401, 403, or 500.
- Request with custom headers fails, while simple GET works.
- Cookies/auth headers are not included or rejected.
- Works locally but fails in deployed environment.

## Reproduce

1. Use browser Network tab and inspect the `OPTIONS` preflight request.
2. Record request headers:
   - `Origin`
   - `Access-Control-Request-Method`
   - `Access-Control-Request-Headers`
3. Record response status and CORS headers.
4. Compare frontend origin exactly, including scheme, host, and port.
5. Reproduce with curl:

```bash
curl -i -X OPTIONS https://api.example.com/api/notes \
  -H "Origin: https://app.example.com" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: authorization,content-type"
```

## Diagnose

### Browser model

CORS is enforced by browsers, not by curl. A successful API response can still be blocked if CORS headers are missing or invalid.

### Common causes

- Origin allowlist does not include exact frontend origin.
- Middleware order is wrong.
- Preflight hits authentication before CORS handling.
- `AllowCredentials` is combined with wildcard origin.
- Requested headers are not allowed.
- Reverse proxy handles `OPTIONS` differently than API.
- Redirect from HTTP to HTTPS breaks preflight.

ASP.NET Core middleware order:

```csharp
app.UseRouting();
app.UseCors("SpaCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

Policy sketch:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCors", policy =>
    {
        policy.WithOrigins("https://app.example.com", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

### Credentialed requests

If cookies or `Authorization` are used:

- Response cannot use `Access-Control-Allow-Origin: *`.
- Response needs `Access-Control-Allow-Credentials: true`.
- Frontend must send credentials intentionally.
- Cookies need compatible `SameSite`/`Secure` settings.

## Fix

- Add exact frontend origins to API config.
- Put CORS middleware before auth/authorization endpoints reject preflight.
- Allow required headers and methods.
- Return successful `OPTIONS` responses.
- Avoid wildcard origins with credentials.
- Configure proxy/CDN to pass `OPTIONS` to the API or handle CORS consistently.
- Remove preflight-triggering custom headers if not needed.

## Prevention

- Store allowed origins in environment-specific config.
- Add a deployment smoke test for `OPTIONS` with real frontend origin.
- Log rejected origins at info/debug level.
- Document local dev ports.
- Keep CORS policy narrow but explicit.
- Review auth middleware order during API setup.

## Interview phrasing

> I would inspect the browser's preflight request, not just the failing POST. The key evidence is the exact Origin, requested method/headers, response status, and CORS response headers. In ASP.NET Core I would check middleware order so CORS runs before auth blocks OPTIONS. The fix is an explicit origin allowlist with credentials configured correctly, and the prevention is an OPTIONS smoke test in each environment.
