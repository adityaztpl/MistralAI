# Soundtrack patterns

Good Remotion sound design is usually a small architecture problem. Rather than scattering audio tags everywhere, organize sound as named tracks: music, narration, effects, ambience, and video source audio.

## Pattern 1: soundtrack component

Create one component that owns the mix.

```tsx
import React from 'react';
import {interpolate, staticFile, useVideoConfig} from 'remotion';
import {Audio} from '@remotion/media';

export const Soundtrack: React.FC = () => {
  const {durationInFrames} = useVideoConfig();

  return (
    <>
      <Audio
        name="Music bed"
        src={staticFile('audio/music.mp3')}
        loop
        loopVolumeCurveBehavior="extend"
        volume={(frame) =>
          interpolate(
            frame,
            [0, 60, durationInFrames - 45, durationInFrames],
            [0, 0.32, 0.32, 0],
            {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
          )
        }
      />
      <Audio
        name="Voiceover"
        from={30}
        src={staticFile('audio/voiceover.mp3')}
        volume={1}
      />
      <Audio
        name="Logo impact"
        from={18}
        durationInFrames={30}
        src={staticFile('audio/impact.wav')}
        volume={0.7}
      />
    </>
  );
};
```

Then mount it once in the composition:

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {Soundtrack} from './Soundtrack';

export const LaunchVideo: React.FC = () => {
  return (
    <AbsoluteFill>
      <Visuals />
      <Soundtrack />
    </AbsoluteFill>
  );
};
```

## Pattern 2: scene-owned sound effects

If an SFX belongs tightly to a scene, keep it next to that scene.

```tsx
import React from 'react';
import {AbsoluteFill, Sequence, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

const ProductReveal: React.FC = () => {
  return (
    <AbsoluteFill>
      <ProductCard />
      <Audio
        name="Reveal whoosh"
        from={12}
        durationInFrames={36}
        src={staticFile('audio/whoosh.wav')}
        volume={0.5}
      />
    </AbsoluteFill>
  );
};

export const SceneSoundExample: React.FC = () => {
  return (
    <Sequence from={90} durationInFrames={120} name="Product reveal">
      <ProductReveal />
    </Sequence>
  );
};
```

The child `from={12}` is local to the `ProductReveal` sequence, so the sound fires at composition frame `102`.

## Pattern 3: music ducking bus

Use a helper function to define ducking regions.

```tsx
type DuckRegion = {
  start: number;
  end: number;
  lowVolume: number;
};

export const duckingVolume = (
  frame: number,
  regions: DuckRegion[],
  baseVolume = 0.4,
  fadeFrames = 15,
) => {
  for (const region of regions) {
    if (frame < region.start - fadeFrames || frame > region.end + fadeFrames) {
      continue;
    }

    if (frame < region.start) {
      return baseVolume -
        ((baseVolume - region.lowVolume) * (frame - (region.start - fadeFrames))) /
          fadeFrames;
    }

    if (frame > region.end) {
      return region.lowVolume +
        ((baseVolume - region.lowVolume) * (frame - region.end)) / fadeFrames;
    }

    return region.lowVolume;
  }

  return baseVolume;
};
```

```tsx
import React from 'react';
import {staticFile} from 'remotion';
import {Audio} from '@remotion/media';
import {duckingVolume} from './duckingVolume';

export const DuckedMusic: React.FC = () => {
  return (
    <Audio
      src={staticFile('audio/music.mp3')}
      loop
      loopVolumeCurveBehavior="extend"
      volume={(frame) =>
        duckingVolume(frame, [
          {start: 30, end: 210, lowVolume: 0.14},
          {start: 270, end: 360, lowVolume: 0.18},
        ])
      }
    />
  );
};
```

## Pattern 4: data-driven SFX cues

```tsx
import React from 'react';
import {staticFile} from 'remotion';
import {Audio} from '@remotion/media';

type SoundCue = {
  name: string;
  file: string;
  frame: number;
  durationInFrames: number;
  volume: number;
};

const cues: SoundCue[] = [
  {name: 'Pop 1', file: 'audio/pop.wav', frame: 24, durationInFrames: 12, volume: 0.4},
  {name: 'Pop 2', file: 'audio/pop.wav', frame: 48, durationInFrames: 12, volume: 0.35},
  {name: 'Shimmer', file: 'audio/shimmer.wav', frame: 96, durationInFrames: 60, volume: 0.5},
];

export const CueTrack: React.FC = () => {
  return (
    <>
      {cues.map((cue) => (
        <Audio
          key={`${cue.name}-${cue.frame}`}
          name={cue.name}
          from={cue.frame}
          durationInFrames={cue.durationInFrames}
          src={staticFile(cue.file)}
          volume={cue.volume}
        />
      ))}
    </>
  );
};
```

This scales well for template videos where a CMS or editor produces cue metadata.

## Pattern 5: isolate video source audio

When a video clip contains usable production audio, treat it as a track and automate it.

```tsx
import React from 'react';
import {interpolate, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const InterviewVideoWithAudio: React.FC = () => {
  return (
    <Video
      name="Interview A-roll"
      src={staticFile('clips/interview.mp4')}
      style={{width: '100%', height: '100%'}}
      objectFit="cover"
      volume={(frame) =>
        interpolate(frame, [0, 15, 285, 300], [0, 1, 1, 0], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        })
      }
    />
  );
};
```

If the clip's audio is not needed, pass `muted` to avoid unnecessary render work.

## Mix review checklist

- [ ] Does every important track have a `name` for Studio timeline readability?
- [ ] Are music and ambience looped with intentional fade behavior?
- [ ] Are SFX placed in local scene time or global composition time consistently?
- [ ] Is voiceover required, and should render fail if missing?
- [ ] Are video clips muted when their source audio is unused?
- [ ] Are repeated cues data-driven instead of copy-pasted?
- [ ] Are fades long enough to prevent clicks?
