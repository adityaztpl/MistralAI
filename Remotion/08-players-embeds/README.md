# 08-players-embeds: Remotion Player, controls, embeds, and thumbnails

`@remotion/player` lets you render a Remotion component inside a normal React app such as Next.js, Vite, or Create React App. It does **not** use `<Composition>` directly; you pass a component plus duration, fps, dimensions, and optional `inputProps`.

Core APIs:

- `<Player>` from `@remotion/player`
- `PlayerRef` for imperative control
- Player events such as `play`, `pause`, `frameupdate`, `timeupdate`, `error`, `waiting`, and `resume`
- `<Thumbnail>` from `@remotion/player`
- `renderPoster`, poster display props, and `@remotion/preload`

## Files

| Guide | Focus |
|---|---|
| [01 Remotion Player](01-remotion-player.md) | Basic Player setup, props, sizing, input props |
| [02 Player controls and events](02-player-controls-and-events.md) | Refs, custom controls, events, time display |
| [03 Embedding in Next/React](03-embedding-in-next-react.md) | Next.js/Vite patterns, lazy components, client boundaries |
| [04 Thumbnail and poster](04-thumbnail-and-poster.md) | `<Thumbnail>`, `renderPoster`, loading states, preloading |

## Minimal Player

```tsx
import React from 'react';
import {Player} from '@remotion/player';
import {MyVideo} from './remotion/MyVideo';

export const App: React.FC = () => {
  return (
    <Player
      component={MyVideo}
      durationInFrames={120}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      style={{width: 400}}
    />
  );
};
```

## Key differences from rendering

- Player runs in the browser; it is for preview and interactive embeds.
- It does not read `<Composition>` registrations.
- Props are passed through `inputProps`.
- Functions are allowed in Player props because they do not cross the renderer serialization boundary.
- Media autoplay, CORS, buffering, and user gestures matter like normal web media.

## Embedding checklist

- [ ] Use `controls` for a quick default UI.
- [ ] Pass stable `inputProps` where possible.
- [ ] Use `lazyComponent` with `useCallback()` for code splitting.
- [ ] Keep Player refs out of components that re-render every frame.
- [ ] Use `renderPoster` for initial, paused, ended, or buffering overlays.
- [ ] Preload important assets for responsive playback.
