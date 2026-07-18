# Three canvas tips

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Make Three.js canvases render reliably by controlling size, camera, lighting, assets, and render cost.

## Mental model

A Three.js scene is sensitive to environment. In Remotion, the environment is the composition dimensions and the headless browser. Stable 3D renders come from explicit canvas sizing, predictable device pixel ratio, careful asset loading, and scene complexity that matches the render machine.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Use composition dimensions as the source of truth. Avoid `window.innerWidth` and responsive breakpoints that change capture output.
2. Choose a camera FOV and position based on the video format: square product card, landscape promo, or vertical reel.
3. Keep lights simple until the scene is correct; expensive shadows can dominate render time.
4. Compress textures and reduce model polygon count before importing.
5. Test with `--concurrency=1` when debugging WebGL crashes.
6. Render key stills at frame 0, middle, and final frame to catch camera clipping and material issues.


## Example


```tsx
import {useVideoConfig} from 'remotion';
import {Canvas} from '@react-three/fiber';

export const StableCanvas = ({children}: {children: React.ReactNode}) => {
  const {width, height} = useVideoConfig();

  return (
    <Canvas
      gl={{antialias: true, preserveDrawingBuffer: true}}
      dpr={1}
      style={{width, height}}
      camera={{position: [0, 1.2, 6], fov: 38, near: 0.1, far: 100}}
    >
      <color attach="background" args={['#0b1020']} />
      <ambientLight intensity={0.55} />
      <directionalLight position={[4, 6, 5]} intensity={1.2} />
      {children}
    </Canvas>
  );
};
```

```bash
# Useful WebGL debugging render
npx remotion render src/index.ts ThreeScene out/three-debug.mp4   --frames=0-90 --concurrency=1
```


## Checklist


- Camera near/far planes do not clip the subject during the full animation.
- DPR is intentional; higher DPR may improve quality but increases cost.
- Shadows, postprocessing, and reflections are benchmarked separately.
- Model and texture sizes are appropriate for output resolution.
- A low-concurrency render succeeds before high-throughput settings are attempted.


## Pitfalls


- Forgetting a background can create transparent or black frames depending on output settings.
- High-poly models can crash headless Chrome even if they preview locally.
- Responsive CSS around a canvas can introduce one-pixel seams or scaling artifacts.
- Loading models from arbitrary user URLs creates security, CORS, and reliability problems.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- `@remotion/three`: <https://www.remotion.dev/docs/three>
- Rendering troubleshooting: <https://www.remotion.dev/docs/troubleshooting/debug-failed-render>
