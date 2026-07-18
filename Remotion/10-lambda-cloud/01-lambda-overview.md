# Remotion Lambda overview

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Understand why Remotion Lambda exists, how it scales rendering, and which workloads fit it best.

## Core idea

Remotion Lambda deploys render workers into AWS Lambda and uploads the Remotion site/bundle to cloud storage. A render request fans out work across Lambda invocations, renders chunks of frames, and stitches the result into a final artifact. The developer still writes React components; the cloud layer handles distributed execution.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Keep authoring local: build and preview with Studio and CLI until composition behavior is stable.
2. Deploy Lambda infrastructure for the Remotion version you are using.
3. Deploy a site, which is the cloud-hosted bundle of your Remotion project.
4. Start renders by composition id and input props.
5. Poll progress or use webhooks to learn when the render is complete.
6. Download or serve the output from the configured bucket.


## Practical example


```text
Client/API request
   -> application backend validates input
   -> backend calls @remotion/lambda
   -> Lambda workers render chunks
   -> output is written to S3
   -> backend receives webhook or polls progress
   -> user receives signed download URL
```

```ts
// Conceptual payload accepted by a product backend.
type RenderRequest = {
  compositionId: 'ProductPromo' | 'InstagramReel';
  props: Record<string, unknown>;
  output: 'mp4' | 'webm' | 'png';
  priority: 'draft' | 'final';
};
```


## Operational checklist


- Composition code is deterministic and does not depend on unavailable local files.
- AWS account boundaries, IAM policies, and bucket names are documented.
- Function/site versions are tied to a release or Git SHA.
- Application jobs store render id, bucket path, progress, logs, and requester.
- Render output is not assumed to be instantly available after the start call.


## Risks and trade-offs


- Lambda is excellent for bursty rendering but not a replacement for validating composition correctness.
- Large or unusual codecs may need careful memory and timeout settings.
- AWS account quotas and region support can become product limits.
- Cloud rendering can amplify bad input; validate props before starting expensive jobs.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Remotion Lambda: <https://www.remotion.dev/docs/lambda>
- Lambda API overview: <https://www.remotion.dev/docs/lambda/api>
- `getRenderProgress()`: <https://www.remotion.dev/docs/lambda/getrenderprogress>
