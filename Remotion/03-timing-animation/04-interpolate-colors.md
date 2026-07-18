# 04 - `interpolateColors()`

`interpolateColors()` maps an input value to a color range. It is useful for
background transitions, theme changes, SVG colors, gradients, and animated UI
states.

## Basic color transition

```tsx
import {
  AbsoluteFill,
  interpolateColors,
  useCurrentFrame,
} from 'remotion';

export const ColorFade: React.FC = () => {
  const frame = useCurrentFrame();

  const backgroundColor = interpolateColors(
    frame,
    [0, 60],
    ['#020617', '#2563eb'],
  );

  return <AbsoluteFill style={{backgroundColor}} />;
};
```

At frame `0`, the background is dark. At frame `60`, it is blue.

## Multi-color timeline

```tsx
import {interpolateColors, useCurrentFrame} from 'remotion';

export const MultiColor: React.FC = () => {
  const frame = useCurrentFrame();

  const color = interpolateColors(
    frame,
    [0, 45, 90, 135],
    ['#020617', '#1d4ed8', '#7c3aed', '#be123c'],
  );

  return (
    <div
      style={{
        width: '100%',
        height: '100%',
        backgroundColor: color,
      }}
    />
  );
};
```

Use equal-length input and output arrays.

## Color with fade and text

```tsx
import {
  AbsoluteFill,
  interpolate,
  interpolateColors,
  useCurrentFrame,
} from 'remotion';

export const ColorTitle: React.FC = () => {
  const frame = useCurrentFrame();

  const backgroundColor = interpolateColors(
    frame,
    [0, 50],
    ['#111827', '#f8fafc'],
  );

  const textColor = interpolateColors(frame, [0, 50], ['#ffffff', '#0f172a']);

  const opacity = interpolate(frame, [0, 20], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        backgroundColor,
        color: textColor,
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1 style={{opacity, fontSize: 96}}>Theme transition</h1>
    </AbsoluteFill>
  );
};
```

## Animated SVG colors

```tsx
import {interpolateColors, useCurrentFrame} from 'remotion';

export const AnimatedLogoMark: React.FC = () => {
  const frame = useCurrentFrame();

  const fill = interpolateColors(frame, [0, 40], ['#38bdf8', '#a78bfa']);
  const stroke = interpolateColors(frame, [0, 40], ['#bfdbfe', '#ffffff']);

  return (
    <svg width="240" height="240" viewBox="0 0 240 240">
      <circle cx="120" cy="120" r="84" fill={fill} stroke={stroke} strokeWidth="8" />
      <path
        d="M86 126L112 152L158 88"
        fill="none"
        stroke={stroke}
        strokeWidth="14"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
};
```

Because SVG attributes are just values, Remotion can animate them using frame
math.

## Gradient transitions

You can interpolate individual stops and assemble a gradient string:

```tsx
import {AbsoluteFill, interpolateColors, useCurrentFrame} from 'remotion';

export const GradientShift: React.FC = () => {
  const frame = useCurrentFrame();

  const start = interpolateColors(frame, [0, 90], ['#020617', '#1e1b4b']);
  const middle = interpolateColors(frame, [0, 90], ['#1d4ed8', '#7c3aed']);
  const end = interpolateColors(frame, [0, 90], ['#22d3ee', '#f97316']);

  return (
    <AbsoluteFill
      style={{
        background: `linear-gradient(135deg, ${start}, ${middle}, ${end})`,
      }}
    />
  );
};
```

## Color changes driven by progress

```tsx
import {interpolateColors, useCurrentFrame, useVideoConfig} from 'remotion';

export const ProgressColor: React.FC = () => {
  const frame = useCurrentFrame();
  const {durationInFrames} = useVideoConfig();

  const progress = frame / (durationInFrames - 1);
  const color = interpolateColors(progress, [0, 0.5, 1], [
    '#ef4444',
    '#facc15',
    '#22c55e',
  ]);

  return <div style={{backgroundColor: color}}>Progress color</div>;
};
```

This is useful for charts, progress bars, gauges, and score visualizations.

## Combine with `interpolate()`

```tsx
import {
  interpolate,
  interpolateColors,
  useCurrentFrame,
} from 'remotion';

export const AlertBadge: React.FC = () => {
  const frame = useCurrentFrame();

  const color = interpolateColors(frame, [0, 30], ['#475569', '#ef4444']);
  const scale = interpolate(frame, [0, 15, 30], [1, 1.1, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        background: color,
        transform: `scale(${scale})`,
        color: 'white',
        borderRadius: 999,
        padding: '12px 24px',
        fontWeight: 800,
      }}
    >
      Alert
    </div>
  );
};
```

## Theme object interpolation

For bigger theme transitions, interpolate several values together:

```tsx
import {interpolateColors, useCurrentFrame} from 'remotion';

const useThemeTransition = () => {
  const frame = useCurrentFrame();

  return {
    background: interpolateColors(frame, [0, 60], ['#020617', '#f8fafc']),
    foreground: interpolateColors(frame, [0, 60], ['#ffffff', '#0f172a']),
    accent: interpolateColors(frame, [0, 60], ['#38bdf8', '#2563eb']),
    muted: interpolateColors(frame, [0, 60], ['#94a3b8', '#475569']),
  };
};

export const ThemedCard: React.FC = () => {
  const theme = useThemeTransition();

  return (
    <div
      style={{
        background: theme.background,
        color: theme.foreground,
        border: `4px solid ${theme.accent}`,
      }}
    >
      <p style={{color: theme.muted}}>Animated theme</p>
    </div>
  );
};
```

## Practical notes

- Keep color formats consistent within a range.
- Use branded color constants to avoid scattered hex values.
- Interpolate individual gradient stops instead of trying to parse whole
  gradient strings.
- Use color transitions sparingly; excessive color motion can distract from
  content.
- Pair color interpolation with opacity or movement for richer transitions.

## Complete color scene

```tsx
import {
  AbsoluteFill,
  interpolate,
  interpolateColors,
  useCurrentFrame,
} from 'remotion';

export const ColorScene: React.FC = () => {
  const frame = useCurrentFrame();

  const background = interpolateColors(frame, [0, 80], [
    '#020617',
    '#312e81',
  ]);

  const accent = interpolateColors(frame, [0, 80], ['#38bdf8', '#f0abfc']);

  const y = interpolate(frame, [0, 30], [70, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        background,
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <div
        style={{
          transform: `translateY(${y}px)`,
          textAlign: 'center',
        }}
      >
        <div
          style={{
            width: 180,
            height: 12,
            borderRadius: 999,
            background: accent,
            margin: '0 auto 28px',
          }}
        />
        <h1 style={{fontSize: 96, margin: 0}}>Color as motion</h1>
      </div>
    </AbsoluteFill>
  );
};
```
