# API cheatsheet

## Core packages

| Package | Purpose | Common imports |
|---------|---------|----------------|
| `remotion` | Core composition and animation APIs | `Composition`, `AbsoluteFill`, `Sequence`, `useCurrentFrame`, `useVideoConfig`, `interpolate`, `spring`, `staticFile` |
| `@remotion/cli` | Studio and render commands | package scripts and terminal commands |
| `@remotion/renderer` | Server-side rendering from Node/Bun | `selectComposition`, `renderMedia`, `renderStill` |
| `@remotion/bundler` | Bundle Remotion projects for renderer APIs | `bundle` |
| `@remotion/player` | Browser preview/player | `Player` |
| `@remotion/lambda` | AWS Lambda rendering | `renderMediaOnLambda`, `renderStillOnLambda`, `getRenderProgress` |
| `@remotion/cloudrun` | Google Cloud Run rendering | `renderMediaOnCloudrun`, service/site helpers |
| `@remotion/captions` | Subtitle utilities | `parseSrt`, `serializeSrt`, caption helpers |
| `@remotion/three` | React Three Fiber / Three.js rendering | Three integration helpers |

## Composition registry

```tsx
import {Composition} from 'remotion';

<Composition
  id="ProductPromo"
  component={ProductPromo}
  durationInFrames={180}
  fps={30}
  width={1920}
  height={1080}
  defaultProps={{title: 'Launch'}}
/>
```

## Frame math

```tsx
const frame = useCurrentFrame();
const {fps, durationInFrames, width, height} = useVideoConfig();
const seconds = frame / fps;
const opacity = interpolate(frame, [0, 20], [0, 1], {extrapolateRight: 'clamp'});
```

## Server render shape

```ts
const serveUrl = await bundle({entryPoint: 'src/index.ts'});
const composition = await selectComposition({serveUrl, id: 'ProductPromo', inputProps});
await renderMedia({serveUrl, composition, inputProps, codec: 'h264', outputLocation: 'out/final.mp4'});
```

## Asset loading

- Use `staticFile('asset.png')` for files in `public/`.
- Use `OffthreadVideo` for render-friendly video layers.
- Use `delayRender()` and `continueRender()` for asynchronous work that must complete before capture.
- Use `cancelRender()` when a required load fails.

## Official docs

- API overview: <https://www.remotion.dev/docs/api>
- Core Remotion APIs: <https://www.remotion.dev/docs/remotion>
- Renderer: <https://www.remotion.dev/docs/renderer>
