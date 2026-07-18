# `<Freeze>` and `<Loop>`

`<Freeze>` and `<Loop>` manipulate child time:

- `<Freeze>` holds children at one frame.
- `<Loop>` repeats child timelines.

For new timeline-item freezes, Remotion recommends `<Sequence freeze={...}>` because Studio can represent it visually.

## Prefer `<Sequence freeze>`

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const HoldOnLogo: React.FC = () => {
  return (
    <Sequence durationInFrames={90} freeze={45} name="Logo hold">
      <LogoBuild />
    </Sequence>
  );
};
```

Children see local frame `45` for the whole sequence.

## Direct `<Freeze>`

Use `<Freeze>` when freezing is conditional or does not map cleanly to a sequence item.

```tsx
import React from 'react';
import {Freeze} from 'remotion';

export const FrozenFrame: React.FC = () => {
  return (
    <Freeze frame={30}>
      <AnimatedChart />
    </Freeze>
  );
};
```

Inside `<Freeze>`, `useCurrentFrame()` returns the frozen frame. Video elements pause and audio renders muted.

## Conditional freeze

```tsx
import React from 'react';
import {Freeze, useCurrentFrame} from 'remotion';

export const FreezeAfterReveal: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <Freeze frame={60} active={frame >= 60}>
      <ProductSpin />
    </Freeze>
  );
};
```

`active` may be a boolean or a callback.

## Basic `<Loop>`

```tsx
import React from 'react';
import {Loop} from 'remotion';

export const PulsingBackground: React.FC = () => {
  return (
    <Loop durationInFrames={45}>
      <Pulse />
    </Loop>
  );
};
```

The child timeline resets every 45 frames.

## Fixed loop count

```tsx
import React from 'react';
import {Loop} from 'remotion';

export const RepeatThreeTimes: React.FC = () => {
  return (
    <Loop durationInFrames={30} times={3}>
      <Sparkle />
    </Loop>
  );
};
```

The content lasts `90` frames total.

## Nested loops

```tsx
import React from 'react';
import {Loop} from 'remotion';

export const NestedLoopPattern: React.FC = () => {
  return (
    <Loop durationInFrames={120}>
      <OrbitingGroup />
      <Loop durationInFrames={24}>
        <SmallPulse />
      </Loop>
    </Loop>
  );
};
```

Loops cascade: child local frame is affected by each parent loop.

## `Loop.useLoop()`

Children can inspect loop metadata.

```tsx
import React from 'react';
import {Loop, useCurrentFrame} from 'remotion';

const LoopAwareBadge: React.FC = () => {
  const frame = useCurrentFrame();
  const loop = Loop.useLoop();

  return (
    <div style={{fontSize: 48}}>
      frame {frame}, iteration {loop?.iteration ?? 'none'}
    </div>
  );
};

export const LoopAwareExample: React.FC = () => {
  return (
    <Loop durationInFrames={50} times={4}>
      <LoopAwareBadge />
    </Loop>
  );
};
```

`Loop.useLoop()` returns `null` outside a loop. Inside a loop, it returns:

- `durationInFrames`
- `iteration`, starting at `0`

## Looping media

Use media-level `loop` when available. Use `<Loop>` when repeating a full component timeline.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {Loop, staticFile} from 'remotion';

export const LoopComparison: React.FC = () => {
  return (
    <>
      <Audio src={staticFile('audio/music-loop.mp3')} loop volume={0.3} />

      <Loop durationInFrames={60}>
        <AnimatedEqualizer />
      </Loop>
    </>
  );
};
```

For `<OffthreadVideo>`, which does not implement `loop`, prefer `@remotion/media` `<Video loop>` or wrap a known-duration clip in `<Loop>`.

## Freeze portions of a motion

```tsx
import React from 'react';
import {Freeze, Sequence} from 'remotion';

export const FreezeMiddle: React.FC = () => {
  return (
    <>
      <Sequence durationInFrames={45}>
        <AnimatedCard />
      </Sequence>
      <Sequence from={45} durationInFrames={30}>
        <Freeze frame={44}>
          <AnimatedCard />
        </Freeze>
      </Sequence>
      <Sequence from={75} trimBefore={45}>
        <AnimatedCard />
      </Sequence>
    </>
  );
};
```

This pattern animates in, holds, then resumes from where it paused.

## Checklist

- [ ] Use `<Sequence freeze>` for simple timeline freezes.
- [ ] Use `<Freeze>` for conditional or cross-cutting freezes.
- [ ] Use `<Loop>` for component timelines, not just media playback.
- [ ] Keep loop durations explicit and tied to design beats.
- [ ] Use `Loop.useLoop()` for iteration-aware visuals.
- [ ] Remember frozen media audio is muted.
