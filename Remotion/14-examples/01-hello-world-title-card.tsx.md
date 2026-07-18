# Hello world title card (TSX in markdown)

This is the smallest useful Remotion composition: a title card with a background, animated headline, subtitle, and render command. It demonstrates the basic frame loop without external assets.

## Component

```tsx
import React from 'react';
import {
  AbsoluteFill,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

export type HelloWorldTitleCardProps = {
  title: string;
  subtitle?: string;
  accentColor?: string;
};

export const HelloWorldTitleCard: React.FC<HelloWorldTitleCardProps> = ({
  title,
  subtitle = 'Built with React and Remotion',
  accentColor = '#38bdf8',
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const entrance = spring({frame, fps, config: {damping: 18, stiffness: 120}});
  const opacity = interpolate(frame, [0, 18], [0, 1], {extrapolateRight: 'clamp'});
  const stripeX = interpolate(frame, [0, 90], [-420, 0], {extrapolateRight: 'clamp'});

  return (
    <AbsoluteFill
      style={{
        background: 'linear-gradient(135deg, #020617 0%, #0f172a 55%, #111827 100%)',
        color: 'white',
        fontFamily: 'Inter, system-ui, sans-serif',
        justifyContent: 'center',
        padding: 96,
        overflow: 'hidden',
      }}
    >
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background: `radial-gradient(circle at 25% 30%, ${accentColor}55, transparent 32%)`,
          opacity: 0.9,
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: stripeX,
          top: 0,
          width: 420,
          height: '100%',
          background: accentColor,
          opacity: 0.16,
          transform: 'skewX(-12deg)',
        }}
      />
      <div style={{position: 'relative', maxWidth: 1280}}>
        <div
          style={{
            color: accentColor,
            fontSize: 34,
            fontWeight: 800,
            letterSpacing: '0.22em',
            marginBottom: 26,
            opacity,
            textTransform: 'uppercase',
          }}
        >
          Remotion 101
        </div>
        <h1
          style={{
            fontSize: 132,
            lineHeight: 0.92,
            margin: 0,
            opacity,
            transform: `translateY(${(1 - entrance) * 90}px) scale(${0.94 + entrance * 0.06})`,
          }}
        >
          {title}
        </h1>
        <p style={{fontSize: 42, lineHeight: 1.2, maxWidth: 850, opacity: 0.82}}>{subtitle}</p>
      </div>
    </AbsoluteFill>
  );
};
```

## Composition registration

```tsx
import {Composition} from 'remotion';
import {HelloWorldTitleCard} from './HelloWorldTitleCard';

export const RemotionRoot = () => (
  <Composition
    id="HelloWorldTitleCard"
    component={HelloWorldTitleCard}
    durationInFrames={150}
    fps={30}
    width={1920}
    height={1080}
    defaultProps={{title: 'Hello, video'}}
  />
);
```

## Render commands

```bash
npx remotion still src/index.ts HelloWorldTitleCard out/hello-poster.png --frame=45
npx remotion render src/index.ts HelloWorldTitleCard out/hello-world.mp4
```

## Why this works

- `useCurrentFrame()` gives deterministic timing.
- `spring()` creates natural entrance motion without timers.
- `interpolate()` maps frame ranges to visual values.
- Props make the title card reusable for multiple outputs.
- A still render at frame 45 is a quick visual regression check.

## Extensions

- Add a `logoUrl` prop and load the logo from `staticFile()`.
- Add a 9:16 version by changing composition dimensions and safe-area padding.
- Add a schema so Studio can edit `title`, `subtitle`, and `accentColor` safely.
