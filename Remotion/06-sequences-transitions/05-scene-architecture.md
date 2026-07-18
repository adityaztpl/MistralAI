# Scene architecture

As videos grow, timing bugs usually come from mixing three concerns in one component:

1. Scene order and duration.
2. Scene internals.
3. Global tracks such as audio, subtitles, watermarks, and overlays.

Separate them.

## Recommended structure

```text
src/
|-- Root.tsx
|-- compositions/
|   `-- MarketingVideo.tsx
|-- scenes/
|   |-- IntroScene.tsx
|   |-- FeatureScene.tsx
|   `-- OutroScene.tsx
|-- tracks/
|   |-- Soundtrack.tsx
|   `-- Captions.tsx
`-- data/
   `-- marketingVideo.ts
```

## Define scene data

```tsx
export type MarketingScene =
  | {type: 'intro'; title: string; durationInFrames: number}
  | {type: 'feature'; headline: string; body: string; durationInFrames: number}
  | {type: 'outro'; cta: string; durationInFrames: number};

export const scenes: MarketingScene[] = [
  {type: 'intro', title: 'Launch faster', durationInFrames: 90},
  {
    type: 'feature',
    headline: 'Automate every render',
    body: 'Use props, schemas, and server-side rendering.',
    durationInFrames: 135,
  },
  {type: 'outro', cta: 'Start building', durationInFrames: 75},
];
```

## Render with `<Series>`

```tsx
import React from 'react';
import {AbsoluteFill, Series} from 'remotion';
import {scenes, type MarketingScene} from '../data/marketingVideo';

const SceneRenderer: React.FC<{scene: MarketingScene}> = ({scene}) => {
  switch (scene.type) {
    case 'intro':
      return <IntroScene title={scene.title} />;
    case 'feature':
      return <FeatureScene headline={scene.headline} body={scene.body} />;
    case 'outro':
      return <OutroScene cta={scene.cta} />;
  }
};

export const MarketingVideo: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#020617'}}>
      <Series>
        {scenes.map((scene, index) => (
          <Series.Sequence
            key={`${scene.type}-${index}`}
            durationInFrames={scene.durationInFrames}
            name={`${index + 1}. ${scene.type}`}
          >
            <SceneRenderer scene={scene} />
          </Series.Sequence>
        ))}
      </Series>
      <Soundtrack />
      <Watermark />
    </AbsoluteFill>
  );
};
```

## Keep scene internals local

Inside a scene, use local frames and local sequences.

```tsx
import React from 'react';
import {AbsoluteFill, interpolate, Sequence, useCurrentFrame} from 'remotion';

export const FeatureScene: React.FC<{headline: string; body: string}> = ({
  headline,
  body,
}) => {
  const frame = useCurrentFrame();
  const headlineY = interpolate(frame, [0, 30], [80, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{padding: 90, color: 'white'}}>
      <h1 style={{fontSize: 84, transform: `translateY(${headlineY}px)`}}>
        {headline}
      </h1>
      <Sequence from={20}>
        <p style={{fontSize: 38, maxWidth: 820, opacity: 0.8}}>{body}</p>
      </Sequence>
      <Sequence from={55} durationInFrames={50}>
        <FeatureCards />
      </Sequence>
    </AbsoluteFill>
  );
};
```

The scene can be moved in the outer edit without rewriting its internal timing.

## Global tracks

Global tracks are mounted outside the scene series so they can span cuts.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {interpolate, staticFile, useVideoConfig} from 'remotion';

export const Soundtrack: React.FC = () => {
  const {durationInFrames} = useVideoConfig();

  return (
    <Audio
      name="Global music"
      src={staticFile('audio/music.mp3')}
      loop
      loopVolumeCurveBehavior="extend"
      volume={(frame) =>
        interpolate(frame, [0, 45, durationInFrames - 45, durationInFrames], [0, 0.3, 0.3, 0], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        })
      }
    />
  );
};
```

## Scene transitions at the outer layer

Use `<TransitionSeries>` for the outer edit, then keep each scene internally simple.

```tsx
import React from 'react';
import {TransitionSeries, linearTiming} from '@remotion/transitions';
import {fade} from '@remotion/transitions/fade';

export const TransitionedMarketingVideo: React.FC = () => {
  return (
    <TransitionSeries>
      {scenes.map((scene, index) => (
        <React.Fragment key={`${scene.type}-${index}`}>
          <TransitionSeries.Sequence durationInFrames={scene.durationInFrames}>
            <SceneRenderer scene={scene} />
          </TransitionSeries.Sequence>
          {index < scenes.length - 1 ? (
            <TransitionSeries.Transition
              timing={linearTiming({durationInFrames: 18})}
              presentation={fade()}
            />
          ) : null}
        </React.Fragment>
      ))}
    </TransitionSeries>
  );
};
```

## Duration calculation

For pure `<Series>`, total duration is the sum of scene durations plus offsets. For `<TransitionSeries>`, subtract transition durations and do not subtract overlays.

```tsx
export const totalSeriesDuration = scenes.reduce(
  (sum, scene) => sum + scene.durationInFrames,
  0,
);
```

If duration is data-driven, pass it into `<Composition>` or compute it in `calculateMetadata()`.

## Testing scene boundaries

Create debug frames while authoring:

```tsx
import React from 'react';
import {AbsoluteFill, useCurrentFrame} from 'remotion';

export const FrameBurnIn: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <AbsoluteFill style={{justifyContent: 'flex-end', alignItems: 'flex-start'}}>
      <div style={{padding: 20, color: 'white', backgroundColor: 'rgba(0,0,0,0.5)'}}>
        frame {frame}
      </div>
    </AbsoluteFill>
  );
};
```

Mount it temporarily above all scenes to verify cuts and transitions.

## Architecture checklist

- [ ] Keep outer edit and scene internals separate.
- [ ] Represent repeated scene structures with data.
- [ ] Keep global tracks outside scene-specific series.
- [ ] Name scenes in Studio.
- [ ] Compute dynamic duration in one place.
- [ ] Use transitions at scene boundaries, not inside every component.
- [ ] Make each scene previewable in isolation when possible.
