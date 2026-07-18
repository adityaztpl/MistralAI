# S3 artifacts and webhooks

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Persist outputs safely, expose signed URLs, verify webhook signatures, and keep artifact lifecycle predictable.

## Core idea

The final render is usually not served directly from the render worker. It is written to object storage, associated with metadata, and surfaced to the application through a signed URL or copied into a product-owned bucket. Webhooks let cloud rendering notify the app, but they must be verified and idempotent.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Treat the provider output path as an internal artifact location.
2. Copy or tag completed artifacts with tenant id, job id, composition id, version, and expiration policy.
3. Generate signed URLs for user downloads instead of making every object public.
4. Validate webhook signatures before updating job state.
5. Make webhook handlers idempotent because delivery can be duplicated.
6. Store final metadata: codec, width, height, duration, bytes, checksum if available, and creation time.
7. Add lifecycle policies for drafts, failed partials, and expired exports.


## Practical example


```ts
import {validateWebhookSignature} from '@remotion/lambda/client';

export async function handleRemotionWebhook(req: Request) {
  const body = await req.text();
  const signature = req.headers.get('x-remotion-signature');

  validateWebhookSignature({
    body,
    signatureHeader: signature,
    secret: process.env.REMOTION_WEBHOOK_SECRET!,
  });

  const event = JSON.parse(body);
  await upsertRenderEvent(event.renderId, event);
  return new Response('ok');
}
```

```text
Artifact metadata
- tenant_id
- render_job_id
- composition_id
- git_sha or site_version
- codec and container
- dimensions and fps
- duration frames and seconds
- source input hash
- retention class: draft, final, archived
```


## Operational checklist


- No bucket is made public by accident.
- Signed URLs expire according to product requirements.
- Webhook secrets are stored in secret management, not source control.
- Duplicate webhook deliveries do not regress job status.
- Storage lifecycle rules remove abandoned outputs and partial renders.


## Risks and trade-offs


- Trusting unsigned webhooks lets attackers mark renders complete or failed.
- Returning raw S3 paths can leak bucket structure or enable accidental public coupling.
- Never delete artifacts immediately after generating a URL unless the URL references a copied stable object.
- Long-lived signed URLs can become data leakage if shared outside the product.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Download media: <https://www.remotion.dev/docs/lambda/downloadmedia>
- Validate webhook signature: <https://www.remotion.dev/docs/lambda/validatewebhooksignature>
- App Router webhook: <https://www.remotion.dev/docs/lambda/approuterwebhook>
- Express webhook: <https://www.remotion.dev/docs/lambda/expresswebhook>
