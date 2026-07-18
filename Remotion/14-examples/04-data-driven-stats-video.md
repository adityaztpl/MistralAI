# Data-driven stats video

Data-driven videos turn metrics into motion. This pattern is useful for monthly reports, customer dashboards, product updates, investor summaries, and social proof clips.

## Data shape

```ts
type Stat = {
  label: string;
  value: number;
  suffix?: string;
  description: string;
};

const stats: Stat[] = [
  {label: 'Activation', value: 42, suffix: '%', description: 'increase after onboarding redesign'},
  {label: 'Time saved', value: 18, suffix: 'h', description: 'per team each month'},
  {label: 'NPS', value: 71, description: 'from enterprise accounts'},
];
```

## Animated stat card

```tsx
import {interpolate, useCurrentFrame} from 'remotion';

export const StatCard = ({stat, delay = 0}: {stat: Stat; delay?: number}) => {
  const frame = Math.max(0, useCurrentFrame() - delay);
  const count = Math.round(interpolate(frame, [0, 45], [0, stat.value], {extrapolateRight: 'clamp'}));
  const opacity = interpolate(frame, [0, 18], [0, 1], {extrapolateRight: 'clamp'});

  return (
    <div style={{opacity, padding: 42, borderRadius: 32, background: 'white', color: '#0f172a'}}>
      <div style={{fontSize: 30, textTransform: 'uppercase', letterSpacing: '0.18em'}}>{stat.label}</div>
      <div style={{fontSize: 120, fontWeight: 950}}>
        {count}{stat.suffix ?? ''}
      </div>
      <div style={{fontSize: 34, lineHeight: 1.2}}>{stat.description}</div>
    </div>
  );
};
```

## Composition

```tsx
import {AbsoluteFill} from 'remotion';

export const StatsVideo = ({stats}: {stats: Stat[]}) => (
  <AbsoluteFill style={{background: '#0f172a', padding: 86, color: 'white', fontFamily: 'Inter'}}>
    <h1 style={{fontSize: 78, margin: 0}}>This month in numbers</h1>
    <div style={{display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 28, marginTop: 70}}>
      {stats.map((stat, index) => <StatCard key={stat.label} stat={stat} delay={index * 18} />)}
    </div>
  </AbsoluteFill>
);
```

## Data hygiene checklist

- Validate numeric ranges before rendering.
- Decide how to display missing values.
- Freeze data snapshots for each render job.
- Document whether metrics are rounded, estimated, or exact.
- Use accessible contrast and avoid overly tiny labels.

## Render command

```bash
npx remotion render src/index.ts StatsVideo out/stats.mp4 --props=./props/monthly-stats.json
```
