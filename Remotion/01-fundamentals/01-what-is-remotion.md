# 01 - What is Remotion?

Remotion is a way to create video with React. You write components, register
them as compositions, preview them in Remotion Studio, and render them into media
files. The key difference from normal web React is that the component is
evaluated at a specific frame number.

## React, but the clock is a frame

In a normal web app, state changes over time because events happen: users click,
timers fire, requests finish. In Remotion, the render engine asks:

```text
What should this component look like at frame 0?
What should this component look like at frame 1?
What should this component look like at frame 2?
...
```

Your component answers by reading `useCurrentFrame()`:

```tsx
import {useCurrentFrame} from 'remotion';

export const Counter: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <div style={{fontSize: 80, fontFamily: 'monospace'}}>
      Frame {frame}
    </div>
  );
};
```

This means animation should generally be expressed as a pure function of:

- the current frame,
- composition configuration,
- input props,
- assets that have loaded,
- deterministic calculations.

## A composition is the render target

React components are not renderable by Remotion until you register them with
`<Composition>`.

```tsx
import {Composition} from 'remotion';
import {Counter} from './Counter';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="Counter"
        component={Counter}
        width={1280}
        height={720}
        fps={30}
        durationInFrames={120}
      />
    </>
  );
};
```

`id` is the stable identifier shown in Studio and passed to rendering commands.
The other values define the output canvas and timing.

## Why this model is powerful

Because every frame is deterministic, Remotion works well for video systems that
need repeatability:

- Generate 10,000 personalized videos from a CSV.
- Render product videos from database rows.
- Build a browser-based editor with live React previews.
- Turn analytics into animated reports.
- Reuse a web design system in video templates.
- Make tests for individual scene components.

## A real scene skeleton

```tsx
import {
  AbsoluteFill,
  Sequence,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type ProductSceneProps = {
  name: string;
  price: string;
  accentColor: string;
};

export const ProductScene: React.FC<ProductSceneProps> = ({
  name,
  price,
  accentColor,
}) => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const titleOpacity = interpolate(frame, [0, 20], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const priceY = interpolate(frame, [30, 55], [40, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const sceneOpacity = interpolate(
    frame,
    [durationInFrames - 20, durationInFrames],
    [1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );

  return (
    <AbsoluteFill
      style={{
        opacity: sceneOpacity,
        background: '#0f172a',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1
        style={{
          opacity: titleOpacity,
          fontSize: 96,
          margin: 0,
          color: accentColor,
        }}
      >
        {name}
      </h1>

      <Sequence from={30}>
        <div
          style={{
            transform: `translateY(${priceY}px)`,
            fontSize: 52,
            marginTop: 28,
          }}
        >
          Starting at {price}
        </div>
      </Sequence>
    </AbsoluteFill>
  );
};
```

Notice how there is no `setTimeout()`, no `requestAnimationFrame()`, and no
mutable animation state. Frame math drives the result.

## What Remotion is not

Remotion is not primarily a drag-and-drop video editor. Studio helps with
previewing and timeline inspection, but the source of truth is code. That is a
benefit for developers and automation-heavy teams, but it means the workflow is
different from After Effects, Premiere, Final Cut, or Canva-style editors.

## Determinism rules of thumb

Good Remotion component:

```tsx
const opacity = interpolate(frame, [0, 20], [0, 1], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Risky Remotion component:

```tsx
const opacity = Math.random();
```

If you need randomness, seed it deterministically:

```tsx
import {random, useCurrentFrame} from 'remotion';

export const Stars: React.FC = () => {
  const frame = useCurrentFrame();

  const stars = new Array(50).fill(true).map((_, index) => {
    const x = random(`star-x-${index}`) * 100;
    const y = random(`star-y-${index}`) * 100;
    const twinkle = random(`star-phase-${index}`) * 20;

    return {x, y, opacity: ((frame + twinkle) % 40) / 40};
  });

  return (
    <div>
      {stars.map((star, index) => (
        <div
          key={index}
          style={{
            position: 'absolute',
            left: `${star.x}%`,
            top: `${star.y}%`,
            opacity: star.opacity,
            width: 4,
            height: 4,
            borderRadius: 999,
            background: 'white',
          }}
        />
      ))}
    </div>
  );
};
```

`random()` from Remotion gives stable pseudo-random values for the same seed.

## The core vocabulary

- **Composition**: A registered render target.
- **Frame**: The current integer moment in the timeline.
- **FPS**: Frames per second, used to convert seconds into frames.
- **Duration**: The number of frames in a composition or sequence.
- **AbsoluteFill**: A full-frame absolutely positioned container.
- **Sequence**: A way to time-shift or trim a subtree.
- **Series**: A helper for playing sequences one after another.
- **interpolate()**: Maps an input range to an output range.
- **spring()**: Produces physics-like animation values.
- **Still**: A static image render target.
