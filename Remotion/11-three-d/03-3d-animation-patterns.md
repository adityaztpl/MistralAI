# 3D animation patterns

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Use frame-based formulas for product spins, camera pushes, parallax, and scene transitions.

## Mental model

3D animation in Remotion works best as a timeline. You can map frame ranges to transforms, camera paths, opacity, light intensity, and material properties. Keep each motion curve readable and align scene changes with the same FPS grid used for editing.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Divide the composition into beats: reveal, hero hold, detail callout, transition, outro.
2. Use `interpolate()` and `spring()` for motion rather than hidden mutable timelines.
3. Animate the camera for large movement and the object for smaller product emphasis.
4. Use `Sequence` to keep overlays and labels synchronized with 3D beats.
5. Fade lights or materials at scene transitions to hide abrupt geometry changes.
6. Render stills for every beat boundary and a full render for motion feel.


## Example


```tsx
import {interpolate, spring, useCurrentFrame, useVideoConfig, Sequence} from 'remotion';

export const useHeroCamera = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const push = spring({frame, fps, config: {damping: 18, stiffness: 90}});
  return {
    z: interpolate(push, [0, 1], [7, 4.6]),
    y: interpolate(frame, [0, 120], [1.5, 0.8], {extrapolateRight: 'clamp'}),
  };
};

const ProductSpin = () => {
  const frame = useCurrentFrame();
  const spin = interpolate(frame, [0, 150], [-0.4, Math.PI * 2.2]);
  const bob = Math.sin(frame / 12) * 0.08;
  return <mesh rotation={[0.2, spin, 0]} position={[0, bob, 0]} />;
};

export const ThreePromoTimeline = () => (
  <>
    <ProductSpin />
    <Sequence from={45} durationInFrames={90}>
      <div className="callout">Machined aluminum body</div>
    </Sequence>
  </>
);
```


## Checklist


- Motion curves have named frame ranges and do not rely on magic values only.
- Camera moves do not cause motion sickness or clip through objects.
- Text overlays are sequenced with 3D events.
- The scene has deliberate holds so viewers can understand the product.
- Motion is checked at the final platform aspect ratio.


## Pitfalls


- Continuous fast spins can create encoding artifacts and make product details unreadable.
- Animating everything at once reduces visual hierarchy.
- Hard cuts between 3D scenes can expose loading or lighting differences.
- A beautiful Studio preview can still be too slow for batch rendering if shadows and postprocessing are unchecked.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- `@remotion/three`: <https://www.remotion.dev/docs/three>
- `spring()`: <https://www.remotion.dev/docs/spring>
- `Sequence`: <https://www.remotion.dev/docs/sequence>
