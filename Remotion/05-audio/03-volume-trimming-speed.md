# Volume, trimming, speed, loop, and pitch

Audio timing should be expressed in frames. Remotion gives you a compact set of props for source trimming, timeline placement, volume automation, looping, and playback speed.

## Static volume

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {staticFile} from 'remotion';

export const QuietMusic: React.FC = () => {
  return <Audio src={staticFile('audio/music.mp3')} volume={0.25} />;
};
```

`volume={1}` is full volume. `volume={0}` is silent. With Web Audio support, some fallback paths may allow values above `1`; avoid amplification unless you intentionally want it.

## Frame-based fade

```tsx
import React from 'react';
import {interpolate, staticFile, useVideoConfig} from 'remotion';
import {Audio} from '@remotion/media';

export const FadeInOutMusic: React.FC = () => {
  const {durationInFrames} = useVideoConfig();

  return (
    <Audio
      src={staticFile('audio/music.mp3')}
      volume={(frame) =>
        interpolate(
          frame,
          [0, 45, durationInFrames - 45, durationInFrames],
          [0, 0.5, 0.5, 0],
          {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          },
        )
      }
    />
  );
};
```

The callback receives the local audio frame. If the audio starts at `from={60}`, the first callback value is `0` at composition frame `60`.

## Ducking music under voiceover

```tsx
import React from 'react';
import {interpolate, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

const duck = (frame: number) => {
  const fadeDown = interpolate(frame, [20, 50], [0.45, 0.16], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const fadeUp = interpolate(frame, [210, 240], [0.16, 0.45], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  if (frame < 50) {
    return fadeDown;
  }

  if (frame > 210) {
    return fadeUp;
  }

  return 0.16;
};

export const MusicDuckedUnderVoice: React.FC = () => {
  return (
    <>
      <Audio src={staticFile('audio/music.mp3')} loop volume={duck} />
      <Audio from={30} src={staticFile('audio/voiceover.mp3')} volume={1} />
    </>
  );
};
```

## Trim source audio

`trimBefore` removes audio from the beginning of the source. `trimAfter` removes audio after a source frame.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {staticFile} from 'remotion';

export const TrimmedInterview: React.FC = () => {
  return (
    <Audio
      src={staticFile('audio/interview.mp3')}
      trimBefore={60}
      trimAfter={330}
      from={30}
      durationInFrames={270}
    />
  );
};
```

At `30fps`, this means:

- Start playing at second `2` of the source.
- Stop at second `11` of the source.
- Mount the clip at composition frame `30`.
- Keep it mounted for `270` frames.

## Playback speed

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const FastDisclaimer: React.FC = () => {
  return (
    <Audio
      src="https://example.com/disclaimer.mp3"
      playbackRate={1.2}
      volume={0.9}
    />
  );
};
```

Rules:

- `1` is normal speed.
- `0.5` is half speed.
- `2` is double speed.
- Reverse playback is not supported.
- Very extreme values can fail in development because browser media APIs have limits.

## Pitch notes

`preservePitch` affects preview behavior on HTML5 fallback/native media. Render-time pitch adjustment uses `toneFrequency`.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const LowPitchedEffect: React.FC = () => {
  return (
    <Audio
      src="https://example.com/robot.wav"
      toneFrequency={0.75}
      fallbackHtml5AudioProps={{preservePitch: false}}
    />
  );
};
```

`toneFrequency` only works in server-side rendering and must stay stable while the tag is mounted.

## Looping

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {staticFile} from 'remotion';

export const LoopingAmbience: React.FC = () => {
  return <Audio src={staticFile('audio/room-tone.wav')} loop volume={0.2} />;
};
```

When using a volume callback with `loop`, `loopVolumeCurveBehavior` controls the frame passed to the callback:

- `'repeat'`: start frame from `0` each loop iteration.
- `'extend'`: keep increasing frame numbers across loop iterations.

```tsx
import React from 'react';
import {interpolate, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const LoopWithLongFade: React.FC = () => {
  return (
    <Audio
      src={staticFile('audio/four-bar-loop.mp3')}
      loop
      loopVolumeCurveBehavior="extend"
      volume={(frame) =>
        interpolate(frame, [0, 120], [0, 0.4], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        })
      }
    />
  );
};
```

Use `'extend'` for composition-level fades. Use `'repeat'` when the volume curve should restart on every loop.

## Muting by frame

`muted` can change over time.

```tsx
import React from 'react';
import {staticFile, useCurrentFrame} from 'remotion';
import {Audio} from '@remotion/media';

export const CensoredMoment: React.FC = () => {
  const frame = useCurrentFrame();

  return (
    <Audio
      src={staticFile('audio/dialogue.mp3')}
      muted={frame >= 120 && frame < 150}
    />
  );
};
```

For smoother results, use `volume` curves instead of abrupt muting.

## Mix checklist

- [ ] Fade in and out at clip boundaries to avoid clicks.
- [ ] Duck music before voiceover starts, not exactly on the same frame.
- [ ] Use `trimBefore`/`trimAfter` for source edits; use `from` for timeline placement.
- [ ] Decide whether looped volume curves should repeat or extend.
- [ ] Avoid over-amplification; fix source loudness when possible.
