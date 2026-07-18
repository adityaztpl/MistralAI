# Audio visualization

Remotion's `@remotion/media-utils` package can decode waveform data and map it to frame-based visualization values. The core APIs are:

- `getAudioData(src, options?)`: async function that loads and decodes audio/video audio data.
- `useAudioData(src, options?)`: hook that wraps `getAudioData()` in a `delayRender()` / `continueRender()` pattern.
- `visualizeAudio({fps, frame, audioData, numberOfSamples})`: returns samples for the current frame.

Install:

```bash
npx remotion add @remotion/media-utils
```

## Basic waveform bars

```tsx
import React from 'react';
import {AbsoluteFill, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {Audio} from '@remotion/media';
import {useAudioData, visualizeAudio} from '@remotion/media-utils';

export const WaveformBars: React.FC = () => {
  const src = staticFile('audio/music.mp3');
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const audioData = useAudioData(src);

  if (!audioData) {
    return null;
  }

  const samples = visualizeAudio({
    fps,
    frame,
    audioData,
    numberOfSamples: 48,
  });

  return (
    <AbsoluteFill
      style={{
        backgroundColor: '#020617',
        justifyContent: 'center',
        alignItems: 'center',
      }}
    >
      <Audio src={src} />
      <div style={{display: 'flex', gap: 6, alignItems: 'center'}}>
        {samples.map((sample, index) => (
          <div
            key={index}
            style={{
              width: 10,
              height: 24 + sample * 260,
              borderRadius: 999,
              backgroundColor: '#38bdf8',
            }}
          />
        ))}
      </div>
    </AbsoluteFill>
  );
};
```

`useAudioData()` returns `null` while loading. It internally handles render delays, so the final render does not capture the loading state.

## Circular visualizer

```tsx
import React from 'react';
import {
  AbsoluteFill,
  interpolate,
  staticFile,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';
import {Audio} from '@remotion/media';
import {useAudioData, visualizeAudio} from '@remotion/media-utils';

export const CircularVisualizer: React.FC = () => {
  const src = staticFile('audio/drums.wav');
  const frame = useCurrentFrame();
  const {fps, width, height} = useVideoConfig();
  const audioData = useAudioData(src, {sampleRate: 48000});

  if (!audioData) {
    return null;
  }

  const samples = visualizeAudio({
    fps,
    frame,
    audioData,
    numberOfSamples: 64,
  });

  const centerX = width / 2;
  const centerY = height / 2;

  return (
    <AbsoluteFill style={{backgroundColor: '#111827'}}>
      <Audio src={src} />
      <svg width={width} height={height}>
        {samples.map((sample, index) => {
          const angle = (Math.PI * 2 * index) / samples.length;
          const radius = interpolate(sample, [0, 1], [220, 430]);
          const x2 = centerX + Math.cos(angle) * radius;
          const y2 = centerY + Math.sin(angle) * radius;
          const x1 = centerX + Math.cos(angle) * 180;
          const y1 = centerY + Math.sin(angle) * 180;

          return (
            <line
              key={index}
              x1={x1}
              y1={y1}
              x2={x2}
              y2={y2}
              stroke="#a78bfa"
              strokeWidth={6}
              strokeLinecap="round"
            />
          );
        })}
      </svg>
    </AbsoluteFill>
  );
};
```

## Server-side preloading with `getAudioData()`

`getAudioData()` is useful in scripts, metadata calculation, or custom hooks. It returns waveform data, duration, sample rate, channel count, and metadata about whether the source is remote.

```tsx
import {getAudioData} from '@remotion/media-utils';
import {staticFile} from 'remotion';

export const inspectAudio = async () => {
  const audioData = await getAudioData(staticFile('audio/music.mp3'), {
    sampleRate: 48000,
  });

  console.log(audioData.durationInSeconds);
  console.log(audioData.numberOfChannels);
  console.log(audioData.sampleRate);
};
```

Remote files need CORS support for waveform decoding. If you only need duration, use `getAudioDurationInSeconds()` instead because it is faster.

## Visualizer component with props

```tsx
import React from 'react';
import {Audio} from '@remotion/media';
import {useAudioData, visualizeAudio} from '@remotion/media-utils';
import {AbsoluteFill, useCurrentFrame, useVideoConfig} from 'remotion';

type AudioMeterProps = {
  src: string;
  bars: number;
  color: string;
};

export const AudioMeter: React.FC<AudioMeterProps> = ({src, bars, color}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const audioData = useAudioData(src);

  if (!audioData) {
    return null;
  }

  const values = visualizeAudio({
    fps,
    frame,
    audioData,
    numberOfSamples: bars,
  });

  return (
    <AbsoluteFill style={{justifyContent: 'flex-end', padding: 80}}>
      <Audio src={src} />
      <div style={{display: 'grid', gridTemplateColumns: `repeat(${bars}, 1fr)`, gap: 4}}>
        {values.map((value, index) => (
          <div
            key={index}
            style={{
              height: 8 + value * 180,
              backgroundColor: color,
              borderRadius: 8,
              alignSelf: 'end',
            }}
          />
        ))}
      </div>
    </AbsoluteFill>
  );
};
```

Use it from a composition:

```tsx
import React from 'react';
import {staticFile} from 'remotion';
import {AudioMeter} from './AudioMeter';

export const PodcastClip: React.FC = () => {
  return (
    <AudioMeter
      src={staticFile('audio/podcast-intro.mp3')}
      bars={80}
      color="#22c55e"
    />
  );
};
```

## Performance notes

- `getAudioData()` memoizes results for the same `src`; reload the page to clear the cache.
- Use a stable `sampleRate` such as `48000` for deterministic behavior across devices.
- Do not request hundreds of samples if the visual design only needs dozens.
- Render SVG or div bars for simple visualizers; use canvas only when drawing many points.
- Keep remote visualization sources CORS-compatible.

## Failure behavior

`useAudioData()` can throw if the file has no audio track. If you need a custom fallback, build your own hook with `getAudioData()`, `delayRender()`, and `cancelRender()`, or validate media server-side before selecting it.
