# Troubleshooting renders

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Debug failed, blank, slow, desynced, or visually incorrect renders with a repeatable process.

## Mental model

Render failures usually come from one of six buckets: invalid composition metadata, missing assets, asynchronous work that never resolves, browser crashes, encoding constraints, or nondeterministic component logic. A good debugging flow narrows those buckets quickly.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Reproduce locally with the shortest command that fails.
2. Render one still at the failing frame if the error is visual.
3. Render a small frame range around the failure if the error is temporal.
4. Turn on logs and inspect browser console output.
5. Confirm every media file is reachable through `staticFile()`, public URLs, or allowed filesystem paths.
6. Remove scene complexity until a minimal component renders, then reintroduce layers.
7. Compare Studio, CLI, and renderer API inputs for differences.


## Reference implementation


```bash
# 1. Narrow to a still
npx remotion still src/index.ts Promo out/debug-frame.png --frame=123

# 2. Narrow to a small range
npx remotion render src/index.ts Promo out/debug-range.mp4 --frames=110-140

# 3. Lower pressure to test for memory/concurrency issues
npx remotion render src/index.ts Promo out/debug-safe.mp4 --concurrency=1 --scale=0.5
```

```tsx
import {delayRender, continueRender, cancelRender} from 'remotion';

const handle = delayRender('load remote image metadata');
loadMetadata()
  .then(() => continueRender(handle))
  .catch((err) => cancelRender(err));
```


## Review checklist


- The failure has a minimal reproduction command.
- Failing frame, props, codec, and machine details are recorded.
- Asset paths are checked in a clean environment.
- `delayRender()` handles always resolve or cancel.
- Browser and FFmpeg logs are attached to render job records.


## Common pitfalls


- Blank frames can come from CSS opacity, offscreen transforms, transparent output, or a component returning `null` at that frame.
- Audio drift can come from mismatched FPS assumptions, trimmed media, or incorrectly sequenced audio.
- Random values should be seeded or derived from stable props/frame numbers.
- Remote assets can be rate-limited or slow; prefer local/static assets for critical renders.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- Debug failed render: <https://www.remotion.dev/docs/troubleshooting/debug-failed-render>
- Get help: <https://www.remotion.dev/docs/get-help>
- `delayRender()`: <https://www.remotion.dev/docs/delay-render>
- `cancelRender()`: <https://www.remotion.dev/docs/cancel-render>
