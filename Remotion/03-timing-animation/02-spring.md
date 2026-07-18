# 02 - `spring()`

`spring()` is Remotion's physics-based animation primitive. It produces motion
that feels like a mass attached to a spring, often moving from `0` to `1` with
natural acceleration, deceleration, and optional overshoot.

Official docs: <https://www.remotion.dev/docs/spring>

## Basic spring

```tsx
import {spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const SpringScale: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const scale = spring({
    frame,
    fps,
  });

  return <div style={{transform: `scale(${scale})`}}>Pop in</div>;
};
```

## Spring mapped through interpolate

Because `spring()` generally returns a driver value from `0` to `1`, pair it
with `interpolate()` for custom ranges:

```tsx
import {
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const SpringSlide: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const progress = spring({
    frame,
    fps,
    config: {
      damping: 14,
      stiffness: 120,
    },
  });

  const x = interpolate(progress, [0, 1], [-400, 0]);
  const opacity = interpolate(progress, [0, 1], [0, 1]);

  return (
    <div style={{opacity, transform: `translateX(${x}px)`}}>
      Spring slide
    </div>
  );
};
```

## Spring parameters

Important options include:

- `frame`: usually `useCurrentFrame()`.
- `fps`: usually from `useVideoConfig()`.
- `from`: start value, default `0`.
- `to`: end value, default `1`.
- `config.mass`: spring mass.
- `config.damping`: how strongly it slows down.
- `config.stiffness`: spring stiffness.
- `config.overshootClamping`: whether to prevent overshooting.
- `durationInFrames`: stretch spring to a duration.
- `delay`: delay the spring by a number of frames.
- `reverse`: reverse the spring.

## Tuning examples

### Bouncy

```tsx
const progress = spring({
  frame,
  fps,
  config: {
    damping: 8,
    stiffness: 130,
    mass: 1,
  },
});
```

### Snappy but controlled

```tsx
const progress = spring({
  frame,
  fps,
  config: {
    damping: 16,
    stiffness: 180,
    mass: 0.8,
  },
});
```

### No overshoot

```tsx
const progress = spring({
  frame,
  fps,
  config: {
    damping: 100,
    overshootClamping: true,
  },
});
```

## Delaying a spring

You can delay by subtracting from the frame:

```tsx
const progress = spring({
  frame: frame - 20,
  fps,
});
```

Or use the `delay` option in Remotion versions that support it:

```tsx
const progress = spring({
  frame,
  fps,
  delay: 20,
});
```

## Duration-controlled spring

Use `durationInFrames` when the motion must finish around a specific time:

```tsx
const progress = spring({
  frame,
  fps,
  durationInFrames: 36,
  config: {
    damping: 14,
    stiffness: 120,
  },
});
```

This is useful when scenes must fit a strict timeline.

## Spring from one value to another

```tsx
const scale = spring({
  frame,
  fps,
  from: 0.8,
  to: 1,
  config: {damping: 12},
});
```

Or keep `spring()` as a normalized driver and interpolate:

```tsx
const driver = spring({frame, fps});
const scale = interpolate(driver, [0, 1], [0.8, 1]);
```

The second pattern is often clearer when multiple properties share one driver.

## One spring, many properties

```tsx
import {
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const SpringCard: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const progress = spring({
    frame,
    fps,
    config: {damping: 15, stiffness: 150},
  });

  const y = interpolate(progress, [0, 1], [90, 0]);
  const scale = interpolate(progress, [0, 1], [0.94, 1]);
  const opacity = interpolate(progress, [0, 1], [0, 1]);

  return (
    <div
      style={{
        opacity,
        transform: `translateY(${y}px) scale(${scale})`,
        padding: 48,
        borderRadius: 28,
        background: '#2563eb',
        color: 'white',
        fontSize: 48,
        fontWeight: 800,
      }}
    >
      One spring drives everything
    </div>
  );
};
```

## Spring inside Sequence

Because `useCurrentFrame()` is local inside sequences, a spring can start at
frame `0` wherever the sequence begins.

```tsx
import {Sequence} from 'remotion';

export const StaggeredCards: React.FC = () => {
  return (
    <>
      <Sequence from={0}>
        <SpringCard />
      </Sequence>
      <Sequence from={12}>
        <SpringCard />
      </Sequence>
      <Sequence from={24}>
        <SpringCard />
      </Sequence>
    </>
  );
};
```

Each `SpringCard` uses the same local spring and enters at a different global
time.

## Reverse spring

```tsx
const reverseProgress = spring({
  frame,
  fps,
  reverse: true,
  durationInFrames: 30,
});
```

This is useful for exits if you want symmetry with an entrance.

## Full example: spring hero

```tsx
import {
  AbsoluteFill,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const SpringHero: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const hero = spring({
    frame,
    fps,
    config: {damping: 13, stiffness: 125, mass: 0.9},
  });

  const titleY = interpolate(hero, [0, 1], [70, 0]);
  const glowScale = interpolate(hero, [0, 1], [0.5, 1]);

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        overflow: 'hidden',
      }}
    >
      <div
        style={{
          position: 'absolute',
          width: 680,
          height: 680,
          borderRadius: 999,
          background: 'rgba(37, 99, 235, 0.38)',
          filter: 'blur(80px)',
          transform: `scale(${glowScale})`,
        }}
      />
      <h1
        style={{
          position: 'relative',
          fontSize: 112,
          transform: `translateY(${titleY}px)`,
          margin: 0,
        }}
      >
        Physical motion
      </h1>
    </AbsoluteFill>
  );
};
```

## Checklist

- Always pass the composition `fps`.
- Use `frame - delay` or `delay` to start later.
- Use a single spring as a shared driver for related properties.
- Increase `damping` for less bounce.
- Use `durationInFrames` when timing must be exact.
- Use `overshootClamping` or high damping when overshoot is undesirable.
