# Thumbnail and poster

Use `<Thumbnail>` to render one frame of a Remotion component in a normal React app. Use `renderPoster` on `<Player>` to show an overlay before playback, while paused, when ended, or while buffering.

## `<Thumbnail>`

```tsx
import React from 'react';
import {Thumbnail} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

export const PromoThumbnail: React.FC = () => {
  return (
    <Thumbnail
      component={PromoVideo}
      compositionWidth={1080}
      compositionHeight={1080}
      durationInFrames={150}
      fps={30}
      frameToDisplay={45}
      inputProps={{
        title: 'Summer sale',
        subtitle: '40% off',
        backgroundColor: '#fef3c7',
      }}
      style={{width: 280}}
    />
  );
};
```

`<Thumbnail>` does not use `<Composition>`. Pass the component directly.

Important props:

- `component` or `lazyComponent`
- `frameToDisplay`
- `compositionWidth`, `compositionHeight`
- `durationInFrames`
- `fps`
- `inputProps`
- `renderLoading`
- `errorFallback`
- `style`, `className`

## Thumbnail loading and errors

```tsx
import React, {useCallback} from 'react';
import {AbsoluteFill} from 'remotion';
import {
  Thumbnail,
  type ErrorFallback,
  type RenderLoading,
} from '@remotion/player';

export const RobustThumbnail: React.FC = () => {
  const renderLoading: RenderLoading = useCallback(({width, height}) => {
    return (
      <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
        Loading thumbnail {width}x{height}
      </AbsoluteFill>
    );
  }, []);

  const errorFallback: ErrorFallback = useCallback(({error}) => {
    return (
      <AbsoluteFill style={{backgroundColor: '#fee2e2', padding: 24}}>
        {error.message}
      </AbsoluteFill>
    );
  }, []);

  return (
    <Thumbnail
      component={PromoVideo}
      compositionWidth={1080}
      compositionHeight={1080}
      durationInFrames={150}
      fps={30}
      frameToDisplay={30}
      renderLoading={renderLoading}
      errorFallback={errorFallback}
    />
  );
};
```

## Thumbnail ref

```tsx
import React, {useEffect, useRef} from 'react';
import {Thumbnail, type ThumbnailRef} from '@remotion/player';

export const MeasuredThumbnail: React.FC = () => {
  const ref = useRef<ThumbnailRef>(null);

  useEffect(() => {
    const thumbnail = ref.current;
    if (!thumbnail) {
      return;
    }

    console.log('scale', thumbnail.getScale());
    const node = thumbnail.getContainerNode();
    console.log(node);
  }, []);

  return (
    <Thumbnail
      ref={ref}
      component={PromoVideo}
      compositionWidth={1080}
      compositionHeight={1080}
      durationInFrames={150}
      fps={30}
      frameToDisplay={60}
    />
  );
};
```

## Player poster

`renderPoster` renders an overlay. Control when it appears with poster state props.

```tsx
import React, {useCallback} from 'react';
import {AbsoluteFill} from 'remotion';
import {Player, type RenderPoster} from '@remotion/player';

export const PlayerWithPoster: React.FC = () => {
  const renderPoster: RenderPoster = useCallback(({height, width, isBuffering}) => {
    return (
      <AbsoluteFill
        style={{
          width,
          height,
          backgroundColor: '#020617',
          color: 'white',
          justifyContent: 'center',
          alignItems: 'center',
        }}
      >
        <div style={{fontSize: 42}}>
          {isBuffering ? 'Buffering...' : 'Click to play'}
        </div>
      </AbsoluteFill>
    );
  }, []);

  return (
    <Player
      component={PromoVideo}
      durationInFrames={150}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      renderPoster={renderPoster}
      showPosterWhenUnplayed
      showPosterWhenBuffering
      showPosterWhenBufferingAndPaused
    />
  );
};
```

Poster state props:

- `showPosterWhenUnplayed`
- `showPosterWhenPaused`
- `showPosterWhenEnded`
- `showPosterWhenBuffering`
- `showPosterWhenBufferingAndPaused`

For `showPosterWhenEnded`, use `moveToBeginningWhenEnded={false}`.

## Poster fill mode

```tsx
<Player
  component={PromoVideo}
  durationInFrames={150}
  compositionWidth={1080}
  compositionHeight={1080}
  fps={30}
  renderPoster={renderPoster}
  showPosterWhenUnplayed
  posterFillMode="composition-size"
/>
```

- `player-size`: poster renders at displayed Player size. Good for fixed-size UI such as a play button.
- `composition-size`: poster renders in composition coordinates and scales with the video. Good for freeze-frame-like posters.

## Preloading assets for Player

```tsx
import {preloadAudio, preloadImage, preloadVideo} from '@remotion/preload';

const cleanupImage = preloadImage('https://cdn.example.com/poster.jpg');
const cleanupVideo = preloadVideo('https://cdn.example.com/preview.mp4');
const cleanupAudio = preloadAudio('https://cdn.example.com/music.mp3');

// Later:
cleanupImage();
cleanupVideo();
cleanupAudio();
```

For guaranteed full download, use `prefetch()`:

```tsx
import {prefetch} from 'remotion';

const {waitUntilDone, free} = prefetch('https://cdn.example.com/short-preview.mp4');

await waitUntilDone();
free();
```

`prefetch()` is heavier because it downloads the full media and creates a Blob URL. Prefer `@remotion/preload` for large assets unless you need a readiness promise.

## Grid of thumbnails

```tsx
import React from 'react';
import {Thumbnail} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

const variants = [
  {title: 'Summer sale', color: '#fef3c7'},
  {title: 'Webinar', color: '#dbeafe'},
  {title: 'New feature', color: '#dcfce7'},
];

export const ThumbnailGrid: React.FC = () => {
  return (
    <div style={{display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16}}>
      {variants.map((variant) => (
        <Thumbnail
          key={variant.title}
          component={PromoVideo}
          compositionWidth={1080}
          compositionHeight={1080}
          durationInFrames={150}
          fps={30}
          frameToDisplay={45}
          inputProps={{
            title: variant.title,
            subtitle: 'Preview',
            backgroundColor: variant.color,
          }}
          style={{width: '100%'}}
        />
      ))}
    </div>
  );
};
```

## Checklist

- [ ] Use `<Thumbnail>` for static preview frames.
- [ ] Use `renderPoster` for playback-state overlays.
- [ ] Choose `posterFillMode` based on whether poster UI should scale with the composition.
- [ ] Preload media for responsive Player startup.
- [ ] Add error/loading fallbacks for thumbnail grids.
