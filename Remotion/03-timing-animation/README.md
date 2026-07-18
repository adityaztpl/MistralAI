# 03 - Timing and Animation

Remotion animation is usually a pure calculation:

```text
frame + config + props -> visual state
```

This section covers the core timing and animation tools:

- `interpolate()`
- `spring()`
- `Easing`
- `interpolateColors()`
- CSS transforms and motion patterns
- `delayRender()`, `continueRender()`, and prefetching

## Section map

1. `01-interpolate.md` - map frame ranges to values.
2. `02-spring.md` - physics-based motion.
3. `03-easing.md` - nonlinear timing curves.
4. `04-interpolate-colors.md` - color transitions.
5. `05-transforms-and-motion.md` - practical motion recipes.
6. `06-delay-render-and-prefetch.md` - async readiness and media/data loading.

## The essential pattern

```tsx
import {
  AbsoluteFill,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const TimingPattern: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const opacity = interpolate(
    frame,
    [0, 20, durationInFrames - 20, durationInFrames - 1],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );

  return (
    <AbsoluteFill style={{opacity, background: '#020617', color: 'white'}}>
      Hello
    </AbsoluteFill>
  );
};
```

## Animation vocabulary

- **Input range**: frames or progress values you expect.
- **Output range**: CSS values you want.
- **Clamping**: preventing values from extending beyond the range.
- **Easing**: changing the pacing within a range.
- **Spring**: a physics-style value, usually from `0` to `1`.
- **Sequence**: starts a child subtree at a different local frame.
- **Series**: places sequences one after another.

## Use frame-local animation components

Because `useCurrentFrame()` is sequence-relative, small animation components can
be reused anywhere:

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const SlideUp: React.FC<{children: React.ReactNode}> = ({children}) => {
  const frame = useCurrentFrame();

  const y = interpolate(frame, [0, 20], [40, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div style={{transform: `translateY(${y}px)`, opacity}}>{children}</div>
  );
};
```

Use it at different times:

```tsx
import {Sequence} from 'remotion';

export const ReusedAnimation: React.FC = () => {
  return (
    <>
      <Sequence from={0}>
        <SlideUp>First line</SlideUp>
      </Sequence>
      <Sequence from={30}>
        <SlideUp>Second line</SlideUp>
      </Sequence>
    </>
  );
};
```

Each `SlideUp` starts at local frame `0`.
