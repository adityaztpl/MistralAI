# `<Audio>` vs `<Html5Audio>`

Remotion has two relevant audio components:

- `<Audio>` from `@remotion/media`: recommended for new audio usage.
- `<Html5Audio>` from `remotion`: native HTML5 audio behavior and fallback target.

Use `<Audio>` by default. Reach for `<Html5Audio>` only when you specifically need native element behavior or a fallback prop that is not exposed by `<Audio>`.

## Side-by-side imports

```tsx
import {Audio} from '@remotion/media';
import {Html5Audio, staticFile} from 'remotion';
```

## Recommended component

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const RecommendedAudio: React.FC = () => {
  return (
    <AbsoluteFill>
      <Audio src={staticFile('audio/music.mp3')} volume={0.4} loop />
    </AbsoluteFill>
  );
};
```

Why this is preferred:

- It is the modern Remotion media path.
- It extracts exact audio using Mediabunny during rendering.
- It supports clip timing props (`from`, `durationInFrames`) directly.
- It supports fallback to `<Html5Audio>` if processing fails.

## Native HTML5 audio behavior

```tsx
import React from 'react';
import {AbsoluteFill, Html5Audio, staticFile} from 'remotion';

export const NativeAudioBehavior: React.FC = () => {
  return (
    <AbsoluteFill>
      <Html5Audio
        src={staticFile('audio/music.mp3')}
        volume={0.4}
        pauseWhenBuffering
        acceptableTimeShiftInSeconds={0.25}
      />
    </AbsoluteFill>
  );
};
```

`<Html5Audio>` wraps a native audio element and is not supported in `@remotion/web-renderer`. Use it only when you know you need it.

## Shared concepts

Both APIs support the concepts you use most often:

| Capability | `<Audio>` | `<Html5Audio>` |
|---|---:|---:|
| Local `staticFile()` source | Yes | Yes |
| Remote source | Yes | Yes |
| `volume` number/callback | Yes | Yes |
| `trimBefore` / `trimAfter` | Yes | Yes |
| `playbackRate` | Yes | Yes |
| `loop` | Yes | Yes |
| `muted` | Yes | Yes |
| `name` / timeline label | Yes | Yes |
| `requestInit` | Yes | No; use native/cross-origin patterns |
| Fallback controls | Yes | Not applicable |
| Client-side rendering | Designed for modern media path | Not supported |

## Fallback configuration

`<Audio>` may fall back to `<Html5Audio>` when media processing fails. You can configure the fallback with `fallbackHtml5AudioProps`.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const FallbackConfiguredAudio: React.FC = () => {
  return (
    <Audio
      src="https://example.com/music.mp3"
      onError={(error) => {
        console.warn('Audio processing failed, using fallback', error.message);
        return 'fallback';
      }}
      fallbackHtml5AudioProps={{
        pauseWhenBuffering: true,
        acceptableTimeShiftInSeconds: 0.2,
        crossOrigin: 'anonymous',
        useWebAudioApi: true,
      }}
    />
  );
};
```

Fallback props include:

- `onError`
- `useWebAudioApi`
- `acceptableTimeShiftInSeconds`
- `pauseWhenBuffering`
- `crossOrigin`
- `preservePitch`

Fallback is not possible when using `@remotion/web-renderer` for client-side rendering.

## When to fail instead of fallback

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const RequiredNarration: React.FC<{src: string}> = ({src}) => {
  return (
    <Audio
      src={src}
      onError={(error) => {
        console.error('Required narration failed', error);
        return 'fail';
      }}
    />
  );
};
```

Fail for:

- Voiceover or dialogue that is essential to the video.
- Compliance/legal disclaimers.
- Automated render pipelines where silent output would be worse than no output.

Fallback for:

- Decorative background ambience.
- Optional sound effects.
- Preview-only convenience audio.

## Web Audio API considerations

Native HTML5 volume is limited in some environments, especially iOS Safari. The HTML5 fallback supports `useWebAudioApi`, which can enable volume values above `1` and better volume control, but it requires CORS-compatible sources when used with `crossOrigin="anonymous"`.

```tsx
import React from 'react';
import {Html5Audio} from 'remotion';

export const WebAudioVolume: React.FC = () => {
  return (
    <Html5Audio
      src="https://cdn.example.com/music.mp3"
      crossOrigin="anonymous"
      useWebAudioApi
      volume={1.25}
    />
  );
};
```

On Safari, Web Audio API volume control cannot be combined with `playbackRate`; volume changes may be ignored.

## Preserving pitch

`preservePitch` controls preview behavior when playback speed changes. During rendering, pitch changes are controlled through `toneFrequency`.

```tsx
import React from 'react';
import {Audio} from '@remotion/media';

export const FastPodcast: React.FC = () => {
  return (
    <Audio
      src="https://example.com/podcast.mp3"
      playbackRate={1.25}
      fallbackHtml5AudioProps={{
        preservePitch: true,
      }}
    />
  );
};
```

## Practical recommendation

Use this default:

```tsx
<Audio src={staticFile('audio/track.mp3')} volume={0.5} />
```

Only add fallback knobs when you have observed a real fallback case or need Player buffering behavior.
