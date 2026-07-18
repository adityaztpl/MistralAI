# 02 - Frames, FPS, and Duration

Remotion time is frame-based. The composition does not ask "what time is it in
milliseconds?" It asks "what frame is being rendered?"

## Frame numbering

Frames are zero-indexed:

```text
durationInFrames = 90
valid frames      = 0 through 89
```

The official `useCurrentFrame()` docs state that the first frame is `0` and the
last frame is `durationInFrames - 1`.

```tsx
import {useCurrentFrame, useVideoConfig} from 'remotion';

export const FrameBounds: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const isFirstFrame = frame === 0;
  const isLastFrame = frame === durationInFrames - 1;

  return (
    <div>
      {isFirstFrame && 'First frame'}
      {isLastFrame && 'Last frame'}
      {!isFirstFrame && !isLastFrame && `Frame ${frame}`}
    </div>
  );
};
```

## FPS means frames per second

If a composition has `fps={30}`, then:

```text
1 second  = 30 frames
2 seconds = 60 frames
5 seconds = 150 frames
```

If it has `fps={60}`, then:

```text
1 second  = 60 frames
2 seconds = 120 frames
5 seconds = 300 frames
```

Prefer deriving frame counts from `fps` when the intention is time in seconds:

```tsx
import {useVideoConfig} from 'remotion';

export const TimingConstants: React.FC = () => {
  const {fps} = useVideoConfig();

  const introFrames = 2 * fps;
  const titleFadeFrames = Math.round(0.5 * fps);
  const holdFrames = 3 * fps;

  return (
    <pre>
      {JSON.stringify({introFrames, titleFadeFrames, holdFrames}, null, 2)}
    </pre>
  );
};
```

## Duration belongs to compositions and sequences

Composition duration:

```tsx
<Composition
  id="Explainer"
  component={Explainer}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={8 * 30}
/>
```

Sequence duration:

```tsx
import {Sequence} from 'remotion';

export const Explainer: React.FC = () => {
  return (
    <>
      <Sequence from={0} durationInFrames={60}>
        <Intro />
      </Sequence>
      <Sequence from={60} durationInFrames={120}>
        <MainPoint />
      </Sequence>
      <Sequence from={180} durationInFrames={60}>
        <Outro />
      </Sequence>
    </>
  );
};
```

The component inside a `<Sequence>` is mounted only while the current frame is
within that sequence's time range. It also receives a shifted frame from
`useCurrentFrame()`.

## Local frame vs absolute frame

Inside a sequence starting at frame 50, `useCurrentFrame()` returns `0` at the
moment the sequence starts.

```tsx
import {Sequence, useCurrentFrame} from 'remotion';

const Child: React.FC = () => {
  const localFrame = useCurrentFrame();
  return <div>Local frame: {localFrame}</div>;
};

export const Parent: React.FC = () => {
  const absoluteFrame = useCurrentFrame();

  return (
    <>
      <div>Absolute frame: {absoluteFrame}</div>
      <Sequence from={50} durationInFrames={40}>
        <Child />
      </Sequence>
    </>
  );
};
```

At composition frame `50`, the parent sees `50`, while `Child` sees `0`.

If a child needs the global frame, pass it explicitly:

```tsx
type ChildProps = {
  absoluteFrame: number;
};

const Child: React.FC<ChildProps> = ({absoluteFrame}) => {
  const localFrame = useCurrentFrame();

  return (
    <div>
      Absolute {absoluteFrame}, local {localFrame}
    </div>
  );
};

export const Parent: React.FC = () => {
  const absoluteFrame = useCurrentFrame();

  return (
    <Sequence from={50}>
      <Child absoluteFrame={absoluteFrame} />
    </Sequence>
  );
};
```

## Time conversion helpers

Small helpers make code easier to read:

```ts
export const secondsToFrames = (seconds: number, fps: number): number => {
  return Math.round(seconds * fps);
};

export const framesToSeconds = (frames: number, fps: number): number => {
  return frames / fps;
};
```

Usage:

```tsx
import {Sequence, useVideoConfig} from 'remotion';
import {secondsToFrames} from './timing';

export const SceneTimeline: React.FC = () => {
  const {fps} = useVideoConfig();

  return (
    <>
      <Sequence
        from={secondsToFrames(0, fps)}
        durationInFrames={secondsToFrames(1.5, fps)}
      >
        <IntroCard />
      </Sequence>

      <Sequence
        from={secondsToFrames(1.5, fps)}
        durationInFrames={secondsToFrames(4, fps)}
      >
        <FeatureDemo />
      </Sequence>
    </>
  );
};
```

## Progress values

Many animations become simpler if you compute normalized progress from `0` to
`1`.

```tsx
import {interpolate, useCurrentFrame, useVideoConfig} from 'remotion';

export const ProgressBar: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames, width} = useVideoConfig();

  const progress = frame / (durationInFrames - 1);
  const barWidth = interpolate(progress, [0, 1], [0, width]);

  return (
    <div
      style={{
        position: 'absolute',
        bottom: 0,
        left: 0,
        height: 12,
        width: barWidth,
        background: '#22c55e',
      }}
    />
  );
};
```

When you use progress, remember that the final valid frame is
`durationInFrames - 1`, so dividing by `durationInFrames - 1` gives exactly `1`
on the last frame.

## Common timing mistakes

### Mistake: using seconds where frames are expected

```tsx
// Wrong: this lasts 4 frames, not 4 seconds.
<Sequence durationInFrames={4}>
  <Intro />
</Sequence>
```

Correct:

```tsx
const {fps} = useVideoConfig();

<Sequence durationInFrames={4 * fps}>
  <Intro />
</Sequence>;
```

### Mistake: forgetting to clamp interpolation

```tsx
// Keeps growing after frame 20.
const scale = interpolate(frame, [0, 20], [0, 1]);
```

Correct:

```tsx
const scale = interpolate(frame, [0, 20], [0, 1], {
  extrapolateLeft: 'clamp',
  extrapolateRight: 'clamp',
});
```

### Mistake: calculating all scene starts manually

Use `<Series>` when scenes play sequentially:

```tsx
import {Series} from 'remotion';

export const EasierTimeline: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={45}>
        <Intro />
      </Series.Sequence>
      <Series.Sequence durationInFrames={120}>
        <Demo />
      </Series.Sequence>
      <Series.Sequence durationInFrames={60}>
        <Outro />
      </Series.Sequence>
    </Series>
  );
};
```

## Timing debug overlay

During development, a frame overlay can save time:

```tsx
import {useCurrentFrame, useVideoConfig} from 'remotion';

export const DebugTimecode: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps, durationInFrames} = useVideoConfig();
  const seconds = frame / fps;

  return (
    <div
      style={{
        position: 'absolute',
        top: 16,
        right: 16,
        padding: '8px 12px',
        borderRadius: 8,
        background: 'rgba(0, 0, 0, 0.65)',
        color: 'white',
        fontFamily: 'monospace',
        fontSize: 20,
      }}
    >
      {frame}/{durationInFrames - 1} ({seconds.toFixed(2)}s)
    </div>
  );
};
```
