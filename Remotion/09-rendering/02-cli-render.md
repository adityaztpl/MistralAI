# CLI rendering: videos, stills, and frames

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Render reproducible artifacts with `npx remotion render`, `npx remotion still`, and targeted frame ranges for fast iteration.

## Mental model

The CLI is the simplest automation boundary. It bundles the project, launches a browser renderer, captures frames, and encodes output. It is ideal for local scripts, CI smoke renders, and small production jobs before you need a queue or custom Node service.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Identify the Remotion entry file, usually `src/index.ts` or `src/index.tsx`.
2. Identify the composition id registered in `<Composition id="..." />`.
3. Render a draft with the default codec first, then add options only when you need them.
4. Use `--props` for parameterized renders. Keep complex props in a JSON file to avoid shell escaping bugs.
5. Use frame ranges for regression checks and expensive scenes.
6. Use `remotion still` for thumbnails, posters, Open Graph images, and video preview cards.


## Reference implementation


```bash
# Full video render
npx remotion render src/index.ts ProductPromo out/product-promo.mp4

# Render with props from a JSON file
npx remotion render src/index.ts ProductPromo out/acme.mp4   --props=./render-props/acme.json

# Render a still at a specific frame
npx remotion still src/index.ts ProductPromo out/poster.png --frame=90

# Render a frame range for a scene-level smoke test
npx remotion render src/index.ts ProductPromo out/scene-two.mp4   --frames=90-149

# Render transparent frames for compositing workflows
npx remotion render src/index.ts Overlay out/overlay.webm   --codec=vp8 --pixel-format=yuva420p
```

```json
// render-props/acme.json
{
  "brandName": "Acme Cloud",
  "headline": "Ship videos from React",
  "accentColor": "#38bdf8",
  "plan": "enterprise"
}
```


## Review checklist


- The command is saved in `package.json` or project docs.
- CI smoke renders use short frame ranges, not full 10-minute videos.
- `--props` files are validated against composition schemas when the project uses Zod.
- Output directories are ignored by Git unless the artifact is intentionally versioned.
- Commands distinguish videos (`render`) from stills (`still`).


## Common pitfalls


- Shell-escaped JSON breaks parameterized renders. Prefer `--props=path/to/file.json`.
- Rendering every frame for every pull request is expensive. Use one still and a short frame range for CI confidence.
- Frame ranges can hide errors later in the video. Use full renders before release.
- Stills at frame 0 often miss the best visual state; choose intentional poster frames.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- CLI render: <https://www.remotion.dev/docs/cli/render>
- Still images: <https://www.remotion.dev/docs/cli/still>
- Rendering guide: <https://www.remotion.dev/docs/render>
