# Security and license

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Understand trusted-code boundaries, secret handling, untrusted media, and Remotion licensing considerations.

## Mental model

Rendering executes React and browser code. That is powerful and dangerous when code, props, media URLs, or templates come from outside your trust boundary. Security design should decide who may submit code, who may submit data, where secrets live, and how outputs are licensed and retained.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Treat composition code as trusted application code unless you have a sandbox strategy.
2. Treat user props and uploaded media as untrusted input.
3. Validate props with schemas and enforce size, URL, duration, and file type limits.
4. Keep cloud credentials and license configuration on the server side.
5. Use signed URLs or controlled asset proxying for private media.
6. Review Remotion license requirements for the organization and product use case.
7. Record attribution and third-party asset licenses for templates, fonts, music, and footage.


## Example


```ts
const renderRequestSchema = z.object({
  compositionId: z.enum(['ProductPromo', 'CaptionedClip']),
  inputProps: z.record(z.unknown()),
  assetUrls: z.array(z.string().url()).max(20),
});

export function authorizeRender(user: User, request: unknown) {
  const parsed = renderRequestSchema.parse(request);
  if (!user.canRender(parsed.compositionId)) {
    throw new Error('not authorized');
  }
  return parsed;
}
```

```text
Security boundary
- Browser client: can preview public props, cannot access cloud render credentials.
- App API: validates input, creates jobs, signs upload/download URLs.
- Render worker: runs trusted composition code with minimal required secrets.
- Object storage: stores artifacts with lifecycle and access controls.
```


## Checklist


- Render credentials never ship to browsers.
- User-controlled URLs are allowlisted, proxied, or scanned.
- Props are schema-validated and size-limited.
- Generated artifacts have retention policies.
- License obligations are reviewed before commercial deployment.


## Pitfalls


- Allowing arbitrary user React code is remote code execution unless sandboxed.
- Rendering private URLs from cloud workers can accidentally create SSRF-style access.
- Long-lived public artifact URLs can leak customer content.
- Ignoring license terms until launch can create procurement or compliance delays.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Security: <https://www.remotion.dev/docs/security>
- License: <https://www.remotion.dev/docs/license>
- License pricing: <https://www.remotion.dev/docs/license/pricing>
