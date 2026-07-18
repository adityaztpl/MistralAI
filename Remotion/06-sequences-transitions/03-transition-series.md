# `<TransitionSeries>`

`<TransitionSeries>` from `@remotion/transitions` behaves like `<Series>` but allows transition and overlay items between scenes.

Install:

```bash
npx remotion add @remotion/transitions
```

## Basic crossfade

```tsx
import React from 'react';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';

export const CrossfadeScenes: React.FC = () => {
  return (
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={90}>
        <SceneA />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition
        timing={linearTiming({durationInFrames: 24})}
        presentation={fade()}
      />
      <TransitionSeries.Sequence durationInFrames={90}>
        <SceneB />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  );
};
```

During a transition, both adjacent scenes render at the same time. The total duration is shortened by the transition duration.

In the example above: `90 + 90 - 24 = 156` frames.

## Slide and wipe

```tsx
import React from 'react';
import {TransitionSeries, springTiming, linearTiming} from '@remotion/transitions';
import {slide} from '@remotion/transitions/slide';
import {wipe} from '@remotion/transitions/wipe';

export const MotionTransitions: React.FC = () => {
  return (
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={75}>
        <Intro />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition
        timing={springTiming({config: {damping: 200}})}
        presentation={slide({direction: 'from-right'})}
      />
      <TransitionSeries.Sequence durationInFrames={120}>
        <Product />
      </TransitionSeries.Sequence>
      <TransitionSeries.Transition
        timing={linearTiming({durationInFrames: 30})}
        presentation={wipe({direction: 'from-left'})}
      />
      <TransitionSeries.Sequence durationInFrames={75}>
        <Outro />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  );
};
```

## Duration math

```tsx
const total =
  introDuration +
  productDuration +
  outroDuration -
  introToProductTransition -
  productToOutroTransition;
```

For `75 + 120 + 75 - 23 - 30`, total duration is `217` frames.

You can ask a timing for its duration:

```tsx
import {springTiming} from '@remotion/transitions';

const timing = springTiming({config: {damping: 200}});
const frames = timing.getDurationInFrames({fps: 30});
```

## Overlay between scenes

`<TransitionSeries.Overlay>` renders content on top of the cut point without shortening the timeline. This is useful for flashes, light leaks, dust, or graphic accents.

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {TransitionSeries} from '@remotion/transitions';

const Flash: React.FC = () => (
  <AbsoluteFill style={{backgroundColor: 'white', opacity: 0.65}} />
);

export const OverlayCut: React.FC = () => {
  return (
    <TransitionSeries>
      <TransitionSeries.Sequence durationInFrames={80}>
        <Before />
      </TransitionSeries.Sequence>
      <TransitionSeries.Overlay durationInFrames={12}>
        <Flash />
      </TransitionSeries.Overlay>
      <TransitionSeries.Sequence durationInFrames={80}>
        <After />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  );
};
```

The overlay is centered on the cut by default. Use `offset` to shift it earlier or later.

```tsx
<TransitionSeries.Overlay durationInFrames={20} offset={-5}>
  <LightLeak />
</TransitionSeries.Overlay>
```

## Enter and exit animations

You can put a transition at the beginning or end to animate a scene entering or leaving.

```tsx
import React from 'react';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {slide} from '@remotion/transitions/slide';

export const EnterOnly: React.FC = () => {
  return (
    <TransitionSeries>
      <TransitionSeries.Transition
        timing={linearTiming({durationInFrames: 20})}
        presentation={slide({direction: 'from-bottom'})}
      />
      <TransitionSeries.Sequence durationInFrames={90}>
        <TitleCard />
      </TransitionSeries.Sequence>
    </TransitionSeries>
  );
};
```

## Data-driven transition list

```tsx
import React from 'react';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';
import {slide} from '@remotion/transitions/slide';

type Scene = {
  id: string;
  durationInFrames: number;
  transition: 'fade' | 'slide';
};

const scenes: Scene[] = [
  {id: 'intro', durationInFrames: 90, transition: 'fade'},
  {id: 'feature', durationInFrames: 120, transition: 'slide'},
  {id: 'outro', durationInFrames: 75, transition: 'fade'},
];

export const DataDrivenTransitions: React.FC = () => {
  return (
    <TransitionSeries>
      {scenes.map((scene, index) => (
        <React.Fragment key={scene.id}>
          <TransitionSeries.Sequence durationInFrames={scene.durationInFrames}>
            <SceneRenderer sceneId={scene.id} />
          </TransitionSeries.Sequence>
          {index < scenes.length - 1 ? (
            <TransitionSeries.Transition
              timing={linearTiming({durationInFrames: 24})}
              presentation={scene.transition === 'fade' ? fade() : slide()}
            />
          ) : null}
        </React.Fragment>
      ))}
    </TransitionSeries>
  );
};
```

## Rules from the docs

- A transition must not be longer than the previous or next sequence.
- Two transitions cannot be adjacent.
- Two overlays cannot be adjacent.
- A transition and an overlay cannot be adjacent.
- There must be at least one sequence before or after a transition or overlay.

## When not to use it

If a scene has complex internal choreography, use normal `<Sequence>` layers inside that scene. Use `<TransitionSeries>` for the outer edit between scenes, not every tiny element.
