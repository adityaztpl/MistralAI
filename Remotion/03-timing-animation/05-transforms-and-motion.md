# 05 - Transforms and Motion

Most Remotion animation ends up as CSS transforms, opacity, color, filters, SVG
attributes, or layout values. Transforms are especially useful because they are
expressive and familiar to web developers.

## Transform basics

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const TransformBasics: React.FC = () => {
  const frame = useCurrentFrame();

  const x = interpolate(frame, [0, 30], [-120, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const scale = interpolate(frame, [0, 30], [0.9, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const rotate = interpolate(frame, [0, 30], [-6, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        transform: `translateX(${x}px) scale(${scale}) rotate(${rotate}deg)`,
      }}
    >
      Transform me
    </div>
  );
};
```

Order matters in CSS transforms. `translateX(...) scale(...)` is not always the
same as `scale(...) translateX(...)`.

## Slide in from a side

```tsx
import {Easing, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';

export const SlideFromLeft: React.FC = () => {
  const frame = useCurrentFrame();
  const {width} = useVideoConfig();

  const x = interpolate(frame, [0, 28], [-width, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <div style={{transform: `translateX(${x}px)`}}>Slide from left</div>;
};
```

Using `width` from `useVideoConfig()` makes the motion adapt to composition size.

## Scale pop

```tsx
import {interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const ScalePop: React.FC = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const progress = spring({
    frame,
    fps,
    config: {damping: 12, stiffness: 140},
  });

  const scale = interpolate(progress, [0, 1], [0.6, 1]);

  return <div style={{transform: `scale(${scale})`}}>Pop</div>;
};
```

## Camera push

Use a large wrapper to create camera-like motion:

```tsx
import {AbsoluteFill, Easing, interpolate, useCurrentFrame} from 'remotion';

export const CameraPush: React.FC = () => {
  const frame = useCurrentFrame();

  const scale = interpolate(frame, [0, 120], [1, 1.12], {
    easing: Easing.inOut(Easing.quad),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{overflow: 'hidden', background: '#020617'}}>
      <AbsoluteFill
        style={{
          transform: `scale(${scale})`,
          background:
            'radial-gradient(circle at 50% 45%, #2563eb 0%, #020617 55%)',
        }}
      />
      <AbsoluteFill
        style={{
          color: 'white',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 90,
        }}
      >
        Slow push
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
```

## Parallax

```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

const Layer: React.FC<{
  depth: number;
  color: string;
  top: number;
}> = ({depth, color, top}) => {
  const frame = useCurrentFrame();
  const x = interpolate(frame, [0, 120], [0, -depth]);

  return (
    <div
      style={{
        position: 'absolute',
        top,
        left: x,
        width: '140%',
        height: 160,
        borderRadius: 80,
        background: color,
      }}
    />
  );
};

export const ParallaxScene: React.FC = () => {
  return (
    <AbsoluteFill style={{background: '#020617', overflow: 'hidden'}}>
      <Layer depth={80} color="#1e3a8a" top={250} />
      <Layer depth={160} color="#2563eb" top={430} />
      <Layer depth={260} color="#38bdf8" top={620} />
    </AbsoluteFill>
  );
};
```

Different layers move at different speeds.

## Staggered list

```tsx
import {Sequence} from 'remotion';

const items = ['Plan', 'Animate', 'Render'];

export const StaggeredList: React.FC = () => {
  return (
    <>
      {items.map((item, index) => (
        <Sequence key={item} from={index * 10}>
          <ListItem>{item}</ListItem>
        </Sequence>
      ))}
    </>
  );
};
```

```tsx
import {Easing, interpolate, useCurrentFrame} from 'remotion';

const ListItem: React.FC<{children: React.ReactNode}> = ({children}) => {
  const frame = useCurrentFrame();

  const x = interpolate(frame, [0, 24], [-60, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  const opacity = interpolate(frame, [0, 14], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        opacity,
        transform: `translateX(${x}px)`,
        fontSize: 64,
        marginBottom: 24,
      }}
    >
      {children}
    </div>
  );
};
```

## Motion blur-like trail

Remotion does not automatically apply motion blur to CSS transforms, but you can
fake trails with repeated translucent elements.

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const TrailDot: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <>
      {[0, 1, 2, 3, 4].map((index) => {
        const delayedFrame = frame - index * 2;
        const x = interpolate(delayedFrame, [0, 60], [0, 600], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });

        return (
          <div
            key={index}
            style={{
              position: 'absolute',
              left: x,
              top: 200,
              width: 42,
              height: 42,
              borderRadius: 999,
              background: '#38bdf8',
              opacity: 1 - index * 0.16,
              filter: `blur(${index * 1.5}px)`,
            }}
          />
        );
      })}
    </>
  );
};
```

## Text reveal mask

```tsx
import {Easing, interpolate, useCurrentFrame} from 'remotion';

export const MaskedTextReveal: React.FC<{children: React.ReactNode}> = ({
  children,
}) => {
  const frame = useCurrentFrame();

  const y = interpolate(frame, [0, 24], [110, 0], {
    easing: Easing.out(Easing.cubic),
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div style={{overflow: 'hidden'}}>
      <div
        style={{
          transform: `translateY(${y}%)`,
          fontSize: 96,
          fontWeight: 900,
          lineHeight: 1,
        }}
      >
        {children}
      </div>
    </div>
  );
};
```

## Transform origin

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const DoorSwing: React.FC = () => {
  const frame = useCurrentFrame();

  const rotateY = interpolate(frame, [0, 45], [90, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        transformOrigin: 'left center',
        transform: `perspective(800px) rotateY(${rotateY}deg)`,
      }}
    >
      Door swing
    </div>
  );
};
```

## Combining Series and overlay motion

```tsx
import {AbsoluteFill, Sequence, Series} from 'remotion';

export const MotionTimeline: React.FC = () => {
  return (
    <AbsoluteFill style={{background: '#020617', color: 'white'}}>
      <Series>
        <Series.Sequence durationInFrames={60}>
          <SceneTitle title="Plan" />
        </Series.Sequence>
        <Series.Sequence durationInFrames={60}>
          <SceneTitle title="Build" />
        </Series.Sequence>
        <Series.Sequence durationInFrames={60}>
          <SceneTitle title="Render" />
        </Series.Sequence>
      </Series>

      <Sequence from={20} durationInFrames={140}>
        <ProgressRibbon />
      </Sequence>
    </AbsoluteFill>
  );
};
```

This creates sequential scenes with an overlapping animated ribbon.

## Performance notes

- Transform and opacity animations are usually cheaper than layout-heavy changes.
- Avoid extremely large blur filters unless needed.
- Reuse calculated values when multiple properties share a driver.
- Keep DOM size reasonable for complex particle/trail effects.
- Use static assets thoughtfully; huge images increase render cost.

## Checklist

- Use `transform` for movement, scale, and rotation.
- Use `useVideoConfig()` for dimension-aware movement.
- Use `Sequence` to stagger repeated components.
- Use `Series` for back-to-back scenes.
- Keep transform order intentional.
- Prefer deterministic motion helpers over ad hoc repeated formulas.
