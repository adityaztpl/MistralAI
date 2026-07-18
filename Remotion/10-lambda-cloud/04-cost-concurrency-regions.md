# Cost, concurrency, and regions

> This guide is part of the local Remotion curriculum dump and should be read alongside the official Remotion Lambda and Cloud Run docs.

## Outcome

Estimate cloud render cost and tune region, memory, concurrency, and quotas for predictable production behavior.

## Core idea

Cloud rendering cost is driven by duration, resolution, frame complexity, memory allocation, Lambda execution time, storage, data transfer, and failed retries. Concurrency improves throughput but can also collide with account quotas, downstream API limits, and storage bandwidth.

Cloud rendering changes the bottleneck from one machine to an orchestrated fleet. A render request must identify a deployed site or bundle, a function or service version, a composition id, input props, codec/output settings, and a storage destination. The app then tracks progress, exposes the resulting artifact, and cleans up temporary resources.

## Implementation workflow


1. Define render classes: draft, standard, final, vertical short, long-form, transparent overlay, still.
2. Benchmark each class with representative props and assets.
3. Track frames rendered per second, Lambda duration, output size, and failure rate.
4. Set concurrency limits at the product layer before cloud quotas are reached.
5. Choose regions close to storage and users, but prioritize regions officially supported by the render platform.
6. Add budget alarms and per-tenant rate limits before launching public self-serve rendering.


## Practical example


```text
Cost model inputs
- composition duration: 900 frames
- fps: 30
- resolution: 1920x1080
- render type: final h264
- average Lambda wall time: 140 seconds
- memory setting: 3 GB
- output size: 80 MB
- retry rate: 2%
```

```ts
const limits = {
  free: {maxDurationFrames: 300, maxConcurrentRenders: 1, maxDailyRenders: 10},
  pro: {maxDurationFrames: 1800, maxConcurrentRenders: 3, maxDailyRenders: 200},
  enterprise: {maxDurationFrames: 9000, maxConcurrentRenders: 20, maxDailyRenders: 5000},
};
```


## Operational checklist


- Each product plan has explicit render limits.
- Failed renders are measured and included in cost estimates.
- Storage lifecycle policies delete temporary or expired artifacts.
- Region choice is documented with latency, compliance, and availability assumptions.
- Queue depth and age are monitored, not only individual render progress.


## Risks and trade-offs


- Unlimited concurrency can create a self-inflicted denial of wallet.
- Rendering in a distant region can increase artifact latency and egress cost.
- Higher memory can be cheaper if it shortens runtime significantly, but it must be benchmarked.
- Retries without backoff can multiply cost during a dependency outage.


## Interview-ready explanation

For an interview or design review, explain that Remotion Lambda and Cloud Run are not separate animation systems. They run the same Remotion compositions but move rendering into cloud infrastructure. The hard parts become deployment versioning, permissions, regional latency, concurrency limits, artifact storage, webhooks, and cost controls.

## Practice tasks

1. Diagram a render request from API call through cloud execution to final S3 or GCS artifact.
2. Pick one composition and define the minimal `inputProps` payload that should be accepted from an external product.
3. Write a retry policy that distinguishes user input errors from transient cloud failures.
4. Estimate cost for a batch of 1,000 renders using duration, resolution, concurrency, and storage assumptions.


## Official docs

- Estimate price: <https://www.remotion.dev/docs/lambda/estimateprice>
- Lambda regions: <https://www.remotion.dev/docs/lambda/getregions>
- Cloud Run regions: <https://www.remotion.dev/docs/cloudrun/getregions>
