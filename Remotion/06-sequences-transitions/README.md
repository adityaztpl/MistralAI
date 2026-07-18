# 06-sequences-transitions: Timing, scene layout, and transitions

Remotion compositions are frame-based React trees. Timing primitives decide what is mounted, what local frame child components see, and how scenes connect.

Core APIs:

- `<Sequence>` from `remotion`
- `<Series>` from `remotion`
- `<TransitionSeries>` from `@remotion/transitions`
- `<Freeze>` and `<Loop>` from `remotion`

## Files

| Guide | Focus |
|---|---|
| [01 Sequence](01-sequence.md) | Delay, trim, nest, freeze, and name timeline items |
| [02 Series](02-series.md) | Play scenes sequentially without hand-calculating offsets |
| [03 TransitionSeries](03-transition-series.md) | Crossfades, slides, wipes, overlays, and transition duration math |
| [04 Freeze, Loop](04-freeze-loop-freeze.md) | Freeze frames, repeated motion, loop metadata |
| [05 Scene architecture](05-scene-architecture.md) | Structure larger videos as scenes, tracks, and reusable sections |

## Decision sheet

| Need | Use |
|---|---|
| Start a layer later | `<Sequence from={...}>` |
| End/unmount a layer | `<Sequence durationInFrames={...}>` |
| Advance child time while mounting later | `<Sequence trimBefore={...}>` |
| Play scenes back-to-back | `<Series>` |
| Overlap scenes manually | `<Series.Sequence offset={-...}>` |
| Built-in scene transitions | `<TransitionSeries>` |
| Freeze child time | `<Sequence freeze={...}>` or `<Freeze>` |
| Repeat child timeline | `<Loop durationInFrames={...}>` |

## Basic scene stack

```tsx
import React from 'react';
import {AbsoluteFill, Sequence} from 'remotion';

export const ThreeLayerTimeline: React.FC = () => {
  return (
    <AbsoluteFill>
      <Sequence durationInFrames={90} name="Intro">
        <Intro />
      </Sequence>
      <Sequence from={75} durationInFrames={120} name="Product">
        <Product />
      </Sequence>
      <Sequence from={180} name="Outro">
        <Outro />
      </Sequence>
    </AbsoluteFill>
  );
};
```

The `Product` scene overlaps the end of `Intro` for 15 frames because it starts at frame `75` while `Intro` lasts until frame `89`.

## Timing vocabulary

- **Composition frame**: absolute frame in the final video.
- **Local frame**: value returned by `useCurrentFrame()` inside a timed child.
- **Mount window**: when React components exist in the tree.
- **Trim**: skip part of a child timeline or media source.
- **Premount/postmount**: keep components mounted slightly before/after visibility to avoid flicker during seeking.

## Senior-level habit

Name meaningful sequences. Timeline labels make Studio debugging much easier:

```tsx
<Sequence name="Pricing cards enter" from={90} durationInFrames={80}>
  <PricingCards />
</Sequence>
```
