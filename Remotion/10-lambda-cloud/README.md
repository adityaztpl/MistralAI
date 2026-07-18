# 10 - Lambda and cloud rendering

This section covers Remotion rendering on AWS Lambda, Google Cloud Run, and distributed job architectures. The goal is to move beyond local rendering into repeatable, scalable production workflows that can serve users, queue jobs, store artifacts, and report completion.

## Learning goals

- Explain when Lambda, Cloud Run, a container worker, or a custom render farm is the right fit.
- Deploy Remotion cloud infrastructure with least-privilege credentials and repeatable versioning.
- Render videos and stills through CLI commands and APIs such as `renderMediaOnLambda()`.
- Estimate cost and tune concurrency, memory, timeout, and region choices.
- Store outputs in S3 or GCS and notify applications through webhooks.
- Design distributed rendering patterns that tolerate retries, idempotency, progress polling, and partial failure.

## Files

| File | Focus |
|------|-------|
| [01-lambda-overview.md](01-lambda-overview.md) | Mental model for Remotion Lambda and when to use it |
| [02-setup-and-deploy.md](02-setup-and-deploy.md) | AWS setup, permissions, functions, sites, and deployment workflow |
| [03-lambda-render-cli-api.md](03-lambda-render-cli-api.md) | Rendering via CLI and `@remotion/lambda` API calls |
| [04-cost-concurrency-regions.md](04-cost-concurrency-regions.md) | Cost drivers, concurrency, regions, memory, and operational limits |
| [05-cloudrun.md](05-cloudrun.md) | Google Cloud Run rendering model and API shape |
| [06-distributed-rendering-patterns.md](06-distributed-rendering-patterns.md) | Queues, idempotency, status models, retries, and fan-out/fan-in |
| [07-s3-artifacts-webhooks.md](07-s3-artifacts-webhooks.md) | Artifact storage, signed URLs, metadata, and webhook verification |

## Recommended path

1. Render locally first so visual issues are separated from cloud issues.
2. Deploy a minimal Lambda function and site with a test composition.
3. Render one still, then one short video, then a realistic production payload.
4. Add progress polling, output persistence, and webhooks.
5. Put a queue in front of rendering before letting users submit large jobs.

## Official docs

- Lambda overview: <https://www.remotion.dev/docs/lambda>
- Lambda API: <https://www.remotion.dev/docs/lambda/api>
- Cloud Run: <https://www.remotion.dev/docs/cloudrun>
- Cloud Run API: <https://www.remotion.dev/docs/cloudrun/api>
