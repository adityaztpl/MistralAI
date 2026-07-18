# 11 - Three-dimensional video

Remotion can render 3D scenes by combining React, React Three Fiber, Three.js, and `@remotion/three`. The key is to keep the 3D scene deterministic frame-by-frame while respecting the extra cost of WebGL, lighting, cameras, textures, and model loading.

## Learning goals

- Use `@remotion/three` and React Three Fiber concepts inside a Remotion composition.
- Animate cameras, lights, meshes, materials, and model transforms with frame-derived values.
- Avoid common canvas and WebGL pitfalls during headless rendering.
- Design 3D scenes that can be rendered locally, on a server, or in cloud infrastructure.

## Files

| File | Focus |
|------|-------|
| [01-remotion-three.md](01-remotion-three.md) | Setup and mental model for `@remotion/three` |
| [02-three-canvas-tips.md](02-three-canvas-tips.md) | Canvas sizing, camera, lighting, assets, and performance tips |
| [03-3d-animation-patterns.md](03-3d-animation-patterns.md) | Frame-based camera moves, product spins, parallax, and scene transitions |

## Official docs

- `@remotion/three`: <https://www.remotion.dev/docs/three>
- API overview: <https://www.remotion.dev/docs/api>
