# `<Player>`

`<Player>` from `@remotion/player` renders a Remotion component inside a regular React app. Use it for previews, landing-page embeds, internal editors, review tools, and interactive demos.

Install:

```bash
npx remotion add @remotion/player
```

## Basic setup

```tsx
import React from 'react';
import {Player} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

export const PromoPlayer: React.FC = () => {
  return (
    <Player
      component={PromoVideo}
      durationInFrames={180}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      style={{width: 480}}
    />
  );
};
```

The Player does not use `<Composition>`. Pass your video component directly.

## Passing input props

```tsx
import React from 'react';
import {Player} from '@remotion/player';
import {PromoVideo, type PromoProps} from './remotion/PromoVideo';

const inputProps: PromoProps = {
  title: 'Summer sale',
  subtitle: '40% off annual plans',
  backgroundColor: '#fef3c7',
};

export const ParameterizedPlayer: React.FC = () => {
  return (
    <Player
      component={PromoVideo}
      inputProps={inputProps}
      durationInFrames={150}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      style={{width: '100%', maxWidth: 540}}
    />
  );
};
```

Unlike renderer input props, Player props may contain functions because they stay in the browser React tree. Still prefer serializable props if the same component is also used for rendering.

## Sizing

`compositionWidth` and `compositionHeight` define the internal coordinate system. The CSS `style` prop controls how large the Player appears in the page.

```tsx
<Player
  component={PromoVideo}
  durationInFrames={120}
  compositionWidth={1920}
  compositionHeight={1080}
  fps={30}
  style={{width: 640, borderRadius: 16, overflow: 'hidden'}}
/>
```

Use `playerRef.current?.getScale()` if you need to align external UI with the scaled video.

## Controls and playback behavior

```tsx
<Player
  component={PromoVideo}
  durationInFrames={240}
  compositionWidth={1080}
  compositionHeight={1080}
  fps={30}
  controls
  autoPlay={false}
  loop={false}
  showVolumeControls
  allowFullscreen
  clickToPlay
  doubleClickToFullscreen
  spaceKeyToPlayOrPause
/>
```

Useful props:

- `controls`: show built-in controls.
- `autoPlay`: start playback when loaded.
- `loop`: restart at the end.
- `initialFrame`: mount at a specific frame.
- `inFrame`, `outFrame`: constrain playback range.
- `playbackRate`: Player playback speed.
- `initiallyMuted`: useful for autoplay-friendly embeds.
- `initialVolume`: fixed initial volume without localStorage persistence.
- `volumePersistenceKey`: localStorage key for persisted volume.
- `showPlaybackRateControl`: show a rate selector.

## Loading and error UI

```tsx
import React, {useCallback} from 'react';
import {AbsoluteFill} from 'remotion';
import {Player, type ErrorFallback, type RenderLoading} from '@remotion/player';

export const RobustPlayer: React.FC = () => {
  const renderLoading: RenderLoading = useCallback(({height, width}) => {
    return (
      <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
        Loading {width}x{height}
      </AbsoluteFill>
    );
  }, []);

  const errorFallback: ErrorFallback = useCallback(({error}) => {
    return (
      <AbsoluteFill
        style={{
          backgroundColor: '#fee2e2',
          color: '#7f1d1d',
          justifyContent: 'center',
          alignItems: 'center',
          padding: 40,
        }}
      >
        Could not preview video: {error.message}
      </AbsoluteFill>
    );
  }, []);

  return (
    <Player
      component={PromoVideo}
      durationInFrames={120}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      renderLoading={renderLoading}
      errorFallback={errorFallback}
      controls
    />
  );
};
```

`renderLoading` is shown for lazy components or React Suspense. `errorFallback` catches render-function errors in the video component.

## License acknowledgement

Some apps pass `acknowledgeRemotionLicense` to suppress the console license message when appropriate for their Remotion license.

```tsx
<Player
  component={PromoVideo}
  durationInFrames={120}
  compositionWidth={1080}
  compositionHeight={1080}
  fps={30}
  acknowledgeRemotionLicense
/>
```

## Checklist

- [ ] Do not wrap Player components in `<Composition>`.
- [ ] Match duration/fps/dimensions to render settings.
- [ ] Use `style` for displayed size, not composition dimensions.
- [ ] Memoize expensive `inputProps`.
- [ ] Add loading and error UI for production embeds.
