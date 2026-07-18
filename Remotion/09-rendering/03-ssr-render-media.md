# Server-side rendering with `@remotion/renderer`

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Build a Node.js or Bun rendering service using `bundle()`, `selectComposition()`, and `renderMedia()`.

## Mental model

`@remotion/renderer` gives application code the same rendering power as the CLI. Instead of shelling out to `npx remotion`, a service can bundle a Remotion project, select a composition with typed `inputProps`, render media, report progress, and store the resulting file.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Bundle the Remotion entry point once per version or deployment when possible.
2. Call `selectComposition()` with the composition id and the same `inputProps` that will be used for rendering.
3. Use the returned composition metadata as the source of truth for dimensions, FPS, and duration.
4. Call `renderMedia()` with output location, codec, and progress callbacks.
5. Run this work in a background worker when requests can exceed normal HTTP timeouts.
6. Store artifacts in durable storage and return an asset URL, not the whole binary through a long request.


## Reference implementation


```ts
import path from 'node:path';
import {bundle} from '@remotion/bundler';
import {selectComposition, renderMedia} from '@remotion/renderer';

const entry = path.join(process.cwd(), 'src', 'index.ts');

export async function renderProductPromo(inputProps: {
  title: string;
  accentColor: string;
}) {
  const serveUrl = await bundle({entryPoint: entry});

  const composition = await selectComposition({
    serveUrl,
    id: 'ProductPromo',
    inputProps,
  });

  const outputLocation = path.join(process.cwd(), 'out', `${Date.now()}.mp4`);

  await renderMedia({
    composition,
    serveUrl,
    codec: 'h264',
    outputLocation,
    inputProps,
    onProgress: ({progress}) => {
      console.log(`render ${(progress * 100).toFixed(1)}%`);
    },
  });

  return {outputLocation, durationInFrames: composition.durationInFrames};
}
```

```ts
// Express-style route sketch: enqueue rather than blocking forever.
app.post('/renders', async (req, res) => {
  const job = await renderQueue.add('product-promo', req.body);
  res.status(202).json({jobId: job.id});
});
```


## Review checklist


- Rendering runs outside the request thread for long videos.
- Bundles are reused or cached by version to reduce cold-start work.
- `selectComposition()` receives the same `inputProps` as `renderMedia()`.
- Progress and logs are persisted against a job id.
- The worker has enough CPU, memory, disk, Chrome dependencies, and FFmpeg support for the selected codec.


## Common pitfalls


- Calling `renderMedia()` inside a serverless function without enough timeout will fail halfway through encoding.
- Mutating props between `selectComposition()` and `renderMedia()` can change duration or layout unexpectedly.
- Running untrusted React code is a security risk; treat render bundles as code execution.
- Writing outputs to ephemeral disk and then returning before upload can lose artifacts.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- Renderer package: <https://www.remotion.dev/docs/renderer>
- `renderMedia()`: <https://www.remotion.dev/docs/renderer/render-media>
- `selectComposition()`: <https://www.remotion.dev/docs/renderer/select-composition>
- Bundling: <https://www.remotion.dev/docs/bundle>
