# 06 - First Animation

This walkthrough builds a small animated title card using:

- `<Composition>`
- `<AbsoluteFill>`
- `useCurrentFrame()`
- `useVideoConfig()`
- `interpolate()`
- `spring()`
- `<Sequence>`

## Goal

Create a 5-second 1080p video:

1. background gradient is visible for the whole duration,
2. title scales in with a spring,
3. subtitle fades up after a short delay,
4. a call-to-action pill slides in near the end,
5. the whole scene fades out over the final half second.

At 30 FPS, 5 seconds is 150 frames.

## Register the composition

```tsx
// src/Root.tsx
import {Composition} from 'remotion';
import {FirstAnimation} from './FirstAnimation';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="FirstAnimation"
      component={FirstAnimation}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={150}
      defaultProps={{
        title: 'Build video with React',
        subtitle: 'Frame-accurate motion, typed props, real components',
        cta: 'Start rendering',
      }}
    />
  );
};
```

## Write the component

```tsx
// src/FirstAnimation.tsx
import {
  AbsoluteFill,
  Sequence,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type FirstAnimationProps = {
  title: string;
  subtitle: string;
  cta: string;
};

export const FirstAnimation: React.FC<FirstAnimationProps> = ({
  title,
  subtitle,
  cta,
}) => {
  const frame = useCurrentFrame();
  const {fps, durationInFrames} = useVideoConfig();

  const titleScale = spring({
    frame,
    fps,
    config: {
      damping: 12,
      stiffness: 120,
      mass: 0.8,
    },
  });

  const subtitleOpacity = interpolate(frame, [18, 34], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const sceneOpacity = interpolate(
    frame,
    [durationInFrames - fps * 0.5, durationInFrames - 1],
    [1, 0],
    {
      extrapolateLeft: 'clamp',
      extrapolateRight: 'clamp',
    },
  );

  return (
    <AbsoluteFill
      style={{
        opacity: sceneOpacity,
        background:
          'radial-gradient(circle at 50% 35%, #1d4ed8 0%, #020617 55%)',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1
        style={{
          margin: 0,
          fontSize: 112,
          lineHeight: 1,
          letterSpacing: -4,
          transform: `scale(${titleScale})`,
        }}
      >
        {title}
      </h1>

      <p
        style={{
          opacity: subtitleOpacity,
          marginTop: 30,
          marginBottom: 0,
          fontSize: 34,
          color: '#bfdbfe',
        }}
      >
        {subtitle}
      </p>

      <Sequence from={90} durationInFrames={45}>
        <CtaPill>{cta}</CtaPill>
      </Sequence>
    </AbsoluteFill>
  );
};

const CtaPill: React.FC<{children: React.ReactNode}> = ({children}) => {
  const frame = useCurrentFrame();

  const y = interpolate(frame, [0, 18], [48, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{alignItems: 'center', justifyContent: 'flex-end'}}>
      <div
        style={{
          marginBottom: 130,
          transform: `translateY(${y}px)`,
          opacity,
          padding: '18px 34px',
          borderRadius: 999,
          background: 'white',
          color: '#1e3a8a',
          fontSize: 30,
          fontWeight: 800,
          boxShadow: '0 24px 80px rgba(0, 0, 0, 0.35)',
        }}
      >
        {children}
      </div>
    </AbsoluteFill>
  );
};
```

## Why this works

### Title spring

```tsx
const titleScale = spring({
  frame,
  fps,
  config: {
    damping: 12,
    stiffness: 120,
    mass: 0.8,
  },
});
```

`spring()` produces a value that generally moves from `0` to `1`. Applying it to
CSS scale gives a physical pop-in.

### Subtitle interpolation

```tsx
const subtitleOpacity = interpolate(frame, [18, 34], [0, 1], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Frames `0` through `18` stay at opacity `0`. Frames `18` through `34` fade from
`0` to `1`. After frame `34`, clamping keeps opacity at `1`.

### Sequence-local CTA animation

The CTA starts at composition frame `90`:

```tsx
<Sequence from={90} durationInFrames={45}>
  <CtaPill>{cta}</CtaPill>
</Sequence>
```

Inside `CtaPill`, `useCurrentFrame()` starts at `0` when the sequence starts.
That makes the CTA component reusable:

```tsx
const frame = useCurrentFrame();
const y = interpolate(frame, [0, 18], [48, 0], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

No CTA-specific code needs to know it begins at global frame `90`.

## Render it

```bash
npx remotion render src/index.ts FirstAnimation out/first-animation.mp4
```

## Variant: use Series for multiple beats

If the animation grows into multiple scenes, use `<Series>`:

```tsx
import {Series} from 'remotion';

export const ThreeBeatVideo: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={60}>
        <OpeningBeat />
      </Series.Sequence>
      <Series.Sequence durationInFrames={75}>
        <FeatureBeat />
      </Series.Sequence>
      <Series.Sequence durationInFrames={45}>
        <CtaBeat />
      </Series.Sequence>
    </Series>
  );
};
```

## Variant: animate multiple properties

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const MultiPropertyBox: React.FC = () => {
  const frame = useCurrentFrame();

  const opacity = interpolate(frame, [0, 20], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const x = interpolate(frame, [0, 30], [-100, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const rotate = interpolate(frame, [0, 30], [-8, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        opacity,
        transform: `translateX(${x}px) rotate(${rotate}deg)`,
      }}
    >
      Animated box
    </div>
  );
};
```

## Debugging your first animation

- If nothing appears, confirm the composition ID and render command match.
- If an animation keeps moving forever, add `extrapolateRight: 'clamp'`.
- If a child animation starts at the wrong frame, check whether it is inside a
  `<Sequence>`.
- If timing feels tied to 30 FPS, derive frame counts from `fps`.
- If the render differs from preview, check async loading and deterministic
  values.

## Complete checklist

```text
[x] Composition registered
[x] Duration expressed in frames
[x] Frame read with useCurrentFrame()
[x] Config read with useVideoConfig()
[x] Full-frame layout through AbsoluteFill
[x] Property mapping through interpolate()
[x] Physical entrance through spring()
[x] Delayed subtree through Sequence
```
