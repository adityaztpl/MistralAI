# Setup and deploy

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Set up AWS credentials, deploy a Remotion Lambda function, deploy a site, and verify permissions safely.

## Core idea

A Remotion Lambda deployment has two main assets: a Lambda function capable of rendering and a Remotion site containing your bundled code. Setup is mostly about credentials and repeatability. Avoid one-off console changes; prefer scripts that can recreate infrastructure for a known Remotion version.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Install matching Remotion packages (`remotion`, `@remotion/cli`, `@remotion/lambda`).
2. Configure AWS credentials outside the repository using environment variables, SSO, or an instance role.
3. Use Remotion-provided helpers to inspect or simulate required permissions.
4. Deploy the function in the target region with chosen memory, timeout, disk, and architecture settings.
5. Deploy the site from your Remotion entry point.
6. Store function name, region, site name, bucket, and Remotion version in environment-specific config.
7. Run a tiny smoke render and clean up unused older deployments on a schedule.


## Practical example


```bash
# Install package used by the cloud API and CLI helpers
npm i @remotion/lambda

# Example deploy shape; confirm exact flags with the official docs for your Remotion version.
npx remotion lambda functions deploy --region=us-east-1
npx remotion lambda sites create src/index.ts --region=us-east-1

# Useful inventory commands during setup
npx remotion lambda functions ls --region=us-east-1
npx remotion lambda sites ls --region=us-east-1
```

```env
REMOTION_AWS_REGION=us-east-1
REMOTION_FUNCTION_NAME=remotion-render-2026-07
REMOTION_SITE_NAME=product-video-site
REMOTION_BUCKET_NAME=remotionlambda-123456789-us-east-1
```


## Operational checklist


- AWS credentials are not committed and are not embedded into frontend bundles.
- Deployed Remotion package versions match project dependencies.
- Function and site names are recorded per environment.
- Old sites/functions are deleted only after active jobs are finished.
- A smoke render is part of release validation.


## Risks and trade-offs


- Permission errors are common during first setup. Use the official policy helpers rather than broad administrator access in production.
- Region mismatch between function, site, and bucket causes confusing failures and latency.
- Updating composition code requires deploying a new site; a new Git commit alone does not update cloud-rendered code.
- Deleting a function or site used by in-flight jobs can break progress checks or retries.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Lambda setup: <https://www.remotion.dev/docs/lambda/setup>
- Deploy function: <https://www.remotion.dev/docs/lambda/deployfunction>
- Deploy site: <https://www.remotion.dev/docs/lambda/deploysite>
- Permissions helpers: <https://www.remotion.dev/docs/lambda/simulatepermissions>
