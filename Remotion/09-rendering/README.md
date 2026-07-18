# 09 - Rendering

Rendering is where a Remotion composition becomes an artifact: MP4, WebM, ProRes, GIF, PNG, JPEG, or a single still. This section covers Studio renders, CLI renders, server-side rendering with `@remotion/renderer`, codecs and quality, browser/client-side approaches, and troubleshooting.

## Learning goals

- Explain the difference between previewing in Studio and rendering a deterministic artifact.
- Use `npx remotion render` for videos, stills, and frame ranges.
- Use `@remotion/renderer` with `selectComposition()` and `renderMedia()` from Node.js or Bun.
- Choose codecs, CRF/quality, concurrency, and output settings intentionally.
- Understand why client-side rendering is limited and when Remotion Player is a better fit.
- Diagnose failed renders with logs, browser screenshots, asset checks, and minimal reproductions.

## Files

| File | Focus |
|------|-------|
| [01-studio-render.md](01-studio-render.md) | Rendering from Remotion Studio and validating compositions visually |
| [02-cli-render.md](02-cli-render.md) | `npx remotion render`, stills, image sequences, and frame ranges |
| [03-ssr-render-media.md](03-ssr-render-media.md) | Server-side rendering with `@remotion/renderer`, `selectComposition()`, and `renderMedia()` |
| [04-codecs-quality-concurrency.md](04-codecs-quality-concurrency.md) | Codec selection, quality knobs, pixel formats, parallelism, and output trade-offs |
| [05-client-side-rendering.md](05-client-side-rendering.md) | Browser-side rendering limits, Player workflows, and why server rendering is usually preferred |
| [06-troubleshooting-renders.md](06-troubleshooting-renders.md) | Practical debugging flow for crashed, slow, empty, or desynced renders |

## Recommended path

1. Render the demo composition from Studio to validate the visual design.
2. Repeat the render with the CLI and commit the exact command to documentation or package scripts.
3. Move rendering into a Node route, worker, or queue using `@remotion/renderer`.
4. Tune codecs and concurrency only after correctness is stable.
5. Add troubleshooting notes before you scale to Lambda, Cloud Run, or a job queue.

## Official docs

- Rendering: <https://www.remotion.dev/docs/render>
- CLI render command: <https://www.remotion.dev/docs/cli/render>
- Server-side rendering: <https://www.remotion.dev/docs/ssr>
- Renderer package: <https://www.remotion.dev/docs/renderer>
- Client-side rendering notes: <https://www.remotion.dev/docs/client-side-rendering/>
