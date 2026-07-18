# Studio rendering

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Use Remotion Studio as the fastest loop for validating compositions, props, timing, and basic output before automating renders elsewhere.

## Mental model

Studio is a development server plus UI around the same composition tree that will later be rendered by the CLI or renderer package. It lets you scrub frames, inspect compositions, adjust props when schemas are present, and trigger renders from the browser when a local server is connected.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Start Studio with `npm start`, `npm run remotion`, or `npx remotion studio` depending on the template.
2. Select the composition by `id`; verify duration, FPS, width, and height in the sidebar.
3. Scrub through important frames: first frame, scene cuts, animation peaks, text overlays, audio entrances, and final frame.
4. Render a draft from the Studio UI at a lower resolution or shorter frame range if iteration speed matters.
5. Compare the draft with the preview. If the render differs, suspect asynchronous assets, nondeterministic state, browser-only APIs, fonts, or missing static files.
6. Once the visual result is stable, translate the same settings into a CLI command or renderer function so the build can run without clicking the UI.


## Reference implementation


```jsonc
// package.json
{
  "scripts": {
    "start": "remotion studio",
    "render:demo": "remotion render src/index.ts Demo out/demo.mp4",
    "render:still": "remotion still src/index.ts Demo out/poster.png --frame=45"
  }
}
```

```tsx
// src/Root.tsx
import {Composition} from 'remotion';
import {z} from 'zod';

const promoSchema = z.object({
  title: z.string(),
  accent: z.string().default('#7c3aed'),
});

export const RemotionRoot = () => (
  <Composition
    id="ProductPromo"
    component={ProductPromo}
    durationInFrames={180}
    fps={30}
    width={1920}
    height={1080}
    schema={promoSchema}
    defaultProps={{title: 'Launch day', accent: '#7c3aed'}}
  />
);
```


## Review checklist


- The selected composition has the expected dimensions and FPS.
- `defaultProps` are representative of real production input.
- Fonts and media assets are loaded via Remotion-safe primitives such as `staticFile()`, font loaders, `delayRender()`, or `continueRender()` when needed.
- The draft render is inspected outside Studio in a normal video player.
- Any Studio-only convenience is converted into repeatable CLI or API settings before handoff.


## Common pitfalls


- Studio preview looks correct but render is blank: check `delayRender()` handles, thrown promises, CSS that depends on viewport assumptions, and missing static assets.
- Render button is unavailable or does not start: ensure the CLI package is installed and the dev server is actually running.
- A render is slower than preview: encoding, frame capture, and audio muxing are extra work; Studio preview only displays frames interactively.
- Composition props differ between preview and render: align Studio props, `defaultProps`, CLI `--props`, and server `inputProps`.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- Studio: <https://www.remotion.dev/docs/studio>
- Rendering: <https://www.remotion.dev/docs/render>
- Composition API: <https://www.remotion.dev/docs/composition>
