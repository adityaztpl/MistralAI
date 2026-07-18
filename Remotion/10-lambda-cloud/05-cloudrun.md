# Cloud Run rendering

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Use Google Cloud Run as a Remotion rendering target and understand how it differs from AWS Lambda.

## Core idea

Remotion Cloud Run runs rendering in Google Cloud infrastructure. The shape is familiar: deploy a service, deploy a site, start media or still renders, poll progress, and store artifacts. It is a strong fit when the rest of the stack already lives on GCP or when Cloud Run operational controls match your team better than Lambda.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Confirm the project is already stable with local renders.
2. Configure GCP credentials and project selection outside the repository.
3. Deploy or identify the Cloud Run service that will execute renders.
4. Deploy the Remotion site used by that service.
5. Start a still render, then a short media render.
6. Add progress polling and artifact handling to the application backend.
7. Monitor service revisions, concurrency, memory, timeouts, and logs.


## Practical example


```ts
import {
  renderMediaOnCloudrun,
  getServiceInfo,
} from '@remotion/cloudrun/client';

export async function renderOnGcp(inputProps: Record<string, unknown>) {
  const service = await getServiceInfo({
    region: 'us-central1',
    serviceName: process.env.REMOTION_CLOUDRUN_SERVICE!,
  });

  return renderMediaOnCloudrun({
    region: 'us-central1',
    serviceName: service.serviceName,
    serveUrl: process.env.REMOTION_CLOUDRUN_SITE!,
    composition: 'ProductPromo',
    codec: 'h264',
    inputProps,
  });
}
```

```bash
# Typical setup inventory shape; verify exact commands with the installed package version.
npx remotion cloudrun services ls --region=us-central1
npx remotion cloudrun sites ls --region=us-central1
```


## Operational checklist


- GCP project, region, service name, and site URL are environment-specific config.
- Service logs are connected to render job ids.
- Timeouts are set high enough for expected render classes.
- IAM roles are scoped to render and artifact operations.
- Cloud Run revisions are rolled forward deliberately when composition code changes.


## Risks and trade-offs


- Cloud Run concurrency settings can overload a single container if frame work is CPU-heavy.
- Region and project mismatches are common during multi-environment setup.
- A container image may need browser/FFmpeg dependencies that local development hides.
- As with Lambda, do not expose cloud credentials or render service controls directly to browsers.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Cloud Run overview: <https://www.remotion.dev/docs/cloudrun>
- Cloud Run API: <https://www.remotion.dev/docs/cloudrun/api>
- `renderMediaOnCloudrun()`: <https://www.remotion.dev/docs/cloudrun/rendermediaoncloudrun>
- Deploy service: <https://www.remotion.dev/docs/cloudrun/deployservice>
