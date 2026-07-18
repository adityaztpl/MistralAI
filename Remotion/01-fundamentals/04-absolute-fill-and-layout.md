# 04 - AbsoluteFill and Layout

`<AbsoluteFill>` is one of Remotion's most common building blocks. It creates a
full-size absolutely positioned container that fills its parent. Since a
composition is a fixed-size canvas, full-frame layers are a natural layout
pattern.

## Basic usage

```tsx
import {AbsoluteFill} from 'remotion';

export const FullScreenCard: React.FC = () => {
  return (
    <AbsoluteFill
      style={{
        backgroundColor: '#0f172a',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: 72,
      }}
    >
      Hello Remotion
    </AbsoluteFill>
  );
};
```

Think of `<AbsoluteFill>` as a convenient full-frame layer.

## Layering

Because each `<AbsoluteFill>` occupies the same rectangle, multiple fills stack
on top of each other in DOM order.

```tsx
import {AbsoluteFill} from 'remotion';

export const LayeredScene: React.FC = () => {
  return (
    <AbsoluteFill style={{background: '#020617'}}>
      <AbsoluteFill
        style={{
          background:
            'radial-gradient(circle at 50% 40%, rgba(59, 130, 246, 0.5), transparent 45%)',
        }}
      />

      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          color: 'white',
          fontSize: 96,
          fontWeight: 800,
        }}
      >
        Layered
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
```

The first child is a background glow. The second child is foreground content.

## Safe area pattern

Many videos need a padded content area, especially for social or broadcast-style
layouts.

```tsx
import {AbsoluteFill} from 'remotion';

type SafeAreaProps = {
  children: React.ReactNode;
};

export const SafeArea: React.FC<SafeAreaProps> = ({children}) => {
  return (
    <AbsoluteFill
      style={{
        padding: 80,
        boxSizing: 'border-box',
      }}
    >
      {children}
    </AbsoluteFill>
  );
};
```

Usage:

```tsx
export const TitleLayout: React.FC = () => {
  return (
    <AbsoluteFill style={{background: '#111827', color: 'white'}}>
      <SafeArea>
        <div style={{fontSize: 40, color: '#93c5fd'}}>Launch Brief</div>
        <h1 style={{fontSize: 108, maxWidth: 1100}}>
          Your team dashboard, automated
        </h1>
      </SafeArea>
    </AbsoluteFill>
  );
};
```

## Flexbox and CSS Grid work normally

Remotion uses browser rendering, so normal CSS layout works:

```tsx
import {AbsoluteFill} from 'remotion';

const Stat: React.FC<{label: string; value: string}> = ({label, value}) => {
  return (
    <div
      style={{
        borderRadius: 28,
        background: 'rgba(255,255,255,0.08)',
        padding: 36,
      }}
    >
      <div style={{fontSize: 26, color: '#cbd5e1'}}>{label}</div>
      <div style={{fontSize: 74, fontWeight: 800}}>{value}</div>
    </div>
  );
};

export const DashboardLayout: React.FC = () => {
  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: 72,
        boxSizing: 'border-box',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1 style={{fontSize: 72, margin: '0 0 48px'}}>Quarterly Report</h1>

      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 32,
        }}
      >
        <Stat label="Revenue" value="$4.2M" />
        <Stat label="Growth" value="+38%" />
        <Stat label="NPS" value="72" />
      </div>
    </AbsoluteFill>
  );
};
```

## Responsive composition-aware layout

Use `useVideoConfig()` when layout depends on canvas size:

```tsx
import {AbsoluteFill, useVideoConfig} from 'remotion';

export const ResponsiveTitle: React.FC = () => {
  const {width, height} = useVideoConfig();
  const isSquare = width === height;

  return (
    <AbsoluteFill
      style={{
        background: '#172554',
        color: 'white',
        padding: isSquare ? 72 : 120,
        boxSizing: 'border-box',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          maxWidth: isSquare ? width - 144 : width * 0.65,
          fontSize: isSquare ? 76 : 104,
          lineHeight: 1,
          fontWeight: 900,
        }}
      >
        Designed for {width}x{height}
      </div>
    </AbsoluteFill>
  );
};
```

This is useful when the same scene supports multiple aspect ratios.

## Combining AbsoluteFill with Sequence

`<Sequence>` wraps children in an `<AbsoluteFill>` by default. This makes scenes
overlay cleanly without extra wrappers.

```tsx
import {AbsoluteFill, Sequence} from 'remotion';

export const OverlayLayout: React.FC = () => {
  return (
    <AbsoluteFill style={{background: '#020617'}}>
      <Sequence from={0} durationInFrames={90} name="Background card">
        <BackgroundCard />
      </Sequence>

      <Sequence from={20} durationInFrames={80} name="Headline">
        <Headline />
      </Sequence>

      <Sequence from={50} durationInFrames={60} name="CTA">
        <CallToAction />
      </Sequence>
    </AbsoluteFill>
  );
};
```

If you need a sequence to participate in normal document flow, pass
`layout="none"`:

```tsx
import {Sequence} from 'remotion';

export const InlineSequenceExample: React.FC = () => {
  return (
    <div style={{display: 'flex', gap: 24}}>
      <Sequence from={10} layout="none">
        <div>Appears without an AbsoluteFill wrapper</div>
      </Sequence>
    </div>
  );
};
```

## Positioning overlays

Absolute positioning works well for lower thirds, bugs, labels, and captions.

```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

export const LowerThird: React.FC<{name: string; role: string}> = ({
  name,
  role,
}) => {
  const frame = useCurrentFrame();
  const x = interpolate(frame, [0, 18], [-520, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill>
      <div
        style={{
          position: 'absolute',
          left: 80,
          bottom: 80,
          transform: `translateX(${x}px)`,
          background: 'rgba(15, 23, 42, 0.9)',
          color: 'white',
          borderRadius: 22,
          padding: '24px 32px',
          minWidth: 460,
          fontFamily: 'Inter, Arial, sans-serif',
        }}
      >
        <div style={{fontSize: 34, fontWeight: 800}}>{name}</div>
        <div style={{fontSize: 24, color: '#bfdbfe'}}>{role}</div>
      </div>
    </AbsoluteFill>
  );
};
```

## Layout anti-patterns

Avoid relying on viewport units for video dimensions:

```tsx
// Risky: the browser viewport is not the composition contract.
<div style={{width: '100vw', height: '100vh'}} />
```

Prefer composition-relative layout:

```tsx
import {AbsoluteFill} from 'remotion';

<AbsoluteFill />;
```

Avoid using mutable DOM measurement as the primary source of animation. If you
need measurements, make sure rendering waits for them and remains deterministic.

## Practical layout recipe

```tsx
import {AbsoluteFill} from 'remotion';

export const SceneShell: React.FC<{children: React.ReactNode}> = ({children}) => {
  return (
    <AbsoluteFill
      style={{
        background: 'linear-gradient(160deg, #020617 0%, #111827 100%)',
        color: 'white',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <AbsoluteFill
        style={{
          padding: 80,
          boxSizing: 'border-box',
        }}
      >
        {children}
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
```

This shell provides a full-frame background and a padded safe area that can be
reused by many scenes.
