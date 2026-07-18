# `<Sequence>`

`<Sequence>` time-shifts its children. It can delay when content mounts, limit how long it stays mounted, trim child time, freeze child time, and label the Studio timeline.

## Delay and unmount

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const BasicSequences: React.FC = () => {
  return (
    <>
      <Sequence durationInFrames={30} name="Intro">
        <Intro />
      </Sequence>
      <Sequence from={30} durationInFrames={60} name="Main">
        <Main />
      </Sequence>
      <Sequence from={90} name="Outro">
        <Outro />
      </Sequence>
    </>
  );
};
```

- `Intro` is visible on frames `0-29`.
- `Main` is visible on frames `30-89`.
- `Outro` starts at frame `90` and lasts until the composition ends.

## Local frame behavior

Children inside a sequence see a shifted `useCurrentFrame()` value.

```tsx
import React from 'react';
import {Sequence, useCurrentFrame} from 'remotion';

const Counter: React.FC<{label: string}> = ({label}) => {
  const frame = useCurrentFrame();
  return <div>{label}: {frame}</div>;
};

export const SequenceFrameDemo: React.FC = () => {
  return (
    <>
      <Counter label="Global child" />
      <Sequence from={30}>
        <Counter label="Sequenced child" />
      </Sequence>
    </>
  );
};
```

At composition frame `30`, the global child sees `30`, while the sequenced child sees `0`.

## `trimBefore`

`trimBefore` advances the child timeline while keeping the mount start at `from`.

```tsx
import React from 'react';
import {Sequence, useCurrentFrame} from 'remotion';

const MovingBox: React.FC = () => {
  const frame = useCurrentFrame();
  return (
    <div
      style={{
        width: 120,
        height: 120,
        backgroundColor: '#38bdf8',
        transform: `translateX(${frame * 8}px)`,
      }}
    />
  );
};

export const TrimmedSequence: React.FC = () => {
  return (
    <Sequence from={60} trimBefore={20} durationInFrames={80}>
      <MovingBox />
    </Sequence>
  );
};
```

The box appears at composition frame `60`, but `MovingBox` receives local frame `20`.

## Nested sequences cascade

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const NestedSequences: React.FC = () => {
  return (
    <Sequence from={30} name="Scene">
      <SceneBackground />
      <Sequence from={20} durationInFrames={45} name="CTA">
        <CallToAction />
      </Sequence>
    </Sequence>
  );
};
```

The `CTA` starts at composition frame `50`.

## Layout behavior

By default, `<Sequence>` wraps children in an absolutely positioned fill element. Disable that with `layout="none"` when you need to preserve DOM structure or use components that cannot accept a wrapping `div`.

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const InlineSequence: React.FC = () => {
  return (
    <div style={{display: 'flex', gap: 24}}>
      <Sequence from={20} layout="none">
        <span>Appears later without an AbsoluteFill wrapper</span>
      </Sequence>
    </div>
  );
};
```

When `layout="none"`, you cannot use `style` or `className` on the sequence wrapper because there is no wrapper.

## Freezing with `<Sequence freeze>`

For new code, prefer the `freeze` prop for timeline items that should hold on one child frame.

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const FrozenTitleCard: React.FC = () => {
  return (
    <Sequence freeze={45} durationInFrames={90} name="Frozen title">
      <AnimatedTitle />
    </Sequence>
  );
};
```

The child sees frame `45` for the entire sequence.

## Premount and postmount

`premountFor` and `postmountFor` keep children mounted outside their visible range. This can reduce flicker when seeking around media-heavy scenes.

```tsx
import React from 'react';
import {Sequence} from 'remotion';

export const PremountedScene: React.FC = () => {
  return (
    <Sequence
      from={120}
      durationInFrames={90}
      premountFor={30}
      postmountFor={15}
      styleWhilePremounted={{opacity: 0}}
      name="Premounted product scene"
    >
      <ProductScene />
    </Sequence>
  );
};
```

## Selectable layout-none content

When using `layout="none"`, pass `outlineRef` so Remotion Studio can draw a selection outline.

```tsx
import React, {useRef} from 'react';
import {Sequence} from 'remotion';

export const SelectableInline: React.FC = () => {
  const ref = useRef<HTMLDivElement>(null);

  return (
    <Sequence layout="none" outlineRef={ref} from={45}>
      <div ref={ref} style={{padding: 40, backgroundColor: '#fef3c7'}}>
        Selectable in Studio
      </div>
    </Sequence>
  );
};
```

## Checklist

- [ ] Use `from` for timeline placement.
- [ ] Use `durationInFrames` for mount duration.
- [ ] Use `trimBefore` when child animation should already be progressed.
- [ ] Use `layout="none"` only when the wrapper is a problem.
- [ ] Name important sequences for Studio readability.
- [ ] Prefer `<Sequence freeze>` for timeline-level freezes.
