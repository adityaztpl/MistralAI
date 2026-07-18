# Remotion Three

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Understand how to compose Three.js scenes inside Remotion and render them as deterministic video.

## Mental model

`@remotion/three` bridges Remotion with React Three Fiber. React still describes the scene, but the output is a WebGL canvas captured frame-by-frame. The frame number should drive animation; Three.js imperative mutation should be contained and predictable.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Install `three`, `@react-three/fiber`, and `@remotion/three` alongside Remotion.
2. Register a composition with dimensions appropriate for the final video, not the browser window.
3. Put the canvas inside an `AbsoluteFill` so the render size is stable.
4. Drive rotation, position, material properties, and camera moves from `useCurrentFrame()` and `interpolate()`.
5. Load textures and GLTF/GLB models with render-aware loading so assets are available before capture.
6. Render short frame ranges first; 3D scenes can fail late due to memory or shader issues.


## Example


```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';
import {Canvas} from '@react-three/fiber';

const ProductMesh = () => {
  const frame = useCurrentFrame();
  const rotationY = interpolate(frame, [0, 120], [0, Math.PI * 2]);

  return (
    <mesh rotation={[0.4, rotationY, 0]}>
      <boxGeometry args={[2.4, 1.4, 0.4]} />
      <meshStandardMaterial color="#7c3aed" roughness={0.35} metalness={0.25} />
    </mesh>
  );
};

export const ThreeProductCard = () => (
  <AbsoluteFill style={{background: '#050816'}}>
    <Canvas camera={{position: [0, 0, 5], fov: 45}}>
      <ambientLight intensity={0.5} />
      <directionalLight position={[3, 4, 5]} intensity={1.4} />
      <ProductMesh />
    </Canvas>
  </AbsoluteFill>
);
```


## Checklist


- The canvas fills the composition exactly.
- Every animation is derived from the frame number.
- Assets are local, cached, or loaded with proper render blocking.
- Camera, lights, and background are explicitly set.
- A still render at representative frames is inspected before a full video render.


## Pitfalls


- Using `requestAnimationFrame` directly can desync from Remotion's frame capture.
- Device-specific canvas sizes create mismatched output between Studio and renderer.
- Remote textures or GLB files can timeout or violate CORS in headless rendering.
- Complex shaders can be much slower on cloud render environments than on a developer GPU.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- `@remotion/three`: <https://www.remotion.dev/docs/three>
- `useCurrentFrame()`: <https://www.remotion.dev/docs/use-current-frame>
- `interpolate()`: <https://www.remotion.dev/docs/interpolate>
