# Lambda render CLI and API

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Start Remotion Lambda renders from repeatable CLI commands or application code using `@remotion/lambda`.

## Core idea

The CLI is useful for operator-driven renders and smoke tests. The API is useful for products: validate a request, start a render, store the render id, poll progress, and expose output when complete. Both paths need the same core information: region, function, site, composition id, and input props.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Render a still first to validate the deployed site and props cheaply.
2. Render a short video with conservative codec settings.
3. Add application API code that starts renders asynchronously.
4. Persist `renderId`, `bucketName`, `functionName`, `region`, `compositionId`, and `inputProps` hash.
5. Poll `getRenderProgress()` or process a webhook.
6. Mark jobs complete only after output metadata is available.


## Practical example


```ts
import {renderMediaOnLambda, getRenderProgress} from '@remotion/lambda/client';

export async function startLambdaRender(inputProps: Record<string, unknown>) {
  const result = await renderMediaOnLambda({
    region: 'us-east-1',
    functionName: process.env.REMOTION_FUNCTION_NAME!,
    serveUrl: process.env.REMOTION_SITE_URL!,
    composition: 'ProductPromo',
    codec: 'h264',
    inputProps,
  });

  return {
    renderId: result.renderId,
    bucketName: result.bucketName,
  };
}

export async function pollLambdaRender(renderId: string, bucketName: string) {
  return getRenderProgress({
    region: 'us-east-1',
    functionName: process.env.REMOTION_FUNCTION_NAME!,
    renderId,
    bucketName,
  });
}
```

```bash
# CLI smoke-test shape; check exact flags for the installed Remotion version.
npx remotion lambda render ProductPromo out/product-promo.mp4   --region=us-east-1   --props=./render-props/acme.json
```


## Operational checklist


- API routes validate and normalize user props before starting the render.
- The job table can resume polling after a process restart.
- The render id and bucket name are treated as required state, not log-only details.
- Progress responses are mapped to product states such as queued, rendering, encoding, completed, and failed.
- Errors returned to users avoid leaking credentials or internal bucket structure.


## Risks and trade-offs


- Starting renders synchronously from a request and waiting for completion can exceed API gateway or load balancer timeouts.
- Losing `renderId` means the app cannot reliably resume progress tracking.
- Mixing site versions with old props can create hard-to-debug layout or schema failures.
- User-visible retry buttons should be idempotent so repeated clicks do not start duplicate expensive renders.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- `renderMediaOnLambda()`: <https://www.remotion.dev/docs/lambda/rendermediaonlambda>
- `renderStillOnLambda()`: <https://www.remotion.dev/docs/lambda/renderstillonlambda>
- `getRenderProgress()`: <https://www.remotion.dev/docs/lambda/getrenderprogress>
- CLI docs: <https://www.remotion.dev/docs/cli>
