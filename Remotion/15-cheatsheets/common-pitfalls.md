# Common pitfalls

## Studio works, render fails

Likely causes:

- Missing asset in `public/` or inaccessible remote URL.
- `delayRender()` handle never resolved.
- Browser-only API used in a server/headless render path.
- Font loaded in preview but not ready during capture.
- Props differ between Studio and CLI/API render.

Fixes:

- Render a still at the failing frame.
- Use `staticFile()` for project assets.
- Add explicit font loading.
- Use props JSON files.
- Resolve or cancel all async work.

## Render is slow

Likely causes:

- Huge resolution or high DPR.
- Heavy CSS filters, shadows, blur, or WebGL effects.
- Remote media downloads during every render.
- Too much or too little concurrency.
- Large videos encoded at unnecessarily high quality.

Fixes:

- Use draft scale for iteration.
- Benchmark concurrency on the actual machine.
- Cache or localize assets.
- Simplify expensive layers.
- Tune CRF after visual approval.

## Captions drift

Likely causes:

- Mixing milliseconds, seconds, and frames incorrectly.
- Changing FPS after caption timing was authored.
- Rounding too aggressively during export.
- ASR word timings are imprecise.

Fixes:

- Store canonical timing in milliseconds or seconds.
- Convert to frames only for display.
- Use the same source for burned-in captions and sidecar subtitles.
- Allow editorial correction.

## Cloud renders are unreliable

Likely causes:

- Region mismatch between function, site, and bucket.
- Deployed site does not match local code.
- IAM permissions missing or too broad to debug safely.
- Jobs are not idempotent.
- Output artifacts are deleted or inaccessible.

Fixes:

- Store function/site/version/region with every job.
- Run a still smoke test after deploy.
- Persist provider render ids.
- Verify webhooks and poll progress with backoff.
- Use lifecycle rules carefully.

## Design looks bad after upload

Likely causes:

- Text too small for mobile.
- Contrast too low after platform compression.
- Important content under platform UI.
- Fast motion creates encoding artifacts.
- Poster frame chosen before the visual hook appears.

Fixes:

- Review on the target device.
- Use larger type and stronger contrast.
- Leave safe areas for social UI.
- Slow down high-detail motion.
- Render intentional cover frames.
