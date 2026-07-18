# Codecs, quality, and concurrency

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Choose render settings that match distribution requirements instead of blindly maximizing quality or parallelism.

## Mental model

A Remotion render has two expensive phases: frame capture and media encoding. Codec choices affect compatibility and final size. Quality settings affect compression artifacts. Concurrency affects speed but also memory, CPU pressure, browser crashes, and I/O contention.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Start with the distribution target: social upload, transparent overlay, archival master, web preview, audio-only, or still frame.
2. Pick a codec based on compatibility. H.264 MP4 is the safest default for broad playback.
3. Tune quality only after the composition is final; visual changes can alter compressibility.
4. Increase concurrency gradually while watching memory and CPU saturation.
5. Use lower resolution drafts during creative iteration and reserve final resolution for release renders.
6. Record the exact settings used for deliverables so future renders are comparable.


## Reference implementation


```bash
# Compatible social/web default
npx remotion render src/index.ts Promo out/promo.mp4 --codec=h264 --crf=18

# Smaller draft for review
npx remotion render src/index.ts Promo out/promo-draft.mp4   --scale=0.5 --crf=28

# Transparent web overlay
npx remotion render src/index.ts LowerThird out/lower-third.webm   --codec=vp8 --pixel-format=yuva420p

# Image sequence for external compositing
npx remotion render src/index.ts ProductShot out/frames   --image-format=png --frames=0-119
```

```ts
await renderMedia({
  composition,
  serveUrl,
  codec: 'h264',
  crf: 18,
  concurrency: 4,
  outputLocation: 'out/final.mp4',
});
```


## Review checklist


- The codec is playable in the target environment.
- The chosen CRF or quality setting is documented alongside the artifact.
- Transparency requirements are tested in the final destination, not just a local player.
- Concurrency is benchmarked on the actual render machine.
- Audio sample rate and channel assumptions are checked when combining media.


## Common pitfalls


- Higher concurrency is not always faster; it can trigger memory pressure or slower encodes.
- A very low CRF can create huge files with little visible improvement.
- Some codecs and pixel formats are incompatible with common players.
- Draft scale can hide text legibility problems; always inspect final resolution.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- Render options: <https://www.remotion.dev/docs/cli/render>
- `renderMedia()` options: <https://www.remotion.dev/docs/renderer/render-media>
- Rendering guide: <https://www.remotion.dev/docs/render>
