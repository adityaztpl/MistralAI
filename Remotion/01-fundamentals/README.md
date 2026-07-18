# 01 - Fundamentals

This section explains the core Remotion mental model: video is React evaluated
at a frame number. Once that clicks, the rest of the API becomes easier to
predict.

## Files in this section

1. `01-what-is-remotion.md` - conceptual model and how React becomes video.
2. `02-frames-fps-duration.md` - frame math, FPS, duration, and time conversion.
3. `03-project-structure.md` - typical generated project layout.
4. `04-absolute-fill-and-layout.md` - layout primitives and full-frame layers.
5. `05-hooks-useCurrentFrame-useVideoConfig.md` - core hooks for frame and
   composition information.
6. `06-first-animation.md` - a guided first animation using official primitives.

## The three fundamentals

### 1. The frame is the source of truth

In Remotion, the current moment is represented by an integer frame:

```tsx
import {useCurrentFrame} from 'remotion';

export const FrameReadout: React.FC = () => {
  const frame = useCurrentFrame();

  return <div>Current frame: {frame}</div>;
};
```

Frame numbers are zero-indexed. In a 90-frame composition, frames run from `0`
through `89`.

### 2. Composition config defines the canvas

A video component becomes renderable when it is registered:

```tsx
import {Composition} from 'remotion';
import {FrameReadout} from './FrameReadout';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="FrameReadout"
      component={FrameReadout}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={90}
    />
  );
};
```

The values `width`, `height`, `fps`, and `durationInFrames` define the render
target. Components can read them with `useVideoConfig()`.

### 3. Animation is pure mapping

An animation usually maps the current frame to a style value:

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const FadeIn: React.FC = () => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 30], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <div style={{opacity}}>Hello</div>;
};
```

The same frame always produces the same opacity. That determinism is what makes
Remotion renderable.

## Minimal composition using fundamentals

```tsx
import {
  AbsoluteFill,
  interpolate,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export const FundamentalsDemo: React.FC = () => {
  const frame = useCurrentFrame();
  const {width, height, fps, durationInFrames} = useVideoConfig();

  const progress = frame / (durationInFrames - 1);
  const x = interpolate(progress, [0, 1], [80, width - 280]);

  return (
    <AbsoluteFill
      style={{
        background: '#101827',
        color: 'white',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <div style={{padding: 64, fontSize: 36}}>
        {width}x{height} at {fps} FPS - frame {frame}
      </div>

      <div
        style={{
          position: 'absolute',
          top: height / 2 - 60,
          left: x,
          width: 200,
          height: 120,
          borderRadius: 24,
          background: '#38bdf8',
          display: 'grid',
          placeItems: 'center',
          color: '#082f49',
          fontWeight: 800,
        }}
      >
        {Math.round(progress * 100)}%
      </div>
    </AbsoluteFill>
  );
};
```

## Checklist for beginners

- Does every renderable scene have a `<Composition>` entry?
- Are frame counts derived from `fps` when the timing is in seconds?
- Are animations deterministic from `frame`, props, and static assets?
- Are full-screen layers using `<AbsoluteFill>` where appropriate?
- Are `interpolate()` calls clamped when values should not continue growing?
- Are sequences used for scene offsets instead of scattering magic frame numbers?
