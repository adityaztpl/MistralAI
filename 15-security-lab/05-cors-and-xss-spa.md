# 05 - CORS and XSS in an SPA

Single-page apps are browser applications. Security depends on understanding what the browser enforces, what the API enforces, and which controls solve different problems.

This lab covers two often-confused topics:

- **CORS** controls whether browsers allow frontend JavaScript from one origin to read responses from another origin.
- **XSS** happens when untrusted content executes as script in the user's browser.

CORS does not prevent XSS. XSS does not get fixed by opening CORS. They address different boundaries.

## Learning goals

By the end, you should be able to:

- Explain same-origin policy and CORS preflight.
- Configure CORS narrowly for an ASP.NET API.
- Explain why wildcard CORS with credentials is unsafe.
- Identify dangerous SPA rendering sinks.
- Prevent reflected, stored, and DOM-based XSS.
- Add Content Security Policy as defense in depth.
- Explain cookie vs bearer-token trade-offs for SPAs.

## Broken scenario

A React or Angular SPA runs at:

```text
https://app.example.com
```

The API runs at:

```text
https://api.example.com
```

During development, someone sets:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Open", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});
```

Then auth starts using cookies, so they try to add credentials. Another developer also renders AI-generated markdown and user comments through raw HTML for nicer formatting.

The result is a browser trust-boundary mess: overly broad cross-origin access plus script injection risk.

## CORS mental model

The browser's same-origin policy prevents JavaScript from `https://evil.example` reading responses from `https://api.example.com` unless the API opts in with CORS headers.

CORS is not an authentication control. Non-browser clients can still call the API. The API must still validate auth and authorization.

### Safe CORS shape

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Spa", policy =>
        policy.WithOrigins(
                "https://app.example.com",
                "https://admin.example.com")
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Authorization", "Content-Type", "X-Correlation-Id")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
});
```

If using cookies:

```csharp
policy.WithOrigins("https://app.example.com")
      .AllowCredentials();
```

Do not combine credentials with a dynamic "reflect every origin" policy.

## CORS review checklist

- [ ] Production origin list is explicit.
- [ ] Development origin list is separate from production.
- [ ] No `AllowAnyOrigin` for authenticated APIs.
- [ ] No origin reflection without an allowlist.
- [ ] Credentials are used only with exact origins.
- [ ] Allowed headers and methods are narrow.
- [ ] API still enforces auth and authorization.
- [ ] Preflight failures are observable in logs or docs.

## XSS mental model

XSS occurs when untrusted data becomes executable script. Common sources:

- user profile fields;
- comments;
- document titles;
- markdown;
- AI-generated content;
- query parameters;
- URL fragments;
- third-party widgets;
- tool results.

Common dangerous sinks:

- React `dangerouslySetInnerHTML`;
- Angular `[innerHTML]` with bypassed sanitizer;
- direct `element.innerHTML = ...`;
- template strings inserted into the DOM;
- inline event handlers;
- unsafe markdown renderers;
- scriptable URLs such as `javascript:`.

## Broken SPA patterns

### React

```tsx
// DO NOT USE IN PROD.
export function GeneratedPreview({ html }: { html: string }) {
  return <div dangerouslySetInnerHTML={{ __html: html }} />;
}
```

### Angular

```ts
// DO NOT USE IN PROD.
this.preview = this.sanitizer.bypassSecurityTrustHtml(modelOutput);
```

Both examples treat untrusted content as trusted markup.

## Hardened rendering pattern

Prefer rendering text as text:

```tsx
export function SafeCaption({ caption }: { caption: string }) {
  return <p>{caption}</p>;
}
```

If markdown is a real requirement:

- use a well-maintained parser;
- disable raw HTML by default;
- sanitize with an allowlist;
- rewrite links to safe protocols;
- add `rel="noopener noreferrer"` for external links;
- test malicious markdown cases.

Example React shape:

```tsx
import Markdown from "react-markdown";
import rehypeSanitize from "rehype-sanitize";

export function SafeMarkdown({ markdown }: { markdown: string }) {
  return (
    <Markdown
      rehypePlugins={[rehypeSanitize]}
      components={{
        a: ({ href, children }) => {
          const safeHref = href?.startsWith("https://") ? href : undefined;
          return (
            <a href={safeHref} rel="noopener noreferrer" target="_blank">
              {children}
            </a>
          );
        },
      }}
    >
      {markdown}
    </Markdown>
  );
}
```

## Content Security Policy

CSP is defense in depth. It reduces the blast radius if markup injection slips through.

Example starting point:

```http
Content-Security-Policy:
  default-src 'self';
  script-src 'self';
  style-src 'self' 'unsafe-inline';
  img-src 'self' data: https:;
  connect-src 'self' https://api.example.com;
  frame-ancestors 'none';
  base-uri 'self';
  object-src 'none';
```

Tune this to your framework and hosting setup. Avoid using CSP as an excuse to keep unsafe rendering sinks.

## Cookie vs bearer-token SPA auth

| Approach | Benefit | Risk | Required controls |
| --- | --- | --- | --- |
| HttpOnly secure cookies | Token not readable by JS | CSRF if not protected | SameSite, CSRF token, narrow CORS credentials |
| Bearer token in memory | Simpler API auth | Lost on refresh; XSS can act as user | XSS prevention, refresh pattern |
| Bearer token in localStorage | Survives refresh | XSS can steal token | Avoid for high-risk apps |

For interview answers, emphasize trade-offs. There is no magic storage location that fixes XSS. If attacker-controlled script runs, the session is at risk.

## AI-specific XSS concerns

AI apps often render:

- generated markdown;
- citations with titles from documents;
- snippets from retrieved pages;
- tool outputs;
- user-uploaded content.

Treat all of these as untrusted. Even if the model generated the output, the model may have copied unsafe content from inputs.

## Test cases

- CORS allows production SPA origin.
- CORS denies unknown origin.
- Credentials are not allowed for wildcard origins.
- API auth fails without token/cookie even if CORS allows origin.
- Markdown renderer escapes raw HTML.
- Unsafe URL protocols are removed.
- Generated captions render as text, not HTML.
- CSP header exists on SPA host.
- Stored comment cannot execute script when viewed by another user.
- AI-generated output cannot inject event handlers.

## Interview answer framework

1. **CORS:** "CORS is a browser read-permission policy, not API authentication."
2. **XSS:** "XSS is untrusted content executing in the browser."
3. **Fix:** "Use exact CORS origins, avoid dangerous DOM sinks, sanitize markdown, and add CSP."
4. **Auth trade-off:** "Cookies need CSRF controls; bearer tokens need careful storage and XSS prevention."
5. **AI angle:** "Model output and retrieved snippets are untrusted UI input."

