# 03 - Easing

Easing changes how an animation progresses between keyframes. Linear motion moves
at a constant rate. Eased motion accelerates, decelerates, or follows a custom
curve.

In Remotion, easing is commonly used through the `easing` option of
`interpolate()`.

## Linear vs eased

Linear:

```tsx
const x = interpolate(frame, [0, 30], [0, 400], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Eased:

```tsx
import {Easing, interpolate} from 'remotion';

const x = interpolate(frame, [0, 30], [0, 400], {
  easing: Easing.out(Easing.cubic),
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

`Easing.out(Easing.cubic)` starts fast and settles smoothly.

## Common easing choices

```tsx
import {Easing} from 'remotion';

const easeOut = Easing.out(Easing.cubic);
const easeIn = Easing.in(Easing.cubic);
const easeInOut = Easing.inOut(Easing.cubic);
const custom = Easing.bezier(0.22, 1, 0.36, 1);
```

General use:

- `out`: entrances and reveals.
- `in`: exits and dismissals.
- `inOut`: camera-like moves or symmetric transitions.
- `bezier`: brand-specific motion curves.

## Eased entrance

```tsx
import {AbsoluteFill, Easing, interpolate, useCurrentFrame} from 'remotion';

export const EasedEntrance: React.FC = () => {
  const frame = useCurrentFrame();

  const y = interpolate(frame, [0, 28], [80, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 18], [0, 1], {
    easing: Easing.out(Easing.quad),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <h1 style={{opacity, transform: `translateY(${y}px)`, fontSize: 96}}>
        Eased motion
      </h1>
    </AbsoluteFill>
  );
};
```

## Eased exit

```tsx
import {Easing, interpolate, useCurrentFrame} from 'remotion';

export const ExitLeft: React.FC = () => {
  const frame = useCurrentFrame();

  const x = interpolate(frame, [60, 85], [0, -600], {
    easing: Easing.in(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <div style={{transform: `translateX(${x}px)`}}>Leaving</div>;
};
```

For exits, `Easing.in(...)` often feels natural because the element accelerates
away.

## Multi-point interpolation with easing

```tsx
const scale = interpolate(frame, [0, 20, 50], [0.8, 1.08, 1], {
  easing: Easing.out(Easing.cubic),
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

The easing applies to each active segment.

## Per-segment easing

Recent Remotion versions support an array of easing functions, one per segment.

```tsx
const value = interpolate(frame, [0, 20, 60], [0, 1.1, 1], {
  easing: [Easing.out(Easing.cubic), Easing.inOut(Easing.quad)],
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Use this when the first part of a motion should feel different from the second.

## Custom Bezier

```tsx
const brandEase = Easing.bezier(0.16, 1, 0.3, 1);

const y = interpolate(frame, [0, 32], [120, 0], {
  easing: brandEase,
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Bezier curves are useful when matching a design system's motion guidelines.

## Easing with Sequence

```tsx
import {Sequence} from 'remotion';

export const StaggeredEasing: React.FC = () => {
  return (
    <>
      <Sequence from={0}>
        <EasedLine>Plan</EasedLine>
      </Sequence>
      <Sequence from={10}>
        <EasedLine>Build</EasedLine>
      </Sequence>
      <Sequence from={20}>
        <EasedLine>Render</EasedLine>
      </Sequence>
    </>
  );
};
```

```tsx
import {Easing, interpolate, useCurrentFrame} from 'remotion';

const EasedLine: React.FC<{children: React.ReactNode}> = ({children}) => {
  const frame = useCurrentFrame();

  const x = interpolate(frame, [0, 22], [-80, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 12], [0, 1], {
    easing: Easing.out(Easing.quad),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <div style={{opacity, transform: `translateX(${x}px)`}}>{children}</div>;
};
```

Each line eases from its own local frame `0`.

## Easing vs spring

Use easing when:

- the motion should hit exact keyframes,
- you want predictable duration,
- the design calls for a specific curve,
- motion should not overshoot unless you author it.

Use spring when:

- physical motion feels better,
- slight overshoot is acceptable or desired,
- multiple properties should follow a natural driver,
- you want a less mechanical entrance.

## Practical recipe: enter, hold, leave

```tsx
import {
  AbsoluteFill,
  Easing,
  interpolate,
  useCurrentFrame,
} from 'remotion';

export const EnterHoldLeave: React.FC = () => {
  const frame = useCurrentFrame();

  const enter = interpolate(frame, [0, 28], [80, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const leave = interpolate(frame, [90, 118], [0, -80], {
    easing: Easing.in(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 16, 96, 118], [0, 1, 1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        alignItems: 'center',
        justifyContent: 'center',
        background: '#020617',
      }}
    >
      <div
        style={{
          opacity,
          transform: `translateY(${enter + leave}px)`,
          color: 'white',
          fontSize: 80,
        }}
      >
        Timed precisely
      </div>
    </AbsoluteFill>
  );
};
```

## Checklist

- Use `Easing.out(...)` for most entrances.
- Use `Easing.in(...)` for most exits.
- Use `Easing.inOut(...)` for camera-like moves.
- Clamp eased interpolations unless extension is intentional.
- Consider `spring()` for organic entrance motion.
- Keep easing curves consistent across a video package.
