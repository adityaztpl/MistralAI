# 01 - `interpolate()`

`interpolate()` maps an input value to an output value. In Remotion, the input is
often the current frame, but it can also be a spring value, a normalized progress
number, or another calculated driver.

Official docs: <https://www.remotion.dev/docs/interpolate>

## Basic fade in

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const FadeInText: React.FC = () => {
  const frame = useCurrentFrame();

  const opacity = interpolate(frame, [0, 20], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <h1 style={{opacity}}>Fade in</h1>;
};
```

This says:

```text
frame 0  -> opacity 0
frame 20 -> opacity 1
```

Frames between are linearly interpolated.

## Why clamping matters

Without clamping, values continue beyond the output range:

```tsx
const opacity = interpolate(frame, [0, 20], [0, 1]);
```

At frame `40`, this returns `2`. That is not what you want for opacity.

Clamp it:

```tsx
const opacity = interpolate(frame, [0, 20], [0, 1], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Now frames before `0` return `0`, and frames after `20` return `1`.

## Fade in, hold, fade out

Multi-point ranges are common:

```tsx
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';

export const FadeInOut: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const opacity = interpolate(
    frame,
    [0, 20, durationInFrames - 20, durationInFrames - 1],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );

  return <div style={{opacity}}>Visible in the middle</div>;
};
```

Timeline:

```text
0              20              end - 20         end - 1
opacity 0  ->  opacity 1  ->   opacity 1  ->    opacity 0
```

## Translate and scale

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const SlideAndScale: React.FC = () => {
  const frame = useCurrentFrame();

  const x = interpolate(frame, [0, 30], [-160, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const scale = interpolate(frame, [0, 30], [0.85, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        transform: `translateX(${x}px) scale(${scale})`,
      }}
    >
      Slide and scale
    </div>
  );
};
```

## Use progress instead of frames

Sometimes you want animation based on normalized progress:

```tsx
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';

export const ProgressDriven: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const progress = frame / (durationInFrames - 1);

  const y = interpolate(progress, [0, 1], [120, -120]);
  const opacity = interpolate(progress, [0, 0.1, 0.9, 1], [0, 1, 1, 0]);

  return (
    <div style={{opacity, transform: `translateY(${y}px)`}}>
      Progress {Math.round(progress * 100)}%
    </div>
  );
};
```

This makes the animation scale with composition duration.

## Interpolating a spring

The official docs show that `interpolate()` can map any driver value, including
a spring:

```tsx
import {
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const SpringMapped: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const driver = spring({frame, fps});
  const x = interpolate(driver, [0, 1], [-300, 0]);

  return <div style={{transform: `translateX(${x}px)`}}>Spring mapped</div>;
};
```

`spring()` provides the timing feel; `interpolate()` maps the `0` to `1` spring
value to the desired CSS property.

## Different extrapolation modes

`interpolate()` supports extrapolation options. The common choice is `clamp`, but
other modes exist:

```tsx
interpolate(frame, [0, 30], [0, 100], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

Conceptually:

- `extend`: continue beyond the output range.
- `clamp`: stay at the nearest output value.
- `identity`: return the input value outside the range.
- `wrap`: loop values.

Most visual properties like opacity and scale should be clamped unless a
deliberate overshoot is desired.

## Reusable helper: fade window

```ts
import {interpolate} from 'remotion';

export const fadeWindow = ({
  frame,
  fadeInStart,
  fadeInEnd,
  fadeOutStart,
  fadeOutEnd,
}: {
  frame: number;
  fadeInStart: number;
  fadeInEnd: number;
  fadeOutStart: number;
  fadeOutEnd: number;
}) => {
  return interpolate(
    frame,
    [fadeInStart, fadeInEnd, fadeOutStart, fadeOutEnd],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );
};
```

Usage:

```tsx
const opacity = fadeWindow({
  frame,
  fadeInStart: 0,
  fadeInEnd: 15,
  fadeOutStart: durationInFrames - 15,
  fadeOutEnd: durationInFrames - 1,
});
```

## Reusable component: Reveal

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

type RevealProps = {
  children: React.ReactNode;
  delay?: number;
  distance?: number;
};

export const Reveal: React.FC<RevealProps> = ({
  children,
  delay = 0,
  distance = 32,
}) => {
  const frame = useCurrentFrame() - delay;

  const opacity = interpolate(frame, [0, 16], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const y = interpolate(frame, [0, 20], [distance, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div style={{opacity, transform: `translateY(${y}px)`}}>{children}</div>
  );
};
```

Usage:

```tsx
<Reveal>
  <h1>First</h1>
</Reveal>
<Reveal delay={12}>
  <p>Second</p>
</Reveal>
```

## Interpolation with arrays of keyframes

```tsx
const rotate = interpolate(
  frame,
  [0, 12, 24, 36],
  [-4, 4, -2, 0],
  {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
);
```

This is useful for hand-authored motion curves.

## Posterized interpolation

Recent Remotion versions support posterizing an interpolation, which samples a
value every `n` frames:

```tsx
const opacity = interpolate(frame, [0, 60], [0, 1], {
  posterize: 3,
});
```

This creates stepped motion. It is useful for stop-motion effects or stylized UI.

## CSS string interpolation

Recent Remotion versions support interpolation of transform-related CSS strings
such as scale, translate, rotate, and transform origin. Check your installed
version before relying on this.

```tsx
const rotate = interpolate(frame, [0, 30], ['0deg', '90deg']);
const translate = interpolate(frame, [0, 30], ['0px 0px', '120px 40px']);

return <div style={{rotate, translate}}>CSS transform strings</div>;
```

For broad compatibility, numeric interpolation remains easy to reason about:

```tsx
const degrees = interpolate(frame, [0, 30], [0, 90]);
return <div style={{transform: `rotate(${degrees}deg)`}} />;
```

## Checklist

- Match input and output range lengths.
- Clamp properties that have natural bounds.
- Use `durationInFrames` for end-of-video fades.
- Use progress when animation should scale with duration.
- Use springs as drivers when you want physical timing.
- Extract repeated animation formulas into helpers or components.
