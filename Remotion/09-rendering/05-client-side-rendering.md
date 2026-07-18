# Client-side rendering

> This guide is part of the local Remotion curriculum dump. It summarizes the workflow in practical terms and points back to the official Remotion docs for API-level details.

## Outcome

Understand when rendering in a browser is feasible, when Remotion Player is enough, and why production rendering usually belongs on a server.

## Mental model

The browser is excellent for previewing and interactive playback, but full media rendering needs frame capture, encoding, memory, long-running work, and file handling. Remotion supports browser-side patterns for some workflows, but teams should separate preview experiences from durable artifact generation.

Remotion renders are deterministic React evaluations over a frame number. If a composition is 300 frames at 30 FPS, Remotion asks React what the screen should look like for frame 0, frame 1, frame 2, and so on. The renderer captures those frames and encodes them into the requested media format. That means the best Remotion projects keep visual state derived from `useCurrentFrame()`, `useVideoConfig()`, props, and loaded assets instead of timers, random browser state, or user interaction.

## Production workflow


1. Use `@remotion/player` when the user needs preview, playback, pausing, seeking, or interactive customization.
2. Use server-side rendering when the user needs a downloadable file, durable artifact, webhook, or repeatable job.
3. If a browser workflow must export media, constrain duration, resolution, and codec expectations.
4. Avoid sending secrets, private render credentials, or trusted backend logic into browser render code.
5. For SaaS products, let the client submit render input and poll a backend job instead of performing the heavy render itself.


## Reference implementation


```tsx
import {Player} from '@remotion/player';
import {ProductPromo} from './ProductPromo';

export const PreviewCard = () => {
  return (
    <Player
      component={ProductPromo}
      durationInFrames={180}
      fps={30}
      compositionWidth={1080}
      compositionHeight={1080}
      inputProps={{title: 'Preview title', accentColor: '#f97316'}}
      controls
      loop
    />
  );
};
```

```ts
// Browser submits intent; backend owns rendering.
await fetch('/api/renders', {
  method: 'POST',
  headers: {'content-type': 'application/json'},
  body: JSON.stringify({
    compositionId: 'ProductPromo',
    inputProps: {title: 'Customer export'},
  }),
});
```


## Review checklist


- Preview and export requirements are documented separately.
- Large renders are pushed to a backend worker or cloud renderer.
- Client inputs are validated on the server before rendering.
- The UI exposes progress, cancellation, and retry states for backend jobs.
- Browser-only APIs are not assumed to be available in server renders.


## Common pitfalls


- Treating Player as an export engine leads to unreliable downloads for long videos.
- Browser memory limits vary by device and can break customer exports.
- Client-side code cannot safely hold AWS, GCP, or private storage credentials.
- A preview that depends on user gestures or wall-clock timers may not render deterministically elsewhere.


## Interview-ready explanation

When explaining this topic, start with the frame-based rendering model: Remotion turns React components into still frames, then encodes them. Mention the boundary between authoring, previewing, and rendering. Then describe the operational concern this file covers: inputs, concurrency, codecs, cloud execution, Studio behavior, captions, or 3D rendering. Close by naming the relevant package or CLI surface so the listener knows where the behavior lives.

## Practice tasks

1. Build a 5-second composition that uses `inputProps` for at least two visual decisions.
2. Render a low-resolution draft, inspect the output, then render the final settings.
3. Write down the exact command or API call you used and which options affect speed, quality, and reproducibility.
4. Add one failure-mode note to your project README so a teammate can diagnose the same render later.


## Official docs

- Client-side rendering: <https://www.remotion.dev/docs/client-side-rendering/>
- Player: <https://www.remotion.dev/docs/player/>
- Security: <https://www.remotion.dev/docs/security>
