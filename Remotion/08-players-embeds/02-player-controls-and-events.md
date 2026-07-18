# Player controls and events

Attach a `PlayerRef` to control playback and listen to events. Keep frame-synchronized UI outside the component that renders `<Player>` to avoid re-rendering the whole player every frame.

## Basic ref controls

```tsx
import React, {useRef} from 'react';
import {Player, type PlayerRef} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

export const ControlledPlayer: React.FC = () => {
  const ref = useRef<PlayerRef>(null);

  return (
    <div>
      <Player
        ref={ref}
        component={PromoVideo}
        durationInFrames={180}
        compositionWidth={1080}
        compositionHeight={1080}
        fps={30}
        controls
      />
      <button onClick={(event) => ref.current?.play(event)}>Play</button>
      <button onClick={() => ref.current?.pause()}>Pause</button>
      <button onClick={(event) => ref.current?.toggle(event)}>Toggle</button>
      <button onClick={() => ref.current?.seekTo(90)}>Seek to frame 90</button>
    </div>
  );
};
```

Pass the click event to `play()` or `toggle()` when playback starts from a user gesture; this helps with browser autoplay policies.

## Useful `PlayerRef` methods

- `play(event?)`
- `pause()`
- `toggle(event?)`
- `seekTo(frame)`
- `getCurrentFrame()`
- `isPlaying()`
- `mute()`, `unmute()`, `isMuted()`
- `getVolume()`, `setVolume(volume)`
- `requestFullscreen()`, `exitFullscreen()`, `isFullscreen()`
- `getScale()`
- `addEventListener()`, `removeEventListener()`

## Event listeners

```tsx
import React, {useEffect, useRef} from 'react';
import {
  Player,
  type CallbackListener,
  type PlayerRef,
} from '@remotion/player';

export const EventfulPlayer: React.FC = () => {
  const ref = useRef<PlayerRef>(null);

  useEffect(() => {
    const player = ref.current;
    if (!player) {
      return;
    }

    const onPlay: CallbackListener<'play'> = () => console.log('play');
    const onPause: CallbackListener<'pause'> = () => console.log('pause');
    const onFrame: CallbackListener<'frameupdate'> = (event) => {
      console.log('frame', event.detail.frame);
    };
    const onError: CallbackListener<'error'> = (event) => {
      console.error(event.detail.error);
    };

    player.addEventListener('play', onPlay);
    player.addEventListener('pause', onPause);
    player.addEventListener('frameupdate', onFrame);
    player.addEventListener('error', onError);

    return () => {
      player.removeEventListener('play', onPlay);
      player.removeEventListener('pause', onPause);
      player.removeEventListener('frameupdate', onFrame);
      player.removeEventListener('error', onError);
    };
  }, []);

  return (
    <Player
      ref={ref}
      component={PromoVideo}
      durationInFrames={180}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
    />
  );
};
```

Common events:

- `play`, `pause`, `ended`
- `seeked`
- `timeupdate`: throttled periodic updates
- `frameupdate`: every frame update
- `ratechange`
- `volumechange`, `mutechange`
- `fullscreenchange`, `scalechange`
- `waiting`, `resume`
- `error`

Use `timeupdate` for UI that does not need every frame. Use `frameupdate` for frame-accurate indicators.

## Efficient current-frame hook

```tsx
import {useCallback, useSyncExternalStore} from 'react';
import type {CallbackListener, PlayerRef} from '@remotion/player';

export const useCurrentPlayerFrame = (
  ref: React.RefObject<PlayerRef | null>,
) => {
  const subscribe = useCallback(
    (onStoreChange: () => void) => {
      const player = ref.current;
      if (!player) {
        return () => undefined;
      }

      const updater: CallbackListener<'frameupdate'> = () => {
        onStoreChange();
      };

      player.addEventListener('frameupdate', updater);
      return () => player.removeEventListener('frameupdate', updater);
    },
    [ref],
  );

  return useSyncExternalStore(
    subscribe,
    () => ref.current?.getCurrentFrame() ?? 0,
    () => 0,
  );
};
```

Use it in a sibling component:

```tsx
import React, {useRef} from 'react';
import {Player, type PlayerRef} from '@remotion/player';
import {useCurrentPlayerFrame} from './useCurrentPlayerFrame';

const TimeDisplay: React.FC<{playerRef: React.RefObject<PlayerRef | null>}> = ({
  playerRef,
}) => {
  const frame = useCurrentPlayerFrame(playerRef);
  return <div>Current frame: {frame}</div>;
};

export const PlayerWithTime: React.FC = () => {
  const playerRef = useRef<PlayerRef>(null);

  return (
    <>
      <Player
        ref={playerRef}
        component={PromoVideo}
        durationInFrames={180}
        compositionWidth={1080}
        compositionHeight={1080}
        fps={30}
      />
      <TimeDisplay playerRef={playerRef} />
    </>
  );
};
```

## Custom control buttons

```tsx
import React, {useCallback} from 'react';
import {
  Player,
  type RenderFullscreenButton,
  type RenderPlayPauseButton,
} from '@remotion/player';

export const CustomControlsPlayer: React.FC = () => {
  const renderPlayPauseButton: RenderPlayPauseButton = useCallback(
    ({playing, isBuffering}) => {
      if (isBuffering) {
        return <span>Loading...</span>;
      }
      return <span>{playing ? 'Pause' : 'Play'}</span>;
    },
    [],
  );

  const renderFullscreenButton: RenderFullscreenButton = useCallback(
    ({isFullscreen}) => <span>{isFullscreen ? 'Exit' : 'Fullscreen'}</span>,
    [],
  );

  return (
    <Player
      component={PromoVideo}
      durationInFrames={180}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      renderPlayPauseButton={renderPlayPauseButton}
      renderFullscreenButton={renderFullscreenButton}
    />
  );
};
```

For custom mute buttons, attach click behavior through `PlayerRef`; the render callback only renders UI.

## Buffer state UI

`waiting` fires immediately when the Player enters buffering. `renderPoster()` and `renderPlayPauseButton()` report `isBuffering` only after `bufferStateDelayInMilliseconds`.

```tsx
<Player
  component={PromoVideo}
  durationInFrames={180}
  compositionWidth={1080}
  compositionHeight={1080}
  fps={30}
  bufferStateDelayInMilliseconds={200}
/>
```

## Checklist

- [ ] Clean up every event listener.
- [ ] Use `timeupdate` unless you need every frame.
- [ ] Keep current-frame UI in a sibling component.
- [ ] Pass click events to `play()`/`toggle()` from user gestures.
- [ ] Use `PlayerRef` for custom controls that need actions.
