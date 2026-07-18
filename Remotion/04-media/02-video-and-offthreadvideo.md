# `<Video>`, `<OffthreadVideo>`, and transparent video notes

For new video usage, prefer `<Video>` from `@remotion/media`. Remotion's docs describe it as the recommended video component: during rendering it extracts exact frames using Mediabunny and draws them into a `<canvas>`, keeping playback synchronized with the Remotion timeline.

`<OffthreadVideo>` remains important when you need direct FFmpeg-based extraction or need to understand fallback behavior. During rendering, it extracts frames outside the browser and displays them through an image-like element; during preview, it behaves closer to a video element.

## Installation and imports

```bash
npx remotion add @remotion/media
```

```tsx
import {Video} from '@remotion/media';
import {AbsoluteFill, OffthreadVideo, staticFile} from 'remotion';
```

## Recommended: `<Video>` from `@remotion/media`

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const HeroVideo: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: 'black'}}>
      <Video
        src={staticFile('clips/launch.mp4')}
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
      />
    </AbsoluteFill>
  );
};
```

Useful `<Video>` props:

- `src`: local `staticFile()` URL, remote URL, or supported HLS playlist.
- `from`, `durationInFrames`: clip-level timing, same idea as `<Sequence>`.
- `trimBefore`, `trimAfter`: trim media content without changing where the clip starts.
- `volume`: static number or per-frame callback.
- `playbackRate`: speed up or slow down; reverse playback is not supported.
- `loop`: loop the clip indefinitely.
- `objectFit`: `'contain'`, `'cover'`, `'fill'`, `'none'`, or `'scale-down'`. Use this instead of CSS `object-fit`.
- `requestInit`: fetch options such as credentials or `cache: 'no-store'`.
- `onError`: return `'fallback'` to fall back to `<OffthreadVideo>` or `'fail'` to fail.
- `fallbackOffthreadVideoProps`: configure direct offthread fallback props like `transparent` or `toneMapped`.

## Clip timing without a wrapping sequence

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const BrollLayer: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video
        name="B-roll: warehouse"
        from={45}
        durationInFrames={120}
        trimBefore={30}
        trimAfter={180}
        src={staticFile('clips/warehouse.mp4')}
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
        volume={0}
      />
    </AbsoluteFill>
  );
};
```

In this example:

- The layer appears at composition frame `45`.
- Playback starts from frame `30` of the file.
- Playback stops at frame `180` of the file.
- The clip stays mounted for `120` composition frames.

## Volume and speed

```tsx
import React from 'react';
import {AbsoluteFill, interpolate, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const SpeedRampClip: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video
        src={staticFile('clips/city-night.mp4')}
        playbackRate={1.5}
        volume={(frame) =>
          interpolate(frame, [0, 30, 150, 180], [0, 0.8, 0.8, 0], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          })
        }
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
      />
    </AbsoluteFill>
  );
};
```

Set `muted` when the video has no useful audio. Remotion does not need to download and mix the video's audio during rendering if it is muted.

## Remote video and HLS

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {Video} from '@remotion/media';

export const RemoteVideo: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video
        src="https://remotion.media/BigBuckBunny.mp4"
        requestInit={{cache: 'no-store'}}
        style={{width: '100%', height: '100%'}}
        objectFit="contain"
      />
    </AbsoluteFill>
  );
};
```

For remote media, check:

- The server supports range requests and stable downloads.
- Authentication is expressed through `requestInit` if needed.
- CORS is configured if you need canvas processing, Web Audio, or browser-side fetches.
- You have a fallback policy: fail early for compliance-critical renders, or fallback for best-effort content.

## Direct `<OffthreadVideo>`

`<OffthreadVideo>` extracts frames using FFmpeg during rendering. It is not supported in client-side rendering; use `<Video>` from `@remotion/media` for client-side rendering and Player-compatible media.

```tsx
import React from 'react';
import {AbsoluteFill, OffthreadVideo, staticFile} from 'remotion';

export const FfmpegExtractedClip: React.FC = () => {
  return (
    <AbsoluteFill>
      <OffthreadVideo
        src={staticFile('clips/prores-title.mov')}
        trimBefore={12}
        trimAfter={132}
        style={{width: '100%', height: '100%', objectFit: 'cover'}}
      />
    </AbsoluteFill>
  );
};
```

Useful `<OffthreadVideo>` props:

- `transparent`: extract frames as PNG to preserve alpha.
- `toneMapped`: disable only if you can tolerate color inaccuracies and want faster extraction.
- `audioStreamIndex`: choose an audio stream during render.
- `toneFrequency`: render-time pitch adjustment.
- `onVideoFrame`: inspect or manipulate extracted frames.
- `pauseWhenBuffering`: integrate with Player buffering behavior.
- `delayRenderTimeoutInMilliseconds`, `delayRenderRetries`: tune internal render delays.

## Transparent video

Transparent video is usually used for overlays: alpha-channel lower thirds, confetti, smoke, or ProRes/VP9 assets. With `<OffthreadVideo>`, set `transparent` to `true`. With `<Video>`, pass it through the fallback props when fallback extraction matters.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const TransparentOverlay: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video
        src={staticFile('overlays/smoke-alpha.webm')}
        fallbackOffthreadVideoProps={{
          transparent: true,
        }}
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
      />
    </AbsoluteFill>
  );
};
```

Trade-offs:

- Transparent extraction is slower because PNG frames are heavier than bitmap extraction.
- Only enable it when the video actually has an alpha channel and the final composition needs it.
- For exported transparent videos, coordinate this with render settings such as codec, image format, and pixel format.

## Offthread loop pattern

`<OffthreadVideo>` does not implement a native `loop` prop. If you need looping and can use `@remotion/media`, prefer `<Video loop>`. If you must use offthread extraction, measure the clip duration and wrap it in `<Loop>`.

```tsx
import React from 'react';
import {Loop, OffthreadVideo, staticFile} from 'remotion';

export const LoopingOffthreadBackground: React.FC = () => {
  return (
    <Loop durationInFrames={90}>
      <OffthreadVideo
        src={staticFile('clips/short-texture.webm')}
        muted
        style={{width: '100%', height: '100%', objectFit: 'cover'}}
      />
    </Loop>
  );
};
```

For dynamic source durations, compute duration ahead of time in `calculateMetadata()` or an async pre-render step rather than guessing.

## Debug decision tree

1. **Frame mismatch or browser seeking problem?** Prefer `<Video>` or direct `<OffthreadVideo>` extraction over native HTML5 behavior.
2. **Need client-side rendering or Player usage?** Use `<Video>` from `@remotion/media`.
3. **Need alpha channel?** Use transparent extraction and check codec support.
4. **Huge file or slow render?** Mute unused audio, avoid transparent extraction, consider cache sizing and clipping with `trimBefore`/`trimAfter`.
5. **Protected remote URL?** Use `requestInit`, signed URLs, or controlled CDN delivery.

## Interview-style summary

Say: "For new video clips I use `@remotion/media`'s `<Video>` because it is synchronized to the Remotion timeline, supports buffering, and can fall back to offthread extraction. I use `<OffthreadVideo>` when I need to configure FFmpeg-based extraction directly, especially for exact frames or transparent overlays. Transparent video is slower, so I only enable alpha extraction when the source and output require it."
