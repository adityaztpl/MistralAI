# Rendering cheatsheet

## Choose the render surface

| Need | Use |
|------|-----|
| Quick preview and scrubbing | Studio |
| Repeatable local artifact | CLI |
| Backend service render | `@remotion/renderer` |
| Burst cloud render on AWS | `@remotion/lambda` |
| GCP-native render | `@remotion/cloudrun` |
| Browser preview inside product | `@remotion/player` |

## Video defaults

- H.264 MP4 is the safest default for broad compatibility.
- Use lower-resolution drafts while iterating.
- Render stills at important frames for visual regression checks.
- Keep props in JSON files for reproducible CLI renders.

## Quality knobs

| Knob | Effect |
|------|--------|
| Codec | Compatibility, speed, output type |
| CRF/quality | Compression level and file size |
| Scale | Draft speed and output resolution |
| Concurrency | Frame capture throughput and memory pressure |
| Pixel format | Transparency and player compatibility |
| Frame range | Debugging and smoke-test scope |

## Render API pattern

```ts
const serveUrl = await bundle({entryPoint});
const composition = await selectComposition({serveUrl, id, inputProps});
await renderMedia({serveUrl, composition, inputProps, codec: 'h264', outputLocation});
```

## Cloud render pattern

```text
validate request -> create job -> start Lambda/Cloud Run render -> persist provider render id -> poll/webhook -> store artifact -> notify user
```

## Debug order

1. Render a still at the failing frame.
2. Render a short frame range.
3. Lower concurrency and scale.
4. Check assets and async render handles.
5. Compare props across Studio, CLI, and API.
6. Inspect browser/renderer logs.

## Official docs

- Rendering: <https://www.remotion.dev/docs/render>
- Renderer: <https://www.remotion.dev/docs/renderer>
- Lambda: <https://www.remotion.dev/docs/lambda>
- Cloud Run: <https://www.remotion.dev/docs/cloudrun>
