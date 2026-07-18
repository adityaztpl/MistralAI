# Using audio in Remotion

For new audio usage, Remotion recommends `<Audio>` from `@remotion/media`. It extracts exact audio using Mediabunny during rendering and keeps playback synchronized with the timeline. Use `staticFile()` for local files in `public/`.

## Basic audio

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const BasicAudio: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#111827'}}>
      <Audio src={staticFile('audio/background.mp3')} />
    </AbsoluteFill>
  );
};
```

The audio is mounted from frame `0` and plays until the composition or source ends.

## Place a voiceover later

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const DelayedVoiceover: React.FC = () => {
  return (
    <AbsoluteFill>
      <Audio
        name="Narration"
        from={45}
        durationInFrames={210}
        src={staticFile('audio/narration.mp3')}
      />
    </AbsoluteFill>
  );
};
```

`from` places the clip on the parent timeline. `durationInFrames` controls how long the tag stays mounted. These behave like the same props on `<Sequence>`.

## Music bed plus narration

```tsx
import React from 'react';
import {AbsoluteFill, interpolate, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const MusicAndVoiceover: React.FC = () => {
  return (
    <AbsoluteFill>
      <Audio
        name="Music bed"
        src={staticFile('audio/ambient-bed.mp3')}
        loop
        volume={(frame) =>
          interpolate(frame, [0, 60, 270, 330], [0, 0.35, 0.35, 0], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          })
        }
      />
      <Audio
        name="Voiceover"
        from={30}
        src={staticFile('audio/voiceover.mp3')}
        volume={1}
      />
    </AbsoluteFill>
  );
};
```

This fades the music in and out, while voiceover stays full volume.

## Sound effects

Short sounds are easiest to read when named and placed directly.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const SfxTimeline: React.FC = () => {
  return (
    <AbsoluteFill>
      <Audio
        name="Logo whoosh"
        from={18}
        durationInFrames={45}
        src={staticFile('audio/whoosh.wav')}
        volume={0.7}
      />
      <Audio
        name="Notification ding"
        from={72}
        durationInFrames={24}
        src={staticFile('audio/ding.wav')}
        volume={0.5}
      />
      <Audio
        name="Impact"
        from={120}
        durationInFrames={30}
        src={staticFile('audio/impact.wav')}
        volume={0.9}
      />
    </AbsoluteFill>
  );
};
```

## Remote audio

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {Audio} from '@remotion/media';

export const RemoteAudio: React.FC = () => {
  return (
    <AbsoluteFill>
      <Audio
        src="https://remotion.media/audio.wav"
        requestInit={{cache: 'no-store'}}
        volume={0.5}
      />
    </AbsoluteFill>
  );
};
```

Remote audio notes:

- Remote files need to be fetchable by the render process.
- CORS matters for browser-side audio analysis and Web Audio fallbacks.
- Use `requestInit={{credentials: 'include'}}` or signed URLs for protected assets.
- The `requestInit` value is captured when the component mounts; passing an inline object is okay.

## Fallback-aware error handling

`<Audio>` can fall back to `<Html5Audio>` when processing fails. Return `'fallback'` or `'fail'` from `onError`.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const ProtectedAudio: React.FC = () => {
  return (
    <Audio
      src="https://example.com/protected/voiceover.mp3"
      requestInit={{credentials: 'include'}}
      onError={(error) => {
        console.error('Could not process audio', error);
        return 'fail';
      }}
    />
  );
};
```

Use `'fail'` for required voiceover. Use `'fallback'` for non-critical decorative audio where native playback is acceptable.

## Audio with video

Video components can also contribute audio. If a video clip should be silent, mute it.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Video} from '@remotion/media';
import {Audio} from '@remotion/media';

export const VideoWithSeparateMix: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video
        src={staticFile('clips/product.mp4')}
        muted
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
      />
      <Audio src={staticFile('audio/product-voiceover.mp3')} />
      <Audio src={staticFile('audio/music.mp3')} loop volume={0.2} />
    </AbsoluteFill>
  );
};
```

Muting unused video audio can speed up rendering because Remotion does not need to download and mix the video's audio.

## What makes audio deterministic?

- All timing uses frames, not wall-clock timers.
- Volume callbacks derive values from the current frame.
- Trimming and looping happen through Remotion props.
- Async audio analysis must use `delayRender()` or the `useAudioData()` helper to block until ready.

## Interview-style summary

Say: "I model audio as timeline layers. I use `@remotion/media`'s `<Audio>` for new code, place clips with `from` and `durationInFrames`, trim source content with `trimBefore` and `trimAfter`, and mix with frame-based volume curves. Required remote voiceover should fail the render if it cannot load; decorative audio can fall back."
