# 05 - Hooks: useCurrentFrame() and useVideoConfig()

Two hooks appear in almost every Remotion project:

- `useCurrentFrame()` tells you where you are in the timeline.
- `useVideoConfig()` tells you the current composition or sequence dimensions,
  FPS, duration, ID, and props-related metadata.

Together, they let a React component adapt to both time and canvas.

## useCurrentFrame()

`useCurrentFrame()` returns the current frame number.

```tsx
import {useCurrentFrame} from 'remotion';

export const FrameNumber: React.FC = () => {
  const frame = useCurrentFrame();

  return <div>Frame: {frame}</div>;
};
```

Frame numbers start at `0`. If the composition lasts `300` frames, the final
frame is `299`.

## useCurrentFrame() inside Sequence

The official docs highlight an important behavior: inside a `<Sequence>`, the
frame is relative to that sequence.

```tsx
import {Sequence, useCurrentFrame} from 'remotion';

const Label: React.FC<{name: string}> = ({name}) => {
  const frame = useCurrentFrame();
  return (
    <div>
      {name}: {frame}
    </div>
  );
};

export const SequenceFrameExample: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <>
      <Label name="Top level" />
      <Sequence from={30}>
        <Label name="Inside sequence" />
      </Sequence>
      <div>Absolute frame from parent: {frame}</div>
    </>
  );
};
```

At composition frame `30`, the top-level label sees `30`, while the label inside
the sequence sees `0`.

## Passing absolute frame into a sequence

Sometimes a child needs both local and absolute time:

```tsx
import {Sequence, useCurrentFrame} from 'remotion';

type AnimatedCaptionProps = {
  absoluteFrame: number;
};

const AnimatedCaption: React.FC<AnimatedCaptionProps> = ({absoluteFrame}) => {
  const localFrame = useCurrentFrame();

  return (
    <div>
      Local frame: {localFrame}, absolute frame: {absoluteFrame}
    </div>
  );
};

export const ParentTimeline: React.FC = () => {
  const absoluteFrame = useCurrentFrame();

  return (
    <Sequence from={45} durationInFrames={90}>
      <AnimatedCaption absoluteFrame={absoluteFrame} />
    </Sequence>
  );
};
```

This avoids relying on hidden global state.

## useVideoConfig()

`useVideoConfig()` returns configuration for the current context:

```tsx
import {useVideoConfig} from 'remotion';

export const ConfigReadout: React.FC = () => {
  const {width, height, fps, durationInFrames, id} = useVideoConfig();

  return (
    <pre>
      {JSON.stringify({id, width, height, fps, durationInFrames}, null, 2)}
    </pre>
  );
};
```

The official docs describe properties such as:

- `width`
- `height`
- `fps`
- `durationInFrames`
- `id`
- `defaultProps`
- `props`
- default render settings such as codec/sample rate in newer versions

## useVideoConfig() inside Sequence

When a sequence defines its own dimensions or duration, child components see the
sequence context.

```tsx
import {AbsoluteFill, Sequence, useVideoConfig} from 'remotion';

const Child: React.FC = () => {
  const {width, height, durationInFrames} = useVideoConfig();

  return (
    <div>
      Child sees {width}x{height}, duration {durationInFrames}
    </div>
  );
};

export const SequenceConfigExample: React.FC = () => {
  return (
    <AbsoluteFill>
      <Sequence width={640} height={360} durationInFrames={90}>
        <Child />
      </Sequence>
    </AbsoluteFill>
  );
};
```

This is useful for reusable components designed for a specific mini-canvas.

## Hook-driven animation

The canonical pattern is:

1. read the frame,
2. read the composition config,
3. derive style values,
4. render markup.

```tsx
import {
  AbsoluteFill,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const HookDrivenCard: React.FC = () => {
  const frame = useCurrentFrame();
  const {width, durationInFrames} = useVideoConfig();

  const x = interpolate(frame, [0, 40], [-width, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(
    frame,
    [0, 20, durationInFrames - 20, durationInFrames],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          transform: `translateX(${x}px)`,
          opacity,
          width: 720,
          padding: 48,
          borderRadius: 32,
          background: '#2563eb',
          color: 'white',
          fontSize: 54,
          fontWeight: 800,
          textAlign: 'center',
        }}
      >
        Hooks make time explicit
      </div>
    </AbsoluteFill>
  );
};
```

## Hook usage in smaller components

It is fine for small components to read Remotion hooks if their behavior is
intrinsically time-based.

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

type PulsingDotProps = {
  color: string;
  delay: number;
};

export const PulsingDot: React.FC<PulsingDotProps> = ({color, delay}) => {
  const frame = useCurrentFrame();
  const phase = (frame - delay) % 45;

  const scale = interpolate(phase, [0, 22, 45], [0.8, 1.2, 0.8], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        width: 18,
        height: 18,
        borderRadius: 999,
        background: color,
        transform: `scale(${scale})`,
      }}
    />
  );
};
```

For purely presentational components, pass derived values as props:

```tsx
type MetricCardProps = {
  label: string;
  value: string;
  opacity: number;
};

export const MetricCard: React.FC<MetricCardProps> = ({
  label,
  value,
  opacity,
}) => {
  return (
    <div style={{opacity}}>
      <div>{label}</div>
      <strong>{value}</strong>
    </div>
  );
};
```

## Reading props from useVideoConfig()

`useVideoConfig()` can expose composition props after Remotion transformations.
Most of the time, use typed React props directly. But config access can be useful
for generic debugging or infrastructure-level components.

```tsx
import {useVideoConfig} from 'remotion';

export const PropsDebug: React.FC = () => {
  const {props, defaultProps} = useVideoConfig();

  return (
    <pre
      style={{
        position: 'absolute',
        left: 20,
        bottom: 20,
        color: 'white',
        background: 'rgba(0,0,0,0.7)',
        padding: 16,
      }}
    >
      {JSON.stringify({props, defaultProps}, null, 2)}
    </pre>
  );
};
```

## Pattern: frame windows

Small helper functions can make hook usage easier:

```ts
export const isFrameBetween = (
  frame: number,
  start: number,
  endExclusive: number,
): boolean => {
  return frame >= start && frame < endExclusive;
};
```

```tsx
import {useCurrentFrame} from 'remotion';
import {isFrameBetween} from './timing';

export const ConditionalLayers: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <>
      {isFrameBetween(frame, 0, 60) && <Intro />}
      {isFrameBetween(frame, 60, 150) && <Demo />}
      {isFrameBetween(frame, 150, 210) && <Outro />}
    </>
  );
};
```

Prefer `<Sequence>` for most timeline mounting, but helpers like this are useful
inside individual scenes.

## Checklist

- Use `useCurrentFrame()` inside renderable Remotion components, not outside
  React.
- Remember that frame values are local inside `<Sequence>`.
- Use `useVideoConfig()` instead of duplicating `fps`, `width`, or duration.
- Clamp interpolation values when an animation should stop.
- Keep hook-derived calculations deterministic.
