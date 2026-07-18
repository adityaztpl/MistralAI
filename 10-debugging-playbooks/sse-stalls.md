# Debugging Playbook: Server-Sent Events Stall

## Symptoms

- Chat response starts but stops mid-answer.
- Tokens arrive all at once after a long delay instead of streaming.
- Works locally but stalls behind Nginx, Azure App Service, Cloudflare, or another proxy.
- Browser `EventSource` reconnects repeatedly.
- Server logs show generation completed, but client never receives `done`.
- Long responses fail around a consistent timeout.

## Reproduce

1. Test streaming with `curl -N` to bypass frontend parsing:

```bash
curl -N -H "Accept: text/event-stream" http://localhost:5000/api/chat/stream
```

2. Compare local direct API vs deployed route through proxy/CDN.
3. Inspect response headers:
   - `Content-Type: text/event-stream`
   - `Cache-Control: no-cache`
   - `Connection: keep-alive` where applicable
   - proxy buffering headers such as `X-Accel-Buffering: no`
4. Add server logs for first byte, each flush, heartbeat, cancellation, and done event.
5. Check browser Network timing and EventStream preview.

## Diagnose

### Server flush behavior

- Is the response body flushed after each event?
- Is the framework buffering JSON serialization?
- Is compression buffering small chunks?
- Is the action returning before streaming completes?

ASP.NET Core sketch:

```csharp
context.Response.Headers.ContentType = "text/event-stream";
context.Response.Headers.CacheControl = "no-cache";

await foreach (var token in chat.StreamAsync(request, cancellationToken))
{
    await context.Response.WriteAsync($"event: token\ndata: {JsonSerializer.Serialize(new { text = token })}\n\n", cancellationToken);
    await context.Response.Body.FlushAsync(cancellationToken);
}
```

### Proxy buffering/timeouts

- Nginx may buffer unless disabled.
- Some platforms timeout idle connections without heartbeats.
- HTTP/2/proxy settings can affect flush timing.
- CDN/proxy may not support long-lived responses on a route.

Nginx hints:

```nginx
proxy_buffering off;
proxy_cache off;
proxy_read_timeout 300s;
add_header X-Accel-Buffering no;
```

### Client parsing

- `EventSource` only supports GET; POST streaming needs `fetch` and stream parser.
- Missing blank line between SSE events prevents dispatch.
- JSON data must be serialized safely on one or multiple `data:` lines.
- Client may abort on route changes or component unmount.

Correct event format:

```text
event: token
data: {"text":"hello"}

```

The final blank line matters.

### Cancellation and provider stream

- Is cancellation token passed to the LLM/provider stream?
- Are provider chunks converted into SSE events promptly?
- Are exceptions sent as an `error` event before closing?

## Fix

- Set correct SSE headers.
- Flush after each event.
- Send heartbeats every 10-20 seconds for long pauses:

```text
: heartbeat

```

- Disable response compression for SSE route if buffering occurs.
- Disable proxy buffering and increase idle timeouts.
- Use GET `EventSource` or implement a robust `fetch` stream parser for POST.
- Send explicit `done` and `error` events.
- Propagate cancellation from client disconnect to provider call.

## Prevention

- Add an integration test that asserts multiple chunks arrive before completion.
- Run a deployed smoke test with `curl -N` through the public URL.
- Add metrics: time to first byte, tokens per second, stream duration, disconnect reason.
- Document proxy requirements in deployment docs.
- Keep streaming endpoints out of generic JSON response middleware that buffers.
- Alert on rising stream disconnects or missing done events.

## Interview phrasing

> I would first determine whether the stall is server-side, proxy-side, or client-side by testing with `curl -N` directly against the API and then through the deployed route. I would verify SSE formatting, headers, flush calls, proxy buffering, and idle timeouts. The fix is usually explicit flushing plus disabling buffering and adding heartbeats. I would prevent regressions with a deployed streaming smoke test and metrics for time to first byte and disconnects.
