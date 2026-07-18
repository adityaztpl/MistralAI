# `<Series>`

`<Series>` lays scenes out one after another without requiring you to manually calculate `from` offsets. Use it when your composition is primarily a list of scenes or sections.

## Basic series

```tsx
import React from 'react';
import {Series} from 'remotion';

export const SequentialScenes: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={60}>
        <Intro />
      </Series.Sequence>
      <Series.Sequence durationInFrames={120}>
        <Feature />
      </Series.Sequence>
      <Series.Sequence durationInFrames={75}>
        <Outro />
      </Series.Sequence>
    </Series>
  );
};
```

The second scene starts at frame `60`. The third starts at frame `180`.

## Why use `<Series>`?

- It removes hand-maintained offset math.
- It makes reorder operations safer.
- It reads like an edit decision list.
- It keeps scene durations close to scene definitions.
- It accepts many `<Sequence>` props in modern Remotion versions.

## Offsets

`offset` changes the start time of a sequence relative to the previous one:

- Positive `offset`: creates a gap.
- Negative `offset`: overlaps with the previous scene.

```tsx
import React from 'react';
import {Series} from 'remotion';

export const OffsetSeries: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={90}>
        <SceneA />
      </Series.Sequence>
      <Series.Sequence durationInFrames={90} offset={-15}>
        <SceneB />
      </Series.Sequence>
      <Series.Sequence durationInFrames={60} offset={10}>
        <SceneC />
      </Series.Sequence>
    </Series>
  );
};
```

Here, `SceneB` overlaps `SceneA` by 15 frames. `SceneC` starts 10 frames after `SceneB` ends.

## Timeline labels and refs

```tsx
import React, {useRef} from 'react';
import {Series} from 'remotion';

export const LabeledSeries: React.FC = () => {
  const introRef = useRef<HTMLDivElement>(null);

  return (
    <Series name="Launch video">
      <Series.Sequence
        ref={introRef}
        name="Intro headline"
        durationInFrames={75}
      >
        <Intro />
      </Series.Sequence>
      <Series.Sequence name="Feature cards" durationInFrames={150}>
        <FeatureCards />
      </Series.Sequence>
    </Series>
  );
};
```

The `<Series>` component itself is a sequence under the hood and can accept sequence props such as `from`, `name`, `className`, `style`, and `layout`.

## Freeze a series segment

```tsx
import React from 'react';
import {Series} from 'remotion';

export const FrozenSeriesSegment: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={80}>
        <AnimatedIntro />
      </Series.Sequence>
      <Series.Sequence durationInFrames={60} freeze={30}>
        <ProductSpin />
      </Series.Sequence>
      <Series.Sequence durationInFrames={80}>
        <Outro />
      </Series.Sequence>
    </Series>
  );
};
```

The product spin scene holds on local frame `30`.

## Composing with scene props

```tsx
import React from 'react';
import {Series} from 'remotion';

type Slide = {
  title: string;
  body: string;
  color: string;
  duration: number;
};

const slides: Slide[] = [
  {title: 'Fast setup', body: 'Start from a template.', color: '#dbeafe', duration: 90},
  {title: 'Typed props', body: 'Parameterize every render.', color: '#dcfce7', duration: 105},
  {title: 'Automated output', body: 'Render at scale.', color: '#fee2e2', duration: 90},
];

export const SlideDeck: React.FC = () => {
  return (
    <Series>
      {slides.map((slide) => (
        <Series.Sequence key={slide.title} durationInFrames={slide.duration}>
          <SlideScene {...slide} />
        </Series.Sequence>
      ))}
    </Series>
  );
};
```

This is a strong pattern for marketing templates: data drives the number, order, and duration of scenes.

## Last sequence duration

Only the last `<Series.Sequence>` may have `Infinity` duration. In most production compositions, prefer explicit positive integer durations because they make total duration easier to reason about.

## `<Series>` vs `<Sequence>`

Use `<Sequence>` when:

- Layers overlap freely.
- You need exact start frames.
- You are building a single scene with many internal elements.

Use `<Series>` when:

- Scenes play mostly one after another.
- Durations are known per scene.
- Reordering scenes should not require recalculating offsets.

## Checklist

- [ ] Give every sequence a positive integer duration unless it is the final open-ended segment.
- [ ] Use negative offsets sparingly; if transitions become complex, consider `<TransitionSeries>`.
- [ ] Keep scene duration constants near the scene data.
- [ ] Name sequences if they are meaningful timeline sections.
